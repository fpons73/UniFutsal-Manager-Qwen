namespace UniFutsal.Core.Domain.Geography
{
    /// <summary>
    /// Confederación continental de futsal (UEFA, CONMEBOL, AFC...).
    /// </summary>
    public class Confederation
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}