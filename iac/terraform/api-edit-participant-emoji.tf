# Changing the face a participant is marked with.
#
# The request model bounds the length of the emoji but cannot state the list it has to come from —
# a JSON schema has no way to say "one of these twenty" without repeating them here, where they
# would drift from PersonEmoji.All. The handler's validator is what enforces membership, and it is
# the only check the field needs.
module "lambda-edit-participant-emoji" {
  source                                            = "./modules/api"
  gateway_rest_api_id                               = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id                               = aws_api_gateway_resource.participant-emoji-resource.id
  gateway_http_method                               = "PUT"
  gateway_http_operation_name                       = "EditParticipantEmoji"
  request_validator_id                              = aws_api_gateway_request_validator.body.id
  gateway_method_request_parameters                 = {}
  gateway_method_request_model_name                 = "EditParticipantEmojiRequest"
  gateway_method_request_model_description          = "A request to change the face a participant is marked with."
  gateway_method_request_model_schema_file_location = "../../src/GiftExchange.Library/Schemas/EditParticipantEmojiRequest.schema.json"
  include_404_response                              = true
  include_409_response                              = false
  good_response_model_name                          = ""
  good_response_model_description                   = ""
  good_response_model_schema_file_location          = ""
  lambda_invoke_arn                                 = aws_lambda_function.giftexchange_app.invoke_arn
  authorizer_id                                     = aws_api_gateway_authorizer.session.id
  authorizer_type                                   = "CUSTOM"
}
