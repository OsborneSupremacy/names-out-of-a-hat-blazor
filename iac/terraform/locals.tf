locals {
  cooled_off_scheduler_group_name = "giftexchange-cooled-off"

  common_environment_variables = {
    "TABLE_NAME" = aws_dynamodb_table.giftexchange.name,
    "INVITATIONS_QUEUE_URL" = aws_sqs_queue.invitations-queue.url,
    "CONTENT_MODERATION_THRESHOLD" = "0.5" // higher is _less_ sensitive
    "SESSION_SIGNING_KEY_PARAMETER" = aws_ssm_parameter.session_signing_key.name
  }
  publish_zip_path  = "../../src/GiftExchange.Library/bin/GiftExchange.Library.zip"
}
