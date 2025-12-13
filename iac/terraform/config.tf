provider "aws" {
  region  = "us-east-1"
  profile = "benosborne"

  default_tags {
    tags = {
      Environment = "live"
      Application = "giftexchange"
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
    profile      = "benosborne"
    use_lockfile = true
    key          = "giftexchange/live"
    region       = "us-east-1"
  }
}
