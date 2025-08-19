using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization; // Newtonsoft 쓰면 JsonProperty로 교체

namespace BlueChips.Models.Upbit {
    public class Market {
        [JsonPropertyName("market")]     // Upbit의 "market" 필드와 매핑 (Newtonsoft: [JsonProperty("market")])
        public string Symbol { get; set; } = "";   // 예: "KRW-BTC"

        [JsonPropertyName("korean_name")]
        public string KoreanName { get; set; } = "";

        [JsonPropertyName("english_name")]
        public string EnglishName { get; set; } = "";

        public string Base => Symbol.Contains('-') ? Symbol.Split('-')[1] : Symbol; // BTC
        public string Quote => Symbol.Contains('-') ? Symbol.Split('-')[0] : "";     // KRW
    }
}