using System.IO;
using Microsoft.Data.Sqlite;
using UniFutsal.Data;
using Xunit;

namespace UniFutsal.Tests.Engine
{
    public class SeasonSimulatorTests
    {
        /// <summary>
        /// Borra un archivo ignorando errores (SQLite puede mantener handles abiertos brevemente).
        /// </summary>
        private static void SafeDelete(string path)
        {
            if (!File.Exists(path)) return;
            try
            {
                // Forzar GC para liberar handles de SQLite mantenidos por referencias finales
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
        /// Fixture reutilizable: BD mínima con 3 clubes, 1 competición, 1 temporada y calendario.
        /// </summary>
        private static string CreateMinimalSeasonDb()
        {
            string dbPath = Path.Combine(Path.GetTempPath(), $"ufm_season_{System.Guid.NewGuid():N}.db");

            // Inicializar BD vacía con el schema completo
            DatabaseInitializer.Initialize(dbPath, "data/migrations");

            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            using var cmd = new SqliteCommand(@"
                INSERT OR REPLACE INTO meta (key, value) VALUES ('world_seed', 'test_season');
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

            // Jugadores con CA distinto por club
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

            // Generar calendario usando CalendarGenerator
            var generator = new CalendarGenerator(dbPath);
            generator.Generate("comp-test", "2026/27");

            return dbPath;
        }

        [Fact]
        public void SimulateSeason_SimulatesAllMatches()
        {
            string dbPath = CreateMinimalSeasonDb();
            try
            {
                var simulator = new SeasonSimulator(dbPath);
                var report = simulator.SimulateSeason("comp-test", "2026/27", persist: true);

                Assert.Equal(6, report.TotalMatches);
                Assert.Equal(3, report.FinalStandings.Count);
                Assert.Equal("comp-test", report.CompetitionUid);
                Assert.Equal("2026/27", report.SeasonLabel);
            }
            finally
            {
                SafeDelete(dbPath);
            }
        }

        [Fact]
        public void SimulateSeason_IsDeterministic()
        {
            string dbPath1 = CreateMinimalSeasonDb();
            string dbPath2 = CreateMinimalSeasonDb();
            try
            {
                var report1 = new SeasonSimulator(dbPath1).SimulateSeason("comp-test", "2026/27", persist: false);
                var report2 = new SeasonSimulator(dbPath2).SimulateSeason("comp-test", "2026/27", persist: false);

                Assert.Equal(report1.Champion?.ClubUid, report2.Champion?.ClubUid);
                for (int i = 0; i < report1.FinalStandings.Count; i++)
                {
                    Assert.Equal(report1.FinalStandings[i].ClubUid, report2.FinalStandings[i].ClubUid);
                    Assert.Equal(report1.FinalStandings[i].Points, report2.FinalStandings[i].Points);
                }
                Assert.Equal(report1.TotalGoals, report2.TotalGoals);
            }
            finally
            {
                SafeDelete(dbPath1);
                SafeDelete(dbPath2);
            }
        }

        [Fact]
        public void SimulateSeason_StrongerClubWinsMoreOften()
        {
            int aWins = 0;
            int cWins = 0;
            for (int run = 0; run < 5; run++)
            {
                string dbPath = CreateMinimalSeasonDb();
                try
                {
                    var report = new SeasonSimulator(dbPath).SimulateSeason("comp-test", "2026/27", persist: false);
                    if (report.Champion?.ClubUid == "club-a") aWins++;
                    else if (report.Champion?.ClubUid == "club-c") cWins++;
                }
                finally
                {
                    SafeDelete(dbPath);
                }
            }
            Assert.True(aWins >= cWins,
                $"El club fuerte (A) debería ganar más ligas que el débil (C): A={aWins}, C={cWins}");
        }

        [Fact]
        public void SimulateSeason_PersistsResultsToDatabase()
        {
            string dbPath = CreateMinimalSeasonDb();
            try
            {
                var simulator = new SeasonSimulator(dbPath);
                simulator.SimulateSeason("comp-test", "2026/27", persist: true);

                using var connection = new SqliteConnection($"Data Source={dbPath}");
                connection.Open();
                using var cmd = new SqliteCommand(
                    "SELECT COUNT(*) FROM matches WHERE status = 'jugado'", connection);
                var result = cmd.ExecuteScalar();
                long played = result != null && result != System.DBNull.Value
                    ? System.Convert.ToInt64(result)
                    : 0;
                Assert.Equal(6, played);
            }
            finally
            {
                SafeDelete(dbPath);
            }
        }

        [Fact]
        public void SimulateSeason_ThrowsOnMissingCompetition()
        {
            string dbPath = CreateMinimalSeasonDb();
            try
            {
                var simulator = new SeasonSimulator(dbPath);
                Assert.Throws<System.ArgumentException>(() =>
                    simulator.SimulateSeason("no-existe", "2026/27"));
            }
            finally
            {
                SafeDelete(dbPath);
            }
        }

        [Fact]
        public void SimulateSeason_Idempotent_SecondRunSkipsPlayedMatches()
        {
            string dbPath = CreateMinimalSeasonDb();
            try
            {
                var simulator = new SeasonSimulator(dbPath);
                var r1 = simulator.SimulateSeason("comp-test", "2026/27", persist: true);
                var r2 = simulator.SimulateSeason("comp-test", "2026/27", persist: true);

                Assert.Equal(r1.TotalMatches, r2.TotalMatches);
                Assert.Equal(r1.TotalGoals, r2.TotalGoals);
                Assert.Equal(r1.Champion?.ClubUid, r2.Champion?.ClubUid);
            }
            finally
            {
                SafeDelete(dbPath);
            }
        }
    }
}