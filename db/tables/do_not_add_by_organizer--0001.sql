-- Addresses that must never be added to any gift exchange run by one particular organizer.
--
-- The middle of the three lists, and the one somebody reaches for when the problem is a person
-- rather than an event: an organizer who adds them to a new exchange every year without asking.
-- Blocking the exchange they just left would buy them eleven months.
--
-- Keyed by the organizer's address rather than by person_id, deliberately. An address is already
-- the unique identity of a person here -- uq_person_email says so -- and holding the address means
-- the check needs no person lookup before it can run. That is what lets all three lists be
-- consulted concurrently from what the request already carries, which for every caller is an
-- organizer's email and nothing else.
--
-- Both addresses are stored lower-cased and trimmed, for the reason given in
-- do_not_add_to_exchange--0001.sql.
CREATE TABLE do_not_add_by_organizer (
    do_not_add_by_organizer_id UUID PRIMARY KEY,
    -- The organizer being blocked, lower-cased and trimmed.
    organizer_email_normalized VARCHAR(254) NOT NULL,
    -- Who is refusing them, lower-cased and trimmed.
    email_normalized           VARCHAR(254) NOT NULL,
    created_at                 TIMESTAMPTZ NOT NULL
)
