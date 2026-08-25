resource "aws_dsql_cluster" "giftexchange_dsql_cluster" {
  deletion_protection_enabled = false
  force_destroy               = true
  tags = {
    Name = "giftexchange-dsql-cluster"
  }
}

resource "aws_route53_record" "giftexchange_dsql_cluster_record" {
  zone_id = data.aws_route53_zone.main.zone_id
  name    = "giftexchange-db.${data.aws_route53_zone.main}"
  type    = "CNAME"
  ttl     = "60"
  records = [
    "${aws_dsql_cluster.giftexchange_dsql_cluster.identifier}.dsql.${data.aws_region.current.region}.on.aws"
  ]
}
