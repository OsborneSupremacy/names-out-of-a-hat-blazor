# ---------------------------------------------------------------------------------------------
# Unauthenticated sign-in endpoints.
#
# These are written out longhand rather than through ./modules/api, because that module assumes
# every endpoint is authenticated and carries a response model. Fold them in later if the shape
# settles down.
# ---------------------------------------------------------------------------------------------

resource "aws_api_gateway_resource" "auth-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_rest_api.giftexchange-gateway.root_resource_id
  path_part   = "auth"
}

resource "aws_api_gateway_resource" "auth-requestlink-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_resource.auth-resource.id
  path_part   = "requestlink"
}

resource "aws_api_gateway_resource" "auth-redeem-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_resource.auth-resource.id
  path_part   = "redeem"
}

locals {
  auth_endpoints = {
    requestlink = aws_api_gateway_resource.auth-requestlink-resource.id
    redeem      = aws_api_gateway_resource.auth-redeem-resource.id
  }
}

resource "aws_api_gateway_method" "auth-post" {
  for_each = local.auth_endpoints

  rest_api_id   = aws_api_gateway_rest_api.giftexchange-gateway.id
  resource_id   = each.value
  http_method   = "POST"
  authorization = "NONE"
}

resource "aws_api_gateway_integration" "auth-post" {
  for_each = local.auth_endpoints

  rest_api_id             = aws_api_gateway_rest_api.giftexchange-gateway.id
  resource_id             = each.value
  http_method             = aws_api_gateway_method.auth-post[each.key].http_method
  integration_http_method = "POST"
  type                    = "AWS_PROXY"
  uri                     = aws_lambda_function.giftexchange_app.invoke_arn
  content_handling        = "CONVERT_TO_TEXT"
}

module "gateway-options-response-auth-requestlink" {
  source              = "./modules/gateway-options-response"
  gateway_rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id = aws_api_gateway_resource.auth-requestlink-resource.id
}

module "gateway-options-response-auth-redeem" {
  source              = "./modules/gateway-options-response"
  gateway_rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id = aws_api_gateway_resource.auth-redeem-resource.id
}

# Per-address throttling lives in the application (see LoginTokenProvider). This is the blunt
# instrument that keeps a single source from generating load in the first place.
resource "aws_api_gateway_method_settings" "auth-throttle" {
  for_each = local.auth_endpoints

  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  stage_name  = aws_api_gateway_stage.live-stage.stage_name
  method_path = "auth/${each.key}/POST"

  settings {
    metrics_enabled        = true
    logging_level          = "INFO"
    data_trace_enabled     = false
    throttling_rate_limit  = 5
    throttling_burst_limit = 10
  }

  depends_on = [
    aws_api_gateway_account.this,
    aws_api_gateway_method.auth-post
  ]
}
