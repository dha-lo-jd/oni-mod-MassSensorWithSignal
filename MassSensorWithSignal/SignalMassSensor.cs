using System.Collections.Generic;
using KSerialization;

namespace MassSensorWithSignal;

[SerializationConfig(MemberSerialization.OptIn)]
public class SignalMassSensor : KMonoBehaviour, ISaveLoadable, IThresholdSwitch
{
    private const float CheckIntervalSeconds = 0.2f;
    private const float MaxMassKg = int.MaxValue;

    [Serialize] public float thresholdMass;
    [Serialize] public bool activateAbove = true;

    private int _cell;
    private bool _isOn;
    private bool _signalSent;

    private KAnimControllerBase _kanimController;
    private LogicPorts _logicPorts;

    // Reused across scans to avoid per-tick allocations.
    private readonly HashSet<int> _visited = new HashSet<int>();
    private readonly Queue<int> _queue = new Queue<int>();

    #region IThresholdSwitch

    public float Threshold
    {
        get => thresholdMass;
        set => thresholdMass = value;
    }

    public bool ActivateAboveThreshold
    {
        get => activateAbove;
        set => activateAbove = value;
    }

    public float CurrentValue => CalculateConnectedMass(_cell);

    public float RangeMin => 0f;
    public float RangeMax => MaxMassKg;
    public float GetRangeMinInputField() => RangeMin;
    public float GetRangeMaxInputField() => RangeMax;

    public LocString Title => STRINGS.UI.MASSSENSORWITHSIGNAL.TITLE;
    public LocString ThresholdValueName => STRINGS.UI.MASSSENSORWITHSIGNAL.THRESHOLD_VALUE_NAME;
    public string ThresholdValueUnits => "kg";
    LocString IThresholdSwitch.ThresholdValueUnits() => ThresholdValueUnits;

    public LocString ThresholdTooltip => STRINGS.UI.MASSSENSORWITHSIGNAL.THRESHOLD_TOOLTIP;
    public string AboveToolTip => STRINGS.UI.MASSSENSORWITHSIGNAL.ABOVE_TOOLTIP;
    public string BelowToolTip => STRINGS.UI.MASSSENSORWITHSIGNAL.BELOW_TOOLTIP;

    public ThresholdScreenLayoutType LayoutType => ThresholdScreenLayoutType.SliderBar;
    public int IncrementScale => 1;

    public NonLinearSlider.Range[] GetRanges => new[] { new NonLinearSlider.Range(100f, MaxMassKg) };

    public float ProcessedSliderValue(float input) => input;
    public float ProcessedInputValue(float input) => input;

    public string Format(float value, bool units)
    {
        return units ? $"{value:N0} {ThresholdValueUnits}" : value.ToString("F0");
    }

    #endregion

    protected override void OnSpawn()
    {
        base.OnSpawn();
        _cell = Grid.PosToCell(gameObject);
        _kanimController = GetComponent<KAnimControllerBase>();
        _logicPorts = GetComponent<LogicPorts>();

        UpdateVisualState(_isOn);
        InvokeRepeating(nameof(CheckMassAndOutputSignal), 0f, CheckIntervalSeconds);
    }

    protected override void OnCleanUp()
    {
        CancelInvoke(nameof(CheckMassAndOutputSignal));
        base.OnCleanUp();
    }

    private void CheckMassAndOutputSignal()
    {
        float totalMassKg = CalculateConnectedMass(_cell);
        bool isOn = activateAbove ? totalMassKg > thresholdMass : totalMassKg < thresholdMass;

        // Always push the first result so the output matches the sensor state after loading a save.
        if (_signalSent && isOn == _isOn) return;

        _isOn = isOn;
        _signalSent = true;
        _logicPorts?.SendSignal(LogicSwitch.PORT_ID, isOn ? 1 : 0);
        UpdateVisualState(isOn);
    }

    private void UpdateVisualState(bool isOn)
    {
        if (_kanimController == null) return;
        _kanimController.Play(isOn ? "on" : "off", KAnim.PlayMode.Loop, 0.2f);
    }

    /// <summary>Sums the mass of the 4-connected region of the same element that contains <paramref name="startCell"/>.</summary>
    private float CalculateConnectedMass(int startCell)
    {
        if (!Grid.IsValidCell(startCell)) return 0f;

        Element targetElement = Grid.Element[startCell];
        if (targetElement == null || targetElement.IsVacuum) return 0f;

        float totalMassKg = 0f;
        _visited.Clear();
        _queue.Clear();
        _queue.Enqueue(startCell);
        _visited.Add(startCell);

        while (_queue.Count > 0)
        {
            int current = _queue.Dequeue();
            totalMassKg += Grid.Mass[current];

            TryEnqueue(Grid.CellLeft(current), targetElement);
            TryEnqueue(Grid.CellRight(current), targetElement);
            TryEnqueue(Grid.CellAbove(current), targetElement);
            TryEnqueue(Grid.CellBelow(current), targetElement);
        }

        return totalMassKg;
    }

    private void TryEnqueue(int cell, Element targetElement)
    {
        if (!Grid.IsValidCell(cell) || Grid.Element[cell] != targetElement || !_visited.Add(cell)) return;
        _queue.Enqueue(cell);
    }
}
