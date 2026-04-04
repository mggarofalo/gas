using FluentAssertions;
using GasTracker.Core.Entities;

namespace GasTracker.Tests.Entities;

public class FillUpTests
{
    [Fact]
    public void PaperlessSyncStatus_DefaultsToNone()
    {
        var fillUp = new FillUp { StationName = "Shell" };
        fillUp.PaperlessSyncStatus.Should().Be("none");
    }

    [Fact]
    public void PaperlessSyncAttempts_DefaultsToZero()
    {
        var fillUp = new FillUp { StationName = "Shell" };
        fillUp.PaperlessSyncAttempts.Should().Be(0);
    }

    [Fact]
    public void OptionalFields_DefaultToNull()
    {
        var fillUp = new FillUp { StationName = "Shell" };
        fillUp.StationAddress.Should().BeNull();
        fillUp.Latitude.Should().BeNull();
        fillUp.Longitude.Should().BeNull();
        fillUp.ReceiptPath.Should().BeNull();
        fillUp.Notes.Should().BeNull();
        fillUp.PaperlessDocumentId.Should().BeNull();
        fillUp.PaperlessSyncError.Should().BeNull();
        fillUp.PaperlessSyncedAt.Should().BeNull();
    }
}
