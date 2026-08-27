data "aws_caller_identity" "current" {}

data "aws_route53_zone" "main" {
  name         = "namesoutofahat.com"
  private_zone = false
}
