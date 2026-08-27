-- Recorded when invitations are sent, so an abuse report can be tied to an origin as well as to
-- the organizer's verified address. 45 characters is the longest an IPv6 address can be.
--
-- Deliberately not the inet type: DSQL's support for it is unverified, and this value is only
-- ever read by a human investigating a report.
ALTER TABLE hats ADD COLUMN invitations_sent_from_ip VARCHAR(45)
