global using Amazon.DynamoDBv2;

global using Amazon.Lambda.APIGatewayEvents;
global using Amazon.Lambda.Core;

global using dotenv.net.Utilities;

global using JetBrains.Annotations;

global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.DependencyInjection;

global using System.Collections.Immutable;
global using System.Net;

global using GiftExchange.Library.Abstractions;
global using GiftExchange.Library.Builders;
global using GiftExchange.Library.Extensions;
global using GiftExchange.Library.Messaging;
global using GiftExchange.Library.Models;
global using GiftExchange.Library.Providers;
global using GiftExchange.Library.Services;
global using GiftExchange.Library.Utility;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
