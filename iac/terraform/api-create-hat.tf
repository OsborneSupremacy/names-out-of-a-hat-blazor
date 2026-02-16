module "lambda-create-hat" {
  source                                            = "./modules/api"
  gateway_rest_api_id                               = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id                               = aws_api_gateway_resource.hat-resource.id
  gateway_http_method                               = "POST"
  gateway_http_operation_name                       = "CreateHat"
  gateway_method_request_parameters                 = {}
  gateway_method_request_model_name                 = "CreateHatRequest"
  gateway_method_request_model_description          = "A request to create a hat."
  gateway_method_request_model_schema_file_location = "../../src/GiftExchange.Library/Schemas/CreateHatRequest.schema.json"
  include_404_response                              = true
  include_409_response                              = true
  good_response_model_name                          = "CreateHatResponse"
  good_response_model_description                   = "A response to a request to create a hat."
  good_response_model_schema_file_location          = "../../src/GiftExchange.Library/Schemas/CreateHatResponse.schema.json"
  api_name                                          = "giftexchange-create-hat"
  lambda_invoke_arn                                 = aws_lambda_function.giftexchange_app.invoke_arn
  authorizer_id                                     = aws_api_gateway_authorizer.cognito.id
}
