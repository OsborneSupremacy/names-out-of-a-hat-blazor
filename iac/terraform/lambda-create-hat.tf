module "lambda-create-hat" {
  source                                            = "./modules/lambda"
  environment_variables                             = local.common_environment_variables
  gateway_rest_api_id                               = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id                               = aws_api_gateway_resource.hat-resource.id
  gateway_http_method                               = "POST"
  gateway_http_operation_name                       = "CreateHat"
  gateway_method_request_parameters                 = {}
  gateway_method_request_model_name                 = "CreateHatRequest"
  gateway_method_request_model_description          = "A request to create a hat."
  gateway_method_request_model_schema_file_location = "../../src/GiftExchange.Library/Schemas/CreateHatRequest.schema.json"
  include_404_response                              = true
  good_response_model_name                          = "CreateHatResponse"
  good_response_model_description                   = "A response to a request to create a hat."
  good_response_model_schema_file_location          = "../../src/GiftExchange.Library/Schemas/CreateHatResponse.schema.json"
  function_description                              = "Create a hat"
  function_memory_size                              = 128
  function_name                                     = "giftexchange-create-hat"
  deployment_package_filename                       = data.archive_file.lambda_function.output_path
  deployment_package_source_code_hash               = data.archive_file.lambda_function.output_base64sha256
  dynamodb_table_arn                                = aws_dynamodb_table.giftexchange.arn
}
