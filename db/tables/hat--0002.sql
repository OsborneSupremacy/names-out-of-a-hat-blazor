-- The row meaning "no gift exchange". The counterpart to the sentinel in person--0002.sql.
--
-- Its organizer is the sentinel person, so the sentinel is self-consistent: following it leads to
-- the other sentinel rather than off the end of the table. That is also why person--0002 is applied
-- first.
--
-- Every column is stated, because the table has no defaults. The two timestamps are the minimum,
-- which is what the application already reads as "no date".
INSERT INTO hat (
    hat_id,
    organizer_person_id,
    name,
    name_normalized,
    status,
    additional_information,
    price_range,
    invitations_queued_at,
    invitations_sent_from_ip,
    created_at
) VALUES (
    '00000000-0000-0000-0000-000000000000',
    '00000000-0000-0000-0000-000000000000',
    '',
    '',
    '',
    '',
    '',
    '0001-01-01 00:00:00+00',
    '',
    '0001-01-01 00:00:00+00'
)
