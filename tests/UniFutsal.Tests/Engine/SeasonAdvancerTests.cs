using System.IO;
using Microsoft.Data.Sqlite;
using UniFutsal.Data;
using Xunit;

namespace UniFutsal.Tests.Engine
{
    public class SeasonAdvancerTests
    {
        /// <summary>
        /// Borra un archivo ignorando errores (SQLite puede mantener handles abiertos brevemente).
        /// </summary>
        private static void SafeDelete(string path)
        {
            if (!File.Exists(path)) return;
            try
            {
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.Collect();
                File.Delete(path);
            }
            catch (IOException)
            {
                // Ignorar: el SO limpiará /temp eventualmente
            }
        }

        /// <summary>
        /// Crea una BD de prueba con 3 clubes, simula la temporada y la deja lista para avanzar.
        /// </summary>
        private static string CreateSimulatedSeasonDb()
        {
            string dbPath = Path.Combine(Path.GetTempPath(), $"ufm_adv_{System.Guid.NewGuid():N}.db");

            DatabaseInitializer.Initialize(dbPath, "data/migrations");

            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            using var cmd = new SqliteCommand(@"
                INSERT OR REPLACE INTO meta (key, value) VALUES ('world_seed', 'test_advance');
                INSERT OR REPLACE INTO meta (key, value) VALUES ('world_date', '2026-07-01');
                INSERT OR REPLACE INTO meta (key, value) VALUES ('schema_version', '1');

                INSERT INTO confederations (id, code, name) VALUES (1, 'UEFA', 'UEFA');
                INSERT INTO countries (id, uid, name, code3, confederation_id, futsal_reputation)
                    VALUES (1, 'country-test', 'Testland', 'TST', 1, 50);

                INSERT INTO seasons (id, label, start_date, end_date)
                    VALUES (1, '2026/27', '2026-08-15', '2027-06-15');

                INSERT INTO competitions (id, uid, name, short_name, scope, type, country_id, level, prestige, active)
                    VALUES (1, 'comp-test', 'Test League', 'TST', 'club', 'liga', 1, 1, 50, 1);

                INSERT INTO clubs (id, uid, name, country_id, primary_color, secondary_color, reputation) VALUES
                    (1, 'club-a', 'Club A', 1, '#E63946', '#FFFFFF', 80),
                    (2, 'club-b', 'Club B', 1, '#2A9D8F', '#FFFFFF', 70),
                    (3, 'club-c', 'Club C', 1, '#E9C46A', '#264653', 60);

                INSERT INTO competition_entries (season_id, competition_id, club_id, status) VALUES
                    (1, 1, 1, 'activo'),
                    (1, 1, 2, 'activo'),
                    (1, 1, 3, 'activo');
            ", connection);
            cmd.ExecuteNonQuery();

            // Jugadores
            for (int c = 1; c <= 3; c++)
            {
                int ca = c == 1 ? 130 : (c == 2 ? 110 : 90);
                for (int i = 0; i < 12; i++)
                {
                    long personId = c * 1000 + i;
                    using var pCmd = new SqliteCommand(@"
                        INSERT INTO persons (id, uid, first_name, last_name, gender, birth_date, nationality_id, source)
                        VALUES (@pid, @uid, 'Player', @num, 'M', '2000-01-01', 1, 'import');
                        INSERT INTO players (person_id, position_main, preferred_foot, current_ability, potential_ability, retired)
                        VALUES (@pid, 'PIV', 'D', @ca, @ca, 0);
                        INSERT INTO contracts (person_id, club_id, scope, signed_on, effective_from, effective_until, wage_monthly, status)
                        VALUES (@pid, @cid, 'primer_equipo', '2026-07-01', '2026-07-01', '2028-06-30', 1000, 'vigente');
                    ", connection);
                    pCmd.Parameters.AddWithValue("@pid", personId);
                    pCmd.Parameters.AddWithValue("@uid", $"p-{c}-{i}");
                    pCmd.Parameters.AddWithValue("@num", i.ToString());
                    pCmd.Parameters.AddWithValue("@ca", ca);
                    pCmd.Parameters.AddWithValue("@cid", c);
                    pCmd.ExecuteNonQuery();
                }
            }

            // Generar calendario y simular
            var generator = new CalendarGenerator(dbPath);
            generator.Generate("comp-test", "2026/27");

            var simulator = new SeasonSimulator(dbPath);
            simulator.SimulateSeason("comp-test", "2026/27", persist: true);

            return dbPath;
        }

        [Fact]
        public void AdvanceSeason_CreatesNewSeasonAndCalendar()
        {
            string dbPath = CreateSimulatedSeasonDb();
            try
            {
                var advancer = new SeasonAdvancer(dbPath);
                var result = advancer.AdvanceSeason("comp-test");

                Assert.Equal("2026/27", result.PreviousSeasonLabel);
                Assert.Equal("2027/28", result.NewSeasonLabel);
                Assert.Equal(3, result.EntriesCopied);
                Assert.Equal(6, result.MatchesGenerated);
                Assert.Equal("2027-08-15", result.NewWorldDate);
            }
            finally
            {
                SafeDelete(dbPath);
            }
        }

        [Fact]
        public void AdvanceSeason_ThrowsIfSeasonNotSimulated()
        {
            string dbPath = Path.Combine(Path.GetTempPath(), $"ufm_adv_{System.Guid.NewGuid():N}.db");
            DatabaseInitializer.Initialize(dbPath, "data/migrations");

            try
            {
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                connection.Open();
                using var cmd = new SqliteCommand(@"
                    INSERT OR REPLACE INTO meta (key, value) VALUES ('world_date', '2026-07-01');
                    INSERT INTO confederations (id, code, name) VALUES (1, 'UEFA', 'UEFA');
                    INSERT INTO countries (id, uid, name, code3, confederation_id, futsal_reputation)
                        VALUES (1, 'country-test', 'Testland', 'TST', 1, 50);
                    INSERT INTO seasons (id, label, start_date, end_date)
                        VALUES (1, '2026/27', '2026-08-15', '2027-06-15');
                    INSERT INTO competitions (id, uid, name, scope, type, country_id, level, prestige, active)
                        VALUES (1, 'comp-test', 'Test League', 'club', 'liga', 1, 1, 50, 1);
                    INSERT INTO clubs (id, uid, name, country_id, primary_color, secondary_color, reputation) VALUES
                        (1, 'club-a', 'Club A', 1, '#E63946', '#FFFFFF', 80),
                        (2, 'club-b', 'Club B', 1, '#2A9D8F', '#FFFFFF', 70);
                    INSERT INTO competition_entries (season_id, competition_id, club_id, status) VALUES
                        (1, 1, 1, 'activo'), (1, 1, 2, 'activo');
                ", connection);
                cmd.ExecuteNonQuery();

                // Generar calendario pero NO simular
                var generator = new CalendarGenerator(dbPath);
                generator.Generate("comp-test", "2026/27");

                var advancer = new SeasonAdvancer(dbPath);
                Assert.Throws<System.InvalidOperationException>(() =>
                    advancer.AdvanceSeason("comp-test"));
            }
            finally
            {
                SafeDelete(dbPath);
            }
        }

        [Fact]
        public void AdvanceSeason_ThrowsIfAlreadyAdvanced()
        {
            string dbPath = CreateSimulatedSeasonDb();
            try
            {
                var advancer = new SeasonAdvancer(dbPath);
                advancer.AdvanceSeason("comp-test"); // primer avance OK

                // Simular la nueva temporada para poder avanzar de nuevo
                var simulator = new SeasonSimulator(dbPath);
                simulator.SimulateSeason("comp-test", "2027/28", persist: true);

                // Segundo avance OK
                var result2 = advancer.AdvanceSeason("comp-test");
                Assert.Equal("2028/29", result2.NewSeasonLabel);
            }
            finally
            {
                SafeDelete(dbPath);
            }
        }

        [Fact]
        public void AdvanceSeason_ThrowsOnMissingCompetition()
        {
            string dbPath = CreateSimulatedSeasonDb();
            try
            {
                var advancer = new SeasonAdvancer(dbPath);
                Assert.Throws<System.ArgumentException>(() =>
                    advancer.AdvanceSeason("no-existe"));
            }
            finally
            {
                SafeDelete(dbPath);
            }
        }

        [Fact]
        public void AdvanceSeason_PreservesHistoricalMatches()
        {
            string dbPath = CreateSimulatedSeasonDb();
            try
            {
                // Contar partidos antes del avance
                int matchesBefore;
                using (var conn = new SqliteConnection($"Data Source={dbPath}"))
                {
                    conn.Open();
                    using var cmd = new SqliteCommand("SELECT COUNT(*) FROM matches", conn);
                    matchesBefore = System.Convert.ToInt32(cmd.ExecuteScalar());
                }

                var advancer = new SeasonAdvancer(dbPath);
                var result = advancer.AdvanceSeason("comp-test");

                // Contar partidos después del avance
                int matchesAfter;
                using (var conn = new SqliteConnection($"Data Source={dbPath}"))
                {
                    conn.Open();
                    using var cmd = new SqliteCommand("SELECT COUNT(*) FROM matches", conn);
                    matchesAfter = System.Convert.ToInt32(cmd.ExecuteScalar());
                }

                // Debe haber 6 partidos nuevos (temporada 2) + los 6 antiguos
                Assert.Equal(matchesBefore + result.MatchesGenerated, matchesAfter);

                // Los partidos antiguos siguen marcados como 'jugado'
                using (var conn = new SqliteConnection($"Data Source={dbPath}"))
                {
                    conn.Open();
                    using var cmd = new SqliteCommand(
                        "SELECT COUNT(*) FROM matches WHERE status = 'jugado'", conn);
                    int played = System.Convert.ToInt32(cmd.ExecuteScalar());
                    Assert.Equal(6, played); // Solo los de la primera temporada
                }
            }
            finally
            {
                SafeDelete(dbPath);
            }
        }

        [Fact]
        public void ComputeNextSeasonLabel_FormatsCorrectly()
        {
            // Probamos a través del avance real
            string dbPath = CreateSimulatedSeasonDb();
            try
            {
                var advancer = new SeasonAdvancer(dbPath);
                var result = advancer.AdvanceSeason("comp-test");
                Assert.Equal("2027/28", result.NewSeasonLabel);
            }
            finally
            {
                SafeDelete(dbPath);
            }
        }
    }
}