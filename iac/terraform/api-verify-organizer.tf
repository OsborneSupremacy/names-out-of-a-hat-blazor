module "lambda-verify-organizer" {
  source                                            = "./modules/api"
  gateway_rest_api_id                               = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id                               = aws_api_gateway_resource.organizer-verify-resource.id
  gateway_http_method                               = "POST"
  gateway_http_operation_name                       = "VerifyOrganizer"
  gateway_method_request_parameters                 = {}
  gateway_method_request_model_name                 = "VerifyOrganizerRequest"
  gateway_method_request_model_description          = "A request to verify an organizer with a verification code."
  gateway_method_request_model_schema_file_location = "../../src/GiftExchange.Library/Schemas/VerifyOrganizerRequest.schema.json"
  include_404_response                              = true
  include_409_response                              = false
  good_response_model_name                          = ""
  good_response_model_description                   = ""
  good_response_model_schema_file_location          = ""
  api_name                                          = "giftexchange-verify-organizer"
  lambda_invoke_arn                                 = aws_lambda_function.giftexchange_app.invoke_arn
  authorizer_id                                     = aws_api_gateway_authorizer.cognito.id
}
