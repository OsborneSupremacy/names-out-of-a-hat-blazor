resource "aws_lambda_function" "invitation-queue-handler" {
  function_name    = "giftexchange-invitation-queue-handler"
  description      = "Function that consumes messages from the SQS email invitations queue and sends emails"
  handler          = "GiftExchange.Library::GiftExchange.Library.Handlers.InvitationQueueHandler::FunctionHandler"
  runtime          = "dotnet10"
  architectures    = ["arm64"]
  memory_size      = 512
  timeout          = 300
  filename         = local.publish_zip_path
  source_code_hash = filebase64sha256(local.publish_zip_path)
  role             = aws_iam_role.invitation-queue-handler-role.arn

  environment {
    variables = local.common_environment_variables
  }
}

resource "aws_iam_role" "invitation-queue-handler-role" {
  name = "giftexchange-invitation-queue-handler-lambda-role"

  # Allow Lambda service to assume this role
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Service = "lambda.amazonaws.com"
        }
        Action = "sts:AssumeRole"
      }
    ]
  })
}

# Inline policy for SQS, SES, and DynamoDB access
resource "aws_iam_role_policy" "invitation-queue-handler-policy" {
  name = "giftexchange-invitation-queue-handler-policy"
  role = aws_iam_role.invitation-queue-handler-role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "sqs:ReceiveMessage",
          "sqs:DeleteMessage",
          "sqs:ChangeMessageVisibility",
          "sqs:GetQueueAttributes",
          "sqs:GetQueueUrl"
        ]
        Resource = aws_sqs_queue.invitations-queue.arn
      },
      {
        Effect = "Allow"
        Action = [
          "ses:SendEmail",
          "ses:SendRawEmail",
          "ses:SendTemplatedEmail",
          "ses:SendBulkTemplatedEmail"
        ]
        Resource = "*"
      },
      {
        Effect = "Allow"
        Action = [
          "logs:CreateLogGroup",
          "logs:CreateLogStream",
          "logs:PutLogEvents"
        ]
        Resource = "arn:aws:logs:*:*:*"
      }
    ]
  })
}

resource "aws_lambda_event_source_mapping" "invitation-queue-handler-sqs-trigger" {
  event_source_arn                   = aws_sqs_queue.invitations-queue.arn
  function_name                      = aws_lambda_function.invitation-queue-handler.arn
  batch_size                         = 1
  maximum_batching_window_in_seconds = 30
  enabled                            = true

  # The same ceiling, for the same reason, as the delivery events mapping -- see the comment there
  # for the incident that put it on both. This is the other fan-out: sending invitations queues one
  # message per participant, and an organizer with a large exchange is exactly the person whose
  # next page load must not be the one that gets refused.
  #
  # Slower, and that is fine. Thirty invitations two at a time is a few seconds, and nobody is
  # waiting on the queue -- EnqueueInvitationsService has already answered the organizer by the
  # time any of this runs.
  scaling_config {
    maximum_concurrency = 2
  }
}
