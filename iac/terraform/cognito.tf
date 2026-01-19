
# Cognito User Pool
resource "aws_cognito_user_pool" "namesoutofahat" {
  name = "namesoutofahat-users"

  # Use email as the username
  username_attributes      = ["email"]
  auto_verified_attributes = ["email"]

  # Password policy
  password_policy {
    minimum_length                   = 8
    require_lowercase                = false
    require_numbers                  = false
    require_symbols                  = false
    require_uppercase                = false
    temporary_password_validity_days = 7
  }

  # Required and optional attributes
  schema {
    attribute_data_type      = "String"
    name                     = "email"
    required                 = true
    mutable                  = false
    developer_only_attribute = false

    string_attribute_constraints {
      min_length = 1
      max_length = 256
    }
  }

  schema {
    attribute_data_type      = "String"
    name                     = "given_name"
    required                 = true
    mutable                  = false
    developer_only_attribute = false

    string_attribute_constraints {
      min_length = 1
      max_length = 256
    }
  }

  # Account recovery settings
  account_recovery_setting {
    recovery_mechanism {
      name     = "verified_email"
      priority = 1
    }
  }

  # Email configuration using SES
  email_configuration {
    email_sending_account = "DEVELOPER"
    source_arn            = data.terraform_remote_state.email.outputs.ses_domain_identity_arn
    from_email_address    = "donotreply@mail.namesoutofahat.com"
  }

  # MFA configuration (optional, but good practice)
  mfa_configuration = "OPTIONAL"

  software_token_mfa_configuration {
    enabled = true
  }

  # Require verification when attributes are updated
  user_attribute_update_settings {
    attributes_require_verification_before_update = ["email"]
  }

  # Prevent accidental deletion
  deletion_protection = "ACTIVE"

  tags = {
    Name = "namesoutofahat-user-pool"
  }
}

# Cognito User Pool Client for SPA
resource "aws_cognito_user_pool_client" "spa_client" {
  name         = "namesoutofahat-spa-client"
  user_pool_id = aws_cognito_user_pool.namesoutofahat.id

  # Token validity periods
  id_token_validity      = 60 # minutes
  access_token_validity  = 60 # minutes
  refresh_token_validity = 30 # days

  token_validity_units {
    id_token      = "minutes"
    access_token  = "minutes"
    refresh_token = "days"
  }

  # Auth flows for SPA
  explicit_auth_flows = [
    "ALLOW_USER_SRP_AUTH",
    "ALLOW_REFRESH_TOKEN_AUTH",
    "ALLOW_USER_PASSWORD_AUTH"
  ]

  # Disable client secret for public SPA client
  generate_secret = false

  # OAuth settings
  allowed_oauth_flows_user_pool_client = true
  allowed_oauth_flows                  = ["code", "implicit"]
  allowed_oauth_scopes                 = ["email", "openid", "profile"]

  # Callback URLs (update these with your actual URLs)
  callback_urls = [
    "http://localhost:3000/callback",
    "https://namesoutofahat.com/callback"
  ]

  logout_urls = [
    "http://localhost:3000/logout",
    "https://namesoutofahat.com/logout"
  ]

  # Prevent user existence errors for security
  prevent_user_existence_errors = "ENABLED"

  # Read and write attributes
  read_attributes  = ["email", "given_name", "email_verified"]
  write_attributes = ["email", "given_name"]
}

# Cognito User Pool Domain
resource "aws_cognito_user_pool_domain" "namesoutofahat" {
  domain       = "namesoutofahat-auth-${data.aws_caller_identity.current.account_id}"
  user_pool_id = aws_cognito_user_pool.namesoutofahat.id
}

# IAM Role for Cognito to send emails via SES
resource "aws_iam_role" "cognito_ses" {
  name = "namesoutofahat-cognito-ses-role"

  assume_role_policy = jsonencode({ 
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "cognito-idp.amazonaws.com"
        }
        Condition = {
          StringEquals = {
            "sts:ExternalId" = "cognito-ses-${data.aws_caller_identity.current.account_id}"
          }
        }
      }
    ]
  })

  tags = {
    Name = "namesoutofahat-cognito-ses-role"
  }
}

# IAM Policy for Cognito to send emails via SES
resource "aws_iam_role_policy" "cognito_ses" {
  name = "namesoutofahat-cognito-ses-policy"
  role = aws_iam_role.cognito_ses.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "ses:SendEmail",
          "ses:SendRawEmail"
        ]
        Resource = data.terraform_remote_state.email.outputs.ses_domain_identity_arn
      }
    ]
  })
}

