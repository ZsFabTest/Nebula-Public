﻿using Il2CppInterop.Runtime.Injection;
using Nebula.VoiceChat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Virial.Assignable;
using Virial.Game;

namespace Nebula.Roles.Crewmate;

file static class UbiquitousDroneAsset
{
    public static XOnlyDividedSpriteLoader droneSprite = XOnlyDividedSpriteLoader.FromResource("Nebula.Resources.Drone.png", 100f, 4);
    public static SpriteLoader droneShadeSprite = SpriteLoader.FromResource("Nebula.Resources.DroneShade.png", 150f);
}

public class UbiquitousDrone : MonoBehaviour
{
    SpriteRenderer droneRenderer = null!;
    SpriteRenderer shadeRenderer = null!;
    Rigidbody2D myRigidBody = null!;
    const float DroneHeight = 0.95f;
    static UbiquitousDrone() => ClassInjector.RegisterTypeInIl2Cpp<UbiquitousDrone>();

    
    public void Awake()
    {
        myRigidBody = UnityHelper.CreateObject<Rigidbody2D>("DroneBody", transform.parent, transform.localPosition, LayerExpansion.GetPlayersLayer());
        myRigidBody.velocity = Vector2.zero;
        myRigidBody.gravityScale = 0f;
        myRigidBody.freezeRotation = true;
        myRigidBody.fixedAngle = true;
        myRigidBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        myRigidBody.sleepMode = RigidbodySleepMode2D.NeverSleep;
        myRigidBody.interpolation = RigidbodyInterpolation2D.Interpolate;

        CircleCollider2D collider = myRigidBody.gameObject.AddComponent<CircleCollider2D>();
        collider.radius = 0.2f;
        collider.isTrigger = false;

        transform.SetParent(myRigidBody.transform, true);

        //レイヤーがデフォルトなら影内のドローンは見えない
        droneRenderer = UnityHelper.CreateObject<SpriteRenderer>("Renderer", transform, Vector3.zero,LayerExpansion.GetShipLayer());
        droneRenderer.sprite = UbiquitousDroneAsset.droneSprite.GetSprite(0);
        
        shadeRenderer = UnityHelper.CreateObject<SpriteRenderer>("ShadeRenderer", myRigidBody.transform, Vector3.zero, LayerExpansion.GetDefaultLayer());
        shadeRenderer.sprite = UbiquitousDroneAsset.droneShadeSprite.GetSprite();

        IEnumerator CoMoveOffset()
        {
            float t = 0f;
            while (t < 2f)
            {
                t += Time.deltaTime;
                float p = Mathf.Pow(t / 2f, 0.7f);
                transform.localPosition = new(0f, p * DroneHeight);
                shadeRenderer.color = new(1f, 1f, 1f, 1f - p * 0.5f);
                yield return null;
            }
        }

        StartCoroutine(CoMoveOffset().WrapToIl2Cpp());
    }

    float updateTimer = 0f;
    int imageIndex = 0;
    
    public Vector3 ColliderPosition => myRigidBody.transform.position;

    void UpdateSprite()
    {
        imageIndex = (imageIndex + 1) % 2;

        if (Math.Abs(myRigidBody.velocity.x) > 0.1f)
        {
            //横方向へ移動中
            droneRenderer.flipX = myRigidBody.velocity.x < 0f;
            droneRenderer.sprite = UbiquitousDroneAsset.droneSprite.GetSprite(2 + imageIndex);
        }
        else {
            //静止
            droneRenderer.sprite = UbiquitousDroneAsset.droneSprite.GetSprite(imageIndex);
        }
    }

    public void FixedUpdate()
    {
        bool isOperating = HudManager.Instance.PlayerCam.Target == this;
        if (isOperating)
            myRigidBody.velocity = DestroyableSingleton<HudManager>.Instance.joystick.DeltaL * 3.5f;
        else
            myRigidBody.velocity = Vector2.zero;

        var pos = myRigidBody.transform.position;
        pos.z = pos.y / 1000f;
        myRigidBody.transform.position = pos;
    }

    public void Update()
    {
        updateTimer -= Time.deltaTime;
        if(updateTimer < 0f)
        {
            UpdateSprite();
            updateTimer = 0.15f;
        }

        droneRenderer.transform.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 1.4f) * 0.08f, -3f);
    }

    public void CallBack()
    {
        IEnumerator CoCallBack()
        {
            float t = 0f;
            while (t < 0.8f)
            {
                t += Time.deltaTime;
                float p = Mathf.Pow(t / 0.8f, 4.5f);
                transform.localPosition = new(0f, DroneHeight + p * 1.25f);
                droneRenderer.color = new(1f, 1f, 1f, 1f - p);
                shadeRenderer.color = new(1f, 1f, 1f, 0.5f - p * 0.5f);
                yield return null;
            }

            DestroyDroneObject();
        }
        StartCoroutine(CoCallBack().WrapToIl2Cpp());
    }

    public void DestroyDroneObject()
    {
        GameObject.Destroy(myRigidBody.gameObject);
    }
}

public class UbiquitousDetachedDrone : MonoBehaviour, IVoiceComponent
{
    SpriteRenderer droneRenderer = null!;
    static UbiquitousDetachedDrone() => ClassInjector.RegisterTypeInIl2Cpp<UbiquitousDetachedDrone>();

    public void Awake()
    {
        droneRenderer = UnityHelper.CreateObject<SpriteRenderer>("Renderer", transform, new(0f,0f,-3f), LayerExpansion.GetShipLayer());
        droneRenderer.sprite = UbiquitousDroneAsset.droneSprite.GetSprite(0);

        var shadeRenderer = UnityHelper.CreateObject<SpriteRenderer>("ShadeRenderer", transform, Vector3.zero, LayerExpansion.GetDefaultLayer());
        shadeRenderer.sprite = UbiquitousDroneAsset.droneShadeSprite.GetSprite();
        shadeRenderer.color = new(1f, 1f, 1f, 0.5f);

        var pos = transform.position;
        pos.z = pos.y / 1000f;
        transform.position = pos;
    }

    float updateTimer = 0f;
    int imageIndex = 0;

    void UpdateSprite()
    {
        imageIndex = (imageIndex + 1) % 2;
        droneRenderer.sprite = UbiquitousDroneAsset.droneSprite.GetSprite(imageIndex);
    }

    const float DroneHeight = 0.35f;

    float IVoiceComponent.Radious => Ubiquitous.MyRole.droneMicrophoneRadiousOption.GetFloat();

    float IVoiceComponent.Volume => 0.95f;

    Vector2 IVoiceComponent.Position => transform.position;

    public void Update()
    {
        updateTimer -= Time.deltaTime;
        if (updateTimer < 0f)
        {
            UpdateSprite();
            updateTimer = 0.15f;
        }

        droneRenderer.transform.localPosition = new Vector3(0f,  Mathf.Sin(Time.time * 1.4f) * 0.08f + DroneHeight, -3f);
    }

    bool IVoiceComponent.CanPlaySoundFrom(IVoiceComponent mic)
    {
        return (mic != (this as IVoiceComponent)) && mic is UbiquitousDetachedDrone;
    }
}

public class UbiquitousMapLayer : MonoBehaviour
{
    ObjectPool<SpriteRenderer> darkIconPool = null!;
    ObjectPool<SpriteRenderer> lightIconPool = null!;
    List<Vector2> dronePos = null!;
    AchievementToken<bool> challengeToken = null!;

    static UbiquitousMapLayer() => ClassInjector.RegisterTypeInIl2Cpp<UbiquitousMapLayer>();
    public void ReferenceDronePos(List<Vector2> list)
    {
        dronePos = list;
    }

    public void Awake()
    {
        darkIconPool = new(ShipStatus.Instance.MapPrefab.HerePoint, transform);
        darkIconPool.OnInstantiated = icon => PlayerMaterial.SetColors(new Color(0.3f, 0.3f, 0.3f), icon);

        lightIconPool = new(ShipStatus.Instance.MapPrefab.HerePoint, transform);
        lightIconPool.OnInstantiated = icon => PlayerMaterial.SetColors(new Color(1f, 1f, 1f), icon);

        challengeToken = new("ubiquitous.challenge",false,(val,_) => val && Ubiquitous.MyRole.droneDetectionRadiousOption.GetFloat() < 3f);
    }

    public void Update()
    {
        darkIconPool.RemoveAll();
        lightIconPool.RemoveAll();

        var center = VanillaAsset.GetMapCenter(AmongUsUtil.CurrentMapId);
        var scale = VanillaAsset.GetMapScale(AmongUsUtil.CurrentMapId);

        int alive = 0, shown = 0;

        foreach (var p in NebulaGameManager.Instance!.AllPlayerInfo())
        {
            //自分自身、死んでいる場合は何もしない
            if (p.AmOwner || p.IsDead) continue;

            alive++;

            //不可視のプレイヤーは何もしない
            if(p.IsInvisible || p.MyControl.inVent) continue;

            foreach (var pos in dronePos)
            {
                float d = pos.Distance(p.MyControl.transform.position);
                if(d < Ubiquitous.MyRole.droneDetectionRadiousOption.GetFloat())
                {
                    var icon = (DynamicPalette.IsLightColor(Palette.PlayerColors[p.PlayerId]) ? lightIconPool : darkIconPool).Instantiate();
                    icon.transform.localPosition = VanillaAsset.ConvertToMinimapPos(p.MyControl.transform.position, center, scale);
                    shown++;
                    if (alive >= 10 && alive == shown) challengeToken.Value = true;
                    break;
                }
            }
        }
    }
}

[NebulaRPCHolder]
public class Ubiquitous : ConfigurableStandardRole
{
    static public Ubiquitous MyRole = new Ubiquitous();

    public override RoleCategory Category => RoleCategory.CrewmateRole;
    public override string LocalizedName => "ubiquitous";
    public override Color RoleColor => new Color(56f / 255f, 155f / 255f, 223f / 255f);
    public override RoleTeam Team => Crewmate.MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);


    public NebulaConfiguration droneMicrophoneRadiousOption = null!;
    public NebulaConfiguration droneDetectionRadiousOption = null!;
    public NebulaConfiguration doorHackCoolDownOption = null!;
    public NebulaConfiguration doorHackRadiousOption = null!;

    protected override void LoadOptions()
    {
        base.LoadOptions();

        RoleConfig.AddTags(ConfigurationHolder.TagFunny, ConfigurationHolder.TagDifficult);

        droneMicrophoneRadiousOption = new(RoleConfig, "microphoneRadious", null, 0f, 5f, 0.25f, 2f, 2f) { Decorator = NebulaConfiguration.OddsDecorator };
        droneDetectionRadiousOption = new(RoleConfig, "detectionRadious", null, 0f, 10f, 0.25f, 2f, 2f) { Decorator = NebulaConfiguration.OddsDecorator };
        doorHackCoolDownOption = new(RoleConfig, "doorHackCoolDown", null, 10f, 120f, 2.5f, 30f, 30f) { Decorator = NebulaConfiguration.SecDecorator };
        doorHackRadiousOption = new(RoleConfig, "doorHackRadious", null, 0f, 10f, 0.25f, 3f, 3f) { Decorator = NebulaConfiguration.OddsDecorator };
    }


    public class Instance : Crewmate.Instance, IGamePlayerEntity
    {
        public override AbstractRole Role => MyRole;
        public Instance(PlayerModInfo player) : base(player)
        {
        }

        UbiquitousDrone? myDrone = null;
        UbiquitousMapLayer? mapLayer = null;
        List<Vector2> dronePos = new();

        static private ISpriteLoader droneButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.DroneButton.png", 115f);
        static private ISpriteLoader callBackButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.DroneCallBackButton.png", 115f);
        static private ISpriteLoader hackButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.DroneHackButton.png", 115f);

        void IGameEntity.OnOpenNormalMap()
        {
            if (AmOwner)
            {
                if (mapLayer is null)
                {
                    mapLayer = UnityHelper.CreateObject<UbiquitousMapLayer>("UbiquitousLayer", MapBehaviour.Instance.transform, new(0, 0, -1f));
                    mapLayer.ReferenceDronePos(dronePos);
                    this.Bind(mapLayer.gameObject);
                }

                mapLayer.gameObject.SetActive(!MeetingHud.Instance);
            }
        }

        void IGameEntity.OnOpenAdminMap()
        {
            if (AmOwner && mapLayer) mapLayer?.gameObject.SetActive(false);
        }
        void IGameEntity.OnMeetingStart()
        {
            if (myDrone)
            {
                AmongUsUtil.SetCamTarget();
                RpcSpawnDetachedDrone.Invoke(myDrone!.ColliderPosition);
                dronePos.Add(myDrone!.ColliderPosition);
                myDrone.DestroyDroneObject();
            }
        }

        public override void OnActivated()
        {
            if (AmOwner)
            {
                ModAbilityButton callBackButton = null!;
                ModAbilityButton droneButton;

                droneButton = Bind(new ModAbilityButton()).KeyBind(Virial.Compat.VirtualKeyInput.Ability).SubKeyBind(Virial.Compat.VirtualKeyInput.AidAction);
                droneButton.SetSprite(droneButtonSprite.GetSprite());
                droneButton.Availability = (button) => MyPlayer.MyControl.CanMove || (myDrone && AmongUsUtil.CurrentCamTarget == myDrone);
                droneButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead;
                droneButton.OnClick = (button) =>
                {
                    if (!myDrone) myDrone = UnityHelper.CreateObject<UbiquitousDrone>("Drone", null, MyPlayer.MyControl.GetTruePosition());
                    AmongUsUtil.ToggleCamTarget(myDrone, null);
                };
                droneButton.CoolDownTimer = Bind(new Timer(0f).SetAsAbilityCoolDown().Start());
                droneButton.SetLabel("drone");
                droneButton.OnSubAction = (button) =>
                {
                    NebulaManager.Instance.ScheduleDelayAction(() =>
                    {
                        if (callBackButton.IsVisible)
                        {
                            droneButton.ResetKeyBind();
                            callBackButton!.KeyBind(Virial.Compat.VirtualKeyInput.Ability);
                            callBackButton!.SubKeyBind(Virial.Compat.VirtualKeyInput.AidAction);
                        }
                    });
                };

                callBackButton = Bind(new ModAbilityButton());
                callBackButton.SetSprite(callBackButtonSprite.GetSprite());
                callBackButton.Availability = (button) => MyPlayer.MyControl.CanMove || (myDrone && AmongUsUtil.CurrentCamTarget == myDrone);
                callBackButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead && myDrone;
                callBackButton.OnClick = (button) =>
                {
                    callBackButton.DoSubClick();
                    AmongUsUtil.SetCamTarget();
                    myDrone!.CallBack();
                    myDrone = null;
                };
                callBackButton.CoolDownTimer = Bind(new Timer(0f).SetAsAbilityCoolDown().Start());
                callBackButton.SetLabel("callBack");
                callBackButton.OnSubAction = (button) =>
                {
                    NebulaManager.Instance.ScheduleDelayAction(() =>
                    {
                        callBackButton.ResetKeyBind();
                        droneButton.KeyBind(Virial.Compat.VirtualKeyInput.Ability);
                        droneButton.SubKeyBind(Virial.Compat.VirtualKeyInput.AidAction);

                    });
                };

                AchievementToken<int> totalAchievement = new("ubiquitous.common1", 0, (val, _) => val >= 5);

                var hackButton = Bind(new ModAbilityButton()).KeyBind(Virial.Compat.VirtualKeyInput.SecondaryAbility);
                hackButton.SetSprite(hackButtonSprite.GetSprite());
                hackButton.Availability = (button) => MyPlayer.MyControl.CanMove || (myDrone && AmongUsUtil.CurrentCamTarget == myDrone);
                hackButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead && myDrone;
                hackButton.OnClick = (button) =>
                {
                    float distance = MyRole.doorHackRadiousOption.GetFloat();
                    foreach(var door in ShipStatus.Instance.AllDoors)
                    {
                        if (!door.IsOpen && door.Room != SystemTypes.Decontamination && myDrone!.ColliderPosition.Distance(door.transform.position) < distance)
                        {
                            ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)(door.Id | 64));

                            totalAchievement.Value++;
                            new StaticAchievementToken("ubiquitous.common2");
                        }
                    }
                    
                    hackButton.StartCoolDown();
                };
                hackButton.CoolDownTimer = Bind(new Timer(MyRole.doorHackCoolDownOption.GetFloat()).SetAsAbilityCoolDown().Start());
                var pred = hackButton.CoolDownTimer.Predicate;
                hackButton.CoolDownTimer.SetPredicate(()=>pred!.Invoke() || (myDrone && AmongUsUtil.CurrentCamTarget == myDrone));
                hackButton.SetLabel("doorHack");
            }
        }


        protected override void OnInactivated()
        {
            if (AmOwner)
            {
                AmongUsUtil.SetCamTarget();
                if (myDrone) myDrone!.DestroyDroneObject();
            }
        }

        void IGamePlayerEntity.OnDead()
        {
            //死亡時、元の視点に戻す
            if(AmOwner) AmongUsUtil.SetCamTarget();
        }

        bool IsOperatingDrone => myDrone && AmongUsUtil.CurrentCamTarget == myDrone;
        //ドローン視点では壁を無視
        public override bool EyesightIgnoreWalls => IsOperatingDrone;
        public override void EditLightRange(ref float range)
        {
            if (IsOperatingDrone) range *= 1.8f;
        }

    }

    static RemoteProcess<Vector2> RpcSpawnDetachedDrone = new("SpawnDetachedDrone",
        (message,_) => {
            var drone = UnityHelper.CreateObject<UbiquitousDetachedDrone>("DetachedDrone", null, message);
            NebulaGameManager.Instance?.VoiceChatManager?.AddMicrophone(drone);
            NebulaGameManager.Instance?.VoiceChatManager?.AddSpeaker(drone);
        });
    
}