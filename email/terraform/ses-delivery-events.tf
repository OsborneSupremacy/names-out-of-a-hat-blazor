# What SES reports back about the mail this application sends.
#
# Until this existed the application sent invitations and heard nothing. SES accepts a message
# addressed to a typo, returns a message id, and publishes the bounce minutes later on a channel
# nobody was subscribed to -- so a delivered invitation and one that came back looked exactly alike,
# and a participant with a mistyped address was simply never told whose name they drew.
#
# Here rather than in iac/terraform, alongside DKIM and the MAIL FROM domain, because a
# configuration set is a property of how this domain sends mail. The application state consumes the
# two outputs at the bottom, the same way it already consumes the receipt rule set name.
#
# Nothing here turns on open or click tracking, deliberately. Both work by rewriting the message --
# a pixel for opens, a redirect through an SES domain for clicks -- and neither reports what it
# appears to: Apple Mail pre-fetches images for every recipient at delivery, plenty of clients block
# them outright, and the corporate gateways that render mail before delivering it would fetch both.

resource "aws_sesv2_configuration_set" "outbound" {
  configuration_set_name = "giftexchange-outbound"

  # Per-configuration-set bounce and complaint rates in CloudWatch. The reason to want them is that
  # this domain's reputation is shared by every exchange: one organizer typing addresses badly is
  # everybody else's deliverability, and a rate is the only view that shows that building.
  reputation_options {
    reputation_metrics_enabled = true
  }

  sending_options {
    sending_enabled = true
  }
}

resource "aws_sns_topic" "delivery_events" {
  name         = "sns-topic-namesoutofahat-delivery-events"
  display_name = "Names Out of a Hat Delivery Events"
}

# Same shape as the policy on the inbox topic: SES may publish, and only when the call is made on
# behalf of this account.
resource "aws_sns_topic_policy" "delivery_events" {
  arn = aws_sns_topic.delivery_events.arn

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Service = "ses.amazonaws.com"
        }
        Action   = "SNS:Publish"
        Resource = aws_sns_topic.delivery_events.arn
        Condition = {
          StringEquals = {
            "aws:SourceAccount" = data.aws_caller_identity.current.account_id
          }
        }
      }
    ]
  })
}

# The v2 resources rather than aws_ses_event_destination, for one reason: DELIVERY_DELAY exists only
# here. A message being retried is neither delivered nor bounced, and without that event a temporary
# failure looks identical to silence for as long as the retries last.
#
# SEND is included so this feed carries the whole lifecycle. That is what lets the sending function
# stay out of the database entirely -- there is no row for it to write, because SES announces the
# send itself, and one writer cannot race itself.
resource "aws_sesv2_configuration_set_event_destination" "delivery_events" {
  configuration_set_name = aws_sesv2_configuration_set.outbound.configuration_set_name
  event_destination_name = "giftexchange-delivery-events"

  event_destination {
    enabled = true

    matching_event_types = [
      "SEND",
      "DELIVERY",
      "DELIVERY_DELAY",
      "BOUNCE",
      "COMPLAINT",
      "REJECT",
      "RENDERING_FAILURE"
    ]

    sns_destination {
      topic_arn = aws_sns_topic.delivery_events.arn
    }
  }
}
