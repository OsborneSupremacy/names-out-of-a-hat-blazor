resource "aws_lambda_function" "inbound-gift-ideas-handler" {
  function_name = "giftexchange-inbound-gift-ideas-handler"
  description   = "Function invoked by SES for mail arriving at a gift ideas address"
  handler       = "GiftExchange.Library::GiftExchange.Library.Handlers.InboundGiftIdeasHandler::FunctionHandler"
  runtime       = "dotnet10"
  architectures = ["arm64"]

  # Matches the API function rather than the queue handler. This one opens a DSQL connection and
  # signs an IAM token on a cold start, and Lambda scales CPU with memory, so the lower setting
  # buys nothing here.
  memory_size = 1024

  # No caller is waiting: SES invokes this asynchronously. The work is one S3 read, a lookup, a
  # Comprehend call and two sends, so this is a ceiling rather than a target.
  timeout = 60

  filename         = local.publish_zip_path
  source_code_hash = filebase64sha256(local.publish_zip_path)
  role             = aws_iam_role.inbound-gift-ideas-handler-role.arn

  environment {
    variables = merge(
      local.common_environment_variables,
      {
        INBOUND_MAIL_BUCKET = local.inbound_mail_bucket
        INBOUND_MAIL_PREFIX = local.gift_ideas_object_prefix
        LIVE_MODE           = true
      }
    )
  }
}

resource "aws_iam_role" "inbound-gift-ideas-handler-role" {
  name = "giftexchange-inbound-gift-ideas-handler-lambda-role"

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

resource "aws_iam_role_policy" "inbound-gift-ideas-handler-policy" {
  name = "giftexchange-inbound-gift-ideas-handler-policy"
  role = aws_iam_role.inbound-gift-ideas-handler-role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        # Read only, and only the gift ideas prefix of the shared inbound mail bucket. That
        # bucket belongs to the SES repository and also holds everything arriving at
        # mail.namesoutofahat.com; there is no reason for this function to read any of that, so the
        # grant stops at its own prefix.
        #
        # No delete, deliberately. The bucket's lifecycle rule owns expiry, so a bug here cannot
        # destroy the raw message that gift_idea.inbound_message_id points an abuse report at.
        Effect   = "Allow"
        Action   = ["s3:GetObject"]
        Resource = "arn:aws:s3:::${local.inbound_mail_bucket}/${local.gift_ideas_object_prefix}*"
      },
      {
        # SendRawEmail rather than SendEmail. Every message this function sends carries
        # Auto-Submitted: auto-replied, and SendEmail has no way to set a header.
        Effect   = "Allow"
        Action   = ["ses:SendRawEmail"]
        Resource = "*"
      },
      {
        Effect   = "Allow"
        Action   = ["comprehend:DetectToxicContent"]
        Resource = "*"
      },
      {
        # ReplyThrottleProvider, which caps how often the do-not-reply address answers any one
        # sender. A conditional put and nothing else — it never reads the item back.
        Effect   = "Allow"
        Action   = ["dynamodb:PutItem"]
        Resource = [aws_dynamodb_table.giftexchange.arn]
      },
      {
        Effect   = "Allow"
        Action   = ["logs:CreateLogGroup", "logs:CreateLogStream", "logs:PutLogEvents"]
        Resource = "arn:aws:logs:*:*:*"
      }
    ]
  })
}

# Connecting is only half of it. dsql:DbConnect permits opening a connection; which database role
# this IAM role may connect as is decided inside the database, by the AWS IAM GRANT in
# db/roles/giftexchange_user--0008.sql. Without that changeset applied, this function can reach the
# cluster and get no further.
resource "aws_iam_role_policy" "inbound-gift-ideas-handler-dsql-policy" {
  name = "giftexchange-inbound-gift-ideas-handler-dsql-policy"
  role = aws_iam_role.inbound-gift-ideas-handler-role.id

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
