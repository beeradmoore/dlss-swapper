namespace DLSS_Swapper.Data;

internal struct DLSSOnScreenIndicatorSetting
{
    public string Label { get; init; } = string.Empty;
    public int Value { get; init; } = 0;

    public DLSSOnScreenIndicatorSetting(string label, int value)
    {
        Label = label;
        Value = value;
    }

    public override string ToString()
    {
        return Label;
    }
}
