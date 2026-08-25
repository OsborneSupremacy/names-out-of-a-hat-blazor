-- Read only. hat_status is reference data owned by migrations, so the application has no
-- reason to write to it.
GRANT SELECT ON hat_status TO giftexchange_user
