namespace UniFutsal.Core.Domain.Geography
{
    /// <summary>
    /// País del mundo del futsal.
    /// </summary>
    public class Country
    {
        public long Id { get; set; }
        public string Uid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code3 { get; set; } = string.Empty;
        public long ConfederationId { get; set; }
        public double FutsalReputation { get; set; } = 50.0;

        /// <summary>
        /// Referencia resuelta después de cargar el mundo (no se serializa).
        /// </summary>
        public Confederation? Confederation { get; set; }
    }
}