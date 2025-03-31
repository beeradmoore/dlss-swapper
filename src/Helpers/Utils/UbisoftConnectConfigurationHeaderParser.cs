using System;
using System.Collections.Generic;
using System.Diagnostics;
using static DLSS_Swapper.Data.UbisoftConnect.UbisoftConnectLibrary;

namespace DLSS_Swapper.Helpers.Utils;
public static class UbisoftConnectConfigurationParser
{
    // Based on the methods from
    // https://github.com/lutris/lutris/blob/d908066d97e61b2f33715fe9bdff6c02cc7fbc80/lutris/util/ubisoft/parser.py
    public static List<UbisoftRecord> Parse(byte[] configurationFileData)
    {
        var configurationContent = new ReadOnlySpan<byte>(configurationFileData);

        var globalOffset = 0;
        var records = new List<UbisoftRecord>();

        try
        {
            while (globalOffset < configurationContent.Length)
            {
                var data = configurationContent.Slice(globalOffset);

                var configHeaderResult = ParseConfigurationHeader(data);
                var objectSize = configHeaderResult.objectSize;
                var installId = configHeaderResult.installId;
                var launchId = configHeaderResult.launchId;
                var headerSize = configHeaderResult.headerSize;

                launchId = launchId == 0 || launchId == installId ? installId : launchId;

                if (objectSize > 500)
                {
                    records.Add(new UbisoftRecord()
                    {
                        Size = objectSize,
                        Offset = globalOffset + headerSize,
                        InstallId = installId,
                        LaunchId = launchId,
                    });
                }

                var global_offset_tmp = globalOffset;
                globalOffset += objectSize + headerSize;



                if (globalOffset < configurationContent.Length && configurationContent[globalOffset] != 0x0A)
                {
                    var result = ParseConfigurationHeader(data, true);
                    objectSize = result.objectSize;
                    headerSize = result.headerSize;
                    globalOffset = global_offset_tmp + objectSize + headerSize;
                }
            }
        }
        catch (Exception err)
        {
            Logger.Warning($"parse_configuration failed with exception. Possibly 'configuration' file corrupted. - {err.Message}");
            Debugger.Break();
        }

        return records;
    }

    // Based on the methods from
    // https://github.com/lutris/lutris/blob/d908066d97e61b2f33715fe9bdff6c02cc7fbc80/lutris/util/ubisoft/parser.py
    private static (int objectSize, int installId, int launchId, int headerSize) ParseConfigurationHeader(ReadOnlySpan<byte> header, bool secondEight = false)
    {

        try
        {
            var offset = 1; ;
            var multiplier = 1;
            var recordSize = 0;
            var tmpSize = 0;

            if (secondEight)
            {
                while (header[offset] != 0x08 || header[offset] == 0x08 && header[offset + 1] == 0x08)
                {
                    recordSize += header[offset] * multiplier;
                    multiplier *= 256;
                    offset += 1;
                    tmpSize += 1;
                }
            }
            else
            {
                while (header[offset] != 0x08 || recordSize == 0)
                {
                    recordSize += header[offset] * multiplier;
                    multiplier *= 256;
                    offset += 1;
                    tmpSize += 1;
                }
            }

            recordSize = ConvertData(recordSize);

            offset += 1; // skip 0x08

            // look for launch_id
            multiplier = 1;
            var launchId = 0;


            while (header[offset] != 0x10 || header[offset + 1] == 0x10)
            {
                launchId += header[offset] * multiplier;
                multiplier *= 256;
                offset += 1;
            }

            launchId = ConvertData(launchId);

            offset += 1; // skip 0x10

            multiplier = 1;
            var launchId2 = 0;

            while (header[offset] != 0x1A || header[offset] == 0x1A && header[offset + 1] == 0x1A)
            {
                launchId2 += header[offset] * multiplier;
                multiplier *= 256;
                offset += 1;
            }

            launchId2 = ConvertData(launchId2);

            // if object size is smaller than 128b, there might be a chance that secondary size will not occupy 2b
            //if record_size - offset < 128 <= record_size:
            if (recordSize - offset < 128 && 120 <= recordSize)
            {
                tmpSize -= 1;
                recordSize += 1;
            }
            // we end up in the middle of header, return values normalized
            // to end of record as well real yaml size and game launch_id
            return (recordSize - offset, launchId, launchId2, offset + tmpSize + 1);
        }
        catch (Exception err)
        {
            Logger.Warning($"ParseConfigurationHeader Error: {err.Message}");
            // something went horribly wrong, do not crash it,
            // just return 0s, this way it will be handled later in the code
            // 10 is to step a little in configuration file in order to find next game
            return (0, 0, 0, 10);
        }
    }

    // Based on the methods from
    // https://github.com/lutris/lutris/blob/d908066d97e61b2f33715fe9bdff6c02cc7fbc80/lutris/util/ubisoft/parser.py
    private static int ConvertData(int data)
    {
        //calculate object size (konrad's formula)
        if (data > 256 * 256)
        {
            data = data - 128 * 256 * (int)Math.Ceiling(data / (256.0 * 256.0));
            data = data - 128 * (int)Math.Ceiling(data / 256.0);
        }
        else if (data > 256)
        {
            data = data - 128 * (int)Math.Ceiling(data / 256.0);
        }

        return data;
    }
}
