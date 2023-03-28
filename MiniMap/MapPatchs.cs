using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using HUD;
using MonoMod.RuntimeDetour;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using RWCustom;
using Random = UnityEngine.Random;

namespace MiniMap
{
    public class MapPatchs
    {
        public static ConditionalWeakTable<Map, MapModule> mapModules = new ConditionalWeakTable<Map, MapModule>();

        public static List<AbstractRoom> uncoverRooms = new List<AbstractRoom>();
        public static void Patch()
        {
            IL.HUD.Map.Update += new ILContext.Manipulator(Map_Update_IL);

            On.HUD.Map.ctor += Map_ctor;
            On.HUD.Map.Draw += Map_Draw;
            On.HUD.Map.Update += Map_Update;
            On.HUD.Map.ResetReveal += Map_ResetReveal;
            On.HUD.Map.ClearSprites += Map_ClearSprites;
            On.HUD.Map.RoomToMapPos += Map_RoomToMapPos;

            Hook mapContainerHook = new Hook(typeof(HUD.Map).GetProperty("container", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(),
                typeof(MapPatchs).GetMethod("Map_Get_container", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                );
        }

        private static Vector2 Map_RoomToMapPos(On.HUD.Map.orig_RoomToMapPos orig, Map self, Vector2 pos, int room, float timeStacker)
        {
            Vector2 result = orig.Invoke(self, pos, room, timeStacker);
            if(mapModules.TryGetValue(self,out var module))
            {
                Vector2 vector = self.mapData.PositionOfRoom(room) / 3f + new Vector2(10f, 10f);
                Vector2 vector2 = pos - new Vector2((float)self.mapData.SizeOfRoom(room).x * 20f, (float)self.mapData.SizeOfRoom(room).y * 20f) / 2f;
                Vector2 vector3 = vector + vector2 / 20f;
                vector3 -= Vector2.Lerp(self.lastPanPos, self.panPos, timeStacker);
                vector3 *= self.MapScale;
                vector3.x += self.hud.rainWorld.screenSize.x / 2f;
                vector3.y += self.hud.rainWorld.screenSize.y / 2f;

                Vector2 vector4 = new Vector2(Mathf.InverseLerp(0f, self.hud.rainWorld.screenSize.x, vector3.x), Mathf.InverseLerp(0f, self.hud.rainWorld.screenSize.y, vector3.y));
                float num = (float)(2 - self.mapData.LayerOfRoom(room)) / 3f + Mathf.Lerp(self.lastDepth, self.depth, timeStacker) / 2f / 3f;
                num = Mathf.Lerp(3.25f, 4.75f, num) / 4f;
                Vector2 vector5 = vector4;
                vector4 -= new Vector2(0.5f, 0.5f);
                vector4 *= num;
                vector4 += new Vector2(0.5f, 0.5f);
                vector3 += new Vector2((vector4.x - vector5.x) * self.hud.rainWorld.screenSize.x, (vector4.y - vector5.y) * self.hud.rainWorld.screenSize.y);
                result = vector3;
            }
            return result;
        }

        private static void Map_ResetReveal(On.HUD.Map.orig_ResetReveal orig, Map self)
        {
            orig.Invoke(self);
            for(int i = 0; i < self.revealTexture.width; i++)
			{
                for (int j = 0; j < self.revealTexture.height; j++)
                {
                    self.revealTexture.SetPixel(i, j, new Color(self.discoverTexture.GetPixel(i, j).r, 0f, 0f));
                }
            }
            self.revealTexture.Apply();
            Shader.SetGlobalTexture("_mapFogTexture", self.revealTexture);

            for (int k = 0; k < self.mapConnections.Count; k++)
            {
                IntVector2 intVector = IntVector2.FromVector2(self.OnTexturePos(self.mapConnections[k].posInRoomA.ToVector2() * 20f, self.mapConnections[k].roomA, true) / self.DiscoverResolution);
                IntVector2 intVector2 = IntVector2.FromVector2(self.OnTexturePos(self.mapConnections[k].posInRoomB.ToVector2() * 20f, self.mapConnections[k].roomB, true) / self.DiscoverResolution);
                if (self.discoverTexture.GetPixel(intVector.x, intVector.y).r > 0f && self.mapConnections[k].startRevealA < 0)
                {
                    self.mapConnections[k].startRevealA = 1;
                    if (self.mapConnections[k].startRevealB < 0)
                    {
                        self.mapConnections[k].direction = 0f;
                    }
                }
                if (self.discoverTexture.GetPixel(intVector2.x, intVector2.y).r > 0f)
                {
                    if (self.mapConnections[k].startRevealB < 0)
                    {
                        self.mapConnections[k].startRevealB = 1;
                    }
                    if (self.mapConnections[k].startRevealA < 0)
                    {
                        self.mapConnections[k].direction = 1f;
                    }
                }
            }
        }

        private static void Map_Update_IL(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if(c.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<HUD.HudPart>("hud"),
                i => i.Match(OpCodes.Ldfld),
                i => i.Match(OpCodes.Callvirt),
                i => i.Match(OpCodes.Brfalse)
                ))
            {
                c.Index -= 1; // move from jumplabel to brfalse
                c.Emit(OpCodes.Ldarg_0); // get Map instance;
                c.EmitDelegate<Func<bool,Map, bool>>((getReavealMap,map) =>
                {
                    bool result = getReavealMap;
                    if (mapModules.TryGetValue(map, out var module))
                    {
                        try
                        {
                            result |= map.mapLoaded && map.revealTexture != null && map.discoverTexture != null && map.discLoaded && MiniMapHUD.MapVisibility;
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                    return result;


                });
                Debug.Log("[MiniMap]" + c.Index.ToString());
            }
        }

        private static void Map_Update(On.HUD.Map.orig_Update orig, HUD.Map self)
        {
            if (self.slatedForDeletion) return;
            bool tryget = mapModules.TryGetValue(self, out var module);
            bool reloadShader = false;

            if (!self.mapLoaded && tryget) reloadShader = true;

            orig.Invoke(self);

            if (reloadShader && self.mapTexture != null)
            {
                module.ResetShaders(self);
            }

            Player player = self.hud.owner as Player;
            if (tryget)
            {
                if (player != null)
                {
                    module.SetMapPanPos(self, player);
                }
                module.Update(self);
            }

        }

        private static void Map_Draw(On.HUD.Map.orig_Draw orig, HUD.Map self, float timeStacker)
        {
            orig.Invoke(self, timeStacker);
            if(mapModules.TryGetValue(self,out var module))
            {
                module.MapDraw(self, timeStacker);
            }
        }

        private static void Map_ctor(On.HUD.Map.orig_ctor orig, Map self, HUD.HUD hud, Map.MapData mapData)
        {
            if(hud.owner is Player) mapModules.Add(self, new MapModule(self, hud));
            orig.Invoke(self, hud, mapData);
            if(mapModules.TryGetValue(self,out var module))
            {
                self.inFrontContainer.SetPosition(MapModule.bias);
            }
        }

        public static FContainer Map_Get_container(Func<HUD.Map, FContainer> orig, HUD.Map self)
        {
            FContainer result = orig.Invoke(self);
            if(mapModules.TryGetValue(self,out var module))
            {
                result = module.replaceContainer;
            }
            return result;
        }


        private static void Map_ClearSprites(On.HUD.Map.orig_ClearSprites orig, Map self)
        {
            if (mapModules.TryGetValue(self, out var module))
            {
                module.ClearSprites(self);
            }
            orig.Invoke(self);
        }
    }

    public class MapModule
    {
        public static readonly Vector2 bias = new Vector2(10000f, 10000f);
        public static readonly Vector3 miniMapCamBias = new Vector3();
        public static readonly int anyInputMaxCounter = 200;
        public static readonly float maxViewDistance = -700f;

        public static Camera mainCamera;
        public static Camera miniMapCamera;
        public static RenderTexture rt;

        public WeakReference<Map> mapRef;
        public WeakReference<HUD.HUD> hudRef;
        public FContainer replaceContainer;

        public IntVector2 currentPlayerOnTexturePos;
        public IntVector2 lastPlayerOnTexturePos;

        public bool lastPauseButtonPress = false;
        public bool lastMPdown = false;
        public bool lastPCKPdown = false;

        public Vector2 targetPanPos;
        public Vector2 lastPanPos;
        public Vector2 smoothPanPos;
        public Vector2 panVel;

        public float speedUp;

        public float targetScale;
        public float smoothScale;
        public float lastScale;

        public int anyInputCounter = 0;
        public int currentViewDistance = 0;//0 - 2(2 included)

        public Vector3 IdealBias => new Vector3(bias.x, bias.y, 0f/*(int)(-MiniMapConfig.maxDistance * ((currentViewDistance + 1) * 0.33f))*/);

        public MapModule(Map map, HUD.HUD hud)
        {
            CheckAndSetupRenders();

            mapRef = new WeakReference<Map>(map);
            hudRef = new WeakReference<HUD.HUD>(hud);

            replaceContainer = new FContainer();
            Futile.stage.AddChild(replaceContainer);
            replaceContainer.SetPosition(bias);

            lastScale = 100f;
            smoothScale = 100f;
            targetScale = 100f;
        }

        public static void CheckAndSetupRenders()
        {
            if(mainCamera == null)
            {
                mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
            }
            if(miniMapCamera == null)
            {
                var obj = new GameObject("MinimapCamera");
                miniMapCamera = obj.AddComponent<Camera>();
                miniMapCamera.orthographic = true;
                //miniMapCamera.projectionMatrix = mainCamera.projectionMatrix;
            }
            if(rt == null)
            {
                rt = new RenderTexture((int)MiniMapConfig.miniMapSize.x, (int)MiniMapConfig.miniMapSize.y, -10) { filterMode = FilterMode.Point};
                miniMapCamera.targetTexture = rt;
                miniMapCamera.orthographicSize = MiniMapConfig.minScale;
            }

            Vector3 delta = new Vector3(bias.x, bias.y, -500f);
            miniMapCamera.transform.position = mainCamera.transform.position + delta;
        }

        public void MapDraw(Map self, float timeStacker)
        {
            if (self.slatedForDeletion) return;
            if (self.mapSprites == null) return;
            if (self.hud.rainWorld == null) return;
            for (int num9 = 0; num9 < self.mapSprites.Length; num9++)
            {
                self.mapSprites[num9].x = self.hud.rainWorld.screenSize.x / 2f + bias.x;
                self.mapSprites[num9].y = self.hud.rainWorld.screenSize.y / 2f + bias.y;
            }
        }

        public void ResetShaders(Map map)
        {
            if (map.slatedForDeletion) return;
            Plugin.Log("ResetShaders");
            for (int i = 0; i < 3; i++)
            {
                map.mapSprites[i].shader = map.hud.rainWorld.Shaders["FlatMap"];
            }

            map.playerMarkerFade.RemoveFromContainer();
            replaceContainer.AddChild(map.playerMarkerFade);
            map.playerMarker.sprite.RemoveFromContainer();
            replaceContainer.AddChild(map.playerMarker.sprite);
        }

        public void SetMapPanPos(Map map,Player player)
        {
            if (map.slatedForDeletion) return;
            if (player != null && !player.inShortcut && map.discLoaded && map.mapLoaded)
            {
                currentPlayerOnTexturePos = IntVector2.FromVector2(map.OnTexturePos(map.hud.owner.MapOwnerInRoomPosition, map.hud.owner.MapOwnerRoom, true) / map.DiscoverResolution);
                if (lastPlayerOnTexturePos != currentPlayerOnTexturePos)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        map.discoverTexture.SetPixel(currentPlayerOnTexturePos.x + Custom.fourDirectionsAndZero[i].x, currentPlayerOnTexturePos.y + Custom.fourDirectionsAndZero[i].y, new Color(1f, 1f, 1f));
                        map.revealTexture.SetPixel(currentPlayerOnTexturePos.x + Custom.fourDirectionsAndZero[i].x, currentPlayerOnTexturePos.y + Custom.fourDirectionsAndZero[i].y, new Color(1f, 1f, 1f));
                    }
                    for (int j = 0; j < 4; j++)
                    {
                        if (map.discoverTexture.GetPixel(currentPlayerOnTexturePos.x + Custom.diagonals[j].x, currentPlayerOnTexturePos.y + Custom.diagonals[j].y).r < 0.5f)
                        {
                            map.discoverTexture.SetPixel(currentPlayerOnTexturePos.x + Custom.diagonals[j].x, currentPlayerOnTexturePos.y + Custom.diagonals[j].y, new Color(0.5f, 0.5f, 0.5f));
                            map.revealTexture.SetPixel(currentPlayerOnTexturePos.x + Custom.diagonals[j].x, currentPlayerOnTexturePos.y + Custom.diagonals[j].y, new Color(0.5f, 0.5f, 0.5f));
                        }
                    }
                    map.revealTexture.Apply();
                    map.discoverTexture.Apply();
                    Shader.SetGlobalTexture("_mapFogTexture", map.revealTexture);

                    Plugin.Log(currentPlayerOnTexturePos.ToString() + " : " + map.discoverTexture.GetPixel(currentPlayerOnTexturePos.x, currentPlayerOnTexturePos.y).ToString());
                    

                    for (int k = 0; k < map.mapConnections.Count; k++)
                    {
                        IntVector2 intVector = IntVector2.FromVector2(map.OnTexturePos(map.mapConnections[k].posInRoomA.ToVector2() * 20f, map.mapConnections[k].roomA, true) / map.DiscoverResolution);
                        IntVector2 intVector2 = IntVector2.FromVector2(map.OnTexturePos(map.mapConnections[k].posInRoomB.ToVector2() * 20f, map.mapConnections[k].roomB, true) / map.DiscoverResolution);
                        if (map.discoverTexture.GetPixel(intVector.x, intVector.y).r > 0f && map.mapConnections[k].startRevealA < 0)
                        {
                            map.mapConnections[k].startRevealA = 1;
                            if (map.mapConnections[k].startRevealB < 0)
                            {
                                map.mapConnections[k].direction = 0f;
                            }
                        }
                        if (map.discoverTexture.GetPixel(intVector2.x, intVector2.y).r > 0f)
                        {
                            if (map.mapConnections[k].startRevealB < 0)
                            {
                                map.mapConnections[k].startRevealB = 1;
                            }
                            if (map.mapConnections[k].startRevealA < 0)
                            {
                                map.mapConnections[k].direction = 1f;
                            }
                        }
                    }
                }
                lastPlayerOnTexturePos = currentPlayerOnTexturePos;

                if (map.hud.owner.MapInput.AnyDirectionalInput || map.hud.owner.MapInput.AnyInput || map.hud.owner.MapInput.mp)
                {
                    anyInputCounter = anyInputMaxCounter;
                }
                if(anyInputCounter > 0)
                {
                    Vector2 v = map.hud.owner.MapInput.analogueDir;
                    if (v.x == 0f && v.y == 0f && (map.hud.owner.MapInput.x != 0 || map.hud.owner.MapInput.y != 0))
                    {
                        v = Custom.DirVec(new Vector2(0f, 0f), new Vector2((float)map.hud.owner.MapInput.x, (float)map.hud.owner.MapInput.y));
                    }
                    if (v.magnitude > 0.1f)
                    {
                        speedUp = Mathf.Clamp01(speedUp + Custom.LerpMap(Vector2.Dot(panVel.normalized, v.normalized) * v.magnitude, 0.6f, 0.8f, -1f / Custom.LerpMap(speedUp, 0.5f, 1f, 7f, 30f), 0.007142857f));
                        panVel += v * (0.45f + 0.1f * Mathf.InverseLerp(0.5f, 1f, speedUp));
                    }
                    else
                    {
                        speedUp = Mathf.Max(0f, speedUp - 0.033333335f);
                        panVel *= 0.8f;
                    }

                    bool mp = map.hud.owner.MapInput.mp;
                    bool pckp = map.hud.owner.MapInput.pckp;

                    if(mp && !lastMPdown)
                    {
                        MiniMapHUD.currentHoveringCorner++;
                        if (MiniMapHUD.currentHoveringCorner > 3) MiniMapHUD.currentHoveringCorner = 0;
                    }
                    if (pckp && !lastPCKPdown)
                    {
                        currentViewDistance++;
                        if (currentViewDistance > 2) currentViewDistance = 0;
                        targetScale = (1f + (float)currentViewDistance) * MiniMapConfig.minScale;
                    }

                    lastMPdown = mp;
                    lastPCKPdown = pckp;
                    targetPanPos += panVel;
                    anyInputCounter--;
                }
                else
                {
                    Vector2 vector = map.hud.owner.MapOwnerInRoomPosition;
                    int mapOwnerRoom = map.hud.owner.MapOwnerRoom;
                    map.layer = map.mapData.LayerOfRoom(mapOwnerRoom);;
                    Vector2 vector2 = map.mapData.PositionOfRoom(mapOwnerRoom) / 3f;
                    vector -= new Vector2((float)map.mapData.SizeOfRoom(mapOwnerRoom).x * 20f, (float)map.mapData.SizeOfRoom(mapOwnerRoom).y * 20f) / 2f;

                    targetPanPos = vector2 + vector / 20f + new Vector2(10f, 10f);
                }

                smoothPanPos = Vector2.Lerp(lastPanPos, targetPanPos, 0.1f);
                smoothScale = Mathf.Lerp(lastScale, targetScale, 0.1f);

                if(miniMapCamera != null)
                {
                    miniMapCamera.transform.position = mainCamera.transform.position + IdealBias;
                    miniMapCamera.orthographicSize = smoothScale;
                }
                lastPanPos = smoothPanPos;
                lastScale = smoothScale;

                map.panPos = smoothPanPos;
            }
        }

        public void Update(Map map)
        {
            bool flag = RWInput.CheckPauseButton(0, map.hud.rainWorld);
            RainWorldGame game = null;
            Player player = map.hud.owner as Player;
            if(player != null && player.room != null)
            {
                game = player.room.game;
            }

            if (flag && !lastPauseButtonPress && game != null && game.pauseMenu == null)
            {
                game.pauseMenu = new Menu.PauseMenu(game.manager, game);
            }

            lastPauseButtonPress = flag;

            if (MiniMapHUD.instance == null)
            {
                map.hud.AddPart(new MiniMapHUD(map.hud));
            }
        }

        public void ClearSprites(Map map)
        {
            if (MiniMapHUD.instance != null) MiniMapHUD.instance.ReleaseRT();
            rt.Release();
            rt = null;

            MapPatchs.mapModules.Remove(map);
            map.visible = false;
        }
    }
}
