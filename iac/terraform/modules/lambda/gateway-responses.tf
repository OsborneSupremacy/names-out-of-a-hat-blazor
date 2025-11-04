resource "aws_api_gateway_method_response" "get_404_response" {
  rest_api_id = var.gateway_rest_api_id
  resource_id = var.gateway_resource_id
  http_method = var.gateway_http_method
  status_code = "404"
  response_parameters = {
    "method.response.header.Access-Control-Allow-Methods" = true
    "method.response.header.Access-Control-Allow-Headers" = true
    "method.response.header.Access-Control-Allow-Origins" = true
  }
  count       = var.include_404_response ? 1 : 0
}

resource "aws_api_gateway_method_response" "get_200_response" {
  rest_api_id = var.gateway_rest_api_id
  resource_id = var.gateway_resource_id
  http_method = var.gateway_http_method
  status_code = "200"
  response_models = {
    "application/json" = aws_api_gateway_model.good_response_model[0].name
  }
  response_parameters = {
    "method.response.header.Access-Control-Allow-Methods" = true
    "method.response.header.Access-Control-Allow-Headers" = true
    "method.response.header.Access-Control-Allow-Origins" = true
  }
  count = var.good_response_model_name != "" ? 1 : 0
}

resource "aws_api_gateway_model" "good_response_model" {
  rest_api_id  = var.gateway_rest_api_id
  name         = var.good_response_model_name
  description  = var.good_response_model_description
  content_type = "application/json"
  schema       = file(var.good_response_model_schema_file_location)
  count        = var.good_response_model_name != "" ? 1 : 0
}