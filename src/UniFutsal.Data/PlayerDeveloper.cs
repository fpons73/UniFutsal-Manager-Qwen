using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using UniFutsal.Core.Domain.People;
using UniFutsal.Core.Rng;

namespace UniFutsal.Data
{
    /// <summary>
    /// Desarrolla jugadores: envejece, mejora jóvenes, declina veteranos.
    /// Determinista vía IRng. Usa Xoshiro256** sembrado con la temporada.
    /// </summary>
    public sealed class PlayerDeveloper
    {
        private readonly string _dbPath;

        /// <summary>Edad de pico para jugadores de campo.</summary>
        public const int FIELD_PEAK_AGE = 28;

        /// <summary>Edad de pico para porteros (maduran más tarde).</summary>
        public const int GK_PEAK_AGE = 31;

        /// <summary>Edad de declive pronunciado.</summary>
        public const int DECLINE_START_AGE = 32;

        public PlayerDeveloper(string dbPath)
        {
            _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
        }

        /// <summary>
        /// Aplica el desarrollo anual de todos los jugadores activos (no retirados).
        /// Devuelve una lista con los cambios registrados.
        /// </summary>
        /// <param name="seasonLabel">Etiqueta de la temporada (ej. "2027/28"), se usa como parte de la seed.</param>
        public List<DevelopmentRecord> DevelopAll(string seasonLabel)
        {
            int seasonYear = ParseSeasonYear(seasonLabel);

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            // RNG determinista por temporada (substream aislado de otras fuentes)
            ulong masterSeed = (ulong)seasonYear * 0x9E3779B97F4A7C15UL + 0xDEADBEEF;
            var rng = new Xoshiro256StarStar(masterSeed);

            var records = new List<DevelopmentRecord>();

            // 1. Envejecer a TODAS las personas (sumar 1 año a birth_date)
            AgeAllPersons(connection);

            // 2. Obtener jugadores no retirados
            var players = LoadActivePlayers(connection);

            // 3. Desarrollar cada jugador
            foreach (var player in players)
            {
                int age = ComputeAge(player.BirthDate, seasonYear);
                int previousCA = player.CurrentAbility;
                int newCA = ComputeNewCA(rng, player, age);

                if (newCA != previousCA)
                {
                    UpdatePlayerCA(connection, player.PersonId, newCA);
                }

                records.Add(new DevelopmentRecord
                {
                    PersonId = player.PersonId,
                    SeasonYear = seasonYear,
                    AgeAtSnapshot = age,
                    PreviousCA = previousCA,
                    NewCA = newCA
                });
            }

            // 4. Registrar snapshots en development_snapshots
            PersistSnapshots(connection, records);

            return records;
        }

        // ===== Helpers =====

        private static int ParseSeasonYear(string seasonLabel)
        {
            // "2027/28" → 2027
            var parts = seasonLabel.Split('/');
            if (parts.Length >= 1 && int.TryParse(parts[0], out int year))
            {
                return year;
            }
            throw new ArgumentException($"Formato de temporada no reconocido: '{seasonLabel}'");
        }

        private void AgeAllPersons(SqliteConnection connection)
        {
            // SQLite no tiene ADD_MONTHS, así que parseamos y reconstruimos.
            // Manejo del 29 feb (→ 28 feb si el año+1 no es bisiesto).
            using var cmd = new SqliteCommand(@"
                UPDATE persons
                SET birth_date = CASE
                    WHEN SUBSTR(birth_date, 6, 5) = '02-29'
                         AND (CAST(SUBSTR(birth_date, 1, 4) AS INTEGER) + 1) % 4 != 0
                    THEN (CAST(SUBSTR(birth_date, 1, 4) AS INTEGER) + 1) || '-02-28'
                    ELSE (CAST(SUBSTR(birth_date, 1, 4) AS INTEGER) + 1) || SUBSTR(birth_date, 5)
                END", connection);
            cmd.ExecuteNonQuery();
        }

        private sealed class PlayerSnapshot
        {
            public long PersonId;
            public string BirthDate = string.Empty;
            public int CurrentAbility;
            public int PotentialAbility;
            public bool IsGoalkeeper;
            public long? ClubId;
            public int TrainingFacilities;
            public int PhysioRating;
        }

        private List<PlayerSnapshot> LoadActivePlayers(SqliteConnection connection)
        {
            var players = new List<PlayerSnapshot>();
            using var cmd = new SqliteCommand(@"
                SELECT p.person_id, pe.birth_date, p.current_ability, p.potential_ability, p.position_main,
                       c.club_id, cl.training_facilities, cl.physio_rating
                FROM players p
                JOIN persons pe ON pe.id = p.person_id
                LEFT JOIN contracts c ON c.person_id = p.person_id AND c.status = 'vigente' AND c.scope = 'primer_equipo'
                LEFT JOIN clubs cl ON cl.id = c.club_id
                WHERE p.retired = 0", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var snap = new PlayerSnapshot
                {
                    PersonId = reader.GetInt64(0),
                    BirthDate = reader.GetString(1),
                    CurrentAbility = reader.GetInt32(2),
                    PotentialAbility = reader.GetInt32(3),
                    IsGoalkeeper = reader.GetString(4) == "POR"
                };
                if (!reader.IsDBNull(5))
                {
                    snap.ClubId = reader.GetInt64(5);
                    snap.TrainingFacilities = reader.IsDBNull(6) ? 10 : reader.GetInt32(6);
                    snap.PhysioRating = reader.IsDBNull(7) ? 10 : reader.GetInt32(7);
                }
                else
                {
                    snap.TrainingFacilities = 10;
                    snap.PhysioRating = 10;
                }
                players.Add(snap);
            }
            return players;
        }

        private static int ComputeAge(string birthDate, int referenceYear)
        {
            // Parsea "YYYY-MM-DD" y calcula edad al inicio de la temporada.
            int year = int.Parse(birthDate.Substring(0, 4));
            return referenceYear - year;
        }

        private int ComputeNewCA(IRng rng, PlayerSnapshot player, int age)
        {
            int ca = player.CurrentAbility;
            int pa = player.PotentialAbility;
            int peakAge = player.IsGoalkeeper ? GK_PEAK_AGE : FIELD_PEAK_AGE;
            int delta = 0;

            if (age < peakAge)
            {
                // Fase de crecimiento
                int remainingPotential = Math.Max(0, pa - ca);
                double baseGrowth;

                if (age < 21)
                {
                    baseGrowth = 2.5; // Crecimiento rápido
                }
                else if (age < 25)
                {
                    baseGrowth = 1.5; // Crecimiento moderado
                }
                else
                {
                    baseGrowth = 0.5; // Meseta
                }

                // Factor de potencial restante (cuanto más lejos del pico, más rápido)
                double potentialFactor = Math.Min(1.5, remainingPotential / 30.0);
                double growth = baseGrowth * (0.5 + potentialFactor);

                // Bonus por facilidades del club (10 es baseline)
                double facilitiesBonus = ((player.TrainingFacilities - 10) + (player.PhysioRating - 10)) / 40.0;
                growth += facilitiesBonus;

                // Estocástico: ±1 alrededor del valor esperado
                int growthInt = (int)Math.Round(growth);
                int roll = rng.NextInt(0, 3); // 0, 1 o 2
                delta = growthInt - 1 + roll; // rango: [growth-1, growth+1]

                if (delta < 0) delta = 0;
            }
            else if (age < DECLINE_START_AGE)
            {
                // Fase de estabilidad (28-31 campo, 31-34 porteros)
                // 70% estable, 20% +1, 10% -1
                int roll = rng.NextInt(0, 10);
                if (roll < 7) delta = 0;
                else if (roll < 9) delta = 1;
                else delta = -1;
            }
            else
            {
                // Fase de declive
                int yearsOverDecline = age - DECLINE_START_AGE;
                double baseDecline = 1.5 + (yearsOverDecline * 0.5);

                // Estocástico: ±1
                int declineInt = (int)Math.Round(baseDecline);
                int roll = rng.NextInt(0, 3);
                delta = -(declineInt - 1 + roll);

                if (delta > 0) delta = 0;
            }

            // Aplicar delta
            int newCA = ca + delta;

            // Acotar a [1, 200] y nunca superar PA
            if (newCA < 1) newCA = 1;
            if (newCA > pa) newCA = pa;
            if (newCA > 200) newCA = 200;

            return newCA;
        }

        private void UpdatePlayerCA(SqliteConnection connection, long personId, int newCA)
        {
            using var cmd = new SqliteCommand(
                "UPDATE players SET current_ability = @ca WHERE person_id = @pid", connection);
            cmd.Parameters.AddWithValue("@ca", newCA);
            cmd.Parameters.AddWithValue("@pid", personId);
            cmd.ExecuteNonQuery();
        }

        private void PersistSnapshots(SqliteConnection connection, List<DevelopmentRecord> records)
        {
            // La tabla development_snapshots tiene esta estructura (ver 000_init.sql):
            //   person_id INTEGER NOT NULL
            //   month TEXT NOT NULL
            //   ca INTEGER NOT NULL
            //   attributes_json TEXT NOT NULL
            //   PRIMARY KEY (person_id, month)
            //
            // Usamos el año de la temporada como "month" (ej: "2027") para tener un snapshot anual.
            // attributes_json se deja como "{}" (en M3 lo rellenaremos con los atributos medios reales).
            using var transaction = connection.BeginTransaction();
            try
            {
                foreach (var r in records)
                {
                    using var cmd = new SqliteCommand(@"
                        INSERT OR REPLACE INTO development_snapshots
                            (person_id, month, ca, attributes_json)
                        VALUES (@pid, @month, @ca, @attrs)", connection, transaction);
                    cmd.Parameters.AddWithValue("@pid", r.PersonId);
                    cmd.Parameters.AddWithValue("@month", r.SeasonYear.ToString());
                    cmd.Parameters.AddWithValue("@ca", r.NewCA);
                    cmd.Parameters.AddWithValue("@attrs", "{}");
                    cmd.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}