module "lambda-delete-hat" {
  source                                            = "./modules/api"
  gateway_rest_api_id                               = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id                               = aws_api_gateway_resource.hat-resource.id
  gateway_http_method                               = "DELETE"
  gateway_http_operation_name                       = "DeleteHat"
  gateway_method_request_parameters                 = {}
  gateway_method_request_model_name                 = "DeleteHatRequest"
  gateway_method_request_model_description          = "A request to delete a hat."
  gateway_method_request_model_schema_file_location = "../../src/GiftExchange.Library/Schemas/DeleteHatRequest.schema.json"
  include_404_response                              = true
  good_response_model_name                          = ""
  good_response_model_description                   = ""
  good_response_model_schema_file_location          = ""
  api_name                                          = "giftexchange-delete-hat"
  lambda_invoke_arn                                 = aws_lambda_function.giftexchange_app.invoke_arn
}
