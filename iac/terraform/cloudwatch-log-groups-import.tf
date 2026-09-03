# ---------------------------------------------------------------------------------------------
# TEMPORARY. Delete this file after the first successful apply.
#
# All six functions have already run in production, so Lambda has already created all six log
# groups. CreateLogGroup on a name that exists returns ResourceAlreadyExistsException, which means
# that without these blocks the apply that introduces cloudwatch-log-groups.tf fails outright --
# and fails partway, so the retry fails differently than the first attempt did.
#
# These adopt the existing groups instead. Once they are in state the blocks do nothing but cost a
# read on every plan, so they are in their own file to make removing them one deletion rather than
# six careful edits.
#
# The names are written out rather than taken from local.lambda_functions. An import block has to
# resolve entirely at plan time, and reading them from resource attributes makes that dependent on
# how Terraform orders the plan -- which is a subtlety worth nothing at all in a file whose whole
# life is one apply. If a name here ever disagrees with the function it belongs to, the import
# fails loudly at plan, which is the failure mode to want.
#
# If an apply reports "Cannot import non-existent remote object" for one of these, that function
# has never been invoked in this account. Remove that single import block, let Terraform create the
# group, and delete the rest of the file as normal.
# ---------------------------------------------------------------------------------------------

import {
  for_each = toset([
    "giftexchange",
    "giftexchange-authorizer",
    "giftexchange-invitation-queue-handler",
    "giftexchange-delivery-events-handler",
    "giftexchange-inbound-gift-ideas-handler",
    "giftexchange-cooled-off-scheduler-handler",
  ])

  to = aws_cloudwatch_log_group.lambda[each.key]
  id = "/aws/lambda/${each.key}"
}
