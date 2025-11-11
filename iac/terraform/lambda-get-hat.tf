module "lambda-get-hat" {
  source                      = "./modules/lambda"
  environment_variables       = local.common_environment_variables
  gateway_rest_api_id         = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id         = aws_api_gateway_resource.hat-resource.id
  gateway_http_method         = "GET"
  gateway_http_operation_name = "GetHat"
  gateway_method_request_parameters = {
    "method.request.querystring.email"                = true,
    "method.request.querystring.id"                   = true,
    "method.request.querystring.showpickedrecipients" = true,
  }
  gateway_method_request_model_name                 = ""
  gateway_method_request_model_description          = ""
  gateway_method_request_model_schema_file_location = ""
  include_404_response                              = true
  good_response_model_name                          = "Hat"
  good_response_model_description                   = "A gift exchange hat."
  good_response_model_schema_file_location          = "../../src/GiftExchange.Library/Schemas/Hat.schema.json"
  function_description                              = "Get a hat"
  function_memory_size                              = 128
  function_name                                     = "giftexchange-get-hat"
  function_net_class                                = "GetHat"
  deployment_package_filename                       = data.archive_file.lambda_function.output_path
  deployment_package_source_code_hash               = data.archive_file.lambda_function.output_base64sha256
  dynamodb_table_arn                                = aws_dynamodb_table.giftexchange.arn
}
