using System;
using System.Collections.Generic;

namespace UniFutsal.Core.Domain.Competitions
{
    /// <summary>
    /// Fila de la clasificación de una liga.
    /// </summary>
    public sealed class LeagueStanding
    {
        public long ClubId { get; set; }

        /// <summary>Uid del club (para desempate determinista alfabético).</summary>
        public string ClubUid { get; set; } = string.Empty;

        /// <summary>Nombre del club (para mostrar).</summary>
        public string ClubName { get; set; } = string.Empty;

        /// <summary>Partidos jugados (Played).</summary>
        public int Played { get; set; }

        /// <summary>Victorias (Won).</summary>
        public int Won { get; set; }

        /// <summary>Empates (Drawn).</summary>
        public int Drawn { get; set; }

        /// <summary>Derrotas (Lost).</summary>
        public int Lost { get; set; }

        /// <summary>Goles a favor (Goals For).</summary>
        public int GoalsFor { get; set; }

        /// <summary>Goles en contra (Goals Against).</summary>
        public int GoalsAgainst { get; set; }

        /// <summary>Diferencia de goles (GF − GC).</summary>
        public int GoalDifference => GoalsFor - GoalsAgainst;

        /// <summary>Puntos: 3 por victoria, 1 por empate.</summary>
        public int Points => (Won * 3) + Drawn;
    }

    /// <summary>
    /// Tabla de clasificación de una liga.
    /// Se alimenta con los resultados de los partidos y devuelve las filas ordenadas.
    /// 100% determinista: sin Random ni DateTime.Now.
    /// Nota de diseño (DECISIONS.md D-005): recibe datos crudos (goles) en lugar de
    /// MatchOutcome para que Core no dependa de Engine (evita referencia circular).
    /// </summary>
    public sealed class LeagueTable
    {
        private readonly Dictionary<long, LeagueStanding> _standingsById =
            new Dictionary<long, LeagueStanding>();

        /// <summary>
        /// Registra un club en la tabla (con 0 partidos).
        /// Si ya existe, no hace nada.
        /// </summary>
        public void RegisterClub(long clubId, string clubUid, string clubName)
        {
            if (_standingsById.ContainsKey(clubId))
            {
                return;
            }

            _standingsById[clubId] = new LeagueStanding
            {
                ClubId = clubId,
                ClubUid = clubUid,
                ClubName = clubName
            };
        }

        /// <summary>
        /// Actualiza la tabla con el resultado de un partido (goles del tiempo reglamentario).
        /// Los penaltis (si los hay) NO afectan a los puntos de liga.
        /// Ambos clubes deben estar registrados previamente.
        /// </summary>
        public void RecordResult(long homeClubId, long awayClubId, int homeGoals, int awayGoals)
        {
            LeagueStanding? home = GetStanding(homeClubId);
            LeagueStanding? away = GetStanding(awayClubId);

            if (home == null)
            {
                throw new KeyNotFoundException($"Club local {homeClubId} no registrado en la tabla.");
            }
            if (away == null)
            {
                throw new KeyNotFoundException($"Club visitante {awayClubId} no registrado en la tabla.");
            }

            // Contabilizar partidos y goles
            home.Played++;
            away.Played++;
            home.GoalsFor += homeGoals;
            home.GoalsAgainst += awayGoals;
            away.GoalsFor += awayGoals;
            away.GoalsAgainst += homeGoals;

            // Victorias / empates / derrotas
            if (homeGoals > awayGoals)
            {
                home.Won++;
                away.Lost++;
            }
            else if (awayGoals > homeGoals)
            {
                away.Won++;
                home.Lost++;
            }
            else
            {
                home.Drawn++;
                away.Drawn++;
            }
        }

        /// <summary>
        /// Devuelve la clasificación ordenada de mejor a peor.
        /// Criterio: Puntos ↓ → Diferencia de goles ↓ → Goles a favor ↓ → Uid alfabético ↑.
        /// </summary>
        public List<LeagueStanding> GetOrderedStandings()
        {
            var list = new List<LeagueStanding>(_standingsById.Values);
            list.Sort(CompareStandings);
            return list;
        }

        /// <summary>
        /// Comparador determinista para la ordenación.
        /// Devuelve negativo si 'a' va antes (mejor posición) que 'b'.
        /// </summary>
        private static int CompareStandings(LeagueStanding a, LeagueStanding b)
        {
            // 1. Más puntos primero
            int cmp = b.Points.CompareTo(a.Points);
            if (cmp != 0) return cmp;

            // 2. Mejor diferencia de goles
            cmp = b.GoalDifference.CompareTo(a.GoalDifference);
            if (cmp != 0) return cmp;

            // 3. Más goles a favor
            cmp = b.GoalsFor.CompareTo(a.GoalsFor);
            if (cmp != 0) return cmp;

            // 4. Desempate alfabético por Uid (determinista)
            return string.CompareOrdinal(a.ClubUid, b.ClubUid);
        }

        private LeagueStanding? GetStanding(long clubId)
        {
            LeagueStanding? result;
            if (_standingsById.TryGetValue(clubId, out result))
            {
                return result;
            }
            return null;
        }
    }
}