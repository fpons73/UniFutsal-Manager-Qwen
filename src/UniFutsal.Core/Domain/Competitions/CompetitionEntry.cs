namespace UniFutsal.Core.Domain.Competitions
{
    /// <summary>
    /// Inscripción de un club o selección en una competición y temporada.
    /// </summary>
    public class CompetitionEntry
    {
        public long Id { get; set; }
        public long SeasonId { get; set; }
        public long CompetitionId { get; set; }
        public long? ClubId { get; set; }
        public long? NationalTeamId { get; set; }
        public long? GroupId { get; set; }
        public int? Seed { get; set; }
        public long? QualifiedViaLinkId { get; set; }
        public EntryStatus Status { get; set; } = EntryStatus.Active;

        // Referencias resueltas
        public Season? Season { get; set; }
        public Competition? Competition { get; set; }
        public Clubs.Club? Club { get; set; }
        public CompetitionGroup? Group { get; set; }
    }
}