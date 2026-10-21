using System.Runtime.CompilerServices;
using HarmonyLib;

namespace ElinQoL.Features.Mining;

// 移動用プランにだけ採掘を追加し、ゲーム標準の実行経路へ渡す。
[HarmonyPatch(typeof(ActPlan), nameof(ActPlan._Update))]
internal static class MineOnMovePatch
{
    private static readonly ConditionalWeakTable<TaskMine, object> tasks = new();

    internal static void Postfix(ActPlan __instance)
    {
        if (!Plugin.MineOnMoveEnabled || __instance.input != ActInput.Key || __instance.HasAct ||
            !CanMine(__instance.pos))
            return;

        var task = new TaskMine { pos = __instance.pos.Copy() };
        if (__instance.TrySetAct(task))
            tasks.Add(task, new object());
    }

    private static bool CanMine(Point pos)
    {
        var pc = EClass.pc;
        if (!Plugin.MineOnMoveEnabled || pc is null || pc.isDead ||
            EClass._zone == null || EClass._zone.IsRegion || EClass.ui == null || EClass.ui.IsDragging ||
            EInput.isAltDown || HotItemHeld.disableTool || EInput.rightMouse.pressing || AM_Adv.actCount != 0)
            return false;

        if (!pos.IsValid || pos.cell.outOfBounds || pos.IsHidden || !pos.HasBlock ||
            pc.pos.Distance(pos) != 1)
            return false;

        // インベントリ内・装備中ではなく手持ちの採掘道具だけを使う。
        // 220 は標準の HotItemHeld と同じ採掘能力の判定。
        var held = pc.held as Thing;
        return held != null && held.trait.IsTool && held.HasElement(220) && TaskMine.CanMine(pos, held);
    }

    [HarmonyPatch(typeof(ActPlan.Item), nameof(ActPlan.Item.Perform))]
    internal static class PerformPatch
    {
        internal static bool Prefix(ActPlan.Item __instance, ref bool repeated, ref bool __result)
        {
            // 他の Mod や右クリック採掘のタスクには触れない。
            if (__instance.act is not TaskMine task || !tasks.TryGetValue(task, out _))
                return true;

            __result = false;
            // 犯罪警告の確認待ちなどで状況が変わっていれば実行しない。
            if (task.isDestroyed || task.status != AIAct.Status.Running)
            {
                ReleaseKeyboardAction(task);
                return false;
            }

            var pc = EClass.pc;
            if (pc is not null && pc.ai is TaskMine && pc.ai.IsRunning)
                return false;

            if (!CanMine(__instance.pos) || pc is null || !task.CanPerform(pc, null, __instance.pos))
            {
                ReleaseKeyboardAction(task);
                return false;
            }

            // GetAction は初回から repeated=true を渡す。
            // その分岐は採掘先をマウス位置へ書き換えるため、今回のタスクだけ無効化。
            // 犯罪警告・SetAIImmediate・ターン処理は元の Perform に任せる。
            repeated = false;
            return true;
        }

        private static void ReleaseKeyboardAction(TaskMine task)
        {
            var adv = ActionMode.Adv;
            // 自分のキー操作だけを解除し、次フレームの AxisMove で移動先を再判定する。
            // 犯罪警告待ちに別の操作へ切り替わっていれば、その操作には触れない。
            if (adv != null && adv.pressedAction.button == null && adv.planKeyboard.list.Count == 1 &&
                ReferenceEquals(adv.planKeyboard.list[0].act, task))
                adv.pressedAction.Init();
        }
    }
}
