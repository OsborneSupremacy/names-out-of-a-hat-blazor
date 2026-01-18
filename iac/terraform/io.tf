
output "cognito_user_pool_id" {
  description = "The ID of the Cognito User Pool"
  value       = aws_cognito_user_pool.namesoutofahat.id
}

output "cognito_user_pool_arn" {
  description = "The ARN of the Cognito User Pool"
  value       = aws_cognito_user_pool.namesoutofahat.arn
}

output "cognito_user_pool_endpoint" {
  description = "The endpoint of the Cognito User Pool"
  value       = aws_cognito_user_pool.namesoutofahat.endpoint
}

output "cognito_spa_client_id" {
  description = "The ID of the SPA Cognito User Pool Client"
  value       = aws_cognito_user_pool_client.spa_client.id
}

output "cognito_user_pool_domain" {
  description = "The Cognito User Pool domain"
  value       = aws_cognito_user_pool_domain.namesoutofahat.domain
}

output "cognito_hosted_ui_url" {
  description = "The Cognito Hosted UI URL"
  value       = "https://${aws_cognito_user_pool_domain.namesoutofahat.domain}.auth.${data.aws_region.current.region}.amazoncognito.com"
}

output "api_gateway_url" {
  description = "The API Gateway URL"
  value       = "${aws_api_gateway_stage.live-stage.invoke_url}"
}

output "api_gateway_authorizer_id" {
  description = "The ID of the Cognito authorizer for the API Gateway"
  value       = aws_api_gateway_authorizer.cognito.id
}

output "frontend_bucket_name" {
  description = "The name of the frontend S3 bucket"
  value       = aws_s3_bucket.frontend.id
}

output "frontend_bucket_website_endpoint" {
  description = "The website endpoint of the frontend S3 bucket"
  value       = aws_s3_bucket_website_configuration.frontend.website_endpoint
}

output "frontend_bucket_website_url" {
  description = "The full URL of the frontend website"
  value       = "http://${aws_s3_bucket_website_configuration.frontend.website_endpoint}"
}
