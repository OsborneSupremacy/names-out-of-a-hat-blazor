module "lambda-validate-hat" {
  source                                            = "./modules/lambda"
  environment_variables                             = local.common_environment_variables
  gateway_rest_api_id                               = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id                               = aws_api_gateway_resource.hat-validate-resource.id
  gateway_http_method                               = "POST"
  gateway_http_operation_name                       = "ValidateHat"
  gateway_method_request_parameters                 = {}
  gateway_method_request_model_name                 = "ValidateHatRequest"
  gateway_method_request_model_description          = "A request to validate a hat."
  gateway_method_request_model_schema_file_location = "../../src/GiftExchange.Library/Schemas/ValidateHatRequest.schema.json"
  include_404_response                              = true
  good_response_model_name                          = "ValidateHatResponse"
  good_response_model_description                   = "A response to a request to validate a hat."
  good_response_model_schema_file_location          = "../../src/GiftExchange.Library/Schemas/ValidateHatResponse.schema.json"
  function_description                              = "Validate a hat"
  function_memory_size                              = 128
  function_name                                     = "giftexchange-validate-hat"
  function_net_class                                = "ValidateHat"
  deployment_package_filename                       = data.archive_file.lambda_function.output_path
  deployment_package_source_code_hash               = data.archive_file.lambda_function.output_base64sha256
  dynamodb_table_arn                                = aws_dynamodb_table.giftexchange.arn
}
