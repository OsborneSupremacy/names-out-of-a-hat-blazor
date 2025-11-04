resource "aws_iam_role" "exec-role" {
  name = "${var.function_name}-exec-role"

  tags = merge(
    var.common_tags,
    {
      Name = "${var.function_name}-exec-role"
    }
  )

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Principal = {
          Service = "lambda.amazonaws.com"
        }
        Effect = "Allow"
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "exec-role-attachment-lambda-basic-execution" {
  role       = aws_iam_role.exec-role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}