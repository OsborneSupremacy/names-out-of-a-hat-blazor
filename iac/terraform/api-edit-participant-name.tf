# Changing the name a participant is known by.
#
# The request model bounds the length but states no character pattern. API Gateway compares
# patterns with ECMA 262 regex, which has no Unicode property escapes, so the rule the handler
# applies -- letters, numbers, spaces and common punctuation, in any script -- cannot be written
# here without narrowing it to Latin. The handler's validator is what enforces it.
#
# 409 is declared because a name is unique within an exchange, and a rename reaches every exchange
# the person is in.
module "lambda-edit-participant-name" {
  source                                            = "./modules/api"
  gateway_rest_api_id                               = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id                               = aws_api_gateway_resource.participant-name-resource.id
  gateway_http_method                               = "PUT"
  gateway_http_operation_name                       = "EditParticipantName"
  request_validator_id                              = aws_api_gateway_request_validator.body.id
  gateway_method_request_parameters                 = {}
  gateway_method_request_model_name                 = "EditParticipantNameRequest"
  gateway_method_request_model_description          = "A request to change the name a participant is known by."
  gateway_method_request_model_schema_file_location = "../../src/GiftExchange.Library/Schemas/EditParticipantNameRequest.schema.json"
  include_404_response                              = true
  include_409_response                              = true
  good_response_model_name                          = ""
  good_response_model_description                   = ""
  good_response_model_schema_file_location          = ""
  lambda_invoke_arn                                 = aws_lambda_function.giftexchange_app.invoke_arn
  authorizer_id                                     = aws_api_gateway_authorizer.session.id
  authorizer_type                                   = "CUSTOM"
}
