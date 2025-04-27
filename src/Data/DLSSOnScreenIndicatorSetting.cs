using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace DLSS_Swapper.Data;

public struct DLSSOnScreenIndicatorSetting
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
