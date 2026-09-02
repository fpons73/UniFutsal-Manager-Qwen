namespace UniFutsal.Core.Domain.Geography
{
    /// <summary>
    /// Pabellón donde se juegan los partidos.
    /// </summary>
    public class Venue
    {
        public long Id { get; set; }
        public string Uid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? City { get; set; }
        public long? CountryId { get; set; }
        public int Capacity { get; set; } = 1500;
        public VenueSurface Surface { get; set; } = VenueSurface.Parquet;

        /// <summary>
        /// Referencia resuelta después de cargar el mundo.
        /// </summary>
        public Country? Country { get; set; }
    }

    /// <summary>
    /// Tipo de superficie del pabellón.
    /// </summary>
    public enum VenueSurface
    {
        Parquet,
        Linoleum,
        Pvc,
        Taraflex
    }
}