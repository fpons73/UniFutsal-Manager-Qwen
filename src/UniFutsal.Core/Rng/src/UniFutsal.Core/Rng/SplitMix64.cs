namespace UniFutsal.Core.Rng
{
    /// <summary>
    /// SplitMix64: mezclador usado para derivar seeds de Xoshiro256**.
    /// Convierte una seed de 64 bits en una secuencia de valores bien distribuidos.
    /// Referencia: Sebastiano Vigna, 2015.
    /// </summary>
    public static class SplitMix64
    {
        /// <summary>
        /// Genera el siguiente valor de la secuencia SplitMix64.
        /// Avanza el estado (por referencia) y devuelve el valor mezclado.
        /// </summary>
        public static ulong Next(ref ulong state)
        {
            state += 0x9E3779B97F4A7C15UL;
            ulong z = state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>
        /// Deriva las 4 seeds de estado (s0, s1, s2, s3) de Xoshiro256**
        /// a partir de una seed maestra de 64 bits.
        /// </summary>
        public static void DeriveXoshiroState(ulong masterSeed, out ulong s0, out ulong s1, out ulong s2, out ulong s3)
        {
            ulong state = masterSeed;
            s0 = Next(ref state);
            s1 = Next(ref state);
            s2 = Next(ref state);
            s3 = Next(ref state);

            // Xoshiro256** exige que no todo el estado sea cero.
            // SplitMix64 desde una seed cualquiera ya lo garantiza prácticamente siempre,
            // pero por seguridad forzamos s0 a no ser cero.
            if (s0 == 0 && s1 == 0 && s2 == 0 && s3 == 0)
            {
                s0 = 0x12345678ABCDEF01UL;
            }
        }
    }
}