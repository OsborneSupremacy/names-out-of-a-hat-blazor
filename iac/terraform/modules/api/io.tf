# inputs
variable "gateway_rest_api_id" {
  type = string
}

variable "gateway_resource_id" {
  type = string
}

variable "gateway_http_method" {
  type = string
}

variable "gateway_http_operation_name" {
  description = "This is the name used for the API Gateway method's SDK operation name. Doesn't appear to make any functional difference."
  type        = string
}

variable "gateway_method_request_parameters" {
  description = "Request parameters for the API Gateway method"
  type        = map(string)
  default     = {}
}

variable "gateway_method_request_model_schema_file_location" {
  description = "Path to the file containing the request model schema within the local filesystem."
  type        = string
}

variable "gateway_method_request_model_name" {
  description = "The name of the request model"
  type        = string
}

variable "gateway_method_request_model_description" {
  description = "The description of the request model"
  type        = string
}

variable "api_name" {
  type = string
}

variable "lambda_invoke_arn" {
  type = string
}

variable "include_404_response" {
  type    = bool
  default = false
}

variable "include_409_response" {
  type    = bool
  default = false
}

variable "good_response_model_name" {
  type = string
}

variable "good_response_model_description" {
  type = string
}

variable "good_response_model_schema_file_location" {
  type = string
}

variable "authorizer_id" {
  description = "The ID of the API Gateway authorizer to use for authentication"
  type        = string
  default     = ""
}
