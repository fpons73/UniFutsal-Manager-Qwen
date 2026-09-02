using System;
using System.Collections.Generic;
using UniFutsal.Core.Domain.Clubs;
using UniFutsal.Core.Domain.Competitions;
using UniFutsal.Core.Domain.Geography;
using UniFutsal.Core.Domain.People;

namespace UniFutsal.Core.Domain.Matches
{
    /// <summary>
    /// Partido de futsal, de club o selección.
    /// </summary>
    public class Match
    {
        public long Id { get; set; }

        public long SeasonId { get; set; }
        public long CompetitionId { get; set; }
        public long? PhaseId { get; set; }
        public long? GroupId { get; set; }

        public string? RoundLabel { get; set; }
        public int? Matchday { get; set; }

        /// <summary>
        /// Clave compartida para una eliminatoria a doble partido.
        /// </summary>
        public string? TieKey { get; set; }

        /// <summary>
        /// 1 o 2 en eliminatorias a doble partido.
        /// </summary>
        public int? Leg { get; set; }

        // Clubes
        public long? HomeClubId { get; set; }
        public long? AwayClubId { get; set; }

        // Selecciones nacionales. Se modelan como ids por ahora.
        public long? HomeNationalTeamId { get; set; }
        public long? AwayNationalTeamId { get; set; }

        public long? RefereeId { get; set; }

        public DateTime? PlayedOn { get; set; }
        public TimeSpan? Kickoff { get; set; }

        public long? VenueId { get; set; }

        public MatchStatus Status { get; set; } = MatchStatus.Scheduled;

        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }

        public int? HomeHalfTimeScore { get; set; }
        public int? AwayHalfTimeScore { get; set; }

        public int? HomePenalties { get; set; }
        public int? AwayPenalties { get; set; }

        public int? Attendance { get; set; }

        /// <summary>
        /// Semilla determinista del partido. Obligatoria según el schema.
        /// </summary>
        public long RngSeed { get; set; }

        /// <summary>
        /// Si true, el partido almacena stream completo de eventos/keyframes.
        /// </summary>
        public bool FullEvents { get; set; }

        // Referencias resueltas
        public Season? Season { get; set; }
        public Competition? Competition { get; set; }
        public CompetitionPhase? Phase { get; set; }
        public CompetitionGroup? Group { get; set; }
        public Club? HomeClub { get; set; }
        public Club? AwayClub { get; set; }
        public Referee? Referee { get; set; }
        public Venue? Venue { get; set; }

        public List<MatchEvent> Events { get; set; } = new List<MatchEvent>();
        public List<MatchSquadEntry> Squad { get; set; } = new List<MatchSquadEntry>();
        public List<MatchPlayerStats> PlayerStats { get; set; } = new List<MatchPlayerStats>();
        public List<MatchTeamStats> TeamStats { get; set; } = new List<MatchTeamStats>();

        public bool IsPlayed()
        {
            return Status == MatchStatus.Played;
        }

        public MatchResult? GetResult()
        {
            if (HomeScore == null || AwayScore == null)
            {
                return null;
            }

            return new MatchResult
            {
                HomeScore = HomeScore.Value,
                AwayScore = AwayScore.Value,
                HomePenalties = HomePenalties,
                AwayPenalties = AwayPenalties
            };
        }
    }
}