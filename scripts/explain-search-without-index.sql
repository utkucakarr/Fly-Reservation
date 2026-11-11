-- Development database only. Shows the plan of the flight search WITHOUT any index that could help it, then WITH the composite index.
-- PostgreSQL DDL is transactional: the DROP INDEX lines below are undone by ROLLBACK, no index is really lost.
-- While the transaction is open, the Flights table is locked: API requests to flights wait (a few milliseconds).
--
-- Run (PowerShell, repo root):
--   Get-Content scripts/explain-search-without-index.sql | docker exec -i flyreservation-postgres psql -U flyuser -d flyreservation

-- No padding and no row counts: the plan lines are long, this keeps the output readable in a screenshot
\set QUIET on
\pset format unaligned
\pset footer off
\set QUIET off

BEGIN;

DROP INDEX "IX_Flights_Origin_Destination_DepartureTime";
-- The unique (FlightNumber, DepartureTime) index must go too. Otherwise the planner walks that whole index
-- for the date range (measured 2026-11-11: about 3 ms) instead of showing the Seq Scan of a table with no usable index
DROP INDEX "IX_Flights_FlightNumber_DepartureTime";

-- First run warms the cache, its output is hidden. The second run is the one to compare
\o /dev/null
EXPLAIN ANALYZE
SELECT * FROM "Flights"
WHERE "Origin" = 'IST' AND "Destination" = 'AYT'
  AND "DepartureTime" >= '2026-12-15 00:00+00' AND "DepartureTime" < '2026-12-16 00:00+00'
ORDER BY "DepartureTime";
\o

\echo
\echo '========== WITHOUT an index for this search =========='
EXPLAIN ANALYZE
SELECT * FROM "Flights"
WHERE "Origin" = 'IST' AND "Destination" = 'AYT'
  AND "DepartureTime" >= '2026-12-15 00:00+00' AND "DepartureTime" < '2026-12-16 00:00+00'
ORDER BY "DepartureTime";

ROLLBACK;

\o /dev/null
EXPLAIN ANALYZE
SELECT * FROM "Flights"
WHERE "Origin" = 'IST' AND "Destination" = 'AYT'
  AND "DepartureTime" >= '2026-12-15 00:00+00' AND "DepartureTime" < '2026-12-16 00:00+00'
ORDER BY "DepartureTime";
\o

\echo
\echo '========== WITH the index (after ROLLBACK) =========='
EXPLAIN ANALYZE
SELECT * FROM "Flights"
WHERE "Origin" = 'IST' AND "Destination" = 'AYT'
  AND "DepartureTime" >= '2026-12-15 00:00+00' AND "DepartureTime" < '2026-12-16 00:00+00'
ORDER BY "DepartureTime";
