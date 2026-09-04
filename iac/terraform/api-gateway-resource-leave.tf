# ---------------------------------------------------------------------------------------------
# Leaving: /leave/{token}, unauthenticated.
#
# Longhand rather than through ./modules/api, for the reason the Ask and the auth endpoints are:
# that module assumes an authenticated endpoint carrying JSON request and response models, and
# these two are neither. They are reached by clicking a link in the fine print of an invitation
# and they return a web page.
#
# The credential is the token in the path, and there is no session here and cannot be. The reader
# is somebody who has decided they do not want to be in a gift exchange — asking them to create an
# account with the service that mailed them in order to get out of it would be a poor answer, and
# would leave the address on file either way.
# ---------------------------------------------------------------------------------------------

resource "aws_api_gateway_resource" "leave-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_rest_api.giftexchange-gateway.root_resource_id
  path_part   = "leave"
}

resource "aws_api_gateway_resource" "leave-token-resource" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  parent_id   = aws_api_gateway_resource.leave-resource.id
  path_part   = "{token}"
}

locals {
  # GET renders the confirmation; POST behind the button on it does the removal. The same split as
  # the Ask, and here the cost of getting it wrong is higher: mail security scanners fetch links in
  # delivered mail, so a GET that acted would take somebody out of an exchange, send the organizer
  # back to the hat and tell everybody else to disregard their name — all before the participant
  # had opened the invitation.
  leave_methods = ["GET", "POST"]
}

resource "aws_api_gateway_method" "leave" {
  for_each = toset(local.leave_methods)

  rest_api_id   = aws_api_gateway_rest_api.giftexchange-gateway.id
  resource_id   = aws_api_gateway_resource.leave-token-resource.id
  http_method   = each.value
  authorization = "NONE"

  request_parameters = {
    "method.request.path.token" = true
  }
}

resource "aws_api_gateway_integration" "leave" {
  for_each = toset(local.leave_methods)

  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  resource_id = aws_api_gateway_resource.leave-token-resource.id
  http_method = aws_api_gateway_method.leave[each.value].http_method

  # POST regardless of the method the caller used: this is how Lambda proxy integrations are
  # invoked, and has nothing to do with the verb the browser sent.
  integration_http_method = "POST"
  type                    = "AWS_PROXY"
  uri                     = aws_lambda_function.giftexchange_app.invoke_arn
  content_handling        = "CONVERT_TO_TEXT"
}

# No OPTIONS response, as on the Ask. A browser navigates to these and the form on the page we
# return posts back to the same address; there is no preflight to answer, and adding CORS headers
# would only invite calling them cross-origin.

# The only rate limit these have. Unlike the Ask there is no application-level throttle underneath
# — leaving is a thing somebody does once, and a "you have left too recently" message would be
# absurd — so this is what stands between the endpoint and somebody enumerating tokens. It is worth
# little on its own: a token is 256 bits and nothing guessed will ever resolve. What it buys is
# that the guessing costs the guesser more than it costs us.
resource "aws_api_gateway_method_settings" "leave-throttle" {
  for_each = toset(local.leave_methods)

  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  stage_name  = aws_api_gateway_stage.live-stage.stage_name
  method_path = "leave/{token}/${each.value}"

  settings {
    metrics_enabled        = true
    logging_level          = "INFO"
    data_trace_enabled     = false
    throttling_rate_limit  = 5
    throttling_burst_limit = 10
  }

  depends_on = [
    aws_api_gateway_account.this,
    aws_api_gateway_method.leave
  ]
}
