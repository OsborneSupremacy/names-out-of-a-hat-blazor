using System.Reflection;
using GiftExchange.Library.Contexts;

namespace GiftExchange.Library.Tests.ContextTests;

/// <summary>
/// A single assembly-level LambdaSerializer attribute covers every handler in the assembly, so
/// each entry point's event and response types must be registered in the source-generated context.
/// Miss one and that function fails at runtime, on its first invocation, with a deserialization
/// error — nothing at build time complains.
/// </summary>
public class LambdaHandlerSerializationTests
{
    public static TheoryData<string, Type> HandlerTypes
    {
        get
        {
            var data = new TheoryData<string, Type>();

            foreach (var (handler, type) in FindLambdaEventTypes())
                data.Add(handler, type);

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(HandlerTypes))]
    public void LambdaEventType_IsRegisteredInTheSerializerContext(string handler, Type type)
    {
        var typeInfo = GiftExchangeJsonSerializerContext.Default.GetTypeInfo(type);

        typeInfo.Should().NotBeNull(
            $"{handler} sends or receives {type.Name}, so it needs a JsonSerializable attribute on GiftExchangeJsonSerializerContext");
    }

    /// <summary>
    /// Every type crossing the Lambda boundary: the handler's parameters, minus the context, plus
    /// whatever it returns.
    /// </summary>
    private static IEnumerable<(string Handler, Type Type)> FindLambdaEventTypes()
    {
        var handlerMethods = typeof(GiftExchangeDbContext).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false }
                           && type.Namespace == "GiftExchange.Library.Handlers")
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(method => method.Name == "FunctionHandler");

        foreach (var method in handlerMethods)
        {
            var handler = $"{method.DeclaringType!.Name}.{method.Name}";

            foreach (var parameter in method.GetParameters())
                if (parameter.ParameterType != typeof(ILambdaContext))
                    yield return (handler, parameter.ParameterType);

            // Task means the function returns nothing; Task<T> means T goes back over the wire.
            if (method.ReturnType is { IsGenericType: true } returnType
                && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                yield return (handler, returnType.GetGenericArguments()[0]);
        }
    }
}
