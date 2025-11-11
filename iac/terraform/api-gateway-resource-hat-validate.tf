resource "aws_api_gateway_resource" "hat-validate-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_resource.hat-resource.id
  path_part   = "validate"
}

module "gateway-options-response-hat-validate" {
  source = "./modules/gateway-options-response"
  gateway_rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id = aws_api_gateway_resource.hat-validate-resource.id
}