using System;
using System.IO;
using Microsoft.Data.Sqlite;
using UniFutsal.Data;
using UniFutsal.Core.Domain;
using UniFutsal.Core.Domain.Clubs;
using UniFutsal.Core.Domain.Competitions;
using UniFutsal.Core.Domain.Geography;
using UniFutsal.Core.Domain.People;
using Xunit;

namespace UniFutsal.Tests.Domain
{
    public class WorldLoaderIntegrationTests : IDisposable
    {
        private readonly string _dbPath;

        public WorldLoaderIntegrationTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"ufm_test_{Guid.NewGuid():N}.db");
            SetupTestDatabase();
        }

        public void Dispose()
        {
            if (File.Exists(_dbPath))
            {
                try { File.Delete(_dbPath); }
                catch { /* ignorar */ }
            }
        }

        private void SetupTestDatabase()
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            using var cmd = new SqliteCommand(@"
                CREATE TABLE meta (key TEXT PRIMARY KEY, value TEXT NOT NULL);
                INSERT INTO meta VALUES ('world_seed', 'test_seed');
                INSERT INTO meta VALUES ('world_date', '2026-07-01');
                INSERT INTO meta VALUES ('schema_version', '1');

                CREATE TABLE confederations (id INTEGER PRIMARY KEY, code TEXT NOT NULL UNIQUE, name TEXT NOT NULL);
                INSERT INTO confederations VALUES (1, 'UEFA', 'UEFA');

                CREATE TABLE countries (id INTEGER PRIMARY KEY, uid TEXT NOT NULL UNIQUE, name TEXT NOT NULL, code3 TEXT NOT NULL UNIQUE, confederation_id INTEGER NOT NULL, futsal_reputation REAL NOT NULL DEFAULT 50);
                INSERT INTO countries VALUES (1, 'country-esp', 'Spain', 'ESP', 1, 85.0);
                INSERT INTO countries VALUES (2, 'country-bra', 'Brazil', 'BRA', 1, 95.0);

                CREATE TABLE venues (id INTEGER PRIMARY KEY, uid TEXT NOT NULL UNIQUE, name TEXT NOT NULL, city TEXT, country_id INTEGER, capacity INTEGER NOT NULL DEFAULT 1500, surface TEXT NOT NULL DEFAULT 'parquet');
                INSERT INTO venues VALUES (1, 'venue-madrid', 'Madrid Arena', 'Madrid', 1, 5000, 'parquet');

                CREATE TABLE persons (id INTEGER PRIMARY KEY, uid TEXT NOT NULL UNIQUE, first_name TEXT NOT NULL, last_name TEXT NOT NULL, common_name TEXT, gender TEXT NOT NULL DEFAULT 'M', birth_date TEXT NOT NULL, birth_city TEXT, birth_country_id INTEGER, nationality_id INTEGER NOT NULL, second_nationality_id INTEGER, height_cm INTEGER, weight_kg INTEGER, personality_key TEXT, source TEXT NOT NULL DEFAULT 'seed');
                INSERT INTO persons VALUES (1, 'person-001', 'Carlos', 'Ortega', 'Carlitos', 'M', '1995-03-15', NULL, NULL, 1, NULL, 178, 75, NULL, 'import');
                INSERT INTO persons VALUES (2, 'person-002', 'Miguel', 'Santos', NULL, 'M', '1998-07-22', NULL, NULL, 1, NULL, 182, 80, NULL, 'import');

                CREATE TABLE players (person_id INTEGER PRIMARY KEY, position_main TEXT NOT NULL, position_secondary TEXT, preferred_foot TEXT NOT NULL DEFAULT 'D', weak_foot INTEGER NOT NULL DEFAULT 3, current_ability INTEGER NOT NULL, potential_ability INTEGER NOT NULL,
                    t_control INTEGER NOT NULL DEFAULT 10, t_conduccion INTEGER NOT NULL DEFAULT 10, t_pase INTEGER NOT NULL DEFAULT 10, t_pase_un_toque INTEGER NOT NULL DEFAULT 10, t_finalizacion INTEGER NOT NULL DEFAULT 10, t_tiro_lejano INTEGER NOT NULL DEFAULT 10, t_regate INTEGER NOT NULL DEFAULT 10, t_poste INTEGER NOT NULL DEFAULT 10, t_entrada INTEGER NOT NULL DEFAULT 10, t_intercepcion INTEGER NOT NULL DEFAULT 10, t_bloqueo INTEGER NOT NULL DEFAULT 10,
                    g_paradas INTEGER NOT NULL DEFAULT 1, g_reflejos INTEGER NOT NULL DEFAULT 1, g_uno_con_uno INTEGER NOT NULL DEFAULT 1, g_juego_pies INTEGER NOT NULL DEFAULT 1, g_distribucion INTEGER NOT NULL DEFAULT 1, g_posicionamiento INTEGER NOT NULL DEFAULT 1, g_salidas INTEGER NOT NULL DEFAULT 1, g_jugador INTEGER NOT NULL DEFAULT 1,
                    m_vision INTEGER NOT NULL DEFAULT 10, m_decision INTEGER NOT NULL DEFAULT 10, m_anticipacion INTEGER NOT NULL DEFAULT 10, m_concentracion INTEGER NOT NULL DEFAULT 10, m_posicionamiento INTEGER NOT NULL DEFAULT 10, m_agresividad INTEGER NOT NULL DEFAULT 10, m_serenidad INTEGER NOT NULL DEFAULT 10, m_liderazgo INTEGER NOT NULL DEFAULT 10, m_equipo INTEGER NOT NULL DEFAULT 10, m_trabajo INTEGER NOT NULL DEFAULT 10, m_arrojo INTEGER NOT NULL DEFAULT 10,
                    p_aceleracion INTEGER NOT NULL DEFAULT 10, p_velocidad INTEGER NOT NULL DEFAULT 10, p_agilidad INTEGER NOT NULL DEFAULT 10, p_equilibrio INTEGER NOT NULL DEFAULT 10, p_coordinacion INTEGER NOT NULL DEFAULT 10, p_resistencia INTEGER NOT NULL DEFAULT 10, p_fuerza INTEGER NOT NULL DEFAULT 10, p_salto INTEGER NOT NULL DEFAULT 10,
                    h_consistencia INTEGER NOT NULL DEFAULT 10, h_lesiones INTEGER NOT NULL DEFAULT 10, h_juego_duro INTEGER NOT NULL DEFAULT 10, h_temperamento INTEGER NOT NULL DEFAULT 10, retired INTEGER NOT NULL DEFAULT 0);

                -- INSERT con nombres de columnas explícitos para evitar errores de conteo
                INSERT INTO players (person_id, position_main, preferred_foot, current_ability, potential_ability, retired,
                    t_control, t_conduccion, t_pase, t_pase_un_toque, t_finalizacion, t_tiro_lejano, t_regate, t_poste, t_entrada, t_intercepcion, t_bloqueo,
                    g_paradas, g_reflejos, g_uno_con_uno, g_juego_pies, g_distribucion, g_posicionamiento, g_salidas, g_jugador,
                    m_vision, m_decision, m_anticipacion, m_concentracion, m_posicionamiento, m_agresividad, m_serenidad, m_liderazgo, m_equipo, m_trabajo, m_arrojo,
                    p_aceleracion, p_velocidad, p_agilidad, p_equilibrio, p_coordinacion, p_resistencia, p_fuerza, p_salto,
                    h_consistencia, h_lesiones, h_juego_duro, h_temperamento)
                VALUES (1, 'PIV', 'D', 130, 150, 0,
                    14, 13, 12, 11, 16, 15, 13, 14, 8, 9, 8,
                    1, 1, 1, 1, 1, 1, 1, 1,
                    12, 13, 11, 12, 10, 11, 12, 10, 12, 14, 13,
                    12, 13, 11, 12, 13, 12, 11, 12,
                    12, 11, 12, 10);

                INSERT INTO players (person_id, position_main, preferred_foot, current_ability, potential_ability, retired,
                    t_control, t_conduccion, t_pase, t_pase_un_toque, t_finalizacion, t_tiro_lejano, t_regate, t_poste, t_entrada, t_intercepcion, t_bloqueo,
                    g_paradas, g_reflejos, g_uno_con_uno, g_juego_pies, g_distribucion, g_posicionamiento, g_salidas, g_jugador,
                    m_vision, m_decision, m_anticipacion, m_concentracion, m_posicionamiento, m_agresividad, m_serenidad, m_liderazgo, m_equipo, m_trabajo, m_arrojo,
                    p_aceleracion, p_velocidad, p_agilidad, p_equilibrio, p_coordinacion, p_resistencia, p_fuerza, p_salto,
                    h_consistencia, h_lesiones, h_juego_duro, h_temperamento)
                VALUES (2, 'POR', 'D', 120, 140, 0,
                    8, 7, 9, 8, 6, 5, 6, 5, 7, 8, 7,
                    16, 15, 14, 12, 13, 15, 14, 10,
                    12, 13, 14, 13, 12, 11, 10, 12, 11, 13, 10,
                    12, 11, 12, 10, 11, 12, 13, 11,
                    12, 12, 10, 11);

                CREATE TABLE clubs (id INTEGER PRIMARY KEY, uid TEXT NOT NULL UNIQUE, name TEXT NOT NULL, short_name TEXT, nickname TEXT, country_id INTEGER NOT NULL, region_id INTEGER, city TEXT, founded_year INTEGER, primary_color TEXT NOT NULL DEFAULT '#E63946', secondary_color TEXT NOT NULL DEFAULT '#FFFFFF', kit_pattern TEXT NOT NULL DEFAULT 'solid', reputation REAL NOT NULL DEFAULT 40, venue_id INTEGER, training_facilities INTEGER NOT NULL DEFAULT 10, youth_facilities INTEGER NOT NULL DEFAULT 10, recruitment INTEGER NOT NULL DEFAULT 10, physio_rating INTEGER NOT NULL DEFAULT 10, bank_balance INTEGER NOT NULL DEFAULT 0, debt INTEGER NOT NULL DEFAULT 0, transfer_budget INTEGER NOT NULL DEFAULT 0, wage_budget_monthly INTEGER NOT NULL DEFAULT 0, is_active INTEGER NOT NULL DEFAULT 1);
                INSERT INTO clubs VALUES (1, 'club-madrid-fs', 'Madrid FS', 'MAD', 'Los Rojos', 1, NULL, 'Madrid', 1975, '#E63946', '#FFFFFF', 'solid', 85.0, 1, 16, 15, 14, 15, 2500000, 0, 800000, 120000, 1);

                CREATE TABLE contracts (id INTEGER PRIMARY KEY, person_id INTEGER NOT NULL, club_id INTEGER NOT NULL, scope TEXT NOT NULL DEFAULT 'primer_equipo', signed_on TEXT NOT NULL, effective_from TEXT NOT NULL, effective_until TEXT NOT NULL, wage_monthly INTEGER NOT NULL, release_clause INTEGER, squad_number INTEGER, bonus_json TEXT NOT NULL DEFAULT '{}', agent_id INTEGER, agent_fee INTEGER, negotiated_by TEXT, status TEXT NOT NULL DEFAULT 'vigente');
                INSERT INTO contracts VALUES (1, 1, 1, 'primer_equipo', '2026-07-01', '2026-07-01', '2028-06-30', 5000, 300000, 9, '{}', NULL, NULL, NULL, 'vigente');
                INSERT INTO contracts VALUES (2, 2, 1, 'primer_equipo', '2026-07-01', '2026-07-01', '2028-06-30', 4000, 200000, 1, '{}', NULL, NULL, NULL, 'vigente');

                CREATE TABLE seasons (id INTEGER PRIMARY KEY, label TEXT NOT NULL UNIQUE, start_date TEXT NOT NULL, end_date TEXT NOT NULL);
                INSERT INTO seasons VALUES (1, '2026/27', '2026-08-15', '2027-06-15');

                CREATE TABLE competitions (id INTEGER PRIMARY KEY, uid TEXT NOT NULL UNIQUE, name TEXT NOT NULL, short_name TEXT, scope TEXT NOT NULL, type TEXT NOT NULL, country_id INTEGER, confederation_id INTEGER, level INTEGER, prestige REAL NOT NULL DEFAULT 30, rules_json TEXT NOT NULL DEFAULT '{}', active INTEGER NOT NULL DEFAULT 1, source_pack_id INTEGER);
                INSERT INTO competitions VALUES (1, 'comp-lnfs-primera', 'LNFS Primera División', 'LNFS 1ª', 'club', 'liga', 1, NULL, 1, 85.0, '{}', 1, NULL);

                CREATE TABLE competition_entries (id INTEGER PRIMARY KEY, season_id INTEGER NOT NULL, competition_id INTEGER NOT NULL, club_id INTEGER, national_team_id INTEGER, group_id INTEGER, seed INTEGER, qualified_via_link_id INTEGER, status TEXT NOT NULL DEFAULT 'activo');
                INSERT INTO competition_entries VALUES (1, 1, 1, 1, NULL, NULL, NULL, NULL, 'activo');
            ", connection);
            cmd.ExecuteNonQuery();
        }

        [Fact]
        public void WorldLoader_Load_LoadsCountries()
        {
            var loader = new WorldLoader(_dbPath);
            var world = loader.Load();

            Assert.Equal(2, world.Countries.Count);
            Assert.Equal("Spain", world.Countries[0].Name);
            Assert.Equal("ESP", world.Countries[0].Code3);
            Assert.Equal(85.0, world.Countries[0].FutsalReputation);
        }

        [Fact]
        public void WorldLoader_Load_ResolvesCountryConfederation()
        {
            var loader = new WorldLoader(_dbPath);
            var world = loader.Load();

            var spain = world.Countries[0];
            Assert.NotNull(spain.Confederation);
            Assert.Equal("UEFA", spain.Confederation.Code);
        }

        [Fact]
        public void WorldLoader_Load_LoadsPlayersWithAttributes()
        {
            var loader = new WorldLoader(_dbPath);
            var world = loader.Load();

            Assert.Equal(2, world.Players.Count);

            var pivot = world.Players[0];
            Assert.Equal(Position.Pivot, pivot.PositionMain);
            Assert.Equal(130, pivot.CurrentAbility);
            Assert.Equal(150, pivot.PotentialAbility);
            Assert.Equal(16, pivot.T_Finishing);

            var goalkeeper = world.Players[1];
            Assert.Equal(Position.Goalkeeper, goalkeeper.PositionMain);
            Assert.Equal(16, goalkeeper.G_ShotStopping);
        }

        [Fact]
        public void WorldLoader_Load_ResolvesPlayerPerson()
        {
            var loader = new WorldLoader(_dbPath);
            var world = loader.Load();

            var pivot = world.Players[0];
            Assert.NotNull(pivot.Person);
            Assert.Equal("Carlos", pivot.Person.FirstName);
            Assert.Equal("Carlitos", pivot.Person.CommonName);
        }

        [Fact]
        public void WorldLoader_Load_LoadsClubWithVenue()
        {
            var loader = new WorldLoader(_dbPath);
            var world = loader.Load();

            Assert.Single(world.Clubs);
            var club = world.Clubs[0];
            Assert.Equal("Madrid FS", club.Name);
            Assert.NotNull(club.Venue);
            Assert.Equal("Madrid Arena", club.Venue.Name);
            Assert.NotNull(club.Country);
            Assert.Equal("Spain", club.Country.Name);
        }

        [Fact]
        public void WorldLoader_Load_LoadsContractsWithReferences()
        {
            var loader = new WorldLoader(_dbPath);
            var world = loader.Load();

            Assert.Equal(2, world.Contracts.Count);
            var contract = world.Contracts[0];
            Assert.NotNull(contract.Person);
            Assert.NotNull(contract.Club);
            Assert.Equal("Carlos", contract.Person.FirstName);
            Assert.Equal("Madrid FS", contract.Club.Name);
            Assert.Equal(5000, contract.WageMonthly);
            Assert.Equal(ContractStatus.Active, contract.Status);
        }

        [Fact]
        public void WorldLoader_GetPlayersByClub_ReturnsActiveFirstTeamPlayers()
        {
            var loader = new WorldLoader(_dbPath);
            var world = loader.Load();

            var players = world.GetPlayersByClub(1);
            Assert.Equal(2, players.Count);
        }

        [Fact]
        public void WorldLoader_Load_LoadsCompetitionEntries()
        {
            var loader = new WorldLoader(_dbPath);
            var world = loader.Load();

            Assert.Single(world.CompetitionEntries);
            var entry = world.CompetitionEntries[0];
            Assert.NotNull(entry.Competition);
            Assert.Equal("LNFS Primera División", entry.Competition.Name);
            Assert.NotNull(entry.Club);
            Assert.Equal("Madrid FS", entry.Club.Name);
            Assert.NotNull(entry.Season);
            Assert.Equal("2026/27", entry.Season.Label);
        }

        [Fact]
        public void WorldLoader_Load_LoadsWorldDate()
        {
            var loader = new WorldLoader(_dbPath);
            var world = loader.Load();

            Assert.Equal("test_seed", world.WorldSeed);
            Assert.Equal("2026-07-01", world.WorldDate);
        }
    }
}