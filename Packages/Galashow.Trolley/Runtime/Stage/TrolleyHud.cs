using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Galashow.Trolley
{
    /// <summary>
    /// 트롤리 방송 화면 UI (1920x1080 기준, 코드로 생성)
    /// 상단: 단계·라운드·남은 시간 / 딜레마 제목·설명 / 하단: A·B 선택지 카드, 입력 현황 / 중앙: 결과 공개
    /// </summary>
    public class TrolleyHud
    {
        static readonly Color Panel = new Color(0.07f, 0.07f, 0.11f, 0.82f);
        static readonly Color Accent = new Color(1f, 0.84f, 0.25f);

        public class ChoiceCard
        {
            public RectTransform Root;
            public Image Background;
            public RectTransform BarFill;
            public GameObject Bar;
            public TextMeshProUGUI BarText;
            public TextMeshProUGUI Hint;
        }

        readonly RectTransform _root;

        public TextMeshProUGUI PhaseText { get; private set; }
        public TextMeshProUGUI TimerText { get; private set; }
        public GameObject QuestionPanel { get; private set; }
        public GameObject StatusGroup { get; private set; }
        public TextMeshProUGUI StatusText { get; private set; }
        public TextMeshProUGUI HostText { get; private set; }
        public GameObject RevealPanel { get; private set; }
        public TextMeshProUGUI RevealTitle { get; private set; }
        public TextMeshProUGUI RevealResult { get; private set; }
        public TextMeshProUGUI SurvivorNumber { get; private set; }
        public TextMeshProUGUI EliminatedNumber { get; private set; }
        public TextMeshProUGUI RevealNote { get; private set; }
        public List<ChoiceCard> Cards { get; } = new List<ChoiceCard>();

        // 연출 레이어
        public RectTransform TopBar { get; private set; }
        public RectTransform Question { get; private set; }
        public Image Vignette { get; private set; }
        public RectTransform LetterboxTop { get; private set; }
        public RectTransform LetterboxBottom { get; private set; }
        public TextMeshProUGUI CenterText { get; private set; }
        public TextMeshProUGUI SubCenterText { get; private set; }
        public Image Flash { get; private set; }
        public RectTransform RevealRect { get; private set; }

        public TrolleyHud(Transform parent)
        {
            var canvasGo = new GameObject("TrolleyHud", typeof(RectTransform));
            canvasGo.transform.SetParent(parent, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            _root = (RectTransform)canvasGo.transform;
        }

        /// <summary>
        /// 화면 가운데 아래는 트롤리가 보이도록 비워 둔다.
        /// </summary>
        public void Build(TrolleyGameData data)
        {
            // 상단 바: 단계 | 입력 현황·호스트 상태 | 남은 시간
            var top = Rect("TopBar", _root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -55f), new Vector2(0f, 110f));
            TopBar = top;
            Fill(top, new Color(0f, 0f, 0f, 0.6f));
            PhaseText = Text("Phase", top, "", 42, Accent, TextAlignmentOptions.MidlineLeft);
            Place(PhaseText.rectTransform, new Vector2(0f, 0f), new Vector2(0.36f, 1f), new Vector2(40f, 0f), new Vector2(-40f, 0f));

            var status = Rect("Status", top, new Vector2(0.36f, 0f), new Vector2(0.82f, 1f), Vector2.zero, Vector2.zero);
            StatusText = Text("Votes", status, "", 38, Color.white, TextAlignmentOptions.Center);
            Place(StatusText.rectTransform, new Vector2(0f, 0.42f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            HostText = Text("Host", status, "", 26, new Color(0.8f, 0.8f, 0.85f), TextAlignmentOptions.Center);
            Place(HostText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.45f), Vector2.zero, Vector2.zero);
            StatusGroup = status.gameObject;

            TimerText = Text("Timer", top, "", 72, Color.white, TextAlignmentOptions.MidlineRight);
            Place(TimerText.rectTransform, new Vector2(0.8f, 0f), new Vector2(1f, 1f), new Vector2(-40f, 0f), new Vector2(-40f, 0f));

            // 딜레마
            var question = Rect("Question", _root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -205f), new Vector2(1500f, 170f));
            Fill(question, Panel);
            var title = Text("Title", question, data.Title, 56, Accent, TextAlignmentOptions.Center);
            Place(title.rectTransform, new Vector2(0f, 0.56f), new Vector2(1f, 1f), new Vector2(0f, -6f), new Vector2(-60f, -12f));
            var description = Text("Description", question, data.Description, 32, Color.white, TextAlignmentOptions.Top);
            Place(description.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.58f), new Vector2(0f, -4f), new Vector2(-80f, -12f));
            QuestionPanel = question.gameObject;
            Question = question;

            // 선택지 카드 (왼쪽 A, 오른쪽 B = 선로 방향과 같음)
            for (int i = 0; i < 2; i++)
            {
                Cards.Add(BuildCard(i, data.Choices[i]));
            }

            // 결과 공개 (카드 위, 트롤리 경로 아래쪽)
            var reveal = Rect("Reveal", _root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(1000f, 300f));
            Fill(reveal, Panel);
            RevealTitle = Text("RevealTitle", reveal, "", 56, Accent, TextAlignmentOptions.Center);
            Place(RevealTitle.rectTransform, new Vector2(0f, 0.72f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-40f, 0f));
            RevealResult = Text("RevealResult", reveal, "", 34, Color.white, TextAlignmentOptions.Center);
            Place(RevealResult.rectTransform, new Vector2(0f, 0.56f), new Vector2(1f, 0.74f), Vector2.zero, new Vector2(-40f, 0f));

            var aliveLabel = Text("AliveLabel", reveal, "생존", 34, new Color(0.55f, 1f, 0.6f), TextAlignmentOptions.Center);
            Place(aliveLabel.rectTransform, new Vector2(0.05f, 0.43f), new Vector2(0.48f, 0.56f), Vector2.zero, Vector2.zero);
            SurvivorNumber = Text("AliveNumber", reveal, "0", 110, new Color(0.55f, 1f, 0.6f), TextAlignmentOptions.Center);
            Place(SurvivorNumber.rectTransform, new Vector2(0.05f, 0.12f), new Vector2(0.48f, 0.45f), Vector2.zero, Vector2.zero);
            var outLabel = Text("OutLabel", reveal, "탈락", 34, new Color(1f, 0.45f, 0.42f), TextAlignmentOptions.Center);
            Place(outLabel.rectTransform, new Vector2(0.52f, 0.43f), new Vector2(0.95f, 0.56f), Vector2.zero, Vector2.zero);
            EliminatedNumber = Text("OutNumber", reveal, "0", 110, new Color(1f, 0.45f, 0.42f), TextAlignmentOptions.Center);
            Place(EliminatedNumber.rectTransform, new Vector2(0.52f, 0.12f), new Vector2(0.95f, 0.45f), Vector2.zero, Vector2.zero);

            RevealNote = Text("RevealNote", reveal, "", 26, new Color(0.85f, 0.85f, 0.9f), TextAlignmentOptions.Center);
            Place(RevealNote.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.13f), new Vector2(0f, 4f), new Vector2(-40f, 0f));
            RevealPanel = reveal.gameObject;
            RevealRect = reveal;
            RevealPanel.SetActive(false);

            BuildFxLayers();
        }

        /// <summary>
        /// 비네트·레터박스·중앙 문구·플래시 (위에 그려질수록 나중에 만든다)
        /// </summary>
        void BuildFxLayers()
        {
            Vignette = Fill(Rect("Vignette", _root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), new Color(0.9f, 0.05f, 0.05f, 0f));
            Vignette.sprite = VignetteSprite();

            LetterboxTop = Rect("LetterboxTop", _root, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 0f));
            LetterboxTop.pivot = new Vector2(0.5f, 1f);
            Fill(LetterboxTop, Color.black);
            LetterboxBottom = Rect("LetterboxBottom", _root, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 0f));
            LetterboxBottom.pivot = new Vector2(0.5f, 0f);
            Fill(LetterboxBottom, Color.black);

            CenterText = Text("CenterText", _root, "", 230, Color.white, TextAlignmentOptions.Center);
            Place(CenterText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1800f, 360f));
            CenterText.outlineWidth = 0.18f;
            CenterText.outlineColor = new Color32(0, 0, 0, 220);
            CenterText.gameObject.SetActive(false);

            SubCenterText = Text("SubCenterText", _root, "", 64, Color.white, TextAlignmentOptions.Center);
            Place(SubCenterText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(1600f, 120f));
            SubCenterText.outlineWidth = 0.2f;
            SubCenterText.outlineColor = new Color32(0, 0, 0, 220);
            SubCenterText.gameObject.SetActive(false);

            Flash = Fill(Rect("Flash", _root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), new Color(1f, 1f, 1f, 0f));
        }

        static Sprite VignetteSprite()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var d = new Vector2(x / (size - 1f) - 0.5f, y / (size - 1f) - 0.5f) * 2f;
                float a = Mathf.Clamp01((d.magnitude - 0.55f) / 0.75f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        ChoiceCard BuildCard(int index, TrolleyChoice choice)
        {
            var color = index == 0 ? TrolleyWorld.ColorA : TrolleyWorld.ColorB;
            float anchorX = index == 0 ? 0f : 1f;
            float x = index == 0 ? 350f : -350f;

            var card = Rect($"Card{choice.Id}", _root, new Vector2(anchorX, 0f), new Vector2(anchorX, 0f), new Vector2(x, 130f), new Vector2(620f, 200f));
            var bg = Fill(card, new Color(color.r * 0.55f, color.g * 0.55f, color.b * 0.55f, 0.92f));

            var badge = Rect("Badge", card, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(75f, 15f), new Vector2(110f, 110f));
            Fill(badge, color);
            var letter = Text("Letter", badge, (index + 1).ToString(), 84, Color.white, TextAlignmentOptions.Center);
            Stretch(letter.rectTransform);

            var label = Text("Label", card, choice.Label, 46, Color.white, TextAlignmentOptions.TopLeft);
            Place(label.rectTransform, new Vector2(0f, 0.58f), new Vector2(1f, 1f), new Vector2(75f, -12f), new Vector2(-190f, -12f));
            var desc = Text("Desc", card, choice.Description, 28, new Color(0.92f, 0.92f, 0.95f), TextAlignmentOptions.TopLeft);
            Place(desc.rectTransform, new Vector2(0f, 0.3f), new Vector2(1f, 0.6f), new Vector2(75f, 0f), new Vector2(-190f, 0f));

            var hint = Text("Hint", card, $"채팅: {index + 1}", 26, Accent, TextAlignmentOptions.BottomLeft);
            Place(hint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.3f), new Vector2(75f, 12f), new Vector2(-190f, -12f));

            // 분포 막대 (공개 때만 표시, 안내 문구 자리)
            var bar = Rect("Bar", card, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 32f), new Vector2(-30f, 46f));
            Fill(bar, new Color(0f, 0f, 0f, 0.45f));
            var fill = Rect("Fill", bar, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            fill.pivot = new Vector2(0f, 0.5f);
            Fill(fill, color);
            var barText = Text("BarText", bar, "", 32, Color.white, TextAlignmentOptions.Center);
            Stretch(barText.rectTransform);
            bar.gameObject.SetActive(false);

            return new ChoiceCard { Root = card, Background = bg, Bar = bar.gameObject, BarFill = fill, BarText = barText, Hint = hint };
        }

        #region UI Helpers

        static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        static void Stretch(RectTransform rt) => Place(rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        static Image Fill(RectTransform rt, Color color)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = size * 0.5f;
            tmp.fontSizeMax = size;
            return tmp;
        }

        #endregion
    }
}
