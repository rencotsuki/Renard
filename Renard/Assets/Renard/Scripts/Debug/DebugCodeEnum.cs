using System;

[Flags]
public enum DebugCodeEnum
{
    None = 0,

    /// <summary>FPS表示</summary>
    FPS = 1 << 0,

    /// <summary>アンカーの表示</summary>
    AnchorView = 1 << 1,

    /// <summary>フィールド関連の表示</summary>
    FieldDebug = 1 << 2,

    /// <summary>プレイヤーステータス表示</summary>
    PlayerDebug = 1 << 3,

    /// <summary>弾関連の表示</summary>
    BulletDebug = 1 << 4,

    /// <summary>シールド関連の表示</summary>
    ShieldDebug = 1 << 5
}

public static class DebugCodeEnumExtensions
{
    public static DebugCodeEnum SetTo(this DebugCodeEnum target, DebugCodeEnum status)
    {
        return (target = status);
    }

    public static DebugCodeEnum AddTo(this DebugCodeEnum target, params DebugCodeEnum[] status)
    {
        foreach (var item in status)
        {
            target = target | item;
        }
        return target;
    }

    public static DebugCodeEnum RemoveTo(this DebugCodeEnum target, params DebugCodeEnum[] status)
    {
        foreach (var item in status)
        {
            target = target & ~item;
        }
        return target;
    }

    public static bool IsInclude(this DebugCodeEnum target, DebugCodeEnum status)
    {
        return (target & status) != 0;
    }
}
