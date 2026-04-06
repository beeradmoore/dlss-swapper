namespace DLSS_Swapper.Data.NVIDIA;

[System.Xml.Serialization.XmlRootAttribute(Namespace = "http://s3.amazonaws.com/doc/2006-03-01/", IsNullable = false)]
public class ListBucketResult
{
    public string Name { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public string Marker { get; set; } = string.Empty;
    public int MaxKeys { get; set; }
    public bool IsTruncated { get; set; }

    [System.Xml.Serialization.XmlElementAttribute("Contents")]
    public ListBucketResultContents[] Contents { get; set; } = [];
}
