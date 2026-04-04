using FluentAssertions;
using GasTracker.Core.Entities;

namespace GasTracker.Tests.Entities;

public class VehicleTests
{
    [Fact]
    public void Label_ConcatenatesYearMakeModel()
    {
        var vehicle = new Vehicle { Year = 2021, Make = "Toyota", Model = "Tacoma" };
        vehicle.Label.Should().Be("2021 Toyota Tacoma");
    }

    [Fact]
    public void Label_HandlesWhitespaceInNames()
    {
        var vehicle = new Vehicle { Year = 2023, Make = "Land Rover", Model = "Range Rover Sport" };
        vehicle.Label.Should().Be("2023 Land Rover Range Rover Sport");
    }

    [Fact]
    public void IsActive_DefaultsToTrue()
    {
        var vehicle = new Vehicle { Year = 2021, Make = "Toyota", Model = "Tacoma" };
        vehicle.IsActive.Should().BeTrue();
    }

    [Fact]
    public void FillUps_DefaultsToEmptyCollection()
    {
        var vehicle = new Vehicle { Year = 2021, Make = "Toyota", Model = "Tacoma" };
        vehicle.FillUps.Should().BeEmpty();
    }
}
