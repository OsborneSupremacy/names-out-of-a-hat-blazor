-- A face for every participant that already existed.
--
-- They are holding the empty string participant--0003 filled them with, which is what "no value"
-- looks like in this schema and is not a face. This is the statement that gives them one.
--
-- Spread across the list by one byte of the participant id's digest rather than left to chance:
-- an UPDATE has no per-row randomness that is worth relying on, and a hash of the key gives every
-- existing row a stable face without caring what order they are read in. It does not avoid
-- collisions within a hat the way PersonEmoji.Assign does -- there is no way to say "one nobody
-- else here has" in a single statement -- and repeating a face is untidy rather than wrong.
--
-- The sentinel participant is excluded by name: it is the row meaning "not taking part", and every
-- column of it is empty, so it keeps the empty string it was given. This is the backfill
-- NoRecordTests requires for a column added after that row was seeded.
--
-- One statement, as DSQL requires, so both cases live in the same UPDATE.
UPDATE participant
SET emoji = CASE
    WHEN participant_id = '00000000-0000-0000-0000-000000000000' THEN ''
    ELSE (ARRAY[
        '😀', '😃', '😄', '😁', '😆', '😅', '🤣', '😂', '🙂', '🙃',
        '😉', '😊', '😌', '😇', '🥰', '😍', '🤩', '😋', '😛', '😜',
        '🤪', '😝', '🤗', '🤭', '🤫', '😏', '🤠', '🥳', '😎', '🤖',
        '👽', '👾', '👻', '😺', '😸', '😹', '😻', '😼', '🌝', '🌞',
        '🌛', '🌜'
    ])[(get_byte(decode(md5(participant_id::TEXT), 'hex'), 0) % 42) + 1]
END
