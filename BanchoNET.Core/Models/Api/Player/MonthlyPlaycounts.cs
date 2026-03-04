using System.Text.Json.Serialization;
using BanchoNET.Core.Utils.Json;

namespace BanchoNET.Core.Models.Api.Player;

public class MonthlyPlaycounts
{
    [JsonConverter(typeof(SimpleDateFormatConverter))]
    public DateTime StartDate { get; set; }
    public int Count { get; set; }
}