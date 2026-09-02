using UniFutsal.Core.Domain.People;

namespace UniFutsal.Core.Domain.Matches
{
    /// <summary>
    /// Jugador convocado para un partido.
    /// </summary>
    public class MatchSquadEntry
    {
        public long MatchId { get; set; }
        public long PersonId { get; set; }

        public MatchSide Side { get; set; }

        /// <summary>
        /// Slot de convocatoria, entre 1 y 14 según schema.
        /// </summary>
        public int Slot { get; set; }

        // Referencias resueltas
        public Match? Match { get; set; }
        public Person? Person { get; set; }
    }
}