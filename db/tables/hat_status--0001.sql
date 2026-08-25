-- The canonical set of gift exchange statuses.
--
-- DSQL has no foreign keys, so this cannot constrain hats.status; it is a join target and a
-- single place to read the valid set from, not an enforced constraint.
CREATE TABLE hat_status (
    status VARCHAR(30) PRIMARY KEY
)
