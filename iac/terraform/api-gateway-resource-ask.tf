# ---------------------------------------------------------------------------------------------
# The Ask: /ask/{token}, unauthenticated.
#
# Longhand rather than through ./modules/api, for the same reason the auth endpoints are: that
# module assumes an authenticated endpoint carrying JSON request and response models, and these
# two are neither. They are reached by clicking a link in an email and they return a web page.
#
# The credential is the token in the path. There is no session here and cannot be — the reader is
# somebody who opened their invitation on a phone, and the point of the feature is that it takes
# one tap.
# ---------------------------------------------------------------------------------------------

resource "aws_api_gateway_resource" "ask-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_rest_api.giftexchange-gateway.root_resource_id
  path_part   = "ask"
}

resource "aws_api_gateway_resource" "ask-token-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_resource.ask-resource.id
  path_part   = "{token}"
}

locals {
  # GET renders a confirmation page; POST behind the button on it performs the Ask. Split because
  # mail security scanners fetch links in delivered mail, so a GET that acted would send the Ask
  # before the participant had read their invitation.
  ask_methods = ["GET", "POST"]
}

resource "aws_api_gateway_method" "ask" {
  for_each = toset(local.ask_methods)

  rest_api_id   = aws_api_gateway_rest_api.giftexchange-gateway.id
  resource_id   = aws_api_gateway_resource.ask-token-resource.id
  http_method   = each.value
  authorization = "NONE"

  request_parameters = {
    "method.request.path.token" = true
  }
}

resource "aws_api_gateway_integration" "ask" {
  for_each = toset(local.ask_methods)

  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  resource_id = aws_api_gateway_resource.ask-token-resource.id
  http_method = aws_api_gateway_method.ask[each.value].http_method

  # POST regardless of the method the caller used: this is how Lambda proxy integrations are
  # invoked, and has nothing to do with the verb the browser sent.
  integration_http_method = "POST"
  type                    = "AWS_PROXY"
  uri                     = aws_lambda_function.giftexchange_app.invoke_arn
  content_handling        = "CONVERT_TO_TEXT"
}

# No OPTIONS response, unlike every other endpoint here. Nothing calls these from JavaScript —
# a browser navigates to them, and a form on the page we return posts back. There is no preflight
# to answer, and adding CORS headers would only invite calling them cross-origin.

# The application throttles per participant, at one Ask a week, and that is the limit that matters.
# This is the cruder one underneath it: somebody enumerating tokens never reaches the throttle,
# because no token they guess resolves to a participant to throttle.
resource "aws_api_gateway_method_settings" "ask-throttle" {
  for_each = toset(local.ask_methods)

  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  stage_name  = aws_api_gateway_stage.live-stage.stage_name
  method_path = "ask/{token}/${each.value}"

  settings {
    metrics_enabled        = true
    logging_level          = "INFO"
    data_trace_enabled     = false
    throttling_rate_limit  = 5
    throttling_burst_limit = 10
  }

  depends_on = [
    aws_api_gateway_account.this,
    aws_api_gateway_method.ask
  ]
}
