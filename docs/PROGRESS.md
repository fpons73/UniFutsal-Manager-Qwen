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
