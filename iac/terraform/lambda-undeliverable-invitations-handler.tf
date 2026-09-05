# ---------------------------------------------------------------------------------------------
# The delayed check on whether the invitations an organizer sent actually arrived.
#
# One schedule per send, created when the invitations are queued and firing a couple of hours
# later. Its own schedule group rather than sharing the cool-off group, so that the two can be
# listed, counted and deleted apart -- they fire on different clocks for unrelated reasons, and a
# group holding both would make "how many exchanges are waiting to cool off" unanswerable.
# ---------------------------------------------------------------------------------------------

resource "aws_scheduler_schedule_group" "undeliverable-invitations" {
  name = local.undeliverable_scheduler_group_name
}

resource "aws_lambda_function" "undeliverable-invitations-handler" {
  function_name    = "giftexchange-undeliverable-invitations-handler"
  description      = "Function that tells an organizer which of their invitations came back undelivered"
  handler          = "GiftExchange.Library::GiftExchange.Library.Handlers.UndeliverableInvitationsHandler::FunctionHandler"
  runtime          = "dotnet10"
  architectures    = ["arm64"]
  memory_size      = 1024
  timeout          = 30
  filename         = local.publish_zip_path
  source_code_hash = filebase64sha256(local.publish_zip_path)
  role             = aws_iam_role.undeliverable-invitations-handler-role.arn

  # Traces the gateway hop and, separately, the Init phase this function's memory setting is
  # sized for. See xray.tf for what that does and does not show without SDK instrumentation.
  tracing_config {
    mode = "Active"
  }

  environment {
    variables = local.common_environment_variables
  }
}

resource "aws_iam_role" "undeliverable-invitations-handler-role" {
  name = "giftexchange-undeliverable-invitations-handler-lambda-role"

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

resource "aws_iam_role_policy" "undeliverable-invitations-handler-policy" {
  name = "giftexchange-undeliverable-invitations-handler-policy"
  role = aws_iam_role.undeliverable-invitations-handler-role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      # It reads one hat and its participants' delivery rows. It connects as giftexchange_user, so
      # it also needs the database-side mapping in db/roles.
      {
        Effect = "Allow"
        Action = [
          "dsql:DbConnect"
        ]
        Resource = [
          aws_dsql_cluster.giftexchange_dsql_cluster.arn
        ]
      },
      # SendRawEmail alone, because AutomaticEmailSender builds MIME by hand to set the
      # Auto-Submitted header. The other send actions are not granted: nothing in this function has
      # a use for them, and this is the one function in the application whose only outbound message
      # goes to a single organizer.
      {
        Effect = "Allow"
        Action = [
          "ses:SendRawEmail"
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

resource "aws_iam_role" "undeliverable-invitations-scheduler-execution-role" {
  name = "giftexchange-undeliverable-invitations-scheduler-execution-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Service = "scheduler.amazonaws.com"
        }
        Action = "sts:AssumeRole"
      }
    ]
  })
}

resource "aws_iam_role_policy" "undeliverable-invitations-scheduler-execution-policy" {
  name = "giftexchange-undeliverable-invitations-scheduler-execution-policy"
  role = aws_iam_role.undeliverable-invitations-scheduler-execution-role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "lambda:InvokeFunction"
        ]
        Resource = [
          aws_lambda_function.undeliverable-invitations-handler.arn
        ]
      }
    ]
  })
}

resource "aws_lambda_permission" "undeliverable-invitations-handler-allow-scheduler-invoke" {
  statement_id  = "AllowExecutionFromEventBridgeScheduler"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.undeliverable-invitations-handler.arn
  principal     = "scheduler.amazonaws.com"
  source_arn    = "arn:aws:scheduler:${data.aws_region.current.region}:${data.aws_caller_identity.current.account_id}:schedule/${aws_scheduler_schedule_group.undeliverable-invitations.name}/*"
}
