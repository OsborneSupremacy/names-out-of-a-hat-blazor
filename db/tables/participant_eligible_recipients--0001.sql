-- Who each participant is allowed to draw.
--
-- Replaces the DynamoDB string set of names. An empty eligibility list is now simply zero
-- rows, where DynamoDB refused to store an empty set at all.
CREATE TABLE participant_eligible_recipients (
    participant_eligible_recipients_id UUID PRIMARY KEY,
    participant_id                     UUID NOT NULL,
    eligible_participant_id            UUID NOT NULL
)
