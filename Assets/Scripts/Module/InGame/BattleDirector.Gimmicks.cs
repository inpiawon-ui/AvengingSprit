using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 방 기믹 — 되튕기는 벽 · 폭발 통 · 벽 포탑 · 미는 바위, 그리고 **적이 지형을 보는 눈.**
    ///
    /// ── 왜 생겼나 (2026-09-28 배치 개편) ──────────────────────────
    /// 방 90개를 궁수의 전설식으로 다시 짰다. 그 전에는 적 탄이 모든 지형을 통과했고
    /// 적은 지형을 안 보고 쫓기만 했다 — 기둥이 그림일 뿐이었다. 이제
    ///   · 키 큰 것(기둥·덩어리·난간·되튕기는 벽·포탑·바위)은 적 탄도 막는다
    ///   · 원거리 적은 사선이 막히면 옆으로 나와 각을 잡고, 쏘고 나면 다시 숨는다
    ///   · 근접 적은 오목한 지형에 걸리면 옆으로 돌아 나간다
    /// 방 데이터(`RoomTable`)와 배치 원본(`Projects/AVSR/Rooms/rooms90_ch*.txt`)이 이것을 전제한다.
    ///
    /// 이 파일은 지형 쪽이 소유한다. 보스 파일은 안 건드린다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        // ═══════════════════════════════════════════════════════════
        //  적의 사선 · 엄폐
        // ═══════════════════════════════════════════════════════════

        /// <summary>사선 표본 간격(px). 물건이 최소 50 px(0.7 m)라 이 간격이면 안 새 나간다.</summary>
        private const float SightStepPixels = 24f;

        /// <summary>엄폐 뒤에 닿고 나서 내다보기까지. 짧아야 「숨었다」가 아니라 「숨었다 나온다」로 읽힌다.</summary>
        private const float CoverPeekSeconds = 0.7f;

        /// <summary>제자리걸음이 이만큼 이어지면 걸린 것으로 본다.</summary>
        private const float StuckSeconds = 0.4f;

        /// <summary>
        /// 두 점 사이에 **적 탄을 막는 것**이 없는가. 적 탄과 같은 자다 —
        /// 이 자로 「내가 쏠 수 있나」와 「저놈 눈에 내가 보이나」를 둘 다 잰다.
        /// </summary>
        private bool EnemyLineClear(Vector2 from, Vector2 to)
        {
            if (_obstacles.Count == 0) return true;
            var d = to - from;
            float len = d.magnitude;
            if (len < 1f) return true;
            int n = Mathf.CeilToInt(len / SightStepPixels);
            var step = d / n;
            var p = from;
            for (int k = 1; k < n; k++)
            {
                p += step;
                for (int i = 0; i < _obstacles.Count; i++)
                {
                    var o = _obstacles[i];
                    if (!o.BlocksEnemyShot || !o.BlocksShot) continue;
                    if (o.ShotBounds.Contains(p)) return false;
                }
            }
            return true;
        }

        /// <summary>그 자리가 저 눈에서 가려지나 (키 큰 것만 가린다).</summary>
        private bool HiddenFrom(Vector2 spot, Vector2 eye) => !EnemyLineClear(eye, spot);

        private bool Walkable(Unit e, Vector2 spot) => !BlockedAt(spot, FootHalf(e), FootDrop(e));

        /// <summary>개체마다 좌우를 갈라 놓는다. 전부 같은 쪽으로 나오면 한 줄로 늘어선다.</summary>
        private float SideSignOf(Unit e)
            => ((e.GetEntityId().GetHashCode() + _roomIndex) & 1) == 0 ? 1f : -1f;

        /// <summary>
        /// 사선이 막힌 원거리가 **옆으로 나와 각을 잡는** 자리.
        /// 가까운 옆자리부터 본다 — 멀리 나가면 엄폐를 버린 것이 된다.
        /// 옆으로는 안 트이면 한 걸음 다가선다. 그래도 안 되면 다음 프레임에 다시 잰다.
        /// </summary>
        private Vector2 PickLineOfSightSpot(Unit e, Unit me)
        {
            var toMe = me.Position - e.Position;
            var dir = toMe.sqrMagnitude < 0.0001f ? e.Facing : toMe.normalized;
            var side = new Vector2(-dir.y, dir.x) * SideSignOf(e);
            float[] steps = { 1.0f, 1.8f, 2.6f };
            for (int s = 0; s < steps.Length; s++)
            {
                for (int sign = 1; sign >= -1; sign -= 2)
                {
                    var cand = ClampedInField(e, e.Position + side * (sign * Meters(steps[s])));
                    if (!Walkable(e, cand)) continue;
                    if (EnemyLineClear(cand, me.Position)) return cand;
                }
            }
            return ClampedInField(e, e.Position + dir * Meters(1.5f));
        }

        /// <summary>
        /// 근접이 오목한 지형에 걸렸을 때 **돌아 나갈** 자리. 옆으로 2 m —
        /// 두 쪽 다 갈 수 있으면 플레이어에 가까워지는 쪽.
        /// </summary>
        private Vector2 PickDetourSpot(Unit e, Unit me)
        {
            var toMe = me.Position - e.Position;
            var dir = toMe.sqrMagnitude < 0.0001f ? e.Facing : toMe.normalized;
            var side = new Vector2(-dir.y, dir.x);
            var a = ClampedInField(e, e.Position + side * Meters(2f));
            var b = ClampedInField(e, e.Position - side * Meters(2f));
            bool ok1 = Walkable(e, a), ok2 = Walkable(e, b);
            if (ok1 && ok2)
                return Vector2.Distance(a, me.Position) <= Vector2.Distance(b, me.Position) ? a : b;
            if (ok1) return a;
            if (ok2) return b;
            // 양옆이 다 막혔다 — 뒤로 물러나 다시 잰다
            return ClampedInField(e, e.Position - dir * Meters(1.5f));
        }

        /// <summary>
        /// 이번 프레임에 거의 못 움직였는가를 센다. 걸린 것이 확실해지면 <c>true</c>.
        /// 밀림·경직으로 잠깐 멈춘 것은 시간이 짧아 여기 안 걸린다.
        /// </summary>
        private static bool NoteStuck(Unit e, Vector2 before, float dt)
        {
            if ((e.Position - before).sqrMagnitude > 1f) { e.StuckTimer = 0f; return false; }
            e.StuckTimer += dt;
            if (e.StuckTimer < StuckSeconds) return false;
            e.StuckTimer = 0f;
            return true;
        }

        // ═══════════════════════════════════════════════════════════
        //  되튕기는 벽
        // ═══════════════════════════════════════════════════════════
        //
        // 내 탄은 되돌아오고(도탄 버프와 무관), 적 탄은 키 큰 것이라 그냥 막힌다.
        // 벽 뒤에 선 적을 **벽에 쏘아 돌려 맞히는** 것이 이 물건의 값어치다.

        private bool RicochetWallAt(Vector2 at, out Obstacle wall)
        {
            for (int i = 0; i < _obstacles.Count; i++)
            {
                var o = _obstacles[i];
                if (o.Kind != "RICOCHET_WALL" || !o.ShotBounds.Contains(at)) continue;
                wall = o;
                return true;
            }
            wall = null;
            return false;
        }

        /// <summary>부딪힌 면의 바깥 방향. 얕게 겹친 축으로 튕겨야 자연스럽다.</summary>
        private static Vector2 CoverNormal(Obstacle o, Vector2 at)
        {
            float dx = at.x - o.ShotBounds.center.x;
            float dy = at.y - o.ShotBounds.center.y;
            float ox = o.ShotBounds.width * 0.5f - Mathf.Abs(dx);
            float oy = o.ShotBounds.height * 0.5f - Mathf.Abs(dy);
            return ox < oy ? new Vector2(Mathf.Sign(dx), 0f) : new Vector2(0f, Mathf.Sign(dy));
        }

        // ═══════════════════════════════════════════════════════════
        //  폭발 통
        // ═══════════════════════════════════════════════════════════
        //
        // 내 탄 한 발이면 터진다. 반경 2 m 안의 적은 체력의 70 % 를 잃고(잡몹은 대개 죽는다),
        // 나도 안에 있으면 아프다. 옆 통은 한 박자 뒤에 따라 터진다 — 사슬이 눈에 보이게.
        //
        // ⚠ 가시·톱니(`TickHazards`)는 적을 안 아프게 하지만 통은 **적도 아프다.**
        //   저것은 서 있기만 해도 밟히는 바닥이라 적이 제 발로 죽어 나가지만,
        //   통은 내가 쏴야 터진다 — 「통 옆에 선 놈부터」라는 판단이 곧 놀이다.

        private const float BarrelRadiusMeters = 2.0f;
        private const float BarrelEnemyHpRatio = 0.7f;
        private const int BarrelPlayerDamage = 12;
        private const float BarrelFuseSeconds = 0.18f;
        private static readonly Color BarrelLitColor = new(1f, 0.62f, 0.45f, 1f);

        private readonly List<Obstacle> _fused = new();
        private readonly List<float> _fuse = new();

        /// <summary>내 탄이 통에 닿았나. 닿았으면 심지에 불을 붙이고 true.</summary>
        private bool DamageBarrel(Vector2 at)
        {
            for (int i = 0; i < _obstacles.Count; i++)
            {
                var o = _obstacles[i];
                if (o.Kind != "EXPLOSIVE_BARREL" || !o.ShotBounds.Contains(at)) continue;
                LightFuse(o, 0f);
                return true;
            }
            return false;
        }

        private void LightFuse(Obstacle o, float seconds)
        {
            if (_fused.Contains(o)) return;
            _fused.Add(o);
            _fuse.Add(seconds);
            if (o.Img != null) o.Img.color = BarrelLitColor;   // 달아오른다 — 곧 터진다는 신호
        }

        private void TickFuses(float dt)
        {
            for (int i = _fused.Count - 1; i >= 0; i--)
            {
                _fuse[i] -= dt;
                if (_fuse[i] > 0f) continue;
                var o = _fused[i];
                _fused.RemoveAt(i);
                _fuse.RemoveAt(i);
                ExplodeBarrel(o);
            }
        }

        private void ExplodeBarrel(Obstacle o)
        {
            var at = o.ShotBounds.center;
            float r = Meters(BarrelRadiusMeters);
            RemoveObstacle(o);
            SpawnImpact(at, "grenade", r * 2f);

            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying || e.IsBoss) continue;
                if (Vector2.Distance(at, e.Position) > r + e.BodyRadius) continue;
                int dmg = Mathf.Max(1, Mathf.RoundToInt(e.HpMax * BarrelEnemyHpRatio));
                e.IsAggro = true;
                ShowDamage(e.Position, dmg, toEnemy: true);
                if (e.TakeDamage(dmg)) KillEnemy(e);
            }

            var me = Avatar;
            if (_host != null && me != null && Vector2.Distance(at, me.Position) <= r)
                DamagePlayer(BarrelPlayerDamage);

            // 옆 통으로 옮겨붙는다. 상자도 같이 부순다 — 폭발 옆의 상자가 멀쩡하면 거짓말이다.
            for (int i = _obstacles.Count - 1; i >= 0; i--)
            {
                var p = _obstacles[i];
                if (p == o) continue;
                float d = Vector2.Distance(at, p.ShotBounds.center);
                if (p.Kind == "EXPLOSIVE_BARREL" && d <= r * 1.2f) LightFuse(p, BarrelFuseSeconds);
                else if (p.Kind == "CRATE" && d <= r) RemoveObstacle(p);
            }
        }

        /// <summary>물건 하나를 그림자까지 치운다. 목록에서도 뺀다.</summary>
        private void RemoveObstacle(Obstacle o)
        {
            if (o.View != null) Destroy(o.View);
            if (o.View2 != null) Destroy(o.View2.gameObject);
            if (o.Shadow != null) { _obstacleShadows.Remove(o.Shadow); Destroy(o.Shadow); }
            _obstacles.Remove(o);
        }

        // ═══════════════════════════════════════════════════════════
        //  벽 포탑
        // ═══════════════════════════════════════════════════════════
        //
        // 조준하지 않는다. 한 방향으로 박자에 맞춰 쏜다 — 그 사선 위에 서지 않는 것이 답이고,
        // 사선이 방을 가르므로 「언제 건널까」가 생긴다. 죽일 수 없다(지형이다).
        // 십자 포탑(잡몹 `obj_turret`)과 다르다 — 그쪽은 돌고, 죽는다.

        private const float WallTurretInterval = 2.6f;
        private const float WallTurretTelegraph = 0.6f;
        private const int WallTurretDamage = 8;
        private static readonly Color WallTurretHotColor = new(1f, 0.75f, 0.55f, 1f);

        private static bool IsWallTurret(Obstacle o)
            => o.Kind != null && o.Kind.StartsWith("WALL_TURRET");

        private static Vector2 WallTurretDir(Obstacle o)
            => o.Kind.EndsWith("_W") ? Vector2.left
             : o.Kind.EndsWith("_E") ? Vector2.right
             : Vector2.down;   // _S — 입구 쪽(아래)으로

        private void TickWallTurrets(float dt)
        {
            for (int i = 0; i < _obstacles.Count; i++)
            {
                var o = _obstacles[i];
                if (!IsWallTurret(o)) continue;
                o.Timer -= dt;
                if (!o.Telegraph && o.Timer <= WallTurretTelegraph)
                {
                    o.Telegraph = true;
                    if (o.Img != null) o.Img.color = WallTurretHotColor;   // 포신이 달아오른다
                }
                if (o.Timer > 0f) continue;
                o.Timer = WallTurretInterval;
                o.Telegraph = false;
                if (o.Img != null) o.Img.color = o.BaseColor;
                // 유령은 겨누지 않는다(다른 적과 같은 규칙) · 화면 밖 포탑은 안 쏜다(NO_OFFSCREEN_TELEGRAPH)
                if (_host == null || !IsOnScreenAt(o.ShotBounds.center)) continue;
                FireWallTurret(o);
            }
        }

        private void FireWallTurret(Obstacle o)
        {
            var dir = WallTurretDir(o);
            // 제 몸(키 큰 것 — 적 탄을 막는다) 밖에서 출발해야 제 몸에 막히지 않는다
            var from = o.ShotBounds.center + dir * (o.ShotBounds.width * 0.65f);
            float reach = _roomSize.magnitude;
            float speed = _config.ShotSpeedEnemy;
            float life = reach / Mathf.Max(1f, speed) + 0.25f;
            int dmg = Mathf.Max(1, Mathf.RoundToInt(WallTurretDamage * EnemyGrowth() * NormalEnemyAtkMul));

            var shot = RentShot();
            if (shot == null) return;
            shot.SetSprite(ShotFrames("bullet"), "bullet", LoopsFrames("bullet"));
            shot.Fire(from, from + dir * reach, speed, dmg, false, null, _config.ShotSize, ShotEnemyColor, life);
            PlayFx("muzzle", from, 40f, loop: false);
        }

        /// <summary>그 자리가 지금 창 안인가. `IsOnScreen(Unit)` 과 같은 자다.</summary>
        private bool IsOnScreenAt(Vector2 roomPos)
        {
            const float Margin = 60f;
            var v = RoomToView(roomPos);
            return v.y <= Margin && v.y >= -_field.rect.height - Margin
                && v.x >= -Margin && v.x <= _field.rect.width + Margin;
        }

        // ═══════════════════════════════════════════════════════════
        //  미는 바위
        // ═══════════════════════════════════════════════════════════
        //
        // 플레이어가 밀면 밀린다. 다른 물건·벽에 닿으면 멈춘다. 적은 못 민다.
        // 키 큰 것이라 양쪽 탄을 다 막는다 — **엄폐를 옮겨 만드는** 것이 이 물건의 값어치다.

        /// <summary>
        /// 막힌 자리에 바위가 있으면 밀어 본다. 밀렸으면 true — 부르는 쪽이 다시 지나가 본다.
        /// 미는 방향은 입력의 큰 축 하나다. 대각으로 밀면 바위가 비스듬히 미끄러져 격자에서 벗어난다.
        /// </summary>
        private bool TryPushRock(Unit u, Vector2 from, Vector2 delta, Vector2 half, float drop)
        {
            var foot = new Vector2(from.x + delta.x, from.y + delta.y - drop);
            for (int i = 0; i < _obstacles.Count; i++)
            {
                var o = _obstacles[i];
                if (o.Kind != "PUSH_ROCK" || !o.BlocksMove || o.View == null) continue;
                if (Mathf.Abs(foot.x - o.Bounds.center.x) >= o.Bounds.width * 0.5f + half.x ||
                    Mathf.Abs(foot.y - o.Bounds.center.y) >= o.Bounds.height * 0.5f + half.y) continue;

                var push = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                    ? new Vector2(delta.x, 0f) : new Vector2(0f, delta.y);
                var next = o.Bounds;
                next.position += push;
                if (!RockFits(o, next)) return false;

                var shadowDelta = next.center - o.Bounds.center;
                MoveObstacle(o, (RectTransform)o.View.transform, next.center);
                if (o.Shadow != null)
                    ((RectTransform)o.Shadow.transform).anchoredPosition += shadowDelta;
                return true;
            }
            return false;
        }

        /// <summary>바위가 그 자리에 놓일 수 있나 — 방 안이고 다른 물건과 안 겹친다.</summary>
        private bool RockFits(Obstacle rock, Rect next)
        {
            float edge = RoomEdgeMeters * _pxPerMeter;
            if (next.xMin < edge || next.xMax > _roomSize.x - edge) return false;
            if (next.yMax > -edge || next.yMin < -_roomSize.y + edge) return false;
            for (int i = 0; i < _obstacles.Count; i++)
            {
                var o = _obstacles[i];
                if (o == rock || !o.BlocksMove) continue;
                if (o.Bounds.Overlaps(next)) return false;
            }
            return true;
        }

        // ═══════════════════════════════════════════════════════════
        //  한 프레임 · 방 정리
        // ═══════════════════════════════════════════════════════════

        private void TickGimmicks(float dt)
        {
            TickFuses(dt);
            TickWallTurrets(dt);
        }

        private void ClearGimmicks()
        {
            _fused.Clear();
            _fuse.Clear();
        }
    }
}
