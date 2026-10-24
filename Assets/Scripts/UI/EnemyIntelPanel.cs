using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MazeRunner.Enemies;

namespace MazeRunner.UI
{
    /// <summary>
    /// Enemy Intel: what every enemy type does, opened from the pause menu (never over live combat).
    ///
    /// A roster of portraits along the top picks the enemy being inspected; below it the chosen
    /// enemy's portrait and health, its role, its signature ability (attack-style icon and tag, a
    /// short description, the move's real timings and damage) and how to counter it. When the panel
    /// opens it selects the living enemy nearest to the player, marked NEAREST, and says how many of
    /// that type are in the level. Everything shown comes from <see cref="EnemyIntel"/>.
    ///
    /// Drawn like the Settings menu, in the portfolio's three colours only: midnight navy surfaces,
    /// warm mist text and hairlines for selected details and emphasis,
    /// the chosen portrait and the counter-play label. The card is laid out for the game's 1080 px
    /// high canvas and shrinks to fit smaller screens. Portraits are captured from the enemy prefabs
    /// into Resources/EnemyPortraits by Maze Runner > Capture Enemy Portraits.
    /// </summary>
    public class EnemyIntelPanel : MonoBehaviour
    {
        private const float CardWidth = 1600f;
        private const float CardHeight = 980f;
        private const float Margin = 64f;
        private const float RosterY = 184f;
        private const float TileHeight = 120f;
        private const float TileGap = 12f;
        private const float DetailY = 340f;
        private const float PortraitWidth = 420f;
        private const float PortraitHeight = 506f;
        private const float ColumnX = Margin + PortraitWidth + 56f;
        private const float ColumnWidth = CardWidth - Margin - ColumnX;
        private const string PortraitFolder = "EnemyPortraits/";

        public static EnemyIntelPanel Current { get; private set; }
        public static bool IsOpen => Current != null && Current.gameObject.activeSelf;

        private GameObject returnPanel;
        private RectTransform card;
        private TMP_FontAsset bodyFont;
        private TMP_FontAsset headingFont;

        private readonly List<IntelTile> tiles = new List<IntelTile>();
        private int selected;
        private EnemyIntelEntry nearest;
        private readonly Dictionary<EnemyIntelEntry, int> inLevel = new Dictionary<EnemyIntelEntry, int>();

        private Image portrait;
        private RectTransform healthPips;
        private TMP_Text nameText;
        private TMP_Text roleText;
        private TMP_Text presenceText;
        private Image styleIcon;
        private TMP_Text abilityText;
        private RectTransform styleTag;
        private TMP_Text styleTagText;
        private TMP_Text descriptionText;
        private readonly List<TMP_Text> statValues = new List<TMP_Text>();
        private readonly List<TMP_Text> statLabels = new List<TMP_Text>();
        private TMP_Text counterText;
        private TMP_Text pageText;
        private TMP_Text summaryText;

        // ------------------------------------------------------------------ set-up

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded += (scene, mode) => BuildForScene();
            BuildForScene();
        }

        private static void BuildForScene()
        {
            RectTransform pausePanel = null;
            foreach (RectTransform rect in FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (rect.name == "PauseMenuPanel") { pausePanel = rect; break; }
            }
            if (pausePanel == null || pausePanel.parent == null) return;

            Button template = pausePanel.GetComponentInChildren<Button>(true);
            if (template == null) return;
            TMP_Text templateText = template.GetComponentInChildren<TMP_Text>(true);

            var root = new GameObject("EnemyIntelPanel", typeof(RectTransform), typeof(Image));
            var rootRect = (RectTransform)root.transform;
            rootRect.SetParent(pausePanel.parent, false);
            rootRect.SetAsLastSibling();
            PaletteUI.Stretch(rootRect);
            Color navy = PaletteUI.Navy;
            root.GetComponent<Image>().color = new Color(navy.r, navy.g, navy.b, 0.94f); // the paused game, dimmed

            var panel = root.AddComponent<EnemyIntelPanel>();
            panel.returnPanel = pausePanel.gameObject;
            panel.Build(templateText != null ? templateText.font : null);
            root.SetActive(false);
            Current = panel;

            AddPauseMenuButton(pausePanel, template, panel);
        }

        private static void AddPauseMenuButton(RectTransform pausePanel, Button template, EnemyIntelPanel panel)
        {
            Button button = Instantiate(template, pausePanel, false);
            button.name = "EnemyIntelButton";
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(panel.Open);
            foreach (TMP_Text text in button.GetComponentsInChildren<TMP_Text>(true)) text.text = "Enemy Intel";
            // Resume, Restart, Enemy Intel, Settings, Main Menu.
            button.transform.SetSiblingIndex(Mathf.Max(0, pausePanel.childCount - 2));
            button.gameObject.AddComponent<KeepBeforeSettings>();
        }

        /// <summary>
        /// Keeps the Enemy Intel button just above Settings in the pause menu, whichever of the two
        /// was added to the menu first when the scene loaded.
        /// </summary>
        private class KeepBeforeSettings : MonoBehaviour
        {
            private void OnEnable()
            {
                Transform settings = transform.parent != null ? transform.parent.Find("SettingsButton") : null;
                if (settings != null && transform.GetSiblingIndex() > settings.GetSiblingIndex())
                {
                    transform.SetSiblingIndex(settings.GetSiblingIndex());
                }
            }
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        // ------------------------------------------------------------- open/close

        public void Open()
        {
            ScanLevel();
            int start = nearest != null ? System.Array.IndexOf(EnemyIntel.All, nearest) : selected;
            if (returnPanel != null) returnPanel.SetActive(false);
            gameObject.SetActive(true);
            FitCard();
            Select(Mathf.Max(0, start));
            Focus(tiles[selected]);
        }

        public void Close()
        {
            gameObject.SetActive(false);
            if (returnPanel != null)
            {
                returnPanel.SetActive(true);
                Focus(returnPanel.GetComponentInChildren<Button>());
            }
        }

        private static void Focus(Selectable selectable)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(selectable != null ? selectable.gameObject : null);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            // Escape goes back (PauseMenuController leaves Escape to this panel while it is open).
            if (keyboard.escapeKey.wasPressedThisFrame) { Close(); return; }
            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) Step(-1);
            else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) Step(1);
        }

        private void OnRectTransformDimensionsChange() => FitCard();

        /// <summary>
        /// Shrinks the card (never enlarges it) so it fits inside the screen, keeping clear of the web
        /// page's pause and fullscreen buttons in the top-right corner of the game.
        /// </summary>
        private void FitCard()
        {
            if (card == null) return;
            Rect area = ((RectTransform)transform).rect;
            if (area.width <= 0f || area.height <= 0f) return;
            float scale = Mathf.Min(1f, (area.width - 2f * 140f) / CardWidth, (area.height - 2f * 60f) / CardHeight);
            card.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>Counts the living enemies of each type and finds the one nearest to the player.</summary>
        private void ScanLevel()
        {
            inLevel.Clear();
            nearest = null;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            float best = float.MaxValue;
            foreach (EnemyBehaviour behaviour in FindObjectsByType<EnemyBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                EnemyIntelEntry entry = EnemyIntel.For(behaviour);
                var body = behaviour.GetComponent<GoblinEnemy>();
                if (entry == null || body == null || body.IsDead) continue;
                inLevel[entry] = inLevel.TryGetValue(entry, out int n) ? n + 1 : 1;
                if (player == null) continue;
                float distance = Vector2.Distance(player.transform.position, behaviour.transform.position);
                if (distance < best) { best = distance; nearest = entry; }
            }
            int types = inLevel.Count;
            summaryText.text = types == 0
                ? "NO ENEMIES LEFT IN THIS LEVEL"
                : types + (types == 1 ? " TYPE" : " TYPES") + " IN THIS LEVEL";
        }

        private void Step(int delta)
        {
            int count = EnemyIntel.All.Length;
            Select((selected + delta + count) % count);
            Focus(tiles[selected]);
        }

        // ---------------------------------------------------------------- content

        private void Build(TMP_FontAsset font)
        {
            bodyFont = font;
            headingFont = PaletteUI.DisplayFont;
            if (headingFont == null) headingFont = font;
            Color mist = PaletteUI.Mist, green = PaletteUI.Green;

            // Card: a thin mist frame around a navy surface (as in Settings).
            card = PaletteUI.NewRect("Card", transform);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(CardWidth, CardHeight);
            PaletteUI.Fill(card, mist);
            var surface = PaletteUI.NewRect("Surface", card);
            PaletteUI.Stretch(surface, 1.5f);
            PaletteUI.Fill(surface, PaletteUI.Navy, raycast: true);
            Transform c = surface;
            float contentWidth = CardWidth - 2f * Margin;

            // Header.
            PaletteUI.Eyebrow(c, font, "MAZE RUNNER", Margin, 40f, 400f, mist);
            PaletteUI.Text(c, headingFont, "Enemy Intel", 64f, PaletteUI.Cream, Margin, 56f, 900f, 92f, TextAlignmentOptions.Left);
            PaletteUI.Box(c, "Accent", Margin, 150f, 48f, 3f, green);
            summaryText = PaletteUI.Eyebrow(c, font, "", CardWidth - Margin - 700f, 118f, 700f, mist);
            summaryText.alignment = TextAlignmentOptions.TopRight;

            // Roster.
            int count = EnemyIntel.All.Length;
            float tileWidth = (contentWidth - TileGap * (count - 1)) / count;
            for (int i = 0; i < count; i++)
            {
                tiles.Add(MakeTile(c, i, Margin + i * (tileWidth + TileGap), RosterY, tileWidth));
            }

            BuildDetail(c);

            // Footer.
            float footerY = CardHeight - 44f - 56f;
            PaletteUI.Button(c, font, "Previous", PaletteButton.Style.Quiet, Margin, footerY, 200f, 56f, () => Step(-1));
            PaletteUI.Button(c, font, "Next", PaletteButton.Style.Quiet, Margin + 216f, footerY, 200f, 56f, () => Step(1));
            pageText = PaletteUI.Text(c, font, "", 22f, mist, CardWidth / 2f - 150f, footerY, 300f, 56f,
                TextAlignmentOptions.Center, FontStyles.Bold, 6f);
            PaletteUI.Button(c, font, "Back", PaletteButton.Style.Strong, CardWidth - Margin - 240f, footerY, 240f, 56f, Close);
        }

        private IntelTile MakeTile(Transform parent, int index, float x, float y, float width)
        {
            EnemyIntelEntry entry = EnemyIntel.All[index];
            RectTransform rect = PaletteUI.Place(parent, "Tile_" + entry.Id, x, y, width, TileHeight);
            Image frame = PaletteUI.Fill(rect, PaletteUI.Mist, raycast: true);
            var surface = PaletteUI.NewRect("Surface", rect);
            PaletteUI.Stretch(surface, 1.5f);
            PaletteUI.Fill(surface, PaletteUI.Navy);

            var art = PaletteUI.NewRect("Portrait", surface);
            PaletteUI.Stretch(art);
            art.offsetMin = new Vector2(8f, 6f);
            art.offsetMax = new Vector2(-8f, -26f);
            SetPortrait(art.gameObject.AddComponent<Image>(), entry);

            TMP_Text number = PaletteUI.Text(surface, bodyFont, (index + 1).ToString("00"), 17f, PaletteUI.Mist,
                10f, 6f, 60f, 20f, TextAlignmentOptions.TopLeft, FontStyles.Bold, 4f);

            Image underline = PaletteUI.Box(parent, "Underline", x, y + TileHeight + 8f, width, 4f, PaletteUI.Green);

            var tile = rect.gameObject.AddComponent<IntelTile>();
            tile.Init(frame, surface, underline, number);
            int captured = index;
            tile.onClick.AddListener(() => Select(captured));
            return tile;
        }

        private void BuildDetail(Transform c)
        {
            Color mist = PaletteUI.Mist, green = PaletteUI.Green;
            float y = DetailY;

            // Portrait and health.
            RectTransform frame = PaletteUI.Framed(c, "PortraitFrame", Margin, y, PortraitWidth, PortraitHeight, mist, 1.5f, out _);
            var art = PaletteUI.NewRect("Portrait", frame);
            PaletteUI.Stretch(art);
            art.offsetMin = new Vector2(32f, 92f);
            art.offsetMax = new Vector2(-32f, -32f);
            portrait = art.gameObject.AddComponent<Image>();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            PaletteUI.Box(frame, "Rule", 32f, PortraitHeight - 76f, PortraitWidth - 64f, 1.5f, mist);
            PaletteUI.Eyebrow(frame, bodyFont, "HEALTH", 32f, PortraitHeight - 52f, 140f, mist);
            healthPips = PaletteUI.Place(frame, "Health", 150f, PortraitHeight - 52f, PortraitWidth - 182f, 22f);

            // Identity.
            float x = ColumnX, w = ColumnWidth;
            nameText = PaletteUI.Text(c, headingFont, "", 52f, PaletteUI.Cream, x, y - 6f, w, 80f, TextAlignmentOptions.Left);
            roleText = PaletteUI.Eyebrow(c, bodyFont, "", x, y + 82f, w * 0.6f, mist, 18f);
            presenceText = PaletteUI.Eyebrow(c, bodyFont, "", x + w * 0.4f, y + 82f, w * 0.6f, mist, 18f);
            presenceText.alignment = TextAlignmentOptions.TopRight;
            presenceText.richText = true;
            PaletteUI.Box(c, "Rule", x, y + 116f, w, 1.5f, mist);

            // Signature ability.
            PaletteUI.Eyebrow(c, bodyFont, "SIGNATURE ABILITY", x, y + 136f, w, mist);
            RectTransform iconBox = PaletteUI.Framed(c, "StyleIcon", x, y + 168f, 60f, 60f, green, 1.5f, out _);
            var iconRect = PaletteUI.NewRect("Icon", iconBox);
            PaletteUI.Stretch(iconRect, 8f);
            styleIcon = PaletteUI.Fill(iconRect, green);
            abilityText = PaletteUI.Text(c, bodyFont, "", 40f, mist, x + 84f, y + 168f, w - 300f, 60f,
                TextAlignmentOptions.Left, FontStyles.Bold);
            styleTag = PaletteUI.Place(c, "StyleTag", x + 84f, y + 182f, 120f, 32f);
            PaletteUI.Fill(styleTag, green);
            var tagLabel = PaletteUI.NewRect("Label", styleTag);
            PaletteUI.Stretch(tagLabel);
            styleTagText = tagLabel.gameObject.AddComponent<TextMeshProUGUI>();
            if (bodyFont != null) styleTagText.font = bodyFont;
            styleTagText.fontSize = 15f;
            styleTagText.fontStyle = FontStyles.Bold;
            styleTagText.characterSpacing = 12f;
            styleTagText.color = mist;
            styleTagText.alignment = TextAlignmentOptions.Center;
            styleTagText.raycastTarget = false;

            descriptionText = PaletteUI.Text(c, bodyFont, "", 26f, mist, x, y + 246f, w, 104f,
                TextAlignmentOptions.TopLeft, FontStyles.Normal, 0f, wrap: true);
            descriptionText.lineSpacing = 8f;

            // Figures: up to four columns divided by hairlines.
            const int columns = 4;
            float columnWidth = w / columns;
            for (int i = 0; i < columns; i++)
            {
                float cx = x + i * columnWidth + (i == 0 ? 0f : 28f);
                if (i > 0) PaletteUI.Box(c, "Divider", x + i * columnWidth, y + 352f, 1.5f, 70f, mist);
                statValues.Add(PaletteUI.Text(c, bodyFont, "", 34f, mist, cx, y + 350f, columnWidth - 32f, 44f,
                    TextAlignmentOptions.TopLeft, FontStyles.Bold));
                statLabels.Add(PaletteUI.Eyebrow(c, bodyFont, "", cx, y + 398f, columnWidth - 32f, mist, 16f));
            }

            // Counter-play.
            PaletteUI.Box(c, "Rule", x, y + 440f, w, 1.5f, mist);
            PaletteUI.Eyebrow(c, bodyFont, "COUNTER", x, y + 464f, 160f, green);
            counterText = PaletteUI.Text(c, bodyFont, "", 24f, mist, x + 160f, y + 458f, w - 160f, 64f,
                TextAlignmentOptions.TopLeft, FontStyles.Normal, 0f, wrap: true);
        }

        // ---------------------------------------------------------------- selection

        private void Select(int index)
        {
            selected = index;
            for (int i = 0; i < tiles.Count; i++) tiles[i].IsOn = i == index;
            EnemyIntelEntry e = EnemyIntel.All[index];

            SetPortrait(portrait, e);
            SetHealth(e.Health);
            nameText.text = e.Name;
            roleText.text = e.Role;
            inLevel.TryGetValue(e, out int present);
            string presence = present > 0 ? present + " IN THIS LEVEL" : "NOT IN THIS LEVEL";
            presenceText.text = e == nearest ? "<color=#B4C2D6>NEAREST</color>   " + presence : presence;

            // The ability's own artwork (potion, shield) when there is some, else the attack-style line icon.
            Sprite art = EnemyIntelArt.IconFor(e.Id);
            styleIcon.sprite = art != null ? art : IntelIcons.For(e.Style);
            styleIcon.color = art != null ? Color.white : PaletteUI.Green;
            styleIcon.preserveAspect = true;
            abilityText.text = e.Ability;
            styleTagText.text = EnemyIntel.StyleLabel(e.Style);
            // The tag sits just after the ability name.
            float nameWidth = Mathf.Min(abilityText.GetPreferredValues(e.Ability).x, ((RectTransform)abilityText.transform).sizeDelta.x);
            float tagWidth = styleTagText.GetPreferredValues(styleTagText.text).x + 36f;
            styleTag.sizeDelta = new Vector2(tagWidth, styleTag.sizeDelta.y);
            styleTag.anchoredPosition = new Vector2(ColumnX + 84f + nameWidth + 24f, styleTag.anchoredPosition.y);

            descriptionText.text = e.Description;
            for (int i = 0; i < statValues.Count; i++)
            {
                bool has = i < e.Stats.Length;
                statValues[i].text = has ? e.Stats[i].Value : "";
                statValues[i].color = has && e.Stats[i].Severe ? PaletteUI.Green : PaletteUI.Mist;
                statLabels[i].text = has ? e.Stats[i].Label : "";
            }
            counterText.text = e.Counter;
            pageText.text = (index + 1).ToString("00") + "  /  " + EnemyIntel.All.Length.ToString("00");
        }

        private static void SetPortrait(Image image, EnemyIntelEntry entry)
        {
            Sprite art = Resources.Load<Sprite>(PortraitFolder + entry.Id);
            image.preserveAspect = true;
            image.raycastTarget = false;
            if (art != null)
            {
                image.sprite = art;
                image.color = Color.white; // the enemy's own artwork
            }
            else
            {
                // No captured portrait: the attack-style icon in mist keeps the layout intact.
                image.sprite = IntelIcons.For(entry.Style);
                image.color = PaletteUI.Mist;
            }
        }

        /// <summary>One mist square per point of health (the player's stomp deals 1).</summary>
        private void SetHealth(int health)
        {
            for (int i = healthPips.childCount - 1; i >= 0; i--) Destroy(healthPips.GetChild(i).gameObject);
            const float pip = 18f, gap = 8f;
            for (int i = 0; i < health; i++)
            {
                PaletteUI.Box(healthPips, "Pip", i * (pip + gap), 2f, pip, pip, PaletteUI.Mist);
            }
            PaletteUI.Text(healthPips, bodyFont, health.ToString(), 18f, PaletteUI.Mist, health * (pip + gap) + 6f, 0f, 40f, 22f,
                TextAlignmentOptions.TopLeft, FontStyles.Bold);
        }
    }
}
