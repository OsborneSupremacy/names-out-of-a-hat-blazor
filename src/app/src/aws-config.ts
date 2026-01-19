// AWS Amplify Configuration for Cognito Authentication
export const amplifyConfig = {
  Auth: {
    Cognito: {
      userPoolId: 'us-east-1_KfO6lyIL2',
      userPoolClientId: '7inm1b466e49gjfn8v8rsftfi1',
      loginWith: {
        oauth: {
          domain: 'namesoutofahat-auth-182571449491.auth.us-east-1.amazoncognito.com',
          scopes: ['email', 'openid', 'profile'],
          redirectSignIn: ['http://localhost:5173/', 'https://namesoutofahat.com/'],
          redirectSignOut: ['http://localhost:5173/', 'https://namesoutofahat.com/'],
          responseType: 'code',
        },
      },
    },
  },
};

// API Gateway configuration
export const apiConfig = {
  endpoint: 'https://bz8vg16gqk.execute-api.us-east-1.amazonaws.com/live',
};
