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
