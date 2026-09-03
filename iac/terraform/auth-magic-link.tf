# ---------------------------------------------------------------------------------------------
# Magic link authentication.
#
# Two moving parts:
#   * the session signing key, held in SSM Parameter Store
#   * the Lambda authorizer that validates session JWTs on every protected endpoint
#
# The unauthenticated /auth endpoints themselves are in api-gateway-resource-auth.tf.
# ---------------------------------------------------------------------------------------------

# The value is deliberately NOT managed by Terraform, so the signing key never lands in state.
# Seed it once, out of band:
#
#   aws ssm put-parameter --name /giftexchange/live/session-signing-key --type SecureString \
#     --overwrite --value "$(openssl rand -base64 48)" --profile benosborne
#
resource "aws_ssm_parameter" "session_signing_key" {
  name        = "/giftexchange/live/session-signing-key"
  description = "HMAC key used to sign Names Out Of A Hat session tokens"
  type        = "SecureString"
  value       = "placeholder-replace-out-of-band"

  lifecycle {
    ignore_changes = [value]
  }
}

resource "aws_lambda_function" "authorizer" {
  function_name    = "giftexchange-authorizer"
  description      = "Validates session JWTs and supplies the caller's email to the application"
  handler          = "GiftExchange.Library::GiftExchange.Library.Handlers.AuthorizerHandler::FunctionHandler"
  runtime          = "dotnet10"
  architectures    = ["arm64"]
  memory_size      = 512
  timeout          = 10
  filename         = local.publish_zip_path
  source_code_hash = filebase64sha256(local.publish_zip_path)
  role             = aws_iam_role.authorizer_exec_role.arn

  # Traces the gateway hop and, separately, the Init phase this function's memory setting is
  # sized for. See xray.tf for what that does and does not show without SDK instrumentation.
  tracing_config {
    mode = "Active"
  }

  environment {
    variables = local.common_environment_variables
  }
}

resource "aws_iam_role" "authorizer_exec_role" {
  name = "giftexchange-authorizer-exec-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action    = "sts:AssumeRole"
        Effect    = "Allow"
        Principal = { Service = "lambda.amazonaws.com" }
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "authorizer_basic_execution" {
  role       = aws_iam_role.authorizer_exec_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

resource "aws_iam_role_policy" "authorizer_ssm_policy" {
  name = "giftexchange-authorizer-ssm-policy"
  role = aws_iam_role.authorizer_exec_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect   = "Allow"
        Action   = ["ssm:GetParameter"]
        Resource = [aws_ssm_parameter.session_signing_key.arn]
      },
      {
        Effect   = "Allow"
        Action   = ["kms:Decrypt"]
        Resource = ["arn:aws:kms:${data.aws_region.current.region}:${data.aws_caller_identity.current.account_id}:alias/aws/ssm"]
      }
    ]
  })
}

resource "aws_lambda_permission" "authorizer_allow_apigw_invoke" {
  statement_id  = "AllowExecutionFromAPIGatewayAuthorizer"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.authorizer.arn
  principal     = "apigateway.amazonaws.com"
  source_arn    = "arn:aws:execute-api:${data.aws_region.current.region}:${data.aws_caller_identity.current.account_id}:${aws_api_gateway_rest_api.giftexchange-gateway.id}/authorizers/*"
}

resource "aws_api_gateway_authorizer" "session" {
  name                             = "session-token-authorizer"
  rest_api_id                      = aws_api_gateway_rest_api.giftexchange-gateway.id
  type                             = "TOKEN"
  identity_source                  = "method.request.header.Authorization"
  authorizer_uri                   = aws_lambda_function.authorizer.invoke_arn
  authorizer_result_ttl_in_seconds = 300
}

# The application Lambda needs to read the signing key (to mint tokens) and to send the link email.
# It has no SES permission today, because invitations are sent by the queue handler rather than
# by the API Lambda.
resource "aws_iam_role_policy" "giftexchange_app_auth_policy" {
  name = "giftexchange-app-auth-policy"
  role = aws_iam_role.giftexchange_app_exec_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect   = "Allow"
        Action   = ["ssm:GetParameter"]
        Resource = [aws_ssm_parameter.session_signing_key.arn]
      },
      {
        Effect   = "Allow"
        Action   = ["kms:Decrypt"]
        Resource = ["arn:aws:kms:${data.aws_region.current.region}:${data.aws_caller_identity.current.account_id}:alias/aws/ssm"]
      },
      {
        Effect   = "Allow"
        Action   = ["ses:SendEmail", "ses:SendRawEmail"]
        Resource = "*"
      }
    ]
  })
}
