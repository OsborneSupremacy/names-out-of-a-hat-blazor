resource "aws_api_gateway_resource" "hat-preview-invitations-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_resource.hat-resource.id
  path_part   = "previewinvitations"
}

module "gateway-options-response-hat-preview-invitations" {
  source              = "./modules/gateway-options-response"
  gateway_rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id = aws_api_gateway_resource.hat-preview-invitations-resource.id
}
