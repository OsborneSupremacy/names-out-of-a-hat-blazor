# /participant/address.
#
# A child of /participant rather than another verb on it, because PUT /participant already means
# "edit this participant's eligibility" — and that operation resets the hat to IN_PROGRESS, which is
# the one thing a correction made after invitations went out must never do.
resource "aws_api_gateway_resource" "participant-address-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_resource.participant-resource.id
  path_part   = "address"
}

module "gateway-options-response-participant-address" {
  source              = "./modules/gateway-options-response"
  gateway_rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id = aws_api_gateway_resource.participant-address-resource.id
}
