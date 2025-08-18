using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using BlueChips.Enums;
using BlueChips.Views;
using BlueChips.ViewModels;

namespace BlueChips.ViewModels
{
    public partial class MainViewModel : ViewModelBase {
        /** Constructor **/
        public MainViewModel() {
            Console.WriteLine("[MainViewModel] initialized.");
        }

        /** Member Variables **/
        private Frame? _mainFrame;

        /** Member Methods **/
        private void Navigate(PageType page) {
            if (_mainFrame == null) return;

            switch (page) {
                case PageType.LogIn:
                    _mainFrame.Navigate(new LogInView { DataContext = App.Services!.GetService(typeof(LoginViewModel)) });
                    break;

                case PageType.Dashboard:
                    _mainFrame.Navigate(new DashboardView { DataContext = App.Services!.GetService(typeof(DashboardViewModel)) });
                    break;

                case PageType.Market:
                    _mainFrame.Navigate(new MarketView { DataContext = App.Services!.GetService(typeof(MarketViewModel)) });
                    break;

                case PageType.Trade:
                    _mainFrame.Navigate(new TradeView { DataContext = App.Services!.GetService(typeof(TradeViewModel)) });
                    break;

                case PageType.AutoTrade:
                    _mainFrame.Navigate(new AutoTradeView { DataContext = App.Services!.GetService(typeof(AutoTradeViewModel)) });
                    break;

                case PageType.Reports:
                    _mainFrame.Navigate(new ReportsView { DataContext = App.Services!.GetService(typeof(ReportsViewModel)) });
                    break;

                case PageType.Settings:
                    _mainFrame.Navigate(new SettingsView { DataContext = App.Services!.GetService(typeof(SettingsViewModel)) });
                    break;

                case PageType.GoBack:
                    if (_mainFrame.CanGoBack) {
                        _mainFrame.GoBack();
                    } else {
                        Console.WriteLine("[MainViewModel] No page to go back to.");
                    }
                    break;
            }
        }
    }
}
