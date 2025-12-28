module "lambda-get-hats" {
  source                      = "./modules/api"
  gateway_rest_api_id         = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id         = aws_api_gateway_resource.hats-email-resource.id
  gateway_http_method         = "GET"
  gateway_http_operation_name = "GetHats"
  gateway_method_request_parameters = {
    "method.request.path.email" = true
  }
  gateway_method_request_model_name                 = ""
  gateway_method_request_model_description          = ""
  gateway_method_request_model_schema_file_location = ""
  include_404_response                              = true
  good_response_model_name                          = "GetHatsResponse"
  good_response_model_description                   = "The meta data of the gift exchanges."
  good_response_model_schema_file_location          = "../../src/GiftExchange.Library/Schemas/GetHatsResponse.schema.json"
  api_name                                          = "giftexchange-get-hats"
  lambda_invoke_arn                                 = aws_lambda_function.giftexchange_app.invoke_arn
}
