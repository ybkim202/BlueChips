using System;
using System.IO;
using System.Text.Json;

namespace BlueChips.Services {
    /// <summary>
    /// Config/AppSettings.json (+ AppSettings.{ENV}.json)을 읽어 강타입 설정을 제공.
    /// 상대 경로는 실행 폴더 기준 절대 경로로 정규화합니다.
    /// </summary>
    public sealed class SettingsProvider {
        private readonly AppConfig _config;

        public SettingsProvider() {
            // 실행 폴더(bin/…)
            var baseDir = AppContext.BaseDirectory;

            // 기본 설정 파일
            var mainPath = Path.Combine(baseDir, "Config", "AppSettings.json");

            // 환경별 오버라이드 (DOTNET_ENVIRONMENT=Development/Production 등)
            var envName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            var envPath = string.IsNullOrWhiteSpace(envName)
                ? null
                : Path.Combine(baseDir, "Config", $"AppSettings.{envName}.json");

            _config = LoadAndMerge(mainPath, envPath);

            // 경로 정규화(상대경로 → 절대경로)
            NormalizePaths(baseDir, _config);
        }

        /// <summary>강타입 설정 전체를 반환</summary>
        public AppConfig Get() => _config;

        private static AppConfig LoadAndMerge(string mainPath, string? envPath) {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            AppConfig cfg = File.Exists(mainPath)
                ? JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(mainPath), options) ?? new AppConfig()
                : new AppConfig();

            if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath)) {
                var envCfg = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(envPath), options);
                if (envCfg != null) cfg = Merge(cfg, envCfg);
            }

            return cfg;
        }

        private static AppConfig Merge(AppConfig baseCfg, AppConfig overrideCfg) {
            // 필요한 범위만 간단 병합(overrideCfg 값이 있으면 덮어씀)
            baseCfg.Database ??= new DatabaseConfig();
            overrideCfg.Database ??= new DatabaseConfig();
            if (!string.IsNullOrWhiteSpace(overrideCfg.Database.Path)) baseCfg.Database.Path = overrideCfg.Database.Path;
            if (!string.IsNullOrWhiteSpace(overrideCfg.Database.JournalMode)) baseCfg.Database.JournalMode = overrideCfg.Database.JournalMode;
            if (!string.IsNullOrWhiteSpace(overrideCfg.Database.Synchronous)) baseCfg.Database.Synchronous = overrideCfg.Database.Synchronous;
            baseCfg.Database.ForeignKeys = overrideCfg.Database.ForeignKeys ?? baseCfg.Database.ForeignKeys;

            baseCfg.Upbit ??= new UpbitConfig();
            overrideCfg.Upbit ??= new UpbitConfig();
            if (!string.IsNullOrWhiteSpace(overrideCfg.Upbit.BaseUrl)) baseCfg.Upbit.BaseUrl = overrideCfg.Upbit.BaseUrl;
            if (overrideCfg.Upbit.TimeoutSeconds.HasValue) baseCfg.Upbit.TimeoutSeconds = overrideCfg.Upbit.TimeoutSeconds;

            baseCfg.Feed ??= new FeedConfig();
            overrideCfg.Feed ??= new FeedConfig();
            if (overrideCfg.Feed.PollIntervalMs.HasValue) baseCfg.Feed.PollIntervalMs = overrideCfg.Feed.PollIntervalMs;

            baseCfg.Trading ??= new TradingConfig();
            overrideCfg.Trading ??= new TradingConfig();
            if (overrideCfg.Trading.TakerFeeRate.HasValue) baseCfg.Trading.TakerFeeRate = overrideCfg.Trading.TakerFeeRate;
            if (overrideCfg.Trading.MakerFeeRate.HasValue) baseCfg.Trading.MakerFeeRate = overrideCfg.Trading.MakerFeeRate;
            if (overrideCfg.Trading.PriceDecimals.HasValue) baseCfg.Trading.PriceDecimals = overrideCfg.Trading.PriceDecimals;
            if (overrideCfg.Trading.QuantityDecimals.HasValue) baseCfg.Trading.QuantityDecimals = overrideCfg.Trading.QuantityDecimals;

            return baseCfg;
        }

        private static void NormalizePaths(string baseDir, AppConfig cfg) {
            if (cfg.Database is null) { cfg.Database = new DatabaseConfig(); }

            if (!string.IsNullOrWhiteSpace(cfg.Database.Path) && !Path.IsPathRooted(cfg.Database.Path)) {
                cfg.Database.Path = Path.GetFullPath(Path.Combine(baseDir, cfg.Database.Path));
            }
        }
    }

    #region Strongly-typed settings

    public sealed class AppConfig {
        public DatabaseConfig? Database { get; set; } = new();
        public UpbitConfig? Upbit { get; set; } = new();
        public FeedConfig? Feed { get; set; } = new();
        public TradingConfig? Trading { get; set; } = new();
    }

    public sealed class DatabaseConfig {
        public string Path { get; set; } = "Data/bluechips.dev.db";
        public string JournalMode { get; set; } = "WAL";       // PRAGMA journal_mode
        public string Synchronous { get; set; } = "NORMAL";    // PRAGMA synchronous
        public bool? ForeignKeys { get; set; } = true;         // PRAGMA foreign_keys
    }

    public sealed class UpbitConfig {
        public string BaseUrl { get; set; } = "https://api.upbit.com";
        public int? TimeoutSeconds { get; set; } = 10;
    }

    public sealed class FeedConfig {
        public int? PollIntervalMs { get; set; } = 1000;       // PriceFeed 폴링 주기(ms)
    }

    public sealed class TradingConfig {
        public decimal? TakerFeeRate { get; set; } = 0.0005m;  // 0.05%
        public decimal? MakerFeeRate { get; set; } = 0.0005m;
        public int? PriceDecimals { get; set; } = 0;
        public int? QuantityDecimals { get; set; } = 8;
    }

    #endregion
}
