using System.Collections.Generic;
using UniFutsal.Core.Domain.Clubs;
using UniFutsal.Core.Domain.Competitions;
using UniFutsal.Core.Domain.Geography;
using UniFutsal.Core.Domain.Matches;
using UniFutsal.Core.Domain.People;

namespace UniFutsal.Core.Domain
{
    /// <summary>
    /// Contenedor central del mundo del juego en memoria.
    /// El núcleo trabaja siempre con este objeto, nunca con SQLite directamente.
    /// </summary>
    public class World
    {
        // ===== Metadatos =====
        public string WorldSeed { get; set; } = "default";
        public string WorldDate { get; set; } = "2026-07-01";
        public string SchemaVersion { get; set; } = "1";
        public long CurrentSeasonId { get; set; }

        // ===== D1: Geografía =====
        public List<Confederation> Confederations { get; set; } = new List<Confederation>();
        public List<Country> Countries { get; set; } = new List<Country>();
        public List<Region> Regions { get; set; } = new List<Region>();
        public List<Venue> Venues { get; set; } = new List<Venue>();

        // ===== D2: Personas =====
        public List<Person> Persons { get; set; } = new List<Person>();
        public List<Player> Players { get; set; } = new List<Player>();
        public List<Staff> StaffMembers { get; set; } = new List<Staff>();
        public List<Referee> Referees { get; set; } = new List<Referee>();

        // ===== D3: Clubes =====
        public List<Club> Clubs { get; set; } = new List<Club>();
        public List<Contract> Contracts { get; set; } = new List<Contract>();

        // ===== D4: Competiciones =====
        public List<Season> Seasons { get; set; } = new List<Season>();
        public List<Competition> Competitions { get; set; } = new List<Competition>();
        public List<CompetitionEntry> CompetitionEntries { get; set; } = new List<CompetitionEntry>();

        // ===== D5: Partidos =====
        public List<Match> Matches { get; set; } = new List<Match>();

        // ===== Índices de búsqueda rápida (se construyen en IndexAll) =====
        private Dictionary<long, Country> _countriesById = new Dictionary<long, Country>();
        private Dictionary<long, Club> _clubsById = new Dictionary<long, Club>();
        private Dictionary<long, Person> _personsById = new Dictionary<long, Person>();
        private Dictionary<long, Player> _playersByPersonId = new Dictionary<long, Player>();
        private Dictionary<long, Venue> _venuesById = new Dictionary<long, Venue>();
        private Dictionary<long, Competition> _competitionsById = new Dictionary<long, Competition>();
        private Dictionary<long, Season> _seasonsById = new Dictionary<long, Season>();
        private bool _indexed;

        /// <summary>
        /// Construye los índices de búsqueda rápida.
        /// Debe llamarse una vez después de cargar el mundo.
        /// </summary>
        public void IndexAll()
        {
            _countriesById = new Dictionary<long, Country>();
            foreach (var c in Countries)
            {
                _countriesById[c.Id] = c;
            }

            _clubsById = new Dictionary<long, Club>();
            foreach (var c in Clubs)
            {
                _clubsById[c.Id] = c;
            }

            _personsById = new Dictionary<long, Person>();
            foreach (var p in Persons)
            {
                _personsById[p.Id] = p;
            }

            _playersByPersonId = new Dictionary<long, Player>();
            foreach (var p in Players)
            {
                _playersByPersonId[p.PersonId] = p;
            }

            _venuesById = new Dictionary<long, Venue>();
            foreach (var v in Venues)
            {
                _venuesById[v.Id] = v;
            }

            _competitionsById = new Dictionary<long, Competition>();
            foreach (var c in Competitions)
            {
                _competitionsById[c.Id] = c;
            }

            _seasonsById = new Dictionary<long, Season>();
            foreach (var s in Seasons)
            {
                _seasonsById[s.Id] = s;
            }

            _indexed = true;
        }

        /// <summary>
        /// Resuelve todas las referencias cruzadas entre entidades.
        /// Debe llamarse después de IndexAll().
        /// </summary>
        public void ResolveReferences()
        {
            // Countries → Confederation
            foreach (var country in Countries)
            {
                foreach (var conf in Confederations)
                {
                    if (conf.Id == country.ConfederationId)
                    {
                        country.Confederation = conf;
                        break;
                    }
                }
            }

            // Clubs → Country, Venue
            foreach (var club in Clubs)
            {
                club.Country = GetCountryById(club.CountryId);
                if (club.VenueId.HasValue)
                {
                    club.Venue = GetVenueById(club.VenueId.Value);
                }
            }

            // Players → Person
            foreach (var player in Players)
            {
                player.Person = GetPersonById(player.PersonId);
            }

            // Contracts → Person, Club
            foreach (var contract in Contracts)
            {
                contract.Person = GetPersonById(contract.PersonId);
                contract.Club = GetClubById(contract.ClubId);
            }

            // CompetitionEntries → Competition, Club, Season
            foreach (var entry in CompetitionEntries)
            {
                entry.Competition = GetCompetitionById(entry.CompetitionId);
                entry.Season = GetSeasonById(entry.SeasonId);
                if (entry.ClubId.HasValue)
                {
                    entry.Club = GetClubById(entry.ClubId.Value);
                }
            }
        }

        // ===== Métodos de lookup =====

        public Country? GetCountryById(long id)
        {
            EnsureIndexed();
            Country? result;
            if (_countriesById.TryGetValue(id, out result))
            {
                return result;
            }
            return null;
        }

        public Club? GetClubById(long id)
        {
            EnsureIndexed();
            Club? result;
            if (_clubsById.TryGetValue(id, out result))
            {
                return result;
            }
            return null;
        }

        public Person? GetPersonById(long id)
        {
            EnsureIndexed();
            Person? result;
            if (_personsById.TryGetValue(id, out result))
            {
                return result;
            }
            return null;
        }

        public Player? GetPlayerByPersonId(long personId)
        {
            EnsureIndexed();
            Player? result;
            if (_playersByPersonId.TryGetValue(personId, out result))
            {
                return result;
            }
            return null;
        }

        public Venue? GetVenueById(long id)
        {
            EnsureIndexed();
            Venue? result;
            if (_venuesById.TryGetValue(id, out result))
            {
                return result;
            }
            return null;
        }

        public Competition? GetCompetitionById(long id)
        {
            EnsureIndexed();
            Competition? result;
            if (_competitionsById.TryGetValue(id, out result))
            {
                return result;
            }
            return null;
        }

        public Season? GetSeasonById(long id)
        {
            EnsureIndexed();
            Season? result;
            if (_seasonsById.TryGetValue(id, out result))
            {
                return result;
            }
            return null;
        }

        /// <summary>
        /// Devuelve los contratos vigentes de un club.
        /// </summary>
        public List<Contract> GetActiveContractsByClub(long clubId)
        {
            var result = new List<Contract>();
            foreach (var contract in Contracts)
            {
                if (contract.ClubId == clubId && contract.Status == ContractStatus.Active)
                {
                    result.Add(contract);
                }
            }
            return result;
        }

        /// <summary>
        /// Devuelve los jugadores de un club (contratos vigentes de primer equipo).
        /// </summary>
        public List<Player> GetPlayersByClub(long clubId)
        {
            var result = new List<Player>();
            foreach (var contract in Contracts)
            {
                if (contract.ClubId == clubId
                    && contract.Status == ContractStatus.Active
                    && contract.Scope == ContractScope.FirstTeam)
                {
                    var player = GetPlayerByPersonId(contract.PersonId);
                    if (player != null)
                    {
                        result.Add(player);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Devuelve las inscripciones activas de una competición.
        /// </summary>
        public List<CompetitionEntry> GetActiveEntriesByCompetition(long competitionId)
        {
            var result = new List<CompetitionEntry>();
            foreach (var entry in CompetitionEntries)
            {
                if (entry.CompetitionId == competitionId && entry.Status == EntryStatus.Active)
                {
                    result.Add(entry);
                }
            }
            return result;
        }

        private void EnsureIndexed()
        {
            if (!_indexed)
            {
                IndexAll();
            }
        }
    }
}