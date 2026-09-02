using UniFutsal.Core.Domain.Clubs;
using Xunit;
using System;

namespace UniFutsal.Tests.Clubs
{
    public class ClubTests
    {
        [Fact]
        public void Club_DefaultValues_AreReasonable()
        {
            var club = new Club();

            Assert.Equal("#E63946", club.PrimaryColor);
            Assert.Equal("#FFFFFF", club.SecondaryColor);
            Assert.Equal(KitPattern.Solid, club.KitPattern);
            Assert.Equal(40.0, club.Reputation);
            Assert.Equal(10, club.TrainingFacilities);
            Assert.True(club.IsActive);
        }

        [Fact]
        public void Club_CanSetColors()
        {
            var club = new Club
            {
                Name = "Inter FS",
                PrimaryColor = "#0055A5",
                SecondaryColor = "#FFFFFF",
                KitPattern = KitPattern.Stripes
            };

            Assert.Equal("#0055A5", club.PrimaryColor);
            Assert.Equal(KitPattern.Stripes, club.KitPattern);
        }

        [Fact]
        public void Contract_DefaultValues_AreCorrect()
        {
            var contract = new Contract();

            Assert.Equal(ContractScope.FirstTeam, contract.Scope);
            Assert.Equal(ContractStatus.Active, contract.Status);
            Assert.Equal("{}", contract.BonusJson);
        }

        [Fact]
        public void Contract_CanLinkToClubAndPerson()
        {
            var club = new Club { Id = 1, Name = "Test Club" };
            var contract = new Contract
            {
                ClubId = 1,
                Club = club,
                WageMonthly = 5000
            };

            Assert.Equal("Test Club", contract.Club.Name);
            Assert.Equal(5000, contract.WageMonthly);
        }
    }
}