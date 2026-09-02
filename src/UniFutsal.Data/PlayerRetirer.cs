using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using UniFutsal.Core.Rng;

namespace UniFutsal.Data
{
    /// <summary>
    /// Gestiona las retiradas de jugadores al final de cada temporada.
    /// Determinista vía IRng. Los retirados no se borran de la BD,
    /// solo se marca retired=1 (conservan historial).
    /// </summary>
    public sealed class PlayerRetirer
    {
        private readonly string _dbPath;

        /// <summary>Edad mínima para considerar retirada (campo).</summary>
        public const int RETIREMENT_MIN_AGE_FIELD = 34;

        /// <summary>Edad de retirada obligatoria.</summary>
        public const int RETIREMENT_MANDATORY_AGE = 38;

        /// <summary>Bonus de años para porteros (aguantan más).</summary>
        public const int GK_GRACE_YEARS = 2;

        public PlayerRetirer(string dbPath)
        {
            _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
        }

        /// <summary>
        /// Procesa las retiradas de jugadores al inicio de una nueva temporada.
        /// Devuelve la lista de person_id que se retiraron.
        /// </summary>
        public List<long> ProcessRetirements(string newSeasonLabel)
        {
            int seasonYear = ParseSeasonYear(newSeasonLabel);

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            // RNG determinista por temporada (substream diferente al de desarrollo)
            ulong masterSeed = (ulong)seasonYear * 0x6A09E667F3BCC908UL + 0xCAFEBABE;
            var rng = new Xoshiro256StarStar(masterSeed);

            var retiredIds = new List<long>();

            // Cargar jugadores activos (no retirados) con su edad
            var candidates = LoadRetirementCandidates(connection, seasonYear);

            foreach (var c in candidates)
            {
                int effectiveAge = c.Age - (c.IsGoalkeeper ? GK_GRACE_YEARS : 0);

                double retirementProb = ComputeRetirementProbability(effectiveAge);
                if (retirementProb <= 0.0) continue;

                if (rng.Chance(retirementProb))
                {
                    RetirePlayer(connection, c.PersonId);
                    retiredIds.Add(c.PersonId);
                }
            }

            return retiredIds;
        }

        /// <summary>
        /// Marca como expirados todos los contratos cuya effective_until sea anterior
        /// al inicio de la nueva temporada. Devuelve el número de contratos expirados.
        /// </summary>
        public int ProcessContractExpirations(string newSeasonLabel)
        {
            int seasonYear = ParseSeasonYear(newSeasonLabel);
            // Los contratos expiran el 30 de junio del año de fin de temporada.
            // Si la nueva temporada empieza el 1 de agosto de "seasonYear",
            // expiran los que tienen effective_until antes de esa fecha.
            string cutoffDate = $"{seasonYear}-07-01";

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            using var cmd = new SqliteCommand(@"
                UPDATE contracts
                SET status = 'expirado'
                WHERE status = 'vigente'
                  AND effective_until < @cutoff", connection);
            cmd.Parameters.AddWithValue("@cutoff", cutoffDate);
            return cmd.ExecuteNonQuery();
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

        private sealed class RetirementCandidate
        {
            public long PersonId;
            public int Age;
            public bool IsGoalkeeper;
        }

        private List<RetirementCandidate> LoadRetirementCandidates(
            SqliteConnection connection, int seasonYear)
        {
            var candidates = new List<RetirementCandidate>();
            // Nota: persons.id == players.person_id (misma clave).
            // Usamos p.id porque persons no tiene columna person_id.
            using var cmd = new SqliteCommand(@"
                SELECT p.id, p.birth_date, pl.position_main
                FROM persons p
                JOIN players pl ON pl.person_id = p.id
                WHERE pl.retired = 0", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string birthDate = reader.GetString(1);
                int birthYear = int.Parse(birthDate.Substring(0, 4));
                int age = seasonYear - birthYear;

                // Solo considerar los que tienen al menos la edad mínima
                if (age < RETIREMENT_MIN_AGE_FIELD - GK_GRACE_YEARS) continue;

                candidates.Add(new RetirementCandidate
                {
                    PersonId = reader.GetInt64(0),
                    Age = age,
                    IsGoalkeeper = reader.GetString(2) == "POR"
                });
            }
            return candidates;
        }

        private static double ComputeRetirementProbability(int effectiveAge)
        {
            if (effectiveAge >= RETIREMENT_MANDATORY_AGE) return 1.0;
            if (effectiveAge >= 37) return 0.60;
            if (effectiveAge >= 36) return 0.30;
            if (effectiveAge >= 35) return 0.15;
            if (effectiveAge >= 34) return 0.10;
            return 0.0;
        }

        private void RetirePlayer(SqliteConnection connection, long personId)
        {
            // Marcar jugador como retirado
            using var updatePlayer = new SqliteCommand(
                "UPDATE players SET retired = 1 WHERE person_id = @pid", connection);
            updatePlayer.Parameters.AddWithValue("@pid", personId);
            updatePlayer.ExecuteNonQuery();

            // Expirar sus contratos vigentes (si los hubiera)
            using var expireContracts = new SqliteCommand(
                @"UPDATE contracts
                  SET status = 'expirado'
                  WHERE person_id = @pid AND status = 'vigente'", connection);
            expireContracts.Parameters.AddWithValue("@pid", personId);
            expireContracts.ExecuteNonQuery();
        }
    }
}