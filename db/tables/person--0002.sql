-- The row meaning "nobody".
--
-- The application spells absence as a value rather than a null, and an unset person id is the
-- all-zero UUID. This gives that id something to point at: a column holding it resolves to a real
-- row with an empty name and address, so reading through it is an inner join returning "" rather
-- than an outer join returning nothing.
--
-- Its email is the empty string, which no person can hold -- every address that reaches the
-- database has been through validation -- so it cannot collide with a real one under uq_person_email.
INSERT INTO person (person_id, name, email) VALUES
    ('00000000-0000-0000-0000-000000000000', '', '')
