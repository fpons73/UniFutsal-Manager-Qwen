namespace UniFutsal.Core.Domain.People
{
    public enum Gender { Male, Female }
    
    public enum PersonSource { Seed, Import, Generated, Youth }
    
    public enum Position { Goalkeeper, Defender, LeftWing, RightWing, Pivot, Universal }
    
    public enum PreferredFoot { Right, Left, Both }
    
    public enum StaffRole 
    { 
        Coach, Assistant, GoalkeepingCoach, FitnessCoach, Physio, 
        Psychologist, Scout, Analyst, SportingDirector, Doctor 
    }
}