-- CRUD on the rebuilt tables. The grants in --0002 and --0003 went with the tables they named
-- when those were dropped, so this is a fresh grant rather than an addition to them.
GRANT SELECT, INSERT, UPDATE, DELETE
    ON person,
       hat,
       participant,
       participant_eligible_recipient
    TO giftexchange_user
