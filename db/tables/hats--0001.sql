-- One gift exchange, owned by the organizer who created it.
--
-- Lengths match what FluentValidation and the request JSON schemas already enforce, so the
-- database rejects anything that reaches it by another route.
CREATE TABLE hats (
    id                     UUID PRIMARY KEY,
    organizer_email        VARCHAR(254) NOT NULL,
    organizer_name         VARCHAR(100) NOT NULL,
    name                   VARCHAR(50) NOT NULL,
    -- Lower-cased, trimmed copy of name, written by the application. Carried as a column
    -- rather than an expression index so that uniqueness of a hat name per organizer does
    -- not depend on DSQL supporting indexes over expressions.
    name_normalized        VARCHAR(50) NOT NULL,
    status                 VARCHAR(30) NOT NULL,
    additional_information VARCHAR(2000) NOT NULL DEFAULT '',
    price_range            VARCHAR(50) NOT NULL DEFAULT '',
    -- Null until invitations are queued, replacing the DateTimeOffset.MinValue sentinel
    -- carried by the DynamoDB model.
    invitations_queued_at  TIMESTAMPTZ,
    created_at             TIMESTAMPTZ NOT NULL
)
