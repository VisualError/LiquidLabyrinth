using UnityEngine;
using UnityEngine.Events;
namespace LiquidLabyrinth.Events
{
    internal class LiquidEvents : MonoBehaviour
    {
        public static LiquidEvents Instance;
        private void Awake()
        {
            Instance = this;
        }
        public event UnityAction onLiquidPuddleCreate;
        public event UnityAction onLiquidPuddleDestroy;
        public event UnityAction onLiquidPuddleCollide;
        public event UnityAction onConsumeLiquid;
        public event UnityAction onShakeLiquid;
    }
}
