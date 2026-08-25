-- Authorizes the application's IAM role to connect as giftexchange_user.
--
-- This is the half that matters. The IAM policy (dsql:DbConnect, in iac/terraform/dsql.tf)
-- only permits opening a connection; it is this grant that decides which database role that
-- connection may assume. Without it the Lambda authenticates and gets nowhere.
--
-- The ARN arrives as a Liquibase property from the deploy workflow rather than being
-- hardcoded per account. Confirm the mapping afterwards with:
--   SELECT * FROM sys.iam_pg_role_mappings
AWS IAM GRANT giftexchange_user TO '${app_role_arn}'
