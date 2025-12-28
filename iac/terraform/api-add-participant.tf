module "lambda-add-participant" {
  source                                            = "./modules/api"
  gateway_rest_api_id                               = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id                               = aws_api_gateway_resource.participant-resource.id
  gateway_http_method                               = "POST"
  gateway_http_operation_name                       = "AddParticipant"
  gateway_method_request_parameters                 = {}
  gateway_method_request_model_name                 = "AddParticipantRequest"
  gateway_method_request_model_description          = "A request to add a participant."
  gateway_method_request_model_schema_file_location = "../../src/GiftExchange.Library/Schemas/AddParticipantRequest.schema.json"
  include_404_response                              = true
  good_response_model_name                          = ""
  good_response_model_description                   = ""
  good_response_model_schema_file_location          = ""
  api_name                                          = "giftexchange-add-participant"
  lambda_invoke_arn                                 = aws_lambda_function.giftexchange_app.arn
}
