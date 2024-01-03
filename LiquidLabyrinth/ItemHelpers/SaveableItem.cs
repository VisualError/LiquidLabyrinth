using System;
using System.Collections.Generic;
using System.Text;

namespace LiquidLabyrinth.ItemHelpers
{
    internal class SaveableItem : GrabbableObject
    {
        public override int GetItemDataToSave()
        {
            return base.GetItemDataToSave();
        }
    }
}
