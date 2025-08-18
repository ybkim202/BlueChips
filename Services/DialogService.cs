using BlueChips.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Services {
    public sealed class DialogService : IDialogService {
        public DialogService() { }

        public void Show(string message, string? title = null) {
            // 실제 구현
        }

        public bool Confirm(string message, string? title = null) {
            // 실제 구현
            return true;
        }
    }
}