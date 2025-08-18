using BlueChips.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace BlueChips.Contracts
{
    public interface INavigationService {
        void Navigate(PageType page, object? parameter = null);
        bool CanGoBack { get; }
        void GoBack();
        void Reset(PageType page, object? parameter = null);
    }
}
