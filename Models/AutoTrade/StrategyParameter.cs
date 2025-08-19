using System;
using System.Collections.Generic;
using System.Globalization;

namespace BlueChips.Models.AutoTrade {
    /// <summary>
    /// 전략의 개별 파라미터(메타+값).
    /// - Key: 내부 식별 키(코드에서 사용)
    /// - DisplayName: UI 표시명
    /// - Type: 값 타입(문자/정수/소수/불리언/열거 등)
    /// - Value: 현재 값(문자열 저장), Default: 기본값
    /// - Options: 선택지(드롭다운/열거형 UI용)
    /// - 범위: 정수/소수형에 대한 최소/최대
    /// - Required: 필수 여부, Order: 정렬/표시 순서
    /// - Description/Group: UI 설명/그룹핑
    /// </summary>
    public class StrategyParameter {
        public string Key { get; set; } = "";                 // 예: "InitialCash", "Fast", "Slow"
        public string? DisplayName { get; set; }              // 예: "초기 현금", "단기", "장기"
        public StrategyParamType Type { get; set; } = StrategyParamType.String;

        public string? Value { get; set; }                    // 실제 값(문자열 저장)
        public string? Default { get; set; }                  // 기본값(문자열)

        public List<string>? Options { get; set; }            // 선택지(열거형/드롭다운용)

        // 숫자 범위(선택)
        public decimal? MinDecimal { get; set; }
        public decimal? MaxDecimal { get; set; }
        public int? MinInt { get; set; }
        public int? MaxInt { get; set; }

        public bool Required { get; set; } = false;
        public int Order { get; set; } = 0;

        public string? Description { get; set; }
        public string? Group { get; set; }
    }

    public enum StrategyParamType {
        String = 0,
        Decimal = 1,
        Int = 2,
        Bool = 3,
        Enum = 4
    }

    /// <summary>
    /// StrategyParameter 파싱/검증 보조.
    /// </summary>
    public static class StrategyParameterExtensions {
        public static string GetString(this StrategyParameter p, string defaultValue = "")
            => Coalesce(p.Value, p.Default, defaultValue);

        public static bool GetBool(this StrategyParameter p, bool defaultValue = false) {
            var s = Coalesce(p.Value, p.Default, defaultValue ? "true" : "false");
            return s.Equals("1") || s.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   s.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                   s.Equals("on", StringComparison.OrdinalIgnoreCase);
        }

        public static int GetInt(this StrategyParameter p, int defaultValue = 0) {
            var s = Coalesce(p.Value, p.Default, defaultValue.ToString(CultureInfo.InvariantCulture));
            return int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : defaultValue;
        }

        public static decimal GetDecimal(this StrategyParameter p, decimal defaultValue = 0m) {
            var s = Coalesce(p.Value, p.Default, defaultValue.ToString(CultureInfo.InvariantCulture));
            return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : defaultValue;
        }

        /// <summary>
        /// Enum/옵션형에서 유효성 검사(Options 지정 시 포함 여부 검사).
        /// </summary>
        public static bool IsValidOption(this StrategyParameter p) {
            if (p.Type != StrategyParamType.Enum || p.Options is null || p.Options.Count == 0) return true;
            var v = Coalesce(p.Value, p.Default, null);
            return v is null || p.Options.Contains(v); // 값이 비어있다면 통과, 채워져 있으면 옵션 내 존재해야 함
        }

        /// <summary>
        /// 숫자 범위 검사(타입에 맞는 범위가 지정된 경우에만).
        /// </summary>
        public static bool IsWithinRange(this StrategyParameter p) {
            if (p.Type == StrategyParamType.Int) {
                var v = p.GetInt();
                if (p.MinInt.HasValue && v < p.MinInt.Value) return false;
                if (p.MaxInt.HasValue && v > p.MaxInt.Value) return false;
            }
            else if (p.Type == StrategyParamType.Decimal) {
                var v = p.GetDecimal();
                if (p.MinDecimal.HasValue && v < p.MinDecimal.Value) return false;
                if (p.MaxDecimal.HasValue && v > p.MaxDecimal.Value) return false;
            }
            return true;
        }

        /// <summary>
        /// 필수 값 검사.
        /// </summary>
        public static bool HasRequiredValue(this StrategyParameter p) {
            if (!p.Required) return true;
            var v = Coalesce(p.Value, p.Default, null);
            return !string.IsNullOrWhiteSpace(v);
        }

        private static string Coalesce(string? a, string? b, string? c)
            => a ?? b ?? c ?? "";
    }

    /// <summary>
    /// 리스트/딕셔너리 병행 사용을 위한 헬퍼.
    /// - Strategy.Parameters(Dictionary)와 병행 시 편리.
    /// </summary>
    public static class StrategyParameterSetExtensions {
        /// <summary>
        /// 리스트에서 Key로 파라미터 찾기(없으면 null).
        /// </summary>
        public static StrategyParameter? Find(this IEnumerable<StrategyParameter>? list, string key) {
            if (list == null) return null;
            foreach (var p in list) {
                if (string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase))
                    return p;
            }
            return null;
        }

        /// <summary>
        /// 리스트 우선 → 없으면 Strategy.Parameters(Dictionary)로 폴백하여 문자열 가져오기.
        /// </summary>
        public static string GetString(this IEnumerable<StrategyParameter>? list, BlueChips.Models.AutoTrade.Strategy strategy, string key, string defaultValue = "") {
            var p = list.Find(key);
            if (p != null) return p.GetString(defaultValue);

            // Dictionary fallback
            if (strategy?.Parameters != null && strategy.Parameters.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
                return v;

            return defaultValue;
        }

        public static int GetInt(this IEnumerable<StrategyParameter>? list, BlueChips.Models.AutoTrade.Strategy strategy, string key, int defaultValue = 0) {
            var p = list.Find(key);
            if (p != null) return p.GetInt(defaultValue);

            if (strategy?.Parameters != null && strategy.Parameters.TryGetValue(key, out var v) &&
                int.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var n))
                return n;

            return defaultValue;
        }

        public static decimal GetDecimal(this IEnumerable<StrategyParameter>? list, BlueChips.Models.AutoTrade.Strategy strategy, string key, decimal defaultValue = 0m) {
            var p = list.Find(key);
            if (p != null) return p.GetDecimal(defaultValue);

            if (strategy?.Parameters != null && strategy.Parameters.TryGetValue(key, out var v) &&
                decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return d;

            return defaultValue;
        }

        public static bool GetBool(this IEnumerable<StrategyParameter>? list, BlueChips.Models.AutoTrade.Strategy strategy, string key, bool defaultValue = false) {
            var p = list.Find(key);
            if (p != null) return p.GetBool(defaultValue);

            if (strategy?.Parameters != null && strategy.Parameters.TryGetValue(key, out var v)) {
                var s = v.Trim();
                if (s.Equals("1") || s.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                    s.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                    s.Equals("on", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (s.Equals("0") || s.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                    s.Equals("no", StringComparison.OrdinalIgnoreCase) ||
                    s.Equals("off", StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return defaultValue;
        }
    }
}
