using UniFutsal.Core.Domain;
using UniFutsal.Core.Domain.Geography;
using UniFutsal.Core.Domain.People;
using Xunit;

namespace UniFutsal.Tests.Domain
{
    public class WorldTests
    {
        [Fact]
        public void World_DefaultCollections_AreEmpty()
        {
            var world = new World();

            Assert.Empty(world.Clubs);
            Assert.Empty(world.Persons);
            Assert.Empty(world.Players);
            Assert.Empty(world.Competitions);
        }

        [Fact]
        public void World_IndexAll_AllowsLookup()
        {
            var world = new World();
            world.Countries.Add(new Country
            {
                Id = 1,
                Uid = "country-esp",
                Name = "Spain",
                Code3 = "ESP"
            });
            world.IndexAll();

            var country = world.GetCountryById(1);
            Assert.NotNull(country);
            Assert.Equal("Spain", country.Name);
        }

        [Fact]
        public void World_GetCountryById_ReturnsNull_WhenNotFound()
        {
            var world = new World();
            world.IndexAll();

            Assert.Null(world.GetCountryById(999));
        }

        [Fact]
        public void World_ResolveReferences_LinksCountryToConfederation()
        {
            var world = new World();
            var uefa = new Confederation { Id = 1, Code = "UEFA", Name = "UEFA" };
            var spain = new Country
            {
                Id = 1,
                Uid = "country-esp",
                Name = "Spain",
                Code3 = "ESP",
                ConfederationId = 1
            };
            world.Confederations.Add(uefa);
            world.Countries.Add(spain);
            world.IndexAll();
            world.ResolveReferences();

            Assert.NotNull(spain.Confederation);
            Assert.Equal("UEFA", spain.Confederation.Code);
        }
    }
}