# Build and package creation should be done outside of Terraform
# Expected zip file location: ../../src/GiftExchange.Library/bin/giftexchange_function.zip

data "local_file" "lambda_function" {
  filename = local.publish_zip_path
}
