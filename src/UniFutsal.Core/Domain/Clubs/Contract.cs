using System;
using UniFutsal.Core.Domain.People;

namespace UniFutsal.Core.Domain.Clubs
{
    /// <summary>
    /// Contrato entre una persona (jugador/staff) y un club.
    /// </summary>
    public class Contract
    {
        public long Id { get; set; }
        public long PersonId { get; set; }
        public long ClubId { get; set; }
        public ContractScope Scope { get; set; } = ContractScope.FirstTeam;

        // Fechas (ISO-8601)
        public DateTime SignedOn { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveUntil { get; set; }

        // Términos económicos
        public int WageMonthly { get; set; }
        public int? ReleaseClause { get; set; }
        public int? SquadNumber { get; set; }

        // JSON de bonos: {"por_gol":500,"por_asistencia":250,...}
        public string BonusJson { get; set; } = "{}";

        // Agente
        public long? AgentId { get; set; }
        public int? AgentFee { get; set; }

        public NegotiatedBy? NegotiatedBy { get; set; }
        public ContractStatus Status { get; set; } = ContractStatus.Active;

        // Referencias resueltas
        public Person? Person { get; set; }
        public Club? Club { get; set; }
    }
}