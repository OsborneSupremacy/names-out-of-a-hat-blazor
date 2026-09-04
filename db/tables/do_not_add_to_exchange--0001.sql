-- Addresses that must never be added to one particular gift exchange again.
--
-- Written when somebody leaves an exchange from the link in their invitation. Leaving would
-- otherwise be undone by the organizer typing the same address back in, which is not a hypothetical
-- failure: the organizer is told that somebody left and is asked to draw names again, so the
-- participant list is the first thing they open afterwards.
--
-- The narrowest of the three do-not-add lists, and the only one recorded without being asked for.
-- Somebody who leaves has said what they want about this exchange by leaving it; the other two
-- lists are choices they make on the same page, and are theirs to decline.
--
-- Rows here outlive the participant they were written for, and outlive the hat itself. That is the
-- point of the table rather than an oversight in the cleanup: a row deleted along with the
-- participant would block nothing, since the block exists precisely for the state where the
-- participant row is gone. DeleteHatAsync leaves them too, so a re-created exchange of the same
-- name does not quietly become a way back in.
--
-- The address is stored lower-cased and trimmed, so a lookup is plain equality and matching does
-- not depend on how either party happened to type it. VARCHAR(254) is the length the validators
-- allow an address to be.
CREATE TABLE do_not_add_to_exchange (
    do_not_add_to_exchange_id UUID PRIMARY KEY,
    -- The exchange they left. Not a foreign key, like everything else here.
    hat_id                    UUID NOT NULL,
    -- Lower-cased and trimmed by the data layer before it reaches this column.
    email_normalized          VARCHAR(254) NOT NULL,
    created_at                TIMESTAMPTZ NOT NULL
)
