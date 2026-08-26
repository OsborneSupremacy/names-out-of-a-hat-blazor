-- Authorizes the cooled-off scheduler Lambda to connect as giftexchange_user.
--
-- It transitions a hat from INVITATIONS_SENT to READY_TO_CLOSE, which moved from DynamoDB to
-- DSQL along with the rest of the domain data. Without this grant the transition fails and hats
-- stay stuck at INVITATIONS_SENT, which means they can never be closed and the picks are never
-- revealed.
AWS IAM GRANT giftexchange_user TO '${scheduler_role_arn}'
