resource "aws_api_gateway_integration" "lambda-integration" {
  rest_api_id             = var.gateway_rest_api_id
  resource_id             = var.gateway_resource_id
  http_method             = var.gateway_http_method
  integration_http_method = "POST"
  type                    = "AWS_PROXY"
  uri                     = aws_lambda_function.lambda.invoke_arn
  content_handling        = "CONVERT_TO_TEXT"
}