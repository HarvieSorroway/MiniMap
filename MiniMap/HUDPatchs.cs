using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MiniMap
{
    public class HUDPatchs
    {
        public static void Patch()
        {
            On.HUD.HUD.InitSafariHud += HUD_InitSafariHud;
            On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
            On.HUD.HUD.InitMultiplayerHud += HUD_InitMultiplayerHud;
            On.HUD.HUD.ResetMap += HUD_ResetMap;
        }

        private static void HUD_ResetMap(On.HUD.HUD.orig_ResetMap orig, HUD.HUD self, HUD.Map.MapData mapData)
        {
            Plugin.Log("HUD_ResetMap to:" + mapData.regionName);
            if (MiniMapHUD.instance == null)
            {
                Plugin.Log("HUD_ResetMap readd HUD");
                self.AddPart(new MiniMapHUD(self));
            }
            else MiniMapHUD.instance.ReleaseRT();
            orig.Invoke(self, mapData);
            MiniMapHUD.instance.TrySetRT();
        }

        private static void HUD_InitMultiplayerHud(On.HUD.HUD.orig_InitMultiplayerHud orig, HUD.HUD self, ArenaGameSession session)
        {
            orig.Invoke(self, session);
            self.AddPart(new MiniMapHUD(self));
        }

        private static void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
        {
            orig.Invoke(self, cam);
            self.AddPart(new MiniMapHUD(self));
        }

        private static void HUD_InitSafariHud(On.HUD.HUD.orig_InitSafariHud orig, HUD.HUD self, RoomCamera cam)
        {
            orig.Invoke(self, cam);
            self.AddPart(new MiniMapHUD(self));
        }
    }
}
