global using Amazon.Lambda.APIGatewayEvents;
global using Amazon.Lambda.Core;

global using JetBrains.Annotations;

global using System.Collections.Immutable;
global using System.ComponentModel.DataAnnotations;
global using System.Net;

global using GiftExchange.Library.Extensions;
global using GiftExchange.Library.Messaging;
global using GiftExchange.Library.Models;
global using GiftExchange.Library.Services;
global using GiftExchange.Library.Utility;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
