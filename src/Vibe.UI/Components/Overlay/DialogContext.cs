namespace Vibe.UI.Components;

/// <summary>
/// Provides cascading context for Dialog components to communicate state and actions.
/// This enables compositional patterns where DialogTrigger and DialogClose can control the dialog state.
/// </summary>
public class DialogContext
{
    private readonly Func<Task> _openAction;
    private readonly Func<Task> _closeAction;

    /// <summary>
    /// Initializes a new instance of the <see cref="DialogContext"/> class.
    /// </summary>
    /// <param name="openAction">The action to execute when opening the dialog.</param>
    /// <param name="closeAction">The action to execute when closing the dialog.</param>
    public DialogContext(Func<Task> openAction, Func<Task> closeAction)
    {
        _openAction = openAction;
        _closeAction = closeAction;
    }

    /// <summary>
    /// Opens the dialog.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task Open() => _openAction();

    /// <summary>
    /// Closes the dialog.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task Close() => _closeAction();
}
