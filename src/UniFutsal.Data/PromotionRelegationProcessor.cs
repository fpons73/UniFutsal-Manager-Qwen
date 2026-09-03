    using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using UniFutsal.Core.Domain.Competitions;

namespace UniFutsal.Data
{
    /// <summary>
    /// Resultado del procesamiento de ascensos/descensos.
    /// </summary>
    public sealed class PromotionRelegationResult
    {
        public List<string> PromotedClubUids { get; set; } = new List<string>();
        public List<string> RelegatedClubUids { get; set; } = new List<string>();
    }

    /// <summary>
    /// Procesa ascensos y descensos entre competiciones vinculadas.
    /// Lee los enlaces de competition_links y mueve clubes entre divisiones.
    /// </summary>
    public sealed class PromotionRelegationProcessor
    {
        private readonly string _dbPath;

        public PromotionRelegationProcessor(string dbPath)
        {
            _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
        }

        /// <summary>
        /// Procesa todos los ascensos/descensos para la nueva temporada.
        /// Debe ejecutarse DESPUÉS de que SeasonAdvancer copie las inscripciones.
        /// </summary>
        public PromotionRelegationResult Process(string newSeasonLabel)
        {
            var result = new PromotionRelegationResult();

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            // Obtener todos los enlaces de competición
            var links = LoadCompetitionLinks(connection);

            foreach (var link in links)
            {
                // Obtener los clubes de la competición de origen (la que ya se jugó)
                // y los de la competición de destino (a la que van)
                var standings = GetFinalStandings(connection, link.FromCompetitionId, newSeasonLabel);

                if (standings.Count == 0) continue;

                                List<string> clubsToMove;
                if (link.LinkType == "relegation" || link.LinkType == "descenso")
                {
                    // Los últimos N descienden
                    clubsToMove = standings.GetRange(
                        Math.Max(0, standings.Count - link.Slots),
                        Math.Min(link.Slots, standings.Count));
                }
                else if (link.LinkType == "promotion" || link.LinkType == "ascenso")
                {
                    // Los primeros N ascienden
                    clubsToMove = standings.GetRange(0, Math.Min(link.Slots, standings.Count));
                }
                else
                {
                    continue;
                }

                // Mover los clubes: quitar de la competición origen y poner en la destino
                foreach (var clubUid in clubsToMove)
                {
                    MoveClubEntry(connection, newSeasonLabel, link.FromCompetitionId, link.ToCompetitionId, clubUid);

                                        if (link.LinkType == "promotion" || link.LinkType == "ascenso")
                    {
                        result.PromotedClubUids.Add(clubUid);
                    }
                    else if (link.LinkType == "relegation" || link.LinkType == "descenso")
                    {
                        result.RelegatedClubUids.Add(clubUid);
                    }
                }
            }

            return result;
        }

        // ===== Helpers =====

        private sealed class CompetitionLink
        {
            public long FromCompetitionId;
            public long ToCompetitionId;
            public string LinkType = string.Empty;
            public int Slots;
        }

        private List<CompetitionLink> LoadCompetitionLinks(SqliteConnection connection)
        {
            var links = new List<CompetitionLink>();
            using var cmd = new SqliteCommand(
                "SELECT from_competition_id, to_competition_id, link_type, slots FROM competition_links", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                links.Add(new CompetitionLink
                {
                    FromCompetitionId = reader.GetInt64(0),
                    ToCompetitionId = reader.GetInt64(1),
                    LinkType = reader.GetString(2),
                    Slots = reader.GetInt32(3)
                });
            }
            return links;
        }

        /// <summary>
        /// Obtiene la clasificación final de una competición en la temporada ANTERIOR.
        /// Devuelve los uids de los clubes ordenados de mejor a peor.
        /// </summary>
        private List<string> GetFinalStandings(SqliteConnection connection, long competitionId, string newSeasonLabel)
        {
            // La temporada anterior es la que tiene la label inmediatamente anterior
            int newYear = ParseSeasonYear(newSeasonLabel);
            int prevYear = newYear - 1;

            var standings = new List<string>();

            // Obtener los partidos de la temporada anterior para esta competición
            using var cmd = new SqliteCommand(@"
                SELECT ce.club_id, c.uid,
                    SUM(CASE WHEN m.home_club_id = ce.club_id AND m.home_score > m.away_score THEN 3
                             WHEN m.away_club_id = ce.club_id AND m.away_score > m.home_score THEN 3
                             WHEN m.home_score = m.away_score THEN 1 ELSE 0 END) as points
                FROM competition_entries ce
                JOIN clubs c ON c.id = ce.club_id
                JOIN seasons s ON s.id = ce.season_id
                LEFT JOIN matches m ON m.competition_id = ce.competition_id AND m.season_id = ce.season_id AND m.status = 'jugado'
                WHERE ce.competition_id = @comp_id
                  AND s.label LIKE @prev_year || '/%'
                GROUP BY ce.club_id, c.uid
                ORDER BY points DESC", connection);
            cmd.Parameters.AddWithValue("@comp_id", competitionId);
            cmd.Parameters.AddWithValue("@prev_year", prevYear.ToString());

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                standings.Add(reader.GetString(1)); // uid del club
            }

            return standings;
        }

        private void MoveClubEntry(SqliteConnection connection, string newSeasonLabel,
            long fromCompetitionId, long toCompetitionId, string clubUid)
        {
            // Obtener IDs
            long seasonId = GetSeasonId(connection, newSeasonLabel);
            long clubId = GetClubId(connection, clubUid);
            if (seasonId == 0 || clubId == 0) return;

            // Eliminar de la competición origen
            using var deleteCmd = new SqliteCommand(@"
                DELETE FROM competition_entries
                WHERE season_id = @season AND competition_id = @comp AND club_id = @club", connection);
            deleteCmd.Parameters.AddWithValue("@season", seasonId);
            deleteCmd.Parameters.AddWithValue("@comp", fromCompetitionId);
            deleteCmd.Parameters.AddWithValue("@club", clubId);
            deleteCmd.ExecuteNonQuery();

            // Añadir a la competición destino
            using var insertCmd = new SqliteCommand(@"
                INSERT OR IGNORE INTO competition_entries
                    (season_id, competition_id, club_id, status)
                VALUES (@season, @comp, @club, 'activo')", connection);
            insertCmd.Parameters.AddWithValue("@season", seasonId);
            insertCmd.Parameters.AddWithValue("@comp", toCompetitionId);
            insertCmd.Parameters.AddWithValue("@club", clubId);
            insertCmd.ExecuteNonQuery();
        }

        private static int ParseSeasonYear(string seasonLabel)
        {
            var parts = seasonLabel.Split('/');
            if (parts.Length >= 1 && int.TryParse(parts[0], out int year))
            {
                return year;
            }
            throw new ArgumentException($"Formato de temporada no reconocido: '{seasonLabel}'");
        }

        private long GetSeasonId(SqliteConnection connection, string label)
        {
            using var cmd = new SqliteCommand(
                "SELECT id FROM seasons WHERE label = @label", connection);
            cmd.Parameters.AddWithValue("@label", label);
            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value) return 0;
            return Convert.ToInt64(result);
        }

        private long GetClubId(SqliteConnection connection, string uid)
        {
            using var cmd = new SqliteCommand(
                "SELECT id FROM clubs WHERE uid = @uid", connection);
            cmd.Parameters.AddWithValue("@uid", uid);
            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value) return 0;
            return Convert.ToInt64(result);
        }
    }
}