module "lambda-export-hat" {
  source                      = "./modules/api"
  gateway_rest_api_id         = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id         = aws_api_gateway_resource.hat-email-export-id-resource.id
  gateway_http_method         = "GET"
  gateway_http_operation_name = "ExportHat"
  request_validator_id        = aws_api_gateway_request_validator.params.id
  gateway_method_request_parameters = {
    "method.request.path.email" = true,
    "method.request.path.id"    = true,
  }
  gateway_method_request_model_name                 = ""
  gateway_method_request_model_description          = ""
  gateway_method_request_model_schema_file_location = ""
  include_404_response                              = true
  good_response_model_name                          = "ExportHatResponse"
  good_response_model_description                   = "A whole gift exchange, as data, for the organizer to take away."
  good_response_model_schema_file_location          = "../../src/GiftExchange.Library/Schemas/ExportHatResponse.schema.json"
  lambda_invoke_arn                                 = aws_lambda_function.giftexchange_app.invoke_arn
  authorizer_id                                     = aws_api_gateway_authorizer.session.id
  authorizer_type                                   = "CUSTOM"
}
