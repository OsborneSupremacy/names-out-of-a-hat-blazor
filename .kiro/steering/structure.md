# Project Structure

## Solution Organization

```
NamesOutOfAHat2.sln
├── src/GiftExchange.Library          # Main Lambda function library
└── src/GiftExchange.Library.Tests    # Test project
```

## Source Code Layout

### GiftExchange.Library

```
src/GiftExchange.Library/
├── Abstractions/          # Interfaces (e.g., IApiGatewayHandler)
├── Builders/              # Builder pattern implementations
├── DataModels/            # DynamoDB data models (e.g., HatDataModel)
├── Extensions/            # Extension methods
├── Handlers/              # Lambda entry points (Router, InvitationQueueHandler)
├── Messaging/             # Request/Response DTOs
├── Models/                # Domain models (Hat, Participant, Person, Invitation)
├── Providers/             # Data access layer (GiftExchangeProvider, CorsHeaderProvider)
├── Schemas/               # JSON schemas for validation
├── Services/              # Business logic services
├── Utility/               # Helper classes (ApiGatewayAdapter, Result)
└── Usings.cs              # Global using statements
```

### Key Patterns

**Services**: Each API operation has a dedicated service class (e.g., `CreateHatService`, `AddParticipantService`)
- Implement `IApiGatewayHandler` interface
- Use `ApiGatewayAdapter` for request/response handling
- Return `Result<T>` wrapper with HTTP status codes

**Models vs DataModels**:
- `Models/` - Domain objects used in business logic (immutable records)
- `DataModels/` - DynamoDB persistence layer representations

**Handlers**: Lambda function entry points
- `Router` - Routes API Gateway requests to appropriate services
- `InvitationQueueHandler` - Processes SQS messages for email invitations

## Infrastructure Layout

```
iac/terraform/
├── config.tf                          # Provider and backend configuration
├── locals.tf                          # Local variables and build commands
├── data.tf                            # Data sources
├── dynamodb.tf                        # DynamoDB table definitions
├── sqs.tf                             # SQS queue definitions
├── lambda-*.tf                        # Lambda function resources
├── api-*.tf                           # API Gateway endpoints
├── api-gateway*.tf                    # API Gateway base resources
├── build.tf                           # Build automation
└── modules/                           # Reusable Terraform modules
    ├── api/                           # API Gateway integration module
    └── gateway-options-response/      # CORS OPTIONS handling
```

## Code Organization Conventions

- **Namespace = Folder**: Namespaces match folder structure
- **File-scoped namespaces**: Use file-scoped namespace declarations (C# 10+)
- **One class per file**: Each class/record in its own file
- **Internal by default**: Most classes are `internal`, exposed via `InternalsVisibleTo` for testing
- **Immutable models**: Domain models use `record` types with `init` properties
- **Global usings**: Common imports in `Usings.cs` to reduce boilerplate
