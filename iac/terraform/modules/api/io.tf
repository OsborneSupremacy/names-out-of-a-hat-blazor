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

variable "request_validator_id" {
  description = <<-EOT
    Id of a request validator on this REST API, from the shared pair in
    api-gateway-request-validators.tf. Validators are per-API objects and are meant to be shared
    across methods: API Gateway caps how many an API may have, and this module used to create one
    per endpoint, which walked the API into that cap.
  EOT
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

variable "authorizer_type" {
  description = "COGNITO_USER_POOLS or CUSTOM. Ignored when authorizer_id is empty."
  type        = string
  default     = "COGNITO_USER_POOLS"
}
