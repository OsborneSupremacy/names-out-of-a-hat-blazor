-- What somebody suggested about another participant, in reply to being asked.
--
-- Deliberately not gift_idea. That table holds what a participant said about themselves and is
-- forwarded to whoever drew them; a row here is a third party's guess, forwarded to the one person
-- who asked for it, and the two must never be read as the same kind of thing. Mixing them would
-- mean a suggestion Dad made about Mom could reach Mom's giver as though Mom had written it.
--
-- Append-only, as gift_idea is, and for the same two reasons: the text is pulled out of an email,
-- which means a guess was made about where the quoted reply began, and an abuse report is
-- answerable only against what was actually sent.
--
-- The ask carries the rest. Who wrote this, who it is about and who it was sent to are all on the
-- gift_idea_ask row, so they are not repeated here where two copies could disagree.
CREATE TABLE contributed_gift_idea (
    contributed_gift_idea_id UUID PRIMARY KEY,
    gift_idea_ask_id         UUID NOT NULL,
    -- Length matches gift_idea.ideas, and the cap the application applies before the text reaches
    -- Comprehend. Both paths run the same content policy, so both need the same room.
    ideas                    VARCHAR(8000) NOT NULL,
    created_at               TIMESTAMPTZ NOT NULL,
    inbound_message_id       VARCHAR(255) NOT NULL
)
