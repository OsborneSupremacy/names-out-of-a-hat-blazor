module "lambda-update-profile" {
  source                                            = "./modules/api"
  gateway_rest_api_id                               = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id                               = aws_api_gateway_resource.profile-resource.id
  gateway_http_method                               = "PUT"
  gateway_http_operation_name                       = "UpdateProfile"
  gateway_method_request_parameters                 = {}
  gateway_method_request_model_name                 = "UpdateProfileRequest"
  gateway_method_request_model_description          = "A request to change the signed-in organizer's display name."
  gateway_method_request_model_schema_file_location = "../../src/GiftExchange.Library/Schemas/UpdateProfileRequest.schema.json"
  include_404_response                              = true
  include_409_response                              = true
  good_response_model_name                          = ""
  good_response_model_description                   = ""
  good_response_model_schema_file_location          = ""
  api_name                                          = "giftexchange-update-profile"
  lambda_invoke_arn                                 = aws_lambda_function.giftexchange_app.invoke_arn
  authorizer_id                                     = aws_api_gateway_authorizer.session.id
  authorizer_type                                   = "CUSTOM"
}
