using Nebula.Roles.Complex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Profiling;

namespace Nebula.Roles.Neutral;

// if (IsMySidekick(player)) player.RpcInvokerSetRole(Jackal.MyRole, new int[] { JackalTeamId }).InvokeSingle();
public class Madman : ConfigurableStandardRole
{
    static public Madman MyRole = new Madman();
    //static public Team MyTeam = new("teams.chainShifter", MyRole.RoleColor, TeamRevealType.OnlyMe);

    public override RoleCategory RoleCategory => RoleCategory.NeutralRole;

    public override string LocalizedName => "madman";
    public override Color RoleColor => new Color(191f / 255f, 0f / 255f, 32f / 255f);
    public override Team Team => ChainShifter.MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    private KillCoolDownConfiguration killCooldownOption = null!;
    private NebulaConfiguration canBeGuessOption = null!;

    protected override void LoadOptions()
    {
        base.LoadOptions();

        killCooldownOption = new(RoleConfig, "killCooldown", KillCoolDownConfiguration.KillCoolDownType.Immediate, 2.5f, 2.5f, 60f, -22.5f, 35f, 0.125f, 0.125f, 2f, 17.5f, -7.5f, 0.875f);
        canBeGuessOption = new(RoleConfig, "canBeGuess", null, true, true);
    }

    public override bool CanBeGuess => canBeGuessOption.GetBool();

    public class Instance : RoleInstance
    {
        private ModAbilityButton? killButton = null;

        public override AbstractRole Role => MyRole;

        public Instance(PlayerModInfo player) : base(player)
        {
        }

        private PlayerControl? shiftTarget = null;
        private bool canExecuteShift = false;

        public override void OnActivated()
        {
            if (AmOwner)
            {
                PoolablePlayer? shiftIcon = null;

                var killTracker = Bind(ObjectTrackers.ForPlayer(null, MyPlayer.MyControl, (p) => p.PlayerId != MyPlayer.PlayerId && !p.Data.IsDead));

                killButton = Bind(new ModAbilityButton()).KeyBind(KeyAssignmentType.Kill);
                //chainShiftButton.SetSprite(buttonSprite.GetSprite());
                killButton.Availability = (button) => killTracker.CurrentTarget != null && MyPlayer.MyControl.CanMove && shiftTarget == null;
                killButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead;
                killButton.OnClick = (button) => {
                    PlayerControl.LocalPlayer.ModKill(killTracker.CurrentTarget!, true, PlayerState.Dead, EventDetail.Kill);
                    shiftTarget = killTracker.CurrentTarget;
                    shiftIcon = AmongUsUtil.GetPlayerIcon(shiftTarget.GetModInfo()!.DefaultOutfit, killButton!.VanillaButton.transform, new Vector3(-0.4f, 0.35f, -0.5f), new(0.3f, 0.3f)).SetAlpha(0.5f);
                };
                killButton.OnMeeting = (button) =>
                {
                    if (shiftIcon) GameObject.Destroy(shiftIcon!.gameObject);
                    shiftIcon = null;
                };
                killButton.CoolDownTimer = Bind(new Timer(MyRole.killCooldownOption.CurrentCoolDown).SetAsAbilityCoolDown().Start());
                killButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                killButton.SetLabel("kill");
            }
        }

        //会議開始時に生きていればシフトは実行されうる
        public override void OnMeetingStart()
        {
            canExecuteShift = !MyPlayer.IsDead;
        }

        public override IEnumerator? CoMeetingEnd()
        {
            if (!canExecuteShift) yield break;
            if (shiftTarget == null) yield break;
            if (!shiftTarget) yield break;
            var player = shiftTarget.GetModInfo();

            //会議終了時に死亡している相手とはシフトできない
            if (player == null/* || player!.IsDead*/) yield break;

            int[] targetArgument = new int[0];
            var targetRole = player.Role.Role;
            int targetGuess = -1;
            yield return player.CoGetRoleArgument((args) => targetArgument = args);
            yield return player.CoGetLeftGuess((guess) => targetGuess = guess);

            int myGuess = MyPlayer.TryGetModifier<GuesserModifier.Instance>(out var guesser) ? guesser.LeftGuess : -1;

            using (RPCRouter.CreateSection("ChainShift"))
            {
                player.RpcInvokerSetRole(MyRole, null).InvokeSingle();
                MyPlayer.RpcInvokerSetRole(targetRole, targetArgument).InvokeSingle();

                if (targetGuess != -1) player.RpcInvokerUnsetModifier(GuesserModifier.MyRole).InvokeSingle();
                if (myGuess != -1) MyPlayer.RpcInvokerUnsetModifier(GuesserModifier.MyRole).InvokeSingle();

                if (myGuess != -1) player.RpcInvokerSetModifier(GuesserModifier.MyRole, new int[] { myGuess }).InvokeSingle();
                if (targetGuess != -1) MyPlayer.RpcInvokerSetModifier(GuesserModifier.MyRole, new int[] { targetGuess }).InvokeSingle();

                int leftCrewmateTask = 0;
                if (player.Tasks.IsCrewmateTask)
                {
                    leftCrewmateTask = Mathf.Max(0, player.Tasks.Quota - player.Tasks.TotalCompleted);

                }

                if (leftCrewmateTask > 0)
                {
                    int commonTasks = GameOptionsManager.Instance.CurrentGameOptions.GetInt(AmongUs.GameOptions.Int32OptionNames.NumCommonTasks);
                    int shortTasks = GameOptionsManager.Instance.CurrentGameOptions.GetInt(AmongUs.GameOptions.Int32OptionNames.NumShortTasks);
                    int longTasks = GameOptionsManager.Instance.CurrentGameOptions.GetInt(AmongUs.GameOptions.Int32OptionNames.NumLongTasks);
                    float longWeight = (float)longTasks / (float)(commonTasks + shortTasks + longTasks);
                    float commonWeight = (float)commonTasks / (float)(commonTasks + shortTasks + longTasks);

                    int actualLongTasks = (int)((float)System.Random.Shared.NextDouble() * longWeight * leftCrewmateTask);
                    int actualcommonTasks = (int)((float)System.Random.Shared.NextDouble() * commonWeight * leftCrewmateTask);

                    MyPlayer.Tasks.ReplaceTasksAndRecompute(leftCrewmateTask - actualLongTasks - actualcommonTasks, actualLongTasks, actualcommonTasks);
                    MyPlayer.Tasks.BecomeToCrewmate();
                }
                else
                {
                    MyPlayer.Tasks.ReleaseAllTaskState();
                }
            }

            //yield return new WaitForSeconds(0.2f);

            yield break;
        }

        public override void OnMeetingEnd()
        {
            shiftTarget = null;
        }
    }
}
