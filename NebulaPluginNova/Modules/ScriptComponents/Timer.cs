﻿using Nebula.Game;
using Rewired.Libraries.SharpDX.RawInput;
using Rewired.Utils.Platforms.Windows;
using System.ComponentModel.DataAnnotations;
using Virial.Components;
using static Il2CppSystem.Xml.Schema.FacetsChecker.FacetsCompiler;

namespace Nebula.Modules.ScriptComponents;


[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.IsKillTimerEnabled), MethodType.Getter)]
class PlayerIsKillTimerEnabledPatch
{
    public static void Postfix(PlayerControl __instance, ref bool __result)
    {
        if(Minigame.Instance && Minigame.Instance.TryCast<SwitchMinigame>())
        {
            __result = false;
        }
    }
}

public class Timer : INebulaScriptComponent, GameTimer
{
    private Func<bool>? predicate = null;
    private bool isActive;
    protected float currentTime;
    protected float min, max;

    public float Max => max;

    public Timer Pause()
    {
        isActive = false;
        return this;
    }
    public virtual Timer Start(float? time = null)
    {
        isActive = true;
        currentTime = time.HasValue ? time.Value : max;
        return this;
    }
    public Timer Resume()
    {
        isActive = true;
        return this;
    }
    public Timer Reset()
    {
        currentTime = max;
        return this;
    }
    public Timer SetTime(float time)
    {
        currentTime = time;
        return this;
    }
    public Timer SetRange(float min, float max)
    {
        if (min > max)
        {
            this.max = min;
            this.min = max;
        }
        else
        {
            this.max = max;
            this.min = min;
        }
        return this;
    }
    public Timer Expand(float time)
    {
        this.max += time;
        return this;
    }

    public float CurrentTime { get => currentTime; }
    public virtual float Percentage { get => max > min ? (currentTime - min) / (max - min) : 0f; }
    public bool IsInProcess => CurrentTime > min;

    public override void Update()
    {
        if (isActive && (predicate?.Invoke() ?? true))
            currentTime = Mathf.Clamp(currentTime - Time.deltaTime, min, max);
    }

    public Timer(float max) : this(0f, max) { }

    public Timer(float min, float max)
    {
        NebulaGameManager.Instance?.RegisterComponent(this);
        SetRange(min, max);
        Reset();
        Pause();
    }

    public Timer SetPredicate(Func<bool>? predicate)
    {
        this.predicate = predicate;
        return this;
    }

    public void Abandon()
    {
        NebulaGameManager.Instance?.ReleaseComponent(this);
    }

    public Timer SetAsKillCoolDown()
    {
        return SetPredicate(() => PlayerControl.LocalPlayer.IsKillTimerEnabled);
    }

    public Timer SetAsAbilityCoolDown()
    {
        return SetPredicate(() =>
        {
            if (PlayerControl.LocalPlayer.CanMove) return true;
            if (PlayerControl.LocalPlayer.inMovingPlat || PlayerControl.LocalPlayer.onLadder) return true;

            if (Minigame.Instance && 
            ((bool)Minigame.Instance.MyNormTask
            || Minigame.Instance.TryCast<SwitchMinigame>() != null
            || Minigame.Instance.TryCast<IDoorMinigame>() != null
            || Minigame.Instance.TryCast<VitalsMinigame>() != null
            || Minigame.Instance.TryCast<MultistageMinigame>() != null
            )) return true;
            return false;
        });
    }

    GameTimer GameTimer.SetAsKillCoolTimer() => SetAsKillCoolDown();

    GameTimer GameTimer.SetAsAbilityTimer() => SetAsAbilityCoolDown();

    GameTimer GameTimer.Start(float? time) => Start(time);

    GameTimer GameTimer.Pause() => Pause();

    GameTimer GameTimer.Resume() => Resume();

    GameTimer GameTimer.SetRange(float min, float max) => SetRange(min, max);

    GameTimer GameTimer.SetTime(float time) => SetTime(time);

    GameTimer GameTimer.Expand(float time) => Expand(time);
}

public class AdvancedTimer : Timer
{
    protected float visualMax;
    protected float defaultMax;
    public AdvancedTimer(float visualMax,float max) : base(0f, max) {
        this.visualMax = visualMax;
    }

    public AdvancedTimer SetVisualMax(float visualMax)
    {
        this.visualMax = visualMax;
        return this;
    }

    public AdvancedTimer SetDefault(float defaultMax)
    {
        this.defaultMax = defaultMax;
        return this;
    }

    public override Timer Start(float? time = null) => base.Start(time ?? defaultMax);

    public override float Percentage { get => Mathf.Min(1f, visualMax > min ? (currentTime - min) / (visualMax - min) : 0f); }
}
