namespace UniFutsal.Core.Domain.Competitions
{
    /// <summary>
    /// Ámbito de la competición.
    /// </summary>
    public enum CompetitionScope
    {
        Club,           // club
        NationalTeam    // seleccion
    }

    /// <summary>
    /// Tipo de competición.
    /// </summary>
    public enum CompetitionType
    {
        League,         // liga (round-robin)
        Cup             // copa (eliminatorias)
    }

    /// <summary>
    /// Formato de una fase.
    /// </summary>
    public enum PhaseFormat
    {
        RoundRobin,     // round_robin
        Knockout,       // knockout
        MiniTournament, // mini_torneo
        FinalFour       // final_four
    }

    /// <summary>
    /// Tipo de enlace entre competiciones (ascensos, descensos, plazas).
    /// </summary>
    public enum LinkType
    {
        Promotion,      // ascenso
        Relegation,     // descenso
        Qualification,  // clasificacion
        Playoff,        // repesca
        Withdrawal      // baja
    }

    /// <summary>
    /// Estado de una inscripción.
    /// </summary>
    public enum EntryStatus
    {
        Active,         // activo
        Eliminated,     // eliminado
        Withdrawn,      // retirado
        Sanctioned      // sancionado
    }
}