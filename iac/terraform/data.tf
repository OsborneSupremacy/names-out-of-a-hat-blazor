data "aws_caller_identity" "current" {}

data "http" "ipify" {
  url = "https://api.ipify.org"
}

data "aws_region" "current" {}

# Remote state for SES/email configuration
data "terraform_remote_state" "email" {
  backend = "s3"

  config = {
    bucket  = "bro-tfstate"
    key     = "giftexchange/email"
    region  = "us-east-1"
    profile = "benosborne"
  }
}