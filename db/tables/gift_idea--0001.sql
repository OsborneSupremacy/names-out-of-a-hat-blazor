-- What a participant wants, in their own words, shared with the one person who drew them.
--
-- Append-only: a submission is never edited in place, and the newest row for a participant is the
-- one that counts. From the sender's side that still reads as "replace" -- what they send last is
-- what gets forwarded -- but the earlier rows stay. Two things need them. Gift ideas arrive by
-- email, and pulling the text out of a reply means guessing where the quoted message begins; a
-- guess that goes wrong is recoverable while what came before it is still here. And an abuse report
-- arrives after the fact, when the only useful answer is what was actually sent, not what has since
-- been written over it.
--
-- No sentinel row, unlike person, hat and participant. Those exist because a column elsewhere holds
-- the all-zero id and has to resolve to something; nothing holds a gift_idea_id, so asking whether
-- a participant has shared anything is asking whether a row exists, which is a question this table
-- can answer on its own.
--
-- Every column is stated at CREATE, because DSQL cannot ALTER COLUMN: a column added later can be
-- neither given a default nor tightened to NOT NULL afterwards. The cost of a column that nothing
-- writes yet is far lower here than the cost of needing one later.
CREATE TABLE gift_idea (
    gift_idea_id       UUID PRIMARY KEY,
    participant_id     UUID NOT NULL,
    -- Length matches the cap the application applies before the text reaches Comprehend. VARCHAR
    -- counts characters and the application counts UTF-8 bytes, so 8,000 here cannot reject
    -- anything an 8,000 byte cap has already allowed -- no character is smaller than a byte.
    ideas              VARCHAR(8000) NOT NULL,
    -- Which submission is newest. Ordering on this rather than on the id, so that what decides the
    -- winner is a value the application states outright.
    created_at         TIMESTAMPTZ NOT NULL,
    -- The SES message id this arrived in, or the empty string if it did not arrive by email. It is
    -- what ties a stored submission back to the raw message in S3, for the same reason
    -- hat.invitations_sent_from_ip exists: an abuse report is answerable only if the origin was
    -- written down at the time.
    inbound_message_id VARCHAR(255) NOT NULL
)
