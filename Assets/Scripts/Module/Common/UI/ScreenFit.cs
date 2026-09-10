using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 9:16(720×1280) 로 그린 화면을 다른 비율의 기기에 맞춘다 (2026-09-10).
    ///
    /// ── 무엇이 문제였나 ──────────────────────────────────────
    /// 잘리지 않게 맞추는 규칙(`SafeArea` 의 contain)이 기기마다 **보이는 칸**을 바꾼다.
    ///   태블릿 4:3 768×1024 → 960 × 1280 (가로가 240 남는다)
    ///   폰 20:9 1080×2400  → 720 × 1600 (세로가 320 남는다)
    /// 720×1280 으로 그린 UI 가 그 안에 몰려서, 남는 칸이 어두운 띠로 남았다.
    /// 「화면이 잘렸다」는 지적이 여기서 나왔다.
    ///
    /// ── 규칙 (가로·세로 같은 규칙) ──────────────────────────
    ///
    ///   ① **부모를 꽉 채우는 놈**(배경·크롬 판)은 앵커를 스트레치로 바꿔 화면 끝까지 늘린다.
    ///   ② 그 안의 것들은 **어느 변에 붙어 있는지 가려** 그쪽 가장자리에 붙인다.
    ///      앵커와 **pivot 을 같은 쪽으로 맞춰** 두어 좌표가 「그 변에서의 여백」이라는 뜻을 갖게 한다.
    ///   ③ 어느 쪽도 아니면 가운데.
    ///
    /// ── 붙어 있는 것은 묶어서 옮긴다 ────────────────────────
    /// ⚠ 낱개로 판단하면 **한 줄이 찢어진다.** 로비 상단 재화 3칸은 720 한가운데를 걸쳐 있어
    ///   낱개로는 스태미나가 왼쪽 · 잼이 오른쪽으로 갈라진다.
    ///   **다른 축이 겹치고**(같은 줄·같은 단) 이 축으로 24 px 안에 붙은 형제는 한 덩어리다.
    ///
    /// ⚠ 간격만으로는 못 가른다. 재화 3칸 사이(7~9 px)와 HUD 좌우 박스 사이(8 px)가
    ///   **같은 크기**다. 그래서 덩어리가 **양끝에 다 닿을 때만**(줄을 가로지를 때만)
    ///   가장 넓은 틈 하나에서 갈라 양쪽에 붙인다.
    ///
    /// ── pivot 을 앵커에 맞추는 이유 ─────────────────────────
    /// ⚠ 예전에는 pivot 을 0 으로 둔 채 「720 에서 뺀 값」을 좌표에 적어 넣었다.
    ///   그건 앵커를 쓴 것이 아니라 **좌표를 손으로 고친 것**이라, 기준이 바뀌면 전부 다시
    ///   계산해야 하고 어디가 기준인지도 코드에 안 남는다.
    ///
    /// ── 어디에 붙이나 ───────────────────────────────────────
    /// 각 `~UI` · `~Panel` 프리팹의 **루트**에 하나씩. 프리팹이 제 몸을 스스로 맞추므로
    /// 나중에 불러오는 창도 저절로 맞는다.
    ///
    /// 늘어나면 안 되는 놈은 <see cref="ScreenFitLock"/> 을 붙인다.
    /// </summary>
    // ⚠ **누구보다 먼저 뜬다.** 게임 코드가 `Awake` 에서 계층을 옮기는 일이 있는데
    //   (`InGameMainUI` 가 D패드를 `ControlGroup` 밖 루트로 꺼낸다), 그보다 늦게 뜨면
    //   **이미 밀린 자리**가 「그린 값」으로 굳어 버린다.
    //   실제로 태블릿에서 D패드가 34 가 아니라 154 에 그려졌고, 그 값이 폰까지 따라왔다.
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ScreenFit : MonoBehaviour
    {
        /// <summary>이 화면을 그린 기준 크기. 9:16 이다.</summary>
        [SerializeField] private float _baseWidth = 720f;
        [SerializeField] private float _baseHeight = 1280f;

        /// <summary>부모 크기에서 이만큼 안이면 「꽉 채운다」로 본다.</summary>
        private const float FullSlack = 32f;

        /// <summary>
        /// 동시에 부모의 이 비율 이상이어야 「꽉 채운다」로 본다.
        ///
        /// ⚠ 픽셀 여유(32)만 보면 **작은 부모에서 틀린다.** 팁바(높이 70) 안의 글자칸(50)이
        ///   70−32=38 을 넘는다는 이유로 「채운다」가 되어 세로로 늘어났고,
        ///   TMP 자동 크기가 글자를 키워 **줄바꿈이 통째로 달라졌다.**
        ///   50/70 = 0.71 이라 비율로 보면 채우는 것이 아니다.
        /// </summary>
        private const float FullRatio = 0.9f;

        /// <summary>이만큼 안에 붙어 있으면 한 덩어리로 본다.</summary>
        private const float ClusterGap = 24f;

        /// <summary>덩어리 중심이 이 비율 밖이면 그쪽 가장자리에 붙인다.</summary>
        private const float SideBias = 0.4f;

        /// <summary>
        /// 덩어리가 양끝에서 이 비율 안까지 닿으면 「가로지른다」로 보고 갈라 붙인다.
        ///
        /// ⚠ 처음엔 「폭의 70 % 를 덮으면」으로 했다가 틀렸다. 로비의 「고스트 + 재화 3칸」이
        ///   74 % 를 덮는데 그건 가로지르는 줄이 아니라 **왼쪽 덩어리**다.
        ///   갈랐더니 잼 칸 하나가 떨어져 나가 오른쪽으로 날아갔다.
        /// </summary>
        private const float EdgeTouch = 0.06f;

        /// <summary>한 축에서 어디에 붙을지. 가로는 왼·오, 세로는 아래·위다.</summary>
        private enum Side { Min, Center, Max }

        /// <summary>한 축에 대해 그려진 그대로의 값.</summary>
        private struct Box
        {
            public float Min;         // 부모(9:16) 안에서의 시작 변
            public float Size;
            public float ParentBase;  // 그 부모의 9:16 크기
            public bool Full;         // 부모를 꽉 채우는가
            public Side Side;
        }

        private struct Entry
        {
            public RectTransform Rect;
            public Box X;
            public Box Y;
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

            // ⚠ 기준은 **이 루트 자신의 그려진 크기**다. 9:16 캔버스 값을 그대로 쓰면
            //   `~Panel` 처럼 화면보다 작은 판에서 어긋난다 — `HostSelectPanel` 은 높이가
            //   1152 인데 1280 으로 재는 바람에 하단 팁바가 128 칸 위로 올라와
            //   호스트 목록 마지막 줄을 덮었다.
            //   늘어나는 축은 캔버스 기준을, 고정인 축은 제 크기를 쓴다.
            var self = (RectTransform)transform;
            bool stretchX = self.anchorMin.x < 0.01f && self.anchorMax.x > 0.99f;
            bool stretchY = self.anchorMin.y < 0.01f && self.anchorMax.y > 0.99f;
            Scan(self,
                 stretchX ? _baseWidth : self.sizeDelta.x,
                 stretchY ? _baseHeight : self.sizeDelta.y);
        }

        private void Scan(RectTransform parent, float baseW, float baseH)
        {
            int from = _entries.Count;

            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i) is not RectTransform rt) continue;
                // 제 몸을 스스로 맞추는 프리팹(창 등)은 건드리지 않는다. 두 번 맞추면
                // 두 번째가 첫 번째의 결과를 기준으로 삼아 조금씩 밀린다.
                if (rt.GetComponent<ScreenFit>() != null) continue;

                // 잠근 놈은 **아예 건드리지 않는다.** 크기를 게임 코드가 직접 재는 판
                // (플레이 필드)이라, 여기서 다시 적으면 그쪽 계산을 덮어쓴다.
                if (rt.GetComponent<ScreenFitLock>() != null) continue;

                _entries.Add(new Entry
                {
                    Rect = rt,
                    X = ReadBox(rt.anchorMin.x, rt.anchorMax.x, rt.pivot.x,
                                rt.anchoredPosition.x, rt.sizeDelta.x, baseW),
                    Y = ReadBox(rt.anchorMin.y, rt.anchorMax.y, rt.pivot.y,
                                rt.anchoredPosition.y, rt.sizeDelta.y, baseH),
                });
            }

            int to = _entries.Count;
            AssignSides(from, to, horizontal: true);
            AssignSides(from, to, horizontal: false);

            // 커지는 놈 안쪽만 다시 본다. 박스가 그대로인 놈의 자식은 손댈 이유가 없다.
            //
            // ⚠ 안쪽의 기준 크기는 **그 부모 자신의 크기**다. 할아버지 기준을 그대로 넘기면
            //   조작바 배경(`ControlGrid`, 230 높이)의 세로 기준이 1280 으로 잡혀
            //   「꽉 채운다」로 안 읽히고 엉뚱한 변에 붙는다.
            for (int i = from; i < to; i++)
            {
                var e = _entries[i];
                if (!e.X.Full && !e.Y.Full) continue;
                Scan(e.Rect,
                     e.X.Full ? e.X.ParentBase : e.X.Size,
                     e.Y.Full ? e.Y.ParentBase : e.Y.Size);
            }
        }

        /// <summary>앵커가 무엇이든 「9:16 부모 안에서의 시작 변과 크기」로 되돌린다.</summary>
        private static Box ReadBox(float aMin, float aMax, float pivot, float pos, float sizeDelta,
                                   float parentBase)
        {
            bool stretched = aMin < 0.01f && aMax > 0.99f;
            float size = stretched ? parentBase : sizeDelta;
            float min = stretched ? 0f : aMin * parentBase + pos - size * pivot;
            return new Box
            {
                Min = min,
                Size = size,
                ParentBase = parentBase,
                Full = stretched
                       || (size >= parentBase - FullSlack && size >= parentBase * FullRatio),
            };
        }

        // ── 어느 변에 붙일지 정한다 ──────────────────────────────

        private void AssignSides(int from, int to, bool horizontal)
        {
            var items = new List<int>();
            for (int i = from; i < to; i++)
                if (!Axis(_entries[i], horizontal).Full) items.Add(i);
            if (items.Count == 0) return;

            // **다른 축이 겹치고** 이 축으로 가까우면 한 덩어리 (합집합 찾기)
            var group = new int[items.Count];
            for (int i = 0; i < group.Length; i++) group[i] = i;

            for (int a = 0; a < items.Count; a++)
            for (int b = a + 1; b < items.Count; b++)
            {
                var ba = Axis(_entries[items[a]], horizontal);
                var bb = Axis(_entries[items[b]], horizontal);
                var oa = Axis(_entries[items[a]], !horizontal);
                var ob = Axis(_entries[items[b]], !horizontal);
                bool sameLine = oa.Min < ob.Min + ob.Size && ob.Min < oa.Min + oa.Size;
                if (!sameLine) continue;
                float gap = Mathf.Max(ba.Min - (bb.Min + bb.Size), bb.Min - (ba.Min + ba.Size));
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

            foreach (var bucket in buckets.Values) ResolveCluster(bucket, horizontal);
        }

        private static int Find(int[] group, int x)
        {
            while (group[x] != x) x = group[x] = group[group[x]];
            return x;
        }

        /// <summary>
        /// 한 덩어리가 어느 변에 붙을지 정한다.
        ///
        /// ⚠ 덩어리가 **양끝에 다 닿으면**(상단 HUD 한 줄, 또는 세로로 HUD~조작바 전체처럼)
        ///   한쪽으로 몰 수 없다. 가장 넓게 벌어진 곳에서 둘로 갈라 양쪽에 붙인다.
        ///   그래야 「체력은 왼쪽 · 재화는 오른쪽」, 「HUD 는 위 · 조작바는 아래」가 저절로 나온다.
        /// </summary>
        private void ResolveCluster(List<int> bucket, bool horizontal)
        {
            bucket.Sort((a, b) => Axis(_entries[a], horizontal).Min
                        .CompareTo(Axis(_entries[b], horizontal).Min));

            float pBase = Axis(_entries[bucket[0]], horizontal).ParentBase;
            if (pBase <= 0f) return;

            float min = Axis(_entries[bucket[0]], horizontal).Min;
            float max = min;
            for (int i = 0; i < bucket.Count; i++)
            {
                var b = Axis(_entries[bucket[i]], horizontal);
                max = Mathf.Max(max, b.Min + b.Size);
            }

            bool touchesMin = min <= pBase * EdgeTouch;
            bool touchesMax = max >= pBase * (1f - EdgeTouch);

            if (touchesMin && touchesMax && bucket.Count >= 2)
            {
                int cut = -1; float widest = -1f; float edge = min;
                for (int i = 0; i < bucket.Count; i++)
                {
                    var b = Axis(_entries[bucket[i]], horizontal);
                    if (i > 0 && b.Min - edge > widest) { widest = b.Min - edge; cut = i; }
                    edge = Mathf.Max(edge, b.Min + b.Size);
                }
                if (cut > 0)
                {
                    for (int i = 0; i < bucket.Count; i++)
                        SetSide(bucket[i], horizontal, i < cut ? Side.Min : Side.Max);
                    return;
                }
            }

            // 한쪽에만 닿으면 그쪽에 붙인다. 세로에서 이게 없으면 「HUD + 필드」 덩어리가
            // 가운데(비율 0.402)로 판정되어 상단 HUD 가 화면 위에서 떨어진다.
            Side side;
            if (touchesMin && !touchesMax) side = Side.Min;
            else if (touchesMax && !touchesMin) side = Side.Max;
            else
            {
                float ratio = (min + max) * 0.5f / pBase;
                side = ratio < SideBias ? Side.Min : ratio > 1f - SideBias ? Side.Max : Side.Center;
            }
            for (int i = 0; i < bucket.Count; i++) SetSide(bucket[i], horizontal, side);
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
                ApplyAxis(e.Rect, e.X, horizontal: true);
                ApplyAxis(e.Rect, e.Y, horizontal: false);
            }
        }

        private static void ApplyAxis(RectTransform rt, Box box, bool horizontal)
        {
            var aMin = rt.anchorMin; var aMax = rt.anchorMax;
            var pivot = rt.pivot; var size = rt.sizeDelta; var pos = rt.anchoredPosition;

            if (box.Full)
            {
                // 스트레치. `offsetMin/Max` 로 적어야 pivot 과 무관하게 정확히 채운다.
                if (horizontal) { aMin.x = 0f; aMax.x = 1f; pivot.x = 0.5f; }
                else            { aMin.y = 0f; aMax.y = 1f; pivot.y = 0.5f; }
                rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;

                var oMin = rt.offsetMin; var oMax = rt.offsetMax;
                if (horizontal) { oMin.x = 0f; oMax.x = 0f; } else { oMin.y = 0f; oMax.y = 0f; }
                rt.offsetMin = oMin; rt.offsetMax = oMax;
                return;
            }

            float anchor = box.Side == Side.Min ? 0f : box.Side == Side.Max ? 1f : 0.5f;
            float margin = box.Side switch
            {
                Side.Min => box.Min,                                    // 시작 변에서
                Side.Max => box.Min + box.Size - box.ParentBase,        // 끝 변에서(음수)
                _ => box.Min + box.Size * 0.5f - box.ParentBase * 0.5f, // 가운데에서
            };

            if (horizontal) { aMin.x = anchor; aMax.x = anchor; pivot.x = anchor; size.x = box.Size; pos.x = margin; }
            else            { aMin.y = anchor; aMax.y = anchor; pivot.y = anchor; size.y = box.Size; pos.y = margin; }

            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.sizeDelta = size; rt.anchoredPosition = pos;
        }

        private static Box Axis(Entry e, bool horizontal) => horizontal ? e.X : e.Y;

        private void SetSide(int index, bool horizontal, Side side)
        {
            var e = _entries[index];
            if (horizontal) { var b = e.X; b.Side = side; e.X = b; }
            else            { var b = e.Y; b.Side = side; e.Y = b; }
            _entries[index] = e;
        }
    }
}
