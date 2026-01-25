resource "aws_api_gateway_resource" "participant-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_rest_api.giftexchange-gateway.root_resource_id
  path_part   = "participant"
}

resource "aws_api_gateway_resource" "participant-organizeremail-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_resource.participant-resource.id
  path_part   = "{organizerEmail}"
}

resource "aws_api_gateway_resource" "participant-organizeremail-hatid-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_resource.participant-organizeremail-resource.id
  path_part   = "{hatId}"
}

resource "aws_api_gateway_resource" "participant-organizeremail-hatid-participantemail-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_resource.participant-organizeremail-hatid-resource.id
  path_part   = "{participantEmail}"
}

module "gateway-options-response-participant" {
  source = "./modules/gateway-options-response"
  gateway_rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id = aws_api_gateway_resource.participant-resource.id
}

module "gateway-options-response-participant-full-path" {
  source = "./modules/gateway-options-response"
  gateway_rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id = aws_api_gateway_resource.participant-organizeremail-hatid-participantemail-resource.id
}