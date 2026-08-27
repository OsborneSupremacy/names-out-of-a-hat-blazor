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

  # Adding a method or an integration does not change this resource, so without something to force
  # its hand Terraform leaves the existing deployment in place and the new endpoint is never
  # published. It then answers "Missing Authentication Token", which is API Gateway's way of saying
  # the route does not exist and reads like an auth problem instead.
  #
  # The endpoints written out longhand are the ones at risk: those built through ./modules/api are
  # covered by the depends_on below, and these are not.
  triggers = {
    redeployment = sha1(jsonencode([
      aws_api_gateway_resource.auth-requestlink-resource.id,
      aws_api_gateway_resource.auth-redeem-resource.id,
      aws_api_gateway_resource.ask-token-resource.id,
      [for method in aws_api_gateway_method.auth-post : method.id],
      [for method in aws_api_gateway_method.ask : method.id],
      [for integration in aws_api_gateway_integration.auth-post : integration.id],
      [for integration in aws_api_gateway_integration.ask : integration.id],
    ]))
  }

  lifecycle {
    create_before_destroy = true
  }
  depends_on = [
    module.lambda-add-participant,
    module.lambda-assign-recipients,
    module.lambda-close-hat,
    module.lambda-copy-hat,
    module.lambda-create-hat,
    module.lambda-delete-hat,
    module.lambda-edit-hat,
    module.lambda-edit-participant,
    module.lambda-get-hat,
    module.lambda-get-hats,
    module.lambda-get-participant,
    module.lambda-preview-invitations-hat,
    module.lambda-remove-participant,
    module.lambda-send-invitations-hat,
    module.lambda-update-profile,
    module.lambda-validate-hat
  ]
}

resource "aws_api_gateway_stage" "live-stage" {
  stage_name    = "live"
  rest_api_id   = aws_api_gateway_rest_api.giftexchange-gateway.id
  deployment_id = aws_api_gateway_deployment.default.id

  access_log_settings {
    destination_arn = aws_cloudwatch_log_group.api_gateway_access_logs.arn
    format = jsonencode({
      requestId      = "$context.requestId"
      ip             = "$context.identity.sourceIp"
      caller         = "$context.identity.caller"
      user           = "$context.identity.user"
      requestTime    = "$context.requestTime"
      httpMethod     = "$context.httpMethod"
      resourcePath   = "$context.resourcePath"
      status         = "$context.status"
      protocol       = "$context.protocol"
      responseLength = "$context.responseLength"
    })
  }
}

resource "aws_api_gateway_model" "error_response_model" {
  rest_api_id  = aws_api_gateway_rest_api.giftexchange-gateway.id
  name         = "BadRequestOrConflictResponse"
  description  = "A response model for 404 Not Found or 409 Conflict errors."
  content_type = "application/json"
  schema       = file("../../src/GiftExchange.Library/Schemas/ErrorResponse.schema.json")
}
