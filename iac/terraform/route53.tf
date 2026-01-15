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
