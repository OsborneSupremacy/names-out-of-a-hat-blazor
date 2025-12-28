module "lambda-get-participant" {
  source                      = "./modules/api"
  gateway_rest_api_id         = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id         = aws_api_gateway_resource.participant-organizeremail-hatid-participantemail-resource.id
  gateway_http_method         = "GET"
  gateway_http_operation_name = "GetParticipant"
  gateway_method_request_parameters = {
    "method.request.path.organizerEmail"              = true,
    "method.request.path.hatId"                       = true,
    "method.request.path.participantEmail"            = true,
    "method.request.querystring.showpickedrecipients" = true,
  }
  gateway_method_request_model_name                 = ""
  gateway_method_request_model_description          = ""
  gateway_method_request_model_schema_file_location = ""
  include_404_response                              = true
  good_response_model_name                          = "Participant"
  good_response_model_description                   = "A gift exchange participant."
  good_response_model_schema_file_location          = "../../src/GiftExchange.Library/Schemas/Participant.schema.json"
  api_name                                          = "giftexchange-get-participant"
  lambda_invoke_arn                                 = aws_lambda_function.giftexchange_app.invoke_arn
}
