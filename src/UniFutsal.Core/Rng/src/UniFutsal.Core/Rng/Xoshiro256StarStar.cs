using System;

namespace UniFutsal.Core.Rng
{
    /// <summary>
    /// Xoshiro256** — PRNG de 256 bits con período 2^256 − 1.
    /// Implementación 100% determinista: sin Math.*, solo operaciones enteras.
    /// Referencia: Blackman & Vigna, "Scrambled Linear Pseudorandom Number Generators", 2021.
    /// </summary>
    public sealed class Xoshiro256StarStar : IRng
    {
        private ulong _s0;
        private ulong _s1;
        private ulong _s2;
        private ulong _s3;

        /// <summary>
        /// Construye un PRNG a partir de una seed maestra de 64 bits.
        /// Usa SplitMix64 para derivar el estado interno de 256 bits.
        /// </summary>
        public Xoshiro256StarStar(ulong masterSeed)
        {
            SplitMix64.DeriveXoshiroState(masterSeed, out _s0, out _s1, out _s2, out _s3);
        }

        /// <summary>
        /// Construye un PRNG a partir de un seed entero (convierte a ulong).
        /// Útil para rng_seed de la BD.
        /// </summary>
        public Xoshiro256StarStar(long masterSeed)
            : this(unchecked((ulong)masterSeed))
        {
        }

        /// <summary>
        /// Constructor privado para Fork (con estado derivado).
        /// </summary>
        private Xoshiro256StarStar(ulong s0, ulong s1, ulong s2, ulong s3)
        {
            _s0 = s0;
            _s1 = s1;
            _s2 = s2;
            _s3 = s3;
        }

        /// <inheritdoc />
        public ulong NextULong()
        {
            // Función de salida "**" (star-star): multiplicación + rotación + multiplicación
            ulong result = RotateLeft(_s1 * 5, 7) * 9;

            // Transición de estado (xoshiro)
            ulong t = _s1 << 17;
            _s2 ^= _s0;
            _s3 ^= _s1;
            _s1 ^= _s2;
            _s0 ^= _s3;
            _s2 ^= t;
            _s3 = RotateLeft(_s3, 45);

            return result;
        }

        /// <inheritdoc />
        public double NextDouble()
        {
            // Convierte 53 bits (precisión de double IEEE-754) a [0, 1).
            // 0x1.0p-53 = 1 / 2^53
            return (NextULong() >> 11) * (1.0 / (1UL << 53));
        }

        /// <inheritdoc />
        public double NextDouble(double minInclusive, double maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentException($"maxExclusive ({maxExclusive}) debe ser > minInclusive ({minInclusive}).");
            }
            return minInclusive + NextDouble() * (maxExclusive - minInclusive);
        }

        /// <inheritdoc />
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentException($"maxExclusive ({maxExclusive}) debe ser > minInclusive ({minInclusive}).");
            }
            long range = (long)maxExclusive - (long)minInclusive;

            // Rechazo uniforme: descartar valores que causarían sesgo.
            // Esto garantiza distribución perfectamente uniforme sin Math.*.
            ulong maxAcceptable = (ulong.MaxValue / (ulong)range) * (ulong)range;
            ulong r;
            do
            {
                r = NextULong();
            } while (r >= maxAcceptable);

            return minInclusive + (int)(r % (ulong)range);
        }

        /// <inheritdoc />
        public bool Chance(double probability)
        {
            if (probability <= 0.0) return false;
            if (probability >= 1.0) return true;
            return NextDouble() < probability;
        }

        /// <inheritdoc />
        public IRng Fork(ulong substreamKey)
        {
            // Derivamos un nuevo estado mezclando el estado actual con la clave del substream.
            // Esto garantiza que dos Fork con claves distintas son independientes entre sí.
            ulong combined = _s0 ^ _s1 ^ _s2 ^ _s3 ^ substreamKey;
            SplitMix64.DeriveXoshiroState(
                combined,
                out ulong ns0, out ulong ns1, out ulong ns2, out ulong ns3);

            // Aseguramos que el PRNG padre avance para que futuras llamadas al padre
            // no colisionen con las del hijo.
            NextULong();

            return new Xoshiro256StarStar(ns0, ns1, ns2, ns3);
        }

        private static ulong RotateLeft(ulong x, int k)
        {
            return (x << k) | (x >> (64 - k));
        }
    }
}