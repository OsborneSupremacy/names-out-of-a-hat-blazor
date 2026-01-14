resource "aws_lambda_function" "invitation-queue-handler" {
  function_name    = "giftexchange-invitation-queue-handler"
  description      = "Function that consumes messages from the SQS email invitations queue and sends emails"
  handler          = "GiftExchange.Library::GiftExchange.Library.Handlers.InvitationQueueHandler::FunctionHandler"
  runtime          = "dotnet10"
  architectures    = ["arm64"]
  memory_size      = 128
  timeout          = 300
  filename         = data.archive_file.lambda_function.output_path
  source_code_hash = data.archive_file.lambda_function.output_base64sha256
  role             = aws_iam_role.invitation-queue-handler-role.arn

  environment {
    variables = merge(
      local.common_environment_variables,
      {
        LIVE_MODE = false
      }
    )
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
}
