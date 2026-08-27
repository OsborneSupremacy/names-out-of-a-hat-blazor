-- The token that routes a participant's gift ideas email to their row.
--
-- Separate from gift_idea because the two have opposite lifetimes. A token is issued once, when
-- invitations go out and the address carrying it is mailed; a gift_idea row appears only if the
-- participant writes back, and then keeps appearing. Neither table can hold the other's rows.
--
-- Separate from participant for the reason the ideas are: whoever holds the plaintext of this token
-- can write to the exchange, so it is a credential, and a credential does not belong in the row
-- that every organizer-facing query selects.
--
-- Only the hash is stored, as LoginTokenProvider stores only the hash of a magic link token. A dump
-- of this table lets nobody submit anything. Sixty-four characters is what hex-encoded SHA-256
-- takes, which is what Convert.ToHexString produces.
CREATE TABLE gift_idea_token (
    gift_idea_token_id UUID PRIMARY KEY,
    participant_id     UUID NOT NULL,
    token_hash         VARCHAR(64) NOT NULL,
    issued_at          TIMESTAMPTZ NOT NULL
)
