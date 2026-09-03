using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace UniFutsal.Data
{
    public sealed class CompetitionLinkImporter
    {
        private readonly string _dbPath;

        public CompetitionLinkImporter(string dbPath)
        {
            _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
        }

        public ImportResult Import(string csvPath)
        {
            var result = new ImportResult();

            if (!File.Exists(csvPath))
            {
                result.Errors++;
                result.ErrorMessages.Add($"Archivo no encontrado: {csvPath}");
                return result;
            }

            var lines = File.ReadAllLines(csvPath);
            if (lines.Length < 2)
            {
                result.Errors++;
                result.ErrorMessages.Add("El CSV no tiene datos.");
                return result;
            }

            var header = CsvHelper.ParseLine(lines[0]);

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                result.TotalRows++;

                try
                {
                    var fields = CsvHelper.ParseLine(line);
                    var fromUid = CsvHelper.GetField(header, fields, "from_competition_uid");
                    var toUid = CsvHelper.GetField(header, fields, "to_competition_uid");
                    var linkType = CsvHelper.GetField(header, fields, "link_type");
                    var slotsStr = CsvHelper.GetField(header, fields, "slots");
                    var priorityStr = CsvHelper.GetField(header, fields, "priority");

                    // Buscar IDs de competiciones
                    long fromId = GetCompetitionId(connection, fromUid);
                    long toId = GetCompetitionId(connection, toUid);

                    if (fromId == 0)
                    {
                        result.Skipped++;
                        result.ErrorMessages.Add($"Fila {i + 1}: competición origen '{fromUid}' no encontrada.");
                        continue;
                    }
                    if (toId == 0)
                    {
                        result.Skipped++;
                        result.ErrorMessages.Add($"Fila {i + 1}: competición destino '{toUid}' no encontrada.");
                        continue;
                    }

                    int slots = 2;
                    int slotsTemp;
                    if (int.TryParse(slotsStr, out slotsTemp)) slots = slotsTemp;

                    int priority = 1;
                    int priorityTemp;
                    if (int.TryParse(priorityStr, out priorityTemp)) priority = priorityTemp;

                    using var cmd = new SqliteCommand(@"
                        INSERT OR REPLACE INTO competition_links
                            (from_competition_id, to_competition_id, link_type, criteria_json, slots, priority)
                        VALUES (@from, @to, @type, '{}', @slots, @priority)", connection);
                    cmd.Parameters.AddWithValue("@from", fromId);
                    cmd.Parameters.AddWithValue("@to", toId);
                    cmd.Parameters.AddWithValue("@type", linkType);
                    cmd.Parameters.AddWithValue("@slots", slots);
                    cmd.Parameters.AddWithValue("@priority", priority);

                    int affected = cmd.ExecuteNonQuery();
                    if (affected > 0) result.Imported++;
                    else result.Skipped++;
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    result.ErrorMessages.Add($"Fila {i + 1}: {ex.Message}");
                }
            }

            return result;
        }

        private long GetCompetitionId(SqliteConnection connection, string uid)
        {
            using var cmd = new SqliteCommand(
                "SELECT id FROM competitions WHERE uid = @uid", connection);
            cmd.Parameters.AddWithValue("@uid", uid);
            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value) return 0;
            return Convert.ToInt64(result);
        }
    }
}