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
                case "generate-test-data":
                    return HandleGenerateTestData(args);
                case "generate-calendar":
                    return HandleGenerateCalendar(args);
                case "load-world":
                    return HandleLoadWorld(args);
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
            Console.WriteLine("  init-db --out <ruta>              Crea una nueva base de datos.");
            Console.WriteLine("  validate --db <ruta>              Ejecuta las 7 queries de validación.");
            Console.WriteLine("  import --csv <ruta> --db <ruta> --type <tipo>   Importa un CSV.");
            Console.WriteLine("  generate-test-data --out <ruta>   Genera CSVs de prueba.");
            Console.WriteLine("  generate-calendar --db <ruta> --competition <uid> --season <label>");
            Console.WriteLine("  load-world --db <ruta>            Carga el mundo y muestra un resumen.");
            Console.WriteLine("\nTipos de importación:");
            Console.WriteLine("  countries, venues, clubs, people, contracts, seasons, competitions, entries");
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
            string importType = "countries";

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "--csv" && i + 1 < args.Length) { csvPath = args[i + 1]; i++; }
                else if (args[i] == "--db" && i + 1 < args.Length) { dbPath = args[i + 1]; i++; }
                else if (args[i] == "--type" && i + 1 < args.Length) { importType = args[i + 1].ToLowerInvariant(); i++; }
            }

            if (string.IsNullOrEmpty(csvPath))
            {
                Console.WriteLine("❌ Error: debes especificar --csv <ruta>");
                return 1;
            }

            try
            {
                Console.WriteLine($"📥 Importando {importType} desde: {csvPath}");
                Console.WriteLine($"   Base de datos: {dbPath}\n");

                ImportResult result;
                switch (importType)
                {
                    case "countries": result = new CsvImporter(dbPath).ImportCountries(csvPath); break;
                    case "venues": result = new VenueImporter(dbPath).Import(csvPath); break;
                    case "clubs": result = new ClubImporter(dbPath).Import(csvPath); break;
                    case "people": result = new PeopleImporter(dbPath).Import(csvPath); break;
                    case "contracts": result = new ContractImporter(dbPath).Import(csvPath); break;
                    case "seasons": result = new CompetitionImporter(dbPath).ImportSeasons(csvPath); break;
                    case "competitions": result = new CompetitionImporter(dbPath).ImportCompetitions(csvPath); break;
                    case "entries": result = new CompetitionImporter(dbPath).ImportEntries(csvPath); break;
                    default:
                        Console.WriteLine($"⚠️ Tipo de importación no soportado: '{importType}'");
                        return 1;
                }

                Console.WriteLine($"📊 Resultado de la importación:");
                Console.WriteLine($"   Filas totales: {result.TotalRows}");
                Console.WriteLine($"   Importados:    {result.Imported}");
                Console.WriteLine($"   Saltados:      {result.Skipped}");
                Console.WriteLine($"   Errores:       {result.Errors}");

                if (result.ErrorMessages.Count > 0)
                {
                    Console.WriteLine("\n⚠️ Detalles:");
                    foreach (var msg in result.ErrorMessages) Console.WriteLine($"   {msg}");
                }

                Console.WriteLine();
                if (result.Errors > 0) { Console.WriteLine("❌ IMPORTACIÓN CON ERRORES."); return 1; }
                else { Console.WriteLine("✅ IMPORTACIÓN COMPLETADA."); return 0; }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fatal en la importación: {ex.Message}");
                return 1;
            }
        }

        static int HandleGenerateTestData(string[] args)
        {
            string outputPath = "data/csv";

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "--out" && i + 1 < args.Length) { outputPath = args[i + 1]; i++; }
            }

            try
            {
                Console.WriteLine($"🎲 Generando datos de prueba en: {outputPath}\n");
                TestDataGenerator.Generate(outputPath);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al generar datos: {ex.Message}");
                return 1;
            }
        }

        static int HandleGenerateCalendar(string[] args)
        {
            string dbPath = "saves/unifutsal_base.db";
            string competitionUid = "";
            string seasonLabel = "";

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "--db" && i + 1 < args.Length) { dbPath = args[i + 1]; i++; }
                else if (args[i] == "--competition" && i + 1 < args.Length) { competitionUid = args[i + 1]; i++; }
                else if (args[i] == "--season" && i + 1 < args.Length) { seasonLabel = args[i + 1]; i++; }
            }

            if (string.IsNullOrEmpty(competitionUid) || string.IsNullOrEmpty(seasonLabel))
            {
                Console.WriteLine("❌ Error: debes especificar --competition <uid> y --season <label>");
                return 1;
            }

            try
            {
                Console.WriteLine($"📅 Generando calendario para {competitionUid} ({seasonLabel})...");
                var generator = new CalendarGenerator(dbPath);
                int matches = generator.Generate(competitionUid, seasonLabel);
                Console.WriteLine($"✅ Calendario generado: {matches} partidos creados.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al generar calendario: {ex.Message}");
                return 1;
            }
        }

        static int HandleLoadWorld(string[] args)
        {
            string dbPath = "saves/unifutsal_base.db";

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "--db" && i + 1 < args.Length) { dbPath = args[i + 1]; i++; }
            }

            try
            {
                Console.WriteLine($"🌍 Cargando mundo desde: {dbPath}\n");

                var loader = new WorldLoader(dbPath);
                var world = loader.Load();

                Console.WriteLine("📊 Resumen del mundo cargado:");
                Console.WriteLine($"   🌐 Confederaciones:  {world.Confederations.Count}");
                Console.WriteLine($"   🏳️ Países:           {world.Countries.Count}");
                Console.WriteLine($"   🏟️ Pabellones:       {world.Venues.Count}");
                Console.WriteLine($"   👤 Personas:         {world.Persons.Count}");
                Console.WriteLine($"   ⚽ Jugadores:        {world.Players.Count}");
                Console.WriteLine($"   🏢 Clubes:           {world.Clubs.Count}");
                Console.WriteLine($"   📄 Contratos:        {world.Contracts.Count}");
                Console.WriteLine($"   📅 Temporadas:       {world.Seasons.Count}");
                Console.WriteLine($"   🏆 Competiciones:    {world.Competitions.Count}");
                Console.WriteLine($"   📋 Inscripciones:    {world.CompetitionEntries.Count}");
                Console.WriteLine();
                Console.WriteLine($"   📆 Fecha del mundo:  {world.WorldDate}");
                Console.WriteLine($"   🌱 Seed del mundo:   {world.WorldSeed}");

                // Mostrar detalle por club
                if (world.Clubs.Count > 0)
                {
                    Console.WriteLine("\n🏢 Detalle por club:");
                    foreach (var club in world.Clubs)
                    {
                        var players = world.GetPlayersByClub(club.Id);
                        var venueName = club.Venue != null ? club.Venue.Name : "(sin pabellón)";
                        Console.WriteLine($"   {club.Name} ({club.Uid}): {players.Count} jugadores · {venueName}");
                    }
                }

                // Mostrar detalle por competición
                if (world.Competitions.Count > 0)
                {
                    Console.WriteLine("\n🏆 Detalle por competición:");
                    foreach (var comp in world.Competitions)
                    {
                        var entries = world.GetActiveEntriesByCompetition(comp.Id);
                        Console.WriteLine($"   {comp.Name} ({comp.Uid}): {entries.Count} equipos inscritos");
                    }
                }

                Console.WriteLine("\n✅ Mundo cargado correctamente.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al cargar el mundo: {ex.Message}");
                return 1;
            }
        }
    }
}