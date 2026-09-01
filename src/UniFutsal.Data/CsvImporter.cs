using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace UniFutsal.Data
{
    public class ImportResult
    {
        public int TotalRows { get; set; }
        public int Imported { get; set; }
        public int Skipped { get; set; }
        public int Errors { get; set; }
        public List<string> ErrorMessages { get; set; } = new List<string>();
    }

    public class CsvImporter
    {
        private readonly string _dbPath;

        public CsvImporter(string dbPath)
        {
            _dbPath = dbPath;
        }

        public ImportResult ImportCountries(string csvPath)
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

            // Leer el header para mapear columnas por nombre
            var header = ParseCsvLine(lines[0]);

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            // Habilitar foreign keys para esta conexión
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
                    var fields = ParseCsvLine(line);
                    var uid = GetField(header, fields, "uid");
                    var name = GetField(header, fields, "name");
                    var code3 = GetField(header, fields, "code3");
                    var confCode = GetField(header, fields, "confederation_code");
                    var confName = GetField(header, fields, "confederation_name");
                    var reputationStr = GetField(header, fields, "futsal_reputation");

                    // Validar code3 obligatorio (según 03-datos.md §8)
                    if (string.IsNullOrWhiteSpace(code3))
                    {
                        result.Skipped++;
                        result.ErrorMessages.Add($"Fila {i + 1}: code3 obligatorio vacío.");
                        continue;
                    }

                    // Validar name obligatorio
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        result.Skipped++;
                        result.ErrorMessages.Add($"Fila {i + 1}: name obligatorio vacío.");
                        continue;
                    }

                    // Generar uid si no se proporciona
                    if (string.IsNullOrWhiteSpace(uid))
                    {
                        uid = $"country-{code3.ToLowerInvariant()}";
                    }

                    // Parsear reputación con InvariantCulture (determinismo, Plan.md §10.1)
                    double reputation = 50.0;
                    if (!string.IsNullOrWhiteSpace(reputationStr))
                    {
                        if (!double.TryParse(reputationStr,
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out reputation))
                        {
                            reputation = 50.0;
                        }
                    }

                    // Buscar o crear confederación
                    long confederationId = GetOrCreateConfederation(connection, confCode, confName);

                    // Insertar país (INSERT OR IGNORE para idempotencia)
                    using var insertCmd = new SqliteCommand(
                        @"INSERT OR IGNORE INTO countries (uid, name, code3, confederation_id, futsal_reputation)
                          VALUES (@uid, @name, @code3, @conf_id, @rep)", connection);
                    insertCmd.Parameters.AddWithValue("@uid", uid);
                    insertCmd.Parameters.AddWithValue("@name", name);
                    insertCmd.Parameters.AddWithValue("@code3", code3.ToUpperInvariant());
                    insertCmd.Parameters.AddWithValue("@conf_id", confederationId);
                    insertCmd.Parameters.AddWithValue("@rep", reputation);

                    int affected = insertCmd.ExecuteNonQuery();
                    if (affected > 0)
                    {
                        result.Imported++;
                    }
                    else
                    {
                        result.Skipped++; // Ya existía (idempotencia)
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

        private long GetOrCreateConfederation(SqliteConnection connection, string code, string name)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("confederation_code no puede estar vacío.");
            }

            code = code.ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(name))
            {
                name = code; // Usar el código como nombre por defecto
            }

            // Intentar insertar (OR IGNORE si ya existe)
            using var insertCmd = new SqliteCommand(
                "INSERT OR IGNORE INTO confederations (code, name) VALUES (@code, @name)", connection);
            insertCmd.Parameters.AddWithValue("@code", code);
            insertCmd.Parameters.AddWithValue("@name", name);
            insertCmd.ExecuteNonQuery();

            // Obtener el id de la confederación
            using var selectCmd = new SqliteCommand(
                "SELECT id FROM confederations WHERE code = @code", connection);
            selectCmd.Parameters.AddWithValue("@code", code);
            var result = selectCmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
            {
                throw new InvalidOperationException($"No se pudo obtener el id de la confederación '{code}'.");
            }
            return Convert.ToInt64(result);
        }

        private string[] ParseCsvLine(string line)
        {
            // Parseo simple para M0 (sin soporte de comillas/comas dentro de campos).
            // Si más adelante necesitamos parseo complejo, añadiremos CsvHelper
            // y lo registraremos en DECISIONS.md (Plan.md regla 6).
            return line.Split(',');
        }

        private string GetField(string[] header, string[] fields, string columnName)
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