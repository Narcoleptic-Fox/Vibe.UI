using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vibe.UI.Metadata;

namespace Vibe.UI;

/// <summary>Registers the property metadata services.</summary>
public static class MetadataServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IPropertyMetadataResolver"/> with the built-in sources: model profiles,
    /// DataAnnotations attributes and naming convention. Safe to call more than once; each call's
    /// profiles are added to the same options. <c>AddVibeUI</c> calls this with no profiles.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional model profiles.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddVibeMetadata(
        this IServiceCollection services,
        Action<VibeMetadataOptions>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        // The options object is created here so that repeated calls can keep adding profiles to it.
        var options = (VibeMetadataOptions?)
            services
                .FirstOrDefault(descriptor => descriptor.ServiceType == typeof(VibeMetadataOptions))
                ?.ImplementationInstance;
        if (options is null)
        {
            options = new VibeMetadataOptions();
            services.AddSingleton(options);
        }
        configure?.Invoke(options);

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPropertyMetadataProvider, ProfileMetadataProvider>()
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IPropertyMetadataProvider,
                DataAnnotationsMetadataProvider
            >()
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPropertyMetadataProvider, ConventionMetadataProvider>()
        );
        services.TryAddSingleton<IPropertyMetadataResolver, PropertyMetadataResolver>();
        return services;
    }
}
