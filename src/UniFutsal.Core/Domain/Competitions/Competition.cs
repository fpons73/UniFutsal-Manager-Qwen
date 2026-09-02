using System.Collections.Generic;
using UniFutsal.Core.Domain.Geography;

namespace UniFutsal.Core.Domain.Competitions
{
    /// <summary>
    /// Competición (liga o copa).
    /// </summary>
    public class Competition
    {
        public long Id { get; set; }
        public string Uid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ShortName { get; set; }
        public CompetitionScope Scope { get; set; } = CompetitionScope.Club;
        public CompetitionType Type { get; set; } = CompetitionType.League;
        public long? CountryId { get; set; }
        public long? ConfederationId { get; set; }
        public int? Level { get; set; }
        public double Prestige { get; set; } = 30.0;
        public string RulesJson { get; set; } = "{}";
        public bool Active { get; set; } = true;
        public long? SourcePackId { get; set; }

        // Referencias resueltas
        public Country? Country { get; set; }
        public Confederation? Confederation { get; set; }
        public List<CompetitionPhase> Phases { get; set; } = new List<CompetitionPhase>();
        public List<CompetitionLink> Links { get; set; } = new List<CompetitionLink>();
    }

    /// <summary>
    /// Fase de una competición.
    /// </summary>
    public class CompetitionPhase
    {
        public long Id { get; set; }
        public long CompetitionId { get; set; }
        public int PhaseIndex { get; set; }
        public string? Name { get; set; }
        public PhaseFormat Format { get; set; } = PhaseFormat.RoundRobin;
        public int? TeamsIn { get; set; }
        public int? TeamsOut { get; set; }
        public string ConfigJson { get; set; } = "{}";

        // Referencias resueltas
        public Competition? Competition { get; set; }
        public List<CompetitionGroup> Groups { get; set; } = new List<CompetitionGroup>();
    }

    /// <summary>
    /// Grupo dentro de una fase.
    /// </summary>
    public class CompetitionGroup
    {
        public long Id { get; set; }
        public long PhaseId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int GroupIndex { get; set; }
        public long? HostVenueId { get; set; }

        // Referencias resueltas
        public CompetitionPhase? Phase { get; set; }
        public Venue? HostVenue { get; set; }
    }

    /// <summary>
    /// Enlace entre competiciones (ascensos, descensos, plazas).
    /// </summary>
    public class CompetitionLink
    {
        public long Id { get; set; }
        public long FromCompetitionId { get; set; }
        public long? ToCompetitionId { get; set; }
        public LinkType LinkType { get; set; }
        public string CriteriaJson { get; set; } = "{}";
        public int Slots { get; set; } = 1;
        public int Priority { get; set; } = 10;

        // Referencias resueltas
        public Competition? FromCompetition { get; set; }
        public Competition? ToCompetition { get; set; }
    }
}