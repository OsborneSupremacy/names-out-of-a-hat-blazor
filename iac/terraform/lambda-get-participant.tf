module "lambda-get-participant" {
  source                      = "./modules/lambda"
  environment_variables       = local.common_environment_variables
  gateway_rest_api_id         = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id         = aws_api_gateway_resource.participant-resource.id
  gateway_http_method         = "GET"
  gateway_http_operation_name = "GetParticipant"
  gateway_method_request_parameters = {
    "method.request.querystring.organizerEmail"       = true,
    "method.request.querystring.hatId"                = true,
    "method.request.querystring.participantEmail"     = true,
    "method.request.querystring.showpickedrecipients" = true,
  }
  gateway_method_request_model_name                 = ""
  gateway_method_request_model_description          = ""
  gateway_method_request_model_schema_file_location = ""
  include_404_response                              = true
  good_response_model_name                          = "Participant"
  good_response_model_description                   = "A gift exchange participant."
  good_response_model_schema_file_location          = "../../src/GiftExchange.Library/Schemas/Participant.schema.json"
  function_description                              = "Get a gift exchange participant"
  function_memory_size                              = 128
  function_name                                     = "giftexchange-get-participant"
  function_net_class                                = "GetParticipant"
  deployment_package_filename                       = data.archive_file.lambda_function.output_path
  deployment_package_source_code_hash               = data.archive_file.lambda_function.output_base64sha256
  dynamodb_table_arn                                = aws_dynamodb_table.giftexchange.arn
}
