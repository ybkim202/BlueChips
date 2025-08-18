using BlueChips.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Contracts
{
    public interface IDialogService {
        /** Methods **/
        void Show(string message, string? title = null);
        bool Confirm(string message, string? title = null);
        string? Prompt(string message, string? title = null, string? defaultValue = null);
        void ShowError(string message, Exception? ex = null, string? title = null);
    }
}
