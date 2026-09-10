using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 9:16(720×1280) 로 그린 화면을 더 넓은 기기에 맞춘다 (2026-09-10).
    ///
    /// ── 무엇이 문제였나 ──────────────────────────────────────
    /// 잘리지 않게 맞추는 규칙(`SafeArea` 의 contain)이 태블릿 4:3 에서 세로를 기준으로
    /// 고르면서 **보이는 가로 칸이 960** 이 된다. 720 으로 그린 UI 가 그 가운데 몰려
    /// 좌우에 어두운 띠가 남고, 그게 「화면이 잘렸다」로 읽혔다.
    ///
    /// ── 규칙 ────────────────────────────────────────────────
    ///
    ///   ① **부모 폭을 채우는 놈**(배경·크롬 판)은 앵커를 스트레치로 바꿔 화면 끝까지 늘린다.
    ///   ② 그 안의 것들은 **왼쪽/오른쪽을 가려** 그쪽 가장자리에 붙인다.
    ///      앵커와 **pivot 을 같은 쪽으로 맞춰** 두면 좌표가 곧 「그 변에서 이만큼」이 된다.
    ///   ③ 어느 쪽도 아니면(가운데 걸침) 가운데에 둔다.
    ///
    /// ── 붙어 있는 것은 묶어서 옮긴다 ────────────────────────
    /// ⚠ 하나씩 따로 판단하면 **한 줄이 찢어진다.** 로비 상단의 재화 3칸(스태미나·골드·잼)은
    ///   720 한가운데를 걸쳐 있어서, 낱개로 재면 스태미나는 왼쪽·잼은 오른쪽으로 갈라진다.
    ///   가로로 24 px 안에 붙어 있는 형제는 **한 덩어리로 보고 같이** 옮긴다.
    ///
    /// ── pivot 을 앵커에 맞추는 이유 ─────────────────────────
    /// ⚠ 예전에는 pivot 을 0 으로 둔 채 「720 에서 뺀 값」을 좌표에 적어 넣었다.
    ///   그건 앵커를 쓴 것이 아니라 **좌표를 손으로 고친 것**이라, 기준 폭이 바뀌면
    ///   전부 다시 계산해야 하고 어디가 기준인지도 코드에 안 남는다.
    ///   pivot 을 붙는 변에 맞추면 좌표가 여백이라는 **뜻**을 갖는다.
    ///
    /// ── 어디에 붙이나 ───────────────────────────────────────
    /// 각 `~UI` · `~Panel` 프리팹의 **루트**에 하나씩. 프리팹이 제 몸을 스스로 맞추므로
    /// 나중에 불러오는 창도 저절로 맞는다.
    ///
    /// 늘어나면 안 되는 놈(고정 크기 플레이 필드)은 <see cref="ScreenFitLock"/> 을 붙인다.
    /// </summary>
    // ⚠ **누구보다 먼저 뜬다.** 게임 코드가 `Awake` 에서 계층을 옮기는 일이 있는데
    //   (`InGameMainUI` 가 D패드를 `ControlGroup` 밖 루트로 꺼낸다), 그보다 늦게 뜨면
    //   **이미 넓은 화면만큼 밀린 자리**가 「그린 값」으로 굳어 버린다.
    //   실제로 태블릿에서 D패드가 34 가 아니라 154 에 그려졌고, 그 값이 폰까지 따라왔다.
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ScreenFit : MonoBehaviour
    {
        /// <summary>이 화면을 그린 기준 폭. 9:16 의 가로다.</summary>
        [SerializeField] private float _baseWidth = 720f;

        /// <summary>부모 폭에서 이만큼 안이면 「폭을 채운다」로 본다.</summary>
        private const float FullSlack = 32f;

        /// <summary>가로로 이만큼 안에 붙어 있으면 한 덩어리로 본다.</summary>
        private const float ClusterGap = 24f;

        /// <summary>덩어리 중심이 부모의 이 비율 밖이면 그쪽 가장자리에 붙인다.</summary>
        private const float SideBias = 0.4f;

        /// <summary>
        /// 덩어리가 양쪽 끝에서 이 비율 안까지 닿으면 「줄을 가로지른다」로 보고 갈라 붙인다.
        ///
        /// ⚠ 처음엔 「폭의 70 % 를 덮으면」으로 했다가 틀렸다. 로비 상단의
        ///   「고스트 + 재화 3칸」이 74 % 를 덮는데 그건 가로지르는 줄이 아니라
        ///   **왼쪽 덩어리**다. 갈랐더니 잼 칸 하나가 떨어져 나가 오른쪽으로 날아갔다.
        ///   양쪽 끝에 **다 닿아야** 가로지르는 줄이다.
        /// </summary>
        private const float EdgeTouch = 0.06f;

        private enum Side { Left, Center, Right }

        /// <summary>그려진 그대로의 값. 화면이 바뀌어도 **여기서 다시 계산한다.**</summary>
        private struct Entry
        {
            public RectTransform Rect;
            public bool Full;          // 부모 폭을 채우는가
            public float Left;         // 부모(9:16) 안에서의 왼쪽 변
            public float Width;
            public float Bottom;       // 같은 줄인지 가리기 위한 세로 범위
            public float Height;
            public float ParentBase;   // 그 부모의 9:16 폭
            public Side Side;          // 어느 변에 붙일지 (덩어리로 정한다)
        }

        private readonly List<Entry> _entries = new();
        private Vector2Int _lastScreen;
        private bool _captured;

        private void Awake() => Capture();

        private void OnEnable() => Apply();

        private void Update()
        {
            if (Screen.width == _lastScreen.x && Screen.height == _lastScreen.y) return;
            Apply();
        }

        // ── 그려진 값을 뜬다 ─────────────────────────────────────
        //
        // ⚠ **아무것도 건드리기 전에** 떠야 한다. 한 번 맞춘 뒤에 다시 뜨면 맞춘 값이
        //   기준이 되어, 화면을 바꿀 때마다 조금씩 밀린다.

        private void Capture()
        {
            if (_captured) return;
            _captured = true;
            Scan((RectTransform)transform, _baseWidth);
        }

        private void Scan(RectTransform parent, float parentBase)
        {
            int from = _entries.Count;

            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i) is not RectTransform rt) continue;
                // 제 몸을 스스로 맞추는 프리팹(창 등)은 건드리지 않는다. 두 번 맞추면
                // 두 번째가 첫 번째의 결과를 기준으로 삼아 조금씩 밀린다.
                if (rt.GetComponent<ScreenFit>() != null) continue;

                bool stretched = rt.anchorMin.x < 0.01f && rt.anchorMax.x > 0.99f;
                float width = stretched ? parentBase : rt.sizeDelta.x;
                // 앵커가 무엇이든 「9:16 부모 안에서의 왼쪽 변」으로 되돌린다.
                float left = stretched
                    ? 0f
                    : rt.anchorMin.x * parentBase + rt.anchoredPosition.x - width * rt.pivot.x;

                bool locked = rt.GetComponent<ScreenFitLock>() != null;
                bool full = !locked && (stretched || width >= parentBase - FullSlack);

                // 세로는 건드리지 않으므로 지금 값을 그대로 쓴다 — 같은 줄인지만 가리면 된다.
                float ph = parent.rect.height;
                float height = rt.rect.height;
                float bottom = rt.anchorMin.y * ph + rt.anchoredPosition.y - height * rt.pivot.y;

                _entries.Add(new Entry
                {
                    Rect = rt, Full = full, Left = left, Width = width,
                    Bottom = bottom, Height = height, ParentBase = parentBase,
                });
            }

            int to = _entries.Count;
            AssignSides(from, to);

            // 폭이 넓어지는 놈 안쪽만 다시 본다. 박스가 그대로인 놈의 자식은
            // 손댈 이유가 없다 — 건드리면 그리기만 흔들린다.
            for (int i = from; i < to; i++)
                if (_entries[i].Full) Scan(_entries[i].Rect, _entries[i].ParentBase);
        }

        /// <summary>
        /// 형제들을 **같은 줄에서 가로로 붙은 덩어리**로 묶고, 덩어리마다 어느 변에 붙을지 정한다.
        ///
        /// 낱개로 재면 한 줄이 찢어진다. 로비 재화 3칸(스태미나·골드·잼)은 720 한가운데를
        /// 걸쳐 있어서, 낱개로는 스태미나가 왼쪽 · 잼이 오른쪽으로 갈라진다.
        ///
        /// 세로가 안 겹치면 묶지 않는다. 안 그러면 로비 루트에서 넓은 탭바가 위아래 카드들과
        /// 통째로 한 덩어리가 되어 **화면 전체가 한 덩어리**로 판정된다.
        /// </summary>
        private void AssignSides(int from, int to)
        {
            var items = new List<int>();
            for (int i = from; i < to; i++) if (!_entries[i].Full) items.Add(i);
            if (items.Count == 0) return;

            // 같은 줄 + 가로로 가까움 → 한 덩어리 (합집합 찾기)
            var group = new int[items.Count];
            for (int i = 0; i < group.Length; i++) group[i] = i;

            for (int a = 0; a < items.Count; a++)
            for (int b = a + 1; b < items.Count; b++)
            {
                var ea = _entries[items[a]];
                var eb = _entries[items[b]];
                bool sameRow = ea.Bottom < eb.Bottom + eb.Height && eb.Bottom < ea.Bottom + ea.Height;
                if (!sameRow) continue;
                float gap = Mathf.Max(ea.Left - (eb.Left + eb.Width), eb.Left - (ea.Left + ea.Width));
                if (gap > ClusterGap) continue;
                int ra = Find(group, a), rb = Find(group, b);
                if (ra != rb) group[ra] = rb;
            }

            var buckets = new Dictionary<int, List<int>>();
            for (int i = 0; i < items.Count; i++)
            {
                int r = Find(group, i);
                if (!buckets.TryGetValue(r, out var list)) buckets[r] = list = new List<int>();
                list.Add(items[i]);
            }

            foreach (var bucket in buckets.Values) ResolveCluster(bucket);
        }

        private static int Find(int[] group, int x)
        {
            while (group[x] != x) x = group[x] = group[group[x]];
            return x;
        }

        /// <summary>
        /// 한 덩어리가 어느 변에 붙을지 정한다.
        ///
        /// ⚠ 덩어리가 **줄을 가로지르면**(상단 HUD 한 줄처럼) 한쪽으로 몰 수 없다.
        ///   가장 넓게 벌어진 곳에서 둘로 갈라 왼쪽 조각은 왼쪽, 오른쪽 조각은 오른쪽에 붙인다.
        ///   그래야 「체력 칸은 왼쪽 · 재화와 일시정지는 오른쪽」이 저절로 나온다.
        ///
        /// ⚠ 간격만으로는 못 가른다. 재화 3칸 사이(7~9 px)와 HUD 좌우 박스 사이(8 px)가
        ///   **같은 크기**라, 문턱값을 어디에 두든 한쪽은 틀린다. 그래서 「줄을 가로지르는가」를
        ///   먼저 보고, 가로지를 때만 **가장 넓은 틈** 하나에서 가른다.
        /// </summary>
        private void ResolveCluster(List<int> bucket)
        {
            bucket.Sort((a, b) => _entries[a].Left.CompareTo(_entries[b].Left));
            float pBase = _entries[bucket[0]].ParentBase;
            if (pBase <= 0f) return;

            float left = _entries[bucket[0]].Left;
            float right = left;
            for (int i = 0; i < bucket.Count; i++)
                right = Mathf.Max(right, _entries[bucket[i]].Left + _entries[bucket[i]].Width);

            bool touchesBoth = left <= pBase * EdgeTouch && right >= pBase * (1f - EdgeTouch);
            if (touchesBoth && bucket.Count >= 2)
            {
                int cut = -1; float widest = -1f; float edge = left;
                for (int i = 0; i < bucket.Count; i++)
                {
                    var e = _entries[bucket[i]];
                    if (i > 0 && e.Left - edge > widest) { widest = e.Left - edge; cut = i; }
                    edge = Mathf.Max(edge, e.Left + e.Width);
                }
                if (cut > 0)
                {
                    for (int i = 0; i < bucket.Count; i++)
                        SetSide(bucket[i], i < cut ? Side.Left : Side.Right);
                    return;
                }
            }

            float ratio = (left + right) * 0.5f / pBase;
            var side = ratio < SideBias ? Side.Left : ratio > 1f - SideBias ? Side.Right : Side.Center;
            for (int i = 0; i < bucket.Count; i++) SetSide(bucket[i], side);
        }

        private void SetSide(int index, Side side)
        {
            var e = _entries[index];
            e.Side = side;
            _entries[index] = e;
        }

        // ── 맞춘다 ───────────────────────────────────────────────

        private void Apply()
        {
            Capture();
            _lastScreen = new Vector2Int(Screen.width, Screen.height);

            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e.Rect == null) continue;

                if (e.Full)
                {
                    // 가로만 늘린다. 세로는 그린 그대로 둔다 — 세로는 contain 이 이미 맞췄다.
                    e.Rect.anchorMin = new Vector2(0f, e.Rect.anchorMin.y);
                    e.Rect.anchorMax = new Vector2(1f, e.Rect.anchorMax.y);
                    e.Rect.pivot = new Vector2(0.5f, e.Rect.pivot.y);
                    e.Rect.offsetMin = new Vector2(0f, e.Rect.offsetMin.y);
                    e.Rect.offsetMax = new Vector2(0f, e.Rect.offsetMax.y);
                    continue;
                }

                // 앵커와 pivot 을 붙는 변에 맞춘다 → 좌표가 「그 변에서의 여백」이 된다.
                float anchor = e.Side == Side.Left ? 0f : e.Side == Side.Right ? 1f : 0.5f;
                float margin = e.Side switch
                {
                    Side.Left => e.Left,                                   // 왼쪽 변에서
                    Side.Right => e.Left + e.Width - e.ParentBase,         // 오른쪽 변에서(음수)
                    _ => e.Left + e.Width * 0.5f - e.ParentBase * 0.5f,    // 가운데에서
                };

                e.Rect.anchorMin = new Vector2(anchor, e.Rect.anchorMin.y);
                e.Rect.anchorMax = new Vector2(anchor, e.Rect.anchorMax.y);
                e.Rect.pivot = new Vector2(anchor, e.Rect.pivot.y);
                e.Rect.sizeDelta = new Vector2(e.Width, e.Rect.sizeDelta.y);
                e.Rect.anchoredPosition = new Vector2(margin, e.Rect.anchoredPosition.y);
            }
        }
    }
}
