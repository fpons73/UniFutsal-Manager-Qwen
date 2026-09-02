using System.Collections.Generic;
using UniFutsal.Core.Domain;
using UniFutsal.Core.Domain.Clubs;
using UniFutsal.Core.Domain.Matches;
using UniFutsal.Core.Domain.People;
using UniFutsal.Engine;
using Xunit;

namespace UniFutsal.Tests.Engine
{
    public class InstantSimulatorTests
    {
        /// <summary>
        /// Crea un mundo de prueba con 2 clubes y 12 jugadores cada uno.
        /// Club "fuerte" (CA ~130) vs club "débil" (CA ~90).
        /// </summary>
        private static (World world, Club home, Club away) CreateTestWorld()
        {
            var world = new World();

            // País ficticio
            var country = new Core.Domain.Geography.Country
            {
                Id = 1, Uid = "country-test", Name = "Testland", Code3 = "TST",
                ConfederationId = 1, FutsalReputation = 50
            };
            world.Countries.Add(country);

            // Clubes
            var home = new Club { Id = 1, Uid = "club-home", Name = "Home FC", CountryId = 1 };
            var away = new Club { Id = 2, Uid = "club-away", Name = "Away FC", CountryId = 1 };
            world.Clubs.Add(home);
            world.Clubs.Add(away);

            // 12 jugadores por club: local fuertes (CA 130), visitante débiles (CA 90)
            for (int i = 0; i < 12; i++)
            {
                var pHome = new Person { Id = 100 + i, Uid = $"p-home-{i}", FirstName = "H", LastName = $"{i}", NationalityId = 1 };
                var pAway = new Person { Id = 200 + i, Uid = $"p-away-{i}", FirstName = "A", LastName = $"{i}", NationalityId = 1 };
                world.Persons.Add(pHome);
                world.Persons.Add(pAway);

                world.Players.Add(new Player
                {
                    PersonId = pHome.Id,
                    PositionMain = Position.Pivot,
                    CurrentAbility = 130,
                    PotentialAbility = 140
                });
                world.Players.Add(new Player
                {
                    PersonId = pAway.Id,
                    PositionMain = Position.Pivot,
                    CurrentAbility = 90,
                    PotentialAbility = 100
                });

                world.Contracts.Add(new Contract
                {
                    Id = 1000 + i,
                    PersonId = pHome.Id,
                    ClubId = home.Id,
                    Scope = ContractScope.FirstTeam,
                    Status = ContractStatus.Active
                });
                world.Contracts.Add(new Contract
                {
                    Id = 2000 + i,
                    PersonId = pAway.Id,
                    ClubId = away.Id,
                    Scope = ContractScope.FirstTeam,
                    Status = ContractStatus.Active
                });
            }

            world.IndexAll();
            return (world, home, away);
        }

        [Fact]
        public void Simulate_IsDeterministic_SameSeedSameResult()
        {
            var (world, home, away) = CreateTestWorld();
            var sim = new InstantMatchSimulator(world);

            var match = new Match
            {
                Id = 1,
                HomeClubId = home.Id,
                AwayClubId = away.Id,
                RngSeed = 12345L
            };

            var r1 = sim.Simulate(match);
            var r2 = sim.Simulate(match);

            Assert.Equal(r1.HomeScore, r2.HomeScore);
            Assert.Equal(r1.AwayScore, r2.AwayScore);
            Assert.Equal(r1.RngSeed, r2.RngSeed);
        }

        [Fact]
        public void Simulate_DifferentSeeds_DifferentResults()
        {
            var (world, home, away) = CreateTestWorld();
            var sim = new InstantMatchSimulator(world);

            var results = new HashSet<string>();
            for (long seed = 1; seed <= 20; seed++)
            {
                var match = new Match { HomeClubId = home.Id, AwayClubId = away.Id, RngSeed = seed };
                var r = sim.Simulate(match);
                results.Add($"{r.HomeScore}-{r.AwayScore}");
            }

            // Debería haber variación con 20 seeds distintas
            Assert.True(results.Count >= 5, $"Poca variación: {results.Count} resultados distintos");
        }

        [Fact]
        public void Simulate_GoalCount_InPlausibleRange()
        {
            var (world, home, away) = CreateTestWorld();
            var sim = new InstantMatchSimulator(world);

            int totalGoals = 0;
            const int N = 500;
            for (long seed = 1; seed <= N; seed++)
            {
                var match = new Match { HomeClubId = home.Id, AwayClubId = away.Id, RngSeed = seed };
                var r = sim.Simulate(match);
                totalGoals += r.HomeScore + r.AwayScore;
            }

            double avg = (double)totalGoals / N;
            // Objetivo LNFS: ~5.5–6.5 goles/partido. Permitimos rango amplio [3.5, 9.0] para v0
            Assert.True(avg >= 3.5 && avg <= 9.0,
                $"Media de goles fuera de rango: {avg:F2}");
        }

        [Fact]
        public void Simulate_StrongerTeam_WinsMoreOften()
        {
            var (world, home, away) = CreateTestWorld();
            var sim = new InstantMatchSimulator(world);

            int homeWins = 0;
            int awayWins = 0;
            int draws = 0;
            const int N = 300;
            for (long seed = 1; seed <= N; seed++)
            {
                var match = new Match { HomeClubId = home.Id, AwayClubId = away.Id, RngSeed = seed };
                var r = sim.Simulate(match);
                if (r.HomeScore > r.AwayScore) homeWins++;
                else if (r.AwayScore > r.HomeScore) awayWins++;
                else draws++;
            }

            // Local (CA 130) debería ganar más que visitante (CA 90)
            Assert.True(homeWins > awayWins,
                $"El equipo fuerte (home) debería ganar más: home={homeWins}, away={awayWins}, draws={draws}");
            // El equipo fuerte debería ganar al menos el 50% de las veces
            Assert.True(homeWins > N * 0.45,
                $"% victorias home muy bajo: {(double)homeWins/N:P0}");
        }

        [Fact]
        public void Simulate_EvenMatch_DrawsAreFrequent()
        {
            // Creamos dos equipos iguales
            var world = new World();
            var country = new Core.Domain.Geography.Country
            {
                Id = 1, Uid = "country-test", Name = "Testland", Code3 = "TST",
                ConfederationId = 1, FutsalReputation = 50
            };
            world.Countries.Add(country);
            var c1 = new Club { Id = 1, Uid = "c1", Name = "C1", CountryId = 1 };
            var c2 = new Club { Id = 2, Uid = "c2", Name = "C2", CountryId = 1 };
            world.Clubs.Add(c1);
            world.Clubs.Add(c2);

            for (int i = 0; i < 12; i++)
            {
                var p1 = new Person { Id = 100 + i, Uid = $"p1-{i}", FirstName = "X", LastName = $"{i}", NationalityId = 1 };
                var p2 = new Person { Id = 200 + i, Uid = $"p2-{i}", FirstName = "Y", LastName = $"{i}", NationalityId = 1 };
                world.Persons.Add(p1);
                world.Persons.Add(p2);
                world.Players.Add(new Player { PersonId = p1.Id, PositionMain = Position.Pivot, CurrentAbility = 110, PotentialAbility = 110 });
                world.Players.Add(new Player { PersonId = p2.Id, PositionMain = Position.Pivot, CurrentAbility = 110, PotentialAbility = 110 });
                world.Contracts.Add(new Contract { Id = 1000 + i, PersonId = p1.Id, ClubId = c1.Id, Scope = ContractScope.FirstTeam, Status = ContractStatus.Active });
                world.Contracts.Add(new Contract { Id = 2000 + i, PersonId = p2.Id, ClubId = c2.Id, Scope = ContractScope.FirstTeam, Status = ContractStatus.Active });
            }
            world.IndexAll();

            var sim = new InstantMatchSimulator(world);
            int draws = 0;
            const int N = 400;
            for (long seed = 1; seed <= N; seed++)
            {
                var m = new Match { HomeClubId = c1.Id, AwayClubId = c2.Id, RngSeed = seed };
                var r = sim.Simulate(m, allowPenalties: false);
                if (r.HomeScore == r.AwayScore) draws++;
            }

            double drawPct = (double)draws / N;
            // En futsal los empates son ~20-30% en partidos parejos
            Assert.True(drawPct >= 0.15 && drawPct <= 0.45,
                $"% empates fuera de rango: {drawPct:P0}");
        }

        [Fact]
        public void Simulate_WithPenalties_AlwaysProducesWinner()
        {
            var (world, home, away) = CreateTestWorld();
            var sim = new InstantMatchSimulator(world);

            int resolved = 0;
            for (long seed = 1; seed <= 50; seed++)
            {
                var m = new Match { HomeClubId = home.Id, AwayClubId = away.Id, RngSeed = seed };
                var r = sim.Simulate(m, allowPenalties: true);
                char winner = r.GetWinner();
                Assert.True(winner == 'H' || winner == 'A', $"Debería haber ganador: {winner}");
                resolved++;
            }

            Assert.Equal(50, resolved);
        }

        [Fact]
        public void MatchOutcome_GetWinner_DetectsHomeWin()
        {
            var o = new MatchOutcome { HomeScore = 3, AwayScore = 1 };
            Assert.Equal('H', o.GetWinner());
            Assert.False(o.IsDraw());
        }

        [Fact]
        public void MatchOutcome_GetWinner_DetectsDraw()
        {
            var o = new MatchOutcome { HomeScore = 2, AwayScore = 2 };
            Assert.Equal('D', o.GetWinner());
            Assert.True(o.IsDraw());
        }

        [Fact]
        public void MatchOutcome_GetWinner_PenaltiesOverrideScore()
        {
            var o = new MatchOutcome
            {
                HomeScore = 2, AwayScore = 2,
                HomePenalties = 4, AwayPenalties = 5
            };
            Assert.Equal('A', o.GetWinner());
            Assert.False(o.IsDraw());
        }

        [Fact]
        public void Simulate_ThrowsOnMissingClub()
        {
            var world = new World();
            world.IndexAll();
            var sim = new InstantMatchSimulator(world);
            var m = new Match { HomeClubId = 999, AwayClubId = 888, RngSeed = 1L };
            Assert.Throws<System.ArgumentException>(() => sim.Simulate(m));
        }
    }
}