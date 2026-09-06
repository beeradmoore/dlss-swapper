using System;
using System.Collections.Generic;
using System.Text;

namespace NewDLLPropertyGenerator;

internal class PropertyDetails
{
    public string Name { get; init; }
    public string CondensedName { get; init; }
    public string FullName { get; init; }
    public string CleanName { get; init; }
    public string DllName { get; init; }

    public PropertyDetails(string name, string fullName, string cleanName, string dllName)
    {
        Name = name;
        CondensedName = Name.Replace("_", string.Empty).Replace(" ", string.Empty);
        FullName = fullName;
        CleanName = cleanName;
        DllName = dllName;
    }
}
