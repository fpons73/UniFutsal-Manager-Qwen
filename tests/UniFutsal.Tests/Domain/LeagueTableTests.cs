using System.Collections.Generic;
using UniFutsal.Core.Domain.Competitions;
using Xunit;

namespace UniFutsal.Tests.Domain
{
    public class LeagueTableTests
    {
        private static LeagueTable CreateTableWithThreeClubs()
        {
            var table = new LeagueTable();
            table.RegisterClub(1, "club-aaa", "Club A");
            table.RegisterClub(2, "club-bbb", "Club B");
            table.RegisterClub(3, "club-ccc", "Club C");
            return table;
        }

        [Fact]
        public void RegisterClub_AddsStandingWithZeroStats()
        {
            var table = new LeagueTable();
            table.RegisterClub(1, "club-aaa", "Club A");

            var standings = table.GetOrderedStandings();
            Assert.Single(standings);
            Assert.Equal(0, standings[0].Played);
            Assert.Equal(0, standings[0].Points);
            Assert.Equal("Club A", standings[0].ClubName);
        }

        [Fact]
        public void RegisterClub_IsIdempotent()
        {
            var table = new LeagueTable();
            table.RegisterClub(1, "club-aaa", "Club A");
            table.RegisterClub(1, "club-aaa", "Club A"); // duplicado

            Assert.Single(table.GetOrderedStandings());
        }

        [Fact]
        public void RecordResult_HomeWin_UpdatesCorrectly()
        {
            var table = CreateTableWithThreeClubs();
            table.RecordResult(1, 2, 3, 1); // Club A gana 3-1 a Club B

            var standings = table.GetOrderedStandings();
            var clubA = standings.Find(s => s.ClubId == 1);
            var clubB = standings.Find(s => s.ClubId == 2);

            Assert.NotNull(clubA);
            Assert.NotNull(clubB);
            Assert.Equal(1, clubA.Played);
            Assert.Equal(1, clubA.Won);
            Assert.Equal(0, clubA.Drawn);
            Assert.Equal(0, clubA.Lost);
            Assert.Equal(3, clubA.GoalsFor);
            Assert.Equal(1, clubA.GoalsAgainst);
            Assert.Equal(2, clubA.GoalDifference);
            Assert.Equal(3, clubA.Points);

            Assert.Equal(1, clubB.Lost);
            Assert.Equal(0, clubB.Points);
            Assert.Equal(-2, clubB.GoalDifference);
        }

        [Fact]
        public void RecordResult_Draw_GivesOnePointEach()
        {
            var table = CreateTableWithThreeClubs();
            table.RecordResult(1, 2, 2, 2);

            var standings = table.GetOrderedStandings();
            var clubA = standings.Find(s => s.ClubId == 1);
            var clubB = standings.Find(s => s.ClubId == 2);

            Assert.NotNull(clubA);
            Assert.NotNull(clubB);
            Assert.Equal(1, clubA.Drawn);
            Assert.Equal(1, clubB.Drawn);
            Assert.Equal(1, clubA.Points);
            Assert.Equal(1, clubB.Points);
        }

        [Fact]
        public void RecordResult_ThrowsOnUnregisteredClub()
        {
            var table = CreateTableWithThreeClubs();
            Assert.Throws<KeyNotFoundException>(() => table.RecordResult(1, 999, 1, 0));
            Assert.Throws<KeyNotFoundException>(() => table.RecordResult(999, 1, 1, 0));
        }

        [Fact]
        public void GetOrderedStandings_OrdersByPointsDescending()
        {
            var table = CreateTableWithThreeClubs();
            // A gana a B, B gana a C, A gana a C → A=6pts, B=3pts, C=0pts
            table.RecordResult(1, 2, 2, 0);
            table.RecordResult(2, 3, 1, 0);
            table.RecordResult(1, 3, 3, 1);

            var standings = table.GetOrderedStandings();

            Assert.Equal(3, standings.Count);
            Assert.Equal(1, standings[0].ClubId); // Club A, 6 pts
            Assert.Equal(6, standings[0].Points);
            Assert.Equal(2, standings[1].ClubId); // Club B, 3 pts
            Assert.Equal(3, standings[1].Points);
            Assert.Equal(3, standings[2].ClubId); // Club C, 0 pts
            Assert.Equal(0, standings[2].Points);
        }

        [Fact]
        public void GetOrderedStandings_BreaksTieByGoalDifference()
        {
            var table = new LeagueTable();
            table.RegisterClub(1, "club-aaa", "Club A");
            table.RegisterClub(2, "club-bbb", "Club B");

            // Ambos con 3 puntos, pero A con mejor diferencia de goles
            table.RecordResult(1, 2, 5, 0); // A gana 5-0 → A=3pts (+5), B=0pts (-5)
            table.RecordResult(2, 1, 1, 0); // B gana 1-0 → ambos 3pts, A=+4, B=-4

            var standings = table.GetOrderedStandings();

            Assert.Equal(2, standings.Count);
            // A: GF=5, GA=1, DG=+4 · B: GF=1, GA=5, DG=-4
            Assert.Equal(1, standings[0].ClubId);
            Assert.Equal(4, standings[0].GoalDifference);
            Assert.Equal(2, standings[1].ClubId);
            Assert.Equal(-4, standings[1].GoalDifference);
        }

        [Fact]
        public void GetOrderedStandings_BreaksTieByUid_WhenAllEqual()
        {
            var table = new LeagueTable();
            // Dos clubes sin partidos: mismo todo → desempate alfabético por Uid
            table.RegisterClub(2, "club-zzz", "Club Z");
            table.RegisterClub(1, "club-aaa", "Club A");

            var standings = table.GetOrderedStandings();

            Assert.Equal(2, standings.Count);
            // 'club-aaa' va antes que 'club-zzz' alfabéticamente
            Assert.Equal("club-aaa", standings[0].ClubUid);
            Assert.Equal("club-zzz", standings[1].ClubUid);
        }

        [Fact]
        public void GetOrderedStandings_IsDeterministic()
        {
            // Misma secuencia de resultados → mismo orden siempre
            var table1 = CreateTableWithThreeClubs();
            var table2 = CreateTableWithThreeClubs();

            table1.RecordResult(1, 2, 2, 1);
            table1.RecordResult(2, 3, 1, 1);
            table1.RecordResult(3, 1, 0, 2);

            table2.RecordResult(1, 2, 2, 1);
            table2.RecordResult(2, 3, 1, 1);
            table2.RecordResult(3, 1, 0, 2);

            var s1 = table1.GetOrderedStandings();
            var s2 = table2.GetOrderedStandings();

            Assert.Equal(s1.Count, s2.Count);
            for (int i = 0; i < s1.Count; i++)
            {
                Assert.Equal(s1[i].ClubId, s2[i].ClubId);
                Assert.Equal(s1[i].Points, s2[i].Points);
            }
        }
    }
}