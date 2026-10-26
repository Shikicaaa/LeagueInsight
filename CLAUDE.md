# CLAUDE.md — LeagueInsight (LoL Stats)

Sajt za statistiku League of Legends igrača (mix op.gg + leagueofgraphs). Start: samo **EUNE** (`eun1` / `europe`), ranked redovi Solo/Duo (420) i Flex (440).

Pun dizajn: [`docs/dizajn-sistema.md`](docs/dizajn-sistema.md) — šema baze, endpointi, keš ključevi, analitika. Ovaj fajl sadrži pravila koja se **ne smeju prekršiti** i odluke za ciljanu skalu. Kad se ovaj fajl i dizajn dokument razlikuju, važi ovaj fajl (sekcija „Skala“ namerno menja neke odluke iz dokumenta).

## Ciljana skala

| Metrika | Cilj |
| --- | --- |
| Registrovani / indeksirani korisnici | ≥ 2.000.000 |
| DAU | ≥ 100.000 |
| API zahtevi (procena) | ~3 M/dan, prosek ~35 rps, pik 300+ rps |
| Istovremene SignalR konekcije | 10–20k u piku |

**Riot budžet.** Merenje za 10k korisnika daje ~500k Riot poziva/dan (live game ~230k, praćenje ko je u igri ~100k, tuđi profili ~130k, korpus ~30k). Linearno na 100k DAU to je **~5 M/dan**, što je **više od podrazumevanog production limita** (500 / 10 s i 30.000 / 10 min po regionu ≈ 4,3 M/dan, ≈ 50 rps). Zato:

- Cilj je **≤ 3 M Riot poziva/dan** u proseku (rezerva za pikove), uz zahtev Riotu za veći limit kad sajt bude javan.
- Svaka nova funkcionalnost koja zove Riot mora da ima procenu „poziva po korisniku dnevno“ pre implementacije.
- Live game i polling su najskuplji — tu se prvo optimizuje (vidi niže).

## Nepregovarajuća pravila

1. **Korisnički request nikad ne čeka Riot.** Jedini izuzetak: prvi lookup Riot ID → PUUID (`account-v1`, sinhrono, 1 poziv). Sve ostalo ide kroz Hangfire red; API odmah vraća ono što ima (`lastUpdatedAt`, `isStale`, `refreshState`) ili `202`.
2. **Samo Worker zove Riot** (osim gornjeg izuzetka u Api-ju). Svaki poziv prolazi kroz `IRateLimiter` — nema direktnog `HttpClient` poziva van `LolStats.Riot`.
3. **Mečevi su nepromenljivi.** Postgres je izvor istine i čuva ih zauvek. Upis `ON CONFLICT DO NOTHING` na `match_id`. Valkey se može obrisati u bilo kom trenutku bez gubitka podataka.
4. **Nikad ne povlači meč koji već postoji u bazi.** Uvek diff (`WHERE match_id = ANY(@ids)`) pre fetcha. Jedan meč puni podatke za svih 10 učesnika.
5. **Inkrementalni refresh:** lista ID-jeva od `last_match_ts` (`startTime`), `count=10`.
6. **Limite čitaj iz headera** `X-App-Rate-Limit`, `X-Method-Rate-Limit` (+ `-Count`), ne hardkoduj. Na 429: pauziraj ceo bucket za `Retry-After` i `BackgroundJob.Schedule` — ne troši retry pokušaj.
7. **Retry:** 5xx/timeout → eksponencijalni backoff, max 5 pokušaja, timeout 10 s. 404 na meču → bez ponavljanja, meč označen kao nedostupan.
8. **PUUID je enkriptovan po API ključu.** Uvek čuvaj `riot_id_normalized` (lowercase, bez razmaka) da bi igrači mogli da se remapiraju pri prelasku na production ključ. `matchId` je stabilan.
9. **Domen ne zna za infrastrukturu.** Keš, red i publisher evenata su iza `ICache`, `IJobQueue`, `IEventPublisher` (`LolStats.Domain`).
10. **Outbox u istoj transakciji** sa podacima. Nikad ne objavljuj event van transakcije upisa.
11. **`LolStats.Analytics` ne zna za bazu ni Riot** — prima DTO meča/timelinea, vraća features. Testira se snimljenim JSON fixture-ima, bez mreže.
12. Kolona `platform` postoji svuda, iako je danas uvek `eun1`. Ne hardkoduj region u logici — samo u konfiguraciji (`Riot:Platform`, `Riot:Region`).

## Arhitektura (kratko)

```
Browser (React) ──HTTP──> LolStats.Api ──> HybridCache (L1 mem + L2 Valkey) ──> Postgres
       ^                       │ enqueue
       └──SignalR──────────────┤
                               v
                    Hangfire redovi p0..p3 ──> LolStats.Worker ──> IRateLimiter ──> Riot API
                                                     │
                                                     └──> Postgres (podaci + outbox) ──> OutboxDispatcher ──> handleri / SignalR
Data Dragon CDN ← frontend direktno (ikonice, itemi, šampioni; keš po patchu)
```

### Tokovi (dijagrami aktivnosti)

1. **Otvaranje profila:** PUUID u kešu/bazi? → da: vrati profil; ako je stariji od 10 min, enqueue `RefreshPlayerJob`. Ne: `account-v1` (sinhrono) → ne postoji: `404` + `neg:riotid:*` 10 min; postoji: enqueue `RefreshPlayerJob`, vrati profil.
2. **`RefreshPlayerJob` (p0):** drži `lock:job:{puuid}` do kraja → summoner + league (2 poziva, LP snapshot) → ako se rank promenio: outbox `RankChanged` → ID-jevi od `last_match_ts` → diff sa bazom → nema novih: ažuriraj `last_refreshed_at`, otpusti lock → ima: povuci match + timeline kroz limiter → upis u jednoj transakciji (mečevi, učesnici, outbox) → ažuriraj `data_version`, otpusti lock, SignalR.
3. **Poziv Riotu:** token (app + method bucket) → nema: p0/p1 kratko čekaju u procesu, p2/p3 se vraćaju u red → HTTP → 429: pauza bucketa → ne-2xx: obrada greške → 2xx: usaglasi bucket iz `*-Count` headera.
4. **Učitaj još:** ima 10 mečeva u bazi → vrati stranicu odmah (0 Riot poziva). Inače `POST load-more` → `LoadMoreMatchesJob` (p1), ID-jevi od `start=N`, fetch samo nepostojećih → SignalR `MatchesUpdated` → React ponovi upit.
5. **Detalji meča:** timeline u bazi → build + grafovi (item eventi + Data Dragon za patch meča) → `200`. Inače `202` + `FetchMatchJob` (p1) → SignalR → frontend ponovi upit.
6. **Playstyle:** `MatchIngested` (outbox, 1–2 s) → features po učesniku → uzorak ≥ 10 u roli? z-skor prema baseline (rola, tier, patch) : tagovi uz oznaku „malo podataka“ → upis `player_playstyle` (nova verzija ključa) → SignalR `PlaystyleUpdated`. **Nula Riot poziva.** Računa se samo za praćene igrače (profil otvoren u poslednjih 30 dana).

## Skala: odluke koje menjaju/dopunjuju dizajn dokument

### Riot pozivi

- **Budžet po izvoru.** Limiter ima kvote po kategoriji (profil, live, praćenje, korpus/crawl), ne samo po prioritetu. `p2`/`p3` rade samo kad je bucket > 30% (iz dizajna) **i** dnevni budžet kategorije nije potrošen.
- **Live game:** jedan spectator odgovor sadrži svih 10 igrača → upiši `live:{puuid}` za **sve** učesnike (i keš po `gameId`). Rank/summoner tuđih igrača u live igri kešira se dok igra traje (~30–45 min), ne 5 min. „Nije u igri“ = negativni keš 60 s.
- **Praćenje ko je u igri:** polling samo dok je pratilac online (aktivna SignalR konekcija), adaptivan interval (npr. 2 min → 5 min posle neuspeha), i preskoči igrače za koje već imamo `live:*` iz tuđeg spectator odgovora.
- **Tuđi profili:** refresh cooldown `cooldown:refresh:{puuid}` 120 s je minimum; pasivni refresh (otvaranje profila) tek kad je profil stariji od 10 min.
- **Tier 9 drugih igrača** u detaljima meča: lazy, kroz `league:{puuid}`; nikad eager pri ingestu.
- **p3 crawl** je prvi koji se gasi pod opterećenjem; konfigurabilan kill-switch.
- Metrike: brojač Riot poziva po endpointu, kategoriji i prioritetu (OpenTelemetry), alarm na 80% dnevnog budžeta i na stopu 429.

### Više instanci

- Api i Worker rade u **više instanci** od starta produkcije → limiter **mora** biti Lua skripta u Valkeyju (atomično uzimanje tokena). In-memory `System.Threading.RateLimiting` samo za lokalni razvoj.
- SignalR sa **Valkey backplane-om** (`AddStackExchangeRedis`) ili sticky sessions + backplane.
- Sve deduplikacije preko Valkeyja (`SET NX`), nikad preko in-process stanja.

### Red poslova

- Jedan job po meču na ~0,5 M+ mečeva dnevno opterećuje Hangfire storage u Postgresu. Hangfire koristi **zasebnu Postgres bazu/instancu** (ne istu kao podaci), a `FetchMatchJob` za isti refresh se **batchuje** (jedan job za listu matchId-jeva). Ako Hangfire postane usko grlo, prelazak na Valkey storage ili RabbitMQ ide kroz `IJobQueue` — domen se ne menja.

### Postgres

- Napiši sve teške upite (istorija, champion stats, mains, baselines) u Dapperu i proveri `EXPLAIN ANALYZE` na realnoj zapremini.
- **Particionisanje po vremenu** (mesečno, po `game_start`) za `matches`, `match_participants`, `participant_features` — milioni redova mesečno.
- **`participant_frames` i `item_events`/`skill_events` kompaktno:** po jedan red po (`match_id`, `participant_id`) sa nizovima (`int[]`) ili kompresovanim `jsonb`, umesto reda po minutu/eventu (210+ redova po meču inače). Analitika ih čita po meču, ne pojedinačno.
- Raw JSON meča i timelinea ide u **objekt storage (S3/MinIO)**, ne u `jsonb` — `raw_ref` je ključ. (Zatvara otvoreno pitanje iz dizajna.)
- Read replika za Api upite kad pređemo ~200 rps na bazi; Worker piše na primarnu.
- Connection pooling (PgBouncer, transaction mode) ispred baze.
- Paginacija uvek kursorska po `(game_start, match_id)`, nikad `OFFSET`.

### Keš

- Ključevi i TTL-ovi su u dizajn dokumentu (sekcija „Keširanje u Valkeyju“). Obrasci: verzionisani ključevi (`v{ver}` = poslednji ingestovani `matchId`) umesto brisanja; stale-while-revalidate; HybridCache za zaštitu od stampeda; negative caching; Riot ID u ključu normalizovan.
- `match:{matchId}` bez TTL-a, LRU (`maxmemory-policy allkeys-lru`) — Valkey mora imati postavljen `maxmemory`.

### Frontend

- TanStack Query `staleTime` prati backend `isStale`; na SignalR `MatchesUpdated` → `invalidateQueries` za profil i istoriju.
- Data Dragon direktno sa CDN-a, keš po verziji patcha; itemi se tumače po patchu meča (`gameVersion`).
- URL: `/summoner/eune/Ime-TAG`; ime i tag se razdvajaju po **poslednjoj** crtici.
- Profilne stranice su SEO-bitne na ovoj skali → planiraj prerender profila (faza 5).

## Struktura solutiona

```
LeagueInsight/
├─ backend/
│  ├─ LolStats.slnx
│  ├─ docker-compose.yaml    postgres (lolstats + hangfire baze), valkey
│  ├─ src/
│  │  ├─ LolStats.Api          endpointi, HybridCache, SignalR hub, enqueue jobova
│  │  ├─ LolStats.Worker       Hangfire server: ingestion, analitika, noćni jobovi, OutboxDispatcher
│  │  ├─ LolStats.Riot         typed klijent, DTO-ovi, IRateLimiter, routing (eun1/europe)
│  │  ├─ LolStats.Analytics    feature extraction, perf_score, playstyle, zone mape, baselines
│  │  ├─ LolStats.Data         EF Core DbContext, migracije, Dapper upiti, outbox
│  │  ├─ LolStats.Domain       entiteti, eventi, interfejsi (ICache, IJobQueue, IEventPublisher)
│  │  └─ LolStats.Contracts    response modeli (izvor za generisane TS tipove)
│  └─ tests/
│     ├─ LolStats.Analytics.Tests   snimljeni JSON mečevi kao fixture
│     └─ LolStats.Api.Tests
├─ web/                     React + Vite (src/api generisan iz OpenAPI, src/features/{profile,matches,champions})
└─ docs/                    dizajn-sistema.md
```

Zavisnosti: samo Worker (i Api za `account-v1`) zavisi od `LolStats.Riot`. `Analytics` zavisi samo od `Domain`/DTO-ova.

## Stack

.NET 10 (ASP.NET Core, SignalR), Hangfire (Postgres storage, zasebna baza), PostgreSQL + EF Core (Npgsql) za migracije/CRUD + Dapper za agregacije, Valkey + HybridCache, typed `HttpClient` + `Microsoft.Extensions.Http.Resilience`, React + Vite + TanStack Query + React Router + Recharts, Data Dragon.

Odbačeno za sada: Kafka (nema prioritete/odložene poruke — tek kad postoji više nezavisnih potrošača outboxa), RabbitMQ/MassTransit v9 (komercijalan), Next.js SSR.

## Konvencije

- Javni API pod `/api/v1/...`; spisak endpointa u dizajn dokumentu („Javni API“).
- Svaki odgovor ima `dataVersion` i `lastUpdatedAt`. Greške kao `application/problem+json` (RFC 9457).
- `POST .../refresh` → `202 {jobId}` ili `429 {retryAfterSeconds}`.
- SignalR hub `/hubs/player`, grupa `player:{puuid}`; poruke `RefreshProgress`, `MatchesUpdated`, `PlaystyleUpdated`, `RefreshFailed`.
- Konfiguracija ne pominje proizvod: `Cache:ConnectionString`, `Riot:ApiKey`, `Riot:Platform`, `Riot:Region`. API ključ nikad u repou — user-secrets / env.
- TS tipovi na frontendu se **generišu** iz OpenAPI (`LolStats.Contracts`), ne pišu ručno.
- Analitika: tagovi su percentili/z-skor po (rola, tier, patch), nikad apsolutni pragovi. Tag tek uz uzorak ≥ 10 mečeva.

## Komande

Projekat još nije inicijalizovan. Kad se postavi, ovde dopiši stvarne komande; planirano:

```bash
# iz backend/
docker compose up -d            # postgres, valkey (lokalno)
dotnet build
dotnet test
dotnet ef migrations add <Ime> -p src/LolStats.Data -s src/LolStats.Api
dotnet run --project src/LolStats.Api
dotnet run --project src/LolStats.Worker
# iz web/
npm install && npm run dev
```

## Faze

1. Kostur i ingestion (šema, Riot klijent + limiter, `RefreshPlayerJob`, `FetchMatchJob`, profil + 10 mečeva, keš).
2. Detalji meča (timeline, build po minutu, skill order, `perf_score`, MVP/ACE, SignalR).
3. Playstyle (zone mape, features, noćni baselines, tagovi) → prijava za production ključ.
4. Masteries i mains (`p3` crawl, leaderboard).
5. Proširenja (LP graf, live game, champion stats, SEO).

Pre faze 5 (live game) obavezno: budžet po kategoriji i deljenje spectator odgovora na svih 10 igrača — bez toga live game sam troši ~polovinu dnevnog limita.
