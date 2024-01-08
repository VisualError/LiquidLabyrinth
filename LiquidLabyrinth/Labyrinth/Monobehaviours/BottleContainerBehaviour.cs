using UnityEngine;

namespace LiquidLabyrinth.Labyrinth.Monobehaviours
{
    internal class BottleContainerBehaviour : Container
    {
        public override bool AllowsOverflow
        {
            get
            {
                return false;
            }
        }
        public override Vector2 Limits
        {
            get
            {
                return new Vector2(0f, 5f);
            }
        }

        public LiquidContainerController LiquidContainer;

        protected override void Update()
        {
            base.Update();
            LiquidContainer.FillPercentage = Mathf.Clamp01(ScaledLiquidAmount);
            LiquidContainer.Color = GetComputedColor();
        }
    }
}
