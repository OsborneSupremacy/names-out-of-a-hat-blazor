-- Who each participant is allowed to draw.
--
-- Both columns are participant ids, not person ids: eligibility is a fact about this hat, and the
-- same two people may be eligible for each other in one exchange and not in another.
CREATE TABLE participant_eligible_recipient (
    participant_eligible_recipient_id UUID PRIMARY KEY,
    participant_id                    UUID NOT NULL,
    eligible_participant_id           UUID NOT NULL
)
