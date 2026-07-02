using System.Text.Json;
using Microsoft.JSInterop;

namespace VMS.Web.Auth;

public class MockAuthService(IJSRuntime jsRuntime)
{
    private const string StorageKey = "vms.mock.session";
    private MockSession? _currentSession;

    public MockSession? CurrentSession => _currentSession;
    public bool IsAuthenticated => _currentSession is not null;
    public event Action? AuthenticationStateChanged;

    public async Task InitializeAsync()
    {
        if (_currentSession is not null)
        {
            return;
        }

        var json = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        _currentSession = JsonSerializer.Deserialize<MockSession>(json);
        AuthenticationStateChanged?.Invoke();
    }

    public async Task LoginAsAsync(DemoRole role)
    {
        _currentSession = MockRoleCatalog.CreateSession(role);
        var json = JsonSerializer.Serialize(_currentSession);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        AuthenticationStateChanged?.Invoke();
    }

    public async Task LogoutAsync()
    {
        _currentSession = null;
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        AuthenticationStateChanged?.Invoke();
    }

    public bool HasPermission(string permission)
    {
        return _currentSession?.Permissions.Contains(permission) == true;
    }
}
