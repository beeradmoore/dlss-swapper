using System;

namespace DLSS_Swapper.Data.NVIDIA;

public class NGXModel
{
    public string FilePath { get; }
    public Version Version { get; }
    public GameAssetType GameAssetType { get; }

    public string GameAssetTypeString => DLLManager.Instance.GetAssetTypeName(GameAssetType);

    public NGXModel(string filePath, Version version, GameAssetType gameAssetType)
    {
        FilePath = filePath;
        Version = version;
        GameAssetType = gameAssetType;
    }
}
