using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 피해야 하는 장애물 7종 (2026-09-28 — 「장애물도 다양하게 피해야 되는 것」).
    ///
    ///   LASER_H · LASER_V       레이저 문 — 꺼짐 → 깜빡(예고) → 켜짐을 되풀이한다
    ///   SLIDE_BLADE_H · _V      레일 톱날 — 한 줄 위를 오간다
    ///   SWING_HAMMER_H          가로 해머 — 기존 해머(세로)의 가로판
    ///   FLAME_JET_S · _E · _W   화염 분사구 — 예고 뒤 3 m 불기둥을 뿜는다
    ///   DROP_ZONE               낙하물 — 예고 원이 뜨고 1초 뒤 떨어진다. 가까이 있으면 나를 겨눈다
    ///   SLOW_POOL               끈끈이 웅덩이 — 밟으면 느려진다(나도 적도)
    ///   MINE                    근접 지뢰 — 다가가면 예고 원이 뜨고 0.9초 뒤 터진다. 적도 다친다
    ///
    /// 전부 **시간을 읽게 만드는** 물건이다. 서 있기만 하는 엄폐물은 한 번 파악하면 끝이지만
    /// 이것들은 「지금 지나가도 되나」를 매번 묻는다.
    ///
    /// ⚠ 그림은 있는 것을 쓴다 — 레이저(`obj_laser_beam`) · 톱날 · 해머 · 웅덩이(`fx_goo_splat`) ·
    ///   불(`fx_breath_fire`) · 예고 빗금. 분사구와 지뢰 몸통만 발주 대상이다(오기 전엔 색 상자).
    /// </summary>
    public sealed partial class BattleDirector
    {
        // ── 레이저 문 ────────────────────────────────────────────
        private const float LaserCycle = 3.2f;
        private const float LaserOffUntil = 0.50f;    // 주기의 이만큼은 꺼져 있다
        private const float LaserWarnUntil = 0.69f;   // 그 뒤 여기까지 깜빡인다(0.6초)
        private const float LaserOffAlpha = 0.14f;

        // ── 레일 톱날 · 가로 해머 ─────────────────────────────────
        private const float SlideCycle = 2.8f;
        private const float SlideTravel = 4.0f;       // 오가는 폭(m) — 가운데에서 ±2 m

        // ── 화염 분사구 ──────────────────────────────────────────
        private const float FlameCycle = 3.4f;
        private const float FlameOffUntil = 0.53f;
        private const float FlameWarnUntil = 0.71f;   // 0.6초 예고
        private const float FlameLengthMeters = 3.0f;
        private const float FlameWidthMeters = 1.1f;
        private const int FlameDamage = 8;
        private const float FlameTick = 0.5f;
        private const int FlamePuffs = 3;

        // ── 낙하물 ───────────────────────────────────────────────
        private const float DropCycle = 3.6f;
        private const float DropWarnSeconds = 1.0f;
        private const float DropRadiusMeters = 1.1f;
        private const float DropAimMeters = 4.5f;     // 이 안에 있으면 나를 겨눈다
        private const float DropScatterMeters = 2.0f;
        private const int DropDamage = 10;

        // ── 끈끈이 웅덩이 ────────────────────────────────────────
        private const int PoolSlowPercent = 45;
        private const float PoolSlowSeconds = 0.25f;

        // ── 지뢰 ────────────────────────────────────────────────
        private const float MineTriggerMeters = 1.4f;
        private const float MineFuseSeconds = 0.9f;
        private const float MineRadiusMeters = 1.7f;
        private const int MineDamage = 14;
        private const float MineEnemyHpRatio = 0.5f;

        /// <summary>플레이어가 지금 웅덩이를 밟고 있나. 이동 속도에 곱한다.</summary>
        private bool _inSlowPool;

        private float PoolMoveMul => _inSlowPool ? 1f - PoolSlowPercent / 100f : 1f;

        private static bool IsFlameJet(Obstacle o) => o.Kind != null && o.Kind.StartsWith("FLAME_JET");

        private static Vector2 FlameDir(Obstacle o)
            => o.Kind.EndsWith("_W") ? Vector2.left
             : o.Kind.EndsWith("_E") ? Vector2.right
             : Vector2.down;

        /// <summary>새 장애물의 준비물. `SetupMoving` 이 종류를 보고 부른다.</summary>
        private void SetupHazard(Obstacle ob)
        {
            var rt = ob.View != null ? (RectTransform)ob.View.transform : null;
            switch (ob.Kind)
            {
                case "LASER_H":
                    // 그림은 세로로 긴 한 장이다 — 가로 문은 눕힌다. 크기도 같이 바꿔야 늘어나지 않는다.
                    if (rt != null)
                    {
                        rt.sizeDelta = new Vector2(rt.sizeDelta.y, rt.sizeDelta.x);
                        rt.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    }
                    ob.IsHazard = true;
                    break;

                case "LASER_V":
                    ob.IsHazard = true;
                    break;

                case "SLIDE_BLADE_H":
                case "SLIDE_BLADE_V":
                    ob.Frames = EnvFrames("obj_blade", 4);
                    ob.IsHazard = true;
                    if (ob.Damage <= 0) ob.Damage = 10;
                    if (ob.Tick <= 0f) ob.Tick = 0.5f;
                    break;

                case "SWING_HAMMER_H":
                    ob.Frames = new[] { GetSprite("obj_hammer") };
                    ob.IsHazard = true;
                    if (ob.Damage <= 0) ob.Damage = 12;
                    if (ob.Tick <= 0f) ob.Tick = 0.7f;
                    break;

                case "FLAME_JET_S":
                case "FLAME_JET_E":
                case "FLAME_JET_W":
                    SetupFlame(ob);
                    break;

                case "DROP_ZONE":
                    // 자리표시만 한다 — 바닥에 아무것도 안 그린다. 보이는 것은 예고 원과 떨어지는 파편뿐이다.
                    if (ob.Img != null) ob.Img.enabled = false;
                    ob.Timer = DropCycle * (0.4f + ob.Phase * 0.6f);
                    break;

                case "SLOW_POOL":
                    // 바닥 웅덩이 한 장(obj_slow_pool) — 탄 효과 넉 장을 돌리던 것은 점처럼 보여 뺐다
                    ob.Frames = null;
                    break;
            }
        }

        /// <summary>
        /// 불기둥은 분사구 **밖에** 그린다 — 분사구의 자식으로 두면 같이 줄 서서 분사구 뒤로 숨는다.
        /// 세 덩이를 사선 위에 나란히 놓고 넉 장을 돌린다.
        /// </summary>
        private void SetupFlame(Obstacle ob)
        {
            ob.Timer = 0f;
            if (GetSprite("fx_breath_fire_1") == null || _unitLayer == null) return;
            ob.Frames = new[]
            {
                GetSprite("fx_breath_fire_1"), GetSprite("fx_breath_fire_2"),
                GetSprite("fx_breath_fire_3"), GetSprite("fx_breath_fire_4"),
            };

            var zone = FlameZone(ob);
            var go = new GameObject("Flame", typeof(RectTransform));
            go.transform.SetParent(_unitLayer, false);
            var root = (RectTransform)go.transform;
            root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = zone.center;
            root.sizeDelta = zone.size;

            var dir = FlameDir(ob);
            float step = Meters(FlameLengthMeters) / FlamePuffs;
            float puff = Meters(FlameWidthMeters) * 1.5f;
            ob.Puffs = new Image[FlamePuffs];   // 매 프레임 `GetComponent` 하지 않게 여기서 잡아 둔다
            for (int i = 0; i < FlamePuffs; i++)
            {
                var p = new GameObject("Puff", typeof(RectTransform), typeof(Image));
                p.transform.SetParent(root, false);
                var prt = (RectTransform)p.transform;
                prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
                prt.sizeDelta = new Vector2(puff, puff);
                prt.anchoredPosition = dir * (step * (i - (FlamePuffs - 1) * 0.5f));
                var img = p.GetComponent<Image>();
                img.sprite = ob.Frames[0];
                img.raycastTarget = false;
                ob.Puffs[i] = img;
            }
            go.SetActive(false);
            ob.View2 = root;
        }

        /// <summary>불기둥이 닿는 자리(방 좌표). 분사구 앞에서 시작해 3 m.</summary>
        private Rect FlameZone(Obstacle o)
        {
            var dir = FlameDir(o);
            float len = Meters(FlameLengthMeters), wid = Meters(FlameWidthMeters);
            var c = o.ShotBounds.center + dir * (o.ShotBounds.width * 0.5f + len * 0.5f);
            var size = Mathf.Abs(dir.x) > 0.5f ? new Vector2(len, wid) : new Vector2(wid, len);
            return new Rect(c.x - size.x * 0.5f, c.y - size.y * 0.5f, size.x, size.y);
        }

        /// <summary>움직이는 것들의 한 프레임. `TickMovingObstacles` 가 종류를 보고 부른다.</summary>
        private void TickHazardMotion(Obstacle o, RectTransform rt)
        {
            switch (o.Kind)
            {
                case "LASER_H":
                case "LASER_V":
                {
                    float t = Mathf.Repeat(Time.time / LaserCycle + o.Phase, 1f);
                    bool on = t >= LaserWarnUntil;
                    bool warn = !on && t >= LaserOffUntil;
                    o.HazardOn = on;
                    if (o.Img == null) break;
                    float a = on ? 1f
                            : warn ? (Mathf.Repeat(Time.time, 0.16f) < 0.08f ? 0.65f : 0.25f)
                            : LaserOffAlpha;
                    var c = o.BaseColor; c.a = a;
                    if (o.Img.color != c) o.Img.color = c;
                    break;
                }

                case "SLIDE_BLADE_H":
                case "SLIDE_BLADE_V":
                {
                    float t = Mathf.Repeat(Time.time / SlideCycle + o.Phase, 1f);
                    float e = Mathf.Sin(t * Mathf.PI * 2f) * 0.5f;
                    var axis = o.Kind == "SLIDE_BLADE_H" ? Vector2.right : Vector2.up;
                    MoveObstacle(o, rt, o.Home.center + axis * (Meters(SlideTravel) * e));
                    rt.localRotation = Quaternion.Euler(0f, 0f, -Time.time * 720f);
                    break;
                }

                case "SWING_HAMMER_H":
                {
                    float t = Mathf.Repeat(Time.time / HammerCycle + o.Phase, 1f);
                    float e = Mathf.SmoothStep(0f, 1f, t < 0.5f ? t * 2f : (1f - t) * 2f);
                    float span = HammerTravel * _pxPerMeter;
                    MoveObstacle(o, rt, o.Home.center + new Vector2(-span * 0.5f + span * e, 0f));
                    break;
                }

                case "SLOW_POOL":
                    if (o.Frames != null)
                        SetFrame(o, Mathf.FloorToInt(Mathf.Repeat(Time.time * 3f + o.Phase * 4f, 4f)));
                    break;
            }
        }

        /// <summary>시간으로 도는 것들 — 분사구 · 낙하물 · 웅덩이 · 지뢰. `TickGimmicks` 가 부른다.</summary>
        private void TickHazards2(float dt)
        {
            _inSlowPool = false;
            var me = Avatar;
            Vector2 foot = me != null ? new Vector2(me.Position.x, me.Position.y - FootDrop(me)) : default;

            for (int i = _obstacles.Count - 1; i >= 0; i--)
            {
                var o = _obstacles[i];
                if (o.Kind == null) continue;

                if (IsFlameJet(o)) { TickFlame(o, me, foot, dt); continue; }

                switch (o.Kind)
                {
                    case "DROP_ZONE":
                        TickDropZone(o, me, dt);
                        break;

                    case "SLOW_POOL":
                        if (me != null && o.ShotBounds.Contains(foot)) _inSlowPool = true;
                        for (int k = 0; k < _enemies.Count; k++)
                        {
                            var e = _enemies[k];
                            if (e == null || !e.IsAlive || e.IsBoss) continue;
                            if (o.ShotBounds.Contains(e.Position)) e.ApplySlow(PoolSlowPercent, PoolSlowSeconds);
                        }
                        break;

                    case "MINE":
                        TickMine(o, me, dt);
                        break;
                }
            }
        }

        private void TickFlame(Obstacle o, Unit me, Vector2 foot, float dt)
        {
            float t = Mathf.Repeat(Time.time / FlameCycle + o.Phase, 1f);
            bool on = t >= FlameWarnUntil;
            bool warn = !on && t >= FlameOffUntil;

            // 예고 — 분사구가 달아오르고, 불이 닿을 자리에 빗금 띠를 깐다(예고가 시작될 때 한 번).
            if (warn && !o.Telegraph)
            {
                o.Telegraph = true;
                if (o.Img != null) o.Img.color = WallTurretHotColor;
                if (IsOnScreenAt(o.ShotBounds.center))
                {
                    var dir = FlameDir(o);
                    var from = o.ShotBounds.center + dir * (o.ShotBounds.width * 0.5f);
                    // 피해는 불이 켜져 있는 동안 아래에서 준다 — 예고는 보여 주기만 한다
                    StartWarn(BandShape(from, dir, Meters(FlameWidthMeters), Meters(FlameLengthMeters)),
                              (FlameWarnUntil - FlameOffUntil) * FlameCycle, damage: 0);
                }
            }
            if (!warn && !on && o.Telegraph)
            {
                o.Telegraph = false;
                if (o.Img != null) o.Img.color = o.BaseColor;
            }

            if (o.View2 != null)
            {
                if (o.View2.gameObject.activeSelf != on) o.View2.gameObject.SetActive(on);
                if (on && o.Frames != null && o.Puffs != null)
                {
                    int f = Mathf.FloorToInt(Mathf.Repeat(Time.time * 10f, o.Frames.Length));
                    for (int k = 0; k < o.Puffs.Length; k++)
                    {
                        var img = o.Puffs[k];
                        var s = o.Frames[(f + k) % o.Frames.Length];
                        if (img != null && s != null && img.sprite != s) img.sprite = s;
                    }
                }
            }

            if (!on) { o.Timer = 0f; return; }
            if (o.Img != null && o.Img.color != o.BaseColor) o.Img.color = o.BaseColor;
            o.Telegraph = false;
            if (me == null || _host == null) return;

            o.Timer -= dt;
            if (o.Timer > 0f) return;
            if (!FlameZone(o).Contains(foot)) return;
            o.Timer = FlameTick;
            DamagePlayer(FlameDamage);
        }

        private void TickDropZone(Obstacle o, Unit me, float dt)
        {
            o.Timer -= dt;
            if (o.Timer > 0f) return;
            o.Timer = DropCycle;
            // 유령은 겨누지 않는다 · 화면 밖에서는 안 떨어진다
            if (_host == null || me == null || !IsOnScreenAt(o.ShotBounds.center)) return;

            var center = o.ShotBounds.center;
            Vector2 at;
            if (Vector2.Distance(center, me.Position) <= Meters(DropAimMeters)) at = me.Position;
            else
            {
                // 멀리 있으면 제 둘레에 흩뿌린다. 자리는 시각으로 정해 매번 달라진다.
                float a = Mathf.Repeat(Time.time * 137.5f + o.Phase * 360f, 360f) * Mathf.Deg2Rad;
                at = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Meters(DropScatterMeters);
            }
            float r = Meters(DropRadiusMeters);
            at.x = Mathf.Clamp(at.x, r, _roomSize.x - r);
            at.y = Mathf.Clamp(at.y, -_roomSize.y + r, -r);
            StartWarn(DiscShape(at, r), DropWarnSeconds, DropDamage, impactKind: "grenade", fxSize: r * 2f);
        }

        private void TickMine(Obstacle o, Unit me, float dt)
        {
            if (!o.Telegraph)
            {
                if (_host == null || me == null) return;
                if (Vector2.Distance(o.ShotBounds.center, me.Position) > Meters(MineTriggerMeters)) return;
                o.Telegraph = true;
                o.Timer = MineFuseSeconds;
                float r = Meters(MineRadiusMeters);
                StartWarn(DiscShape(o.ShotBounds.center, r), MineFuseSeconds, MineDamage,
                          impactKind: "grenade", fxSize: r * 2f, enemyHpRatio: MineEnemyHpRatio);
                return;
            }

            // 심지가 타는 동안 깜빡인다 — 끝이 가까울수록 빨리
            o.Timer -= dt;
            if (o.Img != null)
            {
                float period = Mathf.Lerp(0.08f, 0.24f, Mathf.Clamp01(o.Timer / MineFuseSeconds));
                o.Img.color = Mathf.Repeat(Time.time, period) < period * 0.5f ? BarrelLitColor : o.BaseColor;
            }
            if (o.Timer <= 0f) RemoveObstacle(o);   // 터지는 것은 예고(`ResolveWarn`)가 한다
        }
    }
}
