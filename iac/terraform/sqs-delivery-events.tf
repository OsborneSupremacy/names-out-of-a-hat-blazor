# The queue SES delivery events arrive on, and the subscription that fills it.
#
# SES publishes to SNS and cannot publish to SQS, so the hop through a topic is not a choice. What
# is a choice is putting a queue after it rather than subscribing the function directly, and the
# reason is what the function does: it writes to DSQL. SNS invokes a Lambda asynchronously, retries
# a handful of times and then drops the event, and a delivery status dropped during a DSQL blip is
# gone for good -- SES will not republish it. A message on a queue is still there to be tried again.

resource "aws_sqs_queue" "delivery-events-queue" {
  name = "giftexchange-delivery-events-queue"

  # Six times the function timeout, which is what AWS recommends and what keeps a slow cold start
  # from making a second consumer pick up a message the first is still working on.
  visibility_timeout_seconds = 360

  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.delivery-events-dlq.arn
    # A DSQL outage is worth riding out, and every one of these retries is free. What this number is
    # really sized for is the other case: an event this application cannot parse, which will fail
    # identically every time and should stop being retried before it costs anything.
    maxReceiveCount = 5
  })
}

# Nothing consumes this. It exists so that an event which can never be processed stops being
# redelivered and starts being visible -- a message sitting here is a bug report with the original
# payload attached.
resource "aws_sqs_queue" "delivery-events-dlq" {
  name = "giftexchange-delivery-events-dlq"

  # Two weeks, the maximum. These arrive without anybody watching, so the retention has to outlast
  # not noticing over a holiday -- which, for this application, is when all the mail is sent.
  message_retention_seconds = 1209600
}

# SNS may write to the queue, and only from this topic. Without this the subscription is created
# successfully and silently delivers nothing.
resource "aws_sqs_queue_policy" "delivery-events-queue" {
  queue_url = aws_sqs_queue.delivery-events-queue.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Service = "sns.amazonaws.com"
        }
        Action   = "sqs:SendMessage"
        Resource = aws_sqs_queue.delivery-events-queue.arn
        Condition = {
          ArnEquals = {
            "aws:SourceArn" = local.delivery_events_topic_arn
          }
        }
      }
    ]
  })
}

# Raw message delivery, so the body is the SES event itself rather than an SNS envelope carrying it
# as a JSON string that would have to be unwrapped and parsed a second time. DeliveryEventsService
# deserializes the body directly and says so.
resource "aws_sns_topic_subscription" "delivery-events" {
  topic_arn            = local.delivery_events_topic_arn
  protocol             = "sqs"
  endpoint             = aws_sqs_queue.delivery-events-queue.arn
  raw_message_delivery = true

  depends_on = [aws_sqs_queue_policy.delivery-events-queue]
}
