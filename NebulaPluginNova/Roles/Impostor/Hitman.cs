using AmongUs.GameOptions;
using Nebula.Configuration;
using Nebula.Modules.ScriptComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Impostor;

public class Hitman : ConfigurableStandardRole
{
    static public Hitman MyRole = new Hitman();
    public override RoleCategory RoleCategory => RoleCategory.ImpostorRole;

    public override string LocalizedName => "hitman";
    public override Color RoleColor => Palette.ImpostorRed;
    public override Team Team => Impostor.MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    private KillCoolDownConfiguration KillCoolDownOption = null!;
    private NebulaConfiguration NumOfKillingOption = null!;
    protected override void LoadOptions()
    {
        base.LoadOptions();

        KillCoolDownOption = new(RoleConfig, "killCoolDown", KillCoolDownConfiguration.KillCoolDownType.Immediate, 2.5f, 2.5f, 60f, -22.5f, 35f, 0.125f, 0.125f, 2f, 30f, 5f, 1.125f);
        NumOfKillingOption = new(RoleConfig, "numOfKilling", null, 2f, 5f, 1f, 2f, 2f);
    }

    public class Instance : Impostor.Instance
    {
        private ModAbilityButton? killButton = null;

        public override AbstractRole Role => MyRole;
        public override bool HasVanillaKillButton => false;
        public Instance(PlayerModInfo player) : base(player)
        {
        }

        int leftKillingData = 0;
        bool pauseCooldown = false;
        TMPro.TMP_Text usedIcon = null!;

        public override void OnActivated()
        {
            base.OnActivated();

            if (AmOwner)
            {
                pauseCooldown = false;
                leftKillingData = 0;
                var killTracker = Bind(ObjectTrackers.ForPlayer(null, MyPlayer.MyControl, (p) => !p.Data.Role.IsImpostor && !p.Data.IsDead));

                killButton = Bind(new ModAbilityButton(false,true)).KeyBind(KeyAssignmentType.Kill);
                killButton.Availability = (button) => killTracker.CurrentTarget != null && MyPlayer.MyControl.CanMove;
                killButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead;
                killButton.OnClick = (button) => {
                    PlayerControl.LocalPlayer.ModKill(killTracker.CurrentTarget!, true, PlayerState.Dead, EventDetail.Kill);

                    leftKillingData--;
                    usedIcon!.text = leftKillingData.ToString();

                    if (leftKillingData <= 0)
                    {
                        button.CoolDownTimer!.Start(MyRole.KillCoolDownOption.CurrentCoolDown);
                        pauseCooldown = false;
                        killButton.EffectTimer!.SetTime(0f);
                    }
                    else
                    {
                        killButton.ActivateEffect();
                    }
                };
                killButton.EffectTimer = Bind(new Timer(1.5f));
                killButton.OnEffectEnd = (button) => {
                    leftKillingData = 0;
                    usedIcon!.text = "0";
                    button.CoolDownTimer!.Start(MyRole.KillCoolDownOption.CurrentCoolDown);
                    pauseCooldown = false;
                };
                killButton.CoolDownTimer = Bind(new Timer(MyRole.KillCoolDownOption.CurrentCoolDown)).Start(10f);
                killButton.SetLabelType(ModAbilityButton.LabelType.Impostor);
                killButton.SetLabel("kill");
                usedIcon = killButton.ShowUsesIcon(2);
                usedIcon!.text = "0";
            }
        }

        public override void LocalUpdate()
        {
            if(!killButton!.CoolDownTimer!.IsInProcess && !pauseCooldown){
                pauseCooldown = true;
                leftKillingData = (int)MyRole.NumOfKillingOption.GetFloat();
                usedIcon!.text = leftKillingData.ToString();
            }
        }
    }
}