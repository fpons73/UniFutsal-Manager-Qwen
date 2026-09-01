using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace UniFutsal.Data
{
    public class ValidationResult
{
    public string QueryName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ProblemCount { get; set; }
    public List<string> Details { get; set; } = new List<string>();
    public bool Passed => ProblemCount == 0;
}

    public class WorldValidator
    {
        private readonly string _dbPath;

        public WorldValidator(string dbPath)
        {
            _dbPath = dbPath;
        }

        public List<ValidationResult> Validate()
        {
            var results = new List<ValidationResult>();

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            // Query 1: Clubes con plantilla insuficiente (<12 contratados vigentes)
            results.Add(RunQuery(connection,
                "Clubes con plantilla insuficiente",
                "Clubes con menos de 12 jugadores contratados vigentes",
                @"SELECT c.name, COUNT(ct.person_id) AS fichas
                  FROM clubs c 
                  LEFT JOIN contracts ct ON ct.club_id = c.id 
                    AND ct.status = 'vigente' 
                    AND ct.scope = 'primer_equipo'
                  GROUP BY c.id 
                  HAVING fichas < 12"));

            // Query 2: Competiciones activas con menos de 4 participantes
            results.Add(RunQuery(connection,
                "Competiciones con pocos participantes",
                "Competiciones activas con menos de 4 equipos inscritos",
                @"SELECT k.name, COUNT(e.id) AS n
                  FROM competitions k 
                  LEFT JOIN competition_entries e ON e.competition_id = k.id 
                    AND e.status = 'activo'
                  WHERE k.active = 1 
                  GROUP BY k.id 
                  HAVING n < 4"));

            // Query 3: Enlaces rotos (ascenso/descenso sin destino)
            results.Add(RunQuery(connection,
                "Enlaces de competición rotos",
                "Ascensos/descensos sin competición destino definida",
                @"SELECT from_competition_id, link_type
                  FROM competition_links
                  WHERE link_type IN ('ascenso','descenso') 
                    AND to_competition_id IS NULL"));

            // Query 4: Incoherencia de potencial (CA > PA)
            results.Add(RunQuery(connection,
                "Incoherencia CA > PA",
                "Jugadores con habilidad actual mayor que su potencial",
                @"SELECT pu.first_name, pu.last_name
                  FROM players pl 
                  JOIN persons pu ON pu.id = pl.person_id
                  WHERE pl.current_ability > pl.potential_ability"));

            // Query 5: Equipos sin pabellón asignado
            results.Add(RunQuery(connection,
                "Clubes sin pabellón",
                "Clubes activos sin venue_id asignado",
                @"SELECT name 
                  FROM clubs 
                  WHERE venue_id IS NULL AND is_active = 1"));

            // Query 6: Partidos jugados sin árbitro
            results.Add(RunQuery(connection,
                "Partidos sin árbitro",
                "Partidos con status 'jugado' pero sin referee_id",
                @"SELECT id, played_on 
                FROM matches 
                WHERE status = 'jugado' AND referee_id IS NULL"));

            // Query 7: Jugadores sancionados en convocatorias
            results.Add(RunQuery(connection,
                "Sancionados en convocatorias",
                "Jugadores con tarjeta roja que aparecen en match_squads",
                @"SELECT ms.match_id, ms.person_id 
                  FROM match_squads ms
                  JOIN match_events me ON me.match_id = ms.match_id
                    AND me.type = 'tarjeta_roja' 
                    AND me.person_id = ms.person_id"));

            return results;
        }

        private ValidationResult RunQuery(SqliteConnection connection, string name, string description, string sql)
        {
            var result = new ValidationResult
            {
                QueryName = name,
                Description = description
            };

            try
            {
                using var command = new SqliteCommand(sql, connection);
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    result.ProblemCount++;
                    
                    // Construir detalle legible
                    var details = new List<string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var colName = reader.GetName(i);
                        var value = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString();
                        details.Add($"{colName}={value}");
                    }
                    result.Details.Add(string.Join(", ", details));
                }
            }
            catch (Exception ex)
            {
                result.ProblemCount = -1; // Error en la query
                result.Details.Add($"ERROR: {ex.Message}");
            }

            return result;
        }
    }
}