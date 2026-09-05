module "lambda-copy-hat" {
  source                                            = "./modules/api"
  gateway_rest_api_id                               = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id                               = aws_api_gateway_resource.hat-copy-resource.id
  gateway_http_method                               = "POST"
  gateway_http_operation_name                       = "CopyHat"
  request_validator_id                              = aws_api_gateway_request_validator.body.id
  gateway_method_request_parameters                 = {}
  gateway_method_request_model_name                 = "CopyHatRequest"
  gateway_method_request_model_description          = "A request to copy a revealed gift exchange into a new one."
  gateway_method_request_model_schema_file_location = "../../src/GiftExchange.Library/Schemas/CopyHatRequest.schema.json"
  include_404_response                              = true
  include_409_response                              = true
  good_response_model_name                          = "CopyHatResponse"
  good_response_model_description                   = "A response to a request to copy a gift exchange."
  good_response_model_schema_file_location          = "../../src/GiftExchange.Library/Schemas/CopyHatResponse.schema.json"
  api_name                                          = "giftexchange-copy-hat"
  lambda_invoke_arn                                 = aws_lambda_function.giftexchange_app.invoke_arn
  authorizer_id                                     = aws_api_gateway_authorizer.session.id
  authorizer_type                                   = "CUSTOM"
}
