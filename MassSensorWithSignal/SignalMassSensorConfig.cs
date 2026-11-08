using System.Collections.Generic;
using TUNING;
using UnityEngine;

namespace MassSensorWithSignal;

public sealed class SignalMassSensorConfig : IBuildingConfig
{
    public const string ID = "MassSensorWithSignal";

    public override BuildingDef CreateBuildingDef()
    {
        BuildingDef def = BuildingTemplates.CreateBuildingDef(
            id: ID,
            width: 1,
            height: 1,
            anim: "MassSensorWithSignal_kanim",
            hitpoints: 30,
            construction_time: 30f,
            construction_mass: BUILDINGS.CONSTRUCTION_MASS_KG.TIER0,
            construction_materials: MATERIALS.REFINED_METALS,
            melting_point: 1600f,
            build_location_rule: BuildLocationRule.Anywhere,
            decor: BUILDINGS.DECOR.PENALTY.TIER0,
            noise: NOISE_POLLUTION.NONE);

        def.Overheatable = false;
        def.Floodable = false;
        def.Entombable = false;
        def.ViewMode = OverlayModes.Logic.ID;
        def.AudioCategory = "Metal";

        def.LogicOutputPorts = new List<LogicPorts.Port>
        {
            LogicPorts.Port.OutputPort(
                LogicSwitch.PORT_ID,
                new CellOffset(0, 0),
                (string)STRINGS.BUILDINGS.PREFABS.MASSSENSORWITHSIGNAL.LOGIC_PORT,
                (string)STRINGS.BUILDINGS.PREFABS.MASSSENSORWITHSIGNAL.LOGIC_PORT_ACTIVE,
                (string)STRINGS.BUILDINGS.PREFABS.MASSSENSORWITHSIGNAL.LOGIC_PORT_INACTIVE,
                true)
        };

        return def;
    }

    public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
    {
    }

    public override void DoPostConfigureComplete(GameObject go)
    {
        go.AddOrGet<SignalMassSensor>();
    }
}
