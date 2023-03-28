using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Menu.Remix;
using Menu.Remix.MixedUI;

namespace MiniMap
{
    public class MiniMapConfig : OptionInterface
    {
        public static Vector2 miniMapSize => new Vector2(MiniMapSize_X.Value, MiniMapSize_Y.Value);
        public static float minScale => MinScale.Value;

        public static KeyCode LeftKey => left.Value;
        public static KeyCode RightKey => right.Value;
        public static KeyCode UpKey => up.Value;
        public static KeyCode DownKey => down.Value;
        public static KeyCode ThrwKey => thrown.Value;
        public static KeyCode JmpKey => jump.Value;
        public static KeyCode PckpKey => pickup.Value;
        public static KeyCode MpKey => map.Value;
        public static KeyCode HideMapKey => hideMap.Value;

        public static Configurable<float> MiniMapSize_X;
        public static Configurable<float> MiniMapSize_Y;
        public static Configurable<float> MinScale;

        public static Configurable<KeyCode> left;
        public static Configurable<KeyCode> right;
        public static Configurable<KeyCode> down;
        public static Configurable<KeyCode> up;
        public static Configurable<KeyCode> thrown;
        public static Configurable<KeyCode> jump;
        public static Configurable<KeyCode> pickup;
        public static Configurable<KeyCode> map;

        public static Configurable<KeyCode> hideMap;

        UIelement[] minimapUIElemnts;

        OpLabel dynamicLabel;

        OpKeyBinder binderLeft;
        OpKeyBinder binderRight;
        OpKeyBinder binderUp;
        OpKeyBinder binderDown;
        OpKeyBinder binderThrown;
        OpKeyBinder binderJump;
        OpKeyBinder binderPickUp;
        OpKeyBinder binderMap;

        OpKeyBinder binderHideMap;

        public MiniMapConfig()
        {
            MiniMapSize_X = config.Bind<float>("MiniMap_MiniMapSize_X", 300f);
            MiniMapSize_Y = config.Bind<float>("MiniMap_MiniMapSize_Y", 185f);
            MinScale = config.Bind<float>("MiniMap_MinScale", 100f);

            left = config.Bind<KeyCode>("MiniMap_left", KeyCode.Keypad4);
            right = config.Bind<KeyCode>("MiniMap_right", KeyCode.Keypad6);
            down = config.Bind<KeyCode>("MiniMap_down", KeyCode.Keypad5);
            up = config.Bind<KeyCode>("MiniMap_up", KeyCode.Keypad8);
            jump = config.Bind<KeyCode>("MiniMap_jump", KeyCode.Keypad9);
            thrown = config.Bind<KeyCode>("MiniMap_thrown", KeyCode.Keypad7);
            pickup = config.Bind<KeyCode>("MiniMap_pickup", KeyCode.Keypad1);
            map = config.Bind<KeyCode>("MiniMap_map", KeyCode.Keypad2);
            hideMap = config.Bind<KeyCode>("MiniMap_hideMap", KeyCode.Keypad3);
        }

        public override void Initialize()
        {
            base.Initialize();

            OpTab opTab = new OpTab(this, "Options");
            Tabs = new OpTab[1]
            {
                opTab
            };
            float biasY = 30f;
            float biasY2 = 40f;
            float gap = 10f;
            minimapUIElemnts = new UIelement[]
            {

                new OpLabel(30f,550f + 20f,"MiniMap Options",true),
                new OpFloatSlider(MiniMapSize_X,new Vector2(30f,550f - biasY),200){
                    min = 100f,
                    max = 600f
                },
                new OpLabel(250f,550f - biasY,"X  Mini Map Size", false)
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                },
                new OpFloatSlider(MiniMapSize_Y,new Vector2(30f,550f - 30f - biasY),200){
                    min = 100f,
                    max = 600f
                },
                new OpLabel(250f,550f - 30f - biasY,"Y", false)
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                },

                new OpFloatSlider(MinScale,new Vector2(30f,550f - 60f - biasY), 200)
                {
                    min = 50f,
                    max = 150f
                },
                new OpLabel(250f,550f - 60f - biasY,"Min scale of map camera\n(bigger means map looks smaller, multiply 3 is max scale)", false)
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                },
                dynamicLabel = new OpLabel(220f,550f - 110f - biasY,"Key Options",true)
                {
                    alignment = FLabelAlignment.Center
                },

                binderDown = new OpKeyBinder(down,new Vector2(230f,550f - 190f - biasY - biasY2),new Vector2(100f,30f)),
                binderUp =  new OpKeyBinder(up,new Vector2(230f,550f - 160f + gap - biasY - biasY2),new Vector2(100f,30f)),
                binderLeft = new OpKeyBinder(left,new Vector2(230f - 100f - gap,550f - 190f - biasY - biasY2),new Vector2(100f,30f)),
                binderRight = new OpKeyBinder(right,new Vector2(230f + 100f + gap,550f - 190f - biasY - biasY2),new Vector2(100f,30f)),

                binderThrown = new OpKeyBinder(thrown,new Vector2(230f - 100f - gap * 2f,550f - 160f + gap * 2f - biasY - biasY2),new Vector2(100f,30f)),
                binderJump = new OpKeyBinder(jump,new Vector2(230f + 100f + gap * 2f,550f - 160f + gap * 2f - biasY - biasY2),new Vector2(100f,30f)),

                binderPickUp = new OpKeyBinder(pickup,new Vector2(230f - 100f - gap * 2f,550f - 210f - gap * 3f - biasY - biasY2),new Vector2(100f,30f)),
                binderMap = new OpKeyBinder(map,new Vector2(230f,550f - 210f - gap * 3f - biasY - biasY2),new Vector2(100f,30f)),
                binderHideMap = new OpKeyBinder(hideMap,new Vector2(230f + 100f + gap * 2f,550f - 210f - gap * 3f - biasY - biasY2),new Vector2(100f,30f)),
            };
            Tabs[0].AddItems(minimapUIElemnts);
        }

        public override void Update()
        {
            base.Update();
            if (binderDown.MouseOver) dynamicLabel.text = "key to scroll map down";
            else if (binderUp.MouseOver) dynamicLabel.text = "key to scroll map up";
            else if (binderLeft.MouseOver) dynamicLabel.text = "key to scroll map left";
            else if (binderRight.MouseOver) dynamicLabel.text = "key to scroll map right";
            else if (binderThrown.MouseOver) dynamicLabel.text = "key to decrease map layer";
            else if (binderJump.MouseOver) dynamicLabel.text = "key to increase map layer";
            else if (binderPickUp.MouseOver) dynamicLabel.text = "key to alternate map scale";
            else if (binderMap.MouseOver) dynamicLabel.text = "key to alternate map hover postion";
            else if (binderHideMap.MouseOver) dynamicLabel.text = "key to toggle map visibility";
            else dynamicLabel.text = "Key Options";
        }
    }
}
