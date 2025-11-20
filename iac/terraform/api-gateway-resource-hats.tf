resource "aws_api_gateway_resource" "hats-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_rest_api.giftexchange-gateway.root_resource_id
  path_part   = "hats"
}

resource "aws_api_gateway_resource" "hats-email-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_resource.hats-resource.id
  path_part   = "{email}"
}

module "gateway-options-response-hats" {
  source = "./modules/gateway-options-response"
  gateway_rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id = aws_api_gateway_resource.hats-resource.id
}