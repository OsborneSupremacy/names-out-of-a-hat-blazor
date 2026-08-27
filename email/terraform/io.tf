output "ses_domain_identity_arn" {
  description = "ARN of the SES domain identity"
  value       = aws_ses_domain_identity.mail.arn
}

output "email_storage_bucket" {
  description = "S3 bucket for storing received emails"
  value       = aws_s3_bucket.email_storage.bucket
}

output "sns_topic_arn" {
  description = "ARN of the SNS topic for email notifications"
  value       = aws_sns_topic.inbox.arn
}

output "sns_topic_name" {
  description = "Name of the SNS topic for email notifications"
  value       = aws_sns_topic.inbox.name
}

output "gift_ideas_domain" {
  description = "Domain that receives participants' gift ideas emails"
  value       = aws_ses_domain_identity.gift_ideas.domain
}

output "gift_ideas_domain_identity_arn" {
  description = "ARN of the SES domain identity for gift ideas mail"
  value       = aws_ses_domain_identity.gift_ideas.arn
}

# The receipt rule set every rule in this account is attached to. Provisioned in the ahzborn-aws
# repository; exported here so the API repository can add its rules to the same set rather than
# repeating the name and drifting from it.
output "ses_receipt_rule_set_name" {
  description = "SES receipt rule set the inbound rules belong to"
  value       = aws_ses_receipt_rule.store_emails.rule_set_name
}
