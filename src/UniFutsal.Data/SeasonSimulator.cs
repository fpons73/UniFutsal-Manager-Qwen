using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using UniFutsal.Core.Domain;
using UniFutsal.Core.Domain.Clubs;
using UniFutsal.Core.Domain.Competitions;
using UniFutsal.Core.Domain.Matches;
using UniFutsal.Engine;

namespace UniFutsal.Data
{
    /// <summary>
    /// Resultado de la simulación de una temporada.
    /// </summary>
    public sealed class SeasonReport
    {
        public string CompetitionName { get; set; } = string.Empty;
        public string CompetitionUid { get; set; } = string.Empty;
        public string SeasonLabel { get; set; } = string.Empty;
        public List<LeagueStanding> FinalStandings { get; set; } = new List<LeagueStanding>();
        public int TotalMatches { get; set; }
        public int TotalGoals { get; set; }
        public int HomeWins { get; set; }
        public int AwayWins { get; set; }
        public int Draws { get; set; }

        public double AverageGoalsPerMatch =>
            TotalMatches > 0 ? (double)TotalGoals / TotalMatches : 0.0;

        public double HomeWinPct =>
            TotalMatches > 0 ? (double)HomeWins / TotalMatches * 100.0 : 0.0;

        public double AwayWinPct =>
            TotalMatches > 0 ? (double)AwayWins / TotalMatches * 100.0 : 0.0;

        public double DrawPct =>
            TotalMatches > 0 ? (double)Draws / TotalMatches * 100.0 : 0.0;

        public LeagueStanding? Champion =>
            FinalStandings.Count > 0 ? FinalStandings[0] : null;

        public LeagueStanding? RunnerUp =>
            FinalStandings.Count > 1 ? FinalStandings[1] : null;
    }

    /// <summary>
    /// Orquestador de temporada: simula todos los partidos de una competición
    /// y devuelve un reporte con la clasificación final.
    /// Determinista: misma seed del mundo → mismo resultado bit a bit.
    /// </summary>
    public sealed class SeasonSimulator
    {
        private readonly string _dbPath;

        public SeasonSimulator(string dbPath)
        {
            _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
        }

        /// <summary>
        /// Simula una temporada completa de una competición.
        /// </summary>
        /// <param name="competitionUid">Uid de la competición a simular.</param>
        /// <param name="seasonLabel">Etiqueta de la temporada (ej. "2026/27").</param>
        /// <param name="persist">Si true, escribe los resultados en la BD.</param>
        /// <returns>Reporte con la clasificación final y estadísticas.</returns>
        public SeasonReport SimulateSeason(string competitionUid, string seasonLabel, bool persist = true)
        {
            // 1. Cargar el mundo en memoria
            var loader = new WorldLoader(_dbPath);
            var world = loader.Load();

            // 2. Buscar competición y temporada
            Competition? competition = null;
            foreach (var c in world.Competitions)
            {
                if (c.Uid == competitionUid)
                {
                    competition = c;
                    break;
                }
            }
            if (competition == null)
            {
                throw new ArgumentException($"Competición '{competitionUid}' no encontrada.");
            }

            Season? season = null;
            foreach (var s in world.Seasons)
            {
                if (s.Label == seasonLabel)
                {
                    season = s;
                    break;
                }
            }
            if (season == null)
            {
                throw new ArgumentException($"Temporada '{seasonLabel}' no encontrada.");
            }

                        // 3. Registrar SOLO los clubes inscritos en esta competición
            var table = new LeagueTable();
            foreach (var entry in world.CompetitionEntries)
            {
                if (entry.CompetitionId == competition.Id
                    && entry.SeasonId == season.Id
                    && entry.ClubId.HasValue
                    && entry.Club != null)
                {
                    table.RegisterClub(entry.Club.Id, entry.Club.Uid, entry.Club.Name);
                }
            }

            // 4. Obtener partidos de la competición/temporada ordenados por jornada
            var matches = GetMatchesForCompetition(_dbPath, competition.Id, season.Id);

            // 5. Simular cada partido y actualizar la tabla
            var simulator = new InstantMatchSimulator(world);
            int totalMatches = 0;
            int totalGoals = 0;
            int homeWins = 0;
            int awayWins = 0;
            int draws = 0;

            var updatedMatches = new List<(long MatchId, int HomeScore, int AwayScore)>();

            foreach (var match in matches)
            {
                // Saltar partidos ya jugados (idempotencia)
                if (match.Status == MatchStatus.Played)
                {
                    // Aún así contabilizar para el reporte
                    if (match.HomeScore.HasValue && match.AwayScore.HasValue)
                    {
                        table.RecordResult(match.HomeClubId!.Value, match.AwayClubId!.Value,
                            match.HomeScore.Value, match.AwayScore.Value);
                        totalMatches++;
                        totalGoals += match.HomeScore.Value + match.AwayScore.Value;
                        if (match.HomeScore.Value > match.AwayScore.Value) homeWins++;
                        else if (match.AwayScore.Value > match.HomeScore.Value) awayWins++;
                        else draws++;
                    }
                    continue;
                }

                // Simular el partido
                var outcome = simulator.Simulate(match, allowPenalties: false);

                // Actualizar la tabla con los goles del tiempo reglamentario
                if (match.HomeClubId.HasValue && match.AwayClubId.HasValue)
                {
                    table.RecordResult(match.HomeClubId.Value, match.AwayClubId.Value,
                        outcome.HomeScore, outcome.AwayScore);
                }

                // Acumular estadísticas
                totalMatches++;
                totalGoals += outcome.HomeScore + outcome.AwayScore;
                if (outcome.HomeScore > outcome.AwayScore) homeWins++;
                else if (outcome.AwayScore > outcome.HomeScore) awayWins++;
                else draws++;

                updatedMatches.Add((match.Id, outcome.HomeScore, outcome.AwayScore));
            }

            // 6. Persistir los resultados en la BD
            if (persist && updatedMatches.Count > 0)
            {
                PersistMatchResults(_dbPath, updatedMatches);
            }

            // 7. Construir reporte
            var report = new SeasonReport
            {
                CompetitionName = competition.Name,
                CompetitionUid = competition.Uid,
                SeasonLabel = season.Label,
                FinalStandings = table.GetOrderedStandings(),
                TotalMatches = totalMatches,
                TotalGoals = totalGoals,
                HomeWins = homeWins,
                AwayWins = awayWins,
                Draws = draws
            };

            return report;
        }

        /// <summary>
        /// Obtiene los partidos de una competición/temporada ordenados por jornada.
        /// </summary>
        private List<Match> GetMatchesForCompetition(string dbPath, long competitionId, long seasonId)
        {
            var matches = new List<Match>();

            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            using var cmd = new SqliteCommand(@"
                SELECT id, season_id, competition_id, matchday, home_club_id, away_club_id,
                       home_score, away_score, rng_seed, status
                FROM matches
                WHERE competition_id = @comp_id AND season_id = @season_id
                ORDER BY matchday ASC, id ASC", connection);
            cmd.Parameters.AddWithValue("@comp_id", competitionId);
            cmd.Parameters.AddWithValue("@season_id", seasonId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var match = new Match
                {
                    Id = reader.GetInt64(0)
                };

                if (!reader.IsDBNull(3)) match.Matchday = reader.GetInt32(3);
                if (!reader.IsDBNull(4)) match.HomeClubId = reader.GetInt64(4);
                if (!reader.IsDBNull(5)) match.AwayClubId = reader.GetInt64(5);
                if (!reader.IsDBNull(6)) match.HomeScore = reader.GetInt32(6);
                if (!reader.IsDBNull(7)) match.AwayScore = reader.GetInt32(7);
                match.RngSeed = reader.GetInt64(8);
                match.Status = ParseMatchStatus(reader.GetString(9));
                matches.Add(match);
            }

            return matches;
        }

        /// <summary>
        /// Persiste los resultados de los partidos simulados en la BD.
        /// </summary>
        private void PersistMatchResults(string dbPath, List<(long MatchId, int HomeScore, int AwayScore)> updates)
        {
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                foreach (var (matchId, homeScore, awayScore) in updates)
                {
                    using var cmd = new SqliteCommand(@"
                        UPDATE matches
                        SET status = 'jugado',
                            home_score = @home,
                            away_score = @away
                        WHERE id = @id", connection, transaction);
                    cmd.Parameters.AddWithValue("@id", matchId);
                    cmd.Parameters.AddWithValue("@home", homeScore);
                    cmd.Parameters.AddWithValue("@away", awayScore);
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

        private static MatchStatus ParseMatchStatus(string value)
        {
            switch (value)
            {
                case "programado": return MatchStatus.Scheduled;
                case "jugado": return MatchStatus.Played;
                case "aplazado": return MatchStatus.Postponed;
                case "cancelado": return MatchStatus.Cancelled;
                case "walkover": return MatchStatus.Walkover;
                default: return MatchStatus.Scheduled;
            }
        }
    }
}