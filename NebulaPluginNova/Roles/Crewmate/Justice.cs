﻿using Mono.CSharp.Linq;
using Nebula.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Assignable;
using Virial.Game;

namespace Nebula.Roles.Crewmate;

public class Justice : ConfigurableStandardRole, HasCitation
{
    static public Justice MyRole = null;//new Justice();

    public override RoleCategory Category => RoleCategory.CrewmateRole;

    public override string LocalizedName => "justice";
    public override Color RoleColor => new Color(255f / 255f, 128f / 255f, 0f / 255f);
    Citation? HasCitation.Citaion => Citations.SuperNewRoles;
    public override RoleTeam Team => Crewmate.MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    private NebulaConfiguration PutJusticeOnTheBalanceOption = null!;
    protected override void LoadOptions()
    {
        base.LoadOptions();

        RoleConfig.AddTags(ConfigurationHolder.TagSNR);

        PutJusticeOnTheBalanceOption = new(RoleConfig, "putJusticeOnTheBalance", null, false, false);
    }

    public class Instance : Crewmate.Instance, IGameEntity
    {
        public override AbstractRole Role => MyRole;
        public Instance(PlayerModInfo player) : base(player) { }

        static private SpriteLoader meetingSprite = SpriteLoader.FromResource("Nebula.Resources.JusticeIcon.png", 115f);


        bool usedBalance = false;
        bool isMyJusticeMeeting = false;

        void IGameEntity.OnMeetingStart()
        {
            void StartJusticeMeeting(GamePlayer p1, GamePlayer p2)
            {
                MeetingModRpc.RpcChangeVotingStyle.Invoke(((1 << p1.PlayerId) | (1 << p2.PlayerId), false, 100f, true));
                new StaticAchievementToken("justice.common1");
                if(p1.IsImpostor || p2.IsImpostor) new StaticAchievementToken("justice.common2");
                isMyJusticeMeeting = true;
            }

            if (AmOwner && !usedBalance)
            {
                NebulaGameManager.Instance?.MeetingPlayerButtonManager.RegisterMeetingAction(new(meetingSprite,
                   p =>
                   {
                       if (MyRole.PutJusticeOnTheBalanceOption)
                       {
                           StartJusticeMeeting(p.MyPlayer,MyPlayer);
                           usedBalance = true;
                       }
                       else
                       {
                           if (p.IsSelected)
                               p.SetSelect(false);
                           else
                           {
                               var selected = NebulaGameManager.Instance?.MeetingPlayerButtonManager.AllStates.FirstOrDefault(p => p.IsSelected);

                               if (selected != null)
                               {
                                   selected.SetSelect(false);

                                   StartJusticeMeeting(p.MyPlayer,selected.MyPlayer);
                                   usedBalance = true;
                               }
                               else
                               {
                                   p.SetSelect(true);
                               }
                           }
                       }
                   },
                   p => !usedBalance && !p.MyPlayer.IsDead
                   ));
            }
        }

        void IGameEntity.OnMeetingEnd(Virial.Game.Player[] exiled)
        {
            if (AmOwner && isMyJusticeMeeting)
            {
                if (exiled.Any(e => e.AmOwner)) new StaticAchievementToken("justice.another1");
                if(exiled.Length == 2)
                {
                    new StaticAchievementToken("justice.common3");
                    if(exiled.All(e => e.IsImpostor)) new StaticAchievementToken("justice.challenge");
                }

                isMyJusticeMeeting = false;
            }
        }
    }
}