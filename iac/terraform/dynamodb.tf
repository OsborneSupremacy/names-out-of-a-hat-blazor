resource "aws_dynamodb_table" "giftexchange_dynamo_table" {
  name         = "giftexchange"
  billing_mode = "PAY_PER_REQUEST"
  hash_key = "id"
}