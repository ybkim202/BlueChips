using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlueChips.Enums;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BlueChips.Messages
{
    public class PageChangeMessage : ValueChangedMessage<PageType> {
        public PageChangeMessage() : base(default) { }
        public PageChangeMessage(PageType value) : base(value) { }
    }
}
