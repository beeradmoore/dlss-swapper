using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace NewDLLPropertyGenerator;

internal class CodeRenderer
{
    List<PropertyDetails> _properties;

    public CodeRenderer(List<PropertyDetails> properties)
    {
        _properties = properties;
    }

    public void RenderAll()
    {
        RenderDLLManager();
        RenderDLLRecord();
        RenderGame();
        RenderGameAsset();
        RenderGameAssetType();
        RenderLibraryPageModel();
        RenderDLLPickerControlModel();
        RenderManifest();
        RenderKnownDLLs();
        Console.WriteLine();
    }

    void RenderHeader(string filename)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', Console.WindowWidth));
        Console.WriteLine(filename);
        Console.WriteLine(new string('=', Console.WindowWidth));
        Console.WriteLine();
    }
    
    void RenderSectionDivider()
    {
        Console.WriteLine();
        Console.WriteLine(new string('-', Console.WindowWidth));
        Console.WriteLine();
    }

    void RenderFooter()
    {
        Console.WriteLine();
    }

    public void RenderDLLManager()
    {
        var fileName = "src\\Data\\DLLManager.cs";
        var codeChecker = new CodeChecker(fileName);

        RenderHeader(fileName);

        var didOutput = false;
        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"public ObservableCollection<DLLRecord> {property.CondensedName}Records {{ get; }} = new ObservableCollection<DLLRecord>();");
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"CancelDownloads({property.CondensedName}Records);");
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"SetGameAssetType(Manifest.{property.Name}, GameAssetType.{property.Name});");
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"SetGameAssetType(ImportedManifest.{property.Name}, GameAssetType.{property.Name});");
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"LoadLocalRecords(Manifest.{property.Name});");
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"LoadLocalRecords(ImportedManifest.{property.Name}, true);");
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"didChangeImportedManifest |= CheckImportedManifestForCleanUp(Manifest.{property.Name}, ImportedManifest?.{property.Name});");
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"MergeManifestsIntoMasterList({property.CondensedName}Records, Manifest.{property.Name}, ImportedManifest?.{property.Name});");
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"GameAssetType.{property.Name} => ResourceHelper.GetString(\"General_Name_{property.Name}\"),");
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"GameAssetType.{property.Name} => GameAssetType.{property.Name}_BACKUP,");
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"GameAssetType.{property.Name} or GameAssetType.{property.Name}_BACKUP => {property.CondensedName}Records,");
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"GameAssetType.{property.Name} or GameAssetType.{property.Name}_BACKUP => KnownDLLs.{property.Name},");
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($$"""                
                        else if (fileName == "{{property.DllName}}")
                        {
                            gameAssetType = GameAssetType.{{property.Name}};
                            recordList = {{property.CondensedName}}Records;
                            importedRecordList = ImportedManifest.{{property.Name}};
                        }
                """, $$"""                
                        if (fileName == "{{property.DllName}}")
                        {
                            gameAssetType = GameAssetType.{{property.Name}};
                            recordList = {{property.CondensedName}}Records;
                            importedRecordList = ImportedManifest.{{property.Name}};
                        }
                """);                
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($$"""   
                        else if (dllRecord.AssetType == GameAssetType.{{property.Name}})
                        {
                            recordList = {{property.CondensedName}}Records;
                            importedRecordList = ImportedManifest?.{{property.Name}};
                        }
                """, $$"""                
                        if (dllRecord.AssetType == GameAssetType.{{property.Name}})
                        {
                            recordList = {{property.CondensedName}}Records;
                            importedRecordList = ImportedManifest?.{{property.Name}};
                        }
                """);
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"GameAssetType.{property.Name} => \"{property.DllName}\",");
        }

        RenderFooter();
    }

    public void RenderDLLRecord()
    {
        var fileName = "src\\Data\\DLLRecord.cs";
        var codeChecker = new CodeChecker(fileName);

        RenderHeader(fileName);

        var didOutput = false;
        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"public const string {property.Name} = \"{property.CleanName}\";");
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"GameAssetType.{property.Name} => {property.Name},");
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        RenderFooter();
    }

    public void RenderGame()
    {
        var fileName = "src\\Data\\Game.cs";
        var codeChecker = new CodeChecker(fileName);

        RenderHeader(fileName);

        var didOutput = false;
        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($$"""
                [ObservableProperty]
                [Ignore]
                public partial GameAsset? Current{{property.Name}} { get; set; } = null;

                [ObservableProperty]
                [Ignore]
                public partial bool Multiple{{property.CondensedName}}Found { get; set; } = false;
            """);
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($$"""
                                    else if (dllName == "{{property.DllName}}")
                                    {
                                        var gameAsset = new GameAsset()
                                        {
                                            Id = ID,
                                            AssetType = GameAssetType.{{property.Name}},
                                            Path = dllPath,
                                        };
                                        ProcessGame_ProcessGameAsset(gameAsset);
                                        GameAssets.Add(gameAsset);
                                    }
                """, $$"""
                                    if (dllName == "{{property.DllName}}")
                                    {
                                        var gameAsset = new GameAsset()
                                        {
                                            Id = ID,
                                            AssetType = GameAssetType.{{property.Name}},
                                            Path = dllPath,
                                        };
                                        ProcessGame_ProcessGameAsset(gameAsset);
                                        GameAssets.Add(gameAsset);
                                    }
                """);
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($$"""
                            else if (gameAssetType == GameAssetType.{{property.Name}})
                            {
                                Current{{property.Name}} = null;
                                Current{{property.Name}} = newGameAsset;
                            }
                """, $$"""
                            if (gameAssetType == GameAssetType.{{property.Name}})
                            {
                                Current{{property.Name}} = null;
                                Current{{property.Name}} = newGameAsset;
                            }
                """);
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"cachedGameAsset.AssetType == GameAssetType.{property.Name}_BACKUP ||", $"cachedGameAsset.AssetType == GameAssetType.{property.Name}_BACKUP)");
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($$"""
                        if (Current{{property.Name}} != game.Current{{property.Name}})
                        {
                            Current{{property.Name}} = game.Current{{property.Name}};
                            didChange = true;
                        }
                """);
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"Current{property.Name} = null;");
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"Multiple{property.CondensedName}Found = GameAssets.Count(x => x.AssetType == GameAssetType.{property.Name}) > 1;");
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($$"""
                            else if (gameAsset.AssetType == GameAssetType.{{property.Name}})
                            {
                                Current{{property.Name}} = gameAsset;
                            }
                """, $$"""
                            if (gameAsset.AssetType == GameAssetType.{{property.Name}})
                            {
                                Current{{property.Name}} = gameAsset;
                            }
                """);
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        RenderFooter();
    }

    public void RenderGameAsset()
    {
        var fileName = "src\\Data\\GameAsset.cs";
        var codeChecker = new CodeChecker(fileName);

        RenderHeader(fileName);

        var didOutput = false;
        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"GameAssetType.{property.Name} => GameAssetType.{property.Name}_BACKUP,");
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        RenderFooter();
    }

    public void RenderGameAssetType()
    {
        var fileName = "src\\Data\\GameAssetType.cs";
        var codeChecker = new CodeChecker(fileName);

        RenderHeader(fileName);

        var didOutput = false;
        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"{property.Name} = ");
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }
        
        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"{property.Name}_BACKUP = ");
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        RenderFooter();
    }

    public void RenderLibraryPageModel()
    {
        var fileName = "src\\Pages\\LibraryPageModel.cs";
        var codeChecker = new CodeChecker(fileName);

        RenderHeader(fileName);

        var didOutput = false;
        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"upscalerSelectorBar.Items.Add(new SelectorBarItem() {{ Text = DLLManager.Instance.GetAssetTypeName(GameAssetType.{property.Name}), Tag = GameAssetType.{property.Name} }});");
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"allDllRecords.AddRange(DLLManager.Instance.{property.CondensedName}Records.Where(x => x.LocalRecord?.IsDownloaded == true));");
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($$"""
                                            dllRecord = DLLManager.Instance.{{property.CondensedName}}Records.FirstOrDefault(x => string.Equals(x.ZipMD5Hash, newZipHash, StringComparison.InvariantCultureIgnoreCase));
                                            if (dllRecord is not null)
                                            {
                                                if (HandleLocalDLLRecordZip(importFile, dllRecord, importResults))
                                                {
                                                    ++totalDllsProcessed;
                                                    App.CurrentApp.RunOnUIThread(() =>
                                                    {
                                                        progressRun.Text = totalDllsProcessed.ToString(CultureInfo.CurrentCulture);
                                                    });
                                                    continue;
                                                }
                                            }
                """);
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"GameAssetType.{property.Name} => DLLManager.Instance.{property.CondensedName}Records,");
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($"startedDownloads += DownloadLatestRecord(DLLManager.Instance.{property.CondensedName}Records);");
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }
        
        RenderFooter();
    }

    public void RenderDLLPickerControlModel()
    {
        var fileName = "src\\UserControls\\DLLPickerControlModel.cs";
        var codeChecker = new CodeChecker(fileName);

        RenderHeader(fileName);

        var didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($$"""
                            case GameAssetType.{{property.Name}}:
                                DLLRecords = [.. DLLManager.Instance.{{property.CondensedName}}Records];
                                if (Settings.Instance.OnlyShowDownloadedDlls == true)
                                {
                                    _ = DLLRecords.RemoveAll(x => x.MD5Hash != Game.Current{{property.Name}}?.Hash && x.LocalRecord?.IsDownloaded is false);
                                }
                                break;
                """);
        }

        if (didOutput)
        {
            RenderSectionDivider();
        }

        RenderFooter();
    }



    public void RenderManifest()
    {
        var fileName = "src\\Data\\Manifest.cs";
        var codeChecker = new CodeChecker(fileName);

        RenderHeader(fileName);

        var didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($$"""
                    [JsonPropertyName(DLLRecord.{{property.Name}})]
                    public List<DLLRecord> {{property.Name}} { get; set; } = new List<DLLRecord>();
                """);
        }

        RenderFooter();
    }

    public void RenderKnownDLLs()
    {
        var fileName = "src\\Data\\KnownDlls.cs";
        var codeChecker = new CodeChecker(fileName);

        RenderHeader(fileName);

        var didOutput = false;

        foreach (var property in _properties)
        {
            didOutput |= codeChecker.CheckAndOutput($$"""
                    [JsonPropertyName(DLLRecord.{{property.Name}})]
                    public List<HashedKnownDLL> {{property.Name}} { get; set; } = new List<HashedKnownDLL>();
                """);
        }

        RenderFooter();
    }
}
