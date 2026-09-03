resource "aws_api_gateway_rest_api" "giftexchange-gateway" {
  name        = "giftexchange-gateway"
  description = "API Gateway for the Gift Exchange App"
  endpoint_configuration {
    types = ["REGIONAL"]
  }
}

# Which deployment the live stage serves is not Terraform's business.
#
# Publishing an API Gateway stage is a release, not a piece of infrastructure. It has to happen
# after the Lambda package is built and uploaded, it has to be repeatable without a plan, and it is
# the step you want to be able to run on its own when something needs republishing. All of that is
# CI's job, and .github/workflows/.reusable-deploy-api-gateway-stage.yml does it: after every apply
# it calls create-deployment and repoints the stage at the result.
#
# This resource exists only because aws_api_gateway_stage requires a deployment_id and will not be
# created without one. It is a bootstrap, superseded seconds later on the very first build and
# never consulted again. Nothing here should ever be made to represent what is actually live —
# an earlier version of this file carried a triggers hash trying to do exactly that, which was
# redundant from the day it was written and only ever managed to imply that Terraform decided
# something it did not.
resource "aws_api_gateway_deployment" "default" {
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id
  description = "Bootstrap deployment. The live stage is published by CI, not from here."

  lifecycle {
    create_before_destroy = true
  }

  # One endpoint, not all of them. Creating a deployment against a REST API with no methods fails
  # outright, so a from-scratch build needs at least one to exist first — and one is all that check
  # requires. Listing every module would look like completeness mattered, and the list would then
  # rot silently: leaving an endpoint out has no consequence, because CI publishes the API a moment
  # later regardless of what this deployment contains.
  depends_on = [module.lambda-get-hat]
}

resource "aws_api_gateway_stage" "live-stage" {
  stage_name  = "live"
  rest_api_id = aws_api_gateway_rest_api.giftexchange-gateway.id

  # Read once, when the stage is first created, and never again. CI repoints the stage after every
  # apply, so without the ignore below every subsequent plan would propose dragging it back to the
  # bootstrap deployment above — a diff that says Terraform is in charge of what is live, which it
  # is not, and which would be genuinely destructive if anybody ever applied it between a build and
  # its publish step.
  deployment_id = aws_api_gateway_deployment.default.id

  lifecycle {
    ignore_changes = [deployment_id]
  }

  # Without this the trace starts at the Lambda invocation and the time API Gateway spent before
  # it -- authorizer, request validation, integration setup -- is simply missing from it. Since
  # what these traces are mostly for is the gap between the 28 second function timeout and the
  # 29 second gateway ceiling, the gateway's own share of a request is the half worth having.
  xray_tracing_enabled = true

  access_log_settings {
    destination_arn = aws_cloudwatch_log_group.api_gateway_access_logs.arn
    format = jsonencode({
      requestId      = "$context.requestId"
      ip             = "$context.identity.sourceIp"
      caller         = "$context.identity.caller"
      user           = "$context.identity.user"
      requestTime    = "$context.requestTime"
      httpMethod     = "$context.httpMethod"
      resourcePath   = "$context.resourcePath"
      status         = "$context.status"
      protocol       = "$context.protocol"
      responseLength = "$context.responseLength"
    })
  }
}

resource "aws_api_gateway_model" "error_response_model" {
  rest_api_id  = aws_api_gateway_rest_api.giftexchange-gateway.id
  name         = "BadRequestOrConflictResponse"
  description  = "A response model for 404 Not Found or 409 Conflict errors."
  content_type = "application/json"
  schema       = file("../../src/GiftExchange.Library/Schemas/ErrorResponse.schema.json")
}
