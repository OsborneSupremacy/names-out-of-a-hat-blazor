resource "aws_lambda_function" "lambda" {

  function_name    = var.function_name
  description      = var.function_description
  handler          = "Villains.Library::Villains.Library.Lambda.${var.function_net_class}::FunctionHandler"
  runtime          = "dotnet8"
  architectures    = ["arm64"]
  memory_size      = var.function_memory_size
  timeout          = var.function_timeout
  filename         = var.deployment_package_filename
  source_code_hash = var.deployment_package_source_code_hash
  role             = aws_iam_role.exec-role.arn
  environment {
    variables = var.environment_variables
  }

  tags = merge(
    var.common_tags,
    {
      Name = var.function_name
    }
  )
}