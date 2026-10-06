using System;
using Cysharp.Threading.Tasks;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Resource;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 최상위 단발성 알림. 06_ui.md 규약대로 **코드로 생성**하며
    /// 독립 Canvas(sortingOrder = constants.md 3절 999)를 써 항상 모든 UI 위에 뜬다.
    ///
    /// 프리팹을 쓰지 않는 것은 의도된 예외다. 씬·패널 로드 상태와 무관하게
    /// 어디서든 즉시 띄울 수 있어야 하기 때문이다.
    ///
    /// ── 그림 (2026-10-06) ──────────────────────────────────────────
    /// 예전에는 색 상자 셋(테두리 · 판 · 버튼)이었다. 지금은 통과한 시안
    /// (`Projects/AVSR/_exchange/in/popup_mock_v2.png`)을 쪼갠 부품을 입는다 —
    /// 틀 · 버튼 받침은 9-slice 로 늘리고, 확인 · 취소 버튼은 낱장이다.
    /// 그림은 아틀라스 `atlas/systempopup`(원본 `Assets/BaseResource/SystemPopup/`)에 있다.
    /// 아틀라스가 아직 안 왔으면 **예전 색 상자로 먼저 뜨고**, 오면 그 자리에서 갈아입는다 —
    /// 알림이 그림을 기다리느라 늦게 뜨면 안 된다.
    /// </summary>
    public sealed class SystemPopup : MonoBehaviour
    {
        private const int SortingOrder = 999;   // constants.md 3절
        private const float RefWidth = 720f;
        private const float RefHeight = 1280f;

        private const string AtlasAddress = "atlas/systempopup";

        // ── 자리(기준 해상도 720 x 1280) ──
        // 값은 시안(popup_mock_v2)을 720 폭에서 잰 것이다(2026-10-06).
        private const float PanelWidth = 655f;
        private const float PanelMinHeight = 310f;
        /// <summary>틀 안쪽 여백 — 틀 윗변 금속 띠가 두꺼워 위는 넉넉히.</summary>
        private const float PadTop = 90f, PadSide = 48f;
        private const float TextGap = 30f;          // 본문과 버튼(또는 홈) 사이
        private const float ButtonCenterY = 77f;    // 틀 아랫변에서 버튼 가운데까지
        /// <summary>버튼 둘은 판 위에 바로, 하나는 얇은 홈 안에 조금 크게 — 시안 그대로.</summary>
        private const float PairWidth = 266f, PairHeight = 74f, PairGap = 22f;
        private const float SingleWidth = 300f, SingleHeight = 78f;
        private const float GrooveHeight = 96f, GrooveInset = 42f;
        /// <summary>짧은 한 줄은 시안처럼 크게, 여러 줄 · 긴 글은 한 단계 작게(시안의 3줄 알림).</summary>
        private const float BodyFontSize = 36f, LongFontSize = 26f;
        /// <summary>뒤 화면 어둡게 — 코덱스 검수 「50~55%」. 어디서 불렸는지 알아볼 만큼만.</summary>
        private const float DimAlpha = 0.55f;

        private static SystemPopup s_instance;
        private static SpriteAtlas s_atlas;
        private static bool s_loading;

        private RectTransform _panel;
        private Image _panelImage, _panelFill, _tray;
        private Image _confirmImage, _cancelImage;
        private TextMeshProUGUI _message;
        private TextMeshProUGUI _confirmLabel;
        private TextMeshProUGUI _cancelLabel;
        private Button _confirm;
        private Button _cancel;
        private Action _onConfirm;
        private bool _artApplied;

        public static bool IsShowing => s_instance != null && s_instance.gameObject.activeSelf;

        /// <summary>그림을 미리 받아 둔다. 첫 알림이 색 상자로 뜨지 않게 — 화면이 처음 설 때 부른다.</summary>
        public static void Preload()
        {
            if (s_atlas != null || s_loading) return;
            LoadArtAsync().Forget();   // fire-and-forget: 받는 동안에도 알림은 색 상자로 뜬다
        }

        public static void Show(string message, Action onConfirm,
                                string confirmText = "확인", string cancelText = "취소")
        {
            // 매개변수 기본값은 상수여야 해서 번역을 못 싣는다 — 기본 글자면 여기서 갈아 끼운다
            if (confirmText == "확인") confirmText = Localize.Get("ui.common.ok");
            if (cancelText == "취소") cancelText = Localize.Get("ui.common.cancel");

            Preload();
            if (s_instance == null) s_instance = Build();
            // 언어가 바뀌었거나 폰트가 내려갔을 수 있다 — 띄울 때마다 다시 건다
            if (CoreModule.TryGet<ILanguageService>(out var lang) && lang.IsReady) lang.ApplyFonts(s_instance.transform);
            else s_instance.ApplyFont(FindFont());
            s_instance.ApplyArt();
            s_instance._onConfirm = onConfirm;
            s_instance._message.text = message;
            s_instance._confirmLabel.text = confirmText;
            s_instance._cancelLabel.text = cancelText;
            // 취소만 있는 알림은 없다 — 확인 전용이면 취소 버튼을 감춘다
            s_instance._cancel.gameObject.SetActive(!string.IsNullOrEmpty(cancelText));
            s_instance.gameObject.SetActive(true);
            s_instance.Layout();
            s_instance.transform.SetAsLastSibling();
        }

        public static void Close()
        {
            if (s_instance == null) return;
            s_instance._onConfirm = null;
            s_instance.gameObject.SetActive(false);
        }

        private void OnConfirmClicked()
        {
            var cb = _onConfirm;
            Close();
            cb?.Invoke();
        }

        // ── 그림 ────────────────────────────────────────────────

        private static async UniTaskVoid LoadArtAsync()
        {
            if (!CoreModule.TryGet<IResourceManager>(out var res)) return;
            s_loading = true;
            try { s_atlas = await res.LoadAsync<SpriteAtlas>(AtlasAddress); }
            catch (Exception e) { Debug.LogWarning($"[SystemPopup] 그림 아틀라스를 못 받았다 — 색 상자로 뜬다: {e.Message}"); }
            finally { s_loading = false; }
            if (s_instance != null)
            {
                s_instance.ApplyArt();
                if (s_instance.gameObject.activeSelf) s_instance.Layout();
            }
        }

        /// <summary>아틀라스가 왔으면 부품을 입힌다. 한 번만.</summary>
        private void ApplyArt()
        {
            if (_artApplied || s_atlas == null) return;
            var frame = s_atlas.GetSprite("popup_frame");
            // 버튼 하나일 때의 얇은 홈. 없으면 홈 없이 뜬다(볼트 달린 옛 받침은 시안과 달라 뺐다)
            var tray = s_atlas.GetSprite("popup_groove");
            var ok = s_atlas.GetSprite("popup_button_confirm");
            var cancel = s_atlas.GetSprite("popup_button_cancel");
            if (frame == null || ok == null || cancel == null) return;   // 하나라도 빠지면 색 상자 그대로

            // 틀 한 장이 테두리와 판을 다 들고 있다 — 색 상자 둘(테두리색 바깥 + 판색 안쪽)은 걷는다
            Sliced(_panelImage, frame);
            _panelFill.enabled = false;
            if (tray != null) Sliced(_tray, tray);
            Simple(_confirmImage, ok);
            Simple(_cancelImage, cancel);
            _confirmLabel.color = new Color(0.16f, 0.10f, 0.02f, 1f);
            _cancelLabel.color = new Color(0.95f, 0.97f, 1f, 1f);
            _artApplied = true;
        }

        private static void Sliced(Image img, Sprite s)
        {
            img.sprite = s;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.fillCenter = true;
        }

        private static void Simple(Image img, Sprite s)
        {
            img.sprite = s;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.color = Color.white;
        }

        // ── 배치 ────────────────────────────────────────────────

        /// <summary>
        /// 글 줄 수에 맞춰 높이를 정하고, 버튼 하나면 가운데 · 둘이면 좌우에 놓는다.
        /// 틀은 9-slice 라 높이가 바뀌어도 모서리 장식은 그대로다.
        /// </summary>
        private void Layout()
        {
            float textWidth = PanelWidth - PadSide * 2f;
            _message.fontSize = BodyFontSize;
            bool isLong = _message.text.IndexOf('\n') >= 0
                          || _message.GetPreferredValues(_message.text, 0f, 0f).x > textWidth;
            if (isLong) _message.fontSize = LongFontSize;
            var pref = _message.GetPreferredValues(_message.text, textWidth, 0f);
            float textHeight = Mathf.Max(_message.fontSize * 1.4f, pref.y);

            bool two = _cancel.gameObject.activeSelf;
            // 본문 아래 끝 = 버튼(하나일 때는 홈) 위 끝
            float bottomZone = ButtonCenterY + (two ? PairHeight : GrooveHeight) * 0.5f;
            float height = Mathf.Max(PanelMinHeight, PadTop + textHeight + TextGap + bottomZone);
            _panel.sizeDelta = new Vector2(PanelWidth, height);

            var mrt = _message.rectTransform;
            mrt.anchorMin = new Vector2(0f, 1f);
            mrt.anchorMax = new Vector2(1f, 1f);
            mrt.pivot = new Vector2(0.5f, 1f);
            // 본문 칸은 틀 위쪽 여백에서 버튼 위까지 — 그 안에서 가운데로 앉는다
            float bodyHeight = height - PadTop - TextGap - bottomZone;
            mrt.anchoredPosition = new Vector2(0f, -PadTop);
            mrt.sizeDelta = new Vector2(-PadSide * 2f, bodyHeight);

            // 홈은 버튼 하나일 때만 — 시안의 버튼 둘은 판 위에 바로 앉는다
            _tray.enabled = !two && _tray.sprite != null;
            var trt = _tray.rectTransform;
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 0f);
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.anchoredPosition = new Vector2(0f, ButtonCenterY);
            trt.sizeDelta = new Vector2(-GrooveInset * 2f, GrooveHeight);

            var confirm = _confirm.GetComponent<RectTransform>();
            if (two)
            {
                float half = (PairWidth + PairGap) * 0.5f;
                PlaceButton(confirm, half, PairWidth, PairHeight);
                PlaceButton(_cancel.GetComponent<RectTransform>(), -half, PairWidth, PairHeight);
            }
            else PlaceButton(confirm, 0f, SingleWidth, SingleHeight);
        }

        private static void PlaceButton(RectTransform rt, float x, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, ButtonCenterY);
            rt.sizeDelta = new Vector2(w, h);
        }

        // ─────────────────────────────────────────────────────────
        private static SystemPopup Build()
        {
            var font = FindFont();

            var root = new GameObject("SystemPopup", typeof(RectTransform), typeof(Canvas),
                                      typeof(CanvasScaler), typeof(GraphicRaycaster));
            UnityEngine.Object.DontDestroyOnLoad(root);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            var popup = root.AddComponent<SystemPopup>();

            // 뒤 화면 차단용 딤 — 클릭도 함께 막는다
            var dim = NewImage("Dim", root.transform, new Color(0f, 0f, 0f, DimAlpha));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            // 그림이 오기 전의 모양(색 상자) — 바깥이 테두리색, 안쪽 Fill 이 판색.
            // 자식 그래픽은 언제나 부모 위에 그려지므로 테두리를 자식으로 두면 판을 덮는다.
            popup._panelImage = NewImage("Panel", root.transform, new Color(0.239f, 0.31f, 0.435f, 1f));
            popup._panel = popup._panelImage.rectTransform;
            Center(popup._panel, PanelWidth, PanelMinHeight);

            popup._panelFill = NewImage("PanelFill", popup._panel, new Color(0.055f, 0.078f, 0.133f, 1f));
            Stretch(popup._panelFill.rectTransform, 3f);

            // 버튼 받침 — 그림이 오기 전에는 숨긴다(색 상자 시절엔 없던 것이다)
            popup._tray = NewImage("ButtonTray", popup._panel, Color.white);
            popup._tray.enabled = false;

            popup._message = NewText("MessageText", popup._panel, font, BodyFontSize,
                                     TextAlignmentOptions.Center);
            popup._message.textWrappingMode = TextWrappingModes.Normal;
            popup._message.fontStyle = FontStyles.Bold;   // 언어 표가 오기 전 대신 — 표가 오면 굵은 폰트로 바뀐다

            (popup._cancel, popup._cancelLabel) =
                NewButton("CancelButton", popup._panel, font,
                          new Color(0.153f, 0.184f, 0.259f, 1f), new Color(0.84f, 0.87f, 0.93f, 1f));
            popup._cancelImage = popup._cancel.GetComponent<Image>();

            (popup._confirm, popup._confirmLabel) =
                NewButton("ConfirmButton", popup._panel, font,
                          new Color(0.847f, 0.565f, 0.094f, 1f), new Color(0.14f, 0.10f, 0.03f, 1f));
            popup._confirmImage = popup._confirm.GetComponent<Image>();

            // 시안: 본문 · 취소는 흰 굵은 글씨에 검은 외곽선, 노란 확인은 외곽선 없이 짙은 굵은 글씨.
            // 굵은 폰트 교체와 외곽선은 언어 모듈이 HeavyText 를 보고 입힌다(언어가 바뀌어도 유지)
            var outline = new Color32(0, 0, 0, 255);
            popup._message.gameObject.AddComponent<HeavyText>().Set(0.25f, 0.28f, outline);
            popup._cancelLabel.gameObject.AddComponent<HeavyText>().Set(0.25f, 0.28f, outline);
            popup._confirmLabel.gameObject.AddComponent<HeavyText>().Set(0.25f, 0f, outline);

            popup._confirm.onClick.AddListener(popup.OnConfirmClicked);
            popup._cancel.onClick.AddListener(Close);

            root.SetActive(false);
            return popup;
        }

        /// <summary>
        /// 본문 폰트. **문자열 표가 들고 있는 지금 언어의 폰트**를 쓴다 — 판 내내 살아 있다.
        ///
        /// ⚠ 예전에는 씬에 떠 있는 아무 글자의 폰트를 빌렸다. 빌드에서는 그 폰트가 **씬 번들과 함께 내려가**
        ///   (로비를 떠나면 ui 번들이 풀린다) 인게임 퍼즈 창의 글자가 하나도 안 보였다(기획 2026-09-15).
        ///   에디터에서는 폰트가 하나뿐이고 안 내려가서 멀쩡해 보였다.
        /// </summary>
        private static TMP_FontAsset FindFont()
        {
            if (CoreModule.TryGet<ILanguageService>(out var lang) && lang.GothicFont != null) return lang.GothicFont;
            return TMP_Settings.defaultFontAsset;
        }

        private void ApplyFont(TMP_FontAsset font)
        {
            if (font == null) return;
            SetFont(_message);
            SetFont(_confirmLabel);
            SetFont(_cancelLabel);

            void SetFont(TextMeshProUGUI t)
            {
                if (t != null && t.font != font) t.font = font;
            }
        }

        private static Image NewImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, TMP_FontAsset font,
                                               float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = size;
            t.alignment = align;
            t.raycastTarget = false;
            t.color = new Color(0.95f, 0.96f, 0.98f, 1f);
            t.lineSpacing = 8f;
            return t;
        }

        private static (Button, TextMeshProUGUI) NewButton(string name, Transform parent,
                                                           TMP_FontAsset font, Color bg, Color fg)
        {
            var img = NewImage(name, parent, bg);
            img.raycastTarget = true;
            var btn = img.gameObject.AddComponent<Button>();
            var label = NewText(name + "Label", img.transform, font, 32f, TextAlignmentOptions.Center);
            label.color = fg;
            label.fontStyle = FontStyles.Bold;
            Stretch(label.rectTransform);
            return (btn, label);
        }

        private static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(-inset * 2f, -inset * 2f);
        }

        private static void Center(RectTransform rt, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w, h);
        }
    }
}
