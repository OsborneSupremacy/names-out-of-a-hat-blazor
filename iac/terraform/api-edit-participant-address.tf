# Correcting the address a participant was invited at, which resends what they missed.
#
# No 429 method response is declared even though the handler returns one when the change is too
# soon after the last. The integration is AWS_PROXY, so method responses are documentation rather
# than a filter — the status the function returns is the status the caller gets.
module "lambda-edit-participant-address" {
  source                                            = "./modules/api"
  gateway_rest_api_id                               = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id                               = aws_api_gateway_resource.participant-address-resource.id
  gateway_http_method                               = "PUT"
  gateway_http_operation_name                       = "EditParticipantAddress"
  request_validator_id                              = aws_api_gateway_request_validator.body.id
  gateway_method_request_parameters                 = {}
  gateway_method_request_model_name                 = "EditParticipantAddressRequest"
  gateway_method_request_model_description          = "A request to correct the email address a participant was invited at."
  gateway_method_request_model_schema_file_location = "../../src/GiftExchange.Library/Schemas/EditParticipantAddressRequest.schema.json"
  include_404_response                              = true
  include_409_response                              = true
  good_response_model_name                          = "EditParticipantAddressResponse"
  good_response_model_description                   = "Says whether correcting the address also resent an email, and which one."
  good_response_model_schema_file_location          = "../../src/GiftExchange.Library/Schemas/EditParticipantAddressResponse.schema.json"
  api_name                                          = "giftexchange-edit-participant-address"
  lambda_invoke_arn                                 = aws_lambda_function.giftexchange_app.invoke_arn
  authorizer_id                                     = aws_api_gateway_authorizer.session.id
  authorizer_type                                   = "CUSTOM"
}
