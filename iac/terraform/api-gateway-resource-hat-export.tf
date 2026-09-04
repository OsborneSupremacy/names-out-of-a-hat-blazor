# /hat/{email}/export/{id}. A sibling of previewinvitations rather than of /hat/{email}/{id},
# because a path parameter cannot be a literal's sibling: {id} directly under {email} already
# claims every single segment there, so "export" has to be a segment of its own.
resource "aws_api_gateway_resource" "hat-email-export-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_resource.hat-email-resource.id
  path_part   = "export"
}

resource "aws_api_gateway_resource" "hat-email-export-id-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_resource.hat-email-export-resource.id
  path_part   = "{id}"
}

module "gateway-options-response-hat-export" {
  source              = "./modules/gateway-options-response"
  gateway_rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id = aws_api_gateway_resource.hat-email-export-id-resource.id
}
