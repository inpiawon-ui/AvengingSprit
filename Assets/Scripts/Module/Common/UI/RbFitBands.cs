using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// RE:BORN 세로 판(스크롤 내용) 화면 맞춤 — 그린 크기(720 폭 · 시안 높이)를 보기 칸에 맞춰 **통째로 키우고**,
    /// 남는 세로는 묶음(띠) 사이에 **고르게 나눠** 벌린다(PD 2026-10-26 「20:9 여백이 심각 · 4:3 옆 여백이 크다 · 스케일링이랑 피봇으로」).
    ///
    /// 배율 = 보기 칸 폭 ÷ 그린 폭 · 보기 칸 높이 ÷ 그린 높이 중 **작은 쪽**(1 ~ <see cref="MaxScale"/>) — 넘치지 않게.
    ///   4:3(보기 960 × 966)  → 폭은 1.33 까지 되지만 높이가 먼저 닿아 1.1 ~ 1.2 · 옆 여백이 그만큼 준다
    ///   20:9(보기 720 × 1286) → 폭이 먼저 닿아 1 · 대신 남는 세로(약 320)를 띠 사이에 나눈다
    /// 기준점(pivot)은 위 가운데 — 커져도 머리 줄 바로 밑에서 시작한다.
    /// 띠 경계(_breaks, 내용 위에서 잰 게임 px)보다 아래에 있는 자식은 경계 하나마다 벌린 몫만큼 내려간다.
    /// 남는 것이 없으면(16:9 거의) 그린 그대로다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class RbFitBands : MonoBehaviour
    {
        public const float MaxScale = 1.5f;

        [SerializeField] private float _designWidth = 720f;
        [SerializeField] private float _designHeight;
        [SerializeField] private float[] _breaks = System.Array.Empty<float>();
        // 층 줄 모드 — 남는 세로를 띠 사이가 아니라 한 판(_growNode) 안 줄(_rowPrefix…) 간격에 준다(탑 층 사다리)
        [SerializeField] private string _growNode, _rowPrefix;
        private Vector2 _growHomeSize;

        private RectTransform _rt;
        private Vector2[] _home;
        private RectTransform[] _kids;
        private Vector2 _lastView = new(-1f, -1f);

        public void Setup(float designHeight, float[] breaks)
        {
            _designHeight = designHeight;
            _breaks = breaks ?? System.Array.Empty<float>();
        }

        /// <summary>남는 세로를 판 하나(bg) 안 줄들(이름 앞머리 rowPrefix) 간격에 준다 — 판은 그만큼 길어지고, 그 아래는 통째로 내려간다.</summary>
        public void GrowRows(string bg, string rowPrefix)
        {
            _growNode = bg;
            _rowPrefix = rowPrefix;
        }

        private void Awake() => _rt = (RectTransform)transform;

        private void OnEnable() => _lastView = new Vector2(-1f, -1f);

        private void LateUpdate()
        {
            if (_rt == null || _rt.parent is not RectTransform view || _designHeight <= 0f) return;
            var size = view.rect.size;
            if ((size - _lastView).sqrMagnitude < 0.25f) return;
            _lastView = size;
            Apply(size);
        }

        private void Apply(Vector2 view)
        {
            var scroll = _rt.parent.GetComponent<ScrollRect>();
            if (_kids == null)
            {
                // 처음 한 번 — 자식의 그린 자리를 기억한다(벌림은 늘 이 자리에서 더한다)
                _kids = new RectTransform[_rt.childCount];
                _home = new Vector2[_rt.childCount];
                for (int i = 0; i < _rt.childCount; i++)
                {
                    _kids[i] = (RectTransform)_rt.GetChild(i);
                    _home[i] = _kids[i].anchoredPosition;
                }
            }
            float k = Mathf.Clamp(Mathf.Min(view.x / _designWidth, view.y / _designHeight), 1f, MaxScale);
            // 그린 폭 그대로(가운데) — 보기 칸이 넓어도 판이 늘어나지 않고 배율로만 커진다
            _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 1f);
            _rt.pivot = new Vector2(0.5f, 1f);
            _rt.sizeDelta = new Vector2(_designWidth, _designHeight);
            _rt.localScale = new Vector3(k, k, 1f);
            // 남는 세로 — 위 · 띠 사이 · 아래로 고르게(띠 경계 수 + 2 칸)
            float spare = Mathf.Max(0f, view.y - _designHeight * k);
            float gap = spare / (_breaks.Length + 2) / k;   // 내용 안 단위로
            // 다 들어가면 스크롤을 끈다(PD 「스크롤 될 필요 없다」) — 켜 두면 스크롤이 내용 자리를 위로 되돌린다
            if (scroll != null) scroll.enabled = _designHeight * k > view.y + 1f;
            _rt.anchoredPosition = new Vector2(0f, -gap * k);
            if (!string.IsNullOrEmpty(_growNode)) { GrowRowsApply(spare / k); return; }
            for (int i = 0; i < _kids.Length; i++)
            {
                if (_kids[i] == null) continue;
                // 자식 윗변(내용 위에서 잰 깊이)으로 몇 번째 띠인지
                float top = -(_home[i].y + _kids[i].rect.height * (1f - _kids[i].pivot.y));
                int band = 0;
                for (int b = 0; b < _breaks.Length; b++) if (top >= _breaks[b]) band++;
                _kids[i].anchoredPosition = _home[i] + new Vector2(0f, -gap * band);
            }
        }

        // 남는 세로(내용 단위) 전부를 판 안 줄 간격에 — 줄 n 개는 길어진 판 안에 고르게, 판 아래 묶음은 통째로 아래로
        private void GrowRowsApply(float extra)
        {
            _rt.anchoredPosition = Vector2.zero;
            RectTransform bg = null;
            int bgIndex = -1;
            var rows = new System.Collections.Generic.List<int>();
            for (int i = 0; i < _kids.Length; i++)
            {
                if (_kids[i] == null) continue;
                if (_kids[i].name == _growNode) { bg = _kids[i]; bgIndex = i; }
                else if (IsRow(_kids[i].name)) rows.Add(i);
            }
            if (bg == null) return;
            if (_growHomeSize == Vector2.zero) _growHomeSize = bg.sizeDelta;
            float bgBottom = -(_home[bgIndex].y - _growHomeSize.y * bg.pivot.y);   // 판 아랫변 깊이
            rows.Sort((a, b) => _home[b].y.CompareTo(_home[a].y));               // 위 → 아래
            for (int i = 0; i < _kids.Length; i++)
            {
                if (_kids[i] == null || i == bgIndex || rows.Contains(i)) continue;
                float top = -(_home[i].y + _kids[i].rect.height * (1f - _kids[i].pivot.y));
                _kids[i].anchoredPosition = _home[i] + new Vector2(0f, top >= bgBottom - 0.5f ? -extra : 0f);
            }
            // 판 — 위 변은 그대로, 아래로 extra 만큼 길어진다
            bg.sizeDelta = _growHomeSize + new Vector2(0f, extra);
            bg.anchoredPosition = _home[bgIndex] + new Vector2(0f, -extra * (1f - bg.pivot.y));
            // 줄 — 줄 사이 n-1 칸 + 위 · 아래 반 칸씩에 고르게
            int n = rows.Count;
            for (int r = 0; r < n; r++)
                _kids[rows[r]].anchoredPosition = _home[rows[r]] + new Vector2(0f, -extra * (r + 0.5f) / n);
        }

        // 앞머리 바로 뒤가 숫자인 것만 줄이다 — ChlFloor0 은 줄, ChlFloorTitle · ChlFloorDesc 는 아래 판 글자
        private bool IsRow(string name) =>
            name.Length > _rowPrefix.Length && name.StartsWith(_rowPrefix) && char.IsDigit(name[_rowPrefix.Length]);
    }
}
