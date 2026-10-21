// パッチの条件分岐をゲームの外で動かすための最小 fixture。
// Harmony の登録、ゲームの CanMine/CanPerform、採掘処理の再現ではない。
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class HarmonyPatch(Type type, string name) : Attribute
    {
        public Type TargetType { get; } = type;
        public string TargetName { get; } = name;
    }
}

namespace ElinQoL
{
    internal static class Plugin
    {
        internal static bool MineOnMoveEnabled = true;
    }
}

public enum ActInput { Key, LeftMouse, RightMouse, AllAction }
public class Act { }
public class Card { }
public class Trait { public bool IsTool = true; }
public class Thing : Card
{
    public Trait trait = new();
    public bool HasMining = true;
    public bool HasElement(int id) => id == 220 && HasMining;
}
public class Cell { public bool outOfBounds; }
public class Point
{
    public int x, z;
    public bool IsValid = true, IsHidden, HasBlock = true;
    public Cell cell = new();
    public int Distance(Point p) => Math.Max(Math.Abs(x - p.x), Math.Abs(z - p.z));
    public Point Copy() => new() { x = x, z = z };
}
public class Chara
{
    public bool isDead;
    public Point pos = new();
    public Card? held = new Thing();
    public AIAct ai = new();
}
public class Zone { public bool IsRegion; }
public class UI { public bool IsDragging; }
public static class EClass
{
    public static Chara? pc = new();
    public static Zone? _zone = new();
    public static UI? ui = new();
}
public class ButtonState { public bool pressing; }
public static class EInput
{
    public static bool isAltDown;
    public static ButtonState rightMouse = new();
}
public class AM_Adv
{
    public static int actCount;
    public ActPlan planKeyboard = new();
    public PressedAction pressedAction = new();
    public class PressedAction
    {
        public object? button;
        public Func<bool>? action;
        public int axis;
        public void Init() { button = null; action = null; axis = 0; }
    }
}
public static class ActionMode { public static AM_Adv Adv = new(); }
public class AIAct : Act
{
    public enum Status { Running, Fail, Success }
    public bool IsRunning;
    public Status status;
}
public static class HotItemHeld { public static bool disableTool; }
public class TaskMine : AIAct
{
    public Point pos = new();
    public static bool MineAllowed = true;
    public bool PerformAllowed = true;
    public bool isDestroyed;
    public bool CanPerform(Chara? cc, Card? tc, Point tp) => PerformAllowed;
    public static bool CanMine(Point pos, Card tool) => MineAllowed;
}
public class ActPlan
{
    public class Item
    {
        public Act act = new();
        public Point pos = new();
        public bool Perform(bool repeated = false) => false;
    }
    public ActInput input = ActInput.Key;
    public Point pos = new() { x = 1 };
    public List<Act> Actions = new();
    public List<Item> list = new();
    public bool HasAct => Actions.Count > 0;
    public bool CanPerform = true;
    public void _Update() { }
    public bool TrySetAct(Act act)
    {
        if (!CanPerform) return false;
        Actions.Add(act);
        list.Add(new Item { act = act, pos = pos.Copy() });
        return true;
    }
}
