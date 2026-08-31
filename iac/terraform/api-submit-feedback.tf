module "lambda-submit-feedback" {
  source                                            = "./modules/api"
  gateway_rest_api_id                               = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id                               = aws_api_gateway_resource.feedback-resource.id
  gateway_http_method                               = "POST"
  gateway_http_operation_name                       = "SubmitFeedback"
  gateway_method_request_parameters                 = {}
  gateway_method_request_model_name                 = "SubmitFeedbackRequest"
  gateway_method_request_model_description          = "A question, feature request, or other feedback sent from the contact form in the footer."
  gateway_method_request_model_schema_file_location = "../../src/GiftExchange.Library/Schemas/SubmitFeedbackRequest.schema.json"
  # Neither status is reachable here. There is nothing to fail to find, and nothing to conflict
  # with -- the endpoint's own failure is a 502 when the publish is refused. Declaring responses
  # this method cannot return costs nothing at runtime, since API Gateway does not enforce them
  # against a proxy integration, and misleads whoever reads the API next.
  include_404_response                     = false
  include_409_response                     = false
  good_response_model_name                 = ""
  good_response_model_description          = ""
  good_response_model_schema_file_location = ""
  api_name                                 = "giftexchange-submit-feedback"
  lambda_invoke_arn                        = aws_lambda_function.giftexchange_app.invoke_arn
  authorizer_id                            = aws_api_gateway_authorizer.session.id
  authorizer_type                          = "CUSTOM"
}
