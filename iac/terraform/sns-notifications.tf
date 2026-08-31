# ---------------------------------------------------------------------------------------------
# The two notification topics, and why there are two.
#
# Sharing one was considered and rejected. Nothing is saved by it: a topic costs nothing to exist,
# and both publishes and deliveries are billed the same whichever topic they went through. What
# sharing would cost is the ability to route them differently — subscriptions are per-topic, so the
# first time alarms need to reach a pager, an SMS, or a Lambda that acts on them, that subscriber
# starts receiving contact form messages as well. A filter policy can undo it, which is paying in
# configuration for what a second topic gives free.
#
# The other half is access. Feedback is published by the application Lambda, which is the process
# handling requests from anybody with an account. Sharing means that role holds sns:Publish on the
# channel alarms arrive over, so a bug in a request handler is a bug that can fabricate an alarm.
# Alarms are published by CloudWatch, which needs no role here at all.
# ---------------------------------------------------------------------------------------------

# Nothing publishes to this yet. It is created ahead of the alarms themselves so that adding one is
# a single resource with an alarm_actions line, rather than an alarm plus a topic plus waiting on a
# subscription confirmation before it can tell anybody anything.
resource "aws_sns_topic" "alarms" {
  name         = "giftexchange-alarms"
  display_name = "Names Out Of A Hat alarms"
}

# The display name is the From name on email deliveries, which is the only kind of subscription
# either topic has. Spelled out rather than abbreviated: an SMS sender ID is capped around ten
# characters, but nothing subscribes by SMS, and compressing the name to fit a channel that does
# not exist only makes the inbox that does exist harder to read.
resource "aws_sns_topic" "feedback" {
  name         = "giftexchange-feedback"
  display_name = "Names Out Of A Hat feedback"
}

locals {
  # Where both topics are delivered. One address, because it is one person's inbox; the split
  # above is about being able to change that for one topic without changing it for both.
  notification_email = "osborne.ben@gmail.com"
}

# Email subscriptions cannot be confirmed by Terraform. Both of these are created in
# "pending confirmation" and deliver nothing until the link in the confirmation mail is clicked --
# an apply that reports success is not the same as a topic that can reach anybody. AWS drops an
# unconfirmed subscription after three days, so a confirmation missed over a weekend means
# re-subscribing rather than waiting.
resource "aws_sns_topic_subscription" "alarms_email" {
  topic_arn = aws_sns_topic.alarms.arn
  protocol  = "email"
  endpoint  = local.notification_email
}

resource "aws_sns_topic_subscription" "feedback_email" {
  topic_arn = aws_sns_topic.feedback.arn
  protocol  = "email"
  endpoint  = local.notification_email
}

# Publish and nothing else. The application never lists, subscribes to, or reads this topic, and
# it is the only topic it may publish to at all -- naming the ARN rather than "*" is what keeps
# the alarms topic out of reach of the process handling public requests.
resource "aws_iam_role_policy" "giftexchange_app_sns_policy" {
  name = "giftexchange-app-sns-policy"
  role = aws_iam_role.giftexchange_app_exec_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect   = "Allow"
        Action   = ["sns:Publish"]
        Resource = [aws_sns_topic.feedback.arn]
      }
    ]
  })
}
