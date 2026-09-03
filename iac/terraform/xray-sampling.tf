# ---------------------------------------------------------------------------------------------
# Sampling, and the group X-Ray Insights watches.
#
# These two are separate from xray.tf, which is about permission and which functions emit traces
# at all. This is about which traces survive and what looks at them afterwards.
# ---------------------------------------------------------------------------------------------

# Trace everything.
#
# The default rule is a reservoir of one request per second plus five percent of whatever exceeds
# it. At this application's normal volume that already records essentially every request, so on a
# quiet week this rule changes nothing at all -- which is the point worth being clear about, because
# it means the reason for it is entirely about the week that is not quiet.
#
# Past one request per second the default keeps five percent, so the single slow request worth
# looking at has a one-in-twenty chance of having been recorded. A launch is precisely the event
# that pushes past that threshold and precisely the event where the unusual request is the one you
# need. Sampling down is a cost control, and there is no cost here to control: the first 100,000
# traces recorded each month are free and this will not approach that even at its busiest.
#
# Priority below the default rule's 10000 would never be consulted; rules are evaluated lowest
# priority number first and the default matches everything.
resource "aws_xray_sampling_rule" "trace_everything" {
  rule_name = "giftexchange-trace-everything"
  priority  = 1000

  # No reservoir. reservoir_size guarantees a number of traces per second before the rate applies,
  # which matters when the rate is a fraction. At a rate of 1 it would be reserving a share of
  # something already being taken in full.
  reservoir_size = 0
  fixed_rate     = 1.0

  # Everything in this account, rather than naming the six functions. A rule listing services is
  # one more place a new function has to be remembered, and forgetting it here fails the way the
  # LIVE_MODE flag in locals.tf failed -- silently, and only for the thing that was forgotten.
  service_name = "*"
  service_type = "*"
  host         = "*"
  http_method  = "*"
  url_path     = "*"
  resource_arn = "*"
  version      = 1
}

# The set of traces Insights reasons over.
#
# Insights builds a baseline of what normal looks like and reports departures from it -- a fault
# rate stepping up, a latency distribution shifting -- which is the one form of monitoring here
# that does not require somebody to have predicted the failure in advance. Every alarm in
# cloudwatch-alarms.tf watches a line that had to be chosen; this watches for the shape of things
# changing.
#
# That makes it most useful in exactly the period this was all built for: the weeks after a launch,
# when nobody knows yet what this application's normal is, including the person who wrote it.
resource "aws_xray_group" "giftexchange" {
  group_name = "giftexchange"

  # Every trace with a fault, an error or a throttle, rather than all traces. Insights is looking
  # for anomalies in failure, and a baseline computed over overwhelmingly successful traffic is a
  # baseline of the successes.
  filter_expression = "fault OR error OR throttle"

  insights_configuration {
    insights_enabled = true

    # Emits to EventBridge, not to an inbox. X-Ray publishes an "AWS X-Ray Insight Update" event
    # under source aws.xray, and reaching the alarms topic from there needs an EventBridge rule and
    # a topic policy granting events.amazonaws.com sns:Publish -- neither of which is here, because
    # writing an aws_sns_topic_policy replaces the default one that the alarms in
    # cloudwatch-alarms.tf currently rely on, and that is not a change worth making untested days
    # before the traffic arrives.
    #
    # So this is on, and an insight is visible in the X-Ray console and queryable, but nobody is
    # told. That is a deliberate half-measure rather than an oversight; the other half is a small
    # follow-up, done on a day when breaking the alarm path costs nothing.
    notifications_enabled = true
  }
}
