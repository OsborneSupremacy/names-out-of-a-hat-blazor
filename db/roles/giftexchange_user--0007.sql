-- CRUD on the gift ideas tables.
--
-- A separate grant rather than an edit to --0006: that changeset has run, and its checksum covers
-- what it said at the time. Adding tables to it would make Liquibase refuse the whole changelog.
--
-- Both the application function and the inbound mail function connect as this role, so one grant
-- covers writing a token when invitations go out and appending a submission when one arrives.
GRANT SELECT, INSERT, UPDATE, DELETE
    ON gift_idea,
       gift_idea_token
    TO giftexchange_user
