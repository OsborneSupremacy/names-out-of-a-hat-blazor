module "lambda-remove-participant" {
  source                                            = "./modules/lambda"
  environment_variables                             = local.common_environment_variables
  gateway_rest_api_id                               = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id                               = aws_api_gateway_resource.participant-resource.id
  gateway_http_method                               = "DELETE"
  gateway_http_operation_name                       = "RemoveParticipant"
  gateway_method_request_parameters                 = {}
  gateway_method_request_model_name                 = "RemoveParticipantRequest"
  gateway_method_request_model_description          = "A request to remove a participant."
  gateway_method_request_model_schema_file_location = "../../src/GiftExchange.Library/Schemas/RemoveParticipantRequest.schema.json"
  include_404_response                              = true
  good_response_model_name                          = ""
  good_response_model_description                   = ""
  good_response_model_schema_file_location          = ""
  function_description                              = "Remove a participant"
  function_memory_size                              = 128
  function_name                                     = "giftexchange-remove-participant"
  function_net_class                                = "RemoveParticipant"
  deployment_package_filename                       = data.archive_file.lambda_function.output_path
  deployment_package_source_code_hash               = data.archive_file.lambda_function.output_base64sha256
  dynamodb_table_arn                                = aws_dynamodb_table.giftexchange.arn
}
