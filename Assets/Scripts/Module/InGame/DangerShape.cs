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
            /// <summary>
            /// **나를 중심으로** 흩어진다. <see cref="Origin"/> 이 내 자리이고
            /// <see cref="Length"/> 가 흩뿌리는 반경이다.
            ///
            /// ⚠ `Scatter` 는 방 아무 데나 떨어진다 — 나를 겨누지 않으므로
            ///   "이상한 데다 쏜다" 가 된다. 던지는 물건은 나를 향해야 의미가 있다.
            /// </summary>
            NearTarget,
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

        /// <summary>
        /// <see cref="Spread.NearTarget"/> 이 조각을 떨구지 <b>않는</b> 안쪽 구멍.
        /// <see cref="Length"/> 에 대한 비율이고, 0 이면 내 발밑까지 떨어진다.
        ///
        /// ⚠ 퍼짐(<see cref="Length"/>)만 넓히면 이 구멍도 같이 커져서
        ///   <b>가만히 서 있으면 안 맞는 도넛</b>이 된다 — 넓힐 때는 여기를 같이 낮춘다.
        ///   기본값 0.35 는 「마디 사출」·「벽돌 낙하」가 쓰던 값이다. 건드리지 않는다.
        /// </summary>
        public float SpreadInner;
        public int Tick;            // 흩뿌림·구멍 고르기의 씨앗. 쓸 때마다 자리가 바뀐다

        public bool IsNone => Shape == Kind.None;

        /// <summary>실제로 그릴 도형 수. 배치가 없으면 언제나 하나다.</summary>
        private int Repeats => Layout == Spread.None || Count <= 1 ? 1 : Count;

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

                case Spread.NearTarget:
                {
                    // 나를 가운데 두고 반경 `Length` 안에 흩뿌린다. 결정적이라
                    // 그릴 때와 때릴 때 자리가 같다.
                    int h = Hash(Tick * 31 + i);
                    float ang = (h & 0xFFFF) / 65535f * 360f * Mathf.Deg2Rad;
                    float rad = Mathf.Lerp(SpreadInner, 1f, ((h >> 16) & 0xFFFF) / 65535f) * Length;
                    var at = Origin + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rad;
                    // 방 밖으로 나가면 피할 자리가 사라진다. 안쪽으로 밀어 넣는다.
                    return new Vector2(Mathf.Clamp(at.x, Radius, roomSize.x - Radius),
                                       Mathf.Clamp(at.y, -roomSize.y + Radius, -Radius));
                }

                default:
                    return Origin;
            }
        }

        /// <summary>
        /// 터진 자리 — 표시(임팩트)를 남길 곳.
        ///
        /// ⚠ <see cref="Origin"/> 을 쓰면 안 된다. 흩뿌리는 도형에서 `Origin` 은
        ///   **보스 자리**이고 실제 도형은 저 멀리 있다 — 미사일이 내 발밑에 떨어졌는데
        ///   폭발은 보스 몸에서 터졌다. 화면에서는 "예고만 하고 아무 일도 안 났다" 가 된다.
        /// </summary>
        /// <summary>이 예고에 들어 있는 도형의 개수. 미사일 세 발이면 3 이다.</summary>
        public int PieceCount => Repeats;

        /// <summary>
        /// i번째 도형의 중심. **날아오는 것을 그 자리로 보내려고** 밖에서 묻는다 —
        /// 탄이 도착하는 자리와 터지는 자리가 같아야 한다(<see cref="CenterOf"/> 하나만 본다).
        /// </summary>
        public Vector2 PieceAt(int i, Vector2 roomSize) => CenterOf(i, roomSize);

        public Vector2 ImpactAt(Vector2 roomSize)
        {
            if (Shape != Kind.Band) return CenterOf(0, roomSize);
            BandOf(0, roomSize, out var from, out var dir);
            return from + dir * (Length * 0.5f);
        }

        /// <summary>i번째 띠의 시작점과 방향. 지금은 띠를 여럿 쓰는 패턴이 없다.</summary>
        private void BandOf(int i, Vector2 roomSize, out Vector2 from, out Vector2 dir)
        {
            from = Origin; dir = Dir;
        }

        // ⚠ 아치 네 곳은 **벽 그림(720×144)에 픽셀로 박힌 자리**다.
        //   구멍 x 30~150 · 210~330 · 390~510 · 570~690 → 가운데 90 · 270 · 450 · 630.
        //   여기서 딴 값을 쓰면 머리가 벽을 뚫고 나온다.
        //   `BattleDirector.PythonStage` 도 이 함수를 본다 — 표는 하나뿐이다.
        private const float WallArtWidth = 720f;
        private static readonly float[] ArchCenterPx = { 90f, 270f, 450f, 630f };

        /// <summary>아치 개수.</summary>
        public static int ArchCount => ArchCenterPx.Length;

        /// <summary>i번째 아치의 입구 자리(방 좌표). 벽 아래쪽 끝에서 시작한다.</summary>
        public static Vector2 ArchAtRoom(int i, Vector2 roomSize)
        {
            int k = ((i % ArchCenterPx.Length) + ArchCenterPx.Length) % ArchCenterPx.Length;
            return new Vector2(roomSize.x * (ArchCenterPx[k] / WallArtWidth), -WallBandDepth(roomSize));
        }

        /// <summary>벽 띠의 두께(px). 그림이 720×144 이므로 방 폭의 1/5 이다.</summary>
        public static float WallBandDepth(Vector2 roomSize) => roomSize.x * (144f / WallArtWidth);

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
                        var to = p - from;
                        float along = Vector2.Dot(to, dir);
                        if (along < 0f || along > Length) continue;
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
                        var side = new Vector2(-dir.y, dir.x) * (Width * 0.5f);
                        var far = from + dir * Length;
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
        /// <summary>
        /// 표에 적힌 반경·폭·길이에 곱하는 값.
        ///
        /// 정본 값이 화면에서 너무 컸다(기획 2026-09-07 — "범위를 다들 2/3 으로
        /// 줄여줘 지금 범위가 너무 커"). 표를 24개 고치는 대신 여기서 한 번 곱한다 —
        /// 되돌릴 때도 이 숫자 하나만 1 로 놓으면 된다.
        /// </summary>
        public const float DangerScale = 2f / 3f;

        /// <summary><see cref="SpreadInner"/> 의 기본값. 「마디 사출」·「벽돌 낙하」가 쓰던 값이다.</summary>
        public const float DefaultSpreadInner = 0.35f;

        public static DangerShape From(BossMove m, Vector2 bossAt, Vector2 dir,
                                       Vector2 playerAt, Vector2 roomSize, float px, int tick)
        {
            if (m == null) return default;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.down;
            dir = dir.normalized;

            var s = new DangerShape
            {
                Origin = bossAt, Dir = dir, Tick = tick,
                SpreadInner = DefaultSpreadInner,
            };
            // ⚠ 표 값을 **여기 한 곳에서만** 줄인다. 패턴마다 흩어 놓으면
            //   나중에 되돌릴 때 빠뜨리는 자리가 생긴다.
            //   방 크기에서 나오는 값(머리 뻗기가 바닥까지, 몸통 밀기가 방 폭)은
            //   여기를 안 거친다 — 그것은 크기가 아니라 **구조**라 줄이면 안 된다.
            float R = m.RadiusMeters * px * DangerScale;      // 반경
            float W = m.WidthMeters * px * DangerScale;       // 폭
            float L = m.LengthMeters * px * DangerScale;      // 길이
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
                    s.Inner = Mathf.Max(0f, m.InnerRadiusMeters * px * DangerScale);
                    s.GapDegrees = 0f;          // 틈 없이 한 바퀴 — 빠질 곳은 안쪽뿐이다
                    break;

                // 나에게 줄을 긋고 그 줄을 타고 밀고 들어온다.
                // ⚠ 길이는 **적어도 나까지**다. 표에 적힌 길이가 짧으면 줄이 나에게
                //   닿기도 전에 끊겨, 보스가 줄 밖으로 튀어나오는 것처럼 보인다.
                case BossDraw.RamCharge:
                    s.Shape = Kind.Band;
                    s.Width = Mathf.Max(1f, W);
                    s.Length = Mathf.Max(Mathf.Max(px, L), toPlayer + px);
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

                // 마디 둘을 떼어 **나에게** 굴린다.
                //
                // ⚠ 예전에는 `Scatter`(방 아무 데나)였다. 보스와도 나와도 상관없는
                //   자리에 떨어져서 "이상한 데다 쏜다" 가 됐다. 떼어 굴리는 물건은
                //   던지는 대상이 있어야 한다 — 내 자리를 중심으로 2.5 m 안에 뿌린다.
                case BossDraw.SegmentLaunch:
                    s.Shape = Kind.Disc;
                    s.Radius = Mathf.Max(1f, R);
                    s.Origin = playerAt;
                    s.Layout = Spread.NearTarget;
                    // ⚠ 착탄점이 **내 자리 둘레 이만큼 안에** 흩어진다.
                    //   흩어지는 폭이 착탄 원보다 훨씬 크면, 원을 아무리 키워도
                    //   흩어진 만큼 그냥 빗나간다 — 쿨을 줄여 더 자주 쏴도 소용없다.
                    //   2.5 → 2 m 로 좁혔다(기획 2026-09-03). 원(1.35 m)을 더 키우면
                    //   피할 틈이 같이 사라지므로, 폭을 좁혀 명중률만 올린다.
                    s.Length = 2f * px;
                    s.Count = Mathf.Max(1, m.Lanes);
                    break;

                // 몸을 말아 제 둘레를 통째로 짓누른다.
                //
                // ⚠ 예전에는 **틈 있는 도넛**이었다(안쪽 안전 · 틈으로 파고들기).
                //   화면에서는 "원 가운데가 비어 있다" 로 읽혀서 무엇을 하라는 건지
                //   알 수 없었다. 기획 2026-09-03 — **틈도 구멍도 없는 꽉 찬 원**이다.
                //   바깥으로 나가는 것이 답이다.
                case BossDraw.CoilWall:
                    s.Shape = Kind.Disc;
                    s.Radius = Mathf.Max(1f, R);
                    break;

                // **제자리에서** 앞을 물어뜯는다.
                //
                // ⚠ 달려가는 쪽은 「마디 돌진」이 가져갔다(기획 2026-09-03).
                //   둘 다 달려가면 구분이 안 된다 — 하나는 날아와 박고, 하나는
                //   제자리에서 문다.
                case BossDraw.HeadBite:
                    s.Shape = Kind.Wedge;
                    s.Degrees = m.Degrees > 0f ? m.Degrees : 120f;
                    s.Radius = Mathf.Max(1f, R);
                    break;


                // ═══ B04 파이썬 ═══════════════════════════════════
                // **나온 구멍에서 시작한다.** 보스 자리가 곧 그 구멍이다
                // (`BattleDirector.PythonStage` 가 머리를 아치에 세워 둔다).

                // 목을 곧장 아래로 뻗는다. 좌우로 비키면 지나간다.
                //
                // ⚠ 길이를 표 값(5 m)으로 끊었더니 **방 한가운데서 멈췄다.**
                //   아래쪽에 서 있으면 아예 닿지도 않아 피할 이유가 없다.
                //   벽 아래 끝에서 **방 바닥까지** 간다 — 그래야 좌우로 비키는 것이
                //   유일한 답이 된다.
                case BossDraw.HeadLunge:
                {
                    float band = WallBandDepth(roomSize);
                    s.Shape = Kind.Band;
                    s.Origin = new Vector2(bossAt.x, -band);
                    s.Dir = Vector2.down;
                    s.Width = Mathf.Max(1f, W);
                    s.Length = Mathf.Max(Mathf.Max(1f, L), roomSize.y - band);
                    break;
                }

                // 독은 **내가 선 자리**에 떨어진다. 웅덩이가 남으므로
                // 다 퍼진 크기로 그려야 "피한 자리로 구름이 따라온다" 가 안 된다.
                case BossDraw.VenomCloud:
                    s.Shape = Kind.Disc;
                    s.Origin = playerAt;
                    s.Radius = Mathf.Max(1f, R);
                    break;

                // 벽이 부서져 **내 둘레** 세 곳에 떨어진다.
                //
                // ⚠ 예전에는 방 아무 데나(`Scatter`) 떨어뜨렸다. 내가 없는 데서
                //   벽돌이 터지니 "어디다 쏘는 거지" 가 됐다 — 던지는 것은 나를
                //   향해야 의미가 있다.
                case BossDraw.BrickFall:
                    s.Shape = Kind.Disc;
                    s.Radius = Mathf.Max(1f, R);
                    s.Origin = playerAt;
                    s.Layout = Spread.NearTarget;
                    // ⚠ 2.2 배로 뿌렸더니 내 둘레라기엔 너무 헐거웠다.
                    //   반경 하나 남짓 안에 떨어져야 "나를 노렸다" 로 읽힌다.
                    s.Length = Mathf.Max(1f, R) * 1.1f;
                    s.Count = Mathf.Max(1, m.Lanes);
                    break;

                // 벽 전체가 방 안으로 밀고 들어온다. **아래로 내려가는 것 말고는 없다.**
                // 머리가 앞장서고 몸이 따라 **방을 가로지른다.** 가로 한 줄이다.
                //
                // ⚠ 두 공격의 축이 갈려야 한다 — 「머리 뻗기」가 세로(내 x 줄)이므로
                //   이쪽은 가로(내 y 줄)다. 하나는 좌우로, 하나는 위아래로 피하게 되어
                //   서로 다른 문제가 된다.
                //   한때 방 폭을 통째로 덮었고(피할 데 없음), 한때 세로로 만들었다
                //   (머리 뻗기와 같은 축이라 구분이 안 됨). 둘 다 아니다.
                case BossDraw.BodyShove:
                {
                    float lane = Mathf.Max(1f, W);
                    s.Shape = Kind.Band;
                    s.Origin = new Vector2(0f, playerAt.y);
                    s.Dir = Vector2.right;
                    s.Width = lane;
                    s.Length = roomSize.x;
                    break;
                }

                // ═══ B03 킹핀 ═════════════════════════════════════
                // 미사일 다섯이 부채꼴로 떨어진다. 원과 원 사이로 빠진다.
                case BossDraw.MissileSalvo:
                    s.Shape = Kind.Disc;
                    s.Radius = Mathf.Max(1f, R);
                    s.Layout = Spread.Fan;
                    // ⚠ 하나도 허용한다. 예전 하한 2 는 「미사일 한 발」을 적을 수 없게 했다 —
                    //   1 을 넣어도 2발이 떨어졌다. 하나면 부채꼴 한가운데, 곧 내 자리다.
                    s.Count = Mathf.Max(1, m.Lanes);
                    // ⚠ 60° 로 뒀더니 다섯 원(반경 1.2 m = 86 px)이 서로 겹쳐
                    //   한 덩어리로 보였다 — 사이로 빠져나갈 틈이 없다.
                    s.Degrees = 90f;
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
                // **내가 선 자리를 뚫고 나온다.** 바닥에 구멍이 생기는 것이 예고다.
                //
                // ⚠ 전에는 바닥에 박힌 구멍 6개 중 몇을 골랐다. 그러면 내가 어디 있든
                //   상관이 없어서 「저기서 뭔가 열리네」로 끝났다 —
                //   버렸다(기획 2026-09-07). 나를 쫓아와야 비켜설 이유가 생긴다.
                case BossDraw.BurrowStrike:
                    s.Shape = Kind.Disc;
                    s.Origin = playerAt;
                    s.Radius = Mathf.Max(1f, R);
                    break;

                // 나온 머리가 빔을 쏜다. 조준선이 먼저 그려진다.
                //
                // ⚠ 길이는 **방 끝까지**다(정본). 표 값을 그대로 쓰면 범위 축소(2/3)에
                //   같이 걸려 방 한복판에서 끊긴다 — 실측 624px, 방 대각선은 1181px.
                //   이건 크기가 아니라 구조다.
                case BossDraw.RailLaser:
                    s.Shape = Kind.Band;
                    s.Width = Mathf.Max(1f, W);
                    s.Length = roomSize.magnitude;
                    // 겨누는 곳은 **내가 선 자리**다. `dir` 이 이미 보스→나 방향이므로
                    //   기본값(`s.Dir = dir`)을 그대로 쓴다.
                    //
                    // ⚠ 전에는 위·아래·좌·우 중 가까운 쪽으로 **꺾어 붙였다.** 그러면
                    //   내가 비스듬히 서 있을 때 빔이 나를 비껴 지나간다 — 조준하는
                    //   기술이 조준을 안 하는 꼴이 됐다(기획 2026-09-07). 버렸다.
                    break;

                // 천장에서 파편 다섯. 그림자 밖으로.
                //
                // ⚠ 예전에는 방 아무 데나(`Scatter`) 떨어뜨렸다. 내가 저 아래 있는데
                //   파편 다섯이 보스 근처에서만 터져서 "어디 이상한 데로 쏜다" 가 됐다
                //   (기획 2026-09-07). 「마디 사출」·「벽돌 낙하」와 같은 병이고
                //   같은 약을 쓴다 — **내 자리**를 중심으로 뿌린다.
                // ⚠ 퍼지는 폭은 조각 셋짜리(`BrickFall` 1.1배)보다 넓다. 다섯을
                //   같은 폭에 넣으면 통째로 겹쳐 한 덩어리가 되고, 그러면 피할 틈이
                //   아예 없어진다 — 넓혀야 사이에 설 자리가 생긴다.
                //
                // ⚠ 2배(116px)로는 아직 한 덩어리였다(실측 97%). **3.5배로 넓히되
                //   안쪽 구멍을 0.35 → 0.15 로 같이 낮춘다.** 퍼짐만 넓히면
                //   구멍(0.35×퍼짐)이 조각 반경을 넘어서서 **가만히 서 있으면
                //   안 맞는 도넛**이 된다 — 3배만 넓혀도 명중률이 95% → 0% 로
                //   떨어졌다(실측 2026-09-07). 둘은 같이 움직여야 한다.
                case BossDraw.DebrisFall:
                    s.Shape = Kind.Disc;
                    s.Radius = Mathf.Max(1f, R);
                    s.Origin = playerAt;
                    s.Layout = Spread.NearTarget;
                    s.Length = Mathf.Max(1f, R) * 3.5f;
                    s.SpreadInner = 0.15f;
                    s.Count = Mathf.Max(2, m.Lanes);
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
