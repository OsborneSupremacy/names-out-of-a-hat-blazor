-- A person in one gift exchange.
--
-- picked_recipient_id references another row in this table, and eligibility is held in
-- participant_eligible_recipients by id. Referring to participants by id rather than by
-- display name is what allows two people with the same name in one exchange.
--
-- There are no foreign keys because DSQL does not support them, so the application owns
-- referential cleanup on delete.
CREATE TABLE participants (
    id                  UUID PRIMARY KEY,
    hat_id              UUID NOT NULL,
    name                VARCHAR(100) NOT NULL,
    email               VARCHAR(254) NOT NULL,
    picked_recipient_id UUID
)
