using System;
using UniFutsal.Data;

namespace UniFutsal.Cli
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelp();
                return 1;
            }

            var command = args[0].ToLower();

            switch (command)
            {
                case "init-db":
                    return HandleInitDb(args);
                case "validate":
                    return HandleValidate(args);
                case "import":
                    return HandleImport(args);
                default:
                    Console.WriteLine($"⚠️ Comando desconocido: '{command}'");
                    PrintHelp();
                    return 1;
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine("🎮 UniFutsal Manager CLI v0.1.0");
            Console.WriteLine("Uso: unifutsal <comando> [opciones]");
            Console.WriteLine("\nComandos disponibles:");
            Console.WriteLine("  init-db --out <ruta>              Crea una nueva base de datos con el schema inicial.");
            Console.WriteLine("  validate --db <ruta>              Ejecuta las 7 queries de validación del mundo.");
            Console.WriteLine("  import --csv <ruta> --db <ruta>   Importa un CSV a la base de datos.");
        }

        static int HandleInitDb(string[] args)
        {
            string outPath = "saves/unifutsal_base.db";
            
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "--out" && i + 1 < args.Length)
                {
                    outPath = args[i + 1];
                    i++;
                }
            }

            try
            {
                DatabaseInitializer.Initialize(outPath, "data/migrations");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al inicializar la base de datos: {ex.Message}");
                return 1;
            }
        }

        static int HandleValidate(string[] args)
        {
            string dbPath = "saves/unifutsal_base.db";
            
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "--db" && i + 1 < args.Length)
                {
                    dbPath = args[i + 1];
                    i++;
                }
            }

            try
            {
                Console.WriteLine($"🔍 Validando mundo en: {dbPath}\n");
                
                var validator = new WorldValidator(dbPath);
                var results = validator.Validate();

                int totalProblems = 0;
                bool hasErrors = false;

                foreach (var result in results)
                {
                    if (result.ProblemCount == -1)
                    {
                        Console.WriteLine($"❌ {result.QueryName}: ERROR EN QUERY");
                        foreach (var detail in result.Details)
                            Console.WriteLine($"   {detail}");
                        hasErrors = true;
                    }
                    else if (result.Passed)
                    {
                        Console.WriteLine($"✅ {result.QueryName}: OK");
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ {result.QueryName}: {result.ProblemCount} problema(s)");
                        int shown = 0;
                        foreach (var detail in result.Details)
                        {
                            if (shown >= 5)
                            {
                                Console.WriteLine($"   ... y {result.Details.Count - 5} más");
                                break;
                            }
                            Console.WriteLine($"   {detail}");
                            shown++;
                        }
                        totalProblems += result.ProblemCount;
                    }
                }

                Console.WriteLine();
                if (hasErrors)
                {
                    Console.WriteLine("❌ VALIDACIÓN FALLIDA: hay errores en las queries.");
                    return 1;
                }
                else if (totalProblems > 0)
                {
                    Console.WriteLine($"⚠️ VALIDACIÓN CON AVISOS: {totalProblems} problema(s) encontrados.");
                    return 0;
                }
                else
                {
                    Console.WriteLine("✅ VALIDACIÓN COMPLETA: mundo limpio.");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al validar: {ex.Message}");
                return 1;
            }
        }

        static int HandleImport(string[] args)
        {
            string csvPath = "";
            string dbPath = "saves/unifutsal_base.db";

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "--csv" && i + 1 < args.Length)
                {
                    csvPath = args[i + 1];
                    i++;
                }
                else if (args[i] == "--db" && i + 1 < args.Length)
                {
                    dbPath = args[i + 1];
                    i++;
                }
            }

            if (string.IsNullOrEmpty(csvPath))
            {
                Console.WriteLine("❌ Error: debes especificar --csv <ruta>");
                Console.WriteLine("   Ejemplo: unifutsal import --csv data/csv/countries.csv --db saves/prueba.db");
                return 1;
            }

            try
            {
                Console.WriteLine($"📥 Importando CSV: {csvPath}");
                Console.WriteLine($"   Base de datos: {dbPath}\n");

                var importer = new CsvImporter(dbPath);
                var result = importer.ImportCountries(csvPath);

                Console.WriteLine($"📊 Resultado de la importación:");
                Console.WriteLine($"   Filas totales: {result.TotalRows}");
                Console.WriteLine($"   Importados:    {result.Imported}");
                Console.WriteLine($"   Saltados:      {result.Skipped}");
                Console.WriteLine($"   Errores:       {result.Errors}");

                if (result.ErrorMessages.Count > 0)
                {
                    Console.WriteLine("\n⚠️ Detalles:");
                    foreach (var msg in result.ErrorMessages)
                    {
                        Console.WriteLine($"   {msg}");
                    }
                }

                Console.WriteLine();
                if (result.Errors > 0)
                {
                    Console.WriteLine("❌ IMPORTACIÓN CON ERRORES.");
                    return 1;
                }
                else
                {
                    Console.WriteLine("✅ IMPORTACIÓN COMPLETADA.");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fatal en la importación: {ex.Message}");
                return 1;
            }
        }
    }
}