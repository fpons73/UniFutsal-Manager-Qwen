using System;
using UniFutsal.Core.Domain.Geography;

namespace UniFutsal.Core.Domain.People
{
    public class Person
    {
        public long Id { get; set; }
        public string Uid { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? CommonName { get; set; }
        public Gender Gender { get; set; } = Gender.Male;
        public DateTime BirthDate { get; set; }
        public string? BirthCity { get; set; }
        public long? BirthCountryId { get; set; }
        public long NationalityId { get; set; }
        public long? SecondNationalityId { get; set; }
        public int? HeightCm { get; set; }
        public int? WeightKg { get; set; }
        public string? PersonalityKey { get; set; }
        public PersonSource Source { get; set; } = PersonSource.Seed;

        public Country? Nationality { get; set; }
        public Country? BirthCountry { get; set; }
    }

    public class Player
    {
        public long PersonId { get; set; }
        public Position PositionMain { get; set; }
        public Position? PositionSecondary { get; set; }
        public PreferredFoot PreferredFoot { get; set; } = PreferredFoot.Right;
        public int WeakFoot { get; set; } = 3;
        public int CurrentAbility { get; set; }
        public int PotentialAbility { get; set; }

        #region Technical (11)
        public int T_Control { get; set; } = 10;
        public int T_Dribbling { get; set; } = 10;
        public int T_Passing { get; set; } = 10;
        public int T_OneTouchPass { get; set; } = 10;
        public int T_Finishing { get; set; } = 10;
        public int T_LongShots { get; set; } = 10;
        public int T_Technique { get; set; } = 10;
        public int T_PostPlay { get; set; } = 10;
        public int T_Tackling { get; set; } = 10;
        public int T_Interceptions { get; set; } = 10;
        public int T_Blocking { get; set; } = 10;
        #endregion

        #region Goalkeeper (8)
        public int G_ShotStopping { get; set; } = 1;
        public int G_Reflexes { get; set; } = 1;
        public int G_OneOnOne { get; set; } = 1;
        public int G_Kicking { get; set; } = 1;
        public int G_Distribution { get; set; } = 1;
        public int G_Positioning { get; set; } = 1;
        public int G_RushingOut { get; set; } = 1;
        public int G_Outfield { get; set; } = 1;
        #endregion

        #region Mental (11)
        public int M_Vision { get; set; } = 10;
        public int M_Decisions { get; set; } = 10;
        public int M_Anticipation { get; set; } = 10;
        public int M_Concentration { get; set; } = 10;
        public int M_Positioning { get; set; } = 10;
        public int M_Aggression { get; set; } = 10;
        public int M_Composure { get; set; } = 10;
        public int M_Leadership { get; set; } = 10;
        public int M_Teamwork { get; set; } = 10;
        public int M_WorkRate { get; set; } = 10;
        public int M_Bravery { get; set; } = 10;
        #endregion

        #region Physical (8)
        public int P_Acceleration { get; set; } = 10;
        public int P_Pace { get; set; } = 10;
        public int P_Agility { get; set; } = 10;
        public int P_Balance { get; set; } = 10;
        public int P_Coordination { get; set; } = 10;
        public int P_Stamina { get; set; } = 10;
        public int P_Strength { get; set; } = 10;
        public int P_Jumping { get; set; } = 10;
        #endregion

        #region Hidden (4)
        public int H_Consistency { get; set; } = 10;
        public int H_InjuryProneness { get; set; } = 10;
        public int H_Dirtiness { get; set; } = 10;
        public int H_Temperament { get; set; } = 10;
        #endregion

        public bool Retired { get; set; } = false;
        public Person? Person { get; set; }
    }

    public class Staff
    {
        public long PersonId { get; set; }
        public StaffRole Role { get; set; }

        #region Coaching Attributes
        public int Coaching_Technical { get; set; } = 10;
        public int Coaching_Attacking { get; set; } = 10;
        public int Coaching_Defensive { get; set; } = 10;
        public int Coaching_Goalkeepers { get; set; } = 10;
        public int Coaching_Fitness { get; set; } = 10;
        public int Coaching_Tactical { get; set; } = 10;
        public int Medicine { get; set; } = 10;
        #endregion

        #region Hidden / Mental
        public int H_JudgingAbility { get; set; } = 10;
        public int H_JudgingPotential { get; set; } = 10;
        public int Motivation { get; set; } = 10;
        public int DressroomManagement { get; set; } = 10;
        public int Negotiation { get; set; } = 10;
        public int Adaptability { get; set; } = 10;
        #endregion

        public bool Retired { get; set; } = false;
        public Person? Person { get; set; }
    }

    public class Referee
    {
        public long PersonId { get; set; }
        public long? CountryId { get; set; }
        public int Strictness { get; set; } = 10;
        public double BigMatchRating { get; set; } = 50.0;

        public Person? Person { get; set; }
        public Country? Country { get; set; }
    }
}