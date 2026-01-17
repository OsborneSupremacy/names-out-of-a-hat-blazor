resource "aws_api_gateway_rest_api" "giftexchange-gateway" {
  name        = "giftexchange-gateway"
  description = "API Gateway for the Gift Exchange App"
  endpoint_configuration {
    types = ["REGIONAL"]
  }
  policy = jsonencode({
    "Version" : "2012-10-17",
    "Statement" : [
      {
        "Effect" : "Allow",
        "Principal" : {
          "AWS" : "*"
        }
        "Action" : "execute-api:Invoke",
        "Resource" : "arn:aws:execute-api:us-east-1:${data.aws_caller_identity.current.account_id}:*/*/*"
        "Condition" : {
          "IpAddress" : {
            "aws:SourceIp" : data.http.ipify.response_body
          }
        }
      }
    ]
  })
}

resource "aws_api_gateway_deployment" "default" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id

  triggers = {
    redeployment = filebase64sha256(local.publish_zip_path)
  }

  lifecycle {
    create_before_destroy = true
  }

  depends_on = [
    module.lambda-add-participant,
    module.lambda-create-hat
  ]
}

resource "aws_api_gateway_stage" "live-stage" {
  stage_name    = "live"
  rest_api_id   = aws_api_gateway_rest_api.giftexchange-gateway.id
  deployment_id = aws_api_gateway_deployment.default.id
}

resource "aws_api_gateway_model" "error_response_model" {
  rest_api_id  = aws_api_gateway_rest_api.giftexchange-gateway.id
  name         = "BadRequestOrConflictResponse"
  description  = "A response model for 404 Not Found or 409 Conflict errors."
  content_type = "application/json"
  schema       = file("../../src/GiftExchange.Library/Schemas/ErrorResponse.schema.json")
}

# Cognito Authorizer for API Gateway
resource "aws_api_gateway_authorizer" "cognito" {
  name            = "cognito-authorizer"
  rest_api_id     = aws_api_gateway_rest_api.giftexchange-gateway.id
  type            = "COGNITO_USER_POOLS"
  provider_arns   = [aws_cognito_user_pool.namesoutofahat.arn]
  identity_source = "method.request.header.Authorization"
}
