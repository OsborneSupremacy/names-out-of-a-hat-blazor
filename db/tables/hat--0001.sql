-- One gift exchange, owned by the person who created it.
--
-- The organizer is a person_id rather than a name and address carried inline, so renaming
-- somebody is one write to person and nothing here has to move with it.
--
-- Lengths match what FluentValidation and the request JSON schemas already enforce, so the
-- database rejects anything that reaches it by another route.
CREATE TABLE hat (
    hat_id                   UUID PRIMARY KEY,
    organizer_person_id      UUID NOT NULL,
    name                     VARCHAR(50) NOT NULL,
    -- Lower-cased, trimmed copy of name, written by the application. Carried as a column rather
    -- than an expression index so that uniqueness of a hat name per organizer does not depend on
    -- DSQL supporting indexes over expressions.
    name_normalized          VARCHAR(50) NOT NULL,
    -- The valid set lives in GiftExchange.Library.Models.HatStatuses. There is no reference table
    -- to join to: DSQL has no foreign keys, so one could never have constrained this column.
    status                   VARCHAR(30) NOT NULL,
    additional_information   VARCHAR(2000) NOT NULL,
    price_range              VARCHAR(50) NOT NULL,
    -- The minimum timestamp until invitations are queued.
    invitations_queued_at    TIMESTAMPTZ NOT NULL,
    -- Where the send came from, so an abuse report can be tied to an origin as well as to the
    -- organizer's verified address. Empty until invitations are sent, and never supplied by a
    -- client. 45 characters is the longest an IPv6 address can be.
    --
    -- Deliberately not the inet type: DSQL's support for it is unverified, and this value is only
    -- ever read by a human investigating a report.
    invitations_sent_from_ip VARCHAR(45) NOT NULL,
    created_at               TIMESTAMPTZ NOT NULL
)
