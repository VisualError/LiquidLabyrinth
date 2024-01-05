using LiquidLabyrinth.Labyrinth;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace LiquidLabyrinth.ItemData
{
    [Serializable]
    public class BottleItemData
    {
        public BottleItemData(string _name, float _fill)
        {
            name = _name ?? "BottleType";
            fill = _fill != 0f ? _fill : UnityEngine.Random.Range(0f, 1f);
        }
        public bool IsNullOrEmpty()
        {
            return string.IsNullOrEmpty(this.name) && this.fill == 0f;
        }
        public string name;
        public float fill;
    }
}
