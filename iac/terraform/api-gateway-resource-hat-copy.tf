resource "aws_api_gateway_resource" "hat-copy-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_resource.hat-resource.id
  path_part   = "copy"
}

module "gateway-options-response-hat-copy" {
  source              = "./modules/gateway-options-response"
  gateway_rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id = aws_api_gateway_resource.hat-copy-resource.id
}
