using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace UniFutsal.Data
{
    public class VenueImporter
    {
        private readonly string _dbPath;

        public VenueImporter(string dbPath)
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
                    var city = CsvHelper.GetField(header, fields, "city");
                    var countryCode3 = CsvHelper.GetField(header, fields, "country_code3");
                    var capacityStr = CsvHelper.GetField(header, fields, "capacity");
                    var surface = CsvHelper.GetField(header, fields, "surface");

                    // Validaciones
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

                    // Buscar país por code3
                    long? countryId = GetCountryIdByCode3(connection, countryCode3.ToUpperInvariant());
                    if (countryId == null)
                    {
                        result.Skipped++;
                        result.ErrorMessages.Add($"Fila {i + 1}: país '{countryCode3}' no encontrado.");
                        continue;
                    }

                    // Parsear capacity
                    int capacity = 1500;
                    if (!string.IsNullOrWhiteSpace(capacityStr))
                    {
                        if (!int.TryParse(capacityStr, out capacity))
                        {
                            capacity = 1500;
                        }
                        // Validar rango según schema
                        capacity = Math.Max(100, Math.Min(60000, capacity));
                    }

                    // Validar surface
                    if (string.IsNullOrWhiteSpace(surface))
                    {
                        surface = "parquet";
                    }
                    var validSurfaces = new[] { "parquet", "linoleum", "pvc", "taraflex" };
                    if (Array.IndexOf(validSurfaces, surface.ToLowerInvariant()) < 0)
                    {
                        surface = "parquet";
                    }

                    // Insertar (INSERT OR IGNORE para idempotencia)
                    using var insertCmd = new SqliteCommand(
                        @"INSERT OR IGNORE INTO venues (uid, name, city, country_id, capacity, surface)
                          VALUES (@uid, @name, @city, @country_id, @capacity, @surface)", connection);
                    insertCmd.Parameters.AddWithValue("@uid", uid);
                    insertCmd.Parameters.AddWithValue("@name", name);
                    insertCmd.Parameters.AddWithValue("@city", (object)city ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@country_id", countryId.Value);
                    insertCmd.Parameters.AddWithValue("@capacity", capacity);
                    insertCmd.Parameters.AddWithValue("@surface", surface.ToLowerInvariant());

                    int affected = insertCmd.ExecuteNonQuery();
                    if (affected > 0)
                    {
                        result.Imported++;
                    }
                    else
                    {
                        result.Skipped++; // Ya existía
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
    }

    /// <summary>
    /// Utilidades de parseo CSV compartidas.
    /// </summary>
    public static class CsvHelper
    {
        public static string[] ParseLine(string line)
        {
            return line.Split(',');
        }

        public static string GetField(string[] header, string[] fields, string columnName)
        {
            for (int i = 0; i < header.Length; i++)
            {
                if (header[i].Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return i < fields.Length ? fields[i].Trim() : "";
                }
            }
            return "";
        }
    }
}