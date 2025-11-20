using Microsoft.Extensions.DependencyInjection;
using Vibe.UI.Services.Dialog;
using Vibe.UI.Services.Toast;

namespace Vibe.UI
{
    /// <summary>
    /// Extension methods for registering Vibe.UI services with dependency injection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Vibe.UI services to the service collection.
        /// Registers essential services like Toast and Dialog.
        /// NOTE: Theming is handled via pure CSS - no service registration required.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddVibeUI(this IServiceCollection services)
        {
            // Register toast and dialog services
            services.AddScoped<IToastService, ToastService>();
            services.AddScoped<IDialogService, DialogService>();

            return services;
        }
    }
}