using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Extensions
{
    public static class Extensions 
    {
        public static float Sqr(this int value) => value * value;
        public static float Sqr(this float value) => value * value;
        public static float Sqrt(this float value) => Mathf.Sqrt(value);
        public static int Floor(this float val) => Mathf.FloorToInt(val);
        public static int Ceil(this float val) => Mathf.CeilToInt(val);
        public static int Saturate(this int val) => (int)Mathf.Clamp01(val);
        public static float Saturate(this float val) => Mathf.Clamp01(val);
        public static int Mod(this int a, int b) => (a % b + b) % b;
        public static float Mod(this float a, float b) => (a % b + b) % b;

        public static string GetText(this ReadyStartObjectButton.Mode modeToGet) => modeToGet switch{
            ReadyStartObjectButton.Mode.NotReady => "Not Ready",
            ReadyStartObjectButton.Mode.Ready => "Ready",
            ReadyStartObjectButton.Mode.Start => "Start Match",
            _ => ""
        };

        public static void ForEach<T>(this IEnumerable<T> source, System.Action<T> action)
        {
            foreach(T item in source)
                action(item);
        }

        public static string GetStringFromSeconds(this float seconds) => seconds < 60 
            ? @"%s\.f" 
            : seconds < 3600
                ? @"%m\:%s\.f"
                : @"%h\:%m\:%s\.f";

        public static bool TryGetComponentInChildren<T>(this GameObject parent, out T component) where T : Component
        {
            component = parent.GetComponentInChildren<T>();
            return component != null;
        }
        public static readonly Vector3 invalidMove = new Vector3(-1, -1, (int)MoveType.None);

        public static float PercentInRange(this float from, float toLimit1, float toLimit2) => toLimit1 + ((toLimit2 - toLimit1) * Mathf.Clamp(from, 0, 1));
        public static float Remap(this float from, float fromLimit1, float fromLimit2, float toLimit1, float toLimit2) =>
            PercentInRange(
                (Mathf.Clamp(from, fromLimit1, fromLimit2) - fromLimit1)/(fromLimit2 - fromLimit1), 
                toLimit1, toLimit2
            );

        public static void Deselect(this EventSystem eventSystem) => eventSystem.SetSelectedGameObject(null);

        public static T ChooseRandom<T>(this List<T> set) => set[UnityEngine.Random.Range(0, set.Count)];
        public static T ChooseRandom<T>(this T[] set) => set[UnityEngine.Random.Range(0, set.Length)];
    }
}