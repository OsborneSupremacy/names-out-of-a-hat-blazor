-- The row meaning "not taking part". The third of the sentinels, after person--0002 and hat--0002.
--
-- It belongs to the sentinel hat and is the sentinel person, and it draws itself. That last part is
-- the point of it: picked_recipient_participant_id is the one column that holds the all-zero id in
-- normal operation -- every participant carries it until the hat is shaken -- so this is the row
-- that lets a query follow a pick with an inner join and get an empty name back rather than have to
-- account for a missing row.
--
-- Applied after the tables it references, though nothing enforces that: DSQL has no foreign keys.
INSERT INTO participant (
    participant_id,
    hat_id,
    person_id,
    picked_recipient_participant_id
) VALUES (
    '00000000-0000-0000-0000-000000000000',
    '00000000-0000-0000-0000-000000000000',
    '00000000-0000-0000-0000-000000000000',
    '00000000-0000-0000-0000-000000000000'
)
