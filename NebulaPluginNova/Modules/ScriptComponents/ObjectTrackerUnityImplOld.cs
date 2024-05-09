﻿using Virial.Components;
using Virial.Events.Game;
using Virial.Game;

namespace Nebula.Modules.ScriptComponents;


public static class ObjectTrackers
{
    static public Predicate<GamePlayer> StandardPredicate = p => !p.AmOwner && !p.IsDead;
    static public Predicate<GamePlayer> ImpostorKillPredicate = p => !p.AmOwner && !p.IsDead && !p.IsImpostor;


    public static ObjectTracker<GamePlayer> ForPlayer(float? distance, GamePlayer tracker, Predicate<GamePlayer> predicate, UnityEngine.Color? color = null, bool canTrackInVent = false, bool ignoreCollider = false) => ForPlayer(distance, tracker, predicate, null, color ?? UnityEngine.Color.yellow, canTrackInVent, ignoreCollider);
    public static ObjectTracker<GamePlayer> ForPlayer(float? distance, GamePlayer tracker, Predicate<GamePlayer> predicate, Predicate<GamePlayer>? predicateHeavier, UnityEngine.Color? color, bool canTrackInVent = false, bool ignoreCollider = false)
    {
        if (!canTrackInVent)
        {
            var lastPredicate = predicate;
            predicate = p => !p.VanillaPlayer.inVent && lastPredicate(p);
        }
        IEnumerable<PlayerControl> FastPlayers() => PlayerControl.AllPlayerControls.GetFastEnumerator();

        return new ObjectTrackerUnityImpl<GamePlayer, PlayerControl>(tracker.VanillaPlayer, distance ?? AmongUsUtil.VanillaKillDistance, FastPlayers, predicate, predicateHeavier, p => p.GetModInfo(), p => p.GetTruePosition(), p => p.cosmetics.currentBodySprite.BodySprite, color, ignoreCollider);
    }

    public static ObjectTracker<GamePlayer> ForDeadBody(float? distance, GamePlayer tracker, Predicate<GamePlayer> predicate, Predicate<GamePlayer>? predicateHeavier = null, UnityEngine.Color? color = null, bool ignoreCollider = false)
    {
        return new ObjectTrackerUnityImpl<GamePlayer, DeadBody>(tracker.VanillaPlayer, distance ?? AmongUsUtil.VanillaKillDistance, () => Helpers.AllDeadBodies().Where(d => d.bodyRenderers.Any(r => r.enabled)), predicate, predicateHeavier, d => NebulaGameManager.Instance.GetPlayer(d.ParentId), d => d.TruePosition, d => d.bodyRenderers[0], color, ignoreCollider);
    }
}



public class ObjectTrackerUnityImpl<V,T> : INebulaScriptComponent, ObjectTracker<V>, IGameOperator where T : MonoBehaviour where V : class
{
    V? ObjectTracker<V>.CurrentTarget => currentTarget?.Item2;
    bool ObjectTracker<V>.IsLocked { get => isLocked; set => isLocked = value; }

    private Tuple<T,V>? currentTarget = null;

    PlayerControl tracker;
    Func<IEnumerable<T>> allTargets;
    Predicate<V> predicate;
    Predicate<V>? predicateHeavier = null;
    Func<T, V> converter;
    Func<T, UnityEngine.Vector2> positionConverter;
    Func<T, SpriteRenderer> rendererConverter;
    UnityEngine.Color color = UnityEngine.Color.yellow;
    bool ignoreColliders;
    float maxDistance;
    private bool isLocked = false;

    public ObjectTrackerUnityImpl(PlayerControl tracker, float maxDistance, Func<IEnumerable<T>> allTargets, Predicate<V> predicate, Predicate<V>? predicateHeavier, Func<T, V> converter, Func<T, Vector2> positionConverter, Func<T, SpriteRenderer> rendererConverter, Color? color = null, bool ignoreColliders = false)
    {
        this.tracker = tracker;
        this.allTargets = allTargets;
        this.predicate = predicate;
        this.predicateHeavier = predicateHeavier;
        this.converter = converter;
        this.positionConverter = positionConverter;
        this.rendererConverter = rendererConverter;
        this.maxDistance = maxDistance;
        this.ignoreColliders = ignoreColliders;
        if(color.HasValue) this.color = color.Value;
    }

    private void ShowTarget()
    {
        if (currentTarget == null) return;

        var renderer = rendererConverter.Invoke(currentTarget!.Item1);
        renderer.material.SetFloat("_Outline", 1f);
        renderer.material.SetColor("_OutlineColor", color);
    }

    void HudUpdate(GameHudUpdateEvent ev)
    {
        if (isLocked)
        {
            ShowTarget();
            return;
        }

        if (!tracker)
        {
            currentTarget = null;
            return;
        }

        Vector2 myPos = tracker.GetTruePosition();

        float distance = maxDistance;
        Tuple<T,V>? candidate = null;

        foreach (var t in allTargets())
        {
            if (!t) continue;

            var v = converter(t);
            if (v == null) continue;

            if (!predicate(v)) continue;

            Vector2 pos = positionConverter(t);
            Vector2 dVec = pos - myPos;
            float magnitude = dVec.magnitude;
            if (distance < magnitude) continue;

            if (!ignoreColliders && PhysicsHelpers.AnyNonTriggersBetween(myPos, dVec.normalized, magnitude, Constants.ShipAndObjectsMask)) continue;
            if (!(predicateHeavier?.Invoke(v) ?? true)) continue;

            candidate = new(t,v);
            distance = magnitude;
        }

        currentTarget = candidate;
        ShowTarget();
    }
}