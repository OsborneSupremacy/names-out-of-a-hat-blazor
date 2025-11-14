module "lambda-send-invitations-hat" {
  source                                            = "./modules/lambda"
  environment_variables                             = local.common_environment_variables
  gateway_rest_api_id                               = aws_api_gateway_rest_api.giftexchange-gateway.id
  gateway_resource_id                               = aws_api_gateway_resource.hat-send-invitations-resource.id
  gateway_http_method                               = "POST"
  gateway_http_operation_name                       = "SendInvitations"
  gateway_method_request_parameters                 = {}
  gateway_method_request_model_name                 = "SendInvitationsRequest"
  gateway_method_request_model_description          = "A request to send invitations to the gift exchange's participants."
  gateway_method_request_model_schema_file_location = "../../src/GiftExchange.Library/Schemas/ValidateHatRequest.schema.json"
  include_404_response                              = true
  good_response_model_name                          = ""
  good_response_model_description                   = ""
  good_response_model_schema_file_location          = ""
  function_description                              = "Composes emails and enqueues in SQS"
  function_memory_size                              = 128
  function_name                                     = "giftexchange-send-invitations"
  function_net_class                                = "SendInvitations"
  deployment_package_filename                       = data.archive_file.lambda_function.output_path
  deployment_package_source_code_hash               = data.archive_file.lambda_function.output_base64sha256
  dynamodb_table_arn                                = aws_dynamodb_table.giftexchange.arn
}

resource "aws_iam_role_policy" "lambda-send-invitations-hat-sqs-policy" {
  name = "${module.lambda-send-invitations-hat.lambda_function_name}-sqs-policy"
  role = module.lambda-send-invitations-hat.function_exec_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "sqs:SendMessage",
          "sqs:GetQueueUrl"
        ]
        Resource = [
          aws_sqs_queue.invitations-queue.arn
        ]
      }
    ]
  })
}
