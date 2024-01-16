﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Modifier;



public class TieBreaker : ConfigurableStandardModifier
{
    static public TieBreaker MyRole = new TieBreaker();
    public override string LocalizedName => "tieBreaker";
    public override string CodeName => "TBR";
    public override Color RoleColor => new Color(239f / 255f, 175f / 255f, 135f / 255f);
    public override ModifierInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    [NebulaRPCHolder]
    public class Instance : ModifierInstance
    {
        public override AbstractModifier Role => MyRole;

        AchievementToken<bool>? acTokenCommon;
        AchievementToken<(bool cleared, byte lastTieVoted)>? acTokenChallenge;

        public Instance(PlayerModInfo player) : base(player)
        {
        }


        public override void OnActivated()
        {
            if (AmOwner)
            {
                acTokenCommon = new("tieBreaker.common1", false, (val, _) => val);
                acTokenChallenge = new("tieBreaker.challenge", (false, 255), (val, _) => val.cleared);
            }
        }

        public override void OnMeetingStart()
        {
            if (acTokenChallenge != null) acTokenChallenge.Value.lastTieVoted = 255;
        }

        static public RemoteProcess<(byte playerId, byte votedFor)> RpcNoticeTieBreak = new("NoticeTieBreak", (message, _) => {
            if(message.playerId == PlayerControl.LocalPlayer.PlayerId && (NebulaGameManager.Instance?.GetModPlayerInfo(message.playerId)?.TryGetModifier<Instance>(out var role) ?? false)) { 
                if (role.acTokenCommon != null) role.acTokenCommon.Value = true;
                if (role.acTokenChallenge != null) role.acTokenChallenge.Value.lastTieVoted = message.votedFor;
            }
        });

        public override void OnTieVotes(ref List<byte> extraVotes, PlayerVoteArea myVoteArea)
        {
            if (!myVoteArea.DidVote) return;
            extraVotes.Add(myVoteArea.VotedFor);
        }

        public override void DecoratePlayerName(ref string text, ref Color color)
        {
            if (AmOwner || (NebulaGameManager.Instance?.CanSeeAllInfo ?? false)) text += " ♠".Color(MyRole.RoleColor);
        }
    }
}
