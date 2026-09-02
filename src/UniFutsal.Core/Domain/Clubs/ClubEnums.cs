namespace UniFutsal.Core.Domain.Clubs
{
    /// <summary>
    /// Patrón de la camiseta del club.
    /// </summary>
    public enum KitPattern
    {
        Solid,      // Liso
        Stripes,    // A rayas
        Halved,     // Mitad y mitad
        Sash        // Banda diagonal
    }

    /// <summary>
    /// Ámbito del contrato.
    /// </summary>
    public enum ContractScope
    {
        FirstTeam,      // primer_equipo
        Youth,          // cantera
        Staff           // staff
    }

    /// <summary>
    /// Estado del contrato.
    /// </summary>
    public enum ContractStatus
    {
        Active,         // vigente
        Renewed,        // renovado
        Terminated,     // rescindido
        Expired,        // expirado
        Loan            // cesion
    }

    /// <summary>
    /// Quién negoció el contrato.
    /// </summary>
    public enum NegotiatedBy
    {
        Manager,            // manager
        Director,           // directivo
        ExternalAgent       // agente_exterior
    }
}