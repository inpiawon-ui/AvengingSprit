using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 7~10챕터 새 기믹 여섯(기획 5-2 · PD 통과 시안 `batch_mocks/`, 2026-10-07).
    ///
    ///   CONVEYOR_E · _W · _N · _S   컨베이어 바닥(7) — 위에 선 것(나 · 적)을 화살표 쪽으로 초당 1.5 m 민다. 거슬러 가면 느리다
    ///   COLLAPSE_FLOOR              무너지는 바닥(8) — 밟으면 1.5초 금이 가고 꺼져 구덩이가 된다(몸은 못 건넌다 · 탄은 넘는다)
    ///   SWITCH_PAD                  스위치 발판(8) — 밟고 있는 동안 방의 레이저 문이 꺼진다
    ///   LASER_PILLAR                회전 레이저 기둥(9) — 4 m 레이저 한 줄이 8초에 한 바퀴 돈다. 광선은 레이저 문 그림을 같이 쓴다
    ///   SHIELD_GEN                  방패 발전기(9) — 2.5 m 안의 적이 보호막(받는 피해 90% 감소). 내 탄으로 부순다
    ///   ONEWAY_GATE                 일방 문(10, 3칸) — 아래에서 위로 지나가면 뒤에서 닫힌다. 탄은 창살 사이로 지난다
    ///
    /// 바닥에 까는 것(컨베이어 · 바닥 · 발판 · 문)은 **칸을 꽉 채운다** — 다른 물건처럼 0.7배로 줄이면 띠가 끊겨 보인다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const float ConveyorMps = 1.5f;
        private const float ConveyorFps = 8f;
        private const float CollapseSeconds = 1.5f;
        private const int CollapseDamage = 6;
        private const float LaserPillarMeters = 4.0f;
        private const float LaserPillarDegPerSec = 45f;
        private const float LaserPillarWidthMeters = 0.35f;
        private const int LaserPillarDamage = 12;
        private const float LaserPillarTick = 0.6f;
        private const float ShieldGenMeters = 2.5f;
        private const int ShieldGenHp = 50;
        private const float GateCloseMeters = 0.4f;    // 문 위로 이만큼 지나야 닫는다(문에 끼지 않게)

        private bool _switchHeld;
        private bool _conveyedPlayer;
        private readonly HashSet<Unit> _shielded = new();
        private readonly HashSet<Unit> _conveyed = new();
        private readonly Dictionary<Unit, Image> _bubbles = new();
        private readonly Dictionary<Obstacle, Image> _extraImg = new();   // 레이저 광선 · 문 창살
        private readonly Dictionary<Obstacle, Image> _extraImg2 = new();  // 문 화살표 · 자물쇠
        private Sprite[] _conveyorFrames, _collapseFrames;
        private Sprite _bubbleSprite;

        private static bool IsConveyor(Obstacle o) => o.Kind != null && o.Kind.StartsWith("CONVEYOR_");

        private static Vector2 ConveyorDir(Obstacle o)
            => o.Kind.EndsWith("_W") ? Vector2.left
             : o.Kind.EndsWith("_N") ? Vector2.up
             : o.Kind.EndsWith("_S") ? Vector2.down
             : Vector2.right;

        private Sprite[] Frames4(string name, int n)
        {
            var f = new Sprite[n];
            for (int i = 0; i < n; i++) f[i] = GetSprite($"{name}_{i + 1}");
            return f;
        }

        /// <summary>바닥에 까는 것 — 칸을 꽉 채우고(0.7배 축소를 되돌린다) 그림을 바른다.</summary>
        private void FillCell(Obstacle ob, Sprite sprite, Vector2 cells)
        {
            var rt = ob.View != null ? (RectTransform)ob.View.transform : null;
            if (rt == null) return;
            var c = ob.Home.center;
            var size = new Vector2(Meters(cells.x), Meters(cells.y));
            var rect = new Rect(c.x - size.x * 0.5f, c.y - size.y * 0.5f, size.x, size.y);
            ob.Home = ob.Bounds = ob.ShotBounds = rect;
            rt.sizeDelta = new Vector2(Mathf.Round(size.x), Mathf.Round(size.y));
            rt.anchoredPosition = c;
            if (sprite != null && ob.Img != null) { ob.Img.sprite = sprite; ob.Img.color = Color.white; ob.BaseColor = Color.white; }
        }

        private Image ChildImage(Obstacle ob, string name, Sprite sprite, Vector2 size, Vector2 pivot, Vector2 local)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(ob.View.transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = pivot;
            rt.sizeDelta = new Vector2(Mathf.Round(size.x), Mathf.Round(size.y));
            rt.anchoredPosition = local;
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = sprite != null ? Color.white : new Color(0.6f, 0.6f, 0.65f, 1f);
            img.raycastTarget = false;
            return img;
        }

        /// <summary>`SetupHazard` 가 부른다. 이 파일의 종류면 true.</summary>
        private bool SetupGimmick2(Obstacle ob)
        {
            if (IsConveyor(ob))
            {
                _conveyorFrames ??= Frames4("obj_conveyor", 4);
                FillCell(ob, _conveyorFrames[0], Vector2.one);
                var d = ConveyorDir(ob);
                ob.View.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                ob.Frames = _conveyorFrames;
                return true;
            }
            switch (ob.Kind)
            {
                case "COLLAPSE_FLOOR":
                    _collapseFrames ??= Frames4("obj_collapse", 4);
                    FillCell(ob, _collapseFrames[0], Vector2.one);
                    ob.Frames = _collapseFrames;
                    ob.Hp = 0;          // 0 멀쩡 · 1 금 가는 중 · 2 꺼짐
                    ob.Timer = 0f;
                    return true;

                case "SWITCH_PAD":
                    ob.Frames = new[] { GetSprite("obj_switch_1"), GetSprite("obj_switch_2") };
                    FillCell(ob, ob.Frames[0], Vector2.one);
                    return true;

                case "LASER_PILLAR":
                {
                    // 광선은 빨간 레이저 그림(obj_laser_crack, 레이저 문과 같은 96×512)을 같이 쓴다 — 청록이면
                    // 아군 효과처럼 보였다(코덱스 검수). 기둥 발자국 가운데에서 뻗는다
                    var view = (RectTransform)ob.View.transform;
                    var local = ob.ShotBounds.center - view.anchoredPosition;
                    var beam = ChildImage(ob, "Beam", GetSprite("obj_laser_crack") ?? GetSprite("obj_laser_beam"),
                                          new Vector2(Meters(LaserPillarWidthMeters), Meters(LaserPillarMeters)),
                                          new Vector2(0.5f, 0f), local);
                    beam.color = new Color(1f, 1f, 1f, 0.95f);
                    _extraImg[ob] = beam;
                    ob.Timer = 0f;
                    return true;
                }

                case "SHIELD_GEN":
                    ob.Hp = ShieldGenHp;
                    ob.Frames = new[] { GetSprite("obj_shield_gen_1"), GetSprite("obj_shield_gen_2") };
                    return true;

                case "ONEWAY_GATE":
                {
                    FillCell(ob, GetSprite("obj_oneway_frame"), new Vector2(3f, 1f));
                    float k = Meters(1f) / 72f;
                    // 창살은 문틀 위로 솟는다(108px) · 화살표는 문턱 가운데 · 자물쇠는 창살 가운데
                    var bars = ChildImage(ob, "Bars", GetSprite("obj_oneway_bars"), new Vector2(216f, 108f) * k,
                                          new Vector2(0.5f, 0f), new Vector2(0f, -Meters(0.5f)));
                    bars.gameObject.SetActive(false);
                    _extraImg[ob] = bars;
                    var arrow = ChildImage(ob, "Arrow", GetSprite("obj_oneway_arrow"), new Vector2(108f, 108f) * k,
                                           new Vector2(0.5f, 0.5f), Vector2.zero);
                    _extraImg2[ob] = arrow;
                    ob.Hp = 0;           // 0 열림 · 1 닫힘
                    ob.Telegraph = false; // 문 아래에 선 적이 있는가
                    return true;
                }
            }
            return false;
        }

        // ═══════════════════════════════════════════════════════════
        //  한 프레임 — `TickHazards2` 가 물건마다 부른다. 이 파일의 종류면 true.
        // ═══════════════════════════════════════════════════════════

        private bool TickGimmick2(Obstacle o, Unit me, Vector2 foot, float dt)
        {
            if (IsConveyor(o)) { TickConveyor(o, me, foot, dt); return true; }
            switch (o.Kind)
            {
                case "COLLAPSE_FLOOR": TickCollapse(o, me, foot, dt); return true;
                case "SWITCH_PAD":
                {
                    bool on = me != null && o.ShotBounds.Contains(foot);
                    if (on) _switchHeld = true;
                    if (o.Img != null && o.Frames != null && o.Frames[on ? 1 : 0] != null) o.Img.sprite = o.Frames[on ? 1 : 0];
                    // 표시등이 작아 전투 중에 안 읽혔다(코덱스 검수) — 안 밟았을 때는 판 전체가 숨 쉬듯 깜빡이고,
                    // 밟는 순간 먼지가 튄다
                    if (o.Img != null)
                    {
                        float k = on ? 1f : 0.72f + 0.28f * (0.5f + 0.5f * Mathf.Sin(Time.time * 7f));
                        var c = new Color(k, k, k, 1f);
                        if (o.Img.color != c) o.Img.color = c;
                    }
                    if (on && !o.Telegraph) _pfx?.Puff(o.ShotBounds.center, ParticleElement.Dust, 0.35f);
                    o.Telegraph = on;
                    return true;
                }
                case "LASER_PILLAR": TickLaserPillar(o, me, foot, dt); return true;
                case "SHIELD_GEN": TickShieldGen(o, dt); return true;
                case "ONEWAY_GATE": TickGate(o, me, foot); return true;
            }
            return false;
        }

        /// <summary>프레임 시작 — 한 프레임만 가는 표시를 비운다. `TickHazards2` 가 물건을 돌기 전에 부른다.</summary>
        private void BeginGimmick2Frame()
        {
            _switchHeld = false;
            _conveyedPlayer = false;
            _conveyed.Clear();
            _shielded.Clear();
        }

        /// <summary>프레임 끝 — 보호막 그림을 켜고 끈다.</summary>
        private void EndGimmick2Frame()
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null) continue;
                bool on = _shielded.Contains(e) && e.IsAlive && !e.IsDying;
                if (!_bubbles.TryGetValue(e, out var img))
                {
                    if (!on) continue;
                    _bubbleSprite ??= GetSprite("fx_shield_bubble");
                    var go = new GameObject("ShieldBubble", typeof(RectTransform), typeof(Image));
                    var rt = (RectTransform)go.transform;
                    rt.SetParent(e.transform, false);
                    rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                    float s = e.BodyRadius * 2.6f;
                    rt.sizeDelta = new Vector2(s, s);
                    img = go.GetComponent<Image>();
                    img.sprite = _bubbleSprite;
                    img.raycastTarget = false;
                    _bubbles[e] = img;
                }
                if (img == null) continue;
                if (img.gameObject.activeSelf != on) img.gameObject.SetActive(on);
                if (on) img.transform.localScale = Vector3.one * (1f + 0.03f * Mathf.Sin(Time.time * 6f));
            }
        }

        private bool IsShielded(Unit u) => u != null && _shielded.Contains(u);

        private void TickConveyor(Obstacle o, Unit me, Vector2 foot, float dt)
        {
            if (o.Frames != null && o.Img != null)
            {
                var f = o.Frames[Mathf.FloorToInt(Time.time * ConveyorFps) % o.Frames.Length];   // 이어 붙은 칸이 한 박자로 돈다
                if (f != null && o.Img.sprite != f) o.Img.sprite = f;
            }
            var step = ConveyorDir(o) * (Meters(ConveyorMps) * dt);
            if (me != null && !_conveyedPlayer && o.ShotBounds.Contains(foot))
            {
                _conveyedPlayer = true;   // 두 칸에 걸쳐 서도 한 번만 민다
                me.Position = SlideMove(me, me.Position, step);
            }
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying || e.IsBoss || _conveyed.Contains(e)) continue;
                if (!o.ShotBounds.Contains(e.Position)) continue;
                _conveyed.Add(e);
                e.Position = SlideMove(e, e.Position, step);
            }
        }

        private void TickCollapse(Obstacle o, Unit me, Vector2 foot, float dt)
        {
            if (o.Hp == 2) return;
            if (o.Hp == 0)
            {
                if (me == null || !o.ShotBounds.Contains(foot)) return;
                o.Hp = 1;
                o.Timer = 0f;
                _pfx?.Puff(o.ShotBounds.center, ParticleElement.Dust, 0.3f);
            }
            o.Timer += dt;
            int frame = o.Timer < CollapseSeconds * 0.5f ? 1 : 2;
            if (o.Timer < CollapseSeconds)
            {
                SetFrame(o, frame);
                if (o.Img != null) o.Img.color = frame == 2 ? new Color(0.72f, 0.66f, 0.6f, 1f) : new Color(0.88f, 0.84f, 0.8f, 1f);
                // 금이 가는 동안 판이 떤다 — 작은 화면에서 금 그림만으로는 「곧 꺼진다」가 약했다(코덱스 검수)
                if (o.View != null)
                {
                    float px = frame == 2 ? 2f : 1f;
                    ((RectTransform)o.View.transform).anchoredPosition = o.Home.center
                        + new Vector2(Mathf.Repeat(Time.time, 0.08f) < 0.04f ? px : -px, 0f);
                }
                return;
            }
            if (o.View != null) ((RectTransform)o.View.transform).anchoredPosition = o.Home.center;

            // 꺼진다 — 구덩이가 된다(몸은 못 건너고 탄은 넘는다)
            o.Hp = 2;
            SetFrame(o, 3);
            if (o.Img != null) o.Img.color = Color.white;
            o.BlocksMove = true;
            _pfx?.Puff(o.ShotBounds.center, ParticleElement.Dust, 0.8f);
            if (me != null && _host != null && o.ShotBounds.Contains(foot)) DamagePlayer(CollapseDamage);
            if (me != null) ResolveObstacles(me);
            for (int i = 0; i < _enemies.Count; i++)
                if (_enemies[i] != null && o.ShotBounds.Contains(_enemies[i].Position)) ResolveObstacles(_enemies[i]);
        }

        private void TickLaserPillar(Obstacle o, Unit me, Vector2 foot, float dt)
        {
            if (!_extraImg.TryGetValue(o, out var beam) || beam == null) return;
            float deg = o.Phase * 360f + Time.time * LaserPillarDegPerSec;
            beam.rectTransform.localRotation = Quaternion.Euler(0f, 0f, deg - 90f);   // 그림은 위(+y)로 뻗는다
            beam.transform.SetAsLastSibling();

            if (me == null || _host == null) return;
            o.Timer -= dt;
            if (o.Timer > 0f) return;
            var from = o.ShotBounds.center;
            var dir = new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));
            float along = Mathf.Clamp(Vector2.Dot(foot - from, dir), 0f, Meters(LaserPillarMeters));
            float off = Vector2.Distance(foot, from + dir * along);
            if (off > Meters(LaserPillarWidthMeters) * 0.5f + FootHalf(me).x) return;
            o.Timer = LaserPillarTick;
            DamagePlayer(LaserPillarDamage);
        }

        private void TickShieldGen(Obstacle o, float dt)
        {
            if (o.Hp <= 0)
            {
                // 부서진 뒤 — 가끔 연기만
                o.Timer -= dt;
                if (o.Timer <= 0f) { o.Timer = 0.8f; _pfx?.Puff(o.ShotBounds.center, ParticleElement.Dust, 0.25f); }
                return;
            }
            float r = Meters(ShieldGenMeters);
            var c = o.ShotBounds.center;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying || e.IsBoss) continue;
                if (Vector2.Distance(c, e.Position) <= r) _shielded.Add(e);
            }
        }

        /// <summary>내 탄이 발전기에 맞았다 — `DamageCrate` 가 맨 앞에서 부른다. 맞았으면 true(탄이 멈춘다).</summary>
        private bool DamageShieldGen(Vector2 at, int damage)
        {
            for (int i = 0; i < _obstacles.Count; i++)
            {
                var o = _obstacles[i];
                if (o.Kind != "SHIELD_GEN" || o.Hp <= 0 || !o.ShotBounds.Contains(at)) continue;
                o.Hp -= Mathf.Max(1, damage);
                ShowDamage(at, damage, toEnemy: true);
                if (o.Hp > 0) return true;
                SetFrame(o, 1);
                SpawnImpact(o.ShotBounds.center, "grenade", Meters(1.4f));
                _pfx?.Hit(o.ShotBounds.center, ParticleElement.Fire, 0.8f);
                return true;
            }
            return false;
        }

        private void TickGate(Obstacle o, Unit me, Vector2 foot)
        {
            if (o.Hp == 1 || me == null) return;
            if (foot.y < o.ShotBounds.yMin) { o.Telegraph = true; return; }   // 문 아래에 있었다
            if (!o.Telegraph || foot.y < o.ShotBounds.yMax + Meters(GateCloseMeters)) return;

            // 지나갔다 — 뒤에서 쾅 닫힌다
            o.Hp = 1;
            o.BlocksMove = true;
            if (_extraImg.TryGetValue(o, out var bars) && bars != null) bars.gameObject.SetActive(true);
            // 바닥 화살표는 자물쇠로 바뀐다 — 창살 가운데에 작게
            if (_extraImg2.TryGetValue(o, out var arrow) && arrow != null)
            {
                var lockSprite = GetSprite("obj_oneway_lock");
                if (lockSprite != null)
                {
                    float k = Meters(1f) / 72f;
                    arrow.sprite = lockSprite;
                    arrow.rectTransform.sizeDelta = new Vector2(Mathf.Round(40f * k), Mathf.Round(40f * k));
                    arrow.rectTransform.anchoredPosition = new Vector2(0f, Meters(0.25f));
                    arrow.transform.SetAsLastSibling();
                }
            }
            _pfx?.Puff(o.ShotBounds.center, ParticleElement.Dust, 0.9f);
            Shake(ShakeOnHit);
            for (int i = 0; i < _enemies.Count; i++)
                if (_enemies[i] != null && o.ShotBounds.Contains(_enemies[i].Position)) ResolveObstacles(_enemies[i]);
        }

        /// <summary>방을 나갈 때 — `ClearWarns` 가 부른다. 그림은 물건과 같이 지워진다 — 표만 비운다.</summary>
        private void ClearGimmick2()
        {
            _extraImg.Clear();
            _extraImg2.Clear();
            _shielded.Clear();
            foreach (var kv in _bubbles)
                if (kv.Value != null) kv.Value.gameObject.SetActive(false);
        }
    }
}
