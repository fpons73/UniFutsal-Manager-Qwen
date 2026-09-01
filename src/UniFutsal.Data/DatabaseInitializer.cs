using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace UniFutsal.Data
{
    public static class DatabaseInitializer
    {
        public static void Initialize(string dbPath, string migrationsFolder)
        {
            Console.WriteLine($"📦 Inicializando base de datos en: {dbPath}");
            
            // Asegurar que la carpeta de destino existe (ej. "saves/")
            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Leer el script SQL completo
            var sqlScriptPath = Path.Combine(migrationsFolder, "000_init.sql");
            if (!File.Exists(sqlScriptPath))
            {
                throw new FileNotFoundException($"No se encontró el script de migración en: {sqlScriptPath}");
            }

            var sqlScript = File.ReadAllText(sqlScriptPath);

            // Conectar a la base de datos (se crea automáticamente si no existe)
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            // Ejecutar el script COMPLETO de una vez (SQLite soporta múltiples statements)
            using var command = new SqliteCommand(sqlScript, connection);
            command.ExecuteNonQuery();

            Console.WriteLine($"✅ Base de datos inicializada correctamente.");
        }
    }
}