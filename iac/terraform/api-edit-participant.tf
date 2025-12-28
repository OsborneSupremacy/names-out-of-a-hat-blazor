module "lambda-edit-participant" {
  source                                            = "./modules/api"
  gateway_rest_api_id                               = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id                               = aws_api_gateway_resource.participant-resource.id
  gateway_http_method                               = "PUT"
  gateway_http_operation_name                       = "EditParticipant"
  gateway_method_request_parameters                 = {}
  gateway_method_request_model_name                 = "EditParticipantRequest"
  gateway_method_request_model_description          = "A request to edit a participant."
  gateway_method_request_model_schema_file_location = "../../src/GiftExchange.Library/Schemas/EditParticipantRequest.schema.json"
  include_404_response                              = true
  good_response_model_name                          = ""
  good_response_model_description                   = ""
  good_response_model_schema_file_location          = ""
  api_name                                          = "giftexchange-edit-participant"
  lambda_invoke_arn                                 = aws_lambda_function.giftexchange_app.invoke_arn
}
