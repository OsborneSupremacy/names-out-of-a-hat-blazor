resource "aws_api_gateway_resource" "organizer-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_rest_api.giftexchange-gateway.root_resource_id
  path_part   = "organizer"
}

resource "aws_api_gateway_resource" "organizer-verification-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_resource.organizer-resource.id
  path_part   = "verification"
}

resource "aws_api_gateway_resource" "organizer-verify-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_resource.organizer-verification-resource.id
  path_part   = "verify"
}

module "gateway-options-response-organizer" {
  source              = "./modules/gateway-options-response"
  gateway_rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id = aws_api_gateway_resource.organizer-resource.id
}
