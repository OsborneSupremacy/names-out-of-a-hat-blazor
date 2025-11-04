data "aws_caller_identity" "current" {}

data "http" "ipify" {
  url = "https://api.ipify.org"
}