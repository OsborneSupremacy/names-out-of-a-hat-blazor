# Gift Exchange Application

## .NET Lambda Function

Build .NET 10.0 Lambda function and create deployment package:

```bash
cd GiftExchange.Library
dotnet publish -o bin/publish -c Release --framework "net10.0" /p:GenerateRuntimeConfigurationFiles=true --runtime linux-arm64 --self-contained false
cd bin/publish
zip -r ../giftexchange_function.zip .
cd -
```

## Deploy the React Application

Build and deploy the React application to AWS S3:

```bash
cd src/app
npm run build
aws s3 sync dist/ s3://bro-namesoutofahat-frontend --delete --profile benosborne
aws cloudfront create-invalidation --distribution-id E1WP2SNZE07ZF0 --paths "/*" --profile benosborne
```
