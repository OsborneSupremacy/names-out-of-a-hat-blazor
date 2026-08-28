# The receipt rules that act on inbound gift ideas mail.
#
# Only the rules live here. The ideas.namesoutofahat.com identity and its DNS are in email/terraform,
# alongside the rest of this domain's email configuration, and reach this state through the
# terraform_remote_state in data.tf -- a separate root with its own state, so the split survives
# both of them being in this repository. What keeps these two rules here is that both invoke a
# Lambda defined in iac/.
#
# Neither rule creates or activates a rule set. ses-inbox-ruleset-main is provisioned in the
# ahzborn-aws repository, one region permits a single active set, and something is already handling
# mail through it — declaring an aws_ses_active_receipt_rule_set here would quietly replace it.

locals {
  # SES writes each message under this prefix in the shared inbound mail bucket. Separate from the
  # emails/ prefix the mail.namesoutofahat.com rule uses, so the two paths cannot read each other's
  # messages by guessing a key.
  gift_ideas_object_prefix = "gift-ideas/"

  inbound_mail_bucket = data.terraform_remote_state.email.outputs.email_storage_bucket
}

# SES checks it can invoke the function at the moment a rule is created, so the permission has to
# exist first. source_account stops another account's SES invoking ours.
resource "aws_lambda_permission" "ses_invoke_inbound_gift_ideas" {
  statement_id   = "AllowExecutionFromSES"
  action         = "lambda:InvokeFunction"
  function_name  = aws_lambda_function.inbound-gift-ideas-handler.function_name
  principal      = "ses.amazonaws.com"
  source_account = data.aws_caller_identity.current.account_id
}

# Everything addressed to the gift ideas domain. The local part is the routing token, which is what
# lets every participant have their own address without a rule each.
#
# S3 first, then Lambda: actions run in order, and the function reads the object the action before
# it wrote. It has to, because the Lambda action carries headers and verdicts and no body at all.
# Event invocation, since nothing is waiting on the result and a synchronous rule would hold the
# SMTP conversation open for the length of the work.
resource "aws_ses_receipt_rule" "gift_ideas" {
  name          = "giftexchange-gift-ideas"
  rule_set_name = data.terraform_remote_state.email.outputs.ses_receipt_rule_set_name
  enabled       = true
  scan_enabled  = true
  recipients    = [data.terraform_remote_state.email.outputs.gift_ideas_domain]

  s3_action {
    position          = 1
    bucket_name       = local.inbound_mail_bucket
    object_key_prefix = local.gift_ideas_object_prefix
  }

  lambda_action {
    position        = 2
    function_arn    = aws_lambda_function.inbound-gift-ideas-handler.arn
    invocation_type = "Event"
  }

  depends_on = [aws_lambda_permission.ses_invoke_inbound_gift_ideas]
}

# Mail sent to the address every outbound message comes from.
#
# This does not replace the existing rule. ses-rule-namesoutofahat-inbox-to-s3, in the SES
# repository, already matches the whole mail.namesoutofahat.com domain and archives everything
# arriving there — the DMARC aggregate reports at dmarc@ among them. SES runs every rule that
# matches, in order, so this one runs after it and adds a reply for the one address a person might
# plausibly write to by hand. The archive still happens; nothing is taken away from it.
#
# No S3 action, because there is nothing to read. The reply says only that nobody is listening and
# points at the button, so the handler needs the sender's address and no part of the body.
resource "aws_ses_receipt_rule" "do_not_reply" {
  name          = "giftexchange-do-not-reply"
  rule_set_name = data.terraform_remote_state.email.outputs.ses_receipt_rule_set_name
  enabled       = true
  scan_enabled  = true
  recipients    = ["donotreply@mail.namesoutofahat.com"]
  after         = aws_ses_receipt_rule.gift_ideas.name

  lambda_action {
    position        = 1
    function_arn    = aws_lambda_function.inbound-gift-ideas-handler.arn
    invocation_type = "Event"
  }

  depends_on = [aws_lambda_permission.ses_invoke_inbound_gift_ideas]
}
