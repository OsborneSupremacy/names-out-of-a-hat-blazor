# CloudFront distribution for API Gateway (api.namesoutofahat.com)
resource "aws_cloudfront_distribution" "api" {
  enabled         = true
  is_ipv6_enabled = true
  comment         = "CloudFront distribution for API Gateway"
  price_class     = "PriceClass_100" # US, Canada, Europe
  aliases         = ["api.namesoutofahat.com"]

  origin {
    domain_name = aws_api_gateway_domain_name.api_prod.regional_domain_name
    origin_id   = "APIGateway-${aws_api_gateway_rest_api.giftexchange-gateway.id}"

    custom_origin_config {
      http_port                = 80
      https_port               = 443
      origin_protocol_policy   = "https-only"
      origin_ssl_protocols     = ["TLSv1.2"]
      origin_keepalive_timeout = 5
      origin_read_timeout      = 30
    }

    custom_header {
      name  = "X-Forwarded-Host"
      value = "api.namesoutofahat.com"
    }
  }

  default_cache_behavior {
    allowed_methods  = ["DELETE", "GET", "HEAD", "OPTIONS", "PATCH", "POST", "PUT"]
    cached_methods   = ["GET", "HEAD"]
    target_origin_id = "APIGateway-${aws_api_gateway_rest_api.giftexchange-gateway.id}"

    viewer_protocol_policy = "redirect-to-https"
    compress               = true

    # CachingDisabled - all requests pass through to API Gateway
    cache_policy_id = "4cc15a8a-d715-48a4-ac15-33158f6c6df9"

    # AllViewerExceptHostHeader - forward all headers except Host, plus query strings and cookies
    origin_request_policy_id = "b689b0a8-53d0-40ab-baf2-68738e2966ac"
  }

  restrictions {
    geo_restriction {
      restriction_type = "whitelist"
      locations        = ["US", "CA"]
    }
  }

  viewer_certificate {
    acm_certificate_arn      = aws_acm_certificate.api_prod.arn
    ssl_support_method       = "sni-only"
    minimum_protocol_version = "TLSv1.2_2021"
  }

  tags = {
    Name = "api-distribution"
  }

  web_acl_id = data.aws_wafv2_web_acl.cloudfront_managed.arn

  depends_on = [
    aws_acm_certificate_validation.api_prod,
    aws_api_gateway_domain_name.api_prod
  ]
}
