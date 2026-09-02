using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace UniFutsal.Data
{
    public class CompetitionImporter
    {
        private readonly string _dbPath;

        public CompetitionImporter(string dbPath)
        {
            _dbPath = dbPath;
        }

        public ImportResult ImportSeasons(string csvPath)
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
                    var label = CsvHelper.GetField(header, fields, "label");
                    var startDate = CsvHelper.GetField(header, fields, "start_date");
                    var endDate = CsvHelper.GetField(header, fields, "end_date");

                    if (string.IsNullOrWhiteSpace(label))
                    {
                        result.Skipped++;
                        continue;
                    }

                    using var cmd = new SqliteCommand(
                        @"INSERT OR IGNORE INTO seasons (label, start_date, end_date)
                          VALUES (@label, @start_date, @end_date)", connection);
                    cmd.Parameters.AddWithValue("@label", label);
                    cmd.Parameters.AddWithValue("@start_date", startDate);
                    cmd.Parameters.AddWithValue("@end_date", endDate);

                    int affected = cmd.ExecuteNonQuery();
                    if (affected > 0)
                        result.Imported++;
                    else
                        result.Skipped++;
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    result.ErrorMessages.Add($"Fila {i + 1}: {ex.Message}");
                }
            }

            return result;
        }

        public ImportResult ImportCompetitions(string csvPath)
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

            // Nota: NO activamos PRAGMA foreign_keys = ON aquí porque
            // las verificaciones de datos se hacen manualmente abajo.
            // Esto evita problemas con tablas que se crean después en el DDL (ej. data_packs).

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                result.TotalRows++;

                try
                {
                    var fields = CsvHelper.ParseLine(line);
                    var uid = CsvHelper.GetField(header, fields, "uid");
                    var name = CsvHelper.GetField(header, fields, "name");
                    var shortName = CsvHelper.GetField(header, fields, "short_name");
                    var scope = CsvHelper.GetField(header, fields, "scope");
                    var type = CsvHelper.GetField(header, fields, "type");
                    var countryCode3 = CsvHelper.GetField(header, fields, "country_code3");
                    var levelStr = CsvHelper.GetField(header, fields, "level");
                    var prestigeStr = CsvHelper.GetField(header, fields, "prestige");
                    var activeStr = CsvHelper.GetField(header, fields, "active");

                    if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(name))
                    {
                        result.Skipped++;
                        continue;
                    }

                    // Buscar país (opcional, NULL = internacional)
                    object countryValue = DBNull.Value;
                    if (!string.IsNullOrWhiteSpace(countryCode3))
                    {
                        long? countryId = GetCountryIdByCode3(connection, countryCode3);
                        if (countryId.HasValue)
                        {
                            countryValue = countryId.Value;
                        }
                    }

                    int level = 0;
                    int levelTemp;
                    if (int.TryParse(levelStr, out levelTemp))
                    {
                        level = levelTemp;
                    }

                    double prestige = 30.0;
                    double prestigeTemp;
                    if (double.TryParse(prestigeStr, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out prestigeTemp))
                    {
                        prestige = prestigeTemp;
                    }

                    int active = 1;
                    if (activeStr == "0" || activeStr.ToLowerInvariant() == "false")
                    {
                        active = 0;
                    }

                    using var cmd = new SqliteCommand(
                        @"INSERT OR IGNORE INTO competitions (uid, name, short_name, scope, type, country_id, level, prestige, active)
                          VALUES (@uid, @name, @short_name, @scope, @type, @country_id, @level, @prestige, @active)", connection);
                    cmd.Parameters.AddWithValue("@uid", uid);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@short_name", ToDbValue(shortName));
                    cmd.Parameters.AddWithValue("@scope", scope);
                    cmd.Parameters.AddWithValue("@type", type);
                    cmd.Parameters.AddWithValue("@country_id", countryValue);
                    cmd.Parameters.AddWithValue("@level", level > 0 ? (object)level : DBNull.Value);
                    cmd.Parameters.AddWithValue("@prestige", prestige);
                    cmd.Parameters.AddWithValue("@active", active);

                    int affected = cmd.ExecuteNonQuery();
                    if (affected > 0)
                        result.Imported++;
                    else
                        result.Skipped++;
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    result.ErrorMessages.Add($"Fila {i + 1}: {ex.Message}");
                }
            }

            return result;
        }

        public ImportResult ImportEntries(string csvPath)
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

            // Nota: NO activamos PRAGMA foreign_keys = ON aquí (misma razón que ImportCompetitions)

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                result.TotalRows++;

                try
                {
                    var fields = CsvHelper.ParseLine(line);
                    var seasonLabel = CsvHelper.GetField(header, fields, "season_label");
                    var competitionUid = CsvHelper.GetField(header, fields, "competition_uid");
                    var clubUid = CsvHelper.GetField(header, fields, "club_uid");
                    var status = CsvHelper.GetField(header, fields, "status");

                    if (string.IsNullOrWhiteSpace(seasonLabel) || string.IsNullOrWhiteSpace(competitionUid) || string.IsNullOrWhiteSpace(clubUid))
                    {
                        result.Skipped++;
                        continue;
                    }

                    // Buscar season_id
                    long? seasonId = GetSeasonIdByLabel(connection, seasonLabel);
                    if (seasonId == null)
                    {
                        result.Skipped++;
                        result.ErrorMessages.Add($"Fila {i + 1}: temporada '{seasonLabel}' no encontrada.");
                        continue;
                    }

                    // Buscar competition_id
                    long? competitionId = GetCompetitionIdByUid(connection, competitionUid);
                    if (competitionId == null)
                    {
                        result.Skipped++;
                        result.ErrorMessages.Add($"Fila {i + 1}: competición '{competitionUid}' no encontrada.");
                        continue;
                    }

                    // Buscar club_id
                    long? clubId = GetClubIdByUid(connection, clubUid);
                    if (clubId == null)
                    {
                        result.Skipped++;
                        result.ErrorMessages.Add($"Fila {i + 1}: club '{clubUid}' no encontrado.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(status))
                    {
                        status = "activo";
                    }

                    using var cmd = new SqliteCommand(
                        @"INSERT OR IGNORE INTO competition_entries (season_id, competition_id, club_id, status)
                          VALUES (@season_id, @competition_id, @club_id, @status)", connection);
                    cmd.Parameters.AddWithValue("@season_id", seasonId.Value);
                    cmd.Parameters.AddWithValue("@competition_id", competitionId.Value);
                    cmd.Parameters.AddWithValue("@club_id", clubId.Value);
                    cmd.Parameters.AddWithValue("@status", status);

                    int affected = cmd.ExecuteNonQuery();
                    if (affected > 0)
                        result.Imported++;
                    else
                        result.Skipped++;
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    result.ErrorMessages.Add($"Fila {i + 1}: {ex.Message}");
                }
            }

            return result;
        }

        private long? GetCountryIdByCode3(SqliteConnection connection, string code3)
        {
            using var cmd = new SqliteCommand("SELECT id FROM countries WHERE code3 = @code3", connection);
            cmd.Parameters.AddWithValue("@code3", code3.ToUpperInvariant());
            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
            {
                return null;
            }
            return Convert.ToInt64(result);
        }

        private long? GetSeasonIdByLabel(SqliteConnection connection, string label)
        {
            using var cmd = new SqliteCommand("SELECT id FROM seasons WHERE label = @label", connection);
            cmd.Parameters.AddWithValue("@label", label);
            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
            {
                return null;
            }
            return Convert.ToInt64(result);
        }

        private long? GetCompetitionIdByUid(SqliteConnection connection, string uid)
        {
            using var cmd = new SqliteCommand("SELECT id FROM competitions WHERE uid = @uid", connection);
            cmd.Parameters.AddWithValue("@uid", uid);
            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
            {
                return null;
            }
            return Convert.ToInt64(result);
        }

        private long? GetClubIdByUid(SqliteConnection connection, string uid)
        {
            using var cmd = new SqliteCommand("SELECT id FROM clubs WHERE uid = @uid", connection);
            cmd.Parameters.AddWithValue("@uid", uid);
            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
            {
                return null;
            }
            return Convert.ToInt64(result);
        }

        private static object ToDbValue(object? value)
        {
            if (value == null)
            {
                return DBNull.Value;
            }
            return value;
        }
    }
}