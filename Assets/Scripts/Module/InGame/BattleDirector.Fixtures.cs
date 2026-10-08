using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 기믹이 「혼자 떠서 움직인다」를 없애는 받침들(PD 피드백 2026-10-08 · 시안 `_exchange/ref/batch_1008/`).
    ///
    ///   레이저 문(LASER_H · _V)  양 끝 아크 발생기 + 바닥 레일 + 6장 전기 아크. 꺼지면 아크만 사라지고 기둥 · 레일은 남는다
    ///   레일 톱날(SLIDE_BLADE)   바닥에 박힌 6 m 레일 홈 + 양 끝 멈춤쇠 — 톱날이 레일을 타고 오간다
    ///   가로 해머(SWING_HAMMER_H) 위를 가로지르는 철골 들보 + 들보를 타는 트롤리 + 사슬 + 추 밑 그림자
    ///   낙하물(DROP_ZONE)         예고 동안 그림자가 커지고 잔해가 위에서 떨어져 깨진다 — 남은 더미는 잠깐 뒤 사라진다
    ///   도랑(CHANNEL)             냉각수가 흐르는 4장(그 무대 그림이 있을 때만)
    ///
    /// 따로 세운 그림은 `_obstacleShadows` 에 같이 넣는다 — 방을 나갈 때 `ClearObstacles` 가 함께 치운다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        // ── 레이저 문 → 전기 아크 문 ─────────────────────────────
        private sealed class ArcGate
        {
            public Image Arc, EmitterL, EmitterR;
        }

        private readonly Dictionary<Obstacle, ArcGate> _arcGates = new();
        private Sprite[] _arcFrames;
        private Sprite _arcEmitterOn, _arcEmitterOff;
        private const float ArcFps = 14f;

        // ── 가로 해머 → 들보에 매단 추 ───────────────────────────
        private sealed class Gantry
        {
            public RectTransform Trolley, Shadow;
            public RectTransform[] Links;
            public float RailY;     // 들보 높이(방 좌표)
        }

        private readonly Dictionary<Obstacle, Gantry> _gantries = new();
        private const float GantryRise = 1.7f;     // 들보가 추보다 위에 걸린 높이(m)
        private const int GantryLinks = 5;

        // ── 낙하물 ───────────────────────────────────────────────
        private sealed class Falling
        {
            public RectTransform Shadow, Debris, Rubble;
            public Vector2 At;
            public float Left, Total, RubbleLeft;
            public bool Landed;
        }

        private readonly List<Falling> _falling = new();
        private const float DropFallSeconds = 0.35f;   // 예고 끝 이만큼 동안 떨어진다
        private const float DropFallFrom = 520f;       // 화면 위쪽 어디서부터(px)
        private const float DropRubbleSeconds = 1.2f;

        private RectTransform LooseImage(RectTransform parent, string name, Sprite sprite, Vector2 size, Vector2 at)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = at;
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return rt;
        }

        /// <summary>`SetupHazard` 가 부른다. 받침을 세우면 true.</summary>
        private bool SetupFixture(Obstacle ob)
        {
            if (ob.View == null) return false;
            var rt = (RectTransform)ob.View.transform;
            float k = Meters(1f) / 72f;
            switch (ob.Kind)
            {
                case "LASER_H":
                case "LASER_V":
                {
                    _arcFrames ??= Frames4("obj_arc", 6);
                    _arcEmitterOn ??= GetSprite("obj_arc_emitter_on");
                    _arcEmitterOff ??= GetSprite("obj_arc_emitter_off");
                    if (_arcFrames == null || _arcFrames[0] == null || _arcEmitterOn == null) return false;
                    // 문은 언제나 가로로 짠다 — 세로 문은 통째로 눕힌다(판정은 `Bounds` 라 그림 회전과 무관하다)
                    bool v = ob.Kind == "LASER_V";
                    float len = v ? ob.Bounds.height : ob.Bounds.width;
                    rt.sizeDelta = new Vector2(len, Meters(1f));
                    rt.localRotation = v ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.identity;
                    if (ob.Img != null) { ob.Img.sprite = GetSprite("obj_arc_rail"); ob.Img.color = Color.white; ob.BaseColor = Color.white; }
                    // 레일을 바닥에 납작하게(본체 그림) — 크기만 줄인다
                    var rail = ChildImage(ob, "Rail", GetSprite("obj_arc_rail"), new Vector2(len, 24f * k),
                                          new Vector2(0.5f, 0.5f), new Vector2(0f, -Meters(0.28f)));
                    if (ob.Img != null) ob.Img.enabled = false;
                    var em = new Vector2(48f, 72f) * k;
                    var g = new ArcGate
                    {
                        Arc = ChildImage(ob, "Arc", _arcFrames[0], new Vector2(len - em.x * 1.2f, Meters(1f)),
                                         new Vector2(0.5f, 0.5f), Vector2.zero),
                        EmitterL = ChildImage(ob, "EmitterL", _arcEmitterOn, em, new Vector2(0.5f, 0.5f),
                                              new Vector2(-len * 0.5f + em.x * 0.5f, 0f)),
                        EmitterR = ChildImage(ob, "EmitterR", _arcEmitterOn, em, new Vector2(0.5f, 0.5f),
                                              new Vector2(len * 0.5f - em.x * 0.5f, 0f)),
                    };
                    g.EmitterR.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
                    rail.transform.SetAsFirstSibling();
                    _arcGates[ob] = g;
                    return true;
                }

                case "SLIDE_BLADE_H":
                case "SLIDE_BLADE_V":
                {
                    bool v = ob.Kind == "SLIDE_BLADE_V";
                    var bed = GetSprite(v ? "obj_rail_bed_v" : "obj_rail_bed_h");
                    if (bed == null) return false;
                    float len = Meters(SlideTravel) + Meters(1.2f);
                    var c = ob.Home.center;
                    var size = v ? new Vector2(56f * k, len) : new Vector2(len, 64f * k);
                    // ⚠ 유닛 층이 아니라 **바닥 층**이다 — 유닛 층에 두면 `SortDepth` 가 정렬 대상을 0 번부터 채워
                    //   정렬 밖의 이 그림이 맨 위로 밀려 톱날 · 캐릭터를 덮었다(PD 2026-10-08 「톱니바퀴 뎁스」)
                    var bedRt = LooseImage(_fieldLayer, $"RailBed_{ob.Kind}", bed, size, c);
                    _obstacleShadows.Add(bedRt.gameObject);
                    var stop = GetSprite("obj_rail_stop");
                    if (stop != null)
                        for (int s = -1; s <= 1; s += 2)
                        {
                            var off = (v ? Vector2.up : Vector2.right) * (len * 0.5f) * s;
                            var st = LooseImage(_fieldLayer, "RailStop", stop, new Vector2(44f, 72f) * k, c + off);
                            st.localRotation = v ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.identity;
                            if (s < 0) st.localScale = new Vector3(-1f, 1f, 1f);
                            _obstacleShadows.Add(st.gameObject);
                        }
                    return true;
                }

                case "SWING_HAMMER_H":
                {
                    var weight = GetSprite("obj_gantry_weight");
                    var railSp = GetSprite("obj_gantry_rail");
                    if (weight == null || railSp == null) return false;
                    if (ob.Img != null) { ob.Img.sprite = weight; ob.Frames = new[] { weight }; }
                    // 매달린 추의 그림자는 아래 `GantryShadow` 가 맡는다 — 바닥 네모 그림자는 치운다
                    if (ob.Shadow != null) { _obstacleShadows.Remove(ob.Shadow); Destroy(ob.Shadow); ob.Shadow = null; }
                    var c = ob.Home.center;
                    float railY = c.y + Meters(GantryRise);
                    float span = HammerTravel * _pxPerMeter + Meters(2.4f);
                    var seg = new Vector2(144f, 28f) * k;
                    int n = Mathf.CeilToInt(span / seg.x);
                    for (int i = 0; i < n; i++)
                    {
                        float x = c.x - n * seg.x * 0.5f + seg.x * (i + 0.5f);
                        var r = LooseImage(_shotLayer, "GantryRail", railSp, seg, new Vector2(x, railY));   // 위에 걸린 것 — 사람보다 위
                        _obstacleShadows.Add(r.gameObject);
                    }
                    var br = GetSprite("obj_gantry_bracket");
                    if (br != null)
                        for (int s = -1; s <= 1; s += 2)
                        {
                            var b = LooseImage(_shotLayer, "GantryBracket", br, new Vector2(54f, 72f) * k,
                                               new Vector2(c.x + s * (n * seg.x * 0.5f), railY));
                            if (s > 0) b.localScale = new Vector3(-1f, 1f, 1f);
                            _obstacleShadows.Add(b.gameObject);
                        }
                    var gn = new Gantry { RailY = railY, Links = new RectTransform[GantryLinks] };
                    var chain = GetSprite("obj_gantry_chain");
                    for (int i = 0; i < GantryLinks; i++)
                    {
                        gn.Links[i] = LooseImage(_shotLayer, "GantryChain", chain, new Vector2(12f, 24f) * k, c);
                        _obstacleShadows.Add(gn.Links[i].gameObject);
                    }
                    gn.Trolley = LooseImage(_shotLayer, "GantryTrolley", GetSprite("obj_gantry_trolley"),
                                            new Vector2(64f, 64f) * k, new Vector2(c.x, railY));
                    _obstacleShadows.Add(gn.Trolley.gameObject);
                    gn.Shadow = LooseImage(_fieldLayer, "GantryShadow", GetSprite("obj_gantry_shadow"),
                                           new Vector2(144f, 40f) * k, new Vector2(c.x, ob.Home.yMin));
                    gn.Shadow.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.55f);
                    _obstacleShadows.Add(gn.Shadow.gameObject);
                    _gantries[ob] = gn;
                    return true;
                }

                case "CHANNEL_H":
                case "CHANNEL_V":
                {
                    var f = EnvFrames(ob.Kind == "CHANNEL_H" ? "obj_channel_h" : "obj_channel_v", 4);
                    if (f[0] == null || f[1] == null) return false;
                    ob.Frames = f;
                    return true;
                }
            }
            return false;
        }

        /// <summary>레이저 문 한 프레임. 받침이 없으면 false — 예전 빛줄기 그림으로 돌아간다.</summary>
        private bool TickArcGate(Obstacle o, bool on, bool warn)
        {
            if (!_arcGates.TryGetValue(o, out var g) || g.Arc == null) return false;
            bool show = on || (warn && Mathf.Repeat(Time.time, 0.16f) < 0.08f);
            if (g.Arc.gameObject.activeSelf != show) g.Arc.gameObject.SetActive(show);
            if (show) g.Arc.sprite = _arcFrames[Mathf.FloorToInt(Time.time * ArcFps) % _arcFrames.Length];
            var em = on || warn ? _arcEmitterOn : _arcEmitterOff ?? _arcEmitterOn;
            g.EmitterL.sprite = em;
            g.EmitterR.sprite = em;
            var tint = on || warn ? Color.white : new Color(0.62f, 0.62f, 0.7f, 1f);   // 꺼진 기둥은 어둡게
            g.EmitterL.color = tint;
            g.EmitterR.color = tint;
            return true;
        }

        /// <summary>가로 해머 — 트롤리는 들보를 따라 추 위에, 사슬은 둘 사이, 그림자는 추 밑에.</summary>
        private void TickGantry(Obstacle o, RectTransform weight)
        {
            if (!_gantries.TryGetValue(o, out var g) || g.Trolley == null) return;
            var w = weight.anchoredPosition;
            g.Trolley.anchoredPosition = new Vector2(w.x, g.RailY);
            float top = w.y + weight.sizeDelta.y * 0.5f, bottom = g.RailY - g.Trolley.sizeDelta.y * 0.3f;
            for (int i = 0; i < g.Links.Length; i++)
            {
                float t = (i + 0.5f) / g.Links.Length;
                g.Links[i].anchoredPosition = new Vector2(w.x, Mathf.Lerp(bottom, top, t));
            }
            // 가운데(가장 낮을 때)가 진하고 크다 — 끝으로 갈수록 작고 옅다
            float off = Mathf.Abs(w.x - o.Home.center.x) / Mathf.Max(1f, HammerTravel * _pxPerMeter * 0.5f);
            g.Shadow.anchoredPosition = new Vector2(w.x, o.Home.yMin);
            g.Shadow.localScale = Vector3.one * Mathf.Lerp(1f, 0.72f, off);
        }

        // ── 낙하물 ───────────────────────────────────────────────

        private void SpawnFallingDebris(Vector2 at, float seconds)
        {
            var debris = GetSprite("obj_drop_debris");
            var shadow = GetSprite("obj_drop_shadow");
            if (debris == null || shadow == null || _shotLayer == null || _fieldLayer == null) return;
            float k = Meters(1f) / 72f;
            var f = new Falling
            {
                At = at, Left = seconds, Total = seconds,
                Shadow = LooseImage(_fieldLayer, "DropShadow", shadow, new Vector2(90f, 36f) * k, at),
                Debris = LooseImage(_shotLayer, "DropDebris", debris, new Vector2(108f, 126f) * k, at + Vector2.up * DropFallFrom),
            };
            f.Debris.gameObject.SetActive(false);
            _falling.Add(f);
        }

        private void TickFalling(float dt)
        {
            TickFades(dt);
            for (int i = _falling.Count - 1; i >= 0; i--)
            {
                var f = _falling[i];
                if (!f.Landed)
                {
                    f.Left -= dt;
                    float p = 1f - Mathf.Clamp01(f.Left / f.Total);
                    // 그림자 — 작고 옅게 시작해 떨어질수록 커지고 진해진다
                    f.Shadow.localScale = Vector3.one * Mathf.Lerp(0.35f, 1.05f, p);
                    f.Shadow.GetComponent<Image>().color = new Color(1f, 1f, 1f, Mathf.Lerp(0.25f, 0.7f, p));
                    if (f.Left <= DropFallSeconds)
                    {
                        if (!f.Debris.gameObject.activeSelf) f.Debris.gameObject.SetActive(true);
                        float q = 1f - Mathf.Clamp01(f.Left / DropFallSeconds);
                        f.Debris.anchoredPosition = f.At + Vector2.up * (DropFallFrom * (1f - q * q));
                        f.Debris.localRotation = Quaternion.Euler(0f, 0f, (1f - q) * 40f);
                    }
                    if (f.Left > 0f) continue;
                    // 닿았다 — 깨지며 흙먼지, 낮은 잔해 더미가 잠깐 남는다
                    f.Landed = true;
                    Destroy(f.Debris.gameObject);
                    Destroy(f.Shadow.gameObject);
                    f.Debris = f.Shadow = null;
                    _pfx?.Shards(f.At, ParticleElement.Dust, 1.4f);
                    _pfx?.Puff(f.At, ParticleElement.Dust, 1.1f);
                    Shake(ShakeOnKill);
                    var rubble = GetSprite("obj_drop_rubble");
                    if (rubble != null)
                        f.Rubble = LooseImage(_fieldLayer, "DropRubble", rubble, new Vector2(120f, 72f) * (Meters(1f) / 72f), f.At);
                    f.RubbleLeft = DropRubbleSeconds;
                    continue;
                }
                f.RubbleLeft -= dt;
                if (f.Rubble != null)
                    f.Rubble.GetComponent<Image>().color = new Color(1f, 1f, 1f, Mathf.Clamp01(f.RubbleLeft / 0.4f));
                if (f.RubbleLeft > 0f) continue;
                if (f.Rubble != null) Destroy(f.Rubble.gameObject);
                _falling.RemoveAt(i);
            }
        }

        // ── 골렘 충격파 (PD 2026-10-08 「맞을 때 이펙트라 골렘이 스킬 쓰는 느낌이 전혀 아니다」) ──
        // 주먹 한 대마다 피격 불꽃이 터지던 것을 — 웅크려 빛을 모으고(0.5초) · 번쩍 · 둘레를 한꺼번에 치는 충격파로.
        private const float GolemSlamMeters = 2.0f;
        private const float GolemSlamCharge = 0.5f;
        // 평소엔 주먹(0.8초) — 몇 초마다 한 번 모아서 충격파(PD 2026-10-08 「골렘도 기본 평타가 있고 스킬을 쓰는 거지?」)
        private const float GolemSlamCooldown = 5f;
        private const float GolemSlamDamageMul = 1.6f;
        private readonly Dictionary<Unit, float> _golemSlam = new();   // 남은 쿨(+) · 모으는 중이면 음수(-충전 경과)
        private Sprite _golemBlast1, _golemBlast2;

        private sealed class Fade
        {
            public RectTransform Rt;
            public Image Img;
            public float Left, Total, S0, S1;
        }

        private readonly List<Fade> _fades = new();

        private void SpawnFade(RectTransform parent, Sprite sprite, Vector2 size, Vector2 at, float seconds, float s0, float s1)
        {
            if (sprite == null || parent == null) return;
            var rt = LooseImage(parent, "Fade", sprite, size, at);
            rt.localScale = Vector3.one * s0;
            _fades.Add(new Fade { Rt = rt, Img = rt.GetComponent<Image>(), Left = seconds, Total = seconds, S0 = s0, S1 = s1 });
        }

        private void TickFades(float dt)
        {
            for (int i = _fades.Count - 1; i >= 0; i--)
            {
                var f = _fades[i];
                f.Left -= dt;
                if (f.Rt == null || f.Left <= 0f) { if (f.Rt != null) Destroy(f.Rt.gameObject); _fades.RemoveAt(i); continue; }
                float p = 1f - f.Left / f.Total;
                f.Rt.localScale = Vector3.one * Mathf.Lerp(f.S0, f.S1, 1f - (1f - p) * (1f - p));
                f.Img.color = new Color(1f, 1f, 1f, Mathf.Clamp01((1f - p) * 1.6f));
            }
        }

        /// <summary>
        /// 공용 충격파 — 둘레로 번지는 고리 + 돌 파편 + 낮게 남는 먼지. 골렘 충격파와 돌 고릴라 내려찍기가 같이 쓴다
        /// (PD 2026-10-08 「골렘이 터지는 게 맞을 때 이펙트라 스킬 쓰는 느낌이 전혀 아니다」 — 예전 내려찍기는 `slam` 한 장).
        /// </summary>
        private void SpawnShockwave(Vector2 at, float diameter)
        {
            float s = Mathf.Max(0.5f, diameter / 216f);
            SpawnFade(_fieldLayer, GetSprite("fx_golem_ring"), new Vector2(216f, 144f) * s, at, 0.45f, 0.45f, 1.15f);
            SpawnFade(_fieldLayer, GetSprite("fx_golem_dust"), new Vector2(216f, 108f) * s, at + new Vector2(0f, -diameter * 0.12f), 0.7f, 0.9f, 1.15f);
            _pfx?.Shards(at, ParticleElement.Dust, 1.1f);
            Shake(ShakeOnKill);
        }

        /// <summary>충격파 그림이 왔는가 — 없으면 예전 주먹으로 친다. 한 번 받은 그림은 방이 바뀔 때까지 쓴다.</summary>
        private bool GolemSlamReady()
        {
            if (_golemBlast1 == null) _golemBlast1 = UnitGet(GolemKey, "blast_1");
            if (_golemBlast2 == null) _golemBlast2 = UnitGet(GolemKey, "blast_2");
            return _golemBlast1 != null && _golemBlast2 != null;
        }

        /// <summary>
        /// 골렘 충격파 차례. 모으는 중 · 터뜨리는 프레임이면 true(평소 주먹을 건너뛴다),
        /// 쿨 중이거나 멀면 false — 그동안은 평소처럼 주먹으로 친다.
        /// </summary>
        private bool TickGolemSlam(Unit g, Unit target, float distance, float dt)
        {
            _golemSlam.TryGetValue(g, out float t);
            if (t > 0f) { _golemSlam[g] = t - dt; return false; }
            if (t == 0f && distance > Meters(GolemSlamMeters) * 0.9f) return false;

            // 모으기 — 웅크리며 빛이 가슴으로(1) → 금이 퍼지며 번쩍(2)
            t -= dt;
            float charge = -t;
            g.SetSpriteOverride(charge < GolemSlamCharge * 0.6f ? _golemBlast1 : _golemBlast2);
            g.SetState(EnemyState.Attack);
            g.SetMoving(false);
            if (charge < GolemSlamCharge) { _golemSlam[g] = t; return true; }

            g.SetSpriteOverride(null);
            _golemSlam[g] = GolemSlamCooldown;
            float r = Meters(GolemSlamMeters);
            int dmg = Mathf.Max(1, Mathf.RoundToInt(g.Atk * GolemSlamDamageMul));
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying) continue;
                if ((e.Position - g.Position).sqrMagnitude > r * r) continue;
                HitEnemyWith(e, dmg, g.Profile);
            }
            SpawnShockwave(g.Position, r * 2f);
            return true;
        }

        private void ClearFixtures()
        {
            _arcGates.Clear();
            _gantries.Clear();
            for (int i = 0; i < _falling.Count; i++)
            {
                var f = _falling[i];
                if (f.Shadow != null) Destroy(f.Shadow.gameObject);
                if (f.Debris != null) Destroy(f.Debris.gameObject);
                if (f.Rubble != null) Destroy(f.Rubble.gameObject);
            }
            _falling.Clear();
            for (int i = 0; i < _fades.Count; i++) if (_fades[i].Rt != null) Destroy(_fades[i].Rt.gameObject);
            _fades.Clear();
            _golemSlam.Clear();
            _golemBlast1 = _golemBlast2 = null;   // 아틀라스는 판마다 다시 올라온다
        }
    }
}
