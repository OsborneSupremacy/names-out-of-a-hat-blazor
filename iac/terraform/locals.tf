locals {
  common_environment_variables = {
    "TABLE_NAME" = aws_dynamodb_table.giftexchange.name,
    "INVITATIONS_QUEUE_URL" = aws_sqs_queue.invitations-queue.url
  }
  project_directory = "../../src/GiftExchange.Library"
  publish_zip_path  = "${local.project_directory}/bin/giftexchange_function.zip"
}
