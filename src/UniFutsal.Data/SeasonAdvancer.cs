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
    }

    /// <summary>
    /// Avanza el mundo de una temporada a la siguiente.
    /// Crea la nueva temporada, copia inscripciones, genera calendario,
    /// desarrolla jugadores, procesa retiradas y expiraciones, y actualiza world_date.
    /// Determinista: no usa Random ni DateTime.Now.
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

            // 3. Verificar que todos los partidos están jugados
            int unplayedCount = GetUnplayedMatchCount(connection, competitionId, currentSeasonId);
            if (unplayedCount > 0)
            {
                throw new InvalidOperationException(
                    $"La temporada actual tiene {unplayedCount} partidos sin jugar. " +
                    $"Simula la temporada antes de avanzar.");
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

            // 7. Copiar inscripciones de clubes
            int entriesCopied = CopyEntries(connection, currentSeasonId, nextSeasonId, competitionId);

            // 8. Desarrollo anual de jugadores (envejecimiento + mejora/declive)
            var developer = new PlayerDeveloper(_dbPath);
            var devRecords = developer.DevelopAll(nextLabel);
            int improved = 0, declined = 0, stable = 0;
            foreach (var r in devRecords)
            {
                if (r.Delta > 0) improved++;
                else if (r.Delta < 0) declined++;
                else stable++;
            }

            // 9. Retiradas de jugadores veteranos
            var retirer = new PlayerRetirer(_dbPath);
            var retiredIds = retirer.ProcessRetirements(nextLabel);

            // 10. Expiración de contratos
            int contractsExpired = retirer.ProcessContractExpirations(nextLabel);

            // 11. Generar calendario (CalendarGenerator usa su propia conexión)
            var generator = new CalendarGenerator(_dbPath);
            int matchesGenerated = generator.Generate(competitionUid, nextLabel);

            // 12. Actualizar world_date
            string newWorldDate = nextStartDate.ToString("yyyy-MM-dd");
            UpdateWorldDate(connection, newWorldDate);

            return new SeasonAdvanceResult
            {
                PreviousSeasonLabel = currentLabel,
                NewSeasonLabel = nextLabel,
                EntriesCopied = entriesCopied,
                MatchesGenerated = matchesGenerated,
                NewWorldDate = newWorldDate,
                PlayersDeveloped = devRecords.Count,
                PlayersImproved = improved,
                PlayersDeclined = declined,
                PlayersStable = stable,
                PlayersRetired = retiredIds.Count,
                ContractsExpired = contractsExpired
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

        private int CopyEntries(SqliteConnection connection, long fromSeasonId, long toSeasonId, long competitionId)
        {
            using var cmd = new SqliteCommand(@"
                INSERT INTO competition_entries (season_id, competition_id, club_id, national_team_id, group_id, seed, qualified_via_link_id, status)
                SELECT @to_season, competition_id, club_id, national_team_id, group_id, seed, qualified_via_link_id, status
                FROM competition_entries
                WHERE season_id = @from_season AND competition_id = @comp_id", connection);
            cmd.Parameters.AddWithValue("@to_season", toSeasonId);
            cmd.Parameters.AddWithValue("@from_season", fromSeasonId);
            cmd.Parameters.AddWithValue("@comp_id", competitionId);
            return cmd.ExecuteNonQuery();
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