# Custom domain name for production API
resource "aws_api_gateway_domain_name" "api_prod" {
  domain_name              = "api.namesoutofahat.com"
  regional_certificate_arn = aws_acm_certificate_validation.api_prod.certificate_arn

  endpoint_configuration {
    types = ["REGIONAL"]
  }

  depends_on = [aws_acm_certificate_validation.api_prod]
}

# Base path mapping for production API
resource "aws_api_gateway_base_path_mapping" "api_prod" {
  api_id      = aws_api_gateway_rest_api.giftexchange-gateway.id
  stage_name  = aws_api_gateway_stage.live-stage.stage_name
  domain_name = aws_api_gateway_domain_name.api_prod.domain_name
}

# Custom domain name for stage API
resource "aws_api_gateway_domain_name" "api_stage" {
  domain_name              = "stage-api.namesoutofahat.com"
  regional_certificate_arn = aws_acm_certificate_validation.api_stage.certificate_arn

  endpoint_configuration {
    types = ["REGIONAL"]
  }

  depends_on = [aws_acm_certificate_validation.api_stage]
}

# Base path mapping for stage API (commented out until stage is created)
# resource "aws_api_gateway_base_path_mapping" "api_stage" {
#   api_id      = aws_api_gateway_rest_api.giftexchange-gateway.id
#   stage_name  = aws_api_gateway_stage.stage-stage.stage_name
#   domain_name = aws_api_gateway_domain_name.api_stage.domain_name
# }
