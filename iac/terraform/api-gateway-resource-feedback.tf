resource "aws_api_gateway_resource" "feedback-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_rest_api.giftexchange-gateway.root_resource_id
  path_part   = "feedback"
}

module "gateway-options-response-feedback" {
  source              = "./modules/gateway-options-response"
  gateway_rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id = aws_api_gateway_resource.feedback-resource.id
}

# The endpoint is authenticated, so the throttle is not what stands between this and a spam run --
# the authorizer is. What it covers is the other case: a signed-in browser stuck in a retry loop,
# where every attempt would otherwise become an email. The application does not throttle per
# sender, deliberately; somebody sending three messages in a row is usually somebody remembering a
# third thing, and refusing that would be worse than the noise.
resource "aws_api_gateway_method_settings" "feedback-throttle" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  stage_name  = aws_api_gateway_stage.live-stage.stage_name
  method_path = "feedback/POST"

  settings {
    metrics_enabled        = true
    logging_level          = "INFO"
    data_trace_enabled     = false
    throttling_rate_limit  = 2
    throttling_burst_limit = 5
  }

  depends_on = [
    aws_api_gateway_account.this,
    module.lambda-submit-feedback
  ]
}
