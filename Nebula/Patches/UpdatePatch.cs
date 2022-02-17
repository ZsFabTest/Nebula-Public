﻿using System;
using System.IO;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Nebula.Objects;

namespace Nebula.Patches
{
    
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class UpdatePatch
    {
        static private Color rewriteImpostorColor(Game.PlayerData player, Color currentColor, Color impostorColor)
        {
            if (player.role.category==Roles.RoleCategory.Impostor)
            {
                return impostorColor;
            }
            else
            {
                if (player.IsMyPlayerData())
                {
                    if (player.role.deceiveImpostorInNameDisplay)
                    {
                        return Palette.ImpostorRed;
                    }
                }

                if (player.role.deceiveImpostorInNameDisplay)
                {
                    return impostorColor;
                }
            }
            return currentColor;
        }

        static void ResetNameTagsAndColors()
        {
            if (PlayerControl.LocalPlayer == null) return;
            if (Game.GameData.data == null) return;

            Color? impostorColor = null;
            if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
            {
                impostorColor = Palette.ImpostorRed;
            }
            else
            {
                impostorColor = Color.white;
            }

            string name;
            Game.PlayerData playerData;
            bool hideFlag;
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (!Game.GameData.data.players.ContainsKey(player.PlayerId))
                {
                    continue;
                }

                playerData = Game.GameData.data.players[player.PlayerId];

                /* 名前を編集する */
                name = "";
                hideFlag = playerData.currentName.Length == 0;

                Helpers.RoleAction(player, (role) => { role.EditDisplayName(player.PlayerId, ref name, hideFlag); });
                
                player.nameText.text = playerData.currentName+" " +name;
                if (player == PlayerControl.LocalPlayer)
                {
                    //自分自身ならロールの色にする
                    player.nameText.color = playerData.role.Color;
                }
                else
                {
                    player.nameText.color = Color.white;
                }
                player.nameText.color = rewriteImpostorColor(playerData, player.nameText.color, (Color)impostorColor);

                //ロールによる色の変更
                Color color = player.nameText.color;
                Helpers.RoleAction(player.PlayerId, (role) => { role.EditDisplayNameColor(player.PlayerId, ref color); });
                player.nameText.color = color;
            }

            if (MeetingHud.Instance != null)
            {
                foreach (PlayerVoteArea player in MeetingHud.Instance.playerStates)
                {
                    if (!Game.GameData.data.players.ContainsKey(player.TargetPlayerId))
                    {
                        continue;
                    }

                    playerData = Game.GameData.data.players[player.TargetPlayerId];

                    /* 名前を編集する */
                    name = "";
                    Helpers.RoleAction(player.TargetPlayerId, (role) => { role.EditDisplayName(player.TargetPlayerId, ref name, false); });
                    if (!name.Equals(""))
                        player.NameText.text = playerData.currentName + " " + name;
                    
                    if (player.TargetPlayerId == PlayerControl.LocalPlayer.PlayerId)
                    {
                        //自分自身ならロールの色にする
                        player.NameText.color = playerData.role.Color;
                    }
                    else
                    {
                        player.NameText.color = Color.white;
                    }

                    player.NameText.color = rewriteImpostorColor(playerData, player.NameText.color, (Color)impostorColor);

                    //色の変更
                    Color color = player.NameText.color;
                    Helpers.RoleAction(player.TargetPlayerId, (role) => { role.EditDisplayNameColor(player.TargetPlayerId, ref color); });
                    player.NameText.color = color;

                }
            }

        }

        private static DeadBody GetDeadBody(byte playerId, DeadBody[] deadBodies)
        {
            foreach (DeadBody player in deadBodies)
            {
                if (player.ParentId == playerId)
                {
                    return player;
                }
            }
            return null; 
        }

        public static void UpdateDraggedPlayer()
        {
            Game.PlayerData data;
            DeadBody[] deadBodies = Helpers.AllDeadBodies();
            DeadBody deadBody;
            float distance;
            Vector3 targetPosition;

            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (!Game.GameData.data.players.ContainsKey(player.PlayerId))
                {
                    continue;
                }
                data = Game.GameData.data.players[player.PlayerId];

                if (data.dragPlayerId==Byte.MaxValue)
                {
                    continue;
                }

                deadBody=GetDeadBody(data.dragPlayerId, deadBodies);

                if ((deadBody == null)||(!data.IsAlive))
                {
                    data.DropPlayer();
                }
                else
                {
                    if (player.inVent) {
                        deadBody.Reported = true;
                        deadBody.bodyRenderer.enabled = false;
                    }
                    else
                    {
                        deadBody.Reported = false;
                        deadBody.bodyRenderer.enabled = true;
                    }
                }

                targetPosition = player.transform.position + new Vector3(-0.1f, -0.1f);
                distance =player.transform.position.Distance(deadBody.transform.position);

                if (distance < 1.8f)
                {
                    deadBody.transform.position+=(targetPosition - deadBody.transform.position)*0.15f;
                }
                else
                {
                    deadBody.transform.position = targetPosition;
                }
            }
        }

        public static void UpdateImpostorKillButton(HudManager __instance)
        {


            if (MeetingHud.Instance != null) return;
            if (Game.GameData.data.myData.getGlobalData().role.side == Roles.Side.Impostor)
            {
                if (Game.GameData.data.myData.getGlobalData().role.HideKillButtonEvenImpostor)
                {
                    __instance.KillButton.Hide();
                }
                else
                {
                    __instance.KillButton.Show();

                    __instance.KillButton.SetTarget(PlayerControlPatch.SetMyTarget(!Roles.Roles.Spy.CanKillImpostor()));
                }
            }
        }

        public static void Postfix(HudManager __instance)
        {
            if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started) return;
            if (!Helpers.HasModData(PlayerControl.LocalPlayer.PlayerId)) return;

            /* サボタージュを確認 */
            if (Helpers.SabotageIsActive())
            {
                EmergencyPatch.SabotageUpdate();
            }

            /* ボタン類の更新 */
            CustomButton.HudUpdate();

            Helpers.RoleAction(PlayerControl.LocalPlayer, (role) => { role.MyUpdate(); });
            if (!PlayerControl.LocalPlayer.Data.Role.IsImpostor && PlayerControl.LocalPlayer.GetModData().role.canUseVents)
            {
                if (Input.GetKeyDown(KeyCode.V))
                HudManagerStartPatch.Manager.ImpostorVentButton.DoClick();
            }
            
            //死後経過時間を更新
            foreach(Game.DeadPlayerData deadPlayer in Game.GameData.data.deadPlayers.Values)
            {
                deadPlayer.Elapsed += Time.deltaTime;
            }

            //名前タグの更新
            ResetNameTagsAndColors();

            //引きずられているプレイヤーの処理
            UpdateDraggedPlayer();

            //マウス角度の調整
            Vector3 mouseDirection = Input.mousePosition - new Vector3(Screen.width / 2, Screen.height / 2);
            Game.GameData.data.myData.getGlobalData().MouseAngle = Mathf.Atan2(mouseDirection.y, mouseDirection.x);


            //インポスターのキルボタンのパッチ
            if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
            {
                UpdateImpostorKillButton(__instance);
            }

            if (PlayerControl.LocalPlayer.GetModData().role.canUseVents)
            {
                //ベントの色の設定
                Color ventColor;
                foreach (Vent vent in ShipStatus.Instance.AllVents)
                {
                    ventColor = PlayerControl.LocalPlayer.GetModData().role.ventColor;
                    vent.myRend.material.SetColor("_OutlineColor", ventColor);

                    if (vent.myRend.material.GetColor("_AddColor").a > 0f)
                        vent.myRend.material.SetColor("_AddColor", ventColor);
                }
            }


            Events.GlobalEvent.Update();
            Events.LocalEvent.Update();

            Objects.Ghost.Update();

            Game.GameData.data.TimerUpdate();

            if (Game.GameData.data.Ghost != null) Game.GameData.data.Ghost.Update();
        }
    }
    
}
