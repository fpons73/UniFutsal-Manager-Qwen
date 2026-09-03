using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace UniFutsal.Data
{
    /// <summary>
    /// Resultado del avance de temporada.
    /// </summary>
    public sealed class SeasonAdvanceResult
    {
        public string PreviousSeasonLabel { get; set; } = string.Empty;
        public string NewSeasonLabel { get; set; } = string.Empty;
        public int EntriesCopied { get; set; }
        public int MatchesGenerated { get; set; }
        public string NewWorldDate { get; set; } = string.Empty;
        public int PlayersDeveloped { get; set; }
        public int PlayersImproved { get; set; }
        public int PlayersDeclined { get; set; }
        public int PlayersStable { get; set; }
        public int PlayersRetired { get; set; }
        public int ContractsExpired { get; set; }
        public List<string> PromotedClubUids { get; set; } = new List<string>();
        public List<string> RelegatedClubUids { get; set; } = new List<string>();
        public int TransfersSigned { get; set; }
        public List<string> TransferDescriptions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Avanza el mundo de una temporada a la siguiente.
    /// Crea la nueva temporada, copia inscripciones, procesa ascensos/descensos,
    /// desarrolla jugadores, procesa retiradas, ejecuta el mercado de fichajes,
    /// genera calendario y actualiza world_date.
    /// </summary>
    public sealed class SeasonAdvancer
    {
        private readonly string _dbPath;

        public SeasonAdvancer(string dbPath)
        {
            _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
        }

        /// <summary>
        /// Avanza a la siguiente temporada de una competición.
        /// </summary>
        public SeasonAdvanceResult AdvanceSeason(string competitionUid)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            // 1. Buscar competición
            long competitionId = GetCompetitionId(connection, competitionUid);
            if (competitionId == 0)
            {
                throw new ArgumentException($"Competición '{competitionUid}' no encontrada.");
            }

            // 2. Buscar la temporada más reciente con inscripciones
            long currentSeasonId = GetLatestSeasonForCompetition(connection, competitionId);
            if (currentSeasonId == 0)
            {
                throw new ArgumentException(
                    $"No se encontró ninguna temporada con inscripciones para '{competitionUid}'.");
            }

            // 3. Verificar que todos los partidos están jugados en TODAS las competiciones
            var allCompIds = GetAllActiveCompetitionIds(connection);
            foreach (var compId in allCompIds)
            {
                int unplayed = GetUnplayedMatchCount(connection, compId, currentSeasonId);
                if (unplayed > 0)
                {
                    string compUidLocal = GetCompetitionUid(connection, compId);
                    throw new InvalidOperationException(
                        $"La temporada actual de '{compUidLocal}' tiene {unplayed} partidos sin jugar. " +
                        $"Simula todas las competiciones antes de avanzar.");
                }
            }

            // 4. Obtener detalles de la temporada actual
            string currentLabel;
            DateTime currentStartDate;
            DateTime currentEndDate;
            GetSeasonDetails(connection, currentSeasonId,
                out currentLabel, out currentStartDate, out currentEndDate);

            // 5. Calcular la siguiente temporada
            string nextLabel = ComputeNextSeasonLabel(currentLabel);
            DateTime nextStartDate = currentStartDate.AddYears(1);
            DateTime nextEndDate = currentEndDate.AddYears(1);

            if (SeasonExists(connection, nextLabel))
            {
                throw new InvalidOperationException(
                    $"La temporada '{nextLabel}' ya existe. No se puede avanzar de nuevo.");
            }

            // 6. Insertar nueva temporada
            long nextSeasonId = InsertSeason(connection, nextLabel, nextStartDate, nextEndDate);

            // 7. COPIAR INSCRIPCIONES DE TODAS LAS COMPETICIONES
            int entriesCopied = CopyEntriesForAllCompetitions(connection, currentSeasonId, nextSeasonId, allCompIds);

            // 8. Procesar ascensos/descensos (mueve clubes entre divisiones)
            var promoReleg = new PromotionRelegationProcessor(_dbPath);
            var promoResult = promoReleg.Process(nextLabel);

            // 9. Desarrollo anual de jugadores
            var developer = new PlayerDeveloper(_dbPath);
            var devRecords = developer.DevelopAll(nextLabel);
            int improved = 0, declined = 0, stable = 0;
            foreach (var r in devRecords)
            {
                if (r.Delta > 0) improved++;
                else if (r.Delta < 0) declined++;
                else stable++;
            }

            // 10. Retiradas
            var retirer = new PlayerRetirer(_dbPath);
            var retiredIds = retirer.ProcessRetirements(nextLabel);

            // 11. Expiración de contratos
            int contractsExpired = retirer.ProcessContractExpirations(nextLabel);

            // 11b. Mercado de fichajes (los clubes reemplazan retirados y expirados)
            var market = new TransferMarketProcessor(_dbPath);
            var marketResult = market.Process(nextLabel);

            // 12. Generar calendario para TODAS las competiciones
            int totalMatchesGenerated = 0;
            var allCompetitionUids = GetAllCompetitionUids(connection);
            foreach (var compUidLocal in allCompetitionUids)
            {
                var generator = new CalendarGenerator(_dbPath);
                int matches = generator.Generate(compUidLocal, nextLabel);
                totalMatchesGenerated += matches;
            }

            // 13. Actualizar world_date
            string newWorldDate = nextStartDate.ToString("yyyy-MM-dd");
            UpdateWorldDate(connection, newWorldDate);

            return new SeasonAdvanceResult
            {
                PreviousSeasonLabel = currentLabel,
                NewSeasonLabel = nextLabel,
                EntriesCopied = entriesCopied,
                MatchesGenerated = totalMatchesGenerated,
                NewWorldDate = newWorldDate,
                PlayersDeveloped = devRecords.Count,
                PlayersImproved = improved,
                PlayersDeclined = declined,
                PlayersStable = stable,
                PlayersRetired = retiredIds.Count,
                ContractsExpired = contractsExpired,
                PromotedClubUids = promoResult.PromotedClubUids,
                RelegatedClubUids = promoResult.RelegatedClubUids,
                TransfersSigned = marketResult.TotalTransfers,
                TransferDescriptions = marketResult.TransfersDescription
            };
        }

        // ===== Helpers =====

        private long GetCompetitionId(SqliteConnection connection, string uid)
        {
            using var cmd = new SqliteCommand(
                "SELECT id FROM competitions WHERE uid = @uid", connection);
            cmd.Parameters.AddWithValue("@uid", uid);
            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value) return 0;
            return Convert.ToInt64(result);
        }

        private string GetCompetitionUid(SqliteConnection connection, long id)
        {
            using var cmd = new SqliteCommand(
                "SELECT uid FROM competitions WHERE id = @id", connection);
            cmd.Parameters.AddWithValue("@id", id);
            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value) return "(unknown)";
            return result.ToString() ?? "(unknown)";
        }

        private List<long> GetAllActiveCompetitionIds(SqliteConnection connection)
        {
            var ids = new List<long>();
            using var cmd = new SqliteCommand(
                "SELECT id FROM competitions WHERE active = 1", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                ids.Add(reader.GetInt64(0));
            }
            return ids;
        }

        private List<string> GetAllCompetitionUids(SqliteConnection connection)
        {
            var uids = new List<string>();
            using var cmd = new SqliteCommand(
                "SELECT uid FROM competitions WHERE active = 1", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                uids.Add(reader.GetString(0));
            }
            return uids;
        }

        private long GetLatestSeasonForCompetition(SqliteConnection connection, long competitionId)
        {
            using var cmd = new SqliteCommand(@"
                SELECT ce.season_id
                FROM competition_entries ce
                JOIN seasons s ON s.id = ce.season_id
                WHERE ce.competition_id = @comp_id AND ce.status = 'activo'
                ORDER BY s.start_date DESC
                LIMIT 1", connection);
            cmd.Parameters.AddWithValue("@comp_id", competitionId);
            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value) return 0;
            return Convert.ToInt64(result);
        }

        private int GetUnplayedMatchCount(SqliteConnection connection, long competitionId, long seasonId)
        {
            using var cmd = new SqliteCommand(@"
                SELECT COUNT(*) FROM matches
                WHERE competition_id = @comp_id AND season_id = @season_id
                AND status != 'jugado'", connection);
            cmd.Parameters.AddWithValue("@comp_id", competitionId);
            cmd.Parameters.AddWithValue("@season_id", seasonId);
            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value) return 0;
            return Convert.ToInt32(result);
        }

        private void GetSeasonDetails(SqliteConnection connection, long seasonId,
            out string label, out DateTime startDate, out DateTime endDate)
        {
            using var cmd = new SqliteCommand(
                "SELECT label, start_date, end_date FROM seasons WHERE id = @id", connection);
            cmd.Parameters.AddWithValue("@id", seasonId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                label = reader.GetString(0);
                startDate = DateTime.Parse(reader.GetString(1));
                endDate = DateTime.Parse(reader.GetString(2));
            }
            else
            {
                throw new ArgumentException($"Temporada con id {seasonId} no encontrada.");
            }
        }

        private static string ComputeNextSeasonLabel(string currentLabel)
        {
            var parts = currentLabel.Split('/');
            if (parts.Length == 2)
            {
                int startYear;
                if (int.TryParse(parts[0], out startYear))
                {
                    int nextStart = startYear + 1;
                    int nextEnd = nextStart + 1;
                    string endStr;
                    if (parts[1].Length == 2)
                    {
                        endStr = (nextEnd % 100).ToString("D2");
                    }
                    else
                    {
                        endStr = nextEnd.ToString();
                    }
                    return $"{nextStart}/{endStr}";
                }
            }
            throw new ArgumentException($"Formato de temporada no reconocido: '{currentLabel}'");
        }

        private bool SeasonExists(SqliteConnection connection, string label)
        {
            using var cmd = new SqliteCommand(
                "SELECT COUNT(*) FROM seasons WHERE label = @label", connection);
            cmd.Parameters.AddWithValue("@label", label);
            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value) return false;
            return Convert.ToInt64(result) > 0;
        }

        private long InsertSeason(SqliteConnection connection, string label, DateTime startDate, DateTime endDate)
        {
            using var cmd = new SqliteCommand(@"
                INSERT INTO seasons (label, start_date, end_date)
                VALUES (@label, @start, @end);
                SELECT last_insert_rowid();", connection);
            cmd.Parameters.AddWithValue("@label", label);
            cmd.Parameters.AddWithValue("@start", startDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@end", endDate.ToString("yyyy-MM-dd"));
            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
            {
                throw new InvalidOperationException("No se pudo insertar la nueva temporada.");
            }
            return Convert.ToInt64(result);
        }

        /// <summary>
        /// COPIA INSCRIPCIONES DE TODAS LAS COMPETICIONES (no solo la de entrada).
        /// </summary>
        private int CopyEntriesForAllCompetitions(SqliteConnection connection,
            long fromSeasonId, long toSeasonId, List<long> competitionIds)
        {
            int total = 0;
            foreach (var compId in competitionIds)
            {
                using var cmd = new SqliteCommand(@"
                    INSERT INTO competition_entries
                        (season_id, competition_id, club_id, national_team_id, group_id, seed, qualified_via_link_id, status)
                    SELECT @to_season, competition_id, club_id, national_team_id, group_id, seed, qualified_via_link_id, status
                    FROM competition_entries
                    WHERE season_id = @from_season AND competition_id = @comp_id", connection);
                cmd.Parameters.AddWithValue("@to_season", toSeasonId);
                cmd.Parameters.AddWithValue("@from_season", fromSeasonId);
                cmd.Parameters.AddWithValue("@comp_id", compId);
                total += cmd.ExecuteNonQuery();
            }
            return total;
        }

        private void UpdateWorldDate(SqliteConnection connection, string newDate)
        {
            using var cmd = new SqliteCommand(
                "INSERT OR REPLACE INTO meta (key, value) VALUES ('world_date', @date)", connection);
            cmd.Parameters.AddWithValue("@date", newDate);
            cmd.ExecuteNonQuery();
        }
    }
}