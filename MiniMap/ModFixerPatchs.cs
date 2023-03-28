using MonoMod.RuntimeDetour;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;

namespace MiniMap
{
    public class ModFixerPatchs
    {
        public static void Patch()
        {
            Type WarpRegionSwitcher = Type.GetType("RegionSwitcher,Warp",false);
            if(WarpRegionSwitcher != null)
            {
                Hook hook = new Hook(
                    WarpRegionSwitcher.GetMethod("WorldLoaded", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                    typeof(ModFixerPatchs).GetMethod("Warp_RegionSwitcher_WorldLoaded_Hook", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    );
            }
            Plugin.Log(WarpRegionSwitcher.ToString());
        }

        //why u bully me warp menu?
        //修复warp导致fsprite没法正常移除的问题
        public static void Warp_RegionSwitcher_WorldLoaded_Hook(Action<object,RainWorldGame,AbstractRoom,World,string,IntVector2> orig,object self,RainWorldGame game, AbstractRoom oldRoom, World oldWorld, string newRoomName, IntVector2 newPos)
        {
            if(game.cameras[0].hud != null && game.cameras[0].hud.map != null)
            {
                game.cameras[0].hud.map.ClearSprites();
            }
            orig.Invoke(self, game, oldRoom, oldWorld, newRoomName, newPos);
        }
    }
}
