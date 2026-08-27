# SNS topic for email notifications
resource "aws_sns_topic" "inbox" {
  name         = "sns-topic-namesoutofahat-inbox"
  display_name = "Names Out of a Hat Inbox Notifications"
}

# SNS topic policy to allow SES to publish
resource "aws_sns_topic_policy" "inbox" {
  arn = aws_sns_topic.inbox.arn

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Service = "ses.amazonaws.com"
        }
        Action   = "SNS:Publish"
        Resource = aws_sns_topic.inbox.arn
        Condition = {
          StringEquals = {
            "aws:SourceAccount" = data.aws_caller_identity.current.account_id
          }
        }
      }
    ]
  })
}
