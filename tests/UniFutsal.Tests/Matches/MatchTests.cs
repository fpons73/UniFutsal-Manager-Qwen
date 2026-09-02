using UniFutsal.Core.Domain.Matches;
using Xunit;

namespace UniFutsal.Tests.Matches
{
    public class MatchTests
    {
        [Fact]
        public void Match_DefaultStatus_IsScheduled()
        {
            var match = new Match();

            Assert.Equal(MatchStatus.Scheduled, match.Status);
            Assert.False(match.IsPlayed());
        }

        [Fact]
        public void Match_GetResult_ReturnsNull_WhenScoresAreMissing()
        {
            var match = new Match();

            var result = match.GetResult();

            Assert.Null(result);
        }

        [Fact]
        public void Match_GetResult_ReturnsResult_WhenScoresExist()
        {
            var match = new Match
            {
                HomeScore = 3,
                AwayScore = 1
            };

            var result = match.GetResult();

            Assert.NotNull(result);
            Assert.Equal(3, result.HomeScore);
            Assert.Equal(1, result.AwayScore);
            Assert.Equal(MatchSide.Home, result.GetWinner());
        }

        [Fact]
        public void MatchResult_DetectsDraw()
        {
            var result = new MatchResult
            {
                HomeScore = 2,
                AwayScore = 2
            };

            Assert.True(result.IsDraw());
            Assert.Null(result.GetWinner());
        }

        [Fact]
        public void MatchResult_UsesPenalties_WhenScoresAreDrawn()
        {
            var result = new MatchResult
            {
                HomeScore = 2,
                AwayScore = 2,
                HomePenalties = 4,
                AwayPenalties = 3
            };

            Assert.True(result.IsDraw());
            Assert.Equal(MatchSide.Home, result.GetWinner());
        }

        [Fact]
        public void MatchEvent_DefaultDetailJson_IsEmptyObject()
        {
            var matchEvent = new MatchEvent
            {
                Type = MatchEventType.Goal,
                Sequence = 1,
                Period = 1,
                ClockMinute = 5,
                ClockSecond = 30
            };

            Assert.Equal("{}", matchEvent.DetailJson);
        }
    }
}