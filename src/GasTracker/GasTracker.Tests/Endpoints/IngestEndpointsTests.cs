using FluentAssertions;
using GasTracker.Core;

namespace GasTracker.Tests;

public class MemoParserTests
{
    [Fact]
    public void Parse_IngestFormat_ReturnsAllFields()
    {
        var result = MemoParser.Parse("Tacoma, 87, $3.299, 45200");
        result.Should().NotBeNull();
        result!.VehicleName.Should().Be("Tacoma");
        result.OctaneRating.Should().Be(87);
        result.PricePerGallon.Should().Be(3.299m);
        result.OdometerMiles.Should().Be(45200);
        result.Gallons.Should().BeNull();
    }

    [Fact]
    public void Parse_IngestFormat_NoSpaces_StillParses()
    {
        var result = MemoParser.Parse("Civic,93,$4.199,12000");
        result.Should().NotBeNull();
        result!.VehicleName.Should().Be("Civic");
        result.OctaneRating.Should().Be(93);
        result.PricePerGallon.Should().Be(4.199m);
        result.OdometerMiles.Should().Be(12000);
    }

    [Fact]
    public void Parse_IngestFormat_ExtraFields_IgnoresTrailing()
    {
        var result = MemoParser.Parse("Tacoma, 87, $3.299, 45200, extra");
        result.Should().NotBeNull();
        result!.OdometerMiles.Should().Be(45200);
    }

    [Fact]
    public void Parse_PushFormat_FullMemo_ReturnsAllFields()
    {
        var result = MemoParser.Parse("14.500gal @ $3.500/gal, 87 oct, 45000mi");
        result.Should().NotBeNull();
        result!.Gallons.Should().Be(14.500m);
        result.PricePerGallon.Should().Be(3.500m);
        result.OctaneRating.Should().Be(87);
        result.OdometerMiles.Should().Be(45000);
        result.VehicleName.Should().BeNull();
    }

    [Fact]
    public void Parse_PushFormat_WithoutOctane_ReturnsPartial()
    {
        var result = MemoParser.Parse("10.000gal @ $4.000/gal, 30000mi");
        result.Should().NotBeNull();
        result!.Gallons.Should().Be(10.000m);
        result.PricePerGallon.Should().Be(4.000m);
        result.OctaneRating.Should().BeNull();
        result.OdometerMiles.Should().Be(30000);
    }

    [Fact]
    public void Parse_PushFormat_GallonsAndPriceOnly()
    {
        var result = MemoParser.Parse("12.500gal @ $3.299/gal");
        result.Should().NotBeNull();
        result!.Gallons.Should().Be(12.500m);
        result.PricePerGallon.Should().Be(3.299m);
        result.OctaneRating.Should().BeNull();
        result.OdometerMiles.Should().BeNull();
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
    public void Parse_NullMemo_ReturnsNull()
    {
        MemoParser.Parse(null).Should().BeNull();
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
        result!.PricePerGallon.Should().Be(3.299m);
    }
}
