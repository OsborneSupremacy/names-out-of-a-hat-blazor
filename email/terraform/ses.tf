# SES Domain Identity for mail subdomain
resource "aws_ses_domain_identity" "mail" {
  domain = "mail.namesoutofahat.com"
}

# Wait for domain verification
resource "aws_ses_domain_identity_verification" "mail" {
  domain = aws_ses_domain_identity.mail.id

  depends_on = [aws_route53_record.ses_verification]
}

# DKIM records for email authentication
resource "aws_ses_domain_dkim" "mail" {
  domain = aws_ses_domain_identity.mail.domain
}

# Configure a custom MAIL FROM domain so SPF can align for DMARC.
resource "aws_ses_domain_mail_from" "mail" {
  domain                 = aws_ses_domain_identity.mail.domain
  mail_from_domain       = "bounce.mail.namesoutofahat.com"
  behavior_on_mx_failure = "UseDefaultValue"
}

# Receipt rule to store emails in S3 and publish to SNS
resource "aws_ses_receipt_rule" "store_emails" {
  name          = "ses-rule-namesoutofahat-inbox-to-s3"
  rule_set_name = "ses-inbox-ruleset-main"
  recipients    = ["mail.namesoutofahat.com"]
  enabled       = true
  scan_enabled  = true

  s3_action {
    bucket_name       = aws_s3_bucket.email_storage.bucket
    object_key_prefix = "emails/"
    position          = 1
  }

  sns_action {
    topic_arn = aws_sns_topic.inbox.arn
    encoding  = "Base64"
    position  = 2
  }
}
