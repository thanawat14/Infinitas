using System;
using System.Text.Json.Serialization;

namespace Infinitas
{
    public class QrcodeFormat
    {

        [JsonPropertyName("account")]
        public string Account { get; set; }

        [JsonPropertyName("price")]
        public int Price { get; set; }

        [JsonPropertyName("type")]
        public int Type { get; set; }
    }
}
