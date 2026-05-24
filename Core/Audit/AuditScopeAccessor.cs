using System.Threading;

namespace Core.Audit;

public interface IAuditScopeAccessor
{
    AuditScopeContext? Current { get; }
    IDisposable Push(AuditScopeContext context);
    IDisposable PushAction(string action, AuditTarget? target = null);
}

public sealed class AuditScopeAccessor : IAuditScopeAccessor
{
    private readonly AsyncLocal<AuditScopeContext?> _current = new();

    public AuditScopeContext? Current => _current.Value;

    public IDisposable Push(AuditScopeContext context)
    {
        var previous = _current.Value;
        _current.Value = context;
        return new RestoreDisposable(_current, previous);
    }

    public IDisposable PushAction(string action, AuditTarget? target = null)
    {
        var current = _current.Value;
        if (current == null)
        {
            var fallback = new AuditScopeContext
            {
                CorrelationId = Guid.NewGuid().ToString(),
                Actor = new AuditActor { Identity = "unknown", AuthType = "Anonymous", IsAdmin = false },
                Client = new AuditClient { IpAddress = "unknown" },
                Action = action,
                Target = target
            };
            return Push(fallback);
        }

        var next = new AuditScopeContext
        {
            CorrelationId = current.CorrelationId,
            Actor = current.Actor,
            Client = current.Client,
            Action = action,
            Target = MergeTarget(current.Target, target)
        };

        return Push(next);
    }

    private static AuditTarget? MergeTarget(AuditTarget? left, AuditTarget? right)
    {
        if (left == null && right == null)
            return null;

        return new AuditTarget
        {
            AccountUid = right?.AccountUid ?? left?.AccountUid,
            AccountLink = right?.AccountLink ?? left?.AccountLink,
            SensorUid = right?.SensorUid ?? left?.SensorUid,
            SensorLink = right?.SensorLink ?? left?.SensorLink,
            DevEui = right?.DevEui ?? left?.DevEui
        };
    }

    private sealed class RestoreDisposable : IDisposable
    {
        private readonly AsyncLocal<AuditScopeContext?> _scope;
        private readonly AuditScopeContext? _previous;
        private bool _disposed;

        public RestoreDisposable(AsyncLocal<AuditScopeContext?> scope, AuditScopeContext? previous)
        {
            _scope = scope;
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _scope.Value = _previous;
            _disposed = true;
        }
    }
}
