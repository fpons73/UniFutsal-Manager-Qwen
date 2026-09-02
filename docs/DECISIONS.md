# DECISIONS.md — Decisiones técnicas registradas

> Registro de decisiones tomadas durante el desarrollo que no están cubiertas explícitamente en los documentos fuente.
> Formato: ## D-NNN — Título · Fecha · Contexto · Decisión · Justificación

## D-001 — Formato de solución `.slnx`
- **Fecha:** 2026-09-02
- **Contexto:** .NET 10 SDK crea soluciones en formato `.slnx` por defecto.
- **Decisión:** Usar `.slnx` en lugar del clásico `.sln`.
- **Justificación:** Es el estándar actual de .NET 10, compatible con build/test/VS Code/Unity. El Plan.md no exige un formato concreto.

## D-002 — `Microsoft.Data.Sqlite` para ejecutar DDL
- **Fecha:** 2026-09-02
- **Contexto:** Se necesita ejecutar un script SQL con múltiples statements.
- **Decisión:** Añadir `Microsoft.Data.Sqlite` a `UniFutsal.Data`.
- **Justificación:** `sqlite-net-pcl` no maneja bien scripts multi-statement. Se mantiene `sqlite-net-pcl` para ORM (mapeo objeto-tabla) según Plan.md §3. Regla 6 del plan cumplida.

## D-003 — Parseo CSV simple
- **Fecha:** 2026-09-02
- **Contexto:** Importador de países para M0.
- **Decisión:** Parseo simple con `Split(',')`, sin librería externa.
- **Justificación:** Los datos de M0 no contienen comas en los campos. Evita dependencias innecesarias (Regla 6). Si se necesita parseo complejo, se evaluará CsvHelper.

## D-004 — Position of table-level CHECK constraints in DDL
- **Fecha:** 2026-09-02
- **Contexto:** SQLite rechaza `CHECK (...)` sin nombre intercalado entre columnas (error "near signed_on: syntax error" en manager_contracts).
- **Decisión:** Mover las CHECK constraints de tabla (XOR null checks) al final de cada `CREATE TABLE`, después de todas las columnas.
- **Justificación:** Compatibilidad con el parser de SQLite. Las CHECKs column-level (ej. status IN ...) pueden ir junto a la columna; las table-level deben ir al final.

## D-005 — Separación Core/Engine: LeagueTable recibe goles crudos
- **Fecha:** 2026-09-02
- **Contexto:** `LeagueTable` (dominio, vive en `Core`) necesita registrar resultados generados por `InstantMatchSimulator` (vive en `Engine`).
- **Decisión:** `LeagueTable.RecordResult` recibe `(homeId, awayId, homeGoals, awayGoals)` crudos, no un `MatchOutcome`. El orquestador descompone el outcome antes de registrar.
- **Justificación:** Evita referencia circular `Core → Engine`. El dominio no debe conocer tipos del motor.
- **Alternativa descartada:** Mover `MatchOutcome` a `Core` (mezclaría motor y dominio).

## D-006 — Tests de SeasonSimulator: SafeDelete con GC.Collect
- **Fecha:** 2026-09-02
- **Contexto:** Tras `WorldLoader.Load()`, SQLite mantiene handles brevemente y `File.Delete` lanza `IOException` en los tests.
- **Decisión:** Helper `SafeDelete()` que hace `GC.Collect()` + `GC.WaitForPendingFinalizers()` + `try/catch(IOException)`.
- **Justificación:** Es el patrón estándar para tests con SQLite en archivos temporales. El SO limpia `/temp` eventualmente.
- **Alternativa descartada:** BD en memoria (`:memory:`) porque `CalendarGenerator` y `WorldLoader` abren conexiones separadas por path.

## D-007 — Observación de calibración v0: diferenciación de fuerza débil [DEUDA M4]
- **Fecha:** 2026-09-02
- **Contexto:** En la simulación v0, el campeón fue Sevilla FS (CA 115) en lugar del favorito teórico Madrid FS (CA 120). Goles/partido = 7.20 (extremo alto del rango).
- **Decisión:** ACEPTAR v0 para M1 (cumple rango y es determinista). Registrar como deuda de calibración para M4.
- **Justificación:** La fórmula `P_gol = base × (att/(att+def)) × 2.0` comprime las diferencias de rating a ~4% entre equipos. La varianza domina en 14 jornadas.
- **Plan para M4:** Amplificar diferencias (ej. `(att/def)^1.5`), ajustar `BASE_GOAL_PROB` a la baja para acercarse a 5.5-6.5 goles/partido. Verificar con tests golden de plausibilidad.

