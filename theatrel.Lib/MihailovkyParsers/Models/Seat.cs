using System.Text.Json.Serialization;

public class Seat
{
    [JsonPropertyName("ID")]
    public string ID { get; set; }

    [JsonPropertyName("NAME")]
    public NAME NAME { get; set; }

    [JsonPropertyName("PROPERTY_PLACE_ID_VALUE")]
    public string PROPERTY_PLACE_ID_VALUE { get; set; }

    [JsonPropertyName("PREVIEW_PICTURE")]
    public object PREVIEW_PICTURE { get; set; }

    [JsonPropertyName("PROPERTY_NUMBER_VALUE")]
    public string PROPERTY_NUMBER_VALUE { get; set; }

    [JsonPropertyName("IBLOCK_SECTION_ID")]
    public string IBLOCK_SECTION_ID { get; set; }

    [JsonPropertyName("ACTIVE")]
    public string ACTIVE { get; set; }

    [JsonPropertyName("PROPERTY_NOTE_VALUE")]
    public string PROPERTY_NOTE_VALUE { get; set; }

    [JsonPropertyName("SORT")]
    public int SORT { get; set; }

    [JsonPropertyName("PROPERTY_LIMITED_REVIEW_VALUE")]
    public object PROPERTY_LIMITED_REVIEW_VALUE { get; set; }

    [JsonPropertyName("PROPERTY_LIMITED_REVIEW_ENUM_ID")]
    public object PROPERTY_LIMITED_REVIEW_ENUM_ID { get; set; }

    //[JsonPropertyName("PROPERTY_LIMITED_REVIEW_VALUE_ID")]
    //public string PROPERTY_LIMITED_REVIEW_VALUE_ID { get; set; }

    [JsonPropertyName("PROPERTY_LEVEL_VALUE")]
    public string PROPERTY_LEVEL_VALUE { get; set; }

    [JsonPropertyName("PROPERTY_COLOR_VALUE")]
    public string PROPERTY_COLOR_VALUE { get; set; }

    //[JsonPropertyName("PROPERTY_PAIR_VALUE")]
    //public object PROPERTY_PAIR_VALUE { get; set; }

    //[JsonPropertyName("PROPERTY_PAIR_ENUM_ID")]
    //public object PROPERTY_PAIR_ENUM_ID { get; set; }

    //[JsonPropertyName("PROPERTY_PAIR_VALUE_ID")]
    //public string PROPERTY_PAIR_VALUE_ID { get; set; }

    //[JsonPropertyName("PROPERTY_FOUR_VALUE")]
    //public object PROPERTY_FOUR_VALUE { get; set; }

    //[JsonPropertyName("PROPERTY_FOUR_ENUM_ID")]
    //public object PROPERTY_FOUR_ENUM_ID { get; set; }

    //[JsonPropertyName("PROPERTY_FOUR_VALUE_ID")]
    //public string PROPERTY_FOUR_VALUE_ID { get; set; }

    [JsonPropertyName("PROPERTY_NOTE_EN_VALUE")]
    public object PROPERTY_NOTE_EN_VALUE { get; set; }

    //[JsonPropertyName("PROPERTY_NOTE_EN_VALUE_ID")]
    //public string PROPERTY_NOTE_EN_VALUE_ID { get; set; }

    //[JsonPropertyName("SECTIONS_ID")]
    //public List<string> SECTIONS_ID { get; set; }

    //[JsonPropertyName("GROUP_PLACE_ID")]
    //public List<string> GROUP_PLACE_ID { get; set; }

    [JsonPropertyName("LANG_NAME")]
    public object LANG_NAME { get; set; }

    [JsonPropertyName("IS_BUSY")]
    public bool IS_BUSY { get; set; }

    [JsonPropertyName("PRICE")]
    public int PRICE { get; set; }

    [JsonPropertyName("JS")]
    public JS JS { get; set; }

    [JsonPropertyName("GROUP_NOTE")]
    public string GROUP_NOTE { get; set; }
}