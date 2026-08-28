-- CRUD on the tables behind asking somebody other than your own pick for gift ideas.
--
-- A separate grant rather than an edit to --0007, for the reason --0007 gives about --0006: that
-- changeset has run, and Liquibase checksums cover what it said at the time.
GRANT SELECT, INSERT, UPDATE, DELETE
    ON gift_idea_ask,
       contributed_gift_idea
    TO giftexchange_user
