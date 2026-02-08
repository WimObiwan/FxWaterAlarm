using Site.Services;
using Xunit;

namespace SiteTests.Services;

public class MessagesOptionsTest
{
    [Fact]
    public void MessagesOptions_CanBeCreated()
    {
        var options = new MessagesOptions
        {
            Messages = new[]
            {
                new Message
                {
                    Id = "msg1",
                    Type = Message.TypeEnum.Warning,
                    Contents = new Dictionary<string, string> { { "en", "Warning message" } },
                    ExpirationUtc = DateTime.UtcNow.AddDays(1)
                }
            },
            DismissRepeatInterval = TimeSpan.FromHours(1)
        };

        Assert.Single(options.Messages);
        Assert.Equal("msg1", options.Messages[0].Id);
        Assert.Equal(Message.TypeEnum.Warning, options.Messages[0].Type);
        Assert.Equal("Warning message", options.Messages[0].Contents["en"]);
    }

    [Fact]
    public void MessagesOptions_Location()
    {
        Assert.Equal("BannerMessages", MessagesOptions.Location);
    }

    [Fact]
    public void MessagesOptions_DismissRepeatIntervalCanBeNull()
    {
        var options = new MessagesOptions
        {
            Messages = Array.Empty<Message>()
        };

        Assert.Null(options.DismissRepeatInterval);
    }

    [Fact]
    public void Message_AllTypes()
    {
        var types = Enum.GetValues<Message.TypeEnum>();
        Assert.Equal(8, types.Length);
        Assert.Contains(Message.TypeEnum.Primary, types);
        Assert.Contains(Message.TypeEnum.Secondary, types);
        Assert.Contains(Message.TypeEnum.Success, types);
        Assert.Contains(Message.TypeEnum.Danger, types);
        Assert.Contains(Message.TypeEnum.Warning, types);
        Assert.Contains(Message.TypeEnum.Info, types);
        Assert.Contains(Message.TypeEnum.Light, types);
        Assert.Contains(Message.TypeEnum.Dark, types);
    }

    [Fact]
    public void Message_MultipleContents()
    {
        var message = new Message
        {
            Id = "test",
            Type = Message.TypeEnum.Info,
            Contents = new Dictionary<string, string>
            {
                { "en", "English" },
                { "nl", "Dutch" },
                { "fr", "French" }
            },
            ExpirationUtc = DateTime.UtcNow.AddDays(1)
        };

        Assert.Equal(3, message.Contents.Count);
    }
}

public class MeasurementDisplayOptionsTest
{
    [Fact]
    public void Location_IsMeasurementDisplay()
    {
        Assert.Equal("MeasurementDisplay", MeasurementDisplayOptions.Location);
    }

    [Fact]
    public void OldMeasurementThresholdIntervals_CanBeNull()
    {
        var options = new MeasurementDisplayOptions
        {
            OldMeasurementThresholdIntervals = null
        };

        Assert.Null(options.OldMeasurementThresholdIntervals);
    }

    [Fact]
    public void OldMeasurementThresholdIntervals_CanBeSet()
    {
        var options = new MeasurementDisplayOptions
        {
            OldMeasurementThresholdIntervals = 5
        };

        Assert.Equal(5, options.OldMeasurementThresholdIntervals);
    }
}
