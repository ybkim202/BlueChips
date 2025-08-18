using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

// Contracts
using BlueChips.Contracts;

// Services
using BlueChips.Services;
using BlueChips.Services.Upbit;
using BlueChips.Services.Trading;
using BlueChips.Services.Reporting;
using BlueChips.Services.AutoTrade;

// ViewModels
using BlueChips.ViewModels;
using TimeProvider = BlueChips.Services.TimeProvider;

namespace BlueChips {
    public partial class App : Application {
        public static IServiceProvider Services { get; private set; } = default!;

        public App() { }

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            var sc = new ServiceCollection();
            ConfigureServices(sc);
            Services = sc.BuildServiceProvider();

            // 1) DB 초기화 (파일 생성, PRAGMA, schema_v1.sql, migrations 적용)
            var dbInit = Services.GetRequiredService<DatabaseInitializer>();
            dbInit.Initialize(); // 비동기 구현이라면 InitializeAsync().GetAwaiter().GetResult();

            // 2) 메인 윈도우 표시
            var main = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };
            main.Show();
        }

        private static void ConfigureServices(IServiceCollection services) {
            // === 인프라/공통 ===
            services.AddSingleton<SettingsProvider>();   // Config/AppSettings.json 로딩
            services.AddSingleton<TimeProvider>();       // UTC 시간 소스
            services.AddSingleton<DatabaseInitializer>(); // DB 파일/스키마 초기화

            // === UI 인프라 ===
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IDialogService, DialogService>();

            // === Upbit (REST + 폴링 캐시) ===
            services.AddHttpClient<IUpbitRestService, UpbitRestService>()
                .ConfigureHttpClient((sp, http) =>
                {
                    // SettingsProvider에서 BaseUrl/Timeout 등을 읽어 설정
                    var settings = sp.GetRequiredService<SettingsProvider>().Get();
                    var baseUrl = settings?.Upbit?.BaseUrl ?? "https://api.upbit.com";
                    http.BaseAddress = new Uri(baseUrl);
                    http.Timeout = TimeSpan.FromSeconds(settings?.Upbit?.TimeoutSeconds ?? 10);
                });

            services.AddSingleton<IPriceFeed, PriceFeed>(); // 주기 폴링 및 최신 시세 캐시

            // === Trading (저장소/엔진/포트폴리오) ===
            services.AddSingleton<ITradeRepository, SQLiteTradeRepository>(); // 실사용
            // 개발 모드에서 InMemory로 바꾸려면 위 줄을 주석처리하고 아래 줄을 활성화
            // services.AddSingleton<ITradeRepository, InMemoryTradeRepository>();

            services.AddSingleton<IFeePolicy, FixedRateFeePolicy>();
            services.AddSingleton<ISlippageModel, NoSlippageModel>();
            services.AddSingleton<ISimExecutionEngine, SimExecutionEngine>();
            services.AddSingleton<IPortfolioService, PortfolioService>();
            services.AddSingleton<IWatchlistRepository, WatchlistRepository>();
            services.AddSingleton<ISettingsRepository, SettingsRepository>();

            // === AutoTrade / Backtest ===
            services.AddSingleton<IStrategyEngine, StrategyEngine>();
            services.AddSingleton<IBacktestService, BacktestService>();

            // === Reporting (여러 구현 등록: IEnumerable<IReportExporter>로 주입 가능) ===
            services.AddSingleton<IReportExporter, CsvReportExporter>();
            services.AddSingleton<IReportExporter, PngChartExporter>();

            // === ViewModels ===
            services.AddSingleton<MainViewModel>();          // 셸
            services.AddTransient<LoginViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<MarketViewModel>();
            services.AddTransient<TradeViewModel>();
            services.AddTransient<AutoTradeViewModel>();
            services.AddTransient<ReportsViewModel>();
            services.AddTransient<SettingsViewModel>();
        }
    }
}
