using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HUD;
using UnityEngine;

namespace MiniMap
{
    public class MiniMapHUD : HudPart
    {
        public static MiniMapHUD instance;
        public static int currentHoveringCorner = 0;
        public static readonly float edgeWidth = 15f;
        public static bool MapVisibility = true;

        FTexture texture;

        public int lastCorner = 0;
        public float drawTimeStacker = 0f;
        public bool lastMapVisibilityButton = false;

        public Vector2 currentIdleHoverPos;
        public Vector2 smoothPos;
        public Vector2 lastPos;

        public float smoothAlpha;
        public float lastAlpha;

        public Vector2 idealHoverPos
        {
            get
            {
                Vector2 middleOfScreen = hud.rainWorld.screenSize / 2f;
                float xBias = middleOfScreen.x - texture.width / 2f - edgeWidth;
                float yBias = middleOfScreen.y - texture.height / 2f - edgeWidth;

                switch (currentHoveringCorner)
                {
                    case 0:
                        xBias = -xBias;
                        yBias = -yBias;
                        break;
                    case 1:
                        xBias = -xBias;
                        break;
                    case 2:
                        break;
                    case 3:
                        yBias = -yBias;
                        break;
                }
                return middleOfScreen + new Vector2(xBias, yBias);
            }
        }

        public MiniMapHUD(HUD.HUD hud) : base(hud)
        {
            currentHoveringCorner = 2;
            lastCorner = 2;
            instance = this;
            Plugin.Log("hud init");
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            if (texture == null) TrySetRT();
            else
            {
                bool visibilityButton = Input.GetKey(MiniMapConfig.HideMapKey);
                if(visibilityButton && !lastMapVisibilityButton)
                {
                    drawTimeStacker = 0f;
                    MapVisibility = !MapVisibility;
                }
                lastMapVisibilityButton = visibilityButton;

                if(lastCorner != currentHoveringCorner)
                {
                    currentIdleHoverPos = idealHoverPos;
                    lastCorner = currentHoveringCorner;
                    drawTimeStacker = 0;
                }
                if(drawTimeStacker < 3f)
                {
                    drawTimeStacker += Time.deltaTime;
                    smoothPos = Vector2.Lerp(lastPos, currentIdleHoverPos, Time.deltaTime * 3f);
                    lastPos = smoothPos;
                    texture.SetPosition(smoothPos);

                    smoothAlpha = Mathf.Lerp(lastAlpha, MapVisibility ? 1f : 0f, Time.deltaTime * 3f);
                    lastAlpha = smoothAlpha;
                    texture.alpha = smoothAlpha;
                }
            }
        }

        public void ReleaseRT()
        {
            Plugin.Log("hud ReleaseRT");
            if (texture != null)
            {
                Plugin.Log("hud RealReleaseRT");
                texture.RemoveFromContainer();
                texture = null;
            }
        }

        public void TrySetRT()
        {
            Plugin.Log("hud TrySetRT");
            if (MapModule.rt != null)
            {
                Plugin.Log("hud SetRT");
                texture = new FTexture(MapModule.rt, "MiniMapTexure");
                hud.fContainers[0].AddChild(texture);

                currentIdleHoverPos = idealHoverPos;
                texture.SetPosition(currentIdleHoverPos);
                smoothPos = currentIdleHoverPos;
                lastPos = currentIdleHoverPos;
            }
        }

        public override void ClearSprites()
        {
            Plugin.Log("hud clearsprites");
            if(texture != null)texture.RemoveFromContainer();
            base.ClearSprites();
        }
    }
}
