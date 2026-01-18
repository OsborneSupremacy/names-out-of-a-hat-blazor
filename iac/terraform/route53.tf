# Data source to reference the existing hosted zone for namesoutofahat.com
data "aws_route53_zone" "main" {
  name         = "namesoutofahat.com"
  private_zone = false
}

# Route53 record for the production API subdomain
resource "aws_route53_record" "api_prod" {
  zone_id = data.aws_route53_zone.main.zone_id
  name    = "api.namesoutofahat.com"
  type    = "A"

  alias {
    name                   = aws_api_gateway_domain_name.api_prod.regional_domain_name
    zone_id                = aws_api_gateway_domain_name.api_prod.regional_zone_id
    evaluate_target_health = true
  }
}

# Route53 record for the stage API subdomain
resource "aws_route53_record" "api_stage" {
  zone_id = data.aws_route53_zone.main.zone_id
  name    = "stage-api.namesoutofahat.com"
  type    = "A"

  alias {
    name                   = aws_api_gateway_domain_name.api_stage.regional_domain_name
    zone_id                = aws_api_gateway_domain_name.api_stage.regional_zone_id
    evaluate_target_health = true
  }
}

# Route53 record for root domain (namesoutofahat.com) -> CloudFront
resource "aws_route53_record" "frontend_root" {
  zone_id = data.aws_route53_zone.main.zone_id
  name    = "namesoutofahat.com"
  type    = "A"

  alias {
    name                   = aws_cloudfront_distribution.frontend.domain_name
    zone_id                = aws_cloudfront_distribution.frontend.hosted_zone_id
    evaluate_target_health = false
  }
}

# Route53 record for www subdomain -> same CloudFront distribution
resource "aws_route53_record" "frontend_www" {
  zone_id = data.aws_route53_zone.main.zone_id
  name    = "www.namesoutofahat.com"
  type    = "A"

  alias {
    name                   = aws_cloudfront_distribution.frontend.domain_name
    zone_id                = aws_cloudfront_distribution.frontend.hosted_zone_id
    evaluate_target_health = false
  }
}
