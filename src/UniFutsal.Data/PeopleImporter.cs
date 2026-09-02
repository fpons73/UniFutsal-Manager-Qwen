using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace UniFutsal.Data
{
    public class PeopleImporter
    {
        private readonly string _dbPath;

        public PeopleImporter(string dbPath)
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
                    var uid = CsvHelper.GetField(header, fields, "uid");
                    var firstName = CsvHelper.GetField(header, fields, "first_name");
                    var lastName = CsvHelper.GetField(header, fields, "last_name");
                    var commonName = CsvHelper.GetField(header, fields, "common_name");
                    var gender = CsvHelper.GetField(header, fields, "gender");
                    var birthDate = CsvHelper.GetField(header, fields, "birth_date");
                    var nationalityCode3 = CsvHelper.GetField(header, fields, "nationality_code3");
                    var heightStr = CsvHelper.GetField(header, fields, "height_cm");
                    var weightStr = CsvHelper.GetField(header, fields, "weight_kg");
                    var source = CsvHelper.GetField(header, fields, "source");
                    var roleType = CsvHelper.GetField(header, fields, "role_type").ToLowerInvariant();

                    // Validaciones
                    if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                    {
                        result.Skipped++;
                        continue;
                    }

                    // Buscar país
                    long? nationalityId = GetCountryIdByCode3(connection, nationalityCode3);
                    if (nationalityId == null)
                    {
                        result.Skipped++;
                        result.ErrorMessages.Add($"Fila {i + 1}: país '{nationalityCode3}' no encontrado.");
                        continue;
                    }

                    // Parsear height y weight como nullable
                    object heightValue = DBNull.Value;
                    int heightTemp;
                    if (int.TryParse(heightStr, out heightTemp))
                    {
                        heightValue = heightTemp;
                    }

                    object weightValue = DBNull.Value;
                    int weightTemp;
                    if (int.TryParse(weightStr, out weightTemp))
                    {
                        weightValue = weightTemp;
                    }

                    // Insertar en persons
                    using var insertPerson = new SqliteCommand(
                        @"INSERT OR IGNORE INTO persons (uid, first_name, last_name, common_name, gender, birth_date,
                          nationality_id, height_cm, weight_kg, source)
                          VALUES (@uid, @first_name, @last_name, @common_name, @gender, @birth_date,
                          @nationality_id, @height_cm, @weight_kg, @source);
                          SELECT id FROM persons WHERE uid = @uid;", connection);
                    insertPerson.Parameters.AddWithValue("@uid", uid);
                    insertPerson.Parameters.AddWithValue("@first_name", firstName);
                    insertPerson.Parameters.AddWithValue("@last_name", lastName);
                    insertPerson.Parameters.AddWithValue("@common_name", ToDbValue(commonName));
                    insertPerson.Parameters.AddWithValue("@gender", gender);
                    insertPerson.Parameters.AddWithValue("@birth_date", birthDate);
                    insertPerson.Parameters.AddWithValue("@nationality_id", nationalityId.Value);
                    insertPerson.Parameters.AddWithValue("@height_cm", heightValue);
                    insertPerson.Parameters.AddWithValue("@weight_kg", weightValue);
                    insertPerson.Parameters.AddWithValue("@source", source);

                    var personIdResult = insertPerson.ExecuteScalar();
                    if (personIdResult == null || personIdResult == DBNull.Value)
                    {
                        result.Errors++;
                        result.ErrorMessages.Add($"Fila {i + 1}: no se pudo obtener el id de la persona '{uid}'.");
                        continue;
                    }
                    long personId = Convert.ToInt64(personIdResult);

                    // Insertar según role_type
                    if (roleType == "player")
                    {
                        InsertPlayer(connection, header, fields, personId);
                    }
                    else if (roleType == "staff")
                    {
                        InsertStaff(connection, header, fields, personId);
                    }
                    else if (roleType == "referee")
                    {
                        InsertReferee(connection, header, fields, personId);
                    }

                    result.Imported++;
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    result.ErrorMessages.Add($"Fila {i + 1}: {ex.Message}");
                }
            }

            return result;
        }

        private void InsertPlayer(SqliteConnection connection, string[] header, string[] fields, long personId)
        {
            var positionMain = CsvHelper.GetField(header, fields, "position_main");
            var preferredFoot = CsvHelper.GetField(header, fields, "preferred_foot");
            int ca = ParseInt(CsvHelper.GetField(header, fields, "current_ability"), 100);
            int pa = ParseInt(CsvHelper.GetField(header, fields, "potential_ability"), 100);

            using var cmd = new SqliteCommand(
                @"INSERT OR IGNORE INTO players (person_id, position_main, preferred_foot, current_ability, potential_ability,
                  t_control, t_conduccion, t_pase, t_pase_un_toque, t_finalizacion, t_tiro_lejano, t_regate, t_poste,
                  t_entrada, t_intercepcion, t_bloqueo,
                  g_paradas, g_reflejos, g_uno_con_uno, g_juego_pies, g_distribucion, g_posicionamiento, g_salidas, g_jugador,
                  m_vision, m_decision, m_anticipacion, m_concentracion, m_posicionamiento, m_agresividad, m_serenidad,
                  m_liderazgo, m_equipo, m_trabajo, m_arrojo,
                  p_aceleracion, p_velocidad, p_agilidad, p_equilibrio, p_coordinacion, p_resistencia, p_fuerza, p_salto,
                  h_consistencia, h_lesiones, h_juego_duro, h_temperamento)
                  VALUES (@person_id, @position_main, @preferred_foot, @ca, @pa,
                  @t_control, @t_conduccion, @t_pase, @t_pase_un_toque, @t_finalizacion, @t_tiro_lejano, @t_regate, @t_poste,
                  @t_entrada, @t_intercepcion, @t_bloqueo,
                  @g_paradas, @g_reflejos, @g_uno_con_uno, @g_juego_pies, @g_distribucion, @g_posicionamiento, @g_salidas, @g_jugador,
                  @m_vision, @m_decision, @m_anticipacion, @m_concentracion, @m_posicionamiento, @m_agresividad, @m_serenidad,
                  @m_liderazgo, @m_equipo, @m_trabajo, @m_arrojo,
                  @p_aceleracion, @p_velocidad, @p_agilidad, @p_equilibrio, @p_coordinacion, @p_resistencia, @p_fuerza, @p_salto,
                  @h_consistencia, @h_lesiones, @h_juego_duro, @h_temperamento)", connection);

            cmd.Parameters.AddWithValue("@person_id", personId);
            cmd.Parameters.AddWithValue("@position_main", positionMain);
            cmd.Parameters.AddWithValue("@preferred_foot", preferredFoot);
            cmd.Parameters.AddWithValue("@ca", ca);
            cmd.Parameters.AddWithValue("@pa", pa);

            // Atributos técnicos (11)
            string[] techAttrs = { "t_control", "t_conduccion", "t_pase", "t_pase_un_toque", "t_finalizacion",
                                   "t_tiro_lejano", "t_regate", "t_poste", "t_entrada", "t_intercepcion", "t_bloqueo" };
            foreach (var attr in techAttrs)
                cmd.Parameters.AddWithValue($"@{attr}", ParseInt(CsvHelper.GetField(header, fields, attr), 10));

            // Atributos de portero (8)
            string[] gkAttrs = { "g_paradas", "g_reflejos", "g_uno_con_uno", "g_juego_pies",
                                "g_distribucion", "g_posicionamiento", "g_salidas", "g_jugador" };
            foreach (var attr in gkAttrs)
                cmd.Parameters.AddWithValue($"@{attr}", ParseInt(CsvHelper.GetField(header, fields, attr), 1));

            // Atributos mentales (11)
            string[] mentalAttrs = { "m_vision", "m_decision", "m_anticipacion", "m_concentracion", "m_posicionamiento",
                                    "m_agresividad", "m_serenidad", "m_liderazgo", "m_equipo", "m_trabajo", "m_arrojo" };
            foreach (var attr in mentalAttrs)
                cmd.Parameters.AddWithValue($"@{attr}", ParseInt(CsvHelper.GetField(header, fields, attr), 10));

            // Atributos físicos (8)
            string[] physAttrs = { "p_aceleracion", "p_velocidad", "p_agilidad", "p_equilibrio",
                                  "p_coordinacion", "p_resistencia", "p_fuerza", "p_salto" };
            foreach (var attr in physAttrs)
                cmd.Parameters.AddWithValue($"@{attr}", ParseInt(CsvHelper.GetField(header, fields, attr), 10));

            // Atributos ocultos (4)
            string[] hiddenAttrs = { "h_consistencia", "h_lesiones", "h_juego_duro", "h_temperamento" };
            foreach (var attr in hiddenAttrs)
                cmd.Parameters.AddWithValue($"@{attr}", ParseInt(CsvHelper.GetField(header, fields, attr), 10));

            cmd.ExecuteNonQuery();
        }

        private void InsertStaff(SqliteConnection connection, string[] header, string[] fields, long personId)
        {
            var role = CsvHelper.GetField(header, fields, "staff_role");

            using var cmd = new SqliteCommand(
                @"INSERT OR IGNORE INTO staff (person_id, role, ent_tecnica, ent_ofensiva, ent_defensiva, ent_porteros,
                  ent_fisica, ent_tactica, medicina, h_juicio_habilidad, h_juicio_potencial,
                  motivacion, gestion_vestuario, negociacion, adaptabilidad)
                  VALUES (@person_id, @role, @ent_tecnica, @ent_ofensiva, @ent_defensiva, @ent_porteros,
                  @ent_fisica, @ent_tactica, @medicina, @h_juicio_habilidad, @h_juicio_potencial,
                  @motivacion, @gestion_vestuario, @negociacion, @adaptabilidad)", connection);

            cmd.Parameters.AddWithValue("@person_id", personId);
            cmd.Parameters.AddWithValue("@role", role);
            cmd.Parameters.AddWithValue("@ent_tecnica", ParseInt(CsvHelper.GetField(header, fields, "ent_tecnica"), 10));
            cmd.Parameters.AddWithValue("@ent_ofensiva", ParseInt(CsvHelper.GetField(header, fields, "ent_ofensiva"), 10));
            cmd.Parameters.AddWithValue("@ent_defensiva", ParseInt(CsvHelper.GetField(header, fields, "ent_defensiva"), 10));
            cmd.Parameters.AddWithValue("@ent_porteros", ParseInt(CsvHelper.GetField(header, fields, "ent_porteros"), 10));
            cmd.Parameters.AddWithValue("@ent_fisica", ParseInt(CsvHelper.GetField(header, fields, "ent_fisica"), 10));
            cmd.Parameters.AddWithValue("@ent_tactica", ParseInt(CsvHelper.GetField(header, fields, "ent_tactica"), 10));
            cmd.Parameters.AddWithValue("@medicina", ParseInt(CsvHelper.GetField(header, fields, "medicina"), 10));
            cmd.Parameters.AddWithValue("@h_juicio_habilidad", ParseInt(CsvHelper.GetField(header, fields, "h_juicio_habilidad"), 10));
            cmd.Parameters.AddWithValue("@h_juicio_potencial", ParseInt(CsvHelper.GetField(header, fields, "h_juicio_potencial"), 10));
            cmd.Parameters.AddWithValue("@motivacion", ParseInt(CsvHelper.GetField(header, fields, "motivacion"), 10));
            cmd.Parameters.AddWithValue("@gestion_vestuario", ParseInt(CsvHelper.GetField(header, fields, "gestion_vestuario"), 10));
            cmd.Parameters.AddWithValue("@negociacion", ParseInt(CsvHelper.GetField(header, fields, "negociacion"), 10));
            cmd.Parameters.AddWithValue("@adaptabilidad", ParseInt(CsvHelper.GetField(header, fields, "adaptabilidad"), 10));

            cmd.ExecuteNonQuery();
        }

        private void InsertReferee(SqliteConnection connection, string[] header, string[] fields, long personId)
        {
            var countryCode3 = CsvHelper.GetField(header, fields, "referee_country");
            long? countryId = GetCountryIdByCode3(connection, countryCode3);

            object countryValue = DBNull.Value;
            if (countryId.HasValue)
            {
                countryValue = countryId.Value;
            }

            using var cmd = new SqliteCommand(
                @"INSERT OR IGNORE INTO referees (person_id, country_id, strictness, big_match_rating)
                  VALUES (@person_id, @country_id, @strictness, @big_match_rating)", connection);

            cmd.Parameters.AddWithValue("@person_id", personId);
            cmd.Parameters.AddWithValue("@country_id", countryValue);
            cmd.Parameters.AddWithValue("@strictness", ParseInt(CsvHelper.GetField(header, fields, "strictness"), 10));
            cmd.Parameters.AddWithValue("@big_match_rating", ParseDouble(CsvHelper.GetField(header, fields, "big_match_rating"), 50.0));

            cmd.ExecuteNonQuery();
        }

        private long? GetCountryIdByCode3(SqliteConnection connection, string code3)
        {
            if (string.IsNullOrWhiteSpace(code3))
            {
                return null;
            }
            using var cmd = new SqliteCommand("SELECT id FROM countries WHERE code3 = @code3", connection);
            cmd.Parameters.AddWithValue("@code3", code3.ToUpperInvariant());
            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
            {
                return null;
            }
            return Convert.ToInt64(result);
        }

        private int ParseInt(string value, int fallback)
        {
            int result;
            if (int.TryParse(value, out result))
            {
                return result;
            }
            return fallback;
        }

        private double ParseDouble(string value, double fallback)
        {
            double result;
            if (double.TryParse(value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out result))
            {
                return result;
            }
            return fallback;
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