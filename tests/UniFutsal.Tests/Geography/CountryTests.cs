using UniFutsal.Core.Domain.Geography;
using Xunit;

namespace UniFutsal.Tests.Geography
{
    public class CountryTests
    {
        [Fact]
        public void Country_DefaultValues_AreReasonable()
        {
            var country = new Country();

            Assert.Equal(50.0, country.FutsalReputation);
            Assert.Equal(string.Empty, country.Uid);
            Assert.Equal(string.Empty, country.Name);
        }

        [Fact]
        public void Country_CanLinkConfederation()
        {
            var uefa = new Confederation { Id = 1, Code = "UEFA", Name = "UEFA" };
            var spain = new Country
            {
                Id = 1,
                Uid = "country-esp",
                Name = "Spain",
                Code3 = "ESP",
                Confederation = uefa
            };

            Assert.Equal("UEFA", spain.Confederation.Code);
        }
    }
}