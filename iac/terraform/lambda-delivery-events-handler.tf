resource "aws_lambda_function" "delivery-events-handler" {
  function_name = "giftexchange-delivery-events-handler"
  description   = "Function that records what SES reports about each participant email"
  handler       = "GiftExchange.Library::GiftExchange.Library.Handlers.DeliveryEventsHandler::FunctionHandler"
  runtime       = "dotnet10"
  architectures = ["arm64"]

  # Matches the inbound mail function rather than the queue handler that sends: this one opens a
  # DSQL connection and signs an IAM token on a cold start, and Lambda scales CPU with memory.
  memory_size = 1024

  # Nothing is waiting. The work is one read and one write of a single row.
  timeout = 60

  filename         = local.publish_zip_path
  source_code_hash = filebase64sha256(local.publish_zip_path)
  role             = aws_iam_role.delivery-events-handler-role.arn

  environment {
    variables = local.common_environment_variables
  }
}

resource "aws_iam_role" "delivery-events-handler-role" {
  name = "giftexchange-delivery-events-handler-lambda-role"

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

resource "aws_iam_role_policy" "delivery-events-handler-policy" {
  name = "giftexchange-delivery-events-handler-policy"
  role = aws_iam_role.delivery-events-handler-role.id

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
        Resource = aws_sqs_queue.delivery-events-queue.arn
      },
      {
        Effect   = "Allow"
        Action   = ["logs:CreateLogGroup", "logs:CreateLogStream", "logs:PutLogEvents"]
        Resource = "arn:aws:logs:*:*:*"
      }
    ]
  })
}

# No SES permission of any kind, deliberately. This function reads what SES said and writes it down;
# it never sends, replies or suppresses anything, and the events it acts on are about messages that
# have already gone.

# As with the inbound mail function: dsql:DbConnect permits opening a connection, and which database
# role it may connect as is decided inside the database by the AWS IAM GRANT in
# db/roles/giftexchange_user--0011.sql. Without that changeset applied this function reaches the
# cluster and gets no further, and the failure is quiet -- mail keeps sending, events keep arriving,
# and the organizer's view simply never fills in.
resource "aws_iam_role_policy" "delivery-events-handler-dsql-policy" {
  name = "giftexchange-delivery-events-handler-dsql-policy"
  role = aws_iam_role.delivery-events-handler-role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect   = "Allow"
        Action   = ["dsql:DbConnect"]
        Resource = [aws_dsql_cluster.giftexchange_dsql_cluster.arn]
      }
    ]
  })
}

# One record at a time, as the invitations queue handler takes them. A batch would save nothing at
# this volume -- an exchange of thirty people produces a few dozen events -- and it would mean one
# unparseable event dragging its whole batch through the retries with it.
resource "aws_lambda_event_source_mapping" "delivery-events-handler-sqs-trigger" {
  event_source_arn = aws_sqs_queue.delivery-events-queue.arn
  function_name    = aws_lambda_function.delivery-events-handler.arn
  batch_size       = 1
  enabled          = true
}
