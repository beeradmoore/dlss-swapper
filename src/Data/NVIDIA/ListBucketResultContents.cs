using System;

namespace DLSS_Swapper.Data.NVIDIA;

public class ListBucketResultContents
{
    public string Key { get; set; } = string.Empty;
    public DateTime LastModified { get; set; } = DateTime.MinValue;
    public string ETag { get; set; } = string.Empty;
    public string ChecksumAlgorithm { get; set; } = string.Empty;
    public string ChecksumType { get; set; } = string.Empty;
    public ulong Size { get; set; }
    public string StorageClass { get; set; } = string.Empty;
}
