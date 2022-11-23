using System.Collections.Generic;
using System.Text.Json.Serialization;

public class JS
{
    [JsonPropertyName("id")]
    public string id { get; set; }

    [JsonPropertyName("name")]
    public string name { get; set; }

    [JsonPropertyName("part_name")]
    public List<string> part_name { get; set; }

    [JsonPropertyName("price")]
    public int? price { get; set; }

    [JsonPropertyName("note")]
    public string note { get; set; }

    [JsonPropertyName("image")]
    public object image { get; set; }

    [JsonPropertyName("arPlaceId")]
    public List<string> arPlaceId { get; set; }

    [JsonPropertyName("groupId")]
    public string groupId { get; set; }

    [JsonPropertyName("groupPrice")]
    public int groupPrice { get; set; }

    [JsonPropertyName("groupPriceFormated")]
    public string groupPriceFormated { get; set; }

    [JsonPropertyName("priceFormated")]
    public string priceFormated { get; set; }
}
