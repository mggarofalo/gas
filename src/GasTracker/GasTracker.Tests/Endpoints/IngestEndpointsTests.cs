using FluentAssertions;
using GasTracker.Api.Endpoints;

namespace GasTracker.Tests.Endpoints;

public class IngestMemoParserTests
{
    [Fact]
    public void ParseMemo_ValidMemo_ReturnsAllFields()
    {
        var result = IngestEndpoints.ParseMemo("Tacoma, 87, $3.299, 45200");
        result.Should().NotBeNull();
        result!.Value.vehicleName.Should().Be("Tacoma");
        result.Value.octane.Should().Be(87);
        result.Value.price.Should().Be(3.299m);
        result.Value.mileage.Should().Be(45200);
    }

    [Fact]
    public void ParseMemo_NoSpaces_StillParses()
    {
        var result = IngestEndpoints.ParseMemo("Civic,93,$4.199,12000");
        result.Should().NotBeNull();
        result!.Value.vehicleName.Should().Be("Civic");
        result.Value.octane.Should().Be(93);
        result.Value.price.Should().Be(4.199m);
        result.Value.mileage.Should().Be(12000);
    }

    [Fact]
    public void ParseMemo_ExtraFields_IgnoresTrailing()
    {
        var result = IngestEndpoints.ParseMemo("Tacoma, 87, $3.299, 45200, extra");
        result.Should().NotBeNull();
        result!.Value.mileage.Should().Be(45200);
    }

    [Theory]
    [InlineData("")]
    [InlineData("just one field")]
    [InlineData("a, b")]
    [InlineData("a, b, c")]
    public void ParseMemo_TooFewFields_ReturnsNull(string memo)
    {
        IngestEndpoints.ParseMemo(memo).Should().BeNull();
    }

    [Fact]
    public void ParseMemo_InvalidOctane_ReturnsNull()
    {
        IngestEndpoints.ParseMemo("Tacoma, abc, $3.299, 45200").Should().BeNull();
    }

    [Fact]
    public void ParseMemo_ZeroOctane_ReturnsNull()
    {
        IngestEndpoints.ParseMemo("Tacoma, 0, $3.299, 45200").Should().BeNull();
    }

    [Fact]
    public void ParseMemo_NegativeOctane_ReturnsNull()
    {
        IngestEndpoints.ParseMemo("Tacoma, -1, $3.299, 45200").Should().BeNull();
    }

    [Fact]
    public void ParseMemo_InvalidPrice_ReturnsNull()
    {
        IngestEndpoints.ParseMemo("Tacoma, 87, notaprice, 45200").Should().BeNull();
    }

    [Fact]
    public void ParseMemo_ZeroPrice_ReturnsNull()
    {
        IngestEndpoints.ParseMemo("Tacoma, 87, $0, 45200").Should().BeNull();
    }

    [Fact]
    public void ParseMemo_InvalidMileage_ReturnsNull()
    {
        IngestEndpoints.ParseMemo("Tacoma, 87, $3.299, abc").Should().BeNull();
    }

    [Fact]
    public void ParseMemo_ZeroMileage_ReturnsNull()
    {
        IngestEndpoints.ParseMemo("Tacoma, 87, $3.299, 0").Should().BeNull();
    }

    [Fact]
    public void ParseMemo_EmptyVehicleName_ReturnsNull()
    {
        IngestEndpoints.ParseMemo(", 87, $3.299, 45200").Should().BeNull();
    }

    [Fact]
    public void ParseMemo_PriceWithoutDollarSign_Parses()
    {
        var result = IngestEndpoints.ParseMemo("Tacoma, 87, 3.299, 45200");
        result.Should().NotBeNull();
        result!.Value.price.Should().Be(3.299m);
    }
}
