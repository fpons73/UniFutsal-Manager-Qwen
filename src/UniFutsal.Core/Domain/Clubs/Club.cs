using UniFutsal.Core.Domain.Geography;

namespace UniFutsal.Core.Domain.Clubs
{
    /// <summary>
    /// Club de futsal.
    /// </summary>
    public class Club
    {
        public long Id { get; set; }
        public string Uid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ShortName { get; set; }
        public string? Nickname { get; set; }
        public long CountryId { get; set; }
        public long? RegionId { get; set; }
        public string? City { get; set; }
        public int? FoundedYear { get; set; }

        // Colores y kit (formato HEX #RRGGBB)
        public string PrimaryColor { get; set; } = "#E63946";
        public string SecondaryColor { get; set; } = "#FFFFFF";
        public KitPattern KitPattern { get; set; } = KitPattern.Solid;

        // Reputación y pabellón
        public double Reputation { get; set; } = 40.0;
        public long? VenueId { get; set; }

        // Instalaciones (1-20)
        public int TrainingFacilities { get; set; } = 10;
        public int YouthFacilities { get; set; } = 10;
        public int Recruitment { get; set; } = 10;
        public int PhysioRating { get; set; } = 10;

        // Finanzas (en euros)
        public int BankBalance { get; set; } = 0;
        public int Debt { get; set; } = 0;
        public int TransferBudget { get; set; } = 0;
        public int WageBudgetMonthly { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        // Referencias resueltas
        public Country? Country { get; set; }
        public Region? Region { get; set; }
        public Venue? Venue { get; set; }
    }
}