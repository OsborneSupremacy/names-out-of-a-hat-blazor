-- Everybody the application knows about, identified by email address.
--
-- Organizers and participants are the same kind of thing and live here together: an organizer is
-- a person a hat points at, nothing more. One row per email address for the whole system, so a
-- name is stored exactly once no matter how many exchanges somebody appears in.
--
-- No column is nullable and none has a default. Absence is spelled with a value instead --
-- '00000000-0000-0000-0000-000000000000' for an id, the minimum timestamp for a date, and the
-- empty string for text -- so reading a row never means asking whether a column is there.
CREATE TABLE person (
    person_id UUID PRIMARY KEY,
    name      VARCHAR(100) NOT NULL,
    email     VARCHAR(254) NOT NULL
)
