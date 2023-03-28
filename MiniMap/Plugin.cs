using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using UnityEngine;


#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace MiniMap
{
    [BepInPlugin(id, "MiniMap","1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const string id = "minimap";

        public static bool inited = false;
        public static Shader flatMapShader;

        public static Plugin instance;
        public static MiniMapConfig config;

        public void OnEnable()
        {
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig.Invoke(self);
            if (inited) return;
            try
            {
                config = new MiniMapConfig();
                MapPatchs.Patch();
                HUDPatchs.Patch();
                OtherPatchs.Patch();
                ModFixerPatchs.Patch();

                LoadResources(self);
                inited = true;

                MachineConnector.SetRegisteredOI(id, config);
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void LoadResources(RainWorld rainWorld)
        {
            string path = AssetManager.ResolveFilePath("AssetBundle/lbiobundle");
            AssetBundle ab = AssetBundle.LoadFromFile(path);
            flatMapShader = ab.LoadAsset<Shader>("assets/flatmap.shader");
            rainWorld.Shaders.Add("FlatMap", FShader.CreateShader("FlatMap", flatMapShader));
        }

        public static void Log(string s)
        {
            Debug.Log("[MiniMap]" + s);
        }
    }
}
