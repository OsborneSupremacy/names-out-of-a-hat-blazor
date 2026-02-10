locals {
  common_environment_variables = {
    "TABLE_NAME" = aws_dynamodb_table.giftexchange.name,
    "INVITATIONS_QUEUE_URL" = aws_sqs_queue.invitations-queue.url,
    "CONTENT_MODERATION_THRESHOLD" = "0.5" // higher is _less_ sensitive
  }
  project_directory = "../../src/GiftExchange.Library"
  publish_zip_path  = "${local.project_directory}/bin/giftexchange_function.zip"
}
