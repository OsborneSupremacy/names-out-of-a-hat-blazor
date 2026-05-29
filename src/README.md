# Gift Exchange Application

## Generate API Documentation

```bash
aws apigateway get-export \
  --rest-api-id bz8vg16gqk \
  --stage-name live \
  --export-type oas30 \
  --accepts application/yaml \
  --profile benosborne \
  ../docs/namesoutofahat_api.yaml
```

## React Application

### Run React Application Locally

```bash
cd src/app
npm run dev
```

### Deploy the React Application

Build and deploy the React application to AWS S3:

```bash
cd src/app
npm run build
aws s3 sync dist/ s3://bro-namesoutofahat-frontend --delete --profile benosborne
aws cloudfront create-invalidation --distribution-id E1WP2SNZE07ZF0 --paths "/*" --profile benosborne
```
