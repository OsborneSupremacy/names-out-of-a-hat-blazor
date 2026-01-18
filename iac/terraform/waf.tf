# AWS-managed Web ACL created automatically by CloudFront free tier
# This Web ACL is created and managed by AWS CloudFront
data "aws_wafv2_web_acl" "cloudfront_managed" {
  name  = "CreatedByCloudFront-6bc475d2"
  scope = "CLOUDFRONT"
}

