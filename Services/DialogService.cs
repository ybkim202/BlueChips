using BlueChips.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BlueChips.Services {
    public sealed class DialogService : IDialogService {
        public void Show(string message, string? title = null)
            => MessageBox.Show(message, title ?? "Info", MessageBoxButton.OK, MessageBoxImage.Information);

        public bool Confirm(string message, string? title = null)
            => MessageBox.Show(message, title ?? "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

        public string? Prompt(string message, string? title = null, string? defaultValue = null) {
            // 간단히 입력창 미구현: 기본값 반환(필요 시 전용 InputDialog로 교체)
            return defaultValue;
        }

        public void ShowError(string message, System.Exception? ex = null, string? title = null)
            => MessageBox.Show(ex is null ? message : $"{message}\n\n{ex}", title ?? "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}