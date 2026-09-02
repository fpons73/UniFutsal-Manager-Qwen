namespace UniFutsal.Core.Domain.Matches
{
    /// <summary>
    /// Estado del partido.
    /// </summary>
    public enum MatchStatus
    {
        Scheduled,  // programado
        Played,     // jugado
        Postponed,  // aplazado
        Cancelled,  // cancelado
        Walkover    // walkover
    }

    /// <summary>
    /// Lado del partido.
    /// </summary>
    public enum MatchSide
    {
        Home,
        Away
    }

    /// <summary>
    /// Tipo de evento de partido según 03-datos.md D5.
    /// </summary>
    public enum MatchEventType
    {
        Goal,
        Shot,
        Save,
        MissedChance,
        Foul,
        YellowCard,
        RedCard,
        TemporaryDismissal,
        ReturnFromDismissal,
        DoublePenalty,
        Penalty,
        Shootout,
        Timeout,
        Substitution,
        Injury,
        PowerPlayOn,
        PowerPlayOff,
        FlyingGoalkeeperOn,
        FlyingGoalkeeperOff,
        PeriodEnd,
        MatchEnd,
        Other
    }
}