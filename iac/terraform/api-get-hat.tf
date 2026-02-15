module "lambda-get-hat" {
  source                      = "./modules/api"
  gateway_rest_api_id         = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id         = aws_api_gateway_resource.hat-email-id-resource.id
  gateway_http_method         = "GET"
  gateway_http_operation_name = "GetHat"
  gateway_method_request_parameters = {
    "method.request.path.email" = true,
    "method.request.path.id"    = true,
  }
  gateway_method_request_model_name                 = ""
  gateway_method_request_model_description          = ""
  gateway_method_request_model_schema_file_location = ""
  include_404_response                              = true
  good_response_model_name                          = "Hat"
  good_response_model_description                   = "A gift exchange hat."
  good_response_model_schema_file_location          = "../../src/GiftExchange.Library/Schemas/Hat.schema.json"
  api_name                                          = "giftexchange-get-hat"
  lambda_invoke_arn                                 = aws_lambda_function.giftexchange_app.invoke_arn
  authorizer_id                                     = aws_api_gateway_authorizer.cognito.id
}
