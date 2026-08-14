using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 최상위 단발성 알림. 06_ui.md 규약대로 **코드로 생성**하며
    /// 독립 Canvas(sortingOrder = constants.md 3절 999)를 써 항상 모든 UI 위에 뜬다.
    ///
    /// 프리팹·아틀라스를 쓰지 않는 것은 의도된 예외다. 씬·패널 로드 상태와 무관하게
    /// 어디서든 즉시 띄울 수 있어야 하기 때문이다.
    /// </summary>
    public sealed class SystemPopup : MonoBehaviour
    {
        private const int SortingOrder = 999;   // constants.md 3절
        private const float RefWidth = 720f;
        private const float RefHeight = 1280f;

        private static SystemPopup s_instance;

        private TextMeshProUGUI _message;
        private TextMeshProUGUI _confirmLabel;
        private TextMeshProUGUI _cancelLabel;
        private Button _confirm;
        private Button _cancel;
        private Action _onConfirm;

        public static bool IsShowing => s_instance != null && s_instance.gameObject.activeSelf;

        public static void Show(string message, Action onConfirm,
                                string confirmText = "확인", string cancelText = "취소")
        {
            if (s_instance == null) s_instance = Build();
            s_instance._onConfirm = onConfirm;
            s_instance._message.text = message;
            s_instance._confirmLabel.text = confirmText;
            s_instance._cancelLabel.text = cancelText;
            // 취소만 있는 알림은 없다 — 확인 전용이면 취소 버튼을 감춘다
            s_instance._cancel.gameObject.SetActive(!string.IsNullOrEmpty(cancelText));
            s_instance.gameObject.SetActive(true);
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
            var dim = NewImage("Dim", root.transform, new Color(0f, 0f, 0f, 0.72f));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            // 자식 그래픽은 언제나 부모 위에 그려진다. 테두리를 자식으로 두면 패널을 덮으므로,
            // 바깥 오브젝트가 테두리색이고 안쪽 Fill 이 본체색인 구조로 만든다.
            var panel = NewImage("Panel", root.transform, new Color(0.239f, 0.31f, 0.435f, 1f));
            Center(panel.rectTransform, 540f, 260f);

            var fill = NewImage("PanelFill", panel.transform, new Color(0.055f, 0.078f, 0.133f, 1f));
            Stretch(fill.rectTransform, 3f);

            popup._message = NewText("MessageText", panel.transform, font, 26f,
                                     TextAlignmentOptions.Center);
            var mrt = popup._message.rectTransform;
            mrt.anchorMin = new Vector2(0f, 1f);
            mrt.anchorMax = new Vector2(1f, 1f);
            mrt.pivot = new Vector2(0.5f, 1f);
            mrt.anchoredPosition = new Vector2(0f, -40f);
            mrt.sizeDelta = new Vector2(-56f, 96f);

            (popup._cancel, popup._cancelLabel) =
                NewButton("CancelButton", panel.transform, font,
                          new Color(0.153f, 0.184f, 0.259f, 1f), new Color(0.84f, 0.87f, 0.93f, 1f));
            Corner(popup._cancel.GetComponent<RectTransform>(), 40f, -180f, 220f, 62f);

            (popup._confirm, popup._confirmLabel) =
                NewButton("ConfirmButton", panel.transform, font,
                          new Color(0.847f, 0.565f, 0.094f, 1f), new Color(0.14f, 0.10f, 0.03f, 1f));
            Corner(popup._confirm.GetComponent<RectTransform>(), 280f, -180f, 220f, 62f);

            popup._confirm.onClick.AddListener(popup.OnConfirmClicked);
            popup._cancel.onClick.AddListener(Close);

            root.SetActive(false);
            return popup;
        }

        /// <summary>씬에 이미 쓰이는 한글 폰트를 재사용한다(전용 에셋 참조를 만들지 않기 위함).</summary>
        private static TMP_FontAsset FindFont()
        {
            var any = UnityEngine.Object.FindAnyObjectByType<TextMeshProUGUI>(FindObjectsInactive.Include);
            if (any != null && any.font != null) return any.font;
            return TMP_Settings.defaultFontAsset;
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
            return t;
        }

        private static (Button, TextMeshProUGUI) NewButton(string name, Transform parent,
                                                           TMP_FontAsset font, Color bg, Color fg)
        {
            var img = NewImage(name, parent, bg);
            img.raycastTarget = true;
            var btn = img.gameObject.AddComponent<Button>();
            var label = NewText(name + "Label", img.transform, font, 24f, TextAlignmentOptions.Center);
            label.color = fg;
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

        /// <summary>부모 좌상단 기준 배치.</summary>
        private static void Corner(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }
    }
}
