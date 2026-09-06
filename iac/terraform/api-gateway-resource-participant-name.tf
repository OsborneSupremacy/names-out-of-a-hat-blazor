# /participant/name.
#
# A child of /participant for the same reason /participant/address and /participant/emoji are:
# PUT /participant means "edit this participant's eligibility", and that resets the hat to
# IN_PROGRESS. Eligibility and picks are held as participant ids, so what somebody is called has no
# bearing on the draw — and a rename must not throw one away.
resource "aws_api_gateway_resource" "participant-name-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_resource.participant-resource.id
  path_part   = "name"
}

module "gateway-options-response-participant-name" {
  source              = "./modules/gateway-options-response"
  gateway_rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id = aws_api_gateway_resource.participant-name-resource.id
}
