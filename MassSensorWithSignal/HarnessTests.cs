#if DEBUG
// In-game tests run by Gravitas.TestHarness:
//   python Gravitas.TestProbe/probe.py run --project <this repo> --tests '{"tags":["masssensor"]}'
// Needs HarnessDllPath in Local.Build.props (Check / Wait come from the harness assembly).
//
// The scene is a small sealed pocket of water carved into solid natural rock, so the connected region is known exactly:
//
//     R R R R R R R R R R
//     R R R R R R D R R R      D  = water that touches the pocket only diagonally (50 kg)
//     R R A S C R E R R R      A S C = the pocket, 100 + 200 + 300 kg (S holds the sensor); E = water behind one rock cell (77 kg)
//     R R R R R R R R R R
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gravitas.TestHarness;
using HarmonyLib;
using UnityEngine;

namespace MassSensorWithSignal;

internal static class HarnessTests
{
    private const string Id = SignalMassSensorConfig.ID;
    private const float Pocket = 600f; // A + S + C

    private sealed class Scene
    {
        private readonly List<(int cell, SimHashes element, float mass, float temperature)> saved = new();
        public int A, S, C, D, E, V;
        public GameObject Sensor;
        public SignalMassSensor Component => Sensor.GetComponent<SignalMassSensor>();

        // Finds 10 x 4 free cells nearest to the colony, turns them into rock, carves the pocket and builds the sensor on S.
        public IEnumerator Create()
        {
            var world = ClusterManager.Instance.activeWorld;
            var home = Arrange.XY(Arrange.HomeCell());
            var best = (x: -1, y: -1);
            var bestDistance = int.MaxValue;
            for (var x = world.WorldOffset.x + 1; x < world.WorldOffset.x + world.WorldSize.x - 11; x++)
            for (var y = world.WorldOffset.y + 1; y < world.WorldOffset.y + world.WorldSize.y - 5; y++)
            {
                var distance = Mathf.Abs(x - home.x) + Mathf.Abs(y - home.y);
                if (distance >= bestDistance || !Free(x, y)) continue;
                best = (x, y);
                bestDistance = distance;
            }

            Check.True(best.x >= 0, "no free 10 x 4 block in the active world");
            var (ox, oy) = best;
            A = Arrange.Cell(ox + 2, oy + 2);
            S = Arrange.Cell(ox + 3, oy + 2);
            C = Arrange.Cell(ox + 4, oy + 2);
            D = Arrange.Cell(ox + 5, oy + 3);
            E = Arrange.Cell(ox + 7, oy + 2);
            V = Arrange.Cell(ox + 8, oy + 1);
            // Fill the whole block with rock first (the save has little natural rock left), then carve the pocket.
            for (var x = ox; x < ox + 10; x++)
            for (var y = oy; y < oy + 4; y++)
            {
                var cell = Arrange.Cell(x, y);
                saved.Add((cell, Grid.Element[cell].id, Grid.Mass[cell], Grid.Temperature[cell]));
                Arrange.SetElement(cell, SimHashes.Granite, 1000f, 293f);
            }

            yield return Wait.Seconds(1f);

            Arrange.SetElement(A, SimHashes.Water, 100f, 293f);
            Arrange.SetElement(S, SimHashes.Water, 200f, 293f);
            Arrange.SetElement(C, SimHashes.Water, 300f, 293f);
            Arrange.SetElement(D, SimHashes.Water, 50f, 293f);
            Arrange.SetElement(E, SimHashes.Water, 77f, 293f);
            Arrange.SetElement(V, SimHashes.Vacuum, 0f, 0f);
            yield return Wait.Seconds(1f);
            Check.True(Grid.Element[S].id == SimHashes.Water, "the pocket was not carved");

            Sensor = Arrange.Build(Id, S);
            yield return null;
            Check.NotNull(Component, "the built sensor has no SignalMassSensor");
        }

        private static bool Free(int x0, int y0)
        {
            for (var x = x0; x < x0 + 10; x++)
            for (var y = y0; y < y0 + 4; y++)
            {
                var cell = Arrange.Cell(x, y);
                var element = Grid.Element[cell];
                if (element == null || element.id == SimHashes.Unobtanium ||
                    Grid.Objects[cell, (int)ObjectLayer.Building] != null || Grid.Objects[cell, (int)ObjectLayer.FoundationTile] != null ||
                    Grid.Objects[cell, (int)ObjectLayer.Pickupables] != null)
                    return false;
            }

            return true;
        }

        public string Masses() =>
            "A=" + Grid.Mass[A] + " S=" + Grid.Mass[S] + " C=" + Grid.Mass[C] + " D=" + Grid.Mass[D] + " E=" + Grid.Mass[E];

        public int Output => Sensor.GetComponent<LogicPorts>().GetOutputValue(LogicSwitch.PORT_ID);

        public void Dispose()
        {
            if (Sensor != null) Arrange.Destroy(Sensor);
            foreach (var (cell, element, mass, temperature) in saved)
                Arrange.SetElement(cell, element, mass, temperature);
        }
    }

    [HarnessTest(Name = "masssensor.in_automation_menu", Tags = "masssensor")]
    public static void InAutomationMenu()
    {
        var def = Assets.GetBuildingDef(Id);
        Check.NotNull(def, "building def " + Id);
        var automation = TUNING.BUILDINGS.PLANORDER.First(p => p.category == new HashedString("Automation"));
        Check.True(automation.buildingAndSubcategoryData.Any(p => p.Key == Id), "the sensor is not in the Automation build menu");
        Check.Equal(1, def.LogicOutputPorts.Count, "logic output ports");
        Check.True(def.BuildingComplete.GetComponent<SignalMassSensor>() != null, "completed building has no SignalMassSensor");
        Check.True(def.BuildingComplete.GetComponent<IThresholdSwitch>() != null, "sensor is not a threshold switch");
        Check.False(string.IsNullOrEmpty(def.Name), "the building has no name");
    }

    [HarnessTest(Name = "masssensor.sums_connected_same_element", Tags = "masssensor", TimeoutSeconds = 240)]
    public static IEnumerator SumsConnectedSameElement()
    {
        var scene = new Scene();
        try
        {
            yield return scene.Create();
            var sensor = scene.Component;
            // Only A + S + C: not the diagonal cell D, not the cell E behind rock, and nothing outside the pocket.
            Check.Near(Pocket, sensor.CurrentValue, 0.5, "connected mass of the pocket (A+S+C = 600)");
            Check.Near(Pocket, Grid.Mass[scene.A] + Grid.Mass[scene.S] + Grid.Mass[scene.C], 0.5, "sim mass of the pocket");

            // The reading follows the connected region when its mass changes, and still leaves out D and E.
            Arrange.SetElement(scene.C, SimHashes.Water, 450f, 293f);
            yield return Wait.Seconds(1f);
            var expected = Grid.Mass[scene.A] + Grid.Mass[scene.S] + Grid.Mass[scene.C];
            Check.True(expected > 700f, "the pocket did not take the extra water: " + scene.Masses());
            Check.Near(expected, sensor.CurrentValue, 0.5, "reading after raising C: " + scene.Masses());
        }
        finally
        {
            scene.Dispose();
        }
    }

    [HarnessTest(Name = "masssensor.vacuum_reads_zero", Tags = "masssensor", TimeoutSeconds = 240)]
    public static IEnumerator VacuumReadsZero()
    {
        var scene = new Scene();
        try
        {
            yield return scene.Create();
            // A sensor standing in vacuum (inside rock, so nothing flows in) weighs nothing.
            Check.True(Grid.Element[scene.V].IsVacuum, "the vacuum cell is not vacuum");
            var empty = Arrange.Build(Id, scene.V);
            try
            {
                yield return null;
                var sensor = empty.GetComponent<SignalMassSensor>();
                Check.Near(0f, sensor.CurrentValue, 0.01, "sensor standing in vacuum");
                sensor.Threshold = 10f;
                sensor.ActivateAboveThreshold = true;
                yield return Wait.Seconds(1f);
                Check.Equal(0, empty.GetComponent<LogicPorts>().GetOutputValue(LogicSwitch.PORT_ID), "output with nothing to weigh (above 10 kg)");
                sensor.ActivateAboveThreshold = false;
                yield return Wait.Seconds(1f);
                Check.Equal(1, empty.GetComponent<LogicPorts>().GetOutputValue(LogicSwitch.PORT_ID), "output with nothing to weigh (below 10 kg)");
            }
            finally
            {
                Arrange.Destroy(empty);
            }
        }
        finally
        {
            scene.Dispose();
        }
    }

    [HarnessTest(Name = "masssensor.signal_above_threshold", Tags = "masssensor", TimeoutSeconds = 240)]
    public static IEnumerator SignalAboveThreshold()
    {
        var scene = new Scene();
        try
        {
            yield return scene.Create();
            var sensor = scene.Component;
            sensor.ActivateAboveThreshold = true;

            sensor.Threshold = 500f;
            yield return Wait.Seconds(1f);
            Check.Equal(1, scene.Output, "Green expected: 600 kg is above 500");

            sensor.Threshold = 700f;
            yield return Wait.Seconds(1f);
            Check.Equal(0, scene.Output, "Red expected: 600 kg is not above 700");

            // The comparison is strict: a threshold equal to the mass does not switch it on.
            sensor.Threshold = sensor.CurrentValue;
            yield return Wait.Seconds(1f);
            Check.Equal(0, scene.Output, "Red expected: the mass equals the threshold");

            sensor.Threshold = 599f;
            yield return Wait.Seconds(1f);
            Check.Equal(1, scene.Output, "Green expected: 600 kg is above 599");
        }
        finally
        {
            scene.Dispose();
        }
    }

    [HarnessTest(Name = "masssensor.signal_below_threshold", Tags = "masssensor", TimeoutSeconds = 240)]
    public static IEnumerator SignalBelowThreshold()
    {
        var scene = new Scene();
        try
        {
            yield return scene.Create();
            var sensor = scene.Component;
            sensor.ActivateAboveThreshold = false;

            sensor.Threshold = 700f;
            yield return Wait.Seconds(1f);
            Check.Equal(1, scene.Output, "Green expected: 600 kg is below 700");

            sensor.Threshold = 500f;
            yield return Wait.Seconds(1f);
            Check.Equal(0, scene.Output, "Red expected: 600 kg is not below 500");

            sensor.Threshold = sensor.CurrentValue;
            yield return Wait.Seconds(1f);
            Check.Equal(0, scene.Output, "Red expected: the mass equals the threshold");
        }
        finally
        {
            scene.Dispose();
        }
    }

    // The signal flips when the mass itself crosses the threshold, with no change to the settings.
    [HarnessTest(Name = "masssensor.signal_follows_mass_crossing_threshold", Tags = "masssensor", TimeoutSeconds = 240)]
    public static IEnumerator SignalFollowsMass()
    {
        var scene = new Scene();
        try
        {
            yield return scene.Create();
            var sensor = scene.Component;
            sensor.ActivateAboveThreshold = true;
            sensor.Threshold = 650f;
            yield return Wait.Seconds(1f);
            Check.Equal(0, scene.Output, "Red expected: 600 kg is not above 650");

            Arrange.SetElement(scene.C, SimHashes.Water, 400f, 293f); // 100 + 200 + 400 = 700
            yield return Wait.Seconds(1f);
            Check.Equal(1, scene.Output, "Green expected after the mass rose to 700 kg");
            Check.Equal("on", sensor.GetComponent<KBatchedAnimController>().GetCurrentAnim()?.name, "the sensor must show its on animation");

            Arrange.SetElement(scene.C, SimHashes.Water, 100f, 293f); // 400
            yield return Wait.Seconds(1f);
            Check.Equal(0, scene.Output, "Red expected after the mass dropped to 400 kg");
        }
        finally
        {
            scene.Dispose();
        }
    }

    // The vanilla threshold input holds 6 characters; the mod widens it to 10 for its own sensor only.
    [HarnessTest(Name = "masssensor.side_screen_input_limit", Tags = "masssensor", TimeoutSeconds = 240)]
    public static IEnumerator SideScreenInputLimit()
    {
        var scene = new Scene();
        GameObject vanilla = null;
        try
        {
            yield return scene.Create();
            var vanillaDef = Assets.GetBuildingDef("LogicTemperatureSensor"); // a vanilla threshold switch
            Check.True(vanillaDef.BuildingComplete.GetComponent<IThresholdSwitch>() != null, "LogicTemperatureSensor is no threshold switch");
            var cell = Arrange.FindBuildableCell(vanillaDef.PrefabID, Arrange.HomeCell());
            Check.True(cell >= 0, "no free cell for " + vanillaDef.PrefabID);
            vanilla = Arrange.Build(vanillaDef.PrefabID, cell);
            yield return null;

            SelectTool.Instance.Select(scene.Sensor.GetComponent<KSelectable>(), true);
            yield return null;
            yield return null;
            var screen = Object.FindObjectsByType<ThresholdSwitchSideScreen>(FindObjectsSortMode.None).FirstOrDefault(s => s.isActiveAndEnabled);
            Check.NotNull(screen, "the threshold side screen did not open for the sensor");
            Check.Equal(10, Limit(screen), "input limit for the mass sensor");

            SelectTool.Instance.Select(vanilla.GetComponent<KSelectable>(), true);
            yield return null;
            yield return null;
            screen = Object.FindObjectsByType<ThresholdSwitchSideScreen>(FindObjectsSortMode.None).FirstOrDefault(s => s.isActiveAndEnabled);
            Check.NotNull(screen, "the threshold side screen did not open for " + vanillaDef.PrefabID);
            Check.Equal(6, Limit(screen), "input limit for " + vanillaDef.PrefabID);
        }
        finally
        {
            SelectTool.Instance.Select(null);
            Arrange.Destroy(vanilla);
            scene.Dispose();
        }
    }

    private static int Limit(ThresholdSwitchSideScreen screen) =>
        Traverse.Create(screen).Field("numberInput").Field("inputField").Property("characterLimit").GetValue<int>();
}
#endif
