using Core.Exceptions;
using Xunit;

namespace CoreTests;

public class ExceptionsTest
{
    // --- AccountNotFoundException ---

    [Fact]
    public void AccountNotFoundException_DefaultConstructor()
    {
        var ex = new AccountNotFoundException();

        Assert.NotNull(ex);
        Assert.Null(ex.AccountUid);
        Assert.Null(ex.Email);
    }

    [Fact]
    public void AccountNotFoundException_WithMessage()
    {
        var ex = new AccountNotFoundException("Account not found");

        Assert.Equal("Account not found", ex.Message);
    }

    [Fact]
    public void AccountNotFoundException_WithMessageAndInner()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new AccountNotFoundException("Account not found", inner);

        Assert.Equal("Account not found", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void AccountNotFoundException_Properties()
    {
        var uid = Guid.NewGuid();
        var ex = new AccountNotFoundException
        {
            AccountUid = uid,
            Email = "test@example.com"
        };

        Assert.Equal(uid, ex.AccountUid);
        Assert.Equal("test@example.com", ex.Email);
    }

    // --- AccountSensorAlarmNotFoundException ---

    [Fact]
    public void AccountSensorAlarmNotFoundException_DefaultConstructor()
    {
        var ex = new AccountSensorAlarmNotFoundException();

        Assert.NotNull(ex);
    }

    [Fact]
    public void AccountSensorAlarmNotFoundException_WithMessage()
    {
        var ex = new AccountSensorAlarmNotFoundException("Alarm not found");

        Assert.Equal("Alarm not found", ex.Message);
    }

    [Fact]
    public void AccountSensorAlarmNotFoundException_WithMessageAndInner()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new AccountSensorAlarmNotFoundException("Alarm not found", inner);

        Assert.Equal("Alarm not found", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void AccountSensorAlarmNotFoundException_Properties()
    {
        var accountUid = Guid.NewGuid();
        var sensorUid = Guid.NewGuid();
        var alarmUid = Guid.NewGuid();
        var ex = new AccountSensorAlarmNotFoundException
        {
            AccountUid = accountUid,
            SensorUid = sensorUid,
            AlarmUid = alarmUid
        };

        Assert.Equal(accountUid, ex.AccountUid);
        Assert.Equal(sensorUid, ex.SensorUid);
        Assert.Equal(alarmUid, ex.AlarmUid);
    }

    // --- AccountSensorDisabledException ---

    [Fact]
    public void AccountSensorDisabledException_DefaultConstructor()
    {
        var ex = new AccountSensorDisabledException();

        Assert.NotNull(ex);
    }

    [Fact]
    public void AccountSensorDisabledException_WithInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new AccountSensorDisabledException(inner);

        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void AccountSensorDisabledException_Message_ContainsUids()
    {
        var accountUid = Guid.NewGuid();
        var sensorUid = Guid.NewGuid();
        var ex = new AccountSensorDisabledException
        {
            AccountUid = accountUid,
            SensorUid = sensorUid
        };

        Assert.Contains(accountUid.ToString(), ex.Message);
        Assert.Contains(sensorUid.ToString(), ex.Message);
        Assert.Contains("disabled", ex.Message);
    }

    [Fact]
    public void AccountSensorDisabledException_Properties()
    {
        var accountUid = Guid.NewGuid();
        var sensorUid = Guid.NewGuid();
        var ex = new AccountSensorDisabledException
        {
            AccountUid = accountUid,
            SensorUid = sensorUid
        };

        Assert.Equal(accountUid, ex.AccountUid);
        Assert.Equal(sensorUid, ex.SensorUid);
    }

    // --- AccountSensorNotFoundException ---

    [Fact]
    public void AccountSensorNotFoundException_DefaultConstructor()
    {
        var ex = new AccountSensorNotFoundException();

        Assert.NotNull(ex);
    }

    [Fact]
    public void AccountSensorNotFoundException_WithMessage()
    {
        var ex = new AccountSensorNotFoundException("Sensor not found");

        Assert.Equal("Sensor not found", ex.Message);
    }

    [Fact]
    public void AccountSensorNotFoundException_WithMessageAndInner()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new AccountSensorNotFoundException("Sensor not found", inner);

        Assert.Equal("Sensor not found", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void AccountSensorNotFoundException_Properties()
    {
        var accountUid = Guid.NewGuid();
        var sensorUid = Guid.NewGuid();
        var ex = new AccountSensorNotFoundException
        {
            AccountUid = accountUid,
            SensorUid = sensorUid
        };

        Assert.Equal(accountUid, ex.AccountUid);
        Assert.Equal(sensorUid, ex.SensorUid);
    }

    // --- SensorCouldNotBeRemovedException ---

    [Fact]
    public void SensorCouldNotBeRemovedException_DefaultConstructor()
    {
        var ex = new SensorCouldNotBeRemovedException();

        Assert.NotNull(ex);
    }

    [Fact]
    public void SensorCouldNotBeRemovedException_WithMessage()
    {
        var ex = new SensorCouldNotBeRemovedException("Cannot remove");

        Assert.Equal("Cannot remove", ex.Message);
    }

    [Fact]
    public void SensorCouldNotBeRemovedException_WithMessageAndInner()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new SensorCouldNotBeRemovedException("Cannot remove", inner);

        Assert.Equal("Cannot remove", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void SensorCouldNotBeRemovedException_Properties()
    {
        var accountUid = Guid.NewGuid();
        var sensorUid = Guid.NewGuid();
        var ex = new SensorCouldNotBeRemovedException
        {
            AccountUid = accountUid,
            SensorUid = sensorUid
        };

        Assert.Equal(accountUid, ex.AccountUid);
        Assert.Equal(sensorUid, ex.SensorUid);
    }

    // --- SensorNotFoundException ---

    [Fact]
    public void SensorNotFoundException_DefaultConstructor()
    {
        var ex = new SensorNotFoundException();

        Assert.NotNull(ex);
    }

    [Fact]
    public void SensorNotFoundException_WithMessage()
    {
        var ex = new SensorNotFoundException("Not found");

        Assert.Equal("Not found", ex.Message);
    }

    [Fact]
    public void SensorNotFoundException_WithMessageAndInner()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new SensorNotFoundException("Not found", inner);

        Assert.Equal("Not found", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void SensorNotFoundException_Properties()
    {
        var sensorUid = Guid.NewGuid();
        var ex = new SensorNotFoundException
        {
            SensorUid = sensorUid,
            DevEui = "dev123"
        };

        Assert.Equal(sensorUid, ex.SensorUid);
        Assert.Equal("dev123", ex.DevEui);
    }

    [Fact]
    public void SensorNotFoundException_NullableProperties_DefaultToNull()
    {
        var ex = new SensorNotFoundException();

        Assert.Null(ex.SensorUid);
        Assert.Null(ex.DevEui);
    }
}
