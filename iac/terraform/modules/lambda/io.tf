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
  type = string
}

variable "gateway_method_request_parameters" {
  description = "Request parameters for the API Gateway method"
  type        = map(string)
  default     = {}
}

variable gateway_method_request_model_schema_file_location {
  description = "Path to the file containing the request model schema within the local filesystem."
  type = string
}

variable gateway_method_request_model_name {
  description = "The name of the request model"
  type = string
}

variable gateway_method_request_model_description {
  description = "The description of the request model"
  type = string
}

variable "function_name" {
  type = string
}

variable "function_net_class" {
  description = "The name of the .NET class that contains the handler function (FunctionHandler is the assumed method name)"
  type = string
}

variable "function_description" {
  type = string
}

variable "function_memory_size" {
  type = number
}

variable "function_timeout" {
  type = number
  default = 30
}

variable "common_tags" {
  type = map(string)
}

variable "environment_variables" {
  type = map(string)
}

variable "include_404_response" {
  type = bool
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

variable "deployment_package_filename" {
  description = "Path to the function's deployment package within the local filesystem. "
  type = string
}

variable "deployment_package_source_code_hash" {
  description = "Base64-encoded representation of the SHA256 hash of the deployment package."
  type = string
}

# outputs

output "function_exec_role" {
  value = aws_iam_role.exec-role
}

output "lambda_function_name" {
  value = aws_lambda_function.lambda.function_name
}

output "lambda_function_arn" {
  value = aws_lambda_function.lambda.arn
}

output "lambda_function_invoke_arn" {
  value = aws_lambda_function.lambda.invoke_arn
}