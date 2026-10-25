using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MazeRunner.UI
{
    /// <summary>
    /// Keyboard control of every menu, on top of each scene's EventSystem and its Input System UI Input
    /// Module (no second input path):
    /// - Arrow keys / WASD move the focus (UI/Navigate; sliders take Left/Right), Enter presses the
    ///   focused control (UI/Submit) and so does Space (sent here: the UI module's Submit is Enter only),
    ///   Escape goes back - handled by each menu that has a Back.
    /// - The open menu is the top-most active panel of <see cref="MenuPanels"/>. A menu that opens gets a
    ///   default focus (its own preference, see <see cref="PreferFocus"/>, else its first control); when it
    ///   closes, the menu below gets back the control that had focus. A focus that becomes invalid (a
    ///   button disabled or hidden) moves to the default again.
    /// - The focused control wears a cream frame - the moon colour of the titles - that pulses gently, so
    ///   keyboard focus reads on every button colour.
    /// - With no menu open (gameplay) nothing keeps the focus, so Space and Enter can never press a hidden
    ///   or HUD button; while a menu is open, <see cref="BlocksGameplayInput"/> keeps the player's jump and
    ///   movement out of it.
    /// Mouse and touch keep working as before (they move the same focus).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class MenuNavigator : MonoBehaviour
    {
        // Top-most first: an overlay before the screen it covers.
        private static readonly string[] MenuPanels =
        {
            "SettingsPanel", "EnemyIntelPanel", "GameOverPanel", "LevelCompletePanel", "PauseMenuPanel",
            "LevelSelectPanel", "MainMenuPanel", "CustomizationPanel",
        };

        private static MenuNavigator instance;
        private static readonly Dictionary<GameObject, Selectable[]> Preferred = new Dictionary<GameObject, Selectable[]>();

        private readonly List<GameObject> panels = new List<GameObject>();
        private readonly Dictionary<GameObject, GameObject> lastFocus = new Dictionary<GameObject, GameObject>();
        private bool scanned;
        private bool freshScene = true;
        private GameObject top;
        private RectTransform frame;
        private Image frameImage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (instance != null) return;
            var go = new GameObject(nameof(MenuNavigator));
            DontDestroyOnLoad(go);
            instance = go.AddComponent<MenuNavigator>();
            SceneManager.sceneLoaded += (scene, mode) =>
            {
                if (instance == null) return;
                instance.scanned = false;
                instance.freshScene = true;
                instance.lastFocus.Clear();
                instance.top = null;
            };
        }

        /// <summary>The menu panel the keyboard is in, or null during gameplay.</summary>
        public static GameObject OpenMenu => instance != null ? instance.FindTop() : null;

        /// <summary>True while a menu is open: the player's jump and movement are ignored.</summary>
        public static bool BlocksGameplayInput => OpenMenu != null;

        /// <summary>True while the named panel (e.g. "GameOverPanel") is the open menu.</summary>
        public static bool IsOpen(string panelName)
        {
            GameObject open = OpenMenu;
            return open != null && open.name == panelName;
        }

        /// <summary>
        /// The controls a panel focuses first when it opens, in order of preference; the first one that is
        /// shown and enabled wins.
        /// </summary>
        public static void PreferFocus(GameObject panel, params Selectable[] candidates)
        {
            if (panel != null) Preferred[panel] = candidates;
        }

        private GameObject FindTop()
        {
            if (!scanned) Scan();
            foreach (GameObject panel in panels) if (panel != null && panel.activeInHierarchy) return panel;
            return null;
        }

        private void Scan()
        {
            scanned = true;
            panels.Clear();
            var found = new Dictionary<string, List<GameObject>>();
            foreach (RectTransform rect in FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (System.Array.IndexOf(MenuPanels, rect.name) < 0) continue;
                if (!found.TryGetValue(rect.name, out List<GameObject> list)) found[rect.name] = list = new List<GameObject>();
                list.Add(rect.gameObject);
            }
            foreach (string name in MenuPanels) if (found.TryGetValue(name, out List<GameObject> list)) panels.AddRange(list);
        }

        private void LateUpdate()
        {
            EventSystem events = EventSystem.current;
            // Menus built in code when a scene loads exist by its first frame: scan again then.
            if (freshScene) { freshScene = false; scanned = false; }
            if (events == null) { ShowFrame(null); return; }

            GameObject open = FindTop();
            GameObject selected = events.currentSelectedGameObject;

            if (open != top)
            {
                // A menu opened or closed: give the new top menu its focus back, or its default.
                top = open;
                if (open != null)
                {
                    GameObject restore = lastFocus.TryGetValue(open, out GameObject last) && Valid(last, open) ? last
                        : Valid(selected, open) ? selected : Default(open);
                    events.SetSelectedGameObject(restore);
                    selected = restore;
                }
            }

            if (open == null)
            {
                // Gameplay: nothing keeps the focus, so Space / Enter press nothing.
                if (selected != null && selected.GetComponentInParent<Canvas>() != null) events.SetSelectedGameObject(null);
                ShowFrame(null);
                return;
            }

            if (!Valid(selected, open))
            {
                selected = Default(open);
                events.SetSelectedGameObject(selected);
            }
            if (selected != null) lastFocus[open] = selected;
            ShowFrame(selected);

            // Space presses the focused control as Enter does (the default UI actions submit on Enter only).
            if (selected != null && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                ExecuteEvents.Execute(selected, new BaseEventData(events), ExecuteEvents.submitHandler);
        }

        private static bool Valid(GameObject go, GameObject panel)
        {
            if (go == null || !go.activeInHierarchy || !go.transform.IsChildOf(panel.transform)) return false;
            var selectable = go.GetComponent<Selectable>();
            return selectable != null && selectable.IsInteractable();
        }

        private static GameObject Default(GameObject panel)
        {
            if (Preferred.TryGetValue(panel, out Selectable[] candidates))
                foreach (Selectable s in candidates)
                    if (s != null && Valid(s.gameObject, panel)) return s.gameObject;
            foreach (Selectable s in panel.GetComponentsInChildren<Selectable>(false))
                if (s.IsInteractable() && s.navigation.mode != Navigation.Mode.None) return s.gameObject;
            foreach (Selectable s in panel.GetComponentsInChildren<Selectable>(false))
                if (s.IsInteractable()) return s.gameObject;
            return null;
        }

        // ---------------------------------------------------------------- focus frame

        private void ShowFrame(GameObject target)
        {
            if (target == null || !(target.transform is RectTransform rect))
            {
                if (frame != null) frame.gameObject.SetActive(false);
                return;
            }
            if (frame == null) BuildFrame();
            if (frame.parent != rect)
            {
                frame.SetParent(rect, false);
                frame.anchorMin = Vector2.zero;
                frame.anchorMax = Vector2.one;
                frame.offsetMin = new Vector2(-7f, -7f);
                frame.offsetMax = new Vector2(7f, 7f);
                frame.localScale = Vector3.one;
                frame.localRotation = Quaternion.identity;
            }
            frame.SetAsLastSibling();
            frame.gameObject.SetActive(true);
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 5f);
            Color c = PaletteButton.Cream;
            frameImage.color = new Color(c.r, c.g, c.b, pulse);
        }

        private void BuildFrame()
        {
            var go = new GameObject("KeyboardFocusFrame", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            frame = (RectTransform)go.transform;
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            frameImage = go.GetComponent<Image>();
            frameImage.sprite = FrameSprite();
            frameImage.type = Image.Type.Sliced;
            frameImage.raycastTarget = false;
        }

        /// <summary>A rounded 4 px ring, 9-sliced so it fits any control.</summary>
        private static Sprite FrameSprite()
        {
            const int size = 32, radius = 10;
            const float ring = 4f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "KeyboardFocusFrame", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Distance from the rounded rectangle's outline, measured inward.
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius), cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float outside = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float depth = radius - outside;
                    float edge = Mathf.Min(x + 0.5f, y + 0.5f, size - x - 0.5f, size - y - 0.5f);
                    if (outside <= 0f) depth = edge;
                    float a = Mathf.Clamp01(depth + 0.5f) * Mathf.Clamp01(ring - depth + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(radius + 2, radius + 2, radius + 2, radius + 2));
        }
    }
}
