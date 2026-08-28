-- What SES said happened to one message sent to one participant.
--
-- The application sends every participant email and then hears nothing. A typo in an address does
-- not fail the send -- SES accepts the message, the bounce arrives minutes later on a channel
-- nobody was listening to, and the organizer's exchange quietly has a person in it who was never
-- told whose name they drew. This table is the ear.
--
-- One row per outbound message rather than one per participant, keyed on the SES message id. That
-- is the only identifier both sides of the conversation share: the send returns it and every event
-- carries it, so an event can find its row without the sender having written one first. It also
-- keeps the invitation and the completion email apart, which a row per participant could not.
--
-- Its own table, not columns on participant, for the reason gift_idea_token gives: DSQL cannot
-- ALTER COLUMN, so a column added to a populated table can be neither defaulted nor made NOT NULL,
-- and a new table is the only way any of this arrives non-nullable.
--
-- Written by the delivery event function alone. The sender writes nothing, because SES publishes a
-- Send event of its own the moment it accepts a message -- so one writer sees the whole lifecycle
-- and the sending path keeps needing no database access at all.
CREATE TABLE participant_email_delivery (
    participant_email_delivery_id UUID PRIMARY KEY,
    -- Who it was sent to, taken from the participant_id message tag on the send. A participant and
    -- not a person: the same address in two exchanges is two different questions.
    participant_id                UUID NOT NULL,
    -- INVITATION or COMPLETION, from the message_type tag. What distinguishes two rows that would
    -- otherwise both just be "an email to this participant".
    message_type                  VARCHAR(20) NOT NULL,
    -- The SES message id. Unique, and the key both writers upsert on.
    ses_message_id                VARCHAR(200) NOT NULL,
    -- The furthest the message is known to have got: SENT, DELIVERED, BOUNCED and the rest. Events
    -- are not ordered, so this only ever moves forwards -- see DeliveryStatus.RankOf.
    status                        VARCHAR(20) NOT NULL,
    -- Why, when the status is one that has a why: the bounce subtype and SMTP diagnostic, the
    -- reject reason. Empty for the statuses that do not. This is the part an organizer can act on,
    -- which is the whole reason a bounce is worth recording rather than counting.
    detail                        VARCHAR(500) NOT NULL,
    -- When SES says the event happened, not when we wrote it down.
    occurred_at                   TIMESTAMPTZ NOT NULL,
    updated_at                    TIMESTAMPTZ NOT NULL
)
