using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 예고 4겹 중 **③ 화살표 · ④ 이름표**.
    ///
    /// ── 왜 필요한가 ─────────────────────────────────────────────
    /// 예고 동안 보스가 하는 일은 노랗게 깜빡이는 것뿐이라 **24개 패턴이 전부
    /// 똑같이 보였다.** 바닥 도형(② 겹)이 붙으면서 "어디가 맞나" 는 읽히게 됐지만,
    /// "무엇인가" 와 "어디로 가라" 는 여전히 안 보인다. 못 읽는 것은 대응할 수 없다.
    ///
    ///   ① body    누가 시작했나   `Unit.SetTellSprite`
    ///   ② floor   어디가 맞나     `DangerView`
    ///   ③ arrow   어디로 가라     여기
    ///   ④ label   무엇인가        여기
    ///
    /// ── 화살표 방향을 어떻게 정하나 ─────────────────────────────
    /// **여기서 정하지 않는다.** 방향은 `BattleDirector` 가 굳어 있는 위험 도형에게
    /// 직접 물어서(`DangerShape.Contains`) 찾는다 — 그리는 도형과 맞는 도형이
    /// 같은 것이므로, 화살표가 가리키는 곳도 **정말로 안 맞는 곳**이 된다.
    /// 여기가 도형을 다시 해석하면 그 보장이 깨진다.
    /// </summary>
    public sealed class DangerHint
    {
        /// <summary>화살표가 플레이어에게서 떨어져 서는 거리(px). 몸에 겹치면 둘 다 안 보인다.</summary>
        private const float ArrowOffset = 62f;

        /// <summary>화살표가 방향을 따라 오가는 폭(px). 움직여야 "가라" 로 읽힌다.</summary>
        private const float ArrowSwing = 14f;

        private static readonly Color ArrowTint = new(1f, 0.92f, 0.35f, 1f);
        private static readonly Color TitleTint = new(1f, 0.86f, 0.55f);
        private static readonly Color DodgeTint = new(0.72f, 0.94f, 1f);

        private readonly RectTransform _root;
        private readonly RectTransform _arrowRt;
        private readonly Image _arrow;
        private readonly RectTransform _labelRt;
        private readonly TextMeshProUGUI _label;

        private Vector2 _arrowAt;
        private Vector2 _arrowDir;
        private float _pulse;

        private DangerHint(RectTransform root, RectTransform arrowRt, Image arrow,
                           RectTransform labelRt, TextMeshProUGUI label)
        {
            _root = root;
            _arrowRt = arrowRt;
            _arrow = arrow;
            _labelRt = labelRt;
            _label = label;
        }

        public static DangerHint Create(Transform parent)
        {
            var go = new GameObject("DangerHint", typeof(RectTransform));
            var root = (RectTransform)go.transform;
            root.SetParent(parent, false);
            // 방 좌표계 그대로 — 왼쪽 위가 (0,0), 아래로 갈수록 y 가 음수다.
            root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = Vector2.zero;

            var arrowGo = new GameObject("Arrow", typeof(RectTransform));
            var arrowRt = (RectTransform)arrowGo.transform;
            arrowRt.SetParent(root, false);
            arrowRt.anchorMin = arrowRt.anchorMax = new Vector2(0f, 1f);
            arrowRt.pivot = new Vector2(0.5f, 0.5f);
            arrowRt.sizeDelta = new Vector2(56f, 56f);
            var arrow = arrowGo.AddComponent<Image>();
            arrow.raycastTarget = false;
            arrow.color = ArrowTint;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.SetParent(root, false);
            labelRt.anchorMin = labelRt.anchorMax = new Vector2(0f, 1f);
            // 보스 **위**에 뜬다. 아래는 위험 도형이 깔려 있어 글자가 묻힌다.
            labelRt.pivot = new Vector2(0.5f, 0f);
            labelRt.sizeDelta = new Vector2(360f, 64f);
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Bottom;
            label.fontSize = 26f;
            label.raycastTarget = false;
            label.lineSpacing = -14f;
            label.font = TMP_Settings.defaultFontAsset;   // 피해 숫자·문 팻말과 같은 폰트

            go.SetActive(false);
            return new DangerHint(root, arrowRt, arrow, labelRt, label);
        }

        /// <summary>
        /// 예고를 띄운다.
        /// </summary>
        /// <param name="arrowFrom">화살표가 설 자리(플레이어). 방 좌표 px.</param>
        /// <param name="arrowDir">가야 할 방향. <c>Vector2.zero</c> 면 화살표를 숨긴다
        /// (「몸을 갈아타라」·「쏘지 마라」는 방향이 아니다).</param>
        /// <param name="labelAt">이름표가 설 자리(보스 머리 위). 방 좌표 px.</param>
        /// <param name="title">패턴 이름. 빈 값이면 이름줄을 뺀다 — 이미 본 패턴이다.</param>
        /// <param name="dodge">회피 한마디. 이건 매번 뜬다.</param>
        public void Show(Vector2 arrowFrom, Vector2 arrowDir, Vector2 labelAt,
                         string title, string dodge, Sprite arrowSprite)
        {
            _root.gameObject.SetActive(true);
            _pulse = 0f;

            bool hasArrow = arrowDir.sqrMagnitude > 0.0001f && arrowSprite != null;
            _arrow.enabled = hasArrow;
            if (hasArrow)
            {
                _arrowDir = arrowDir.normalized;
                _arrowAt = arrowFrom + _arrowDir * ArrowOffset;
                _arrow.sprite = arrowSprite;
                // 그림이 오른쪽(+x)을 보고 있다. 그만큼만 돌린다.
                float deg = Mathf.Atan2(_arrowDir.y, _arrowDir.x) * Mathf.Rad2Deg;
                _arrowRt.localRotation = Quaternion.Euler(0f, 0f, deg);
                _arrowRt.anchoredPosition = _arrowAt;
            }

            // 이름줄과 회피줄을 **한 줄씩** 쌓는다. 이름이 없으면 회피만 남는다.
            _label.text = string.IsNullOrEmpty(title)
                ? Colored(dodge, DodgeTint)
                : Colored(title, TitleTint) + "\n" + Colored(dodge, DodgeTint);
            _labelRt.anchoredPosition = labelAt;
            _label.enabled = !string.IsNullOrEmpty(_label.text);
        }

        /// <summary>
        /// 화살표를 방향을 따라 밀었다 당긴다. 가만히 있으면 표지판이지 지시가 아니다.
        /// 예고가 끝나갈수록 빨라진다 — 남은 시간이 곧 다급함이다.
        /// </summary>
        public void Tick(float dt, float progress)
        {
            if (!_root.gameObject.activeSelf || !_arrow.enabled) return;
            float speed = Mathf.Lerp(3.2f, 8.5f, Mathf.Clamp01(progress));
            _pulse += dt * speed;
            float k = (Mathf.Sin(_pulse) + 1f) * 0.5f;
            _arrowRt.anchoredPosition = _arrowAt + _arrowDir * (k * ArrowSwing);
        }

        public void Hide()
        {
            if (_root != null) _root.gameObject.SetActive(false);
        }

        private static string Colored(string s, Color c)
            => $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{s}</color>";
    }
}
