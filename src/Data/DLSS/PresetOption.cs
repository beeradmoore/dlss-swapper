
namespace DLSS_Swapper.Data.DLSS;

public class PresetOption
{
    public string Name { get; init; }
    public uint Value { get; init; }

    public PresetOption(string name, uint value)
    {
        Name = name;
        Value = value;
    }
}
