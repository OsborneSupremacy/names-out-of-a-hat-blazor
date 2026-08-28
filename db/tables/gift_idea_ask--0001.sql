-- One participant asking another for gift ideas about a third.
--
-- The original Ask needed no table. It asked the person whose name the asker drew, for ideas about
-- themselves, and gift_idea_token already routes that: a token names a participant, and what
-- arrives on it is what that participant wants. Asking somebody else breaks that identity apart.
-- Three different people are now involved -- whoever asked, whoever was asked, and whoever the
-- ideas are about -- and a row naming one participant cannot say which of the three it means.
--
-- So each ask is written down, with its own token. What arrives on that token is checked against
-- the helper, stored against the ask, and forwarded to the asker. Nothing about it touches the
-- subject's own gift_idea rows: these are somebody else's suggestions about them, not their words.
--
-- The subject is recorded rather than read back through the asker's pick, because the helper was
-- told a name and that name has to stay put. An organizer editing picks after invitations went out
-- would otherwise silently re-point an ask that has already been mailed.
--
-- Every column is stated at CREATE, for the reason gift_idea--0001.sql gives: DSQL cannot ALTER
-- COLUMN, so a column added later can be neither defaulted nor made NOT NULL.
CREATE TABLE gift_idea_ask (
    gift_idea_ask_id       UUID PRIMARY KEY,
    -- Who asked, and so where anything shared in reply is sent. Never named to the helper.
    asker_participant_id   UUID NOT NULL,
    -- Who was asked, and the only address a submission on this token may come from.
    helper_participant_id  UUID NOT NULL,
    -- Who the ideas are about. The asker's pick at the moment of asking, held here rather than
    -- followed later, so that the name in the helper's inbox is the name this row means.
    subject_participant_id UUID NOT NULL,
    -- Hex-encoded SHA-256, as gift_idea_token stores it and for the same reason: a dump of this
    -- table lets nobody submit anything.
    token_hash             VARCHAR(64) NOT NULL,
    issued_at              TIMESTAMPTZ NOT NULL
)
