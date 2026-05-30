# CloudFront distribution for API Gateway (api.namesoutofahat.com)
resource "aws_cloudfront_distribution" "api" {
  enabled         = true
  is_ipv6_enabled = true
  comment         = "CloudFront distribution for Gift Exchange API Gateway"
  price_class     = "PriceClass_All"
  aliases         = ["api.namesoutofahat.com"]

  web_acl_id = "arn:aws:wafv2:us-east-1:182571449491:global/webacl/CreatedByCloudFront-5775ad2d/2efbc868-19d6-4baa-a8f5-d99fb6bda060"

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
    cache_policy_id = "4135ea2d-6df8-44a3-9df3-4b5a84be39ad"

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

  depends_on = [
    aws_acm_certificate_validation.api_prod,
    aws_api_gateway_domain_name.api_prod
  ]
}
