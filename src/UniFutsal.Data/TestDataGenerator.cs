using System;
using System.Collections.Generic;
using System.IO;

namespace UniFutsal.Data
{
    /// <summary>
    /// Genera CSVs de prueba con datos plausibles para M1.
    /// </summary>
    public static class TestDataGenerator
    {
        private static readonly Random Rng = new Random(42);

        private static readonly string[] FirstNames = {
            "Javier", "Carlos", "Miguel", "David", "Sergio", "Antonio", "Jesús", "Alberto",
            "Raúl", "Fernando", "Daniel", "Alejandro", "Pablo", "Marcos", "Iván", "Rubén"
        };

        private static readonly string[] LastNames = {
            "García", "Rodríguez", "Martínez", "López", "González", "Hernández", "Pérez", "Sánchez",
            "Ramírez", "Torres", "Flores", "Rivera", "Gómez", "Díaz", "Reyes", "Morales"
        };

        private static readonly string[] CommonNames = {
            "Kike", "Falcão", "Chino", "Pola", "Bebe", "Raúl", "Jesús", "Carlitos"
        };

        private static readonly string[] ClubUids = {
            "club-madrid-fs", "club-sevilla-fs", "club-zaragoza-fs", "club-valencia-fs",
            "club-guadalajara-fs", "club-cartagena-fs", "club-burela-fs", "club-noia-fs"
        };

        public static void Generate(string outputFolder)
        {
            Directory.CreateDirectory(outputFolder);

            GeneratePeopleCsv(Path.Combine(outputFolder, "people.csv"));
            GenerateContractsCsv(Path.Combine(outputFolder, "contracts.csv"));
            GenerateSeasonsCsv(Path.Combine(outputFolder, "seasons.csv"));
            GenerateCompetitionsCsv(Path.Combine(outputFolder, "competitions.csv"));
            GenerateEntriesCsv(Path.Combine(outputFolder, "entries.csv"));

            Console.WriteLine($"✅ Datos de prueba generados en: {outputFolder}");
            Console.WriteLine($"   📄 people.csv (108 personas)");
            Console.WriteLine($"   📄 contracts.csv (104 contratos)");
            Console.WriteLine($"   📄 seasons.csv (1 temporada)");
            Console.WriteLine($"   📄 competitions.csv (1 liga)");
            Console.WriteLine($"   📄 entries.csv (8 inscripciones)");
        }

        private static void GenerateSeasonsCsv(string path)
        {
            var lines = new List<string>();
            lines.Add("label,start_date,end_date");
            lines.Add("2026/27,2026-08-15,2027-06-15");
            File.WriteAllLines(path, lines);
        }

        private static void GenerateCompetitionsCsv(string path)
        {
            var lines = new List<string>();
            lines.Add("uid,name,short_name,scope,type,country_code3,level,prestige,active");
            lines.Add("comp-lnfs-primera,LNFS Primera División,LNFS 1ª,club,liga,ESP,1,85,1");
            File.WriteAllLines(path, lines);
        }

        private static void GenerateEntriesCsv(string path)
        {
            var lines = new List<string>();
            lines.Add("season_label,competition_uid,club_uid,status");
            foreach (var clubUid in ClubUids)
            {
                lines.Add($"2026/27,comp-lnfs-primera,{clubUid},activo");
            }
            File.WriteAllLines(path, lines);
        }

        private static void GeneratePeopleCsv(string path)
        {
            var header = new List<string> {
                "uid", "first_name", "last_name", "common_name", "gender", "birth_date",
                "birth_city", "nationality_code3", "height_cm", "weight_kg", "source",
                "role_type", "position_main", "preferred_foot", "current_ability", "potential_ability",
                "t_control", "t_conduccion", "t_pase", "t_pase_un_toque", "t_finalizacion",
                "t_tiro_lejano", "t_regate", "t_poste", "t_entrada", "t_intercepcion", "t_bloqueo",
                "g_paradas", "g_reflejos", "g_uno_con_uno", "g_juego_pies", "g_distribucion",
                "g_posicionamiento", "g_salidas", "g_jugador",
                "m_vision", "m_decision", "m_anticipacion", "m_concentracion", "m_posicionamiento",
                "m_agresividad", "m_serenidad", "m_liderazgo", "m_equipo", "m_trabajo", "m_arrojo",
                "p_aceleracion", "p_velocidad", "p_agilidad", "p_equilibrio", "p_coordinacion",
                "p_resistencia", "p_fuerza", "p_salto",
                "h_consistencia", "h_lesiones", "h_juego_duro", "h_temperamento",
                "staff_role", "ent_tecnica", "ent_ofensiva", "ent_defensiva", "ent_porteros",
                "ent_fisica", "ent_tactica", "medicina", "h_juicio_habilidad", "h_juicio_potencial",
                "motivacion", "gestion_vestuario", "negociacion", "adaptabilidad",
                "referee_country", "strictness", "big_match_rating"
            };

            var lines = new List<string> { string.Join(",", header) };

            for (int clubIndex = 0; clubIndex < ClubUids.Length; clubIndex++)
            {
                var clubUid = ClubUids[clubIndex];
                int baseCa = 120 - (clubIndex * 5);

                for (int i = 0; i < 12; i++)
                {
                    bool isGoalkeeper = (i < 2);
                    string position = isGoalkeeper ? "POR" : RandomChoice("CIE", "ALI", "ALD", "PIV", "UNI");
                    int ca = Math.Max(40, Math.Min(180, baseCa + Rng.Next(-15, 16)));
                    int pa = Math.Max(ca, Math.Min(200, ca + Rng.Next(0, 31)));

                    string uid = $"person-{clubUid}-{i + 1:000}";
                    string first = RandomChoice(FirstNames);
                    string last = RandomChoice(LastNames);
                    string common = Rng.NextDouble() < 0.3 ? RandomChoice(CommonNames) : "";
                    int birthYear = Rng.Next(1990, 2006);
                    string birthDate = $"{birthYear}-{Rng.Next(1, 13):00}-{Rng.Next(1, 29):00}";
                    int height = isGoalkeeper ? Rng.Next(180, 201) : Rng.Next(170, 196);
                    int weight = Rng.Next(65, 86);
                    string foot = RandomChoice("D", "I", "AM");

                    var row = new List<string> {
                        uid, first, last, common, "M", birthDate, "", "ESP",
                        height.ToString(), weight.ToString(), "import", "player",
                        position, foot, ca.ToString(), pa.ToString()
                    };

                    // Atributos técnicos (11)
                    for (int j = 0; j < 11; j++)
                        row.Add(AttributeValue(isGoalkeeper ? Rng.Next(3, 9) : ca / 10 + Rng.Next(-3, 4)));
                    // Atributos de portero (8)
                    for (int j = 0; j < 8; j++)
                        row.Add(AttributeValue(isGoalkeeper ? ca / 10 + Rng.Next(-2, 5) : 1));
                    // Atributos mentales (11)
                    for (int j = 0; j < 11; j++)
                        row.Add(AttributeValue(isGoalkeeper ? Rng.Next(5, 13) : ca / 10 + Rng.Next(-3, 4)));
                    // Atributos físicos (8)
                    for (int j = 0; j < 8; j++)
                        row.Add(AttributeValue(isGoalkeeper ? Rng.Next(5, 13) : ca / 10 + Rng.Next(-3, 4)));
                    // Atributos ocultos (4)
                    for (int j = 0; j < 4; j++)
                        row.Add(Rng.Next(8, 16).ToString());
                    // Staff fields vacíos (14)
                    for (int j = 0; j < 14; j++) row.Add("");
                    // Referee fields vacíos (3)
                    for (int j = 0; j < 3; j++) row.Add("");

                    lines.Add(string.Join(",", row));
                }

                // Entrenador
                string coachUid = $"person-{clubUid}-coach";
                var coachRow = new List<string> {
                    coachUid, RandomChoice(FirstNames), RandomChoice(LastNames), "", "M",
                    $"{Rng.Next(1970, 1991)}-01-15", "", "ESP", "178", "75", "import", "staff",
                    "", "", "", ""
                };
                for (int j = 0; j < 42; j++) coachRow.Add("");
                coachRow.AddRange(new[] {
                    "entrenador",
                    Rng.Next(10, 19).ToString(), Rng.Next(10, 19).ToString(), Rng.Next(10, 19).ToString(),
                    Rng.Next(8, 16).ToString(), Rng.Next(10, 19).ToString(), Rng.Next(10, 19).ToString(),
                    Rng.Next(8, 16).ToString(), Rng.Next(10, 19).ToString(), Rng.Next(10, 19).ToString(),
                    Rng.Next(10, 19).ToString(), Rng.Next(10, 19).ToString(), Rng.Next(10, 19).ToString(),
                    Rng.Next(8, 16).ToString()
                });
                for (int j = 0; j < 3; j++) coachRow.Add("");
                lines.Add(string.Join(",", coachRow));
            }

            // 4 árbitros
            for (int i = 0; i < 4; i++)
            {
                string uid = $"person-referee-{i + 1:000}";
                var refRow = new List<string> {
                    uid, RandomChoice(FirstNames), RandomChoice(LastNames), "", "M",
                    $"{Rng.Next(1980, 1996)}-05-10", "", "ESP", "180", "78", "import", "referee",
                    "", "", "", ""
                };
                for (int j = 0; j < 56; j++) refRow.Add("");
                refRow.AddRange(new[] { "ESP", Rng.Next(8, 19).ToString(), Rng.Next(40, 81).ToString() });
                lines.Add(string.Join(",", refRow));
            }

            File.WriteAllLines(path, lines);
        }

        private static void GenerateContractsCsv(string path)
        {
            var header = "person_uid,club_uid,scope,signed_on,effective_from,effective_until,wage_monthly,release_clause,squad_number";
            var lines = new List<string> { header };

            for (int clubIndex = 0; clubIndex < ClubUids.Length; clubIndex++)
            {
                var clubUid = ClubUids[clubIndex];
                int baseCa = 120 - (clubIndex * 5);

                for (int i = 0; i < 12; i++)
                {
                    int ca = Math.Max(40, Math.Min(180, baseCa + Rng.Next(-15, 16)));
                    string uid = $"person-{clubUid}-{i + 1:000}";
                    int wage = 2000 + (ca * 30) + Rng.Next(-500, 501);
                    int release = Rng.NextDouble() < 0.7 ? wage * 60 : 0;
                    lines.Add($"{uid},{clubUid},primer_equipo,2026-07-01,2026-07-01,2028-06-30,{wage},{release},{i + 1}");
                }

                string coachUid = $"person-{clubUid}-coach";
                int coachWage = 5000 + Rng.Next(-1000, 3001);
                lines.Add($"{coachUid},{clubUid},staff,2026-07-01,2026-07-01,2028-06-30,{coachWage},0,");
            }

            File.WriteAllLines(path, lines);
        }

        private static string RandomChoice(params string[] options)
        {
            return options[Rng.Next(options.Length)];
        }

        private static string AttributeValue(int value)
        {
            return Math.Max(1, Math.Min(20, value)).ToString();
        }
    }
}