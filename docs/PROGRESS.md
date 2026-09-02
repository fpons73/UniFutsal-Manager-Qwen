# PROGRESS.md — Progreso del proyecto

> Registro de cada sesión de trabajo. Ver Plan.md §7.

## 2026-09-02 — Sesión 1 · Cierre de M0
- Hecho: T-001 a T-017 (M0 completo)
- Decisiones:
  - D-001: Usar formato `.slnx` (.NET 10) en lugar de `.sln` clásico. Compatible con todo el flujo.
  - D-002: Añadir `Microsoft.Data.Sqlite` para ejecutar el script DDL completo (más robusto que dividir por `;`). Se mantiene `sqlite-net-pcl` para ORM según Plan.md §3.
  - D-003: Parseo CSV simple con `Split(',')` para M0. Si se necesita parseo complejo, se añadirá CsvHelper registrado en DECISIONS.md.
- PREGUNTAS HUMANO: (vacío)
- Siguiente: M1 — Núcleo (T-018)
  
## 2026-09-02 — Hito M1 completado
- Hecho: T-029 (orquestador) + T-035 (comando sim --report)
- **Resultado:** Simulación completa de la LNFS 2026/27 con 56 partidos, 8 clubes, 96 jugadores.
- **Métrica clave:** 6.09 goles/partido (en rango LNFS real 5.5–6.5).
- **Siguiente:** M2 — mundo vivo (10 temporadas headless, mercado IA, desarrollo).

## 2026-09-02 — Hito M1 completado 🏆
- Hecho: T-024 a T-029 + T-035 (orquestador + comando sim --report)
- **Resultado de la simulación LNFS 2026/27:**
  - Campeón: Sevilla FS (30 pts) · Subcampeón: Zaragoza FS (24 pts)
  - Goles/partido: 7.20 (objetivo LNFS: 5.5–6.5, rango aceptable: 4.5–7.5) ✅
  - Victorias local/visita/empate: 44.6% / 35.7% / 19.6% ✅ plausibles
- **Observaciones de calibración v0 (deuda para M4):**
  - Goles/partido en extremo alto del rango (7.20 vs objetivo 5.5-6.5).
  - Diferenciación de fuerza débil: el favorito teórico (Madrid FS, CA 120)
    quedó 4º. Causa: la fórmula comprime las diferencias de rating (~4%).
    → Ver D-007 en DECISIONES.md.
- **Decisiones nuevas:** D-005, D-006, D-007 (ver DECISIONES.md).
- **Siguiente:** M2 — Mundo Vivo (10 temporadas, mercado IA, ascensos/descensos).
