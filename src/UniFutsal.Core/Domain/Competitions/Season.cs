using System;

namespace UniFutsal.Core.Domain.Competitions
{
    /// <summary>
    /// Temporada del mundo (ej. 2026/27).
    /// </summary>
    public class Season
    {
        public long Id { get; set; }
        public string Label { get; set; } = string.Empty; // '2026/27' o '2027'
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}