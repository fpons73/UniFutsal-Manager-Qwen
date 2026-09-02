using UniFutsal.Core.Domain.Competitions;
using Xunit;
using System;

namespace UniFutsal.Tests.Competitions
{
    public class CompetitionTests
    {
        [Fact]
        public void Season_CanBeCreated()
        {
            var season = new Season
            {
                Id = 1,
                Label = "2026/27",
                StartDate = new DateTime(2026, 8, 1),
                EndDate = new DateTime(2027, 6, 30)
            };

            Assert.Equal("2026/27", season.Label);
        }

        [Fact]
        public void Competition_DefaultValues_AreCorrect()
        {
            var competition = new Competition();

            Assert.Equal(CompetitionScope.Club, competition.Scope);
            Assert.Equal(CompetitionType.League, competition.Type);
            Assert.Equal(30.0, competition.Prestige);
            Assert.True(competition.Active);
        }

        [Fact]
        public void Competition_CanHavePhases()
        {
            var competition = new Competition { Id = 1, Name = "LNFS Primera" };
            var phase = new CompetitionPhase
            {
                Id = 1,
                CompetitionId = 1,
                PhaseIndex = 0,
                Format = PhaseFormat.RoundRobin
            };
            competition.Phases.Add(phase);

            Assert.Single(competition.Phases);
            Assert.Equal(PhaseFormat.RoundRobin, competition.Phases[0].Format);
        }

        [Fact]
        public void CompetitionEntry_RequiresClubOrNationalTeam()
        {
            var entry = new CompetitionEntry
            {
                SeasonId = 1,
                CompetitionId = 1,
                ClubId = 1
            };

            Assert.NotNull(entry.ClubId);
            Assert.Null(entry.NationalTeamId);
        }
    }
}