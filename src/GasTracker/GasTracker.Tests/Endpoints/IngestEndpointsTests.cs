using FluentAssertions;
using GasTracker.Core;

namespace GasTracker.Tests;

public class MemoParserTests
{
    [Fact]
    public void Parse_ValidMemo_ReturnsAllFields()
    {
        var result = MemoParser.Parse("Tacoma, 87, $3.299, 45200");
        result.Should().NotBeNull();
        result!.Value.VehicleName.Should().Be("Tacoma");
        result.Value.Octane.Should().Be(87);
        result.Value.Price.Should().Be(3.299m);
        result.Value.Mileage.Should().Be(45200);
    }

    [Fact]
    public void Parse_NoSpaces_StillParses()
    {
        var result = MemoParser.Parse("Civic,93,$4.199,12000");
        result.Should().NotBeNull();
        result!.Value.VehicleName.Should().Be("Civic");
        result.Value.Octane.Should().Be(93);
        result.Value.Price.Should().Be(4.199m);
        result.Value.Mileage.Should().Be(12000);
    }

    [Fact]
    public void Parse_ExtraFields_IgnoresTrailing()
    {
        var result = MemoParser.Parse("Tacoma, 87, $3.299, 45200, extra");
        result.Should().NotBeNull();
        result!.Value.Mileage.Should().Be(45200);
    }

    [Theory]
    [InlineData("")]
    [InlineData("just one field")]
    [InlineData("a, b")]
    [InlineData("a, b, c")]
    public void Parse_TooFewFields_ReturnsNull(string memo)
    {
        MemoParser.Parse(memo).Should().BeNull();
    }

    [Fact]
    public void Parse_InvalidOctane_ReturnsNull()
    {
        MemoParser.Parse("Tacoma, abc, $3.299, 45200").Should().BeNull();
    }

    [Fact]
    public void Parse_ZeroOctane_ReturnsNull()
    {
        MemoParser.Parse("Tacoma, 0, $3.299, 45200").Should().BeNull();
    }

    [Fact]
    public void Parse_NegativeOctane_ReturnsNull()
    {
        MemoParser.Parse("Tacoma, -1, $3.299, 45200").Should().BeNull();
    }

    [Fact]
    public void Parse_InvalidPrice_ReturnsNull()
    {
        MemoParser.Parse("Tacoma, 87, notaprice, 45200").Should().BeNull();
    }

    [Fact]
    public void Parse_ZeroPrice_ReturnsNull()
    {
        MemoParser.Parse("Tacoma, 87, $0, 45200").Should().BeNull();
    }

    [Fact]
    public void Parse_InvalidMileage_ReturnsNull()
    {
        MemoParser.Parse("Tacoma, 87, $3.299, abc").Should().BeNull();
    }

    [Fact]
    public void Parse_ZeroMileage_ReturnsNull()
    {
        MemoParser.Parse("Tacoma, 87, $3.299, 0").Should().BeNull();
    }

    [Fact]
    public void Parse_EmptyVehicleName_ReturnsNull()
    {
        MemoParser.Parse(", 87, $3.299, 45200").Should().BeNull();
    }

    [Fact]
    public void Parse_PriceWithoutDollarSign_Parses()
    {
        var result = MemoParser.Parse("Tacoma, 87, 3.299, 45200");
        result.Should().NotBeNull();
        result!.Value.Price.Should().Be(3.299m);
    }
}
