using LiquidLabyrinth.Utilities;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace LiquidLabyrinth.Labyrinth.Monobehaviours
{
    public abstract class Container : MonoBehaviour
    {
        public float TotalLiquidAmount { get; set; }
        public float ScaledLiquidAmount
        {
            get
            {
                return OtherUtils.MapRange(Limits.x, Limits.y, 0f, 1f, TotalLiquidAmount);
            }
        }
        public virtual bool AllowsOverflow
        {
            get
            {
                return true;
            }
        }
        public virtual bool AllowsTransfer
        {
            get
            {
                return true;
            }
        }
        public virtual Vector2 Limits
        {
            get
            {
                return new Vector2(0f, 1f);
            }
        }
        public float UpperLimit
        {
            get
            {
                if (!AllowsOverflow)
                {
                    return Limits.y;
                }
                return float.MaxValue;
            }
        }
        public float LowerLimit
        {
            get
            {
                return Limits.x;
            }
        }

        public float AddLiquid(LiquidAPI.Liquid liquid, float amount)
        {
            if (amount < 1E-45f)
            {
                return 0f;
            }
            float num = Mathf.Clamp(TotalLiquidAmount + amount, LowerLimit, UpperLimit) - TotalLiquidAmount;
            if (float.IsNaN(num) || num <= 1E-45f)
            {
                return 0f;
            }
            TotalLiquidAmount += num;
            if(LiquidDistribution.TryGetValue(liquid, out RefFill refFill))
            {
                refFill.Raw += num;
            }
            else
            {
                liquid.Container = this;
                LiquidDistribution.Add(liquid, new RefFill(num));
                OnLiquidEnter(liquid);
            }
            return num;
        }

        public float RemoveLiquid(LiquidAPI.Liquid liquid, float amount)
        {
            if (amount <= 1E-45f)
            {
                return 0f;
            }
            float num = Mathf.Clamp(TotalLiquidAmount - amount, LowerLimit, UpperLimit) - TotalLiquidAmount;
            if (float.IsNaN(num) || num >= -1E-45f)
            {
                return 0f;
            }
            if(LiquidDistribution.TryGetValue(liquid, out RefFill refFill))
            {
                if(refFill.Raw + num < 0f) 
                {
                    num = -refFill.Raw;
                }
                TotalLiquidAmount += num;
                refFill.Raw += num;
            }
            deleteEmptyEntries = true;
            return num;
        }

        public void DeleteEmptyLiquidEntries()
        {
            deleteEmptyEntries = false;
            foreach (LiquidAPI.Liquid liquid in LiquidAPI.LiquidSet)
            {
                if (LiquidDistribution.TryGetValue(liquid, out RefFill refFill))
                {
                    if (refFill.Raw <= 0.02f && (refFill.Raw <= 1E-45f || refFill.HasDecreased(0.0001f) || !refFill.HasChanged(0.0001f)))
                    {
                        TotalLiquidAmount -= refFill.Raw;
                        OnLiquidExit(liquid);
                        LiquidDistribution.Remove(liquid);
                    }
                    refFill.Previous = refFill.Raw;
                }
            }
        }

        protected virtual void OnLiquidEnter(LiquidAPI.Liquid liquid)
        {
            liquid.OnEnterContainer(this);
        }

        // TODO: Don't do this lmao.
        void OnDestroy()
        {
            foreach(KeyValuePair<LiquidAPI.Liquid, RefFill> liquid in LiquidDistribution)
            {
                OnContainerBreak(liquid.Key);
            }
        }
        RaycastHit[] _potionBreakHits = new RaycastHit[20];
        protected virtual void OnContainerBreak(LiquidAPI.Liquid liquid)
        {
            // REVIVE TEST:
            var size = Physics.SphereCastNonAlloc(new Ray(gameObject.transform.position + gameObject.transform.up * 2f, gameObject.transform.forward), 3f, _potionBreakHits, 2f, 1572872);
            if (size == 0)
            {
                liquid.OnContainerBreak(this);
            }

            foreach (RaycastHit hit in _potionBreakHits)
            {
                if (hit.transform == null) return;
                liquid.OnContainerBreak(this, hit);
            }
            
        }

        protected virtual void OnLiquidExit(LiquidAPI.Liquid liquid)
        {
            liquid.OnExitContainer(this);
        }

        protected virtual void Update()
        {
            timer += Time.deltaTime;
            if(timer > 1f)
            {
                timer = 0f;
                DeleteEmptyLiquidEntries();
                int count = LiquidDistribution.Count;
                int num2 = 0;
                foreach (KeyValuePair<LiquidAPI.Liquid, RefFill> keyValuePair in LiquidDistribution)
                {
                    liquidBuffer[num2] = keyValuePair.Key;
                    num2++;
                }
                for(int i= 0; i < count; i++)
                {
                    liquidBuffer[i].OnUpdate(this);
                }
            }
            if (deleteEmptyEntries)
            {
                DeleteEmptyLiquidEntries();
            }
        }

        public Color ForceCalculateComputedColor(Color fallback)
        {
            isColorCached = false;
            return GetComputedColor(fallback);
        }

        public Color GetComputedColor()
        {
            return GetComputedColor(cachedComputedColour);
        }

        public bool IsFull(float percentage = 0.999f)
        {
            return ScaledLiquidAmount >= percentage;
        }

        public Color GetComputedColor(Color fallback)
        {
            if (isColorCached)
            {
                return cachedComputedColour;
            }
            if (LiquidDistribution.Count == 0)
            {
                return fallback;
            }
            if (Mathf.Approximately(0f, TotalLiquidAmount))
            {
                return fallback;
            }
            Color vector = Color.clear;
            foreach (KeyValuePair<LiquidAPI.Liquid, RefFill> keyValuePair in LiquidDistribution)
            {
                float num = keyValuePair.Value.Raw / TotalLiquidAmount;
                vector += keyValuePair.Key.Color * num;
            }
            vector.r = Mathf.Clamp(vector.r, 0.1f, 1f);
            vector.g = Mathf.Clamp(vector.g, 0f, 1f);
            vector.b = Mathf.Clamp(vector.b, 0f, 1f);
            vector.a = Mathf.Clamp(vector.a, 0f, 1f);
            cachedComputedColour = vector;
            isColorCached = true;
            return cachedComputedColour;
        }

        private float timer;
        private bool deleteEmptyEntries;
        public Dictionary<LiquidAPI.Liquid, RefFill> LiquidDistribution = new Dictionary<LiquidAPI.Liquid, RefFill>();
        public LiquidAPI.Liquid[] liquidBuffer = new LiquidAPI.Liquid[256];
        private bool isColorCached;
        private Color cachedComputedColour;

        public class RefFill
        {
            public float Raw
            {
                get
                {
                    return raw;
                }
                set
                {
                    raw = Mathf.Clamp(value, 0f, float.MaxValue);
                }
            }
            public bool HasIncreased(float threshold = 0.0001f)
            {
                return Raw - Previous > threshold;
            }
            public bool HasDecreased(float threshold = 0.0001f)
            {
                return Previous - Raw > threshold;
            }
            public bool HasChanged(float threshold = 0.0001f)
            {
                return Mathf.Abs(Previous - Raw) > threshold;
            }
            public RefFill(float v)
            {
                Raw = v;
            }
            public float Previous;
            private float raw;
        }
    }
}
