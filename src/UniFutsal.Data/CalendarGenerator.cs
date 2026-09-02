using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace UniFutsal.Data
{
    public class CalendarGenerator
    {
        private readonly string _dbPath;

        public CalendarGenerator(string dbPath)
        {
            _dbPath = dbPath;
        }

        public int Generate(string competitionUid, string seasonLabel)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            using (var pragmaCmd = new SqliteCommand("PRAGMA foreign_keys = ON;", connection))
            {
                pragmaCmd.ExecuteNonQuery();
            }

            // 1. Obtener IDs de temporada y competición
            long seasonId = GetSeasonId(connection, seasonLabel);
            long competitionId = GetCompetitionId(connection, competitionUid);

            if (seasonId == 0 || competitionId == 0)
            {
                throw new Exception($"Temporada '{seasonLabel}' o competición '{competitionUid}' no encontradas.");
            }

            // 2. Obtener fecha de inicio de la temporada
            DateTime startDate = GetSeasonStartDate(connection, seasonId);

            // 3. Obtener clubes inscritos
            List<long> clubIds = GetActiveClubIds(connection, seasonId, competitionId);

            if (clubIds.Count < 2)
            {
                throw new Exception($"Se necesitan al menos 2 clubes para generar un calendario. Encontrados: {clubIds.Count}");
            }

            // 4. Generar emparejamientos Round-Robin
            var matchdays = GenerateRoundRobin(clubIds);

            // 5. Insertar partidos en la BD
            int totalMatches = 0;
            using var transaction = connection.BeginTransaction();

            try
            {
                for (int i = 0; i < matchdays.Count; i++)
                {
                    int matchday = i + 1;
                    DateTime matchDate = startDate.AddDays(i * 7); // 1 jornada por semana
                    string playedOn = matchDate.ToString("yyyy-MM-dd");
                    string roundLabel = $"J{matchday}";

                    foreach (var (homeId, awayId) in matchdays[i])
                    {
                        // Seed determinista basada en los IDs (garantiza que el mismo calendario
                        // siempre genere los mismos resultados de motor de partido).
                        long rngSeed = (seasonId * 10000000) + (competitionId * 10000) + (matchday * 100) + homeId + awayId;

                        using var insertCmd = new SqliteCommand(
                            @"INSERT INTO matches (season_id, competition_id, round_label, matchday,
                              home_club_id, away_club_id, played_on, status, rng_seed, full_events)
                              VALUES (@season_id, @comp_id, @round_label, @matchday,
                              @home_id, @away_id, @played_on, 'programado', @rng_seed, 0)", connection, transaction);

                        insertCmd.Parameters.AddWithValue("@season_id", seasonId);
                        insertCmd.Parameters.AddWithValue("@comp_id", competitionId);
                        insertCmd.Parameters.AddWithValue("@round_label", roundLabel);
                        insertCmd.Parameters.AddWithValue("@matchday", matchday);
                        insertCmd.Parameters.AddWithValue("@home_id", homeId);
                        insertCmd.Parameters.AddWithValue("@away_id", awayId);
                        insertCmd.Parameters.AddWithValue("@played_on", playedOn);
                        insertCmd.Parameters.AddWithValue("@rng_seed", rngSeed);

                        insertCmd.ExecuteNonQuery();
                        totalMatches++;
                    }
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            return totalMatches;
        }

        private long GetSeasonId(SqliteConnection connection, string label)
        {
            using var cmd = new SqliteCommand("SELECT id FROM seasons WHERE label = @label", connection);
            cmd.Parameters.AddWithValue("@label", label);
            var result = cmd.ExecuteScalar();
            return result == null || result == DBNull.Value ? 0 : Convert.ToInt64(result);
        }

        private long GetCompetitionId(SqliteConnection connection, string uid)
        {
            using var cmd = new SqliteCommand("SELECT id FROM competitions WHERE uid = @uid", connection);
            cmd.Parameters.AddWithValue("@uid", uid);
            var result = cmd.ExecuteScalar();
            return result == null || result == DBNull.Value ? 0 : Convert.ToInt64(result);
        }

        private DateTime GetSeasonStartDate(SqliteConnection connection, long seasonId)
        {
            using var cmd = new SqliteCommand("SELECT start_date FROM seasons WHERE id = @id", connection);
            cmd.Parameters.AddWithValue("@id", seasonId);
            var result = cmd.ExecuteScalar();
            return DateTime.Parse((string)result);
        }

        private List<long> GetActiveClubIds(SqliteConnection connection, long seasonId, long competitionId)
        {
            var clubs = new List<long>();
            using var cmd = new SqliteCommand(
                @"SELECT club_id FROM competition_entries
                  WHERE season_id = @season_id AND competition_id = @comp_id AND status = 'activo'
                  ORDER BY club_id", connection);
            cmd.Parameters.AddWithValue("@season_id", seasonId);
            cmd.Parameters.AddWithValue("@comp_id", competitionId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                clubs.Add(reader.GetInt64(0));
            }
            return clubs;
        }

        /// <summary>
        /// Genera los emparejamientos de una liga a doble vuelta (Round-Robin con método del círculo).
        /// Devuelve una lista de jornadas. Cada jornada es una lista de tuplas (home, away).
        /// </summary>
        public static List<List<(long Home, long Away)>> GenerateRoundRobin(List<long> teams)
        {
            int n = teams.Count;
            var result = new List<List<(long, long)>>();

            // Si es impar, añadimos un "bye" (equipo fantasma que descansa)
            List<long> rotatedTeams = new List<long>(teams);
            if (n % 2 != 0)
            {
                rotatedTeams.Add(-1); // -1 representa el bye
                n++;
            }

            int numMatchdaysFirstHalf = n - 1;
            int halfSize = n / 2;

            // El primer equipo se fija en su posición, el resto rota
            long fixedTeam = rotatedTeams[0];
            List<long> rotating = rotatedTeams.GetRange(1, n - 1);

            // === PRIMERA VUELTA (IDA) ===
            for (int round = 0; round < numMatchdaysFirstHalf; round++)
            {
                var matchday = new List<(long, long)>();

                // Emparejar el equipo fijo con el primero de la lista rotante
                long opponent = rotating[0];
                if (fixedTeam != -1 && opponent != -1)
                {
                    // Alternar local/visitante para el equipo fijo
                    if (round % 2 == 0)
                        matchday.Add((fixedTeam, opponent));
                    else
                        matchday.Add((opponent, fixedTeam));
                }

                // Emparejar el resto simétricamente (el 2º con el último, el 3º con el penúltimo...)
                for (int i = 1; i < halfSize; i++)
                {
                    long team1 = rotating[i];
                    long team2 = rotating[n - 1 - i];

                    if (team1 != -1 && team2 != -1)
                    {
                        if (i % 2 == 0)
                            matchday.Add((team1, team2));
                        else
                            matchday.Add((team2, team1));
                    }
                }

                result.Add(matchday);

                // Rotar la lista: el último pasa al principio
                long last = rotating[rotating.Count - 1];
                rotating.RemoveAt(rotating.Count - 1);
                rotating.Insert(0, last);
            }

            // === SEGUNDA VUELTA (VUELTA) ===
            // Invertimos local y visitante de la primera vuelta
            int firstHalfCount = result.Count;
            for (int round = 0; round < firstHalfCount; round++)
            {
                var matchday = new List<(long, long)>();
                foreach (var (home, away) in result[round])
                {
                    matchday.Add((away, home));
                }
                result.Add(matchday);
            }

            return result;
        }
    }
}