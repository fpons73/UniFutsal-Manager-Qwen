using UniFutsal.Core.Domain.People;

namespace UniFutsal.Core.Domain.Matches
{
    /// <summary>
    /// Estadísticas individuales de un jugador en un partido.
    /// </summary>
    public class MatchPlayerStats
    {
        public long MatchId { get; set; }
        public long PersonId { get; set; }

        public MatchSide Side { get; set; }

        public bool Starter { get; set; }
        public int MinutesPlayed { get; set; }

        public int Goals { get; set; }
        public int OwnGoals { get; set; }
        public int Assists { get; set; }

        public int Shots { get; set; }
        public int ShotsOnTarget { get; set; }

        public int PassesAttempted { get; set; }
        public int PassesCompleted { get; set; }
        public int KeyPasses { get; set; }

        public int DribblesCompleted { get; set; }
        public int Interceptions { get; set; }
        public int TacklesWon { get; set; }

        public int FoulsCommitted { get; set; }
        public int FoulsReceived { get; set; }

        public int YellowCards { get; set; }
        public int RedCards { get; set; }

        public int Saves { get; set; }
        public int GoalsConceded { get; set; }

        public double? Rating { get; set; }

        // Referencias resueltas
        public Match? Match { get; set; }
        public Person? Person { get; set; }
    }
}