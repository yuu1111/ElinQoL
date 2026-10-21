using ElinQoL;
using ElinQoL.Features.Mining;

// 契約: 移動先に標準の採掘候補を追加する。マウス操作や既存行動を横取りしない。
// 本番パッチをリンクして実行する。Unity 内の入力と採掘完了は実機確認が必要。
var cases = new (string Name, Action<ActPlan> Arrange)[]
{
    ("feature disabled", _ => Plugin.MineOnMoveEnabled = false),
    ("left click", p => p.input = ActInput.LeftMouse),
    ("right click", p => p.input = ActInput.RightMouse),
    ("context menu", p => p.input = ActInput.AllAction),
    ("existing action wins", p => p.Actions.Add(new Act())),
    ("no player", _ => EClass.pc = null),
    ("dead player", _ => EClass.pc!.isDead = true),
    ("no zone", _ => EClass._zone = null),
    ("world map", _ => EClass._zone!.IsRegion = true),
    ("no UI", _ => EClass.ui = null),
    ("dragging", _ => EClass.ui!.IsDragging = true),
    ("alt held", _ => EInput.isAltDown = true),
    ("tool disabled", _ => HotItemHeld.disableTool = true),
    ("mouse mining active", _ => EInput.rightMouse.pressing = true),
    ("mouse repetition pending", _ => AM_Adv.actCount = 1),
    ("invalid point", p => p.pos.IsValid = false),
    ("map border", p => p.pos.cell.outOfBounds = true),
    ("unexplored", p => p.pos.IsHidden = true),
    ("empty floor", p => p.pos.HasBlock = false),
    ("own tile", p => p.pos.x = 0),
    ("distant wall", p => p.pos.x = 2),
    ("empty hands even if inventory has a pickaxe", _ => EClass.pc!.held = null),
    ("held non-thing", _ => EClass.pc!.held = new Card()),
    ("not a tool", _ => ((Thing)EClass.pc!.held!).trait.IsTool = false),
    ("non-mining tool", _ => ((Thing)EClass.pc!.held!).HasMining = false),
    ("game rejects tool or wall", _ => TaskMine.MineAllowed = false),
    ("game rejects action", p => p.CanPerform = false),
};

foreach (var (name, arrange) in cases)
{
    Reset();
    var plan = new ActPlan();
    arrange(plan);
    var original = plan.Actions.ToArray();
    MineOnMovePatch.Postfix(plan);
    Check(plan.Actions.SequenceEqual(original), name);
}

// 方向に依存せず、隣接した壁だけに候補を作る。斜め方向も通常の隣接距離に従う。
foreach (var (x, z) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1) })
{
    Reset();
    var plan = new ActPlan { pos = new Point { x = x, z = z } };
    MineOnMovePatch.Postfix(plan);
    Check(plan.Actions.Count == 1 && plan.Actions[0] is TaskMine, $"mine at {x},{z}");
    var task = (TaskMine)plan.Actions[0];
    Check(task.pos.x == x && task.pos.z == z, "correct target");
    plan.pos.x = 50;
    Check(task.pos.x == x, "target is a snapshot, not the mutable plan point");
    MineOnMovePatch.Postfix(plan);
    Check(plan.Actions.Count == 1, "no duplicate actions");
}

// 実行直前の判定。標準 Item.Perform へ渡す repeated 引数が true のままだと、
// キー移動先をマウス位置に置換する分岐に入る。今回のタスクだけ false にする。
Reset();
var keyboardPlan = new ActPlan();
MineOnMovePatch.Postfix(keyboardPlan);
var keyboardTask = (TaskMine)keyboardPlan.Actions.Single();
var item = new ActPlan.Item { act = keyboardTask, pos = new Point { x = 1 } };
var repeated = true;
var result = true;
Check(MineOnMovePatch.PerformPatch.Prefix(item, ref repeated, ref result), "native Perform still runs");
Check(!repeated, "keyboard target must not be replaced by mouse target");

// 所有していない標準採掘を変えない。
var mouseItem = new ActPlan.Item { act = new TaskMine(), pos = new Point { x = 4 } };
repeated = true;
Check(MineOnMovePatch.PerformPatch.Prefix(mouseItem, ref repeated, ref result) && repeated,
    "standard mouse mining unchanged");

// 候補表示後に状態が変わるケース。警告ダイアログ待ちも同じ入口を通る。
foreach (var (name, change) in new (string, Action)[]
{
    ("tool put away", () => EClass.pc!.held = null),
    ("disabled after planning", () => Plugin.MineOnMoveEnabled = false),
    ("wall gone", () => item.pos.HasBlock = false),
    ("already mining", () => EClass.pc!.ai = new TaskMine { IsRunning = true }),
    ("action no longer allowed", () => keyboardTask.PerformAllowed = false),
    ("mouse pressed after planning", () => EInput.rightMouse.pressing = true),
})
{
    Reset();
    item.pos.HasBlock = true;
    keyboardTask.PerformAllowed = true;
    change();
    repeated = true;
    result = true;
    Check(!MineOnMovePatch.PerformPatch.Prefix(item, ref repeated, ref result) && !result, name);
}

// 採掘完了やキャンセル後、同じ方向を押し続けても移動判定に戻れる。
foreach (var finish in new Action<TaskMine, Point>[]
{
    (t, p) => p.HasBlock = false,
    (t, p) => t.status = AIAct.Status.Fail,
    (t, p) => t.status = AIAct.Status.Success,
    (t, p) => t.isDestroyed = true,
})
{
    Reset();
    var plan = new ActPlan();
    MineOnMovePatch.Postfix(plan);
    var task = (TaskMine)plan.Actions.Single();
    var pending = plan.list.Single();
    ActionMode.Adv.planKeyboard = plan;
    var pressed = ActionMode.Adv.pressedAction;
    pressed.axis = 1;
    pressed.action = () => false;
    finish(task, pending.pos);
    repeated = true;
    result = true;
    Check(!MineOnMovePatch.PerformPatch.Prefix(pending, ref repeated, ref result), "finished task not reused");
    Check(pressed.axis == 0 && pressed.action == null, "return to movement after completion or cancel");
}

Reset();
var activePlan = new ActPlan();
MineOnMovePatch.Postfix(activePlan);
ActionMode.Adv.planKeyboard = activePlan;
ActionMode.Adv.pressedAction.axis = 1;
EClass.pc!.ai = new TaskMine { IsRunning = true };
repeated = true;
Check(!MineOnMovePatch.PerformPatch.Prefix(activePlan.list.Single(), ref repeated, ref result) &&
    ActionMode.Adv.pressedAction.axis == 1, "running mining is not restarted or released");

// 遅延した犯罪警告のcallbackが、後から選んだマウス操作を解除しない。
Reset();
ActionMode.Adv.planKeyboard = activePlan;
ActionMode.Adv.pressedAction.axis = 1;
ActionMode.Adv.pressedAction.button = new object();
activePlan.list[0].pos.HasBlock = false;
MineOnMovePatch.PerformPatch.Prefix(activePlan.list[0], ref repeated, ref result);
Check(ActionMode.Adv.pressedAction.button != null, "new mouse action is preserved");

Console.WriteLine($"PASS: {cases.Length} exclusions, 6 directions, target safety, and movement resumption.");

static void Reset()
{
    Plugin.MineOnMoveEnabled = true;
    EClass.pc = new Chara();
    EClass._zone = new Zone();
    EClass.ui = new UI();
    EInput.isAltDown = false;
    HotItemHeld.disableTool = false;
    EInput.rightMouse.pressing = false;
    AM_Adv.actCount = 0;
    ActionMode.Adv = new AM_Adv();
    TaskMine.MineAllowed = true;
}

static void Check(bool condition, string name)
{
    if (!condition) throw new Exception($"FAIL: {name}");
}
