resource "aws_api_gateway_request_validator" "request_validator" {
  name                        = "${var.function_name}-request-validator"
  rest_api_id                 = var.gateway_rest_api_id
  validate_request_body       = var.gateway_method_request_model_name != "" ? true : false
  validate_request_parameters = length(var.gateway_method_request_parameters) > 0 ? true : false
}

resource "aws_api_gateway_model" "request_model" {
  rest_api_id  = var.gateway_rest_api_id
  name         = var.gateway_method_request_model_name
  description  = var.gateway_method_request_model_description
  content_type = "application/json"
  schema       = file(var.gateway_method_request_model_schema_file_location)
  count        = var.gateway_method_request_model_name != "" ? 1 : 0
}