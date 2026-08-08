namespace MackySoft.FileSystem;

/// <summary> Specifies how physical path resolution handles a path whose final segment or ancestor is missing. </summary>
public enum MissingPathHandling
{
    /// <summary> Fail when any requested path segment is missing. </summary>
    Reject = 0,

    /// <summary> Resolve every existing prefix and retain the remaining lexical tail. </summary>
    AllowMissingTail,
}
