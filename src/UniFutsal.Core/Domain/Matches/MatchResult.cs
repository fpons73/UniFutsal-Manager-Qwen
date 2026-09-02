namespace UniFutsal.Core.Domain.Matches
{
    /// <summary>
    /// Resultado final de un partido.
    /// </summary>
    public class MatchResult
    {
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }

        public int? HomePenalties { get; set; }
        public int? AwayPenalties { get; set; }

        public bool IsDraw()
        {
            return HomeScore == AwayScore;
        }

        public MatchSide? GetWinner()
        {
            if (HomeScore > AwayScore)
            {
                return MatchSide.Home;
            }

            if (AwayScore > HomeScore)
            {
                return MatchSide.Away;
            }

            if (HomePenalties != null && AwayPenalties != null)
            {
                if (HomePenalties > AwayPenalties)
                {
                    return MatchSide.Home;
                }

                if (AwayPenalties > HomePenalties)
                {
                    return MatchSide.Away;
                }
            }

            return null;
        }
    }
}