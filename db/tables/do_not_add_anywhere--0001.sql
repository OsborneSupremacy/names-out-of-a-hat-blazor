-- Addresses that must never be added to any gift exchange by anybody.
--
-- The widest of the three lists, and the closest thing this application has to an unsubscribe. Sent
-- mail has always been recorded -- participant_email_delivery knows perfectly well when somebody
-- bounced or complained -- but nothing ever stopped the next send. This does.
--
-- One column and one row per address, so the check is a single index seek on the same predicate the
-- other two lists use. There is no scope to carry: the answer does not depend on who is asking,
-- which is exactly what makes this the list worth having.
--
-- Nothing removes rows from this table. Somebody who changes their mind is added by an organizer
-- who asks them first, and there is no self-service way back in on purpose -- an address that can
-- un-block itself from a link in an email is an address anybody who reaches that inbox can
-- un-block.
CREATE TABLE do_not_add_anywhere (
    do_not_add_anywhere_id UUID PRIMARY KEY,
    -- Lower-cased and trimmed by the data layer before it reaches this column.
    email_normalized       VARCHAR(254) NOT NULL,
    created_at             TIMESTAMPTZ NOT NULL
)
