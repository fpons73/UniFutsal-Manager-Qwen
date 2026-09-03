# ToDo.md — UniFutsal Manager

## M0 — Setup ✅ COMPLETADO
- [x] T-001 a T-017: Estructura de proyectos, schema SQLite (59 tablas), CLI base, validador, importadores de países, git, GitHub

## M1 — Núcleo ✅ COMPLETADO (2026-09-02)
- [x] T-018: Entidades geográficas en Core
- [x] T-019: Entidades de personas
- [x] T-020: Entidades de clubes
- [x] T-021: Entidades de competiciones
- [x] T-022: Entidades de partidos
- [x] T-023: Importadores CSV (venues, clubs, people, contracts, seasons, competitions, entries)
- [x] T-024: WorldLoader + comando `load-world`
- [x] T-025: Generador de calendario round-robin (ida y vuelta)
- [x] T-026: IRng + Xoshiro256** (determinismo sagrado)
- [x] T-027: Simulador instantáneo (basado en fuerza de equipos)
- [x] T-028: Cálculo de clasificación (puntos, gol average)
- [x] T-029: Orquestador de temporada + comando `sim --report`
- **Criterio M1 cumplido:** 1 temporada simulada headless, determinista, calibrada (7.20 goles/partido)

## M2 — Mundo Vivo (FASE ACTIVA)
- [x] T-030: advance-season (avance temporal + nueva temporada + calendario)
- [x] T-031: Desarrollo de jugadores (envejecimiento + mejora/declive por edad)
- [x] T-032: Retiradas y fin de contratos
- [x] T-032b: Contratos con duraciones variables (1-5 años)
- [x] T-033: 2ª División + ascensos/descensos (competition_links)
- [x] T-034: Mercado de fichajes IA (reemplazo de retirados/expirados)
- [ ] T-035: Bucle multi-temporada (simular 10 años + evolución histórica)
- [ ] T-036: Save/load del estado del mundo
- [ ] T-037: Test de roundtrip save/load
- **Criterio M2:** 10 temporadas consecutivas, mercado IA, ascensos/descensos funcionales