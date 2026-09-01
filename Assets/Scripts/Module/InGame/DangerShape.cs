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

        public Kind Shape;
        public Vector2 Origin;      // 중심(px). Band 는 시작점
        public Vector2 Dir;         // 바라보는 방향(정규화). Wedge·Band 가 쓴다
        public float Degrees;       // Wedge 의 벌어진 각
        public float Radius;        // Wedge·Disc·Ring 바깥 · Outside 안전 반경(px)
        public float Inner;         // Ring 안쪽 반경(px)
        public float Width, Length; // Band(px)
        public float GapDegrees;    // Ring 의 틈
        public float GapCenterDeg;  // 틈이 지금 어디에 있는가(도). 돌아간다
        public int Lanes;           // Lanes 의 줄 수 · Quads 는 4 고정
        public int LaneMask;        // 위험한 줄/분면의 비트

        public bool IsNone => Shape == Kind.None;

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
                    var to = p - Origin;
                    float along = Vector2.Dot(to, Dir);
                    if (along < 0f || along > Length) return false;
                    var side = new Vector2(-Dir.y, Dir.x);
                    return Mathf.Abs(Vector2.Dot(to, side)) <= Width * 0.5f;
                }

                case Kind.Disc:
                    return (p - Origin).sqrMagnitude <= Radius * Radius;

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
                case Kind.Disc:  AddWedge(verts, tris, Origin, Vector2.right, 360f, Radius, 0f); break;

                case Kind.Band:
                {
                    var side = new Vector2(-Dir.y, Dir.x) * (Width * 0.5f);
                    var far = Origin + Dir * Length;
                    Quad(verts, tris, Origin - side, Origin + side, far + side, far - side);
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
        /// `BossDraw` 20종이 여기서 위 8가지 도형으로 접힌다 — 24개 패턴이
        /// 서로 다르게 **보이는** 것은 매개변수가 다르기 때문이지 도형이 20가지라서가 아니다.
        /// </summary>
        public static DangerShape From(BossMove m, Vector2 bossAt, Vector2 dir,
                                       Vector2 playerAt, Vector2 roomSize, float px, int tick)
        {
            if (m == null) return default;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.down;
            dir = dir.normalized;

            var s = new DangerShape { Origin = bossAt, Dir = dir };

            switch (m.Draw)
            {
                // ── 부채꼴 ────────────────────────────────────────
                case BossDraw.Arc:
                case BossDraw.Fan:
                    s.Shape = Kind.Wedge;
                    s.Degrees = m.Degrees > 0f ? m.Degrees : 90f;
                    s.Radius = Mathf.Max(1f, m.RadiusMeters * px);
                    break;

                // 왼쪽 반원 → 오른쪽 반원. 어느 쪽 차례인지는 `tick` 이 정한다.
                case BossDraw.Halves:
                    s.Shape = Kind.Wedge;
                    s.Degrees = 180f;
                    s.Radius = Mathf.Max(1f, m.RadiusMeters * px);
                    s.Dir = Rotate(dir, (tick & 1) == 0 ? 90f : -90f);
                    break;

                // ── 직선 ──────────────────────────────────────────
                case BossDraw.Line:
                case BossDraw.Sweep:
                case BossDraw.Cable:
                case BossDraw.Homing:
                case BossDraw.CrossLine:
                case BossDraw.Burst:
                    s.Shape = Kind.Band;
                    s.Width = Mathf.Max(1f, m.WidthMeters * px);
                    s.Length = Mathf.Max(1f, m.LengthMeters * px);
                    break;

                // ── 돌진 — 띠지만 플레이어 쪽으로 방을 가로지른다 ──
                case BossDraw.Dash:
                case BossDraw.Shed:
                    s.Shape = Kind.Band;
                    s.Width = Mathf.Max(1f, m.WidthMeters * px);
                    s.Length = roomSize.magnitude;
                    break;

                // ── 표식 — 지금 있는 자리를 찍는다 ─────────────────
                case BossDraw.Mark:
                    s.Shape = Kind.Disc;
                    s.Origin = playerAt;
                    s.Radius = Mathf.Max(1f, m.RadiusMeters * px);
                    break;

                // ── 고리 ──────────────────────────────────────────
                case BossDraw.Ring:
                    s.Shape = Kind.Ring;
                    s.Radius = Mathf.Max(1f, m.RadiusMeters * px);
                    s.Inner = Mathf.Max(0f, m.InnerRadiusMeters * px);
                    s.GapDegrees = m.GapDegrees > 0f ? m.GapDegrees : 50f;
                    // 틈이 시계방향으로 돈다 — 쓸 때마다 자리가 바뀐다
                    s.GapCenterDeg = tick * 70f % 360f;
                    break;

                // ── 자리에 남는 웅덩이 ────────────────────────────
                case BossDraw.Trail:
                case BossDraw.Split:
                    s.Shape = Kind.Disc;
                    s.Radius = Mathf.Max(1f, m.RadiusMeters * px);
                    break;

                // ── 줄 ────────────────────────────────────────────
                case BossDraw.Lane:
                {
                    s.Shape = Kind.Lanes;
                    s.Lanes = Mathf.Max(2, m.Lanes);
                    // 번갈아 — 홀수 줄과 짝수 줄이 교대로 위험해진다
                    int mask = 0;
                    for (int i = 0; i < s.Lanes; i++)
                        if ((i & 1) == (tick & 1)) mask |= 1 << i;
                    s.LaneMask = mask;
                    break;
                }

                // ── 사분면 ────────────────────────────────────────
                case BossDraw.Quad:
                case BossDraw.Cover:
                    s.Shape = Kind.Quads;
                    // 안전한 분면 하나만 빼고 전부 위험하다. 그 하나가 돌아간다.
                    s.LaneMask = 0b1111 & ~(1 << (tick & 3));
                    break;

                // ── 섬 하나만 안전 ────────────────────────────────
                case BossDraw.Island:
                case BossDraw.Overload:
                    s.Shape = Kind.Outside;
                    s.Radius = Mathf.Max(1f, m.RadiusMeters * px);
                    // 섬이 2초마다 자리를 옮긴다 — 방 안을 도는 네 자리
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
