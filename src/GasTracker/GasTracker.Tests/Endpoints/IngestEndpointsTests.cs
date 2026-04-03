using FluentAssertions;
using GasTracker.Core;

namespace GasTracker.Tests;

public class MemoParserTests
{
    // --- Push-sync format ---

    [Fact]
    public void Parse_PushFormat_FullMemo()
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
    public void Parse_PushFormat_WithoutOctane()
    {
        var result = MemoParser.Parse("10.000gal @ $4.000/gal, 30000mi");
        result.Should().NotBeNull();
        result!.Gallons.Should().Be(10.000m);
        result.OctaneRating.Should().BeNull();
        result.OdometerMiles.Should().Be(30000);
    }

    [Fact]
    public void Parse_PushFormat_NoDollarSign()
    {
        var result = MemoParser.Parse("12.500gal @ 3.299/gal");
        result.Should().NotBeNull();
        result!.Gallons.Should().Be(12.500m);
        result.PricePerGallon.Should().Be(3.299m);
    }

    // --- Flexible comma format: standard order ---

    [Fact]
    public void Parse_Flexible_AllFields()
    {
        var result = MemoParser.Parse("Tacoma, 87, $3.299, 45200");
        result.Should().NotBeNull();
        result!.VehicleName.Should().Be("Tacoma");
        result.OctaneRating.Should().Be(87);
        result.PricePerGallon.Should().Be(3.299m);
        result.OdometerMiles.Should().Be(45200);
    }

    [Fact]
    public void Parse_Flexible_NoSpaces()
    {
        var result = MemoParser.Parse("Civic,93,$4.199,12000");
        result.Should().NotBeNull();
        result!.VehicleName.Should().Be("Civic");
        result.OctaneRating.Should().Be(93);
        result.PricePerGallon.Should().Be(4.199m);
        result.OdometerMiles.Should().Be(12000);
    }

    // --- Flexible: missing octane ---

    [Fact]
    public void Parse_Flexible_NoOctane()
    {
        var result = MemoParser.Parse("Tacoma, $3.299, 45200");
        result.Should().NotBeNull();
        result!.VehicleName.Should().Be("Tacoma");
        result.PricePerGallon.Should().Be(3.299m);
        result.OdometerMiles.Should().Be(45200);
        result.OctaneRating.Should().BeNull();
    }

    // --- Flexible: no $ sign ---

    [Fact]
    public void Parse_Flexible_NoDollarSign()
    {
        var result = MemoParser.Parse("Tacoma, 87, 3.299, 45200");
        result.Should().NotBeNull();
        result!.PricePerGallon.Should().Be(3.299m);
    }

    // --- Flexible: swapped order (octane after price) ---

    [Fact]
    public void Parse_Flexible_OctaneAfterPrice()
    {
        var result = MemoParser.Parse("Tacoma, $3.299, 87, 45200");
        result.Should().NotBeNull();
        result!.VehicleName.Should().Be("Tacoma");
        result.PricePerGallon.Should().Be(3.299m);
        result.OctaneRating.Should().Be(87);
        result.OdometerMiles.Should().Be(45200);
    }

    // --- Flexible: known vehicle names ---

    [Fact]
    public void Parse_Flexible_KnownVehicleName()
    {
        var known = new HashSet<string> { "My Truck" };
        var result = MemoParser.Parse("My Truck, 3.50, 55000", known);
        result.Should().NotBeNull();
        result!.VehicleName.Should().Be("My Truck");
        result.PricePerGallon.Should().Be(3.50m);
        result.OdometerMiles.Should().Be(55000);
    }

    // --- Flexible: price + odometer only (minimal) ---

    [Fact]
    public void Parse_Flexible_PriceAndOdometerOnly()
    {
        var result = MemoParser.Parse("$3.50, 42000");
        result.Should().NotBeNull();
        result!.PricePerGallon.Should().Be(3.50m);
        result.OdometerMiles.Should().Be(42000);
        result.VehicleName.Should().BeNull();
        result.OctaneRating.Should().BeNull();
    }

    // --- Null / empty / insufficient ---

    [Fact]
    public void Parse_Null_ReturnsNull() => MemoParser.Parse(null).Should().BeNull();

    [Fact]
    public void Parse_Empty_ReturnsNull() => MemoParser.Parse("").Should().BeNull();

    [Fact]
    public void Parse_SingleField_ReturnsNull() => MemoParser.Parse("just one field").Should().BeNull();

    [Fact]
    public void Parse_NoUsefulFields_ReturnsNull() => MemoParser.Parse("hello, world").Should().BeNull();

    // --- Extra fields ---

    [Fact]
    public void Parse_Flexible_ExtraFields()
    {
        var result = MemoParser.Parse("Tacoma, 87, $3.299, 45200, extra stuff");
        result.Should().NotBeNull();
        result!.OdometerMiles.Should().Be(45200);
    }
}
