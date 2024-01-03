using System;
using System.Collections.Generic;
using System.Text;

namespace LiquidLabyrinth.ItemData
{
    [Serializable]
    public class HeadItemData
    {
        public HeadItemData(string tip, string desc)
        {
            tooltip = tip;
            description = desc;
        }
        public bool IsNullOrEmpty()
        {
            return string.IsNullOrEmpty(tooltip) && string.IsNullOrEmpty(description);
        }
        public string tooltip;
        public string description;
    }
}
