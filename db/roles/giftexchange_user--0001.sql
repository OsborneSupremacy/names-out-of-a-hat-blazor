-- The role the application connects as. Schema changes run as admin from CI; this role can
-- only read and write rows, and holds no DDL privileges.
CREATE ROLE giftexchange_user WITH LOGIN
