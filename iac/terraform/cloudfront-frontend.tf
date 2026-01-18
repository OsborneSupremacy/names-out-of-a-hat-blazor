# CloudFront Origin Access Control for S3
resource "aws_cloudfront_origin_access_control" "frontend" {
  name                              = "frontend-oac"
  description                       = "Origin Access Control for frontend S3 bucket"
  origin_access_control_origin_type = "s3"
  signing_behavior                  = "always"
  signing_protocol                  = "sigv4"
}

# CloudFront distribution for root domain (namesoutofahat.com)
resource "aws_cloudfront_distribution" "frontend" {
  enabled             = true
  is_ipv6_enabled     = true
  default_root_object = "index.html"
  price_class         = "PriceClass_100"
  aliases             = ["namesoutofahat.com"]

  origin {
    domain_name              = aws_s3_bucket.frontend.bucket_regional_domain_name
    origin_id                = "S3-${aws_s3_bucket.frontend.id}"
    origin_access_control_id = aws_cloudfront_origin_access_control.frontend.id
  }

  default_cache_behavior {
    allowed_methods        = ["GET", "HEAD", "OPTIONS"]
    cached_methods         = ["GET", "HEAD"]
    target_origin_id       = "S3-${aws_s3_bucket.frontend.id}"
    viewer_protocol_policy = "redirect-to-https"
    compress               = true

    forwarded_values {
      query_string = false

      cookies {
        forward = "none"
      }
    }

    min_ttl     = 0
    default_ttl = 3600
    max_ttl     = 86400
  }

  # Custom error response for SPA routing
  custom_error_response {
    error_code         = 404
    response_code      = 200
    response_page_path = "/index.html"
  }

  custom_error_response {
    error_code         = 403
    response_code      = 200
    response_page_path = "/index.html"
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    acm_certificate_arn      = aws_acm_certificate.frontend.arn
    ssl_support_method       = "sni-only"
    minimum_protocol_version = "TLSv1.2_2021"
  }

  tags = {
    Name = "frontend-distribution"
  }

  depends_on = [aws_acm_certificate_validation.frontend]
}

# S3 bucket for www redirect
resource "aws_s3_bucket" "frontend_redirect" {
  bucket = "bro-namesoutofahat-frontend-redirect"
}

# Configure www bucket to redirect to root domain
resource "aws_s3_bucket_website_configuration" "frontend_redirect" {
  bucket = aws_s3_bucket.frontend_redirect.id

  redirect_all_requests_to {
    host_name = "namesoutofahat.com"
    protocol  = "https"
  }
}

# Block public access for redirect bucket
resource "aws_s3_bucket_public_access_block" "frontend_redirect" {
  bucket = aws_s3_bucket.frontend_redirect.id

  block_public_acls       = true
  block_public_policy     = false
  ignore_public_acls      = true
  restrict_public_buckets = false
}

# S3 bucket policy for redirect bucket - CloudFront access
resource "aws_s3_bucket_policy" "frontend_redirect" {
  bucket = aws_s3_bucket.frontend_redirect.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "AllowCloudFrontServicePrincipal"
        Effect = "Allow"
        Principal = {
          Service = "cloudfront.amazonaws.com"
        }
        Action   = "s3:GetObject"
        Resource = "${aws_s3_bucket.frontend_redirect.arn}/*"
        Condition = {
          StringEquals = {
            "AWS:SourceArn" = aws_cloudfront_distribution.frontend_redirect.arn
          }
        }
      }
    ]
  })

  depends_on = [aws_s3_bucket_public_access_block.frontend_redirect]
}

# CloudFront Origin Access Control for redirect bucket
resource "aws_cloudfront_origin_access_control" "frontend_redirect" {
  name                              = "frontend-redirect-oac"
  description                       = "Origin Access Control for frontend redirect S3 bucket"
  origin_access_control_origin_type = "s3"
  signing_behavior                  = "always"
  signing_protocol                  = "sigv4"
}

# CloudFront distribution for www (redirects to root)
resource "aws_cloudfront_distribution" "frontend_redirect" {
  enabled         = true
  is_ipv6_enabled = true
  price_class     = "PriceClass_100"
  aliases         = ["www.namesoutofahat.com"]

  origin {
    domain_name = aws_s3_bucket_website_configuration.frontend_redirect.website_endpoint
    origin_id   = "S3-${aws_s3_bucket.frontend_redirect.id}"

    custom_origin_config {
      http_port              = 80
      https_port             = 443
      origin_protocol_policy = "http-only"
      origin_ssl_protocols   = ["TLSv1.2"]
    }
  }

  default_cache_behavior {
    allowed_methods        = ["GET", "HEAD"]
    cached_methods         = ["GET", "HEAD"]
    target_origin_id       = "S3-${aws_s3_bucket.frontend_redirect.id}"
    viewer_protocol_policy = "redirect-to-https"
    compress               = true

    forwarded_values {
      query_string = false

      cookies {
        forward = "none"
      }
    }

    min_ttl     = 0
    default_ttl = 86400
    max_ttl     = 31536000
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    acm_certificate_arn      = aws_acm_certificate.frontend.arn
    ssl_support_method       = "sni-only"
    minimum_protocol_version = "TLSv1.2_2021"
  }

  tags = {
    Name = "frontend-redirect-distribution"
  }

  depends_on = [aws_acm_certificate_validation.frontend]
}
