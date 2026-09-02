using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace UniFutsal.Data
{
    public class ClubImporter
    {
        private readonly string _dbPath;

        public ClubImporter(string dbPath)
        {
            _dbPath = dbPath;
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
                result.ErrorMessages.Add("El CSV no tiene datos (solo header o está vacío).");
                return result;
            }

            var header = CsvHelper.ParseLine(lines[0]);

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            using (var pragmaCmd = new SqliteCommand("PRAGMA foreign_keys = ON;", connection))
            {
                pragmaCmd.ExecuteNonQuery();
            }

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
                    var nickname = CsvHelper.GetField(header, fields, "nickname");
                    var countryCode3 = CsvHelper.GetField(header, fields, "country_code3");
                    var city = CsvHelper.GetField(header, fields, "city");
                    var foundedYearStr = CsvHelper.GetField(header, fields, "founded_year");
                    var primaryColor = CsvHelper.GetField(header, fields, "primary_color");
                    var secondaryColor = CsvHelper.GetField(header, fields, "secondary_color");
                    var kitPattern = CsvHelper.GetField(header, fields, "kit_pattern");
                    var reputationStr = CsvHelper.GetField(header, fields, "reputation");
                    var venueUid = CsvHelper.GetField(header, fields, "venue_uid");
                    var trainingStr = CsvHelper.GetField(header, fields, "training_facilities");
                    var youthStr = CsvHelper.GetField(header, fields, "youth_facilities");
                    var recruitmentStr = CsvHelper.GetField(header, fields, "recruitment");
                    var physioStr = CsvHelper.GetField(header, fields, "physio_rating");
                    var bankStr = CsvHelper.GetField(header, fields, "bank_balance");
                    var debtStr = CsvHelper.GetField(header, fields, "debt");
                    var transferStr = CsvHelper.GetField(header, fields, "transfer_budget");
                    var wageStr = CsvHelper.GetField(header, fields, "wage_budget_monthly");
                    var isActiveStr = CsvHelper.GetField(header, fields, "is_active");

                    // Validaciones obligatorias
                    if (string.IsNullOrWhiteSpace(uid))
                    {
                        result.Skipped++;
                        result.ErrorMessages.Add($"Fila {i + 1}: uid obligatorio vacío.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        result.Skipped++;
                        result.ErrorMessages.Add($"Fila {i + 1}: name obligatorio vacío.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(countryCode3))
                    {
                        result.Skipped++;
                        result.ErrorMessages.Add($"Fila {i + 1}: country_code3 obligatorio vacío.");
                        continue;
                    }

                    // Buscar país
                    long? countryId = GetCountryIdByCode3(connection, countryCode3.ToUpperInvariant());
                    if (countryId == null)
                    {
                        result.Skipped++;
                        result.ErrorMessages.Add($"Fila {i + 1}: país '{countryCode3}' no encontrado.");
                        continue;
                    }

                    // Buscar venue (opcional)
                    long? venueId = null;
                    if (!string.IsNullOrWhiteSpace(venueUid))
                    {
                        venueId = GetVenueIdByUid(connection, venueUid);
                        if (venueId == null)
                        {
                            result.ErrorMessages.Add($"Fila {i + 1}: venue '{venueUid}' no encontrado (se asignará NULL).");
                        }
                    }

                    // Validar colores HEX (formato #RRGGBB)
                    primaryColor = ValidateHexColor(primaryColor, "#E63946");
                    secondaryColor = ValidateHexColor(secondaryColor, "#FFFFFF");

                    // Validar kit_pattern
                    kitPattern = ValidateKitPattern(kitPattern);

                    // Parsear números
                    double reputation = ParseDouble(reputationStr, 40.0);
                    int foundedYear = ParseInt(foundedYearStr, 0);
                    int training = Math.Clamp(ParseInt(trainingStr, 10), 1, 20);
                    int youth = Math.Clamp(ParseInt(youthStr, 10), 1, 20);
                    int recruitment = Math.Clamp(ParseInt(recruitmentStr, 10), 1, 20);
                    int physio = Math.Clamp(ParseInt(physioStr, 10), 1, 20);
                    int bank = ParseInt(bankStr, 0);
                    int debt = ParseInt(debtStr, 0);
                    int transfer = ParseInt(transferStr, 0);
                    int wage = ParseInt(wageStr, 0);
                    bool isActive = isActiveStr == "1" || isActiveStr.ToLowerInvariant() == "true";

                    // Insertar (usando ToDbValue para evitar problemas con null en C# 8)
                    using var insertCmd = new SqliteCommand(
                        @"INSERT OR IGNORE INTO clubs (uid, name, short_name, nickname, country_id, region_id, city, founded_year,
                          primary_color, secondary_color, kit_pattern, reputation, venue_id,
                          training_facilities, youth_facilities, recruitment, physio_rating,
                          bank_balance, debt, transfer_budget, wage_budget_monthly, is_active)
                          VALUES (@uid, @name, @short_name, @nickname, @country_id, NULL, @city, @founded_year,
                          @primary_color, @secondary_color, @kit_pattern, @reputation, @venue_id,
                          @training, @youth, @recruitment, @physio,
                          @bank, @debt, @transfer, @wage, @is_active)", connection);
                    insertCmd.Parameters.AddWithValue("@uid", uid);
                    insertCmd.Parameters.AddWithValue("@name", name);
                    insertCmd.Parameters.AddWithValue("@short_name", ToDbValue(shortName));
                    insertCmd.Parameters.AddWithValue("@nickname", ToDbValue(nickname));
                    insertCmd.Parameters.AddWithValue("@country_id", countryId.Value);
                    insertCmd.Parameters.AddWithValue("@city", ToDbValue(city));
                    insertCmd.Parameters.AddWithValue("@founded_year", ToDbValue(foundedYear > 0 ? (object)foundedYear : null));
                    insertCmd.Parameters.AddWithValue("@primary_color", primaryColor);
                    insertCmd.Parameters.AddWithValue("@secondary_color", secondaryColor);
                    insertCmd.Parameters.AddWithValue("@kit_pattern", kitPattern);
                    insertCmd.Parameters.AddWithValue("@reputation", reputation);
                    insertCmd.Parameters.AddWithValue("@venue_id", ToDbValue(venueId.HasValue ? (object)venueId.Value : null));
                    insertCmd.Parameters.AddWithValue("@training", training);
                    insertCmd.Parameters.AddWithValue("@youth", youth);
                    insertCmd.Parameters.AddWithValue("@recruitment", recruitment);
                    insertCmd.Parameters.AddWithValue("@physio", physio);
                    insertCmd.Parameters.AddWithValue("@bank", bank);
                    insertCmd.Parameters.AddWithValue("@debt", debt);
                    insertCmd.Parameters.AddWithValue("@transfer", transfer);
                    insertCmd.Parameters.AddWithValue("@wage", wage);
                    insertCmd.Parameters.AddWithValue("@is_active", isActive ? 1 : 0);

                    int affected = insertCmd.ExecuteNonQuery();
                    if (affected > 0)
                    {
                        result.Imported++;
                    }
                    else
                    {
                        result.Skipped++;
                    }
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
            using var cmd = new SqliteCommand(
                "SELECT id FROM countries WHERE code3 = @code3", connection);
            cmd.Parameters.AddWithValue("@code3", code3);
            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
            {
                return null;
            }
            return Convert.ToInt64(result);
        }

        private long? GetVenueIdByUid(SqliteConnection connection, string uid)
        {
            using var cmd = new SqliteCommand(
                "SELECT id FROM venues WHERE uid = @uid", connection);
            cmd.Parameters.AddWithValue("@uid", uid);
            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
            {
                return null;
            }
            return Convert.ToInt64(result);
        }

        private string ValidateHexColor(string color, string fallback)
        {
            if (string.IsNullOrWhiteSpace(color)) return fallback;
            color = color.Trim();
            if (color.Length != 7 || color[0] != '#') return fallback;
            for (int i = 1; i < 7; i++)
            {
                char c = color[i];
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                {
                    return fallback;
                }
            }
            return color;
        }

        private string ValidateKitPattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern)) return "solid";
            var valid = new[] { "solid", "stripes", "halved", "sash" };
            var lower = pattern.ToLowerInvariant();
            if (Array.IndexOf(valid, lower) >= 0)
            {
                return lower;
            }
            return "solid";
        }

        private double ParseDouble(string value, double fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            double result;
            if (double.TryParse(value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out result))
            {
                return result;
            }
            return fallback;
        }

        private int ParseInt(string value, int fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            int result;
            if (int.TryParse(value, out result))
            {
                return result;
            }
            return fallback;
        }

        /// <summary>
        /// Convierte un valor C# a valor de base de datos.
        /// Si es null, devuelve DBNull.Value.
        /// Esto evita problemas con el operador ternario en C# 8.
        /// </summary>
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