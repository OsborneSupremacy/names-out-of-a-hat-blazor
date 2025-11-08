resource "aws_dynamodb_table" "giftexchange" {
  name         = "giftexchange"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "OrganizerEmail"
  range_key    = "HatId"

  attribute {
    name = "OrganizerEmail"
    type = "S"
  }

  attribute {
    name = "HatId"
    type = "S"
  }
}
