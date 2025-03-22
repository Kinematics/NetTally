using System.Text.Json.Serialization;

namespace NetTally.Product;
public class GithubRelease
{
    public string Url { get; set; } = string.Empty;
    public string Assets_Url { get; set; } = string.Empty;
    public string Upload_Url { get; set; } = string.Empty;
    public string Html_Url { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Node_Id { get; set; } = string.Empty;
    public string Tag_Name { get; set; } = string.Empty;
    public string Target_Commitish { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Draft { get; set; }
    public bool Prerelease { get; set; }
    public DateTimeOffset Created_At { get; set; }
    public DateTimeOffset Published_At { get; set; }
    
    [JsonIgnore]
    public Version? Version
    {
        get
        {
            string tagVersion = Tag_Name.Trim('v');

            return Version.TryParse(tagVersion, out var version) ?
                version : null;
        }
    }
}
