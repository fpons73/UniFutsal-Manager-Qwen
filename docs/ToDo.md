# ToDo.md — UniFutsal Manager

## M0 · Setup (fase actual)

### Entorno
- [x] T-001: Instalar Git y crear cuenta en GitHub (opcional pero recomendado)
- [x] T-002: Instalar .NET 8 SDK y verificar con `dotnet --version`
- [x] T-003: Instalar VS Code + extensiones C# Dev Kit
- [x] T-004: Instalar DB Browser for SQLite

### Estructura del proyecto
- [x] T-005: Crear carpeta `unifutsal-manager/` e inicializar repo Git
- [x] T-006: Crear estructura de carpetas según Plan.md §4
- [x] T-007: Crear solución `UniFutsal.sln` con los 4 proyectos .NET
- [x] T-008: Configurar `UniFutsal.Core` como netstandard2.1
- [x] T-009: Configurar `UniFutsal.Engine` como netstandard2.1
- [x] T-010: Configurar `UniFutsal.Data` como netstandard2.1 + SQLite
- [x] T-011: Configurar `UniFutsal.Cli` como net8.0
- [x] T-012: Crear proyecto `UniFutsal.Tests` con xUnit

### Schema y datos
- [x] T-013: Copiar DDL de `03-datos.md` a `data/migrations/000_init.sql`
- [x] T-014: Implementar `unifutsal init-db` (crea la BD desde el DDL)
- [x] T-015: Implementar `unifutsal validate` (las 7 queries de QA)
- [x] T-016: Implementar `unifutsal import` (CSV básico de países)
- [x] T-017: Crear CSV de prueba `data/csv/countries.csv` con 5 países

### Criterio de salida de M0
- [x] `dotnet build` verde
- [x] `dotnet test` verde
- [x] `unifutsal init-db` crea la BD
- [x] `unifutsal validate` reporta 0 errores
- [x] `unifutsal import` carga los 5 países

## M1 · Núcleo (fase activa)

### Modelo de dominio (el mundo en memoria)
- [x] T-018: Entidades geográficas en Core (Confederation, Country, Region, Venue)
- [x] T-019: Entidades de personas (Person, Player con atributos, Staff)
- [x] T-020: Entidades de clubes (Club, Contract)
- [x] T-021: Entidades de competiciones (Season, Competition, CompetitionEntry)
- [x] T-022: Entidades de partidos (Match, MatchResult)

### Datos de prueba (una liga mínima)
- [x] T-023: Ampliar CsvImporter (venues, clubs, people, competitions)
  - [x] T-023a: Importador de venues (venues.csv)
  - [x] T-023b: Importador de clubs (clubs.csv)
  - [x] T-023c: Importador de people (people.csv)
  - [x] T-023d: Importador de competitions (competitions.json)
- [x] T-024: Generar CSVs plausibles de una liga de prueba (8 clubes)
  - [x]-024a: Crear la clase World en Core
  - [x]-024b: Crear el WorldLoader en Data
  - [x]-024c: Test de Carta
  - [x]-024d: Comando CLI load-world para verificar
- [x] T-025: Cargar la liga y validar con `validate`

### Mundo en memoria
- [x] T-026: WorldLoader (carga la BD completa a objetos C#)
- [x] T-027: Tests de carga del mundo

### Calendario y simulación
- [ ] T-028: Generador de calendario round-robin (ida y vuelta)
- [ ] T-029: IRng + Xoshiro256** (determinismo)
- [ ] T-030: Simulador instantáneo (basado en fuerza de equipos)
- [ ] T-031: Tests golden de determinismo

### Clasificación y temporada
- [ ] T-032: Cálculo de clasificación (puntos, gol average)
- [ ] T-033: Orquestador de temporada (simular todas las jornadas)

### CLI y persistencia
- [ ] T-034: Comando `new-game`
- [ ] T-035: Comando `sim --seasons`
- [ ] T-036: Save/load del estado del mundo
- [ ] T-037: Test de roundtrip save/load