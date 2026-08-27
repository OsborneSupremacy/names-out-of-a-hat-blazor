-- Dropped rather than renamed. DSQL has no foreign keys, so this could never constrain
-- hats.status; it was only ever a second copy of GiftExchange.Library.Models.HatStatuses, which is
-- where the valid set is actually enforced.
DROP TABLE hat_status
