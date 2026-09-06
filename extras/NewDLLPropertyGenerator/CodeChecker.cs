using System;
using System.Collections.Generic;
using System.Text;

namespace NewDLLPropertyGenerator;

internal class CodeChecker
{
    string _fileName;
    string _fileData;

    public CodeChecker(string fileName)
    {
        _fileName = fileName;
        _fileData = File.ReadAllText(Path.Combine("..", "..", "..", "..", "..", fileName));
    }

    public bool CheckAndOutput(string line, string? line2 = null)
    {
        if (line2 is not null && _fileData.Contains(line2))
        {
            // NOOP
            return false;
        }

        if (_fileData.Contains(line))
        {
            // NOOP
            return false;
        }

        Console.WriteLine(line);

        return true;
    }
}
