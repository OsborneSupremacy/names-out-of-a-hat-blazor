# Two request validators for the whole API, not one per endpoint.
#
# A request validator is a property of the REST API, not of a method: a method just points at one
# by id, and any number of methods may point at the same one. API Gateway caps how many validators
# a single API may hold, and modules/api used to create one per endpoint. At twenty endpoints that
# cap was reached, and the failure is badly disguised — CreateRequestValidator answers HTTP 429
# with LimitExceededException, which the AWS provider reads as throttling and retries twenty-five
# times with backoff, so the apply shows "Still creating..." for a quarter of an hour and never
# says what is wrong.
#
# Across every endpoint there are only two shapes: bodies are validated on the writes, which carry
# a request model and no path parameters, and parameters are validated on the reads, which carry
# path parameters and no body. Hence two.
#
# Both are adopted from validators that already existed, via the moved blocks below, so that this
# change creates nothing. That matters: the API is at its cap, so a plan that created the shared
# pair before destroying the per-endpoint ones would fail with the very error it is fixing.

resource "aws_api_gateway_request_validator" "body" {
  name                        = "giftexchange-body-validator"
  rest_api_id                 = aws_api_gateway_rest_api.giftexchange-gateway.id
  validate_request_body       = true
  validate_request_parameters = false
}

resource "aws_api_gateway_request_validator" "params" {
  name                        = "giftexchange-params-validator"
  rest_api_id                 = aws_api_gateway_rest_api.giftexchange-gateway.id
  validate_request_body       = false
  validate_request_parameters = true
}

moved {
  from = module.lambda-create-hat.aws_api_gateway_request_validator.request_validator
  to   = aws_api_gateway_request_validator.body
}

moved {
  from = module.lambda-get-hat.aws_api_gateway_request_validator.request_validator
  to   = aws_api_gateway_request_validator.params
}
