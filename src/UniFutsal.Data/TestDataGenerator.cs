using System;
using System.Collections.Generic;
using System.IO;

namespace UniFutsal.Data
{
    /// <summary>
    /// Genera CSVs de prueba con datos plausibles para M2.
    /// Incluye 1ª y 2ª división (16 clubes en total).
    /// </summary>
    public static class TestDataGenerator
    {
        private static readonly Random Rng = new Random(42);

        private static readonly string[] FirstNames = {
            "Javier", "Carlos", "Miguel", "David", "Sergio", "Antonio", "Jesús", "Alberto",
            "Raúl", "Fernando", "Daniel", "Alejandro", "Pablo", "Marcos", "Iván", "Rubén",
            "Adrián", "Óscar", "Mario", "Hugo", "Diego", "Álvaro", "Gonzalo", "Jorge"
        };

        private static readonly string[] LastNames = {
            "García", "Rodríguez", "Martínez", "López", "González", "Hernández", "Pérez", "Sánchez",
            "Ramírez", "Torres", "Flores", "Rivera", "Gómez", "Díaz", "Reyes", "Morales",
            "Cruz", "Ortiz", "Gutiérrez", "Chávez", "Ramos", "Vargas", "Castillo", "Jiménez"
        };

        private static readonly string[] CommonNames = {
            "Kike", "Falcão", "Chino", "Pola", "Bebe", "Raúl", "Jesús", "Carlitos",
            "Catela", "Mellado", "Juanjo", "Sergio", "Dario", "Pablo", "Juanpi", "Gadeia"
        };

        // 8 clubes de 1ª
        private static readonly string[] FirstDivClubUids = {
            "club-madrid-fs", "club-sevilla-fs", "club-zaragoza-fs", "club-valencia-fs",
            "club-guadalajara-fs", "club-cartagena-fs", "club-burela-fs", "club-noia-fs"
        };

        // 8 clubes de 2ª
        private static readonly string[] SecondDivClubUids = {
            "club-ourense-fs", "club-santiago-fs", "club-vigo-fs", "club-coruna-fs",
            "club-gijon-fs", "club-oviedo-fs", "club-bilbao-fs", "club-donosti-fs"
        };

        private static readonly string[] Positions = { "POR", "CIE", "ALI", "ALD", "PIV", "UNI" };
        private static readonly string[] Feet = { "D", "I", "AM" };

        public static void Generate(string outputFolder)
        {
            Directory.CreateDirectory(outputFolder);

            GeneratePeopleCsv(Path.Combine(outputFolder, "people.csv"));
            GenerateContractsCsv(Path.Combine(outputFolder, "contracts.csv"));
            GenerateSeasonsCsv(Path.Combine(outputFolder, "seasons.csv"));
            GenerateCompetitionsCsv(Path.Combine(outputFolder, "competitions.csv"));
            GenerateEntriesCsv(Path.Combine(outputFolder, "entries.csv"));
            GenerateCompetitionLinksCsv(Path.Combine(outputFolder, "competition_links.csv"));

            Console.WriteLine($"✅ Datos de prueba generados en: {outputFolder}");
            Console.WriteLine($"   📄 people.csv (208 personas)");
            Console.WriteLine($"   📄 contracts.csv (208 contratos)");
            Console.WriteLine($"   📄 seasons.csv (1 temporada)");
            Console.WriteLine($"   📄 competitions.csv (2 competiciones: 1ª y 2ª)");
            Console.WriteLine($"   📄 entries.csv (16 inscripciones)");
            Console.WriteLine($"   📄 competition_links.csv (2 enlaces: ascenso y descenso)");
        }

        private static void GenerateSeasonsCsv(string path)
        {
            var lines = new List<string> { "label,start_date,end_date" };
            lines.Add("2026/27,2026-08-15,2027-06-15");
            File.WriteAllLines(path, lines);
        }

        private static void GenerateCompetitionsCsv(string path)
        {
            var lines = new List<string> { "uid,name,short_name,scope,type,country_code3,level,prestige,active" };
            lines.Add("comp-lnfs-primera,LNFS Primera División,LNFS 1ª,club,liga,ESP,1,85,1");
            lines.Add("comp-lnfs-segunda,LNFS Segunda División,LNFS 2ª,club,liga,ESP,2,60,1");
            File.WriteAllLines(path, lines);
        }

                private static void GenerateCompetitionLinksCsv(string path)
        {
            var lines = new List<string> { "from_competition_uid,to_competition_uid,link_type,slots,priority" };
            // Descenso: los 2 últimos de 1ª bajan a 2ª
            lines.Add("comp-lnfs-primera,comp-lnfs-segunda,descenso,2,1");
            // Ascenso: los 2 primeros de 2ª suben a 1ª
            lines.Add("comp-lnfs-segunda,comp-lnfs-primera,ascenso,2,1");
            File.WriteAllLines(path, lines);
        }

        private static void GenerateEntriesCsv(string path)
        {
            var lines = new List<string> { "season_label,competition_uid,club_uid,status" };
            foreach (var clubUid in FirstDivClubUids)
            {
                lines.Add($"2026/27,comp-lnfs-primera,{clubUid},activo");
            }
            foreach (var clubUid in SecondDivClubUids)
            {
                lines.Add($"2026/27,comp-lnfs-segunda,{clubUid},activo");
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

            // Generar jugadores para TODOS los clubes (1ª + 2ª)
            var allClubUids = new List<string>();
            allClubUids.AddRange(FirstDivClubUids);
            allClubUids.AddRange(SecondDivClubUids);

            for (int clubIndex = 0; clubIndex < allClubUids.Count; clubIndex++)
            {
                var clubUid = allClubUids[clubIndex];
                // 1ª: CA base 120-85 · 2ª: CA base 80-50
                int baseCa = clubIndex < 8 ? 120 - (clubIndex * 5) : 80 - ((clubIndex - 8) * 5);

                // 12 jugadores por club
                for (int i = 0; i < 12; i++)
                {
                    bool isGoalkeeper = (i < 2);
                    string position = isGoalkeeper ? "POR" : Positions[Rng.Next(Positions.Length)];
                    int ca = Math.Max(30, Math.Min(180, baseCa + Rng.Next(-15, 16)));
                    int pa = Math.Max(ca, Math.Min(200, ca + Rng.Next(0, 31)));

                    string uid = $"person-{clubUid}-{i + 1:000}";
                    string first = FirstNames[Rng.Next(FirstNames.Length)];
                    string last = LastNames[Rng.Next(LastNames.Length)];
                    string common = Rng.NextDouble() < 0.3 ? CommonNames[Rng.Next(CommonNames.Length)] : "";
                    int birthYear = Rng.Next(1990, 2006);
                    string birthDate = $"{birthYear}-{Rng.Next(1, 13):00}-{Rng.Next(1, 29):00}";
                    int height = isGoalkeeper ? Rng.Next(180, 201) : Rng.Next(170, 196);
                    int weight = Rng.Next(65, 86);
                    string foot = Feet[Rng.Next(Feet.Length)];

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

                // 1 entrenador por club
                string coachUid = $"person-{clubUid}-coach";
                var coachRow = new List<string> {
                    coachUid, FirstNames[Rng.Next(FirstNames.Length)], LastNames[Rng.Next(LastNames.Length)], "", "M",
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
                    uid, FirstNames[Rng.Next(FirstNames.Length)], LastNames[Rng.Next(LastNames.Length)], "", "M",
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

            var allClubUids = new List<string>();
            allClubUids.AddRange(FirstDivClubUids);
            allClubUids.AddRange(SecondDivClubUids);

            for (int clubIndex = 0; clubIndex < allClubUids.Count; clubIndex++)
            {
                var clubUid = allClubUids[clubIndex];
                int baseCa = clubIndex < 8 ? 120 - (clubIndex * 5) : 80 - ((clubIndex - 8) * 5);

                // 12 jugadores por club
                for (int i = 0; i < 12; i++)
                {
                    int ca = Math.Max(30, Math.Min(180, baseCa + Rng.Next(-15, 16)));
                    string uid = $"person-{clubUid}-{i + 1:000}";
                    int wage = 1500 + (ca * 25) + Rng.Next(-300, 301);
                    int release = Rng.NextDouble() < 0.7 ? wage * 50 : 0;
                    int durationYears = GetRandomContractDuration();
                    string effectiveUntil = ComputeContractEndDate(2026, durationYears);
                    lines.Add($"{uid},{clubUid},primer_equipo,2026-07-01,2026-07-01,{effectiveUntil},{wage},{release},{i + 1}");
                }

                // 1 entrenador por club
                string coachUid = $"person-{clubUid}-coach";
                int coachWage = 3500 + Rng.Next(-800, 2001);
                int coachDuration = GetRandomContractDuration();
                string coachUntil = ComputeContractEndDate(2026, coachDuration);
                lines.Add($"{coachUid},{clubUid},staff,2026-07-01,2026-07-01,{coachUntil},{coachWage},0,");
            }

            File.WriteAllLines(path, lines);
        }

        /// <summary>
        /// Distribución de duraciones: 30% 1 año, 30% 2 años, 20% 3 años, 15% 4 años, 5% 5 años.
        /// </summary>
        private static int GetRandomContractDuration()
        {
            double roll = Rng.NextDouble();
            if (roll < 0.30) return 1;
            if (roll < 0.60) return 2;
            if (roll < 0.80) return 3;
            if (roll < 0.95) return 4;
            return 5;
        }

        private static string ComputeContractEndDate(int startYear, int durationYears)
        {
            int endYear = startYear + durationYears;
            return $"{endYear}-06-30";
        }

        private static string AttributeValue(int value)
        {
            return Math.Max(1, Math.Min(20, value)).ToString();
        }
    }
}