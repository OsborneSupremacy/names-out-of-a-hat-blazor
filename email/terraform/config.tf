provider "aws" {
  region = "us-east-1"

  default_tags {
    tags = {
      Environment = "live"
      Application = "giftexchange"
      Role        = "email"
      ManagedBy   = "terraform"
      Owner       = "giftexchange@osbornesupremacy.com"
    }
  }
}

terraform {
  required_providers {
    aws = {
      source = "hashicorp/aws"
    }
  }

  backend "s3" {
    bucket       = "bro-tfstate"
    use_lockfile = true
    key          = "giftexchange/email"
    region       = "us-east-1"
  }
}
