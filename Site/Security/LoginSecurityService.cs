using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Site.Security;

public class LoginSecurityOptions
{
    public const string Location = "LoginSecurity";

    public int CodeSendPerIpPerHour { get; init; } = 12;
    public int CodeSendPerTargetPerHour { get; init; } = 6;
    public int CodeVerifyPerIpPerMinute { get; init; } = 30;
    public int CodeVerifyPerTargetPerMinute { get; init; } = 10;
    public int FailedVerifyLockThreshold { get; init; } = 6;
    public int FailedVerifyWindowMinutes { get; init; } = 15;
    public int FailedVerifyLockMinutes { get; init; } = 15;
}

public interface ILoginSecurityService
{
    bool CanSendCode(string clientIp, string target, out TimeSpan retryAfter);
    bool CanVerifyCode(string clientIp, string target, out TimeSpan retryAfter);
    void RecordVerifyResult(string clientIp, string target, bool success);
}

public class LoginSecurityService : ILoginSecurityService
{
    private readonly IMemoryCache _cache;
    private readonly LoginSecurityOptions _options;
    private readonly object _gate = new();

    public LoginSecurityService(IMemoryCache cache, IOptions<LoginSecurityOptions> options)
    {
        _cache = cache;
        _options = options.Value;
    }

    public bool CanSendCode(string clientIp, string target, out TimeSpan retryAfter)
    {
        var now = DateTimeOffset.UtcNow;
        var normalizedTarget = NormalizeTarget(target);

        lock (_gate)
        {
            var ipKey = $"login:send:ip:{clientIp}";
            if (!TryIncrementWithinWindow(ipKey, _options.CodeSendPerIpPerHour, TimeSpan.FromHours(1), now, out retryAfter))
            {
                return false;
            }

            var targetKey = $"login:send:target:{normalizedTarget}";
            if (!TryIncrementWithinWindow(targetKey, _options.CodeSendPerTargetPerHour, TimeSpan.FromHours(1), now, out retryAfter))
            {
                return false;
            }
        }

        retryAfter = TimeSpan.Zero;
        return true;
    }

    public bool CanVerifyCode(string clientIp, string target, out TimeSpan retryAfter)
    {
        var now = DateTimeOffset.UtcNow;
        var normalizedTarget = NormalizeTarget(target);
        var lockIpKey = $"login:verify:lock:ip:{clientIp}";
        var lockTargetKey = $"login:verify:lock:target:{normalizedTarget}";

        lock (_gate)
        {
            if (TryGetLock(lockIpKey, now, out retryAfter) || TryGetLock(lockTargetKey, now, out retryAfter))
            {
                return false;
            }

            var ipKey = $"login:verify:ip:{clientIp}";
            if (!TryIncrementWithinWindow(ipKey, _options.CodeVerifyPerIpPerMinute, TimeSpan.FromMinutes(1), now, out retryAfter))
            {
                return false;
            }

            var targetKey = $"login:verify:target:{normalizedTarget}";
            if (!TryIncrementWithinWindow(targetKey, _options.CodeVerifyPerTargetPerMinute, TimeSpan.FromMinutes(1), now, out retryAfter))
            {
                return false;
            }
        }

        retryAfter = TimeSpan.Zero;
        return true;
    }

    public void RecordVerifyResult(string clientIp, string target, bool success)
    {
        var now = DateTimeOffset.UtcNow;
        var normalizedTarget = NormalizeTarget(target);
        var failIpKey = $"login:verify:fails:ip:{clientIp}";
        var failTargetKey = $"login:verify:fails:target:{normalizedTarget}";
        var lockWindow = TimeSpan.FromMinutes(_options.FailedVerifyLockMinutes);

        lock (_gate)
        {
            if (success)
            {
                _cache.Remove(failIpKey);
                _cache.Remove(failTargetKey);
                _cache.Remove($"login:verify:lock:ip:{clientIp}");
                _cache.Remove($"login:verify:lock:target:{normalizedTarget}");
                return;
            }

            var failureWindow = TimeSpan.FromMinutes(_options.FailedVerifyWindowMinutes);
            var ipFails = IncrementCounter(failIpKey, failureWindow, now);
            var targetFails = IncrementCounter(failTargetKey, failureWindow, now);

            if (ipFails >= _options.FailedVerifyLockThreshold)
            {
                SetLock($"login:verify:lock:ip:{clientIp}", now, lockWindow);
            }

            if (targetFails >= _options.FailedVerifyLockThreshold)
            {
                SetLock($"login:verify:lock:target:{normalizedTarget}", now, lockWindow);
            }
        }
    }

    private static string NormalizeTarget(string target)
    {
        return string.IsNullOrWhiteSpace(target) ? "unknown" : target.Trim().ToLowerInvariant();
    }

    private bool TryGetLock(string key, DateTimeOffset now, out TimeSpan retryAfter)
    {
        if (_cache.TryGetValue<DateTimeOffset>(key, out var lockUntil) && lockUntil > now)
        {
            retryAfter = lockUntil - now;
            return true;
        }

        retryAfter = TimeSpan.Zero;
        return false;
    }

    private void SetLock(string key, DateTimeOffset now, TimeSpan lockWindow)
    {
        var lockUntil = now.Add(lockWindow);
        _cache.Set(key, lockUntil, lockUntil);
    }

    private bool TryIncrementWithinWindow(string key, int maxAttempts, TimeSpan window, DateTimeOffset now, out TimeSpan retryAfter)
    {
        var counter = GetCounter(key, now, window);
        if (counter.Count >= maxAttempts)
        {
            retryAfter = counter.ExpiresUtc - now;
            return false;
        }

        counter = counter with { Count = counter.Count + 1 };
        _cache.Set(key, counter, counter.ExpiresUtc);
        retryAfter = TimeSpan.Zero;
        return true;
    }

    private int IncrementCounter(string key, TimeSpan window, DateTimeOffset now)
    {
        var counter = GetCounter(key, now, window);
        counter = counter with { Count = counter.Count + 1 };
        _cache.Set(key, counter, counter.ExpiresUtc);
        return counter.Count;
    }

    private CounterEntry GetCounter(string key, DateTimeOffset now, TimeSpan? defaultWindow = null)
    {
        if (_cache.TryGetValue<CounterEntry>(key, out var existing) && existing is not null && existing.ExpiresUtc > now)
        {
            return existing;
        }

        var expiresUtc = now.Add(defaultWindow ?? TimeSpan.FromMinutes(1));
        return new CounterEntry(0, expiresUtc);
    }

    private record CounterEntry(int Count, DateTimeOffset ExpiresUtc);
}