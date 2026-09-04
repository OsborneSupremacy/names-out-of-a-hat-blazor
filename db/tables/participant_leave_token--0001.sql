-- The token that lets a participant leave their gift exchange from the link in their invitation.
--
-- Separate from gift_idea_token even though the two are shaped alike, because they authorise
-- different things and a token that could do either would be the wrong trade in both directions. A
-- gift ideas token is handed to other participants by the Ask, so that somebody can be asked what
-- they would like; that is a fine thing to widen and a terrible thing to attach a removal to.
--
-- Issued when invitations go out, alongside the gift ideas tokens and for the same reason: that is
-- the first moment there is an email going to this person to carry it, and a token nobody has been
-- told is only a row. The organizer is skipped -- there is no leaving an exchange you are running,
-- and the surest way to say so is for no token of theirs to exist to be found.
--
-- Only the hash is stored, as gift_idea_token stores only the hash. Sixty-four characters is what
-- hex-encoded SHA-256 takes.
--
-- These rows are deleted with the participant, unlike the do_not_add lists: a token whose
-- participant is gone routes nowhere, and what has to survive the removal is the refusal, not the
-- credential.
CREATE TABLE participant_leave_token (
    participant_leave_token_id UUID PRIMARY KEY,
    participant_id             UUID NOT NULL,
    token_hash                 VARCHAR(64) NOT NULL,
    issued_at                  TIMESTAMPTZ NOT NULL
)
