namespace UniFutsal.Core.Domain.People
{
    /// <summary>
    /// Registro del cambio anual de un jugador (snapshot de desarrollo).
    /// </summary>
    public sealed class DevelopmentRecord
    {
        public long PersonId { get; set; }
        public int SeasonYear { get; set; }
        public int AgeAtSnapshot { get; set; }
        public int PreviousCA { get; set; }
        public int NewCA { get; set; }
        public int Delta => NewCA - PreviousCA;

        public override string ToString()
        {
            string sign = Delta > 0 ? "+" : "";
            return $"ID={PersonId} age={AgeAtSnapshot} CA {PreviousCA}→{NewCA} ({sign}{Delta})";
        }
    }
}