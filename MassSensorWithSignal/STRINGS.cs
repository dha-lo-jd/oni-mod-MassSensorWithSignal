namespace MassSensorWithSignal;

public static class STRINGS
{
    public static class BUILDINGS
    {
        public static class PREFABS
        {
            public static class MASSSENSORWITHSIGNAL
            {
                public static LocString NAME = "Total Mass Sensor";
                public static LocString DESC = "Measures the total mass of a connected body of the same element.";
                public static LocString EFFECT =
                    "Sums the mass of all connected tiles of the same element and sends an automation signal according to the configured threshold.";
                public static LocString LOGIC_PORT = "Total Mass Sensor Output";
                public static LocString LOGIC_PORT_ACTIVE = "Sends a <color=#00FF00>Green</color> signal while the condition is met.";
                public static LocString LOGIC_PORT_INACTIVE = "Sends a <color=#FF0000>Red</color> signal while the condition is not met.";
            }
        }
    }

    public static class UI
    {
        public static class MASSSENSORWITHSIGNAL
        {
            public static LocString TITLE = "Total Mass Threshold";
            public static LocString THRESHOLD_VALUE_NAME = "Total Mass";
            public static LocString THRESHOLD_TOOLTIP = "Sets the total mass threshold at which the automation signal is sent.";
            public static LocString ABOVE_TOOLTIP = "Sends a Green signal when the total mass is above the threshold.";
            public static LocString BELOW_TOOLTIP = "Sends a Green signal when the total mass is below the threshold.";
        }
    }
}
