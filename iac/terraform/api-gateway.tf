resource "aws_api_gateway_rest_api" "giftexchange-gateway" {
  name        = "giftexchange-gateway"
  description = "API Gateway for the Gift Exchange App"
  endpoint_configuration {
    types = ["REGIONAL"]
  }
}

resource "aws_api_gateway_deployment" "default" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  description = "Deployment for the Gift Exchange API Gateway"
  lifecycle {
    create_before_destroy = true
  }
  depends_on = [
    module.lambda-add-participant,
    module.lambda-assign-recipients,
    module.lambda-close-hat,
    module.lambda-create-hat,
    module.lambda-delete-hat,
    module.lambda-edit-hat,
    module.lambda-edit-participant,
    module.lambda-get-hat,
    module.lambda-get-hats,
    module.lambda-get-participant,
    module.lambda-remove-participant,
    module.lambda-send-invitations-hat,
    module.lambda-validate-hat
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
