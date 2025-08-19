using FluentAssertions;
using SatisfactorySaveNet;

namespace SatisfactorySaveNet.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class KnownConstantsTests
{
    [Test]
    public void IsConveyorLift_ModPath_ReturnsTrue()
    {
        var path = "/Conveyors_Mod/Build_LiftMk1.Build_LiftMk1_C_Extra";
        KnownConstants.IsConveyorLift(path).Should().BeTrue();
    }

    [Test]
    public void IsConveyorBelt_ModPath_ReturnsTrue()
    {
        var path = "/Conveyors_Mod/Build_BeltMk1.Build_BeltMk1_C_Extra";
        KnownConstants.IsConveyorBelt(path).Should().BeTrue();
    }

    [Test]
    public void IsPowerLine_ModPath_ReturnsTrue()
    {
        var path = "/FlexSplines/PowerLine/Build_FlexPowerline.Build_FlexPowerline_C_Extra";
        KnownConstants.IsPowerLine(path).Should().BeTrue();
    }

    [Test]
    public void IsVehicle_ModPath_ReturnsTrue()
    {
        var path = "/x3_mavegrag/Vehicles/Trucks/TruckMk1/BP_X3Truck_Mk1.BP_X3Truck_Mk1_C_Extra";
        KnownConstants.IsVehicle(path).Should().BeTrue();
    }

    [Test]
    public void IsLocomotive_ModPath_ReturnsTrue()
    {
        var path = "/x3_mavegrag/Vehicles/Trains/Locomotive_Mk1/BP_X3Locomotive_Mk1.BP_X3Locomotive_Mk1_C_Extra";
        KnownConstants.IsLocomotive(path).Should().BeTrue();
    }

    [Test]
    public void IsFreightWagon_ModPath_ReturnsTrue()
    {
        var path = "/x3_mavegrag/Vehicles/Trains/CargoWagon_Mk1/BP_X3CargoWagon_Mk1.BP_X3CargoWagon_Mk1_C_Extra";
        KnownConstants.IsFreightWagon(path).Should().BeTrue();
    }
}
