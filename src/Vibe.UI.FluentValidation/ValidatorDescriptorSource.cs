using System.Collections.Concurrent;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Vibe.UI.FluentValidation;

/// <summary>
/// Finds the registered validator for a model type and keeps what can be read from it without
/// running it. Validators are usually scoped services, so each lookup resolves inside a scope of
/// its own and only the descriptor is kept.
/// </summary>
internal sealed class ValidatorDescriptorSource(IServiceScopeFactory scopes)
{
    private readonly ConcurrentDictionary<Type, IValidatorDescriptor?> _descriptors = new();

    /// <summary>
    /// Returns the descriptor of the validator registered for <paramref name="modelType"/> or the
    /// nearest base type that has one, or <c>null</c> when none is registered.
    /// </summary>
    public IValidatorDescriptor? Find(Type modelType) => _descriptors.GetOrAdd(modelType, Load);

    /// <summary>Resolves the validator for <paramref name="modelType"/> from <paramref name="services"/>, looking at base types too.</summary>
    public static IValidator? Resolve(IServiceProvider services, Type modelType)
    {
        for (
            Type? type = modelType;
            type is not null && type != typeof(object);
            type = type.BaseType
        )
        {
            if (
                services.GetService(typeof(IValidator<>).MakeGenericType(type))
                is IValidator validator
            )
                return validator;
        }
        return null;
    }

    private IValidatorDescriptor? Load(Type modelType)
    {
        using IServiceScope scope = scopes.CreateScope();
        return Resolve(scope.ServiceProvider, modelType)?.CreateDescriptor();
    }
}
