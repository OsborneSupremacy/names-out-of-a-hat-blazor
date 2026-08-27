-- A person taking part in one gift exchange.
--
-- The row carries no name or address of its own; those belong to the person it points at. What it
-- adds is everything true only within this hat -- who they drew, and, through
-- participant_eligible_recipient, who they were allowed to draw.
--
-- picked_recipient_participant_id references another row in this table, and is the all-zero UUID
-- until the hat is shaken. Referring to participants by id rather than by display name is what
-- allows two people with the same name in one exchange.
--
-- There are no foreign keys because DSQL does not support them, so the application owns
-- referential cleanup on delete.
CREATE TABLE participant (
    participant_id                  UUID PRIMARY KEY,
    hat_id                          UUID NOT NULL,
    person_id                       UUID NOT NULL,
    picked_recipient_participant_id UUID NOT NULL
)
