using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace UniFutsal.Data
{
    public class ContractImporter
    {
        private readonly string _dbPath;

        public ContractImporter(string dbPath)
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
                result.ErrorMessages.Add("El CSV no tiene datos.");
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
                    var personUid = CsvHelper.GetField(header, fields, "person_uid");
                    var clubUid = CsvHelper.GetField(header, fields, "club_uid");
                    var scope = CsvHelper.GetField(header, fields, "scope");
                    var signedOn = CsvHelper.GetField(header, fields, "signed_on");
                    var effectiveFrom = CsvHelper.GetField(header, fields, "effective_from");
                    var effectiveUntil = CsvHelper.GetField(header, fields, "effective_until");
                    var wageStr = CsvHelper.GetField(header, fields, "wage_monthly");
                    var releaseStr = CsvHelper.GetField(header, fields, "release_clause");
                    var squadNumStr = CsvHelper.GetField(header, fields, "squad_number");

                    // Buscar person_id y club_id
                    long? personId = GetPersonIdByUid(connection, personUid);
                    long? clubId = GetClubIdByUid(connection, clubUid);

                    if (personId == null || clubId == null)
                    {
                        result.Skipped++;
                        continue;
                    }

                    int wage = 0;
                    int wageTemp;
                    if (int.TryParse(wageStr, out wageTemp))
                    {
                        wage = wageTemp;
                    }

                    int? release = null;
                    int releaseTemp;
                    if (int.TryParse(releaseStr, out releaseTemp))
                    {
                        release = releaseTemp;
                    }

                    int? squadNum = null;
                    int squadTemp;
                    if (int.TryParse(squadNumStr, out squadTemp))
                    {
                        squadNum = squadTemp;
                    }

                    using var cmd = new SqliteCommand(
                        @"INSERT OR IGNORE INTO contracts (person_id, club_id, scope, signed_on, effective_from,
                          effective_until, wage_monthly, release_clause, squad_number, status)
                          VALUES (@person_id, @club_id, @scope, @signed_on, @effective_from,
                          @effective_until, @wage, @release, @squad_num, 'vigente')", connection);

                    cmd.Parameters.AddWithValue("@person_id", personId.Value);
                    cmd.Parameters.AddWithValue("@club_id", clubId.Value);
                    cmd.Parameters.AddWithValue("@scope", scope);
                    cmd.Parameters.AddWithValue("@signed_on", signedOn);
                    cmd.Parameters.AddWithValue("@effective_from", effectiveFrom);
                    cmd.Parameters.AddWithValue("@effective_until", effectiveUntil);
                    cmd.Parameters.AddWithValue("@wage", wage);
                    cmd.Parameters.AddWithValue("@release", ToDbValue(release));
                    cmd.Parameters.AddWithValue("@squad_num", ToDbValue(squadNum));

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

        private long? GetPersonIdByUid(SqliteConnection connection, string uid)
        {
            using var cmd = new SqliteCommand("SELECT id FROM persons WHERE uid = @uid", connection);
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

        /// <summary>
        /// Convierte un valor C# a valor de base de datos.
        /// Si es null, devuelve DBNull.Value.
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