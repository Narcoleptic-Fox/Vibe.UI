using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vibe.UI.FluentValidation;
using Vibe.UI.Metadata;

namespace Vibe.UI;

/// <summary>Registers the FluentValidation layer of Vibe.UI's property metadata.</summary>
public static class FluentValidationServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="FluentValidationMetadataProvider"/>, so labels, required and limits are read
    /// from the <c>IValidator&lt;T&gt;</c> registered for a model. It sits below a model profile and
    /// above DataAnnotations. Validators themselves are registered by the app, in the client as well
    /// as on the server. Safe to call more than once.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddVibeFluentValidation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddVibeMetadata();
        services.TryAddSingleton<ValidatorDescriptorSource>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IPropertyMetadataProvider,
                FluentValidationMetadataProvider
            >(provider => new FluentValidationMetadataProvider(
                provider.GetRequiredService<ValidatorDescriptorSource>()
            ))
        );
        return services;
    }
}
