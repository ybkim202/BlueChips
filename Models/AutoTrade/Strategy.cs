using System;
using System.Collections.Generic;
using System.Globalization;

namespace BlueChips.Models.AutoTrade {
    /// <summary>
    /// 자동매매 전략 정의(이름 + 파라미터 모음).
    /// - Name: 전략 식별 이름 (예: "BuyAndHold", "SMA_Cross", "RSI")
    /// - Parameters: 전략 파라미터 키-값 (예: { "InitialCash":"10000000", "Fast":"20", "Slow":"60" })
    /// </summary>
    public class Strategy {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");    // 전략 고유 ID (선택) 
        public string Name { get; set; } = string.Empty;    // 전략 이름(필수). 예: "BuyAndHold", "SMA_Cross"
        public string? Description { get; set; }    // 전략 설명(선택)

        /* 전략 파라미터(Key-Value). - 숫자/불리언 등은 문자열로 저장하고, 해석은 서비스/전략 쪽에서 변환. */
        public Dictionary<string, string> Parameters { get; set; } = new();

        public string? DefaultMarket { get; set; }    // 기본 대상으로 쓰는 마켓(선택). 예: "KRW-BTC"

        public string? DefaultTimeframe { get; set; }    // 기본 타임프레임(선택). 예: "m1","m5","h1","d1"

        public bool IsEnabled { get; set; } = true;    // 사용 여부

        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;    // 생성 시각(UTC)
    }

    /* 전략 파라미터를 안전하게 읽고 싶다면 간단한 확장 메서드 */
    public static class StrategyExtensions {
        public static string GetString(this Strategy s, string key, string @default = "")
            => s.Parameters != null && s.Parameters.TryGetValue(key, out var v) ? v : @default;

        public static decimal GetDecimal(this Strategy s, string key, decimal @default = 0m)
            => s.Parameters != null && s.Parameters.TryGetValue(key, out var v)
               && decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
               ? d : @default;

        public static int GetInt(this Strategy s, string key, int @default = 0)
            => s.Parameters != null && s.Parameters.TryGetValue(key, out var v)
               && int.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var n)
               ? n : @default;
    }
}
