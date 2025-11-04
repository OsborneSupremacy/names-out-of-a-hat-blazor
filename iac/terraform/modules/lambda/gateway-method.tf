resource "aws_api_gateway_method" "gateway-operation-method" {
  rest_api_id          = var.gateway_rest_api_id
  resource_id          = var.gateway_resource_id
  http_method          = var.gateway_http_method
  authorization        = "NONE"
  operation_name       = var.gateway_http_operation_name
  request_validator_id = aws_api_gateway_request_validator.request_validator.id
  request_parameters   = var.gateway_method_request_parameters
  request_models = {
    "application/json" = var.gateway_method_request_model_name != "" ? var.gateway_method_request_model_name : null
  }
  count = var.gateway_http_operation_name != "" ? 1 : 0
}