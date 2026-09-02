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