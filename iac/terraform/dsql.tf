resource "aws_dsql_cluster" "giftexchange_dsql_cluster" {
  deletion_protection_enabled = false
  force_destroy               = true
  tags = {
    Name = "giftexchange-dsql-cluster"
  }
}

resource "aws_route53_record" "giftexchange_dsql_cluster_record" {
  zone_id = data.aws_route53_zone.main.zone_id
  name    = "giftexchange-db.${data.aws_route53_zone.main.name}"
  type    = "CNAME"
  ttl     = "60"
  records = [
    "${aws_dsql_cluster.giftexchange_dsql_cluster.identifier}.dsql.${data.aws_region.current.region}.on.aws"
  ]
}

# Lets the application Lambda open a connection as a non-admin database role. The database
# side of this pairing lives in db/roles/giftexchange_user--0004.sql: dsql:DbConnect only
# permits connecting, and it is the AWS IAM GRANT inside the database that decides which
# database role this IAM role may connect as.
#
# The migration workflow is deliberately not covered here. It connects as admin under
# dsql:DbConnectAdmin, which this role does not have.
resource "aws_iam_role_policy" "giftexchange_app_dsql_policy" {
  name = "giftexchange-app-dsql-policy"
  role = aws_iam_role.giftexchange_app_exec_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect   = "Allow"
        Action   = ["dsql:DbConnect"]
        Resource = [aws_dsql_cluster.giftexchange_dsql_cluster.arn]
      }
    ]
  })
}
