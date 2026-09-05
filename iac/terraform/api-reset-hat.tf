module "lambda-reset-hat" {
  source                                            = "./modules/api"
  gateway_rest_api_id                               = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id                               = aws_api_gateway_resource.hat-reset-resource.id
  gateway_http_method                               = "POST"
  gateway_http_operation_name                       = "ResetHat"
  request_validator_id                              = aws_api_gateway_request_validator.body.id
  gateway_method_request_parameters                 = {}
  gateway_method_request_model_name                 = "ResetHatRequest"
  gateway_method_request_model_description          = "A request to take a gift exchange back to the beginning, keeping its participants."
  gateway_method_request_model_schema_file_location = "../../src/GiftExchange.Library/Schemas/ResetHatRequest.schema.json"
  include_404_response                              = true
  include_409_response                              = true
  good_response_model_name                          = ""
  good_response_model_description                   = ""
  good_response_model_schema_file_location          = ""
  lambda_invoke_arn                                 = aws_lambda_function.giftexchange_app.invoke_arn
  authorizer_id                                     = aws_api_gateway_authorizer.session.id
  authorizer_type                                   = "CUSTOM"
}
