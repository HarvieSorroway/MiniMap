using Menu;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Player;
using System.Reflection;
using HUD;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace MiniMap
{
    public class OtherPatchs
    {
        public static void Patch()
        {
            Hook hook = new Hook(
                typeof(Player).GetProperty("MapInput",BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetGetMethod(),
                typeof(OtherPatchs).GetMethod("Player_get_MapInput_Hook", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                );
            On.RainWorldGame.Update += RainWorldGame_Update;
            On.Menu.PauseMenu.ShutDownProcess += PauseMenu_ShutDownProcess;

            On.Menu.PauseMenu.ctor += PauseMenu_ctor;
        }

        private static void PauseMenu_ctor(On.Menu.PauseMenu.orig_ctor orig, PauseMenu self, ProcessManager manager, RainWorldGame game)
        {
            orig.Invoke(self, manager, game);
            Debug.Log("[MiniMap]pause ctor");
            if(game.pauseMenu != null)
            {
                game.pauseMenu.ShutDownProcess();
                game.pauseMenu = null;
            }
        }

        private static void PauseMenu_ShutDownProcess(On.Menu.PauseMenu.orig_ShutDownProcess orig, PauseMenu self)
        {
            orig.Invoke(self);
            for(int i = self.pages.Count - 1;i >= 0; i--)
            {
                for(int j = self.pages[i].subObjects.Count - 1;i >= 0; i--)
                {
                    self.pages[i].subObjects[j].RemoveSprites();
                }
            }
        }

        private static void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            bool lastPause = self.lastPauseButton;
            bool flag = RWInput.CheckPauseButton(0, self.rainWorld);
            if (((flag && !lastPause)) && (self.cameras[0].hud != null && self.cameras[0].hud.map != null) && !self.paused && self.pauseMenu != null)
            {
                Map map = self.cameras[0].hud.map;
                if (MapPatchs.mapModules.TryGetValue(map, out var module) && map.fade != 0)
                {
                    map.fade = 0f;
                }
            }
            orig.Invoke(self);
        }

        public static InputPackage Player_get_MapInput_Hook(Func<Player,InputPackage> orig,Player self)
        {
            InputPackage origPackage = orig.Invoke(self);
            InputPackage replacePackage = new InputPackage();
            if (Input.GetKey(MiniMapConfig.LeftKey)) replacePackage.x = -1;
            if (Input.GetKey(MiniMapConfig.RightKey)) replacePackage.x = 1;
            if (Input.GetKey(MiniMapConfig.UpKey)) replacePackage.y = 1;
            if (Input.GetKey(MiniMapConfig.DownKey)) replacePackage.y = -1;
            if (Input.GetKey(MiniMapConfig.ThrwKey)) replacePackage.thrw = true;
            if (Input.GetKey(MiniMapConfig.JmpKey)) replacePackage.jmp = true;
            if (Input.GetKey(MiniMapConfig.PckpKey)) replacePackage.pckp = true;
            if (Input.GetKey(MiniMapConfig.MpKey)) replacePackage.mp = true;


            return MiniMapHUD.instance == null ? origPackage : replacePackage;
        }
    }
}
