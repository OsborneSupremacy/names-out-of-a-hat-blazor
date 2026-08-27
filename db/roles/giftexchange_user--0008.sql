-- Authorizes the inbound mail Lambda to connect as giftexchange_user.
--
-- SES invokes that function for mail arriving at a gift ideas address. It resolves the routing
-- token to a participant and appends what they wrote, both of which are DSQL reads and writes.
-- Its IAM role carries dsql:DbConnect, but that only permits opening a connection; this is what
-- decides which database role it may open one as. Without it the function reaches the cluster and
-- gets no further, and every submission fails after the sender has already been told nothing.
AWS IAM GRANT giftexchange_user TO '${inbound_mail_role_arn}'
