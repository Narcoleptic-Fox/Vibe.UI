using System;
using System.Threading.Tasks;

namespace Vibe.UI.Services.Toast
{
    /// <summary>
    /// Service for displaying toast notifications.
    /// </summary>
    public class ToastService : IToastService
    {
        /// <summary>
        /// Event raised when a toast notification is added.
        /// </summary>
        public event EventHandler<ToastEventArgs>? OnToastAdded;

        /// <summary>
        /// Event raised when a toast notification is removed.
        /// </summary>
        public event EventHandler<ToastEventArgs>? OnToastRemoved;

        /// <summary>
        /// Shows a default toast notification.
        /// </summary>
        public Task ShowAsync(string title, string? message = null, int duration = 5000)
        {
            return ShowCustomAsync(title, message, "default", null, duration);
        }

        /// <summary>
        /// Shows a success toast notification.
        /// </summary>
        public Task ShowSuccessAsync(string title, string? message = null, int duration = 5000)
        {
            return ShowCustomAsync(title, message, "success", null, duration);
        }

        /// <summary>
        /// Shows an error toast notification.
        /// </summary>
        public Task ShowErrorAsync(string title, string? message = null, int duration = 5000)
        {
            return ShowCustomAsync(title, message, "error", null, duration);
        }

        /// <summary>
        /// Shows a warning toast notification.
        /// </summary>
        public Task ShowWarningAsync(string title, string? message = null, int duration = 5000)
        {
            return ShowCustomAsync(title, message, "warning", null, duration);
        }

        /// <summary>
        /// Shows an info toast notification.
        /// </summary>
        public Task ShowInfoAsync(string title, string? message = null, int duration = 5000)
        {
            return ShowCustomAsync(title, message, "info", null, duration);
        }

        /// <summary>
        /// Shows a custom toast notification.
        /// </summary>
        public Task ShowCustomAsync(string title, string? message, string variant, string? icon = null, int duration = 5000)
        {
            var safeDuration = Math.Max(0, duration);
            var toastId = Guid.NewGuid().ToString();
            var args = new ToastEventArgs
            {
                Id = toastId,
                Title = title,
                Description = message ?? string.Empty,
                Variant = variant,
                Icon = icon,
                Duration = safeDuration
            };

            OnToastAdded?.Invoke(this, args);

            // Schedule removal of the toast after the duration
            _ = RemoveAfterDelayAsync(args, safeDuration);

            return Task.CompletedTask;
        }

        private async Task RemoveAfterDelayAsync(ToastEventArgs args, int duration)
        {
            try
            {
                await Task.Delay(duration).ConfigureAwait(false);
                try
                {
                    OnToastRemoved?.Invoke(this, args);
                }
                catch
                {
                    // Swallow event handler exceptions to avoid unobserved task exceptions.
                }
            }
            catch
            {
                // Ignore: Task.Delay can throw for cancellation, etc. (none expected here).
            }
        }
    }
}
