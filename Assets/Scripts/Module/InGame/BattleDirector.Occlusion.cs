using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 키 큰 물건 뒤에 선 몸이 보이게 — 물건을 반투명으로 (2026-10-07 PD 「2D 라서 위로 긴 것들이 애매하다 …
    /// 지나가야 될 것 같은데 막히고 느낌이 이상하다」).
    ///
    /// ── 무엇이 이상했나 ──────────────────────────────────────
    /// 쿼터뷰라 기둥 그림은 발자국(막히는 칸, 72 x 72)보다 위로 94 px 더 솟는다. 그 솟은 부분은 **바닥 위에 그려진 그림일 뿐**
    /// 실제로는 지나갈 수 있는 자리다. 그런데 몸이 그 뒤로 들어가면 통째로 가려져, 「막혀 있다」로 보이거나
    /// 적이 어디 있는지 안 보였다.
    ///
    /// ── 어떻게 ────────────────────────────────────────────────
    /// 몸(나 · 적)의 발이 물건의 **솟은 부분 안**(발자국 바로 위 칸)에 들어가면 그 물건을 반투명으로 낮춘다.
    /// 나가면 0.15초에 걸쳐 돌아온다. 막힘 · 탄 판정은 그대로다 — 그림만 바뀐다.
    /// 2D 쿼터뷰 게임이 흔히 쓰는 방법이다(나무 · 건물 뒤에 들어가면 비친다).
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const float OccludeAlpha = 0.42f;
        private const float OccludeFadeSpeed = 7f;   // 1/초 — 0.15초 안팎

        private static bool IsTallStatic(string kind)
            => kind == "PILLAR" || kind == "BULK" || kind == "PROP_TALL" || kind == "RAIL"
            || kind == "RICOCHET_WALL" || kind == "DIVIDER";

        /// <summary>매 프레임 — `SortDepth` 다음에 부른다.</summary>
        private void TickOcclusion(float dt)
        {
            for (int i = 0; i < _obstacles.Count; i++)
            {
                var o = _obstacles[i];
                if (o.Img == null || !IsTallStatic(o.Kind)) continue;
                float rise = ObstacleRise.TryGetValue(o.Kind, out var r) ? r : 0f;
                if (rise <= 0f) continue;
                // 솟은 부분 — 발자국 윗변에서 위로 rise 만큼(방 좌표는 위가 +y)
                var art = new Rect(o.ShotBounds.xMin, o.ShotBounds.yMax, o.ShotBounds.width, rise);
                bool hidden = Behind(_ghost, art) || Behind(_host, art);
                for (int k = 0; !hidden && k < _enemies.Count; k++) hidden = Behind(_enemies[k], art);

                var c = o.Img.color;
                float want = (hidden ? OccludeAlpha : 1f) * o.BaseColor.a;
                if (Mathf.Abs(c.a - want) < 0.005f) continue;
                c.a = Mathf.MoveTowards(c.a, want, OccludeFadeSpeed * dt);
                o.Img.color = c;
            }
        }

        private bool Behind(Unit u, Rect art)
        {
            if (u == null || !u.IsAlive || !u.gameObject.activeSelf) return false;
            var foot = new Vector2(u.Position.x, u.Position.y - FootDrop(u));
            return art.Contains(foot);
        }
    }
}
