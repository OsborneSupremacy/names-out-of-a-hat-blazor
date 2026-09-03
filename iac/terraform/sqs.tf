resource "aws_sqs_queue" "invitations-queue" {
  name                       = "giftexchange-invitations-queue"
  visibility_timeout_seconds = 300
  max_message_size           = 1048576

  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.invitations-dlq.arn
    # Lower than the delivery events queue's five, because the two are riding out different things.
    # There, a retry is free and a DSQL blip is worth waiting through. Here, every attempt that gets
    # as far as SES may have sent the mail before failing, so a message retried five times is a
    # participant told five times who they drew. Three is enough to cover a transient SES throttle
    # and few enough that a genuinely poisonous message stops early.
    maxReceiveCount = 3
  })
}

# The queue that was missing, and what its absence cost.
#
# Without a redrive policy a message that fails deterministically -- a malformed address SES will
# never accept, a participant row deleted between queueing and sending -- was retried until the
# retention period expired and then discarded. Nothing recorded that it had happened. The organizer
# saw the exchange as INVITATIONS_SENT, the delivery column showed "No confirmation yet" for that
# participant, and "no confirmation yet" is indistinguishable from mail that is merely slow. So the
# one failure mode the delivery column exists to make visible was the one it could not show.
#
# Nothing consumes this, for the same reason nothing consumes the delivery events DLQ: a message
# sitting here is a bug report with the original payload attached, and the alarm in
# cloudwatch-alarms.tf is what turns it into one that gets read.
resource "aws_sqs_queue" "invitations-dlq" {
  name = "giftexchange-invitations-dlq"

  # Two weeks, the maximum, matching the delivery events DLQ. The reasoning is stronger here: these
  # arrive during the sending season, which is exactly when nobody is at a desk.
  message_retention_seconds = 1209600
}
