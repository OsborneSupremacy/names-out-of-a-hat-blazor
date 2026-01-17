# Gift Exchange Application

## .NET Lambda Function

Build .NET 10.0 Lambda function and create deployment package:

```bash
cd GiftExchange.Library
dotnet publish -o bin/publish -c Release --framework "net10.0" /p:GenerateRuntimeConfigurationFiles=true --runtime linux-arm64 --self-contained false
cd bin/publish
zip -r ../giftexchange_function.zip .
```

