using BlueChips.Contracts;
using BlueChips.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Services
{
    public sealed class NavigationService : INavigationService {
        private readonly Stack<(PageType page, object? param)> _stack = new();

        public bool CanGoBack => _stack.Count > 1;

        public void Navigate(PageType page, object? parameter = null)
            => _stack.Push((page, parameter));

        public void GoBack() {
            if (CanGoBack) _stack.Pop();
        }

        public void Reset(PageType page, object? parameter = null) {
            _stack.Clear();
            _stack.Push((page, parameter));
        }
    }
}
