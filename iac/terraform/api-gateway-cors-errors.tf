# API Gateway generates its own responses for authorizer rejections, request validation failures,
# integration timeouts and malformed Lambda responses. Those never reach the Lambda, so they carry
# none of the CORS headers ProxyResponseBuilder attaches.
#
# A browser cannot read a cross-origin response without them, so every one of those failures
# surfaces in the UI as an opaque "Failed to fetch" — the actual status code and message are
# discarded before any JavaScript sees them. These two responses put the headers back, so the
# frontend's existing error handling can show what really went wrong.
locals {
  cors_error_response_parameters = {
    "gatewayresponse.header.Access-Control-Allow-Origin"  = "'*'"
    "gatewayresponse.header.Access-Control-Allow-Headers" = "'Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token'"
    "gatewayresponse.header.Access-Control-Allow-Methods" = "'GET,OPTIONS,POST,PUT,DELETE'"
  }
}

resource "aws_api_gateway_gateway_response" "cors_default_4xx" {
  rest_api_id         = aws_api_gateway_rest_api.giftexchange-gateway.id
  response_type       = "DEFAULT_4XX"
  response_parameters = local.cors_error_response_parameters
}

resource "aws_api_gateway_gateway_response" "cors_default_5xx" {
  rest_api_id         = aws_api_gateway_rest_api.giftexchange-gateway.id
  response_type       = "DEFAULT_5XX"
  response_parameters = local.cors_error_response_parameters
}
