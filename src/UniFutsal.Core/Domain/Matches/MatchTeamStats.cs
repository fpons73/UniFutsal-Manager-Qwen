namespace UniFutsal.Core.Domain.Matches
{
    /// <summary>
    /// Estadísticas agregadas de un equipo en un partido.
    /// </summary>
    public class MatchTeamStats
    {
        public long MatchId { get; set; }

        public MatchSide Side { get; set; }

        public double? PossessionPct { get; set; }

        public int Shots { get; set; }
        public int ShotsOnTarget { get; set; }

        public int Fouls { get; set; }
        public int YellowCards { get; set; }
        public int RedCards { get; set; }

        public int Corners { get; set; }

        /// <summary>
        /// Timeouts usados: 0 a 2.
        /// </summary>
        public int TimeoutsUsed { get; set; }

        public int PowerPlaySeconds { get; set; }
        public int PowerPlayGoals { get; set; }

        public int DoublePenaltyAttempts { get; set; }
        public int DoublePenaltyGoals { get; set; }

        // Referencia resuelta
        public Match? Match { get; set; }
    }
}