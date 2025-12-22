using System.Collections.Generic;

namespace FontManager.ViewModels
{
    public class FontRowViewModel
    {
        public IEnumerable<FontItemViewModel> Items { get; }
        public FontRowViewModel(IEnumerable<FontItemViewModel> items)
        {
            Items = items;
        }
    }
}