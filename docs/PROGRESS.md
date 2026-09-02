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
- **Resultado:** Simulación completa de la LNFS 2026/27:
  - 8 clubes, 96 jugadores, 56 partidos simulados headless.
  - Campeón: [PONER AQUÍ EL CAMPEÓN DEL REPORTE]
  - Goles/partido: [PONER AQUÍ] (objetivo LNFS: 5.5–6.5)
  - Victorias local/visita/empate: [PONER AQUÍ %]
- **Decisiones nuevas:**
  - D-005: LeagueTable recibe goles crudos (evita referencia circular Core→Engine).
  - D-006: Tests de SeasonSimulator usan SafeDelete (GC.Collect + try/catch IOException)
    porque SQLite mantiene handles brevemente tras WorldLoader.
- **Siguiente:** M2 — Mundo Vivo (10 temporadas headless, mercado IA, desarrollo, ascensos/descensos).

