-- CRUD on the table recording what SES said happened to each participant email.
--
-- A separate grant rather than an edit to --0009, for the reason --0007 gives about --0006: that
-- changeset has run, and Liquibase checksums cover what it said at the time.
GRANT SELECT, INSERT, UPDATE, DELETE
    ON participant_email_delivery
    TO giftexchange_user
