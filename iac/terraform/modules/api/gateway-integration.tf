resource "aws_api_gateway_integration" "lambda-integration" {
  rest_api_id             = var.gateway_rest_api_id
  resource_id             = var.gateway_resource_id
  http_method             = var.gateway_http_method
  integration_http_method = "POST"
  type                    = "AWS_PROXY"
  uri                     = var.lambda_invoke_arn
  content_handling        = "CONVERT_TO_TEXT"

  # http_method is a literal, not a reference to the method resource, so nothing in the graph
  # otherwise stops Terraform putting the integration before the method exists. It usually wins
  # that race; when it loses, PutIntegration returns "Invalid Method identifier specified" and the
  # provider retries it as an eventual-consistency blip rather than failing.
  depends_on = [
    aws_api_gateway_method.gateway-operation-method
  ]
}
