using UniFutsal.Core.Domain.People;
using Xunit;
using System;

namespace UniFutsal.Tests.People
{
    public class PersonTests
    {
        [Fact]
        public void Player_DefaultAttributes_AreCorrect()
        {
            var player = new Player();
            
            Assert.Equal(10, player.T_Passing);
            Assert.Equal(10, player.M_Vision);
            Assert.Equal(10, player.P_Pace);
            Assert.Equal(10, player.H_Temperament);
            
            Assert.Equal(1, player.G_Reflexes);
            Assert.Equal(1, player.G_ShotStopping);
        }

        [Fact]
        public void Person_CanBeLinkedToPlayer()
        {
            var person = new Person
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                BirthDate = new DateTime(2000, 1, 1)
            };
            
            var player = new Player { PersonId = 1, Person = person };

            Assert.Equal("John", player.Person.FirstName);
        }

        [Fact]
        public void Enums_HaveCorrectValues()
        {
            Assert.Equal(Position.Pivot, Position.Pivot);
            Assert.Equal(PreferredFoot.Both, PreferredFoot.Both);
            Assert.Equal(StaffRole.Scout, StaffRole.Scout);
        }
    }
}