# ToDo.md — UniFutsal Manager

## M0 · Setup (fase actual)

### Entorno
- [x] T-001: Instalar Git y crear cuenta en GitHub (opcional pero recomendado)
- [x] T-002: Instalar .NET 8 SDK y verificar con `dotnet --version`
- [x] T-003: Instalar VS Code + extensiones C# Dev Kit
- [x] T-004: Instalar DB Browser for SQLite

### Estructura del proyecto
- [x] T-005: Crear carpeta `unifutsal-manager/` e inicializar repo Git
- [ ] T-006: Crear estructura de carpetas según Plan.md §4
- [ ] T-007: Crear solución `UniFutsal.sln` con los 4 proyectos .NET
- [ ] T-008: Configurar `UniFutsal.Core` como netstandard2.1
- [ ] T-009: Configurar `UniFutsal.Engine` como netstandard2.1
- [ ] T-010: Configurar `UniFutsal.Data` como netstandard2.1 + SQLite
- [ ] T-011: Configurar `UniFutsal.Cli` como net8.0
- [ ] T-012: Crear proyecto `UniFutsal.Tests` con xUnit

### Schema y datos
- [ ] T-013: Copiar DDL de `03-datos.md` a `data/migrations/000_init.sql`
- [ ] T-014: Implementar `unifutsal init-db` (crea la BD desde el DDL)
- [ ] T-015: Implementar `unifutsal validate` (las 7 queries de QA)
- [ ] T-016: Implementar `unifutsal import` (CSV básico de países)
- [ ] T-017: Crear CSV de prueba `data/csv/countries.csv` con 5 países

### Criterio de salida de M0
- [ ] `dotnet build` verde
- [ ] `dotnet test` verde
- [ ] `unifutsal init-db` crea la BD
- [ ] `unifutsal validate` reporta 0 errores
- [ ] `unifutsal import` carga los 5 países