namespace UniFutsal.Core.Domain.Geography
{
    /// <summary>
    /// Región de un país (usada para ojeo y captación).
    /// </summary>
    public class Region
    {
        public long Id { get; set; }
        public long CountryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public double YouthQuality { get; set; } = 50.0;

        /// <summary>
        /// Referencia resuelta después de cargar el mundo.
        /// </summary>
        public Country? Country { get; set; }
    }
}