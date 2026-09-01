

````markdown
# plan.md — UniFutsal Manager · Plan Maestro de Ejecución (v1.0)

> **Tu rol:** implementador. **El rol del humano:** product owner (valida en Unity, aporta datos reales, decide bloqueos).
> **Regla nº 1:** este documento + `ToDo.md` + los 5 documentos fuente (§2) son LA LEY. Si algo no está cubierto, aplica los defaults y regístralo en `DECISIONS.md`.

---

## 0. Reglas de oro (contrato del agente)

1. **Trabaja una tarea a la vez**, en orden, según `ToDo.md`. No saltes, no adelantes fases.
2. **Nunca dejes el build roto.** Cada tarea termina con: build ✅ + tests ✅ + `validate` ✅ (§8).
3. **Todo cambio de schema** se hace por migración numerada (`migrations/NNN_*.sql` + `PRAGMA user_version`) y actualizando el doc fuente. Prohibido ALTER destructivo sin nota de backup.
4. **Determinismo sagrado** (§10.1). Un solo fallo aquí invalida el motor de partidos.
5. **Cero scope creep.** ¿Se te ocurre una mejora? → una línea en `Backlog.md`, sigues con tu tarea.
6. **Prohibido añadir dependencias NuGet/Unity Packages nuevas** sin anotarlo en `DECISIONS.md` con justificación.
7. **Código, identificadores y nombres de test en inglés** · comentarios, commits y documentación en español.
8. Formato de commit: `T-XXX: resumen breve`.
9. Al terminar cada sesión de trabajo: actualiza `ToDo.md` (checkboxes + notas) y añade entrada en `PROGRESS.md` (§7).
10. Si una duda bloquea >30 min → anótala en `PREGUNTAS HUMANO` dentro de `PROGRESS.md`, elige la opción más coherente con el PRD, continúa, y regístrala en `DECISIONS.md`.

---

## 1. El producto en 60 segundos

- **UniFutsal Manager**: simulador de gestión de fútbol sala estilo Football Manager, uso **personal y offline**, con **nombres reales** (no se distribuye).
- **Mundo vivo:** 22 ligas (20 países, pirámide española de 3 niveles), 20 copas nacionales, UEFA Futsal Champions League y 7 torneos de selecciones con clasificatorios (las 6 confederaciones). Todo siempre simulado (Q7 del PRD).
- **Doble rol:** manager de club + selección nacional simultáneos (RF-806…811).
- **Motor de partidos:** una simulación → tres presentadores (texto · 2D chapas · instantáneo). Sin 3D.
- **Editor de datos integrado** en el juego (no herramienta externa), con import/export `.fmpack` e importación masiva CSV.
- Stack: **Unity 6 LTS (solo UI)** + **núcleo C# puro headless** + **SQLite** + **MessagePack**.

**Fuera de alcance (NO construir):** 3D, multijugador, móvil, Steam/Workshop, telemetría, fútbol 11, comentarista de audio.

---

## 2. Documentos fuente (en `docs/`)

| Archivo | Contenido | Consúltalo cuando… |
| :--- | :--- | :--- |
| `01-PRD.md` | Producto, requisitos RF/RNF, 51 competiciones, ciclos, volúmenes | Dudas de alcance, reglas de juego, prioridades |
| `02-estilos.md` | Libro de Estilos: tokens, componentes, motion, voz, accesibilidad | Cualquier trabajo de UI en Unity |
| `03-datos.md` | Modelo SQLite completo (59 tablas, DDL, índices, vistas, estrategia de persistencia, CSV) | Cualquier schema, query o importador |
| `04-wireframes.md` | WF-00…WF-05 (shell, inbox, plantilla, perfil, tácticas, match HUD) | Construcción de pantallas |
| `05-motor.md` | Especificación UME: pipeline, acciones, reglas, eventos, calibración | Fase M4 completa |
| `plan.md` / `ToDo.md` | Este documento / cola de tareas | Siempre |
| `DECISIONS.md`, `PROGRESS.md`, `Backlog.md`, `PERF.md` | Se crean en M0 | Continuamente |

**Prioridad ante conflicto:** PRD > Datos > Motor > Estilos > Wireframes.

---

## 3. Decisiones técnicas fijas (no renegociar)

| Área | Decisión |
| :--- | :--- |
| Núcleo | Librería **C# pura sin dependencia de Unity**, target **`netstandard2.1`** (compilable en .NET 8 SDK y referenciable por Unity) |
| Harness | CLI `net8.0` (`UniFutsal.Cli`) para todo el trabajo headless: importar, simular, calibrar, validar |
| Tests | **xUnit** (`UniFutsal.Tests`), se ejecutan con `dotnet test` — NUNCA se requiere Unity para testear |
| BD | **SQLite** con `sqlite-net-pcl` + `SQLitePCLRaw.bundle_green`; DDL de `03-datos.md` como recurso embebido |
| Saves | Estado del mundo → **MessagePack**; snapshot atómico vía **SQLite Backup API**; autosave rotativo |
| RNG | **Xoshiro256\*\*** sembrado con SplitMix64; 4 substreams (decisiones/resolución/árbitro/lesiones) |
| Config | `data/packs/engine.json` (constantes del motor) y `competitions.json` (competiciones) — todo data-driven |
| Unity | **Unity 6 LTS**, solo UI Toolkit (UXML/USS/TSS). Proyecto en `src/UniFutsal.Unity`, DLLs del núcleo vía `asmdef` + script de copia post-build |
| Fase headless (M0–M2 + M4-core) | **Prohibido abrir Unity.** Unity solo entra en M3 y en la capa visual de M4 |

---

## 4. Estructura del repositorio

```
unifutsal-manager/
├── docs/                      # los 5 documentos + plan.md + ToDo.md + estados
├── src/
│   ├── UniFutsal.Core/        # netstandard2.1 · dominio, calendario, mundo, desarrollo, mercado
│   ├── UniFutsal.Engine/      # netstandard2.1 · UME (match engine) — aislado de Core
│   ├── UniFutsal.Data/        # netstandard2.1 · SQLite, migraciones, importadores CSV/JSON, packs
│   ├── UniFutsal.Cli/         # net8.0 · harness headless
│   └── UniFutsal.Unity/       # proyecto Unity 6 (solo UI Toolkit) — se crea en M3
├── tests/
│   └── UniFutsal.Tests/       # xUnit · cubre Core, Data y Engine
├── data/
│   ├── csv/                   # countries.csv, people.csv, clubs.csv, venues.csv…
│   ├── packs/                 # competitions.json, engine.json, packs del editor
│   └── migrations/            # 000_init.sql, 001_*.sql…
└── tools/                     # generadores de datos plausibles, scripts
```

---

## 5. Arquitectura (resumen operativo)

- **Capas:** `Data` (persistencia) → `Core` (dominio/mundo) → `Engine` (partidos) → `Cli`/`Unity` (presentación). Prohibido que Core/Engine referencien Unity o la UI.
- **Flujo "Continuar":** la UI/Cli invoca `WorldController.Continue()` → el núcleo procesa el tick (día/semana): partidos del día → resultados → tablas → eventos de mundo (mercado, desarrollo, inbox) → persiste. RNF-08: semana completa del mundo < 2 s.
- **Una simulación, tres presentadores:** el motor genera `match_events` (+ keyframes en `detail_json.kf` cuando `full_events=1`). Texto, 2D e instantáneo son solo filtros/renderizadores del stream.
- **Persistencia:** `unifutsal_base.db` (solo lectura, incluida) → al crear partida se copia a `saves/save_NNN.db` → el núcleo trabaja sobre esa copia.
- **Unity ↔ Núcleo:** la UI solo habla con un `IGameController` (fachada) y ViewModels. Nunca consulta SQLite directamente.

---

## 6. Hitos y criterios de salida

| Hito | Contenido | Criterio de salida (DoD de fase) |
| :--- | :--- | :--- |
| **M0** Setup | Repo, solución, schema, CLI, importadores base | `init-db` + `import` + `validate` funcionan en verde |
| **M1** Núcleo | Mundo en memoria, calendario, liga instantánea, save/load | Simular 1 temporada de 1 liga headless, estable y determinista |
| **FD** Datos (paralelo) | CSVs reales España primero, resto plausible→real | ~465 clubes con 12–16 jugadores cargados, `validate` limpio |
| **M2** Mundo vivo | Fases/copas/UCL/NT+clasificatorios, mercado IA, desarrollo | 10 temporadas del mundo completo sin crash ni anomalías; semana < 2 s |
| **M3** UI | 17 secciones en Unity UI Toolkit (solo resultado instantáneo) | Carrera completa jugable sin ver partidos; checklist WF cumple |
| **M4** Motor UME | Engine completo + modo texto + HUD 2D chapas | Calibración dentro de rangos de `05-motor.md` §15; determinismo dorado |
| **M5** Editor | Editor integrado + `.fmpack` + CSV | Crear una liga nueva end-to-end < 30 min sin tocar código |
| **v1.0** | Pulido, rendimiento, balance, build final | `PERF.md` dentro de RNFs; 3 temporadas jugadas por el humano |

---

## 7. Flujo de trabajo por sesión (bucle del agente)

1. Abre `ToDo.md` → localiza la **primera tarea sin marcar** de la fase activa.
2. Lee **solo** las secciones de docs que esa tarea referencia (gestiona tu contexto).
3. Implementa la tarea (con tests). Si es demasiado grande, divídela en sub-tareas T-XXXa/b y regístralo.
4. Ejecuta el DoD global (§9) y los comandos de verificación (§8).
5. Marca la tarea `[x]`, añade 1 línea de nota si hubo desviación, commit `T-XXX: ...`.
6. Añade entrada en `PROGRESS.md`:

```markdown
## 2026-XX-XX — Sesión N
- Hecho: T-014, T-015
- Decisiones: [1 línea c/u, detalladas en DECISIONS.md]
- PREGUNTAS HUMANO: [vacío idealmente]
- Siguiente: T-016
```

---

## 8. Comandos de verificación (CLI)

| Comando | Uso |
| :--- | :--- |
| `unifutsal init-db --out saves/new.db` | Crea base desde DDL + aplica builtin pack |
| `unifutsal import --csv data/csv/people.csv` | Importa y valida un CSV (todos los tipos) |
| `unifutsal validate --db saves/new.db` | Ejecuta las 7 queries de `03-datos.md` §9 → reporte JSON |
| `unifutsal new-game --db ... --club inter-fs` | Configura manager + arranca mundo |
| `unifutsal sim --days 7 --db ...` | Avanza el mundo (RNF-08: <2 s/semana) |
| `unifutsal sim --seasons 1 --db ... --report` | Temporada completa + reporte JSON (campeones, pichichis) |
| `unifutsal match --home X --away Y --seed 42 --speed instant\|text\|events` | Motor UME |
| `unifutsal calibrate --n 5000` | Harness de calibración de `05-motor.md` §15 |

**Verificación mínima por tarea:** `dotnet build && dotnet test && unifutsal validate`.

---

## 9. Definition of Done (global, toda tarea)

- [ ] `dotnet build` sin warnings nuevos.
- [ ] `dotnet test` verde, **incluye al menos 1 test nuevo** por lógica añadida.
- [ ] `unifutsal validate` limpio con el mundo de prueba.
- [ ] Cero strings de UI hardcodeadas (claves i18n) y cero colores hex fuera del theme (solo M3+).
- [ ] Sin `TODO` sueltos en código: o se resuelve o se registra en `Backlog.md`.
- [ ] `ToDo.md` actualizado + commit con ID de tarea.

---

## 10. Guardarraíls técnicos

### 10.1 Determinismo (obligatorio en Core y Engine)
- **Prohibido:** `DateTime.Now`, `Environment.TickCount`, `Random` global, `Guid.NewGuid()`, `Math.Pow/Sin/Cos/Exp/Log/Sqrt` (usar polinomios/tablas), `Thread/Task` dentro de un partido, iterar `Dictionary/HashSet` sin orden explícito, `ToString()` sin `InvariantCulture`.
- **Obligatorio:** toda aleatoriedad vía interfaz `IRng` (substreams); toda colección iterada con orden definido (listas ordenadas por uid/id); constantes solo desde `engine.json`.

### 10.2 Schema
- Cambios solo vía `migrations/NNN_*.sql` + bump de `user_version` + actualización de `03-datos.md` + test de migración (roundtrip).

### 10.3 UI (M3+)
- Componentes `u-*` del `02-estilos.md` §8; listas solo `ListView` virtualizadas; tokens USS únicos; profundidad máx. 3 niveles; atajos registrados en el registro central.

### 10.4 Rendimiento (assert en tests de M2 y v1.0)
- Partido instantáneo < 200 ms · semana de mundo < 2 s · init de temporada < 30 s · save < 5 s y < 50 MB.

---

## 11. Riesgos específicos de IA y mitigaciones

| Riesgo | Mitigación |
| :--- | :--- |
| Deriva de contexto / reinventar specs | Tareas pequeñas y ordenadas; releer solo la sección referenciada |
| APIs de Unity alucinadas | Todo lo testeable vive fuera de Unity; tareas Unity son cortas y verificadas por el humano |
| Romper determinismo sin darse cuenta | Tests golden (hash del stream por seed) en cada PR de motor |
| Scope creep | Regla 5 + `Backlog.md` |
| Datos reales incorrectos/incompletos | El agente solo genera tooling y datos plausibles; el contenido real lo aporta el humano vía CSV |

---

## 12. Qué hace el humano (product owner)

1. Aporta/a corpora las plantillas reales vía CSV (España primero).
2. Valida visualmente las pantallas Unity tras cada bloque de M3/M4 (checklist del wireframe).
3. Juega y da feedback de balance; responde `PREGUNTAS HUMANO`.
4. Toma las decisiones de `Backlog.md` que quiera promover.

---

## 13. Prompt de arranque (cópialo al iniciar el agente)

> Eres el implementador de UniFutsal Manager. Lee `docs/plan.md` completo y `docs/ToDo.md`. Empieza por T-001 y ejecuta el bucle de trabajo de plan.md §7. No hagas nada fuera del plan. Al terminar cada sesión, deja PROGRESS.md y ToDo.md actualizados y el build en verde.
````





