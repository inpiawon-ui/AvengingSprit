using Game.Character;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 위험 구역 하나. **그리는 도형이자 판정하는 도형이다.**
    ///
    /// ── 이것이 이 발주의 핵심이다 ───────────────────────────────
    /// 예고에서 바닥에 그리고, 발동에서 그 안을 친다 — **같은 숫자, 같은 함수.**
    /// 그리기 코드와 판정 코드를 따로 두면 언젠가 한쪽만 고쳐져
    /// "분명히 피했는데 맞았다" 가 나온다. 하나로 묶으면 그 버그가
    /// **구조적으로 불가능해진다.**
    ///
    /// 그래서 <see cref="Contains"/>(판정)와 <see cref="Outline"/>(그리기)는
    /// 반드시 같은 필드만 읽는다. 한쪽에만 있는 값을 만들지 마라.
    ///
    /// ── 왜 struct 인가 ──────────────────────────────────────────
    /// 예고마다 하나씩 생기고 발동하면 사라진다. class 로 두면 방 하나에
    /// 수십 개가 할당됐다 버려진다 — 보스전은 hot path 다.
    ///
    /// 좌표는 전부 **픽셀**이다(방 좌표계). 미터는 부르는 쪽에서 이미 곱해 넘긴다 —
    /// 여기서 다시 곱하면 어디서 곱했는지가 흩어진다.
    /// </summary>
    public struct DangerShape
    {
        public enum Kind
        {
            None,
            /// <summary>부채꼴 — 중심에서 <see cref="Degrees"/> 만큼 벌어진 <see cref="Radius"/> 반경</summary>
            Wedge,
            /// <summary>직선 띠 — <see cref="Width"/> 폭 · <see cref="Length"/> 길이</summary>
            Band,
            /// <summary>원</summary>
            Disc,
            /// <summary>고리 — 안팎 사이가 위험하고 <see cref="GapDegrees"/> 만큼 틈이 있다</summary>
            Ring,
            /// <summary>세로 줄 — 방을 <see cref="Lanes"/> 등분하고 <see cref="LaneMask"/> 줄만 위험</summary>
            Lanes,
            /// <summary>사분면 — <see cref="LaneMask"/> 에 켜진 분면만 위험</summary>
            Quads,
            /// <summary>구역 밖 전부 — 안전한 섬 하나만 남는다(반전)</summary>
            Outside,
        }

        /// <summary>
        /// 도형이 **여럿일 때** 어디에 놓이는가.
        ///
        /// 24개 중 일곱이 도형 하나로 안 된다 — 미사일 5발 · 천장 파편 5 · 그림자 3 ·
        /// 구멍 2와 5 · 세 갈래 3 · 마디 2. 그렇다고 도형 목록을 들고 다니면
        /// 발화마다 힙을 할당하게 되므로, **자리를 계산으로 낸다.**
        ///
        /// ⚠ 그래서 <see cref="Contains"/> 와 <see cref="Outline"/> 이 **같은 함수**
        ///   (<c>CenterOf</c>·<c>BandOf</c>)로 i번째 자리를 구한다.
        ///   여기가 갈라지면 "그린 자리와 맞는 자리가 다르다" 가 다시 살아난다.
        /// </summary>
        public enum Spread
        {
            /// <summary>하나뿐이다.</summary>
            None,
            /// <summary>부채꼴로 흩뿌린다 — <see cref="Origin"/> 에서 <see cref="Dir"/> 쪽으로
            /// <see cref="Length"/> 만큼 떨어진 호 위에 <see cref="Degrees"/> 폭으로 늘어선다.</summary>
            Fan,
            /// <summary>방 안에 흩어진다. <see cref="Tick"/> 이 자리를 정한다.</summary>
            Scatter,
            /// <summary>바닥 구멍 여섯 곳 중 <see cref="Count"/> 곳. 로봇 스네이크 전용.</summary>
            Holes,
            /// <summary>벽에서 방을 가로지르는 띠 여럿. 파이썬 「세 갈래 돌파」.</summary>
            Walls,
        }

        public Kind Shape;
        public Vector2 Origin;      // 중심(px). Band 는 시작점
        public Vector2 Dir;         // 바라보는 방향(정규화). Wedge·Band 가 쓴다
        public float Degrees;       // Wedge 의 벌어진 각 · Fan 의 벌어진 폭
        public float Radius;        // Wedge·Disc·Ring 바깥 · Outside 안전 반경(px)
        public float Inner;         // Ring 안쪽 반경(px)
        public float Width, Length; // Band(px) · Fan 은 Length 가 흩뿌리는 거리
        public float GapDegrees;    // Ring 의 틈
        public float GapCenterDeg;  // 틈이 지금 어디에 있는가(도). 돌아간다
        public int Lanes;           // Lanes 의 줄 수 · Quads 는 4 고정
        public int LaneMask;        // 위험한 줄/분면의 비트

        public Spread Layout;       // 여럿일 때의 배치
        public int Count;           // 도형 개수. 0·1 이면 하나
        public int Tick;            // 흩뿌림·구멍 고르기의 씨앗. 쓸 때마다 자리가 바뀐다

        public bool IsNone => Shape == Kind.None;

        /// <summary>실제로 그릴 도형 수. 배치가 없으면 언제나 하나다.</summary>
        private int Repeats => Layout == Spread.None || Count <= 1 ? 1 : Count;

        // ── 바닥 구멍 여섯 (로봇 스네이크) ─────────────────────────
        //
        // ⚠ **바닥 그림에 픽셀로 박혀 있는 자리다** (58차 발주 · 720×936 기준
        //   (180,288) (360,252) (540,288) (180,576) (360,612) (540,576)).
        //   여기서는 방 크기에 대한 비율로 들고 있어야 방 크기가 바뀌어도 그림과 안 어긋난다.
        //   그림과 코드가 같은 자를 쓰지 않으면 "구멍은 저기 그려져 있는데 뱀은 여기서 나온다".
        private static readonly Vector2[] HoleAt =
        {
            new(0.25f, 0.3077f), new(0.50f, 0.2692f), new(0.75f, 0.3077f),
            new(0.25f, 0.6154f), new(0.50f, 0.6538f), new(0.75f, 0.6154f),
        };

        /// <summary>i번째 도형의 중심. **판정과 그리기가 이 함수 하나를 같이 쓴다.**</summary>
        private Vector2 CenterOf(int i, Vector2 roomSize)
        {
            switch (Layout)
            {
                case Spread.Fan:
                {
                    // 가운데를 0 으로 두고 좌우로 벌린다. 하나면 정면 하나.
                    float half = Degrees * 0.5f;
                    float t = Count <= 1 ? 0.5f : (float)i / (Count - 1);
                    var dir = Rotate(Dir, Mathf.Lerp(-half, half, t));
                    return Origin + dir * Length;
                }

                case Spread.Scatter:
                {
                    // 결정적 흩뿌림 — 같은 tick·같은 i 면 언제나 같은 자리다.
                    // 난수를 쓰면 그릴 때와 때릴 때 자리가 달라진다.
                    int h = Hash(Tick * 31 + i);
                    float fx = (h & 0xFFFF) / 65535f;
                    float fy = ((h >> 16) & 0xFFFF) / 65535f;
                    // 가장자리에 붙으면 피할 자리가 없다. 안쪽 15~85% 에만 떨군다.
                    return new Vector2(Mathf.Lerp(0.15f, 0.85f, fx) * roomSize.x,
                                       -Mathf.Lerp(0.15f, 0.85f, fy) * roomSize.y);
                }

                case Spread.Holes:
                {
                    var h = HoleAt[(Tick + i) % HoleAt.Length];
                    return new Vector2(h.x * roomSize.x, -h.y * roomSize.y);
                }

                default:
                    return Origin;
            }
        }

        /// <summary>i번째 띠의 시작점과 방향. 벽에서 들어와 방을 가로지른다.</summary>
        private void BandOf(int i, Vector2 roomSize, out Vector2 from, out Vector2 dir)
        {
            if (Layout != Spread.Walls) { from = Origin; dir = Dir; return; }

            // 네 벽을 돌아가며 쓴다. tick 이 시작 벽을 옮겨 매번 다른 조합이 된다.
            int wall = (Tick + i * 1) & 3;
            float t = Count <= 1 ? 0.5f : Mathf.Lerp(0.25f, 0.75f, (float)i / (Count - 1));
            switch (wall)
            {
                case 0: from = new Vector2(0f, -roomSize.y * t);          dir = Vector2.right; break;
                case 1: from = new Vector2(roomSize.x * t, 0f);           dir = Vector2.down;  break;
                case 2: from = new Vector2(roomSize.x, -roomSize.y * t);  dir = Vector2.left;  break;
                default: from = new Vector2(roomSize.x * t, -roomSize.y); dir = Vector2.up;    break;
            }
        }

        /// <summary>자리를 흩뿌리는 데 쓰는 결정적 해시. 난수가 아니라야 그린 자리와 맞는다.</summary>
        private static int Hash(int n)
        {
            unchecked
            {
                n = (n ^ 61) ^ (n >> 16);
                n += n << 3;
                n ^= n >> 4;
                n *= 0x27d4eb2d;
                n ^= n >> 15;
                return n & 0x7FFFFFFF;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  판정 — 이 점이 위험한가
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 이 점이 위험 구역 안인가.
        ///
        /// ⚠ 여기서 읽는 필드가 곧 <see cref="Outline"/> 이 그리는 필드다.
        ///   한쪽에만 조건을 더하지 마라 — 그 순간 둘이 갈라진다.
        /// </summary>
        public bool Contains(Vector2 p, Vector2 roomSize)
        {
            switch (Shape)
            {
                case Kind.Wedge:
                {
                    var to = p - Origin;
                    float d = to.magnitude;
                    if (d > Radius) return false;
                    // 중심에 붙어 있으면 각도를 잴 수 없다. 무조건 안이다.
                    if (d < 0.001f) return true;
                    return Vector2.Angle(Dir, to) <= Degrees * 0.5f;
                }

                case Kind.Band:
                {
                    // 여럿이면 하나라도 닿으면 맞은 것이다.
                    for (int i = 0; i < Repeats; i++)
                    {
                        BandOf(i, roomSize, out var from, out var dir);
                        float len = Layout == Spread.Walls ? roomSize.magnitude : Length;
                        var to = p - from;
                        float along = Vector2.Dot(to, dir);
                        if (along < 0f || along > len) continue;
                        var side = new Vector2(-dir.y, dir.x);
                        if (Mathf.Abs(Vector2.Dot(to, side)) <= Width * 0.5f) return true;
                    }
                    return false;
                }

                case Kind.Disc:
                {
                    float rr = Radius * Radius;
                    for (int i = 0; i < Repeats; i++)
                        if ((p - CenterOf(i, roomSize)).sqrMagnitude <= rr) return true;
                    return false;
                }

                case Kind.Ring:
                {
                    var to = p - Origin;
                    float d = to.magnitude;
                    if (d < Inner || d > Radius) return false;
                    if (GapDegrees <= 0f) return true;
                    // 틈에 서 있으면 안전하다. 이 한 줄이 「조임 나선」의 전부다.
                    float ang = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
                    return Mathf.Abs(Mathf.DeltaAngle(ang, GapCenterDeg)) > GapDegrees * 0.5f;
                }

                case Kind.Lanes:
                {
                    if (Lanes <= 0) return false;
                    float w = roomSize.x / Lanes;
                    int i = Mathf.Clamp(Mathf.FloorToInt(p.x / w), 0, Lanes - 1);
                    return (LaneMask & (1 << i)) != 0;
                }

                case Kind.Quads:
                {
                    // 방을 넷으로 나눈다. y 는 아래로 음수라 위/아래가 뒤집혀 보이지만
                    // 분면 번호만 일관되면 그린 것과 같은 자리다.
                    int q = (p.x >= roomSize.x * 0.5f ? 1 : 0)
                          + (p.y <= -roomSize.y * 0.5f ? 2 : 0);
                    return (LaneMask & (1 << q)) != 0;
                }

                case Kind.Outside:
                    // 섬 **밖**이 전부 위험하다. 「최종 용해」가 이것이다.
                    return (p - Origin).sqrMagnitude > Radius * Radius;

                default:
                    return false;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  그리기 — 같은 필드로 윤곽을 낸다
        // ═══════════════════════════════════════════════════════════

        /// <summary>부채꼴·고리를 몇 조각으로 나눠 그리는가. 촘촘할수록 곡선이 매끄럽다.</summary>
        private const int ArcSegments = 24;

        /// <summary>
        /// 이 도형을 채우는 삼각형을 <paramref name="verts"/>·<paramref name="tris"/> 에 쌓는다.
        /// 좌표는 방 픽셀 그대로다 — 부르는 쪽이 RectTransform 에 얹는다.
        /// </summary>
        public void Outline(System.Collections.Generic.List<Vector2> verts,
                            System.Collections.Generic.List<int> tris,
                            Vector2 roomSize)
        {
            switch (Shape)
            {
                case Kind.Wedge: AddWedge(verts, tris, Origin, Dir, Degrees, Radius, 0f); break;

                case Kind.Disc:
                    // ⚠ `Contains` 와 같은 `CenterOf` 를 돈다. 자리를 여기서 따로 구하지 마라.
                    for (int i = 0; i < Repeats; i++)
                        AddWedge(verts, tris, CenterOf(i, roomSize), Vector2.right, 360f, Radius, 0f);
                    break;

                case Kind.Band:
                {
                    for (int i = 0; i < Repeats; i++)
                    {
                        BandOf(i, roomSize, out var from, out var dir);
                        float len = Layout == Spread.Walls ? roomSize.magnitude : Length;
                        var side = new Vector2(-dir.y, dir.x) * (Width * 0.5f);
                        var far = from + dir * len;
                        Quad(verts, tris, from - side, from + side, far + side, far - side);
                    }
                    break;
                }

                case Kind.Ring:
                {
                    // 틈을 뺀 나머지를 부채꼴 하나로 그린다 — 틈이 곧 안전지대다.
                    float span = 360f - GapDegrees;
                    var dir = Rotate(Vector2.right, GapCenterDeg + 180f);
                    AddWedge(verts, tris, Origin, dir, span, Radius, Inner);
                    break;
                }

                case Kind.Lanes:
                {
                    if (Lanes <= 0) break;
                    float w = roomSize.x / Lanes;
                    for (int i = 0; i < Lanes; i++)
                    {
                        if ((LaneMask & (1 << i)) == 0) continue;
                        Quad(verts, tris,
                             new Vector2(w * i, 0f), new Vector2(w * (i + 1), 0f),
                             new Vector2(w * (i + 1), -roomSize.y), new Vector2(w * i, -roomSize.y));
                    }
                    break;
                }

                case Kind.Quads:
                {
                    float hx = roomSize.x * 0.5f, hy = roomSize.y * 0.5f;
                    for (int q = 0; q < 4; q++)
                    {
                        if ((LaneMask & (1 << q)) == 0) continue;
                        float x0 = (q & 1) == 0 ? 0f : hx;
                        float y0 = (q & 2) == 0 ? 0f : -hy;
                        Quad(verts, tris,
                             new Vector2(x0, y0), new Vector2(x0 + hx, y0),
                             new Vector2(x0 + hx, y0 - hy), new Vector2(x0, y0 - hy));
                    }
                    break;
                }

                case Kind.Outside:
                {
                    // 방 전체에서 섬을 도려낸다. 도려낸 자리를 사각형 넷으로 두르는 대신
                    // 섬 둘레를 따라 링을 그린다 — 방 밖까지 넉넉히 덮는다.
                    float outer = roomSize.magnitude;
                    AddWedge(verts, tris, Origin, Vector2.right, 360f, outer, Radius);
                    break;
                }
            }
        }

        // ── 삼각형 쌓기 ──────────────────────────────────────────

        /// <summary>
        /// 부채꼴(또는 고리 조각). <paramref name="inner"/> 가 0 보다 크면 가운데가 뚫린다.
        /// </summary>
        private static void AddWedge(System.Collections.Generic.List<Vector2> verts,
                                     System.Collections.Generic.List<int> tris,
                                     Vector2 at, Vector2 dir, float degrees,
                                     float radius, float inner)
        {
            if (radius <= 0f) return;
            degrees = Mathf.Clamp(degrees, 0f, 360f);
            int seg = Mathf.Max(3, Mathf.CeilToInt(ArcSegments * degrees / 360f));
            float start = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - degrees * 0.5f;

            // ⚠ **다각형이 원을 감싸게 밀어낸다.**
            //
            // 원을 조각으로 그리면 현이 호보다 안쪽으로 들어가, 그 사이에 선 점이
            // **판정에는 맞고 화면에는 안 그려진 자리**가 된다 — 정확히 "피했는데 맞았다" 다.
            //   `1 / cos(조각 반각)` 만큼 꼭짓점을 밀면 다각형이 원을 완전히 덮는다.
            //
            // 반대로 틀리는 것(그려졌는데 안 맞음)은 괜찮다. 빨간 데 서 있었는데
            // 안 맞는 것은 이득이지 배신이 아니다. **틀릴 거면 이쪽으로 틀려야 한다.**
            float half = degrees * Mathf.Deg2Rad / seg * 0.5f;
            float bulge = 1f / Mathf.Max(0.5f, Mathf.Cos(half));
            radius *= bulge;
            // 고리의 안쪽 테두리는 반대로 **당긴다** — 뚫린 구멍이 작아져야 덮는 쪽이 는다.
            if (inner > 0f) inner /= bulge;

            if (inner <= 0f)
            {
                int c = verts.Count;
                verts.Add(at);
                for (int i = 0; i <= seg; i++)
                {
                    float a = (start + degrees * i / seg) * Mathf.Deg2Rad;
                    verts.Add(at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
                }
                for (int i = 0; i < seg; i++) { tris.Add(c); tris.Add(c + 1 + i); tris.Add(c + 2 + i); }
                return;
            }

            // 고리 — 안쪽 테두리와 바깥 테두리를 잇는 띠
            int b = verts.Count;
            for (int i = 0; i <= seg; i++)
            {
                float a = (start + degrees * i / seg) * Mathf.Deg2Rad;
                var u = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                verts.Add(at + u * inner);
                verts.Add(at + u * radius);
            }
            for (int i = 0; i < seg; i++)
            {
                int k = b + i * 2;
                tris.Add(k); tris.Add(k + 1); tris.Add(k + 3);
                tris.Add(k); tris.Add(k + 3); tris.Add(k + 2);
            }
        }

        private static void Quad(System.Collections.Generic.List<Vector2> verts,
                                 System.Collections.Generic.List<int> tris,
                                 Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }

        private static Vector2 Rotate(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        // ═══════════════════════════════════════════════════════════
        //  데이터 → 도형
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 패턴 하나를 이 방의 도형으로 바꾼다.
        ///
        /// **24개 패턴에 24개 가지**다. 예전에는 도형 이름 20개를 24패턴이 나눠 써서
        /// 어느 보스 것인지 알 수 없었는데, 이제 한 가지가 한 패턴이다 —
        /// 그래서 여기서 그 패턴만의 매개변수를 정확히 넣을 수 있다.
        ///
        /// ⚠ 여기서 만든 도형이 **그리기와 판정 양쪽에 그대로** 쓰인다.
        ///   "예고보다 조금 크게" 같은 보정을 여기 넣지 마라 — 넣는 순간 둘이 갈라진다.
        /// </summary>
        public static DangerShape From(BossMove m, Vector2 bossAt, Vector2 dir,
                                       Vector2 playerAt, Vector2 roomSize, float px, int tick)
        {
            if (m == null) return default;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.down;
            dir = dir.normalized;

            var s = new DangerShape { Origin = bossAt, Dir = dir, Tick = tick };
            float R = m.RadiusMeters * px;      // 반경
            float W = m.WidthMeters * px;       // 폭
            float L = m.LengthMeters * px;      // 길이
            float toPlayer = (playerAt - bossAt).magnitude;

            switch (m.Draw)
            {
                // ═══ B01 크러셔 ═══════════════════════════════════
                // 아치형 입이 자기 앞을 내려찍는다. 뒤로 돌면 안 닿는다.
                case BossDraw.Crush:
                case BossDraw.ShieldUp:
                    s.Shape = Kind.Wedge;
                    s.Degrees = m.Degrees > 0f ? m.Degrees : 180f;
                    s.Radius = Mathf.Max(1f, R);
                    break;

                // 파괴구가 원 궤도를 돈다. **안쪽이 안전하다** — 파고들어야 산다.
                case BossDraw.WreckingBall:
                    s.Shape = Kind.Ring;
                    s.Radius = Mathf.Max(1f, R);
                    s.Inner = Mathf.Max(0f, m.InnerRadiusMeters * px);
                    s.GapDegrees = 0f;          // 틈 없이 한 바퀴 — 빠질 곳은 안쪽뿐이다
                    break;

                // 바닥 세 줄 중 둘이 흐른다. 멈춘 줄로 옮겨야 한다.
                case BossDraw.Conveyor:
                {
                    s.Shape = Kind.Lanes;
                    s.Lanes = Mathf.Max(2, m.Lanes);
                    // 멈춘 줄 하나가 돌아간다. 세 줄이면 매번 다른 줄이 안전해진다.
                    int safe = tick % s.Lanes;
                    s.LaneMask = ((1 << s.Lanes) - 1) & ~(1 << safe);
                    break;
                }

                // ═══ B02 가디언 ═══════════════════════════════════
                // 몸을 늘려 찌른다. 길이가 남은 마디 수를 따라간다.
                case BossDraw.SegmentThrust:
                    s.Shape = Kind.Band;
                    s.Width = Mathf.Max(1f, W);
                    s.Length = Mathf.Max(1f, L);
                    break;

                // 마디 둘을 떼어 굴린다. 튕겨 다니므로 자리가 매번 다르다.
                case BossDraw.SegmentLaunch:
                    s.Shape = Kind.Disc;
                    s.Radius = Mathf.Max(1f, R);
                    s.Layout = Spread.Scatter;
                    s.Count = Mathf.Max(1, m.Lanes);
                    break;

                // 몸을 말아 원형 벽. **머리가 그 안에 있다** — 틈으로 들어가는 것이 답이다.
                case BossDraw.CoilWall:
                    s.Shape = Kind.Ring;
                    s.Radius = Mathf.Max(1f, R);
                    s.Inner = Mathf.Max(0f, m.InnerRadiusMeters * px);
                    s.GapDegrees = m.GapDegrees > 0f ? m.GapDegrees : 60f;
                    s.GapCenterDeg = tick * 70f % 360f;
                    break;

                // 머리만 길게 뻗어 문다.
                case BossDraw.HeadBite:
                    s.Shape = Kind.Band;
                    s.Width = Mathf.Max(1f, W);
                    s.Length = Mathf.Max(1f, L);
                    break;

                // ═══ B04 파이썬 ═══════════════════════════════════
                // **벽에서 나온다.** 보스 자리가 아니라 벽에서 시작하는 것이 이 보스의 전부다.
                case BossDraw.WallBurst:
                    s.Shape = Kind.Band;
                    s.Width = Mathf.Max(1f, W);
                    s.Length = Mathf.Max(1f, L);
                    s.Layout = Spread.Walls;
                    s.Count = 1;
                    break;

                // 독구름은 퍼진다. **다 퍼진 크기로 그린다** —
                // 지금 크기로 그리면 "피한 자리로 구름이 따라온다" 가 된다.
                case BossDraw.VenomCloud:
                    s.Shape = Kind.Disc;
                    s.Origin = playerAt;
                    s.Radius = Mathf.Max(1f, R);
                    break;

                // 벽에서 벽으로 몸통이 한 줄을 지나간다.
                case BossDraw.BodyCross:
                {
                    s.Shape = Kind.Lanes;
                    s.Lanes = Mathf.Max(2, m.Lanes);
                    s.LaneMask = 1 << (tick % s.Lanes);   // 한 줄만 위험하다
                    break;
                }

                // 벽 세 곳에서 동시에. 안 겹치는 자리가 하나뿐이다.
                case BossDraw.TripleBurst:
                    s.Shape = Kind.Band;
                    s.Width = Mathf.Max(1f, W);
                    s.Length = Mathf.Max(1f, L);
                    s.Layout = Spread.Walls;
                    s.Count = Mathf.Max(2, m.Lanes);
                    break;

                // ═══ B03 킹핀 ═════════════════════════════════════
                // 미사일 다섯이 부채꼴로 떨어진다. 원과 원 사이로 빠진다.
                case BossDraw.MissileSalvo:
                    s.Shape = Kind.Disc;
                    s.Radius = Mathf.Max(1f, R);
                    s.Layout = Spread.Fan;
                    s.Count = Mathf.Max(2, m.Lanes);
                    s.Degrees = 60f;
                    s.Length = Mathf.Max(px, toPlayer);   // 플레이어 거리에 흩뿌린다
                    break;

                // **지금 입고 있는 몸**에 표식. 몸을 갈아타면 표식이 버려진 몸에 남는다.
                case BossDraw.ExecutionLock:
                    s.Shape = Kind.Disc;
                    s.Origin = playerAt;
                    s.Radius = Mathf.Max(1f, R);
                    break;

                // 탈것으로 방을 가로지른다. **이때만 근접이 닿는다.**
                case BossDraw.StrafingRun:
                    s.Shape = Kind.Band;
                    s.Width = Mathf.Max(1f, W);
                    s.Length = roomSize.magnitude;
                    break;

                // 위로 사라졌다가 그림자 자리로 내려찍는다.
                case BossDraw.BoosterDrop:
                    s.Shape = Kind.Disc;
                    s.Origin = playerAt;
                    s.Radius = Mathf.Max(1f, R);
                    break;

                // ═══ B05 로봇 스네이크 ════════════════════════════
                // **덮개가 열리는 것이 곧 예고다.** 자리는 바닥에 박혀 있다.
                case BossDraw.HatchOpen:
                    s.Shape = Kind.Disc;
                    s.Radius = Mathf.Max(1f, R);
                    s.Layout = Spread.Holes;
                    s.Count = Mathf.Max(1, m.Lanes);
                    break;

                // 나온 머리가 빔을 쏜다. 조준선이 먼저 그려진다.
                case BossDraw.RailLaser:
                    s.Shape = Kind.Band;
                    s.Width = Mathf.Max(1f, W);
                    s.Length = Mathf.Max(1f, L);
                    break;

                // 천장에서 파편 다섯. 그림자 밖으로.
                case BossDraw.DebrisFall:
                    s.Shape = Kind.Disc;
                    s.Radius = Mathf.Max(1f, R);
                    s.Layout = Spread.Scatter;
                    s.Count = Mathf.Max(2, m.Lanes);
                    break;

                // 구멍 여섯 중 다섯이 솟는다. **안 솟은 하나 위가 유일한 안전지대다.**
                case BossDraw.FullEmergence:
                    s.Shape = Kind.Disc;
                    s.Radius = Mathf.Max(1f, R);
                    s.Layout = Spread.Holes;
                    s.Count = Mathf.Clamp(m.Lanes, 1, 5);   // 여섯을 다 채우면 피할 곳이 없다
                    break;

                // ═══ B06 슬러지 ═══════════════════════════════════
                // 가라앉았다 다른 자리에서 솟는다. 바닥이 부풀어 예고한다.
                case BossDraw.Emerge:
                    s.Shape = Kind.Disc;
                    s.Radius = Mathf.Max(1f, R);
                    s.Layout = Spread.Scatter;
                    s.Count = 1;
                    break;

                // 덩어리를 뱉는다. 직각으로 피한다.
                case BossDraw.Spit:
                    s.Shape = Kind.Band;
                    s.Width = Mathf.Max(1f, W);
                    s.Length = Mathf.Max(1f, L);
                    break;

                // 몸이 사라지고 **그림자 셋**만 남는다. 그림자를 보고 미리 비킨다.
                case BossDraw.CeilingCling:
                    s.Shape = Kind.Disc;
                    s.Radius = Mathf.Max(1f, R);
                    s.Layout = Spread.Scatter;
                    s.Count = Mathf.Max(2, m.Lanes);
                    break;

                // 천장 전체로 퍼진다. **깨끗한 자리 하나만 남고 그것이 옮겨 다닌다.**
                case BossDraw.CeilingSpread:
                    s.Shape = Kind.Outside;
                    s.Radius = Mathf.Max(1f, R);
                    s.Origin = IslandAt(tick, roomSize);
                    break;

                default:
                    return default;
            }
            return s;
        }

        /// <summary>안전한 섬이 도는 자리 넷. 방 한가운데를 돌되 구석까지는 안 간다.</summary>
        private static Vector2 IslandAt(int tick, Vector2 roomSize)
        {
            float x = roomSize.x * ((tick & 1) == 0 ? 0.3f : 0.7f);
            float y = -roomSize.y * ((tick & 2) == 0 ? 0.35f : 0.65f);
            return new Vector2(x, y);
        }
    }
}
