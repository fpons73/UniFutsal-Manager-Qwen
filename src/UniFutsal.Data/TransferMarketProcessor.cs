using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using UniFutsal.Core.Rng;

namespace UniFutsal.Data
{
    /// <summary>
    /// Resultado del mercado de fichajes.
    /// </summary>
    public sealed class TransferMarketResult
    {
        public int TotalTransfers { get; set; }
        public List<string> TransfersDescription { get; set; } = new List<string>();
    }

    /// <summary>
    /// Mercado de fichajes IA: los clubes contratan jugadores libres
    /// para reemplazar retirados y mantener plantillas competitivas.
    /// Determinista vía IRng.
    /// </summary>
    public sealed class TransferMarketProcessor
    {
        private readonly string _dbPath;

        public const int MIN_SQUAD_SIZE = 12;
        public const int MAX_SQUAD_SIZE = 15;

        public TransferMarketProcessor(string dbPath)
        {
            _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
        }

        /// <summary>
        /// Procesa el mercado de fichajes al inicio de una nueva temporada.
        /// </summary>
        public TransferMarketResult Process(string newSeasonLabel)
        {
            int seasonYear = ParseSeasonYear(newSeasonLabel);
            var result = new TransferMarketResult();

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            // RNG determinista para el mercado
            ulong masterSeed = (ulong)seasonYear * 0xB5C0428D4B9B7F1AUL + 0xFEEDFACE;
            var rng = new Xoshiro256StarStar(masterSeed);

            // 1. Obtener clubes activos con su plantilla actual
            var clubs = GetActiveClubsWithSquadSize(connection);

            // 2. Obtener jugadores libres (sin contrato vigente)
            var freeAgents = GetFreeAgents(connection);

            // 3. Procesar cada club que necesite refuerzos
            foreach (var club in clubs)
            {
                int squadSize = club.SquadSize;
                int needed = MIN_SQUAD_SIZE - squadSize;

                if (needed <= 0) continue;
                if (freeAgents.Count == 0) break;

                // Contratar jugadores hasta llegar al mínimo
                int signings = 0;
                while (signings < needed && freeAgents.Count > 0)
                {
                    // Seleccionar un jugador libre aleatoriamente
                    int agentIndex = rng.NextInt(0, freeAgents.Count);
                    var agent = freeAgents[agentIndex];

                    // Crear contrato
                    SignPlayer(connection, rng, club.ClubId, agent.PersonId, newSeasonLabel);

                    result.TransfersDescription.Add(
                        $"{club.ClubName} ficha a {agent.FirstName} {agent.LastName} (CA {agent.CA})");

                    freeAgents.RemoveAt(agentIndex);
                    signings++;
                    result.TotalTransfers++;
                }
            }

            return result;
        }

        // ===== Helpers =====

        private static int ParseSeasonYear(string seasonLabel)
        {
            var parts = seasonLabel.Split('/');
            if (parts.Length >= 1 && int.TryParse(parts[0], out int year))
            {
                return year;
            }
            throw new ArgumentException($"Formato de temporada no reconocido: '{seasonLabel}'");
        }

        private sealed class ClubSquad
        {
            public long ClubId;
            public string ClubName = string.Empty;
            public int SquadSize;
        }

        private sealed class FreeAgent
        {
            public long PersonId;
            public string FirstName = string.Empty;
            public string LastName = string.Empty;
            public int CA;
            public int Age;
        }

        private List<ClubSquad> GetActiveClubsWithSquadSize(SqliteConnection connection)
        {
            var clubs = new List<ClubSquad>();
            using var cmd = new SqliteCommand(@"
                SELECT c.id, c.name,
                    (SELECT COUNT(*) FROM contracts ct
                     WHERE ct.club_id = c.id
                       AND ct.status = 'vigente'
                       AND ct.scope = 'primer_equipo') as squad_size
                FROM clubs c
                WHERE c.is_active = 1
                ORDER BY squad_size ASC", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                clubs.Add(new ClubSquad
                {
                    ClubId = reader.GetInt64(0),
                    ClubName = reader.GetString(1),
                    SquadSize = reader.GetInt32(2)
                });
            }
            return clubs;
        }

        private List<FreeAgent> GetFreeAgents(SqliteConnection connection)
        {
            var agents = new List<FreeAgent>();
            // Jugadores no retirados sin contrato vigente
            using var cmd = new SqliteCommand(@"
                SELECT p.id, p.first_name, p.last_name, pl.current_ability, p.birth_date
                FROM persons p
                JOIN players pl ON pl.person_id = p.id
                WHERE pl.retired = 0
                  AND NOT EXISTS (
                    SELECT 1 FROM contracts c
                    WHERE c.person_id = p.id AND c.status = 'vigente'
                  )
                ORDER BY pl.current_ability DESC", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string birthDate = reader.GetString(4);
                int birthYear = int.Parse(birthDate.Substring(0, 4));
                int age = 2027 - birthYear; // referencia aproximada
                agents.Add(new FreeAgent
                {
                    PersonId = reader.GetInt64(0),
                    FirstName = reader.GetString(1),
                    LastName = reader.GetString(2),
                    CA = reader.GetInt32(3),
                    Age = age
                });
            }
            return agents;
        }

        private void SignPlayer(SqliteConnection connection, IRng rng,
            long clubId, long personId, string seasonLabel)
        {
            int seasonYear = ParseSeasonYear(seasonLabel);
            string signedOn = $"{seasonYear}-07-15";
            string effectiveFrom = $"{seasonYear}-07-15";

            // Duración: 60% 2 años, 30% 3 años, 10% 1 año
            double roll = rng.NextDouble();
            int duration;
            if (roll < 0.10) duration = 1;
            else if (roll < 0.70) duration = 2;
            else duration = 3;
            string effectiveUntil = $"{seasonYear + duration}-06-30";

            // Obtener CA del jugador para calcular salario
            int ca = 0;
            using var caCmd = new SqliteCommand(
                "SELECT current_ability FROM players WHERE person_id = @pid", connection);
            caCmd.Parameters.AddWithValue("@pid", personId);
            var result = caCmd.ExecuteScalar();
            if (result != null && result != DBNull.Value)
            {
                ca = Convert.ToInt32(result);
            }

            // Salario: 1500 + (CA * 25) + ruido
            int wage = 1500 + (ca * 25) + rng.NextInt(-300, 301);
            if (wage < 500) wage = 500;

            // Release clause: 50 salarios (70% probabilidad)
            int release = rng.NextDouble() < 0.7 ? wage * 50 : 0;

            using var insertCmd = new SqliteCommand(@"
                INSERT INTO contracts
                    (person_id, club_id, scope, signed_on, effective_from, effective_until,
                     wage_monthly, release_clause, status)
                VALUES
                    (@pid, @cid, 'primer_equipo', @signed, @from, @until,
                     @wage, @release, 'vigente')", connection);
            insertCmd.Parameters.AddWithValue("@pid", personId);
            insertCmd.Parameters.AddWithValue("@cid", clubId);
            insertCmd.Parameters.AddWithValue("@signed", signedOn);
            insertCmd.Parameters.AddWithValue("@from", effectiveFrom);
            insertCmd.Parameters.AddWithValue("@until", effectiveUntil);
            insertCmd.Parameters.AddWithValue("@wage", wage);
            insertCmd.Parameters.AddWithValue("@release", release);
            insertCmd.ExecuteNonQuery();
        }
    }
}