using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Data.Sqlite;

namespace UniFutsal.Data
{
    public static class DatabaseInitializer
    {
        public static void Initialize(string dbPath, string migrationsFolder)
        {
            Console.WriteLine($"📦 Inicializando base de datos en: {dbPath}");
            
            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var sqlScriptPath = Path.Combine(migrationsFolder, "000_init.sql");
            if (!File.Exists(sqlScriptPath))
            {
                throw new FileNotFoundException($"No se encontró el script de migración en: {sqlScriptPath}");
            }

            var sqlScript = File.ReadAllText(sqlScriptPath);

            // Si la BD ya existe, la borramos para empezar limpio
            if (File.Exists(dbPath))
            {
                try
                {
                    File.Delete(dbPath);
                }
                catch (IOException ex)
                {
                    throw new IOException(
                        $"No se pudo borrar '{dbPath}'. " +
                        $"Asegúrate de que DBeaver (u otro programa) no tenga la BD abierta. " +
                        $"Detalle: {ex.Message}", ex);
                }
            }

            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            // Dividir el script en statements individuales y ejecutarlos uno por uno
            var statements = SplitSqlStatements(sqlScript);
            int executed = 0;

            foreach (var statement in statements)
            {
                try
                {
                    using var command = new SqliteCommand(statement, connection);
                    command.ExecuteNonQuery();
                    executed++;
                }
                catch (SqliteException ex)
                {
                    Console.WriteLine($"⚠️ Error ejecutando statement: {ex.Message}");
                    Console.WriteLine($"   Statement: {Truncate(statement, 120)}");
                    throw;
                }
            }

            // Verificar que se crearon las tablas esperadas
            int tableCount = 0;
            using (var countCmd = new SqliteCommand(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';", connection))
            {
                var result = countCmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    tableCount = Convert.ToInt32(result);
                }
            }

            int viewCount = 0;
            using (var viewCmd = new SqliteCommand(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='view';", connection))
            {
                var result = viewCmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    viewCount = Convert.ToInt32(result);
                }
            }

            Console.WriteLine($"✅ Base de datos inicializada correctamente.");
            Console.WriteLine($"   {executed} statements ejecutados · {tableCount} tablas · {viewCount} vistas");
            
            if (tableCount < 55)
            {
                Console.WriteLine($"⚠️ AVISO: se esperaban ~59 tablas pero solo se crearon {tableCount}.");
                Console.WriteLine($"   El script DDL puede estar incompleto. Revisa data/migrations/000_init.sql");
            }
        }

        /// <summary>
        /// Divide un script SQL en statements individuales.
        /// Respeta comillas simples y comentarios de línea.
        /// </summary>
        private static List<string> SplitSqlStatements(string script)
        {
            var statements = new List<string>();
            var currentStatement = new StringBuilder();
            bool inSingleQuote = false;
            bool inLineComment = false;
            
            for (int i = 0; i < script.Length; i++)
            {
                char c = script[i];
                
                // Si estamos en un comentario de línea, buscar el fin de línea
                if (inLineComment)
                {
                    if (c == '\n')
                    {
                        inLineComment = false;
                        currentStatement.Append(' '); // Reemplazar comentario por espacio
                    }
                    continue;
                }
                
                // Detectar inicio de comentario de línea (--)
                if (!inSingleQuote && c == '-' && i + 1 < script.Length && script[i + 1] == '-')
                {
                    inLineComment = true;
                    i++; // Saltar el segundo '-'
                    continue;
                }
                
                // Manejar comillas simples
                if (c == '\'')
                {
                    // Verificar si es una comilla escapada ('')
                    if (inSingleQuote && i + 1 < script.Length && script[i + 1] == '\'')
                    {
                        currentStatement.Append(c);
                        currentStatement.Append(script[i + 1]);
                        i++; // Saltar la segunda comilla
                        continue;
                    }
                    inSingleQuote = !inSingleQuote;
                }
                
                // Dividir por ';' solo fuera de comillas y comentarios
                if (c == ';' && !inSingleQuote)
                {
                    var stmt = currentStatement.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(stmt))
                    {
                        statements.Add(stmt);
                    }
                    currentStatement.Clear();
                    continue;
                }
                
                currentStatement.Append(c);
            }
            
            // Último statement (si el script no termina con ';')
            var lastStmt = currentStatement.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(lastStmt))
            {
                statements.Add(lastStmt);
            }
            
            return statements;
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (value.Length <= maxLength) return value;
            return value.Substring(0, maxLength) + "...";
        }
    }
}