# /participant/emoji.
#
# A child of /participant for the same reason /participant/address is one: PUT /participant means
# "edit this participant's eligibility", and that resets the hat to IN_PROGRESS. Changing the face
# somebody is marked with is decoration, and decoration must not throw away a draw.
resource "aws_api_gateway_resource" "participant-emoji-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_resource.participant-resource.id
  path_part   = "emoji"
}

module "gateway-options-response-participant-emoji" {
  source              = "./modules/gateway-options-response"
  gateway_rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id = aws_api_gateway_resource.participant-emoji-resource.id
}
