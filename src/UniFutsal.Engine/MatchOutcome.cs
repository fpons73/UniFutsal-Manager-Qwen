namespace UniFutsal.Engine
{
    /// <summary>
    /// Resultado simplificado de un partido (para simulación instantánea).
    /// No incluye stream de eventos (eso es el UME completo de M4).
    /// </summary>
    public sealed class MatchOutcome
    {
        /// <summary>Goles del equipo local.</summary>
        public int HomeScore { get; set; }

        /// <summary>Goles del equipo visitante.</summary>
        public int AwayScore { get; set; }

        /// <summary>Goles en tanda de penaltis (si aplicó). null si no hubo tanda.</summary>
        public int? HomePenalties { get; set; }
        public int? AwayPenalties { get; set; }

        /// <summary>Seed usado para reproducir el partido.</summary>
        public long RngSeed { get; set; }

        /// <summary>Rating medio de los jugadores locales (1-20).</summary>
        public double HomeRating { get; set; }

        /// <summary>Rating medio de los jugadores visitantes (1-20).</summary>
        public double AwayRating { get; set; }

        /// <summary>
        /// Devuelve 'H' si ganó local, 'A' si ganó visitante, 'D' si empate.
        /// Si hay penaltis, el ganador es quien ganó la tanda.
        /// </summary>
        public char GetWinner()
        {
            if (HomePenalties.HasValue && AwayPenalties.HasValue)
            {
                return HomePenalties.Value > AwayPenalties.Value ? 'H' : 'A';
            }
            if (HomeScore > AwayScore) return 'H';
            if (AwayScore > HomeScore) return 'A';
            return 'D';
        }

        public bool IsDraw() => HomeScore == AwayScore && !HomePenalties.HasValue;

        public override string ToString()
        {
            var result = $"{HomeScore}-{AwayScore}";
            if (HomePenalties.HasValue)
            {
                result += $" (pen. {HomePenalties}-{AwayPenalties})";
            }
            return result;
        }
    }
}