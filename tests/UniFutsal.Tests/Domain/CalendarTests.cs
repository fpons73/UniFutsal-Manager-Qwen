using System.Collections.Generic;
using UniFutsal.Data;
using Xunit;

namespace UniFutsal.Tests.Domain
{
    public class CalendarTests
    {
        [Fact]
        public void GenerateRoundRobin_8Teams_Generates14Matchdays()
        {
            var teams = new List<long> { 1, 2, 3, 4, 5, 6, 7, 8 };
            var calendar = CalendarGenerator.GenerateRoundRobin(teams);

            // 8 equipos = 7 jornadas ida + 7 jornadas vuelta = 14 jornadas
            Assert.Equal(14, calendar.Count);

            // Cada jornada tiene 4 partidos
            foreach (var matchday in calendar)
            {
                Assert.Equal(4, matchday.Count);
            }

            // Total de partidos = 14 * 4 = 56
            int totalMatches = 0;
            foreach (var matchday in calendar) totalMatches += matchday.Count;
            Assert.Equal(56, totalMatches);
        }

        [Fact]
        public void GenerateRoundRobin_NoTeamPlaysItself()
        {
            var teams = new List<long> { 1, 2, 3, 4 };
            var calendar = CalendarGenerator.GenerateRoundRobin(teams);

            foreach (var matchday in calendar)
            {
                foreach (var (home, away) in matchday)
                {
                    Assert.NotEqual(home, away);
                }
            }
        }

        [Fact]
        public void GenerateRoundRobin_EveryPairPlaysTwice()
        {
            var teams = new List<long> { 1, 2, 3, 4 };
            var calendar = CalendarGenerator.GenerateRoundRobin(teams);

            var matchups = new Dictionary<string, int>();

            foreach (var matchday in calendar)
            {
                foreach (var (home, away) in matchday)
                {
                    // Clave ordenada para contar enfrentamientos totales (independiente de quién es local)
                    long min = System.Math.Min(home, away);
                    long max = System.Math.Max(home, away);
                    string key = $"{min}-{max}";

                    if (!matchups.ContainsKey(key)) matchups[key] = 0;
                    matchups[key]++;
                }
            }

            // 4 equipos = 6 parejas posibles. Cada pareja juega exactamente 2 veces (ida y vuelta)
            Assert.Equal(6, matchups.Count);
            foreach (var count in matchups.Values)
            {
                Assert.Equal(2, count);
            }
        }
    }
}