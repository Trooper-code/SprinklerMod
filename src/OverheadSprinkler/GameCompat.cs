using System;
using S1API.Utils;
using UnityEngine.UI;
#if IL2CPP
using Il2CppInterop.Runtime.InteropTypes;
#endif

namespace OverheadSprinkler;

/// <summary>
/// Cross-runtime helpers bridging Mono and IL2CPP proxy differences.
/// </summary>
internal static class GameCompat
{
#if IL2CPP
    public static T? TryCast<T>(object? obj) where T : Il2CppObjectBase
    {
        if (obj is Il2CppObjectBase il2cppObj)
        {
            return il2cppObj.TryCast<T>();
        }
        return obj as T;
    }
#else
    public static T? TryCast<T>(object? obj) where T : class
    {
        return obj as T;
    }
#endif

    public static void AddButtonListener(Button button, Action callback)
    {
        EventHelper.AddListener(callback, button.onClick);
    }
}
