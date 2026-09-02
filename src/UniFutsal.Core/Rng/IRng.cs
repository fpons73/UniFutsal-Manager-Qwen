namespace UniFutsal.Core.Rng
{
    /// <summary>
    /// Interfaz del generador de números aleatorios del juego.
    /// TODA aleatoriedad del núcleo y motor debe pasar por aquí.
    /// Prohibido usar System.Random, DateTime.Now, etc. directamente (Plan.md §10.1).
    /// </summary>
    public interface IRng
    {
        /// <summary>
        /// Devuelve un ulong aleatorio en todo el rango [0, 2^64).
        /// </summary>
        ulong NextULong();

        /// <summary>
        /// Devuelve un double uniforme en [0, 1).
        /// </summary>
        double NextDouble();

        /// <summary>
        /// Devuelve un entero en [minInclusive, maxExclusive).
        /// </summary>
        int NextInt(int minInclusive, int maxExclusive);

        /// <summary>
        /// Devuelve true con la probabilidad indicada (0.0 a 1.0).
        /// </summary>
        bool Chance(double probability);

        /// <summary>
        /// Devuelve un double en [minInclusive, maxExclusive).
        /// </summary>
        double NextDouble(double minInclusive, double maxExclusive);

        /// <summary>
        /// Crea un sub-stream determinista (para aislar fuentes de aleatoriedad).
        /// Ej: uno para decisiones, otro para árbitro, etc.
        /// </summary>
        IRng Fork(ulong substreamKey);
    }
}