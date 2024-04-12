﻿using Virial.Assignable;
using Virial.Text;

namespace Virial.Game;

public interface IPlayerAttribute
{
    internal int Id { get; }
    internal string Name { get; }
    
    /// <summary>
    /// 分類上の属性を取得します。
    /// </summary>
    IPlayerAttribute CategorizedAttribute { get; }

    /// <summary>
    /// この属性のアイコンを指定します。
    /// </summary>
    Media.Image Image { get; }

    /// <summary>
    /// プレイヤーが属性を認識できるかどうか調べます。
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    bool CanCognize(Player player);
}

public static class PlayerAttributes
{
    /// <summary>
    /// 加速効果を表します。速度倍率のパラメータがあります。
    /// </summary>
    static public IPlayerAttribute Accel { get; internal set; }

    /// <summary>
    /// 減速効果を表します。速度倍率のパラメータがあります。
    /// </summary>
    static public IPlayerAttribute Decel { get; internal set; }

    /// <summary>
    /// 速度反転効果を表します。速度倍率のパラメータがあります。
    /// </summary>
    static public IPlayerAttribute Drunk { get; internal set; }

    /// <summary>
    /// サイズ変更効果を表します。サイズ倍率のパラメータがあります。
    /// </summary>
    static public IPlayerAttribute Size { get; internal set; }

    /// <summary>
    /// 透明化効果を表します。
    /// </summary>
    static public IPlayerAttribute Invisible { get; internal set; }

    /// <summary>
    /// Bloodyの血の足跡効果を表します。
    /// </summary>
    static public IPlayerAttribute CurseOfBloody { get; internal set; }

    /// <summary>
    /// Effacerのインポスターにだけ見える透明化効果を表します。
    /// </summary>
    static public IPlayerAttribute InvisibleElseImpostor { get; internal set; }

    /// <summary>
    /// Alienの情報端末からの無縁化効果を表します。
    /// </summary>
    static public IPlayerAttribute Isolation { get; internal set; }

    /// <summary>
    /// Buskerの偽装死を隠蔽する効果を表します。効果は偽装死に限らず適用されます。
    /// </summary>
    static public IPlayerAttribute BuskerEffect { get; internal set; }
}

public interface Player
{
    internal PlayerControl VanillaPlayer { get; }

    public string Name { get; }
    public byte PlayerId { get; }
    public bool IsDead { get; }
    public bool AmOwner { get; }
    public bool AmHost { get; }
    public void MurderPlayer(Player player, CommunicableTextTag playerState, CommunicableTextTag eventDetail, bool showBlink, bool showKillOverlay);
    public void Suicide(CommunicableTextTag playerState, CommunicableTextTag eventDetail,bool showKillOverlay);
    public void GainAttribute(IPlayerAttribute attribute, float duration, bool canPassMeeting, int priority, string? duplicateTag = null);
    public void GainAttribute(float speedRate, float duration, bool canPassMeeting, int priority, string? duplicateTag = null);
    public bool HasAttribute(IPlayerAttribute attribute);

    public RuntimeRole Role { get; }
    public IEnumerable<RuntimeModifier> Modifiers { get; }

    public bool IsImpostor => Role.Role.Category == RoleCategory.ImpostorRole;
}
