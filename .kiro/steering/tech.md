# Technology Stack

## Framework & Runtime

- **.NET 10.0** (C# 14) - Primary development framework
- **AWS Lambda** - Serverless compute platform
- Target runtime: `linux-arm64`

## Key Libraries & Dependencies

### AWS SDK
- `AWSSDK.DynamoDBv2` - Database operations
- `AWSSDK.SimpleEmail` - Email sending via SES
- `AWSSDK.SQS` - Queue management
- `Amazon.Lambda.Core` - Lambda runtime
- `Amazon.Lambda.APIGatewayEvents` - API Gateway integration
- `Amazon.Lambda.SQSEvents` - SQS event handling

### Utilities
- `Bogus` - Test data generation
- `dotenv.net` - Environment variable management
- `JetBrains.Annotations` - Code annotations
- `Microsoft.Extensions.DependencyInjection` - Dependency injection

### Testing
- `xunit` - Test framework
- `FluentAssertions` - Assertion library
- `Testcontainers.DynamoDb` - Local DynamoDB for integration tests

## Infrastructure as Code

- **Terraform** - AWS infrastructure provisioning
- Backend state stored in S3
- Region: `us-east-1`

## Common Commands

### Build
```bash
dotnet build
```

### Test
```bash
dotnet test
```

### Publish Lambda Package
```bash
cd src/GiftExchange.Library
dotnet publish -o bin/publish -c Release --framework "net10.0" \
  /p:GenerateRuntimeConfigurationFiles=true \
  --runtime linux-arm64 --self-contained false
```

### Terraform Deployment
```bash
cd iac/terraform
terraform init
terraform plan
terraform apply
```

## Environment Variables

Lambda functions require:
- `TABLE_NAME` - DynamoDB table name
- `INVITATIONS_QUEUE_URL` - SQS queue URL for invitations
