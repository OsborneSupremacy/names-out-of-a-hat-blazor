-- Authorizes the delivery event Lambda to connect as giftexchange_user.
--
-- The counterpart of --0008, for the second function that reaches DSQL without going through the
-- API. SNS delivers SES events to a queue and this function drains it, upserting one row per
-- message into participant_email_delivery. Its IAM role carries dsql:DbConnect, which only permits
-- opening a connection; this decides which database role it may open one as.
--
-- Without it the function connects and gets no further, and the failure is close to invisible: the
-- sends keep working, the events keep arriving, and the organizer's view simply never fills in.
AWS IAM GRANT giftexchange_user TO '${delivery_events_role_arn}'
