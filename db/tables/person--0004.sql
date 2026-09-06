-- An introducer for every person who predates the column.
--
-- Reconstructed rather than guessed at. AddParticipantService is the only way somebody who is not
-- an organizer gets into this table, and it is always the organizer of the hat they were added to
-- who calls it -- so the organizer of the earliest exchange a person takes part in is who
-- introduced them. The earliest, because a person in three exchanges was introduced by exactly one
-- of the three, and the first is the only one it can have been.
--
-- Everybody the subquery finds nothing for arrived under their own steam and gets their own id:
-- an organizer who has created an exchange but never been added to one, and anybody who has signed
-- in without creating anything yet. Self-reference is the spelling for "nobody else introduced
-- them", and it reads correctly through the rule -- a person may always change their own name.
--
-- The sentinel is covered by the same COALESCE without being named. It takes part in the sentinel
-- exchange, whose organizer is the sentinel, so it lands on the all-zero id either way -- which is
-- its own id, and the empty value this schema expects it to hold. This is the backfill
-- NoRecordTests requires for a column added after that row was seeded.
--
-- One statement, as DSQL requires.
UPDATE person
SET added_by_person_id = COALESCE(
    (SELECT hat.organizer_person_id
     FROM participant
     JOIN hat ON hat.hat_id = participant.hat_id
     WHERE participant.person_id = person.person_id
     ORDER BY hat.created_at
     LIMIT 1),
    person.person_id)
