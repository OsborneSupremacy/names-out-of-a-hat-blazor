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

# The configuration set every outbound participant email names on the send. Without it on the
# request SES publishes nothing, so this output is the whole subscription.
output "ses_configuration_set_name" {
  description = "SES configuration set that publishes delivery events for outbound mail"
  value       = aws_sesv2_configuration_set.outbound.configuration_set_name
}

# Subscribed to by a queue in the application state, which is where the function that reads these
# events lives.
output "delivery_events_topic_arn" {
  description = "ARN of the SNS topic SES publishes delivery events to"
  value       = aws_sns_topic.delivery_events.arn
}
