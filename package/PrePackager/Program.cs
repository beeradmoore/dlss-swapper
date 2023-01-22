using DLSS_Swapper;
using PrePackager;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// Keep going up directories until we are in the package directory, then go up one more. From there we want to use src/StoredData.
// This is where we store data for DLSS Swapper for the Microsoft Store.
var storedDataPath = String.Empty;
var currentSearchPath = Directory.GetCurrentDirectory() ?? String.Empty;
do
{
    System.Diagnostics.Debug.WriteLine($"Checking {currentSearchPath}");
    if (Path.GetFileName(currentSearchPath) == "package")
    {
        var tempStoredDataPath = Path.Combine(Directory.GetParent(currentSearchPath)?.ToString() ?? String.Empty, "src", "StoredData");
        if (Directory.Exists(tempStoredDataPath))
        {
            storedDataPath = tempStoredDataPath;
        }
        else
        {
            Logger.Error($"Failed to get StoredData path, got \"{tempStoredDataPath}\" instead.");
        }
        break;
    }
    currentSearchPath = Directory.GetParent(currentSearchPath)?.ToString() ?? String.Empty;
} while (currentSearchPath != String.Empty);

// If it can't be found, abort.
if (String.IsNullOrEmpty(storedDataPath))
{
    Logger.Error($"Unable to get StoredData path. Exiting.");
    return 1;
}

// If the dlss_zip directory doesn't exist, create it.
var dllZipPath = Path.Combine(storedDataPath, "dlss_zip");
if (Directory.Exists(dllZipPath) == false)
{
    Directory.CreateDirectory(dllZipPath);
}

var httpClient = new HttpClient();
try
{
    SlimDLSSRecords? dlssRecords = null;
    using (var memoryStream = new MemoryStream())
    {
        // Get a reference of the current dlss_records.json from the dlss-archive.
        var url = "https://raw.githubusercontent.com/beeradmoore/dlss-archive/main/dlss_records.json";
        using (var stream = await httpClient.GetStreamAsync(url))
        {
            stream.CopyTo(memoryStream);
        }
        memoryStream.Position = 0;

        dlssRecords = await JsonSerializer.DeserializeAsync<SlimDLSSRecords>(memoryStream);
        if (dlssRecords == null)
        {
            Logger.Error("Could not parse dlss_records.json");
            return 1;
        }

        // If we loaded the items its also safe to save the file.
        // It should be noted that just because this file saves does not mean that all the DLSS
        // zips in dlss_zips are the current and updated version. 
        // If this application returns exit code 1, assume StoredData is bad data.
        // We also take care to not save a serialized json version of this file, but the original that
        // was downloaded.
        memoryStream.Position = 0;
        using (var fileStream = File.Create(Path.Combine(storedDataPath, "static_json", "dlss_records.json")))
        {
            memoryStream.CopyTo(fileStream);
        }
    }

    var allDLSSRecords = new List<SlimDLSSRecord>();
    allDLSSRecords.AddRange(dlssRecords.Stable);
    allDLSSRecords.AddRange(dlssRecords.Experimental);


    var dlssToDownload = new List<(string OutputPath, SlimDLSSRecord DLSSRecord)>();
    
    var parallelOptions = new ParallelOptions()
    {
        //MaxDegreeOfParallelism = 3
    };

    // Check agasint the known DLSS releases with what is in the zip.
    Console.WriteLine("Checking for required DLSS downloads.");
    Parallel.ForEach(allDLSSRecords, parallelOptions, (dlssRecord, token) =>
    {
        var expectedPath = Path.Combine(dllZipPath, $"{dlssRecord.Version}_{dlssRecord.MD5Hash}.zip");
        if (File.Exists(expectedPath))
        {
            var zipHash = String.Empty;
            using (var fileStream = File.Open(expectedPath, FileMode.Open))
            {
                using (var md5 = MD5.Create())
                {
                    var hash = md5.ComputeHash(fileStream);
                    zipHash = BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
                }
            }

            if (zipHash != dlssRecord.ZipMD5Hash)
            {
                Logger.Error($"Invalid MD5 for {dlssRecord.DownloadUrl}. Expected {dlssRecord.ZipMD5Hash}, got {zipHash}");
                File.Delete(expectedPath);
                dlssToDownload.Add(new(expectedPath, dlssRecord));
            }
        }
        else
        {
            dlssToDownload.Add(new(expectedPath, dlssRecord));
        }
    });

    // If there are any to download, download them.
    if (dlssToDownload.Count > 0)
    {
        Console.WriteLine($"Downloading {dlssToDownload.Count} records.");
        await Parallel.ForEachAsync(dlssToDownload, parallelOptions, async (item, token) =>
        {
            Console.WriteLine($"Downloading {item.DLSSRecord.DownloadUrl}");
            using (var stream = await httpClient.GetStreamAsync(item.DLSSRecord.DownloadUrl))
            {
                using (var fileStream = File.Create(item.OutputPath))
                {
                    await stream.CopyToAsync(fileStream);
                }
            }
        });
    }
    else
    {
        Console.WriteLine($"No records to download.");
    }

    // From here we want to look at every single DLSS record as this should now be every single one.
    // We then confirm the hashes match what we expect. If they are not what we expect then the data
    // is not valid and we should assume StoredData contains bad data somewhere.
    var failedDLSSRecords = new List<SlimDLSSRecord>();
    Console.WriteLine($"Validate DLSS records.");
    Parallel.ForEach(allDLSSRecords, parallelOptions, (dlssRecord, token) =>
    {
        var expectedPath = Path.Combine(dllZipPath, $"{dlssRecord.Version}_{dlssRecord.MD5Hash}.zip");
        if (File.Exists(expectedPath))
        {
            var zipHash = String.Empty;
            using (var fileStream = File.Open(expectedPath, FileMode.Open))
            {
                using (var md5 = MD5.Create())
                {
                    var hash = md5.ComputeHash(fileStream);
                    zipHash = BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
                }
            }

            if (zipHash != dlssRecord.ZipMD5Hash)
            {
                Logger.Error($"Invalid MD5 for {dlssRecord.DownloadUrl}. Expected {dlssRecord.ZipMD5Hash}, got {zipHash}");
                failedDLSSRecords.Add(dlssRecord);
            }
        }
        else
        {
            failedDLSSRecords.Add(dlssRecord);
        }
    });

    if (failedDLSSRecords.Count > 0)
    {
        Console.WriteLine("Invalid DLSS zip hashes. Exiting.");
        return 1;
    }
}
catch (Exception err)
{
    Console.WriteLine($"ERROR: {err.Message}");
    return 1;
}

// Ayyooo
Console.WriteLine("Success: All DLSS records found appear to be in good shape.");
return 0;