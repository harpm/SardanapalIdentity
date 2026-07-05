using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Share.Static;

namespace Sardanapal.Identity.Services.Services.AccountService;

public class LoginAttemptTracker : ILoginAttemptTracker
{
    private readonly ConcurrentDictionary<string, AttemptState> _store = new();
    private readonly SDConfigs _config;

    public LoginAttemptTracker(IOptions<SDConfigs> config)
    {
        _config = config?.Value ?? new SDConfigs();
    }

    public bool IsLockedOut(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;

        if (_store.TryGetValue(key, out var state) && state.LockoutUntil > DateTime.UtcNow)
            return true;

        return false;
    }

    public TimeSpan? GetLockoutRemaining(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        if (_store.TryGetValue(key, out var state) && state.LockoutUntil > DateTime.UtcNow)
            return state.LockoutUntil - DateTime.UtcNow;

        return null;
    }

    public int GetRemainingAttempts(string key)
    {
        if (string.IsNullOrEmpty(key)) return _config.MaxLoginAttempts;

        PurgeIfExpired(key);

        if (!_store.TryGetValue(key, out var state) || state.LockoutUntil > DateTime.UtcNow)
            return _config.MaxLoginAttempts;

        return Math.Max(0, _config.MaxLoginAttempts - state.FailureCount);
    }

    public void RecordFailure(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        var now = DateTime.UtcNow;
        _store.AddOrUpdate(
            key,
            _ => new AttemptState(1, now, null),
            (_, state) =>
            {
                PurgeIfExpired(key, state);
                int failures = state.FailureCount + 1;
                DateTime? lockoutUntil = failures >= _config.MaxLoginAttempts
                    ? now.AddMinutes(_config.LockoutMinutes)
                    : state.LockoutUntil;
                return new AttemptState(failures, now, lockoutUntil);
            });
    }

    public void RecordSuccess(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        Reset(key);
    }

    public void Reset(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        _store.TryRemove(key, out _);
    }

    private void PurgeIfExpired(string key)
    {
        if (_store.TryGetValue(key, out var state))
            PurgeIfExpired(key, state);
    }

    private void PurgeIfExpired(string key, AttemptState state)
    {
        if (state.LockoutUntil.HasValue && state.LockoutUntil.Value <= DateTime.UtcNow
            && state.FailureCount >= _config.MaxLoginAttempts)
        {
            _store.TryRemove(key, out _);
        }
    }

    private sealed record AttemptState(int FailureCount, DateTime LastAttempt, DateTime? LockoutUntil);
}
