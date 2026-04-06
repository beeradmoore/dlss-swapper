using System;

namespace DLSS_Swapper.Data.NVIDIA;

public class NGXModel
{
    public string FilePath { get; }
    public Version Version { get; }
    public GameAssetType GameAssetType { get; }
    public long Size { get; }
    public string GameAssetTypeString => DLLManager.Instance.GetAssetTypeName(GameAssetType);
    public string ETag { get; }

    public NGXModel(string filePath, Version version, GameAssetType gameAssetType, long size, string eTag)
    {
        FilePath = filePath;
        Version = version;
        GameAssetType = gameAssetType;
        Size = size;
        ETag = eTag;
    }
}
