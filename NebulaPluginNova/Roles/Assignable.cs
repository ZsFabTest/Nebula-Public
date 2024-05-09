﻿using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Game;
using Virial.Text;

namespace Nebula.Roles;

public interface IConfiguableAssignable
{
    ConfigurationHolder RoleConfig { get; }
}

public interface ICodeName
{
    string CodeName { get; }
}

public interface IAssignableBase : DefinedAssignable
{
    public ConfigurationHolder? RelatedConfig { get; }
    public string InternalName { get; }
    public string DisplayName { get; }
    public Color RoleColor { get; }
    public int Id { get; set; }

    public void Load();

    //For Config
    public IEnumerable<IAssignableBase> RelatedOnConfig();


    ValueConfiguration? DefinedAssignable.GetConfiguration(string id) => RelatedConfig?.MyConfigurations.FirstOrDefault(c => c.Id == id);
}

public enum ExtraWinCheckPhase
{
    Phase0,
    Phase1,
    Phase2,
    Phase3,
    PhaseMax,

    LoversPhase = Phase1,
    ObsessionPhase = Phase0,
}

public abstract class AssignableInstance : ComponentHolder, RuntimeAssignable, Virial.IBinderLifespan, IGamePlayerOperator
{
    public virtual IAssignableBase AssignableBase { get; } = null!;
    public GamePlayer MyPlayer { get; private init; }
    public bool AmOwner => MyPlayer.AmOwner;

    public AssignableInstance(GamePlayer player)
    {
        this.MyPlayer = player;
    }

    public void Inactivate()
    {
        this.ReleaseIt();
        OnInactivated();
    }

    public virtual bool CheckWins(CustomEndCondition endCondition,ref ulong extraWinMask) => false;
    public virtual bool BlockWins(CustomEndCondition endCondition) => false;
    public virtual bool CheckExtraWins(CustomEndCondition endCondition, ExtraWinCheckPhase phase, int winnersMask,ref ulong extraWinMask) => false;
    public virtual void OnGameEnd(NebulaEndState endState) { }
    public virtual void LocalUpdate() { }
    public virtual void LocalHudUpdate() { }
    
    public virtual void OnActivated() { }
    public virtual void OnSetTaskLocal(ref List<GameData.TaskInfo> tasks, out int extraQuota) { extraQuota = 0; }
    public virtual void OnTaskCompleteLocal() { }
    protected virtual void OnInactivated() { }
    public virtual string? OverrideRoleName(string lastRoleName,bool isShort) => null;
    public virtual void DecoratePlayerName(ref string text, ref Color color) { }
    public virtual void DecorateOtherPlayerName(GamePlayer player,ref string text, ref Color color) { }
    public virtual void DecorateRoleName(ref string text) { }

    public virtual void EditLightRange(ref float range) { }

    public virtual void OnTieVotes(ref List<byte> extraVotes,PlayerVoteArea myVoteArea) { }

    public virtual string? GetExtraTaskText() => null;

    public virtual KillResult CheckKill(GamePlayer killer, CommunicableTextTag playerState, CommunicableTextTag? eventDetail, bool isMeetingKill) { return KillResult.Kill; }


    //////////////////////////////////////////
    //                                      //
    //              Virial API              //
    //                                      //
    //////////////////////////////////////////


    // Virial.AssignableAPI

    DefinedAssignable RuntimeAssignable.Assignable => AssignableBase;

    GamePlayer RuntimeAssignable.MyPlayer => MyPlayer;


    // Virtial.GameEntityAPI
    GamePlayer IGamePlayerOperator.MyPlayer => MyPlayer;

    // Virial.AssignableAspectAPI

    public virtual bool CanFixLight { get => true; }
    public virtual bool CanFixComm { get => true; }
    public virtual bool CanBeAwareAssignment { get => true; }
    public virtual bool CanCallEmergencyMeeting { get => true; }
}