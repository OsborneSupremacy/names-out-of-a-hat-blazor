# ACM Certificate for production API subdomain
resource "aws_acm_certificate" "api_prod" {
  domain_name       = "api.namesoutofahat.com"
  validation_method = "DNS"

  lifecycle {
    create_before_destroy = true
  }

  tags = {
    Name = "api.namesoutofahat.com"
  }
}

# DNS validation record for production certificate
resource "aws_route53_record" "api_prod_cert_validation" {
  for_each = {
    for dvo in aws_acm_certificate.api_prod.domain_validation_options : dvo.domain_name => {
      name   = dvo.resource_record_name
      record = dvo.resource_record_value
      type   = dvo.resource_record_type
    }
  }

  allow_overwrite = true
  name            = each.value.name
  records         = [each.value.record]
  ttl             = 60
  type            = each.value.type
  zone_id         = data.aws_route53_zone.main.zone_id
}

# Wait for production certificate validation to complete
resource "aws_acm_certificate_validation" "api_prod" {
  certificate_arn         = aws_acm_certificate.api_prod.arn
  validation_record_fqdns = [for record in aws_route53_record.api_prod_cert_validation : record.fqdn]
}

# ACM Certificate for stage API subdomain
resource "aws_acm_certificate" "api_stage" {
  domain_name       = "stage-api.namesoutofahat.com"
  validation_method = "DNS"

  lifecycle {
    create_before_destroy = true
  }

  tags = {
    Name = "stage-api.namesoutofahat.com"
  }
}

# DNS validation record for stage certificate
resource "aws_route53_record" "api_stage_cert_validation" {
  for_each = {
    for dvo in aws_acm_certificate.api_stage.domain_validation_options : dvo.domain_name => {
      name   = dvo.resource_record_name
      record = dvo.resource_record_value
      type   = dvo.resource_record_type
    }
  }

  allow_overwrite = true
  name            = each.value.name
  records         = [each.value.record]
  ttl             = 60
  type            = each.value.type
  zone_id         = data.aws_route53_zone.main.zone_id
}

# Wait for stage certificate validation to complete
resource "aws_acm_certificate_validation" "api_stage" {
  certificate_arn         = aws_acm_certificate.api_stage.arn
  validation_record_fqdns = [for record in aws_route53_record.api_stage_cert_validation : record.fqdn]
}

# ACM Certificate for frontend (root and www)
resource "aws_acm_certificate" "frontend" {
  domain_name               = "namesoutofahat.com"
  validation_method         = "DNS"
  subject_alternative_names = ["www.namesoutofahat.com"]

  lifecycle {
    create_before_destroy = true
  }

  tags = {
    Name = "namesoutofahat.com"
  }
}

# DNS validation records for frontend certificate
resource "aws_route53_record" "frontend_cert_validation" {
  for_each = {
    for dvo in aws_acm_certificate.frontend.domain_validation_options : dvo.domain_name => {
      name   = dvo.resource_record_name
      record = dvo.resource_record_value
      type   = dvo.resource_record_type
    }
  }

  allow_overwrite = true
  name            = each.value.name
  records         = [each.value.record]
  ttl             = 60
  type            = each.value.type
  zone_id         = data.aws_route53_zone.main.zone_id
}

# Wait for frontend certificate validation to complete
resource "aws_acm_certificate_validation" "frontend" {
  certificate_arn         = aws_acm_certificate.frontend.arn
  validation_record_fqdns = [for record in aws_route53_record.frontend_cert_validation : record.fqdn]
}
