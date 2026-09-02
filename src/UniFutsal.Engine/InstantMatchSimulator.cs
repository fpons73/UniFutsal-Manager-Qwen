using System;
using System.Collections.Generic;
using UniFutsal.Core.Domain;
using UniFutsal.Core.Domain.Clubs;
using UniFutsal.Core.Domain.Matches;
using UniFutsal.Core.Domain.People;
using UniFutsal.Core.Rng;

namespace UniFutsal.Engine
{
    /// <summary>
    /// Simulador instantáneo de partidos (modo "resultado instantáneo").
    /// Determinista: misma seed → mismo resultado bit a bit.
    ///
    /// Algoritmo v0 (afinación §15 de 05-motor.md):
    /// - Calcula rating promedio (CA) de los jugadores de cada equipo.
    /// - Aplica ventaja local (+1.5% al local).
    /// - Genera ~15 ataques por equipo por periodo (4 periodos) = ~120 ataques totales.
    /// - Cada ataque: P_gol = base × (rating_att / (rating_att + rating_def)) × modificador.
    /// - Objetivo: ~5.5–6.5 goles/partido (LNFS real).
    ///
    /// Sin Math.Pow/Sin/Exp/Log (determinismo sagrado, Plan.md §10.1).
    /// </summary>
    public sealed class InstantMatchSimulator
    {
        // ===== Constantes de calibración v0 =====
        private const double HOME_ADVANTAGE_PCT = 0.015;
        private const int ATTACKS_PER_TEAM = 60;      // ~60 ataques por equipo por partido
        private const double BASE_GOAL_PROB = 0.055;  // 5.5% base por ataque → ~6.6 goles/partido

        private readonly World _world;

        public InstantMatchSimulator(World world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
        }

        /// <summary>
        /// Simula un partido y devuelve el resultado final.
        /// Si allowPenalties=true y hay empate, resuelve por tanda.
        /// </summary>
        public MatchOutcome Simulate(Match match, bool allowPenalties = false)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            if (!match.HomeClubId.HasValue || !match.AwayClubId.HasValue)
            {
                throw new ArgumentException("El partido debe tener HomeClubId y AwayClubId (no selección).");
            }

            var homeClub = _world.GetClubById(match.HomeClubId.Value);
            var awayClub = _world.GetClubById(match.AwayClubId.Value);
            if (homeClub == null) throw new ArgumentException($"Club local {match.HomeClubId} no encontrado.");
            if (awayClub == null) throw new ArgumentException($"Club visitante {match.AwayClubId} no encontrado.");

            var homePlayers = _world.GetPlayersByClub(homeClub.Id);
            var awayPlayers = _world.GetPlayersByClub(awayClub.Id);

            double homeRating = ComputeTeamRating(homePlayers);
            double awayRating = ComputeTeamRating(awayPlayers);

            // Ventaja local: +1.5% al rating del local
            double effectiveHomeRating = homeRating * (1.0 + HOME_ADVANTAGE_PCT);
            double effectiveAwayRating = awayRating;

            // RNG determinista con la seed del partido
            var rng = new Xoshiro256StarStar(match.RngSeed);

            int homeGoals = GenerateGoals(rng, effectiveHomeRating, effectiveAwayRating);
            int awayGoals = GenerateGoals(rng, effectiveAwayRating, effectiveHomeRating);

            var outcome = new MatchOutcome
            {
                HomeScore = homeGoals,
                AwayScore = awayGoals,
                RngSeed = match.RngSeed,
                HomeRating = homeRating,
                AwayRating = awayRating
            };

            // Tanda de penaltis si hay empate y se permite
            if (allowPenalties && homeGoals == awayGoals)
            {
                ResolvePenaltyShootout(rng, effectiveHomeRating, effectiveAwayRating, outcome);
            }

            return outcome;
        }

        /// <summary>
        /// Calcula el rating promedio del equipo (CA de los jugadores en plantilla).
        /// Si no hay jugadores, devuelve un rating mínimo para no dividir por cero.
        /// </summary>
        private double ComputeTeamRating(List<Player> players)
        {
            if (players == null || players.Count == 0) return 5.0;

            long sum = 0;
            int count = 0;
            foreach (var p in players)
            {
                // Solo jugadores no retirados
                if (!p.Retired)
                {
                    sum += p.CurrentAbility;
                    count++;
                }
            }

            if (count == 0) return 5.0;

            // CA está en escala 1-200, convertimos a escala 1-20 (dividir por 10)
            return (double)sum / count / 10.0;
        }

        /// <summary>
        /// Genera los goles de un equipo en base a su rating vs el del rival.
        /// Fórmula v0: P_gol por ataque = BASE_GOAL_PROB × (rating_att / (rating_att + rating_def))
        /// Sin Math.*, solo suma/multiplicación/división.
        /// </summary>
        private int GenerateGoals(IRng rng, double attackerRating, double defenderRating)
        {
            // Probabilidad por ataque, acotada a [0.01, 0.15] para evitar extremos
            double baseProb = BASE_GOAL_PROB;
            double ratioAtt = attackerRating / (attackerRating + defenderRating);
            double prob = baseProb * ratioAtt * 2.0; // multiplicador 2 para llegar a ~6 goles/partido

            if (prob < 0.01) prob = 0.01;
            if (prob > 0.15) prob = 0.15;

            int goals = 0;
            for (int i = 0; i < ATTACKS_PER_TEAM; i++)
            {
                if (rng.Chance(prob))
                {
                    goals++;
                }
            }

            return goals;
        }

        /// <summary>
        /// Resuelve tanda de penaltis (5 lanzamientos + muerte súbita).
        /// P_gol por lanzamiento ≈ 62% (FM/Futsal estándar).
        /// </summary>
        private void ResolvePenaltyShootout(IRng rng, double homeRating, double awayRating, MatchOutcome outcome)
        {
            int homePens = 0;
            int awayPens = 0;

            // 5 lanzamientos por equipo
            for (int i = 0; i < 5; i++)
            {
                if (rng.Chance(PenaltyProb(homeRating))) homePens++;
                if (rng.Chance(PenaltyProb(awayRating))) awayPens++;
            }

            // Muerte súbita si sigue empate
            int suddenDeathRounds = 0;
            while (homePens == awayPens && suddenDeathRounds < 20) // límite de seguridad
            {
                int h = rng.Chance(PenaltyProb(homeRating)) ? 1 : 0;
                int a = rng.Chance(PenaltyProb(awayRating)) ? 1 : 0;
                homePens += h;
                awayPens += a;
                suddenDeathRounds++;
                if (h != a) break;
            }

            outcome.HomePenalties = homePens;
            outcome.AwayPenalties = awayPens;
        }

        private static double PenaltyProb(double rating)
        {
            // Rating 5-20 → prob 0.55-0.75
            // Fórmula lineal: 0.55 + (rating-5)/15 * 0.20
            double norm = (rating - 5.0) / 15.0;
            if (norm < 0.0) norm = 0.0;
            if (norm > 1.0) norm = 1.0;
            return 0.55 + norm * 0.20;
        }
    }
}