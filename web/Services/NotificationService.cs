using MudBlazor;
using application.Models;
using application.Services;

namespace web.Services;

public class NotificationService
{
    private readonly ISnackbar _snackbar;
    private readonly SystemLogger _logger;

    public NotificationService(ISnackbar snackbar, SystemLogger logger)
    {
        _snackbar = snackbar;
        _logger = logger;

        // Subscribe to backend error and notification events
        _logger.OnError += HandleBackendError;
        _logger.OnNotify += HandleBackendNotify;
    }

    private void HandleBackendError(LogEntry entry)
    {
        if (entry.Source == "UI") return;
        _snackbar.Add(entry.Message, Severity.Error);
    }

    private void HandleBackendNotify(LogEntry entry)
    {
        _snackbar.Add(entry.Message, Severity.Success);
    }

    public void NewInfo(string message, bool logOnly = false)
    {
        if (!logOnly)
            _snackbar.Add(message, Severity.Info);
        _logger.Log(application.Models.LogLevel.Info, "UI", message, category: "Notification");
    }

    public void NewSuccess(string message, bool logOnly = false)
    {
        if (!logOnly)
            _snackbar.Add(message, Severity.Success);
        _logger.Log(application.Models.LogLevel.Info, "UI", message, category: "Notification");
    }

    public void NewWarning(string message, bool logOnly = false)
    {
        if (!logOnly)
            _snackbar.Add(message, Severity.Warning);
        _logger.Log(application.Models.LogLevel.Warning, "UI", message, category: "Notification");
    }

    public void NewError(string message, Exception? exception = null, bool logOnly = false)
    {
        if (!logOnly)
            _snackbar.Add(message, Severity.Error);
        _logger.Log(application.Models.LogLevel.Error, "UI", message, exception?.ToString(), "Notification");
    }
}
