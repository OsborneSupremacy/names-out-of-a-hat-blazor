module "lambda-assign-recipients" {
  source                                            = "./modules/api"
  gateway_rest_api_id                               = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id                               = aws_api_gateway_resource.recipients-resource.id
  gateway_http_method                               = "POST"
  gateway_http_operation_name                       = "AssignRecipients"
  gateway_method_request_parameters                 = {}
  gateway_method_request_model_name                 = "AssignRecipientsRequest"
  gateway_method_request_model_description          = "A request to assign recipients."
  gateway_method_request_model_schema_file_location = "../../src/GiftExchange.Library/Schemas/AssignRecipientsRequest.schema.json"
  include_404_response                              = true
  good_response_model_name                          = ""
  good_response_model_description                   = ""
  good_response_model_schema_file_location          = ""
  api_name                                          = "giftexchange-assign-recipients"
  lambda_invoke_arn                                 = aws_lambda_function.giftexchange_app.invoke_arn
}
