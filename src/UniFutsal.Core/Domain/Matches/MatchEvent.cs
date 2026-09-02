using UniFutsal.Core.Domain.People;

namespace UniFutsal.Core.Domain.Matches
{
    /// <summary>
    /// Evento emitido durante un partido.
    /// </summary>
    public class MatchEvent
    {
        public long Id { get; set; }
        public long MatchId { get; set; }

        /// <summary>
        /// Secuencia incremental del evento dentro del partido.
        /// </summary>
        public int Sequence { get; set; }

        /// <summary>
        /// Periodo: 1, 2, 3 o 4.
        /// </summary>
        public int Period { get; set; } = 1;

        public int ClockMinute { get; set; }
        public int ClockSecond { get; set; }

        public MatchEventType Type { get; set; }
        public MatchSide? Side { get; set; }

        public long? PersonId { get; set; }
        public long? SecondaryPersonId { get; set; }

        public int? ScoreHomeAfter { get; set; }
        public int? ScoreAwayAfter { get; set; }

        public string? NarrativeKey { get; set; }

        /// <summary>
        /// JSON con detalles técnicos del evento.
        /// En motor full_events puede incluir detail_json.kf.
        /// </summary>
        public string DetailJson { get; set; } = "{}";

        // Referencias resueltas
        public Match? Match { get; set; }
        public Person? Person { get; set; }
        public Person? SecondaryPerson { get; set; }
    }
}