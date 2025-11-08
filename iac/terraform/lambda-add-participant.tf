module "lambda-add-participant" {
  source                                            = "./modules/lambda"
  environment_variables                             = local.common_environment_variables
  gateway_rest_api_id                               = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id                               = aws_api_gateway_resource.participant-resource.id
  gateway_http_method                               = "POST"
  gateway_http_operation_name                       = "AddParticipant"
  gateway_method_request_parameters                 = {}
  gateway_method_request_model_name                 = "AddParticipantRequest"
  gateway_method_request_model_description          = "A request to add a participant."
  gateway_method_request_model_schema_file_location = "../../src/GiftExchange.Library/Schemas/AddParticipantRequest.schema.json"
  include_404_response                              = true
  good_response_model_name                          = "AddParticipantResponse"
  good_response_model_description                   = "A response to a request to add a participant."
  good_response_model_schema_file_location          = "../../src/GiftExchange.Library/Schemas/AddParticipantResponse.schema.json"
  function_description                              = "Add a participant"
  function_memory_size                              = 128
  function_name                                     = "giftexchange-add-participant"
  function_net_class                                = "AddParticipant"
  deployment_package_filename                       = data.archive_file.lambda_function.output_path
  deployment_package_source_code_hash               = data.archive_file.lambda_function.output_base64sha256
}
