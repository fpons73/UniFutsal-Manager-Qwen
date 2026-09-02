using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using UniFutsal.Core.Domain;
using UniFutsal.Core.Domain.Clubs;
using UniFutsal.Core.Domain.Competitions;
using UniFutsal.Core.Domain.Geography;
using UniFutsal.Core.Domain.People;

namespace UniFutsal.Data
{
    /// <summary>
    /// Carga el mundo desde SQLite a objetos C# en memoria.
    /// </summary>
    public class WorldLoader
    {
        private readonly string _dbPath;

        public WorldLoader(string dbPath)
        {
            _dbPath = dbPath;
        }

        public World Load()
        {
            var world = new World();

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            // Cargar en orden para poder resolver referencias
            LoadMeta(connection, world);
            LoadConfederations(connection, world);
            LoadCountries(connection, world);
            LoadVenues(connection, world);
            LoadPersons(connection, world);
            LoadPlayers(connection, world);
            LoadClubs(connection, world);
            LoadContracts(connection, world);
            LoadSeasons(connection, world);
            LoadCompetitions(connection, world);
            LoadCompetitionEntries(connection, world);

            // Construir índices y resolver referencias
            world.IndexAll();
            world.ResolveReferences();

            return world;
        }

        private void LoadMeta(SqliteConnection connection, World world)
        {
            using var cmd = new SqliteCommand("SELECT key, value FROM meta", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string key = reader.GetString(0);
                string value = reader.GetString(1);
                switch (key)
                {
                    case "world_seed":
                        world.WorldSeed = value;
                        break;
                    case "world_date":
                        world.WorldDate = value;
                        break;
                    case "schema_version":
                        world.SchemaVersion = value;
                        break;
                }
            }
        }

        private void LoadConfederations(SqliteConnection connection, World world)
        {
            using var cmd = new SqliteCommand("SELECT id, code, name FROM confederations ORDER BY id", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                world.Confederations.Add(new Confederation
                {
                    Id = reader.GetInt64(0),
                    Code = reader.GetString(1),
                    Name = reader.GetString(2)
                });
            }
        }

        private void LoadCountries(SqliteConnection connection, World world)
        {
            using var cmd = new SqliteCommand(
                "SELECT id, uid, name, code3, confederation_id, futsal_reputation FROM countries ORDER BY id", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                world.Countries.Add(new Country
                {
                    Id = reader.GetInt64(0),
                    Uid = reader.GetString(1),
                    Name = reader.GetString(2),
                    Code3 = reader.GetString(3),
                    ConfederationId = reader.GetInt64(4),
                    FutsalReputation = reader.GetDouble(5)
                });
            }
        }

        private void LoadVenues(SqliteConnection connection, World world)
        {
            using var cmd = new SqliteCommand(
                "SELECT id, uid, name, city, country_id, capacity, surface FROM venues ORDER BY id", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var surface = reader.GetString(6).ToLowerInvariant();
                VenueSurface venueSurface = VenueSurface.Parquet;
                if (surface == "linoleum") venueSurface = VenueSurface.Linoleum;
                else if (surface == "pvc") venueSurface = VenueSurface.Pvc;
                else if (surface == "taraflex") venueSurface = VenueSurface.Taraflex;

                var venue = new Venue
                {
                    Id = reader.GetInt64(0),
                    Uid = reader.GetString(1),
                    Name = reader.GetString(2),
                    Capacity = reader.GetInt32(5),
                    Surface = venueSurface
                };

                if (!reader.IsDBNull(3))
                {
                    venue.City = reader.GetString(3);
                }
                if (!reader.IsDBNull(4))
                {
                    venue.CountryId = reader.GetInt64(4);
                }

                world.Venues.Add(venue);
            }
        }

        private void LoadPersons(SqliteConnection connection, World world)
        {
            using var cmd = new SqliteCommand(
                @"SELECT id, uid, first_name, last_name, common_name, gender, birth_date,
                  nationality_id, height_cm, weight_kg, source
                  FROM persons ORDER BY id", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var person = new Person
                {
                    Id = reader.GetInt64(0),
                    Uid = reader.GetString(1),
                    FirstName = reader.GetString(2),
                    LastName = reader.GetString(3),
                    NationalityId = reader.GetInt64(7),
                    Source = ParseSource(reader.GetString(10))
                };

                if (!reader.IsDBNull(4)) person.CommonName = reader.GetString(4);
                if (!reader.IsDBNull(5))
                {
                    person.Gender = reader.GetString(5) == "F" ? Gender.Female : Gender.Male;
                }
                if (!reader.IsDBNull(6)) person.BirthDate = DateTime.Parse(reader.GetString(6));
                if (!reader.IsDBNull(8)) person.HeightCm = reader.GetInt32(8);
                if (!reader.IsDBNull(9)) person.WeightKg = reader.GetInt32(9);

                world.Persons.Add(person);
            }
        }

        private void LoadPlayers(SqliteConnection connection, World world)
        {
            using var cmd = new SqliteCommand(
                @"SELECT person_id, position_main, preferred_foot, current_ability, potential_ability,
                  t_control, t_conduccion, t_pase, t_pase_un_toque, t_finalizacion,
                  t_tiro_lejano, t_regate, t_poste, t_entrada, t_intercepcion, t_bloqueo,
                  g_paradas, g_reflejos, g_uno_con_uno, g_juego_pies, g_distribucion,
                  g_posicionamiento, g_salidas, g_jugador,
                  m_vision, m_decision, m_anticipacion, m_concentracion, m_posicionamiento,
                  m_agresividad, m_serenidad, m_liderazgo, m_equipo, m_trabajo, m_arrojo,
                  p_aceleracion, p_velocidad, p_agilidad, p_equilibrio, p_coordinacion,
                  p_resistencia, p_fuerza, p_salto,
                  h_consistencia, h_lesiones, h_juego_duro, h_temperamento
                  FROM players ORDER BY person_id", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var player = new Player
                {
                    PersonId = reader.GetInt64(0),
                    PositionMain = ParsePosition(reader.GetString(1)),
                    PreferredFoot = ParseFoot(reader.GetString(2)),
                    CurrentAbility = reader.GetInt32(3),
                    PotentialAbility = reader.GetInt32(4),
                    T_Control = reader.GetInt32(5),
                    T_Dribbling = reader.GetInt32(6),
                    T_Passing = reader.GetInt32(7),
                    T_OneTouchPass = reader.GetInt32(8),
                    T_Finishing = reader.GetInt32(9),
                    T_LongShots = reader.GetInt32(10),
                    T_Technique = reader.GetInt32(11),
                    T_PostPlay = reader.GetInt32(12),
                    T_Tackling = reader.GetInt32(13),
                    T_Interceptions = reader.GetInt32(14),
                    T_Blocking = reader.GetInt32(15),
                    G_ShotStopping = reader.GetInt32(16),
                    G_Reflexes = reader.GetInt32(17),
                    G_OneOnOne = reader.GetInt32(18),
                    G_Kicking = reader.GetInt32(19),
                    G_Distribution = reader.GetInt32(20),
                    G_Positioning = reader.GetInt32(21),
                    G_RushingOut = reader.GetInt32(22),
                    G_Outfield = reader.GetInt32(23),
                    M_Vision = reader.GetInt32(24),
                    M_Decisions = reader.GetInt32(25),
                    M_Anticipation = reader.GetInt32(26),
                    M_Concentration = reader.GetInt32(27),
                    M_Positioning = reader.GetInt32(28),
                    M_Aggression = reader.GetInt32(29),
                    M_Composure = reader.GetInt32(30),
                    M_Leadership = reader.GetInt32(31),
                    M_Teamwork = reader.GetInt32(32),
                    M_WorkRate = reader.GetInt32(33),
                    M_Bravery = reader.GetInt32(34),
                    P_Acceleration = reader.GetInt32(35),
                    P_Pace = reader.GetInt32(36),
                    P_Agility = reader.GetInt32(37),
                    P_Balance = reader.GetInt32(38),
                    P_Coordination = reader.GetInt32(39),
                    P_Stamina = reader.GetInt32(40),
                    P_Strength = reader.GetInt32(41),
                    P_Jumping = reader.GetInt32(42),
                    H_Consistency = reader.GetInt32(43),
                    H_InjuryProneness = reader.GetInt32(44),
                    H_Dirtiness = reader.GetInt32(45),
                    H_Temperament = reader.GetInt32(46)
                };

                world.Players.Add(player);
            }
        }

        private void LoadClubs(SqliteConnection connection, World world)
        {
            using var cmd = new SqliteCommand(
                @"SELECT id, uid, name, short_name, nickname, country_id, city, founded_year,
                  primary_color, secondary_color, kit_pattern, reputation, venue_id,
                  training_facilities, youth_facilities, recruitment, physio_rating,
                  bank_balance, debt, transfer_budget, wage_budget_monthly, is_active
                  FROM clubs ORDER BY id", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var club = new Club
                {
                    Id = reader.GetInt64(0),
                    Uid = reader.GetString(1),
                    Name = reader.GetString(2),
                    CountryId = reader.GetInt64(5),
                    PrimaryColor = reader.GetString(8),
                    SecondaryColor = reader.GetString(9),
                    KitPattern = ParseKitPattern(reader.GetString(10)),
                    Reputation = reader.GetDouble(11),
                    TrainingFacilities = reader.GetInt32(13),
                    YouthFacilities = reader.GetInt32(14),
                    Recruitment = reader.GetInt32(15),
                    PhysioRating = reader.GetInt32(16),
                    BankBalance = reader.GetInt32(17),
                    Debt = reader.GetInt32(18),
                    TransferBudget = reader.GetInt32(19),
                    WageBudgetMonthly = reader.GetInt32(20),
                    IsActive = reader.GetInt32(21) == 1
                };

                if (!reader.IsDBNull(3)) club.ShortName = reader.GetString(3);
                if (!reader.IsDBNull(4)) club.Nickname = reader.GetString(4);
                if (!reader.IsDBNull(6)) club.City = reader.GetString(6);
                if (!reader.IsDBNull(7)) club.FoundedYear = reader.GetInt32(7);
                if (!reader.IsDBNull(12)) club.VenueId = reader.GetInt64(12);

                world.Clubs.Add(club);
            }
        }

        private void LoadContracts(SqliteConnection connection, World world)
        {
            using var cmd = new SqliteCommand(
                @"SELECT id, person_id, club_id, scope, signed_on, effective_from, effective_until,
                  wage_monthly, release_clause, squad_number, status
                  FROM contracts ORDER BY id", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var contract = new Contract
                {
                    Id = reader.GetInt64(0),
                    PersonId = reader.GetInt64(1),
                    ClubId = reader.GetInt64(2),
                    Scope = ParseContractScope(reader.GetString(3)),
                    SignedOn = DateTime.Parse(reader.GetString(4)),
                    EffectiveFrom = DateTime.Parse(reader.GetString(5)),
                    EffectiveUntil = DateTime.Parse(reader.GetString(6)),
                    WageMonthly = reader.GetInt32(7),
                    Status = ParseContractStatus(reader.GetString(10))
                };

                if (!reader.IsDBNull(8)) contract.ReleaseClause = reader.GetInt32(8);
                if (!reader.IsDBNull(9)) contract.SquadNumber = reader.GetInt32(9);

                world.Contracts.Add(contract);
            }
        }

        private void LoadSeasons(SqliteConnection connection, World world)
        {
            using var cmd = new SqliteCommand(
                "SELECT id, label, start_date, end_date FROM seasons ORDER BY id", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                world.Seasons.Add(new Season
                {
                    Id = reader.GetInt64(0),
                    Label = reader.GetString(1),
                    StartDate = DateTime.Parse(reader.GetString(2)),
                    EndDate = DateTime.Parse(reader.GetString(3))
                });
            }
        }

        private void LoadCompetitions(SqliteConnection connection, World world)
        {
            using var cmd = new SqliteCommand(
                @"SELECT id, uid, name, short_name, scope, type, country_id, level, prestige, active
                  FROM competitions ORDER BY id", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var competition = new Competition
                {
                    Id = reader.GetInt64(0),
                    Uid = reader.GetString(1),
                    Name = reader.GetString(2),
                    Scope = reader.GetString(4) == "seleccion" ? CompetitionScope.NationalTeam : CompetitionScope.Club,
                    Type = reader.GetString(5) == "copa" ? CompetitionType.Cup : CompetitionType.League,
                    Prestige = reader.GetDouble(8),
                    Active = reader.GetInt32(9) == 1
                };

                if (!reader.IsDBNull(3)) competition.ShortName = reader.GetString(3);
                if (!reader.IsDBNull(6)) competition.CountryId = reader.GetInt64(6);
                if (!reader.IsDBNull(7)) competition.Level = reader.GetInt32(7);

                world.Competitions.Add(competition);
            }
        }

        private void LoadCompetitionEntries(SqliteConnection connection, World world)
        {
            using var cmd = new SqliteCommand(
                @"SELECT id, season_id, competition_id, club_id, national_team_id, status
                  FROM competition_entries ORDER BY id", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var entry = new CompetitionEntry
                {
                    Id = reader.GetInt64(0),
                    SeasonId = reader.GetInt64(1),
                    CompetitionId = reader.GetInt64(2),
                    Status = ParseEntryStatus(reader.GetString(5))
                };

                if (!reader.IsDBNull(3)) entry.ClubId = reader.GetInt64(3);
                if (!reader.IsDBNull(4)) entry.NationalTeamId = reader.GetInt64(4);

                world.CompetitionEntries.Add(entry);
            }
        }

        // ===== Helpers de parseo =====

        private static Position ParsePosition(string value)
        {
            switch (value)
            {
                case "POR": return Position.Goalkeeper;
                case "CIE": return Position.Defender;
                case "ALI": return Position.LeftWing;
                case "ALD": return Position.RightWing;
                case "PIV": return Position.Pivot;
                case "UNI": return Position.Universal;
                default: return Position.Universal;
            }
        }

        private static PreferredFoot ParseFoot(string value)
        {
            switch (value)
            {
                case "D": return PreferredFoot.Right;
                case "I": return PreferredFoot.Left;
                case "AM": return PreferredFoot.Both;
                default: return PreferredFoot.Right;
            }
        }

        private static PersonSource ParseSource(string value)
        {
            switch (value)
            {
                case "seed": return PersonSource.Seed;
                case "import": return PersonSource.Import;
                case "generated": return PersonSource.Generated;
                case "youth": return PersonSource.Youth;
                default: return PersonSource.Seed;
            }
        }

        private static KitPattern ParseKitPattern(string value)
        {
            switch (value)
            {
                case "solid": return KitPattern.Solid;
                case "stripes": return KitPattern.Stripes;
                case "halved": return KitPattern.Halved;
                case "sash": return KitPattern.Sash;
                default: return KitPattern.Solid;
            }
        }

        private static ContractScope ParseContractScope(string value)
        {
            switch (value)
            {
                case "primer_equipo": return ContractScope.FirstTeam;
                case "cantera": return ContractScope.Youth;
                case "staff": return ContractScope.Staff;
                default: return ContractScope.FirstTeam;
            }
        }

        private static ContractStatus ParseContractStatus(string value)
        {
            switch (value)
            {
                case "vigente": return ContractStatus.Active;
                case "renovado": return ContractStatus.Renewed;
                case "rescindido": return ContractStatus.Terminated;
                case "expirado": return ContractStatus.Expired;
                case "cesion": return ContractStatus.Loan;
                default: return ContractStatus.Active;
            }
        }

        private static EntryStatus ParseEntryStatus(string value)
        {
            switch (value)
            {
                case "activo": return EntryStatus.Active;
                case "eliminado": return EntryStatus.Eliminated;
                case "retirado": return EntryStatus.Withdrawn;
                case "sancionado": return EntryStatus.Sanctioned;
                default: return EntryStatus.Active;
            }
        }
    }
}