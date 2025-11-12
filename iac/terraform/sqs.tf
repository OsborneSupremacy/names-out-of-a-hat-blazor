resource "aws_sqs_queue" "invitations-queue" {
  name                       = "giftexchange-invitations-queue"
  visibility_timeout_seconds = 300
  max_message_size           = 1048576
}
