-- Development only: inserts 100,000 random future flights to make slow queries visible.
-- Run: Get-Content scripts/seed-flights.sql | docker exec -i flyreservation-postgres psql -U flyuser -d flyreservation
WITH airports AS (
    SELECT ARRAY['IST', 'SAW', 'ESB', 'ADB', 'AYT', 'DLM', 'BJV', 'TZX', 'GZT', 'ERZ'] AS code
),
seed AS (
    SELECT g,
           floor(random() * 10)::int    AS origin,
           1 + floor(random() * 9)::int AS shift,   -- 1..9, so destination is never the origin
           floor(random() * 365)::int   AS day,
           floor(random() * 96)::int    AS slot     -- 15-minute slots in a day
    FROM generate_series(1, 100000) AS g
)
INSERT INTO "Flights" ("FlightNumber", "Origin", "Destination", "DepartureTime", "ArrivalTime",
                       "TotalSeats", "AvailableSeats", "Price")
SELECT chr(65 + (g / 10000) % 26) || chr(65 + g / 260000) || lpad((g % 10000)::text, 4, '0'),  -- AA0001, AA0002, ...
       code[1 + origin],
       code[1 + (origin + shift) % 10],
       timestamptz '2026-12-01 00:00+00' + day * interval '1 day' + slot * interval '15 minutes',
       timestamptz '2026-12-01 00:00+00' + day * interval '1 day' + slot * interval '15 minutes' + interval '90 minutes',
       180,
       180,
       round((500 + random() * 2500)::numeric, 2)
FROM seed, airports;

-- Refresh planner statistics so EXPLAIN shows realistic plans right away
ANALYZE "Flights";
