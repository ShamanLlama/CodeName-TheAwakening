using Awakening.Core.Rules;
using Awakening.UI.Map;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awakening.UI
{
    /// <summary>
    /// Phase-1 spike: proves UI Toolkit, Painter2D, the period fonts and Newtonsoft in a web build,
    /// and reports when the first map frame is on screen. Replaced by the real game in phase 5.
    /// </summary>
    public sealed class SpikeApp : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var host = new GameObject("Awakening");
            DontDestroyOnLoad(host);
            host.AddComponent<SpikeApp>();
        }

        bool _reported;
        int _framesSincePaint = -1;

        void Start()
        {
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.027f, 0.031f, 0.043f);
            camera.cullingMask = 0;

            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1600, 900);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 1f;

            var holder = new GameObject("UI");
            holder.SetActive(false);
            var document = holder.AddComponent<UIDocument>();
            document.panelSettings = settings;
            holder.SetActive(true);

            VisualElement root = document.rootVisualElement;
            root.style.flexGrow = 1;
            root.style.backgroundColor = new Color(0.051f, 0.043f, 0.031f);

            var title = new Label("The Awakening of Cthulhu");
            title.style.unityFontDefinition = FontDefinition.FromFont(Resources.Load<Font>("Fonts/IMFellEnglishSC-Regular"));
            title.style.fontSize = 44;
            title.style.color = new Color(0.86f, 0.79f, 0.62f);
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            root.Add(title);

            var subtitle = new Label("Case file · January 1908 · Influence 35 · Dread 20 · Followers 100");
            subtitle.style.unityFontDefinition = FontDefinition.FromFont(Resources.Load<Font>("Fonts/OldStandard-Italic"));
            subtitle.style.fontSize = 22;
            subtitle.style.color = new Color(0.78f, 0.71f, 0.56f);
            subtitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            root.Add(subtitle);

            var map = new WorldMapElement();
            map.style.marginLeft = map.style.marginRight = 24;
            map.Painted = () => { if (_framesSincePaint < 0) _framesSincePaint = 0; };
            root.Add(map);

            var telegram = new Label("TELEGRAM · SEED greedy-c STATE " + Rng.SeedToState("greedy-c") + " · ALL CAPS STOP");
            telegram.style.unityFontDefinition = FontDefinition.FromFont(Resources.Load<Font>("Fonts/CourierPrime-Bold"));
            telegram.style.fontSize = 18;
            telegram.style.color = new Color(0.86f, 0.79f, 0.62f);
            telegram.style.unityTextAlign = TextAnchor.MiddleCenter;
            telegram.style.marginBottom = 12;
            root.Add(telegram);
        }

        void Update()
        {
            if (_reported || _framesSincePaint < 0) return;
            // Two presented frames after the first paint, so the chart is really on screen.
            if (++_framesSincePaint < 2) return;
            _reported = true;
            Debug.Log($"AWAKENING_READY realtime={Time.realtimeSinceStartup:F3}");
        }
    }
}
