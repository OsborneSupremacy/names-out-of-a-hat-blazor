locals {
  cooled_off_scheduler_group_name = "giftexchange-cooled-off"

  common_environment_variables = {
    "TABLE_NAME"                    = aws_dynamodb_table.giftexchange.name,
    "INVITATIONS_QUEUE_URL"         = aws_sqs_queue.invitations-queue.url,
    "CONTENT_MODERATION_THRESHOLD"  = "0.5" // higher is _less_ sensitive
    "SESSION_SIGNING_KEY_PARAMETER" = aws_ssm_parameter.session_signing_key.name
    # Whether mail goes to the address it is addressed to, or is diverted to the test recipient
    # with " - TEST MODE" appended to the subject.
    #
    # Common rather than per-function, because it was per-function and that went wrong: three
    # services read it, two of the functions carrying them set it, and the one that did not was the
    # router — so magic link sign-in emails went out marked TEST MODE while invitations from the
    # queue handler went out correctly. Nothing failed and nothing logged an error; the flag simply
    # read false wherever it had not been set. Anything that has to be remembered separately for
    # each function will eventually be forgotten for one of them.
    "LIVE_MODE" = "true"
    # The cluster endpoint, not the CNAME. DSQL's certificate covers *.dsql.<region>.on.aws,
    # and the connector always uses verify-full, so connecting via giftexchange-db.namesoutofahat.com
    # would fail hostname verification.
    "DSQL_ENDPOINT" = "${aws_dsql_cluster.giftexchange_dsql_cluster.identifier}.dsql.${data.aws_region.current.region}.on.aws"
  }
  publish_zip_path = "../../src/GiftExchange.Library/bin/GiftExchange.Library.zip"
}
