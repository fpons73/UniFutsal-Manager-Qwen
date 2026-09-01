-- UniFutsal Manager - Schema inicial
-- Basado en Modelo de Datos v1.0.1

PRAGMA journal_mode = WAL;
PRAGMA user_version = 1;

-- Tabla de metadatos
CREATE TABLE meta (
  key   TEXT PRIMARY KEY,
  value TEXT NOT NULL
);

-- Seeds iniciales
INSERT INTO meta (key, value) VALUES ('schema_version', '1');
INSERT INTO meta (key, value) VALUES ('world_seed', 'default');
INSERT INTO meta (key, value) VALUES ('world_date', '2026-07-01');
INSERT INTO meta (key, value) VALUES ('game_version', '0.1.0');
INSERT INTO meta (key, value) VALUES ('locale_default', 'es');

-- ============================================================================
-- D1: Geografía y configuración base
-- ============================================================================

CREATE TABLE confederations (
   id   INTEGER PRIMARY KEY,
   code TEXT NOT NULL UNIQUE,
   name TEXT NOT NULL
);

CREATE TABLE countries (
   id                 INTEGER PRIMARY KEY,
   uid                TEXT NOT NULL UNIQUE,
   name               TEXT NOT NULL,
   code3              TEXT NOT NULL UNIQUE,
   confederation_id   INTEGER NOT NULL REFERENCES confederations(id),
   futsal_reputation  REAL NOT NULL DEFAULT 50
);

CREATE TABLE regions (
   id            INTEGER PRIMARY KEY,
   country_id    INTEGER NOT NULL REFERENCES countries(id),
   name          TEXT NOT NULL,
   youth_quality REAL NOT NULL DEFAULT 50
);

CREATE TABLE venues (
   id         INTEGER PRIMARY KEY,
   uid        TEXT NOT NULL UNIQUE,
   name       TEXT NOT NULL,
   city       TEXT,
   country_id INTEGER REFERENCES countries(id),
   capacity   INTEGER NOT NULL DEFAULT 1500 CHECK (capacity BETWEEN 100 AND 60000),
   surface    TEXT NOT NULL DEFAULT 'parquet'
              CHECK (surface IN ('parquet','linoleum','pvc','taraflex'))
);

-- ============================================================================
-- D2: Personas, jugadores, staff, agentes
-- ============================================================================

CREATE TABLE persons (
   id                    INTEGER PRIMARY KEY,
   uid                   TEXT NOT NULL UNIQUE,
   first_name            TEXT NOT NULL,
   last_name             TEXT NOT NULL,
   common_name           TEXT,
   gender                TEXT NOT NULL DEFAULT 'M' CHECK (gender IN ('M','F')),
   birth_date            TEXT NOT NULL,
   birth_city            TEXT,
   birth_country_id      INTEGER REFERENCES countries(id),
   nationality_id        INTEGER NOT NULL REFERENCES countries(id),
   second_nationality_id INTEGER REFERENCES countries(id),
   height_cm             INTEGER CHECK (height_cm BETWEEN 150 AND 220),
   weight_kg             INTEGER,
   personality_key       TEXT,
   source                TEXT NOT NULL DEFAULT 'seed'
                         CHECK (source IN ('seed','import','generated','youth'))
);

CREATE TABLE traits (
   id          INTEGER PRIMARY KEY,
   key         TEXT NOT NULL UNIQUE,
   name_key    TEXT NOT NULL,
   category    TEXT NOT NULL CHECK (category IN ('tecnica','mental','fisica','portero')),
   effect_json TEXT NOT NULL DEFAULT '{}'
);

CREATE TABLE player_traits (
   player_id  INTEGER NOT NULL REFERENCES persons(id) ON DELETE CASCADE,
   trait_id   INTEGER NOT NULL REFERENCES traits(id),
   learned_on TEXT NOT NULL,
   PRIMARY KEY (player_id, trait_id)
) WITHOUT ROWID;

CREATE TABLE agents (
   id             INTEGER PRIMARY KEY,
   person_id      INTEGER UNIQUE REFERENCES persons(id),
   agency_name    TEXT,
   reputation     REAL NOT NULL DEFAULT 40,
   commission_pct REAL NOT NULL DEFAULT 5 CHECK (commission_pct BETWEEN 0 AND 15)
);

CREATE TABLE agent_clients (
   agent_id  INTEGER NOT NULL REFERENCES agents(id) ON DELETE CASCADE,
   player_id INTEGER NOT NULL REFERENCES persons(id),
   since     TEXT NOT NULL,
   PRIMARY KEY (agent_id, player_id)
) WITHOUT ROWID;

CREATE TABLE players (
   person_id           INTEGER PRIMARY KEY REFERENCES persons(id) ON DELETE CASCADE,
   position_main       TEXT NOT NULL CHECK (position_main IN ('POR','CIE','ALI','ALD','PIV','UNI')),
   position_secondary  TEXT    CHECK (position_secondary IN ('POR','CIE','ALI','ALD','PIV','UNI')),
   preferred_foot      TEXT NOT NULL DEFAULT 'D' CHECK (preferred_foot IN ('D','I','AM')),
   weak_foot           INTEGER NOT NULL DEFAULT 3 CHECK (weak_foot BETWEEN 1 AND 5),
   current_ability     INTEGER NOT NULL CHECK (current_ability BETWEEN 1 AND 200),
   potential_ability   INTEGER NOT NULL CHECK (potential_ability BETWEEN 1 AND 200),
   t_control INTEGER NOT NULL DEFAULT 10 CHECK (t_control BETWEEN 1 AND 20),
   t_conduccion INTEGER NOT NULL DEFAULT 10 CHECK (t_conduccion BETWEEN 1 AND 20),
   t_pase INTEGER NOT NULL DEFAULT 10 CHECK (t_pase BETWEEN 1 AND 20),
   t_pase_un_toque INTEGER NOT NULL DEFAULT 10 CHECK (t_pase_un_toque BETWEEN 1 AND 20),
   t_finalizacion INTEGER NOT NULL DEFAULT 10 CHECK (t_finalizacion BETWEEN 1 AND 20),
   t_tiro_lejano INTEGER NOT NULL DEFAULT 10 CHECK (t_tiro_lejano BETWEEN 1 AND 20),
   t_regate INTEGER NOT NULL DEFAULT 10 CHECK (t_regate BETWEEN 1 AND 20),
   t_poste INTEGER NOT NULL DEFAULT 10 CHECK (t_poste BETWEEN 1 AND 20),
   t_entrada INTEGER NOT NULL DEFAULT 10 CHECK (t_entrada BETWEEN 1 AND 20),
   t_intercepcion INTEGER NOT NULL DEFAULT 10 CHECK (t_intercepcion BETWEEN 1 AND 20),
   t_bloqueo INTEGER NOT NULL DEFAULT 10 CHECK (t_bloqueo BETWEEN 1 AND 20),
   g_paradas INTEGER NOT NULL DEFAULT 1 CHECK (g_paradas BETWEEN 1 AND 20),
   g_reflejos INTEGER NOT NULL DEFAULT 1 CHECK (g_reflejos BETWEEN 1 AND 20),
   g_uno_con_uno INTEGER NOT NULL DEFAULT 1 CHECK (g_uno_con_uno BETWEEN 1 AND 20),
   g_juego_pies INTEGER NOT NULL DEFAULT 1 CHECK (g_juego_pies BETWEEN 1 AND 20),
   g_distribucion INTEGER NOT NULL DEFAULT 1 CHECK (g_distribucion BETWEEN 1 AND 20),
   g_posicionamiento INTEGER NOT NULL DEFAULT 1 CHECK (g_posicionamiento BETWEEN 1 AND 20),
   g_salidas INTEGER NOT NULL DEFAULT 1 CHECK (g_salidas BETWEEN 1 AND 20),
   g_jugador INTEGER NOT NULL DEFAULT 1 CHECK (g_jugador BETWEEN 1 AND 20),
   m_vision INTEGER NOT NULL DEFAULT 10 CHECK (m_vision BETWEEN 1 AND 20),
   m_decision INTEGER NOT NULL DEFAULT 10 CHECK (m_decision BETWEEN 1 AND 20),
   m_anticipacion INTEGER NOT NULL DEFAULT 10 CHECK (m_anticipacion BETWEEN 1 AND 20),
   m_concentracion INTEGER NOT NULL DEFAULT 10 CHECK (m_concentracion BETWEEN 1 AND 20),
   m_posicionamiento INTEGER NOT NULL DEFAULT 10 CHECK (m_posicionamiento BETWEEN 1 AND 20),
   m_agresividad INTEGER NOT NULL DEFAULT 10 CHECK (m_agresividad BETWEEN 1 AND 20),
   m_serenidad INTEGER NOT NULL DEFAULT 10 CHECK (m_serenidad BETWEEN 1 AND 20),
   m_liderazgo INTEGER NOT NULL DEFAULT 10 CHECK (m_liderazgo BETWEEN 1 AND 20),
   m_equipo INTEGER NOT NULL DEFAULT 10 CHECK (m_equipo BETWEEN 1 AND 20),
   m_trabajo INTEGER NOT NULL DEFAULT 10 CHECK (m_trabajo BETWEEN 1 AND 20),
   m_arrojo INTEGER NOT NULL DEFAULT 10 CHECK (m_arrojo BETWEEN 1 AND 20),
   p_aceleracion INTEGER NOT NULL DEFAULT 10 CHECK (p_aceleracion BETWEEN 1 AND 20),
   p_velocidad INTEGER NOT NULL DEFAULT 10 CHECK (p_velocidad BETWEEN 1 AND 20),
   p_agilidad INTEGER NOT NULL DEFAULT 10 CHECK (p_agilidad BETWEEN 1 AND 20),
   p_equilibrio INTEGER NOT NULL DEFAULT 10 CHECK (p_equilibrio BETWEEN 1 AND 20),
   p_coordinacion INTEGER NOT NULL DEFAULT 10 CHECK (p_coordinacion BETWEEN 1 AND 20),
   p_resistencia INTEGER NOT NULL DEFAULT 10 CHECK (p_resistencia BETWEEN 1 AND 20),
   p_fuerza INTEGER NOT NULL DEFAULT 10 CHECK (p_fuerza BETWEEN 1 AND 20),
   p_salto INTEGER NOT NULL DEFAULT 10 CHECK (p_salto BETWEEN 1 AND 20),
   h_consistencia INTEGER NOT NULL DEFAULT 10 CHECK (h_consistencia BETWEEN 1 AND 20),
   h_lesiones INTEGER NOT NULL DEFAULT 10 CHECK (h_lesiones BETWEEN 1 AND 20),
   h_juego_duro INTEGER NOT NULL DEFAULT 10 CHECK (h_juego_duro BETWEEN 1 AND 20),
   h_temperamento INTEGER NOT NULL DEFAULT 10 CHECK (h_temperamento BETWEEN 1 AND 20),
   retired INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE positional_weights (
   position  TEXT NOT NULL CHECK (position IN ('POR','CIE','ALI','ALD','PIV','UNI')),
   attribute TEXT NOT NULL,
   weight    REAL NOT NULL DEFAULT 0,
   PRIMARY KEY (position, attribute)
) WITHOUT ROWID;

CREATE TABLE staff (
   person_id        INTEGER PRIMARY KEY REFERENCES persons(id) ON DELETE CASCADE,
   role             TEXT NOT NULL CHECK (role IN
                      ('entrenador','segundo','prep_porteros','prep_fisico','fisioterapeuta',
                       'psicologo','ojeador','analista','director_deportivo','medico')),
   ent_tecnica INTEGER NOT NULL DEFAULT 10 CHECK (ent_tecnica BETWEEN 1 AND 20),
   ent_ofensiva INTEGER NOT NULL DEFAULT 10 CHECK (ent_ofensiva BETWEEN 1 AND 20),
   ent_defensiva INTEGER NOT NULL DEFAULT 10 CHECK (ent_defensiva BETWEEN 1 AND 20),
   ent_porteros INTEGER NOT NULL DEFAULT 10 CHECK (ent_porteros BETWEEN 1 AND 20),
   ent_fisica INTEGER NOT NULL DEFAULT 10 CHECK (ent_fisica BETWEEN 1 AND 20),
   ent_tactica INTEGER NOT NULL DEFAULT 10 CHECK (ent_tactica BETWEEN 1 AND 20),
   medicina INTEGER NOT NULL DEFAULT 10 CHECK (medicina BETWEEN 1 AND 20),
   h_juicio_habilidad INTEGER NOT NULL DEFAULT 10 CHECK (h_juicio_habilidad BETWEEN 1 AND 20),
   h_juicio_potencial INTEGER NOT NULL DEFAULT 10 CHECK (h_juicio_potencial BETWEEN 1 AND 20),
   motivacion INTEGER NOT NULL DEFAULT 10 CHECK (motivacion BETWEEN 1 AND 20),
   gestion_vestuario INTEGER NOT NULL DEFAULT 10 CHECK (gestion_vestuario BETWEEN 1 AND 20),
   negociacion INTEGER NOT NULL DEFAULT 10 CHECK (negociacion BETWEEN 1 AND 20),
   adaptabilidad INTEGER NOT NULL DEFAULT 10 CHECK (adaptabilidad BETWEEN 1 AND 20),
   retired INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE referees (
   person_id        INTEGER PRIMARY KEY REFERENCES persons(id) ON DELETE CASCADE,
   country_id       INTEGER REFERENCES countries(id),
   strictness       INTEGER NOT NULL DEFAULT 10 CHECK (strictness BETWEEN 1 AND 20),
   big_match_rating REAL NOT NULL DEFAULT 50
);

-- ============================================================================
-- D3: Clubes, finanzas
-- ============================================================================

CREATE TABLE clubs (
   id                  INTEGER PRIMARY KEY,
   uid                 TEXT NOT NULL UNIQUE,
   name                TEXT NOT NULL,
   short_name          TEXT,
   nickname            TEXT,
   country_id          INTEGER NOT NULL REFERENCES countries(id),
   region_id           INTEGER REFERENCES regions(id),
   city                TEXT,
   founded_year        INTEGER,
   primary_color       TEXT NOT NULL DEFAULT '#E63946'
                       CHECK (length(primary_color) = 7 AND primary_color GLOB '#[0-9a-fA-F]*'),
   secondary_color     TEXT NOT NULL DEFAULT '#FFFFFF'
                       CHECK (length(secondary_color) = 7 AND secondary_color GLOB '#[0-9a-fA-F]*'),
   kit_pattern         TEXT NOT NULL DEFAULT 'solid'
                       CHECK (kit_pattern IN ('solid','stripes','halved','sash')),
   reputation          REAL NOT NULL DEFAULT 40,
   venue_id            INTEGER REFERENCES venues(id),
   training_facilities INTEGER NOT NULL DEFAULT 10 CHECK (training_facilities BETWEEN 1 AND 20),
   youth_facilities    INTEGER NOT NULL DEFAULT 10 CHECK (youth_facilities BETWEEN 1 AND 20),
   recruitment         INTEGER NOT NULL DEFAULT 10 CHECK (recruitment BETWEEN 1 AND 20),
   physio_rating       INTEGER NOT NULL DEFAULT 10 CHECK (physio_rating BETWEEN 1 AND 20),
   bank_balance        INTEGER NOT NULL DEFAULT 0,
   debt                INTEGER NOT NULL DEFAULT 0,
   transfer_budget     INTEGER NOT NULL DEFAULT 0,
   wage_budget_monthly INTEGER NOT NULL DEFAULT 0,
   is_active           INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE seasons (
   id         INTEGER PRIMARY KEY,
   label      TEXT NOT NULL UNIQUE,
   start_date TEXT NOT NULL,
   end_date   TEXT NOT NULL
);

CREATE TABLE club_objectives (
   id          INTEGER PRIMARY KEY,
   season_id   INTEGER NOT NULL REFERENCES seasons(id),
   club_id     INTEGER NOT NULL REFERENCES clubs(id) ON DELETE CASCADE,
   objective   TEXT NOT NULL CHECK (objective IN
                 ('posicion_liga','campeon_copa','ucl_fase','ucl_final4','permanencia',
                  'no_descenso_admin','balance_positivo','promocion_cantera')),
   target      TEXT,
   priority    TEXT NOT NULL CHECK (priority IN ('obligatorio','importante','deseable')),
   reward_json TEXT
);

CREATE TABLE club_finances_monthly (
   id          INTEGER PRIMARY KEY,
   club_id     INTEGER NOT NULL REFERENCES clubs(id) ON DELETE CASCADE,
   month       TEXT NOT NULL,
   income      INTEGER NOT NULL DEFAULT 0,
   expenses    INTEGER NOT NULL DEFAULT 0,
   balance_end INTEGER NOT NULL DEFAULT 0,
   UNIQUE (club_id, month)
);

CREATE TABLE financial_transactions (
   id          INTEGER PRIMARY KEY,
   club_id     INTEGER NOT NULL REFERENCES clubs(id) ON DELETE CASCADE,
   date        TEXT NOT NULL,
   category    TEXT NOT NULL CHECK (category IN
                 ('premio','taquilla','abonos','patrocinio','tv','fichaje_in','fichaje_out',
                  'salarios','viajes','agente','instalaciones','cantera','multa','amistoso','otro')),
   amount      INTEGER NOT NULL,
   description TEXT,
   match_id    INTEGER REFERENCES matches(id),
   transfer_id INTEGER REFERENCES transfers(id)
);

-- ============================================================================
-- D4: Competiciones
-- ============================================================================

CREATE TABLE competitions (
   id                INTEGER PRIMARY KEY,
   uid               TEXT NOT NULL UNIQUE,
   name              TEXT NOT NULL,
   short_name        TEXT,
   scope             TEXT NOT NULL CHECK (scope IN ('club','seleccion')),
   type              TEXT NOT NULL CHECK (type IN ('liga','copa')),
   country_id        INTEGER REFERENCES countries(id),
   confederation_id  INTEGER REFERENCES confederations(id),
   level             INTEGER,
   prestige          REAL NOT NULL DEFAULT 30,
   rules_json        TEXT NOT NULL DEFAULT '{}',
   active            INTEGER NOT NULL DEFAULT 1,
   source_pack_id    INTEGER REFERENCES data_packs(id)
);

CREATE TABLE competition_phases (
   id             INTEGER PRIMARY KEY,
   competition_id INTEGER NOT NULL REFERENCES competitions(id) ON DELETE CASCADE,
   phase_index    INTEGER NOT NULL,
   name           TEXT,
   format         TEXT NOT NULL CHECK (format IN
                    ('round_robin','knockout','mini_torneo','final_four')),
   teams_in       INTEGER,
   teams_out      INTEGER,
   config_json    TEXT NOT NULL DEFAULT '{}',
   UNIQUE (competition_id, phase_index)
);

CREATE TABLE competition_groups (
   id            INTEGER PRIMARY KEY,
   phase_id      INTEGER NOT NULL REFERENCES competition_phases(id) ON DELETE CASCADE,
   name          TEXT NOT NULL,
   group_index   INTEGER NOT NULL,
   host_venue_id INTEGER REFERENCES venues(id)
);

CREATE TABLE competition_links (
   id                  INTEGER PRIMARY KEY,
   from_competition_id INTEGER NOT NULL REFERENCES competitions(id) ON DELETE CASCADE,
   to_competition_id   INTEGER REFERENCES competitions(id),
   link_type           TEXT NOT NULL CHECK (link_type IN
                         ('ascenso','descenso','clasificacion','repesca','baja')),
   criteria_json       TEXT NOT NULL,
   slots               INTEGER NOT NULL DEFAULT 1,
   priority            INTEGER NOT NULL DEFAULT 10
);

CREATE TABLE competition_entries (
   id                    INTEGER PRIMARY KEY,
   season_id             INTEGER NOT NULL REFERENCES seasons(id),
   competition_id        INTEGER NOT NULL REFERENCES competitions(id),
   club_id               INTEGER REFERENCES clubs(id),
   national_team_id      INTEGER REFERENCES national_teams(id),
   group_id              INTEGER REFERENCES competition_groups(id),
   seed                  INTEGER,
   qualified_via_link_id INTEGER REFERENCES competition_links(id),
   status                TEXT NOT NULL DEFAULT 'activo'
                         CHECK (status IN ('activo','eliminado','retirado','sancionado')),
   CHECK ((club_id IS NULL) <> (national_team_id IS NULL)),
   UNIQUE (season_id, competition_id, club_id),
   UNIQUE (season_id, competition_id, national_team_id)
);

-- ============================================================================
-- D5: Calendario y partidos
-- ============================================================================

CREATE TABLE matches (
   id              INTEGER PRIMARY KEY,
   season_id       INTEGER NOT NULL REFERENCES seasons(id),
   competition_id  INTEGER NOT NULL REFERENCES competitions(id),
   phase_id        INTEGER REFERENCES competition_phases(id),
   group_id        INTEGER REFERENCES competition_groups(id),
   round_label     TEXT,
   matchday        INTEGER,
   tie_key         TEXT,
   leg             INTEGER CHECK (leg IN (1,2)),
   home_club_id    INTEGER REFERENCES clubs(id),
   away_club_id    INTEGER REFERENCES clubs(id),
   home_nt_id      INTEGER REFERENCES national_teams(id),
   away_nt_id      INTEGER REFERENCES national_teams(id),
   referee_id      INTEGER REFERENCES referees(person_id),
   played_on       TEXT,
   kickoff         TEXT,
   venue_id        INTEGER REFERENCES venues(id),
   status          TEXT NOT NULL DEFAULT 'programado'
                   CHECK (status IN ('programado','jugado','aplazado','cancelado','walkover')),
   home_score      INTEGER,
   away_score      INTEGER,
   home_ht         INTEGER,
   away_ht         INTEGER,
   home_pens       INTEGER,
   away_pens       INTEGER,
   attendance      INTEGER,
   rng_seed        INTEGER NOT NULL,
   full_events     INTEGER NOT NULL DEFAULT 0,
   CHECK ((home_club_id IS NULL) <> (home_nt_id IS NULL)),
   CHECK ((away_club_id IS NULL) <> (away_nt_id IS NULL))
);

CREATE TABLE match_events (
   id                  INTEGER PRIMARY KEY,
   match_id            INTEGER NOT NULL REFERENCES matches(id) ON DELETE CASCADE,
   seq                 INTEGER NOT NULL,
   period              INTEGER NOT NULL CHECK (period IN (1,2,3,4)),
   clock_mm            INTEGER NOT NULL CHECK (clock_mm BETWEEN 0 AND 20),
   clock_ss            INTEGER NOT NULL CHECK (clock_ss BETWEEN 0 AND 59),
   type                TEXT NOT NULL CHECK (type IN (
     'gol','tiro','parada','ocasion_fallada','falta','tarjeta_amarilla','tarjeta_roja',
     'expulsion_temporal','reincorporacion','doble_penalti','penalti','tanda',
     'timeout','cambio','lesion','power_play_on','power_play_off',
     'portero_jugador_on','portero_jugador_off','fin_periodo','fin_partido','otro')),
   side                TEXT CHECK (side IN ('home','away')),
   person_id           INTEGER REFERENCES persons(id),
   secondary_person_id INTEGER REFERENCES persons(id),
   score_home_after    INTEGER,
   score_away_after    INTEGER,
   narrative_key       TEXT,
   detail_json         TEXT NOT NULL DEFAULT '{}',
   UNIQUE (match_id, seq)
);

CREATE TABLE match_squads (
   match_id  INTEGER NOT NULL REFERENCES matches(id) ON DELETE CASCADE,
   person_id INTEGER NOT NULL REFERENCES persons(id),
   side      TEXT NOT NULL CHECK (side IN ('home','away')),
   slot      INTEGER NOT NULL CHECK (slot BETWEEN 1 AND 14),
   PRIMARY KEY (match_id, person_id)
) WITHOUT ROWID;

CREATE TABLE match_player_stats (
   match_id         INTEGER NOT NULL REFERENCES matches(id) ON DELETE CASCADE,
   person_id        INTEGER NOT NULL REFERENCES persons(id),
   side             TEXT NOT NULL CHECK (side IN ('home','away')),
   starter          INTEGER NOT NULL DEFAULT 0,
   minutes_played   INTEGER NOT NULL DEFAULT 0,
   goals            INTEGER NOT NULL DEFAULT 0,
   own_goals        INTEGER NOT NULL DEFAULT 0,
   assists          INTEGER NOT NULL DEFAULT 0,
   shots            INTEGER NOT NULL DEFAULT 0,
   shots_on_target  INTEGER NOT NULL DEFAULT 0,
   passes_attempted INTEGER NOT NULL DEFAULT 0,
   passes_completed INTEGER NOT NULL DEFAULT 0,
   key_passes       INTEGER NOT NULL DEFAULT 0,
   dribbles_completed INTEGER NOT NULL DEFAULT 0,
   interceptions    INTEGER NOT NULL DEFAULT 0,
   tackles_won      INTEGER NOT NULL DEFAULT 0,
   fouls_committed  INTEGER NOT NULL DEFAULT 0,
   fouls_received   INTEGER NOT NULL DEFAULT 0,
   yellow_cards     INTEGER NOT NULL DEFAULT 0,
   red_cards        INTEGER NOT NULL DEFAULT 0,
   saves            INTEGER NOT NULL DEFAULT 0,
   goals_conceded   INTEGER NOT NULL DEFAULT 0,
   rating           REAL,
   PRIMARY KEY (match_id, person_id)
) WITHOUT ROWID;

CREATE TABLE match_team_stats (
   match_id           INTEGER NOT NULL REFERENCES matches(id) ON DELETE CASCADE,
   side               TEXT NOT NULL CHECK (side IN ('home','away')),
   possession_pct     REAL,
   shots              INTEGER NOT NULL DEFAULT 0,
   shots_on_target    INTEGER NOT NULL DEFAULT 0,
   fouls              INTEGER NOT NULL DEFAULT 0,
   yellow_cards       INTEGER NOT NULL DEFAULT 0,
   red_cards          INTEGER NOT NULL DEFAULT 0,
   corners            INTEGER NOT NULL DEFAULT 0,
   timeouts_used      INTEGER NOT NULL DEFAULT 0 CHECK (timeouts_used BETWEEN 0 AND 2),
   power_play_seconds INTEGER NOT NULL DEFAULT 0,
   pp_goals           INTEGER NOT NULL DEFAULT 0,
   dp_attempts        INTEGER NOT NULL DEFAULT 0,
   dp_goals           INTEGER NOT NULL DEFAULT 0,
   PRIMARY KEY (match_id, side)
) WITHOUT ROWID;

-- ============================================================================
-- D6: Contratos, traspasos y scouting
-- ============================================================================

CREATE TABLE contracts (
   id              INTEGER PRIMARY KEY,
   person_id       INTEGER NOT NULL REFERENCES persons(id) ON DELETE CASCADE,
   club_id         INTEGER NOT NULL REFERENCES clubs(id) ON DELETE CASCADE,
   scope           TEXT NOT NULL DEFAULT 'primer_equipo'
                   CHECK (scope IN ('primer_equipo','cantera','staff')),
   signed_on       TEXT NOT NULL,
   effective_from  TEXT NOT NULL,
   effective_until TEXT NOT NULL,
   wage_monthly    INTEGER NOT NULL CHECK (wage_monthly >= 0),
   release_clause  INTEGER,
   squad_number    INTEGER CHECK (squad_number BETWEEN 1 AND 99),
   bonus_json      TEXT NOT NULL DEFAULT '{}',
   agent_id        INTEGER REFERENCES agents(id),
   agent_fee       INTEGER,
   negotiated_by   TEXT CHECK (negotiated_by IN ('manager','directivo','agente_exterior')),
   status          TEXT NOT NULL DEFAULT 'vigente'
                   CHECK (status IN ('vigente','renovado','rescindido','expirado','cesion')),
   UNIQUE (person_id, club_id, effective_from)
);

CREATE TABLE transfer_windows (
   id             INTEGER PRIMARY KEY,
   competition_id INTEGER NOT NULL REFERENCES competitions(id),
   season_id      INTEGER NOT NULL REFERENCES seasons(id),
   opens_on       TEXT NOT NULL,
   closes_on      TEXT NOT NULL,
   max_in         INTEGER,
   max_out        INTEGER,
   CHECK (closes_on >= opens_on)
);

CREATE TABLE transfers (
   id                INTEGER PRIMARY KEY,
   player_id         INTEGER NOT NULL REFERENCES persons(id),
   from_club_id      INTEGER REFERENCES clubs(id),
   to_club_id        INTEGER REFERENCES clubs(id),
   loan_from_club_id INTEGER REFERENCES clubs(id),
   happened_on       TEXT NOT NULL,
   window_id         INTEGER REFERENCES transfer_windows(id),
   fee               INTEGER NOT NULL DEFAULT 0,
   type              TEXT NOT NULL CHECK (type IN
                       ('permanente','cesion','fin_cesion','libre','cantera','retiro')),
   add_on_pct        REAL CHECK (add_on_pct BETWEEN 0 AND 30),
   installments_json TEXT NOT NULL DEFAULT '{}',
   loan_until        TEXT,
   recall_clause     INTEGER NOT NULL DEFAULT 0,
   status            TEXT NOT NULL DEFAULT 'completado'
                     CHECK (status IN ('completado','cancelado','fallido')),
   seed_version      INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE transfer_offers (
   id             INTEGER PRIMARY KEY,
   player_id      INTEGER NOT NULL REFERENCES persons(id),
   bidder_club_id INTEGER NOT NULL REFERENCES clubs(id),
   seller_club_id INTEGER NOT NULL REFERENCES clubs(id),
   offered_on     TEXT NOT NULL,
   expires_on     TEXT NOT NULL,
   fee            INTEGER NOT NULL DEFAULT 0,
   terms_json     TEXT NOT NULL DEFAULT '{}',
   agent_fee      INTEGER,
   status         TEXT NOT NULL DEFAULT 'pendiente'
                  CHECK (status IN ('pendiente','aceptada','rechazada','contraofertada',
                                    'caducada','retirada')),
   counter_json   TEXT
);

CREATE TABLE player_knowledge (
   observer_club_id INTEGER NOT NULL REFERENCES clubs(id) ON DELETE CASCADE,
   person_id        INTEGER NOT NULL REFERENCES persons(id) ON DELETE CASCADE,
   knowledge        INTEGER NOT NULL DEFAULT 0 CHECK (knowledge BETWEEN 0 AND 100),
   last_scouted_on  TEXT,
   report_json      TEXT,
   PRIMARY KEY (observer_club_id, person_id)
) WITHOUT ROWID;

CREATE TABLE scout_assignments (
   id             INTEGER PRIMARY KEY,
   club_id        INTEGER NOT NULL REFERENCES clubs(id) ON DELETE CASCADE,
   scout_id       INTEGER NOT NULL REFERENCES staff(person_id),
   target_type    TEXT NOT NULL CHECK (target_type IN ('region','jugador','competicion')),
   region_id      INTEGER REFERENCES regions(id),
   person_id      INTEGER REFERENCES persons(id),
   competition_id INTEGER REFERENCES competitions(id),
   assigned_on    TEXT NOT NULL,
   until          TEXT
);

CREATE TABLE shortlists (
   id      INTEGER PRIMARY KEY,
   club_id INTEGER NOT NULL REFERENCES clubs(id) ON DELETE CASCADE,
   name    TEXT NOT NULL DEFAULT 'Principal'
);

CREATE TABLE shortlist_items (
   list_id   INTEGER NOT NULL REFERENCES shortlists(id) ON DELETE CASCADE,
   person_id INTEGER NOT NULL REFERENCES persons(id) ON DELETE CASCADE,
   note      TEXT,
   added_on  TEXT NOT NULL,
   PRIMARY KEY (list_id, person_id)
) WITHOUT ROWID;

-- ============================================================================
-- D7: Selecciones nacionales
-- ============================================================================

CREATE TABLE national_teams (
   id               INTEGER PRIMARY KEY,
   uid              TEXT NOT NULL UNIQUE,
   country_id       INTEGER NOT NULL UNIQUE REFERENCES countries(id),
   reputation       REAL NOT NULL DEFAULT 40,
   manager_id       INTEGER REFERENCES persons(id),
   federation_trust INTEGER NOT NULL DEFAULT 60 CHECK (federation_trust BETWEEN 0 AND 100),
   kit_primary      TEXT NOT NULL DEFAULT '#FFFFFF',
   kit_secondary    TEXT NOT NULL DEFAULT '#0000FF'
);

CREATE TABLE nt_objectives (
   id             INTEGER PRIMARY KEY,
   nt_id          INTEGER NOT NULL REFERENCES national_teams(id) ON DELETE CASCADE,
   season_id      INTEGER NOT NULL REFERENCES seasons(id),
   competition_id INTEGER NOT NULL REFERENCES competitions(id),
   target         TEXT NOT NULL CHECK (target IN
                    ('clasificar','fase_grupos','cuartos','semifinal','final','campeon')),
   priority       TEXT NOT NULL CHECK (priority IN ('obligatorio','importante','deseable'))
);

CREATE TABLE nt_calls (
   id        INTEGER PRIMARY KEY,
   nt_id     INTEGER NOT NULL REFERENCES national_teams(id) ON DELETE CASCADE,
   person_id INTEGER NOT NULL REFERENCES persons(id) ON DELETE CASCADE,
   called_on TEXT NOT NULL,
   call_type TEXT NOT NULL CHECK (call_type IN ('prelista','final','amistoso')),
   match_id  INTEGER REFERENCES matches(id),
   status    TEXT NOT NULL DEFAULT 'convocado'
             CHECK (status IN ('convocado','rechazado','lesionado','reserva','descartado')),
   UNIQUE (nt_id, person_id, called_on)
);

CREATE TABLE nt_manager_contracts (
   id             INTEGER PRIMARY KEY,
   manager_id     INTEGER NOT NULL REFERENCES persons(id) ON DELETE CASCADE,
   nt_id          INTEGER NOT NULL REFERENCES national_teams(id),
   signed_on      TEXT NOT NULL,
   until_date     TEXT NOT NULL,
   wage_monthly   INTEGER NOT NULL DEFAULT 0,
   objectives_json TEXT NOT NULL DEFAULT '{}'
);

-- ============================================================================
-- D8: Entrenamiento y desarrollo
-- ============================================================================

CREATE TABLE training_sessions (
   id         INTEGER PRIMARY KEY,
   club_id    INTEGER NOT NULL REFERENCES clubs(id) ON DELETE CASCADE,
   session_on TEXT NOT NULL,
   focus      TEXT NOT NULL CHECK (focus IN
                ('tecnica','finalizacion','transiciones','tactica','fisico','porteros',
                 'estrategia','recuperacion','descanso')),
   intensity  TEXT NOT NULL CHECK (intensity IN ('baja','media','alta')),
   coach_id   INTEGER REFERENCES staff(person_id)
);

CREATE TABLE individual_training (
   id         INTEGER PRIMARY KEY,
   person_id  INTEGER NOT NULL REFERENCES persons(id) ON DELETE CASCADE,
   club_id    INTEGER NOT NULL REFERENCES clubs(id) ON DELETE CASCADE,
   attribute  TEXT NOT NULL,
   coach_id   INTEGER REFERENCES staff(person_id),
   started_on TEXT NOT NULL,
   ends_on    TEXT,
   status     TEXT NOT NULL DEFAULT 'activo'
              CHECK (status IN ('activo','completado','cancelado'))
);

CREATE TABLE trait_learning (
   person_id   INTEGER NOT NULL REFERENCES persons(id) ON DELETE CASCADE,
   trait_id    INTEGER NOT NULL REFERENCES traits(id),
   started_on  TEXT NOT NULL,
   weeks_total INTEGER NOT NULL DEFAULT 8,
   weeks_done  INTEGER NOT NULL DEFAULT 0,
   PRIMARY KEY (person_id, trait_id)
) WITHOUT ROWID;

CREATE TABLE injuries (
   id              INTEGER PRIMARY KEY,
   person_id       INTEGER NOT NULL REFERENCES persons(id) ON DELETE CASCADE,
   injury_key      TEXT NOT NULL,
   severity        INTEGER NOT NULL CHECK (severity BETWEEN 1 AND 5),
   occurred_on     TEXT NOT NULL,
   expected_return TEXT NOT NULL,
   actual_return   TEXT,
   match_id        INTEGER REFERENCES matches(id),
   risk_json       TEXT
);

CREATE TABLE development_snapshots (
   person_id       INTEGER NOT NULL REFERENCES persons(id) ON DELETE CASCADE,
   month           TEXT NOT NULL,
   ca              INTEGER NOT NULL,
   attributes_json TEXT NOT NULL,
   PRIMARY KEY (person_id, month)
) WITHOUT ROWID;

-- ============================================================================
-- D9: Tácticas y alineaciones
-- ============================================================================

CREATE TABLE tactics (
   id              INTEGER PRIMARY KEY,
   club_id         INTEGER NOT NULL REFERENCES clubs(id) ON DELETE CASCADE,
   name            TEXT NOT NULL,
   formation       TEXT NOT NULL CHECK (formation IN ('4-0','3-1','2-2','1-2-1','1-3','y','custom')),
   config_json     TEXT NOT NULL DEFAULT '{}',
   set_pieces_json TEXT NOT NULL DEFAULT '{}',
   is_default      INTEGER NOT NULL DEFAULT 0,
   updated_on      TEXT NOT NULL
);

CREATE TABLE tactic_slots (
   tactic_id INTEGER NOT NULL REFERENCES tactics(id) ON DELETE CASCADE,
   slot      INTEGER NOT NULL CHECK (slot BETWEEN 1 AND 5),
   position  TEXT NOT NULL CHECK (position IN ('POR','CIE','ALI','ALD','PIV','UNI')),
   person_id INTEGER REFERENCES persons(id),
   PRIMARY KEY (tactic_id, slot)
) WITHOUT ROWID;

-- ============================================================================
-- D10: Managers, carrera e inbox
-- ============================================================================

CREATE TABLE managers (
   person_id        INTEGER PRIMARY KEY REFERENCES persons(id) ON DELETE CASCADE,
   is_user          INTEGER NOT NULL DEFAULT 0,
   license          TEXT NOT NULL DEFAULT 'nivel_1' CHECK (license IN ('nivel_1','nivel_2','nivel_3','pro')),
   reputation       REAL NOT NULL DEFAULT 20,
   attr_motivacion  INTEGER NOT NULL DEFAULT 10 CHECK (attr_motivacion BETWEEN 1 AND 20),
   attr_tactica     INTEGER NOT NULL DEFAULT 10 CHECK (attr_tactica BETWEEN 1 AND 20),
   attr_juveniles   INTEGER NOT NULL DEFAULT 10 CHECK (attr_juveniles BETWEEN 1 AND 20),
   attr_prensa      INTEGER NOT NULL DEFAULT 10 CHECK (attr_prensa BETWEEN 1 AND 20),
   attr_negociacion INTEGER NOT NULL DEFAULT 10 CHECK (attr_negociacion BETWEEN 1 AND 20),
   attr_vestuario   INTEGER NOT NULL DEFAULT 10 CHECK (attr_vestuario BETWEEN 1 AND 20)
);

CREATE TABLE manager_contracts (
   id           INTEGER PRIMARY KEY,
   manager_id   INTEGER NOT NULL REFERENCES managers(person_id) ON DELETE CASCADE,
   club_id      INTEGER REFERENCES clubs(id),
   nt_id        INTEGER REFERENCES national_teams(id),
   CHECK ((club_id IS NULL) <> (nt_id IS NULL)),
   signed_on    TEXT NOT NULL,
   until_date   TEXT,
   wage_monthly INTEGER NOT NULL DEFAULT 0,
   status       TEXT NOT NULL DEFAULT 'vigente'
                CHECK (status IN ('vigente','finalizado','rescindido'))
);

CREATE TABLE manager_history (
   id         INTEGER PRIMARY KEY,
   manager_id INTEGER NOT NULL REFERENCES managers(person_id) ON DELETE CASCADE,
   club_id    INTEGER REFERENCES clubs(id),
   nt_id      INTEGER REFERENCES national_teams(id),
   started_on TEXT NOT NULL,
   ended_on   TEXT,
   end_reason TEXT CHECK (end_reason IN ('despido','renuncia','fin_contrato','ascenso','otro'))
);

CREATE TABLE promises (
   id          INTEGER PRIMARY KEY,
   player_id   INTEGER NOT NULL REFERENCES persons(id) ON DELETE CASCADE,
   club_id     INTEGER NOT NULL REFERENCES clubs(id),
   type        TEXT NOT NULL CHECK (type IN
                 ('minutos','rol_titular','fichaje_estrella','renovacion','venta_no','seleccion')),
   made_on     TEXT NOT NULL,
   deadline    TEXT,
   detail_json TEXT NOT NULL DEFAULT '{}',
   status      TEXT NOT NULL DEFAULT 'pendiente'
               CHECK (status IN ('pendiente','cumplida','rota','expirada'))
);

CREATE TABLE confidence (
   scope_type TEXT NOT NULL CHECK (scope_type IN ('club_junta','club_aficion','federacion')),
   scope_id   INTEGER NOT NULL,
   value      INTEGER NOT NULL DEFAULT 60 CHECK (value BETWEEN 0 AND 100),
   updated_on TEXT NOT NULL,
   PRIMARY KEY (scope_type, scope_id)
) WITHOUT ROWID;

CREATE TABLE messages (
   id           INTEGER PRIMARY KEY,
   received_on  TEXT NOT NULL,
   category     TEXT NOT NULL CHECK (category IN
                  ('junta','ojeador','agente','prensa','competicion','jugador','seleccion',
                   'sistema','mercado')),
   sender_key   TEXT NOT NULL,
   subject_key  TEXT NOT NULL,
   body_key     TEXT NOT NULL,
   context_json TEXT NOT NULL DEFAULT '{}',
   actions_json TEXT NOT NULL DEFAULT '{}',
   is_read      INTEGER NOT NULL DEFAULT 0,
   archived     INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE news_items (
   id           INTEGER PRIMARY KEY,
   published_on TEXT NOT NULL,
   category     TEXT NOT NULL CHECK (category IN
                  ('resultado','fichaje','rumor','premio','lesion','otro')),
   title_key    TEXT NOT NULL,
   body_key     TEXT NOT NULL,
   context_json TEXT NOT NULL DEFAULT '{}'
);

-- ============================================================================
-- D11: Editor y data packs
-- ============================================================================

CREATE TABLE data_packs (
   id             INTEGER PRIMARY KEY,
   uid            TEXT NOT NULL UNIQUE,
   name           TEXT NOT NULL,
   author         TEXT,
   version        TEXT NOT NULL,
   schema_version INTEGER NOT NULL,
   created_on     TEXT NOT NULL,
   description    TEXT,
   is_builtin     INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE pack_objects (
   id          INTEGER PRIMARY KEY,
   pack_id     INTEGER NOT NULL REFERENCES data_packs(id) ON DELETE CASCADE,
   object_type TEXT NOT NULL CHECK (object_type IN
     ('country','club','venue','person','player','staff','competition','phase','link',
      'rule','calendar')),
   object_uid  TEXT NOT NULL,
   action      TEXT NOT NULL CHECK (action IN ('create','update','delete')),
   payload_json TEXT NOT NULL
);

CREATE TABLE pack_validation_errors (
   pack_id     INTEGER NOT NULL REFERENCES data_packs(id) ON DELETE CASCADE,
   severity    TEXT NOT NULL CHECK (severity IN ('error','aviso')),
   code        TEXT NOT NULL,
   message_key TEXT NOT NULL,
   object_uid  TEXT,
   PRIMARY KEY (pack_id, code, object_uid)
) WITHOUT ROWID;

-- ============================================================================
-- D12: Palmarés, premios y rivalidades
-- ============================================================================

CREATE TABLE honours (
   id                INTEGER PRIMARY KEY,
   season_id         INTEGER NOT NULL REFERENCES seasons(id),
   competition_id    INTEGER NOT NULL REFERENCES competitions(id),
   winner_club_id    INTEGER REFERENCES clubs(id),
   winner_nt_id      INTEGER REFERENCES national_teams(id),
   runner_up_club_id INTEGER REFERENCES clubs(id),
   runner_up_nt_id   INTEGER REFERENCES national_teams(id),
   detail_json       TEXT
);

CREATE TABLE awards (
   id          INTEGER PRIMARY KEY,
   season_id   INTEGER NOT NULL REFERENCES seasons(id),
   type        TEXT NOT NULL CHECK (type IN
                 ('mejor_jugador','mejor_joven','mejor_manager','mvp_torneo','pichichi',
                  'mejor_portero','quinteto_ideal')),
   scope       TEXT,
   person_id   INTEGER REFERENCES persons(id),
   club_id     INTEGER REFERENCES clubs(id),
   rank        INTEGER NOT NULL DEFAULT 1,
   detail_json TEXT
);

CREATE TABLE club_rivalries (
   club_a    INTEGER NOT NULL REFERENCES clubs(id) ON DELETE CASCADE,
   club_b    INTEGER NOT NULL REFERENCES clubs(id) ON DELETE CASCADE,
   intensity INTEGER NOT NULL DEFAULT 50 CHECK (intensity BETWEEN 1 AND 100),
   name      TEXT,
   PRIMARY KEY (club_a, club_b),
   CHECK (club_a < club_b)
) WITHOUT ROWID;

-- ============================================================================
-- Índices (rendimiento)
-- ============================================================================

CREATE INDEX idx_matches_comp    ON matches(season_id, competition_id, played_on);
CREATE INDEX idx_matches_home    ON matches(home_club_id, played_on);
CREATE INDEX idx_matches_away    ON matches(away_club_id, played_on);
CREATE INDEX idx_events_match    ON match_events(match_id, seq);
CREATE INDEX idx_stats_person    ON match_player_stats(person_id);
CREATE INDEX idx_contracts_club  ON contracts(club_id, status);
CREATE INDEX idx_contracts_person ON contracts(person_id, status);
CREATE INDEX idx_transfers_player ON transfers(player_id, happened_on DESC);
CREATE INDEX idx_offers_seller   ON transfer_offers(seller_club_id, status);
CREATE INDEX idx_finances_club   ON financial_transactions(club_id, date);
CREATE INDEX idx_entries_comp    ON competition_entries(competition_id, season_id);
CREATE INDEX idx_messages_date   ON messages(archived, received_on DESC);
CREATE INDEX idx_news_date       ON news_items(published_on DESC);
CREATE INDEX idx_injuries_person ON injuries(person_id);
CREATE INDEX idx_training_club   ON training_sessions(club_id, session_on);

-- ============================================================================
-- Vistas útiles
-- ============================================================================

CREATE VIEW v_current_squads AS
 SELECT c.id as club_id, p.person_id, pu.common_name, pl.position_main,
        ct.effective_until, ct.wage_monthly
 FROM contracts ct
 JOIN players pl ON pl.person_id = ct.person_id
 JOIN persons pu ON pu.id = ct.person_id
 JOIN (SELECT person_id, MAX(effective_from) AS eff
       FROM contracts WHERE status = 'vigente' GROUP BY person_id) cur
   ON cur.person_id = ct.person_id AND cur.eff = ct.effective_from
 JOIN clubs c ON c.id = ct.club_id;

CREATE VIEW v_contract_expiries AS
 SELECT club_id, person_id, effective_until
 FROM contracts
 WHERE status = 'vigente' AND scope = 'primer_equipo'
   AND date(effective_until) <= date('now', '+12 months');

CREATE VIEW v_active_injuries AS
 SELECT i.person_id, i.injury_key, i.expected_return
 FROM injuries i WHERE i.actual_return IS NULL;

-- ============================================================================
-- Activar foreign keys (debe ser al final)
-- ============================================================================

PRAGMA foreign_keys = ON;