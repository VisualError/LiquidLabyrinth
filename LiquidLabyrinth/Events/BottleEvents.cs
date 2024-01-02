using UnityEngine;
using UnityEngine.Events;

namespace LiquidLabyrinth.Events;

internal class BottleEvents : MonoBehaviour
{
    public static BottleEvents Instance;
    private void Awake()
    {
        Instance = this;
    }
    public event UnityAction onDrink;
    public event UnityAction onToast;
    public event UnityAction onShakeBottle;
}