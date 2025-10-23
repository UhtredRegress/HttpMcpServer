using System.Text.Json.Serialization;

namespace McpServer.Model;

public class Invoice
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("partner_id")]
    public List<Object> Partner { get; set; }
    [JsonPropertyName("amount_total")]
    public decimal Amount { get; set; }
    [JsonPropertyName("currency_id")]
    public List<Object> Currency { get; set; }

    [JsonIgnore] 
    public int CurrencyId => Currency[0] == null ? throw new InvalidDataException("There is error with the found invoice, currency id of found id is null") : Convert.ToInt32(Currency[0].ToString());
    [JsonIgnore]
    public int PartnerId => Partner[0] == null ? throw new InvalidDataException("There is error with the found invoice, currency id of found id is null") : Convert.ToInt32(Partner[0].ToString());
}