using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 원작(궁수의 전설) 레일 톱 — 3단계 2026-10-22.
    ///
    /// 원작 방의 `t` 칸은 레일이다. 톱이 레일을 따라 칸 차례대로 움직인다 —
    /// 고리 레일이면 한 방향으로 돌고, 외줄이면 끝에서 끝을 오간다(`Tools/origin_rooms_bake.py` 가 레일을 줄로 나눈다).
    /// 지금 우리 「레일 톱날」(`SLIDE_BLADE_H/V`)은 제자리에서 ±3 m 를 오가는 직선뿐이라 원작의 고리 · 격자를 못 그린다.
    ///
    /// 레일 바닥은 칸마다 깐다(`obj_rail_bed_h/v` — 무대 그림이 있으면 그것). 톱은 `RAIL_SAW` 장애물 하나다 —
    /// 피해 · 그림자 · 방 나갈 때 치우기는 장애물 체계를 그대로 탄다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>톱이 레일을 따라가는 속도(m/초). 원작 값은 데이터에 없다 — 지금 레일 톱날 평균 속도(약 4 m/초)보다 조금 느리게.</summary>
        private const float RailSawSpeedMeters = 3.0f;
        private const int RailSawDamage = 10;
        private const float RailSawTick = 0.5f;

        /// <summary>톱 크기(칸 대비). 원작 톱은 칸보다 조금 크다.</summary>
        private const float RailSawCellScale = 1.1f;

        private readonly List<GameObject> _railBeds = new();

        /// <summary>방에 들어설 때 — 레일 바닥을 깔고 톱을 세운다. `SpawnObstacles` 뒤에 부른다.</summary>
        private void SpawnTracks(RoomEntry room)
        {
            if (room == null || room.Tracks == null || room.Tracks.Count == 0 || _unitLayer == null) return;
            float cell = Meters(OriginCellMeters);
            for (int t = 0; t < room.Tracks.Count; t++)
            {
                var src = room.Tracks[t];
                if (src.Points == null || src.Points.Count < 2) continue;
                var pts = new Vector2[src.Points.Count];
                for (int i = 0; i < pts.Length; i++) pts[i] = ToPixels(src.Points[i]);

                for (int i = 0; i < pts.Length; i++) LayRailBed(pts, i, src.Loop, cell);

                float length = 0f;
                int segs = src.Loop ? pts.Length : pts.Length - 1;
                for (int i = 0; i < segs; i++) length += Vector2.Distance(pts[i], pts[(i + 1) % pts.Length]);
                int saws = Mathf.Max(1, src.Saws);
                for (int s = 0; s < saws; s++)
                    SpawnRailSaw(pts, src.Loop, length, (float)s / saws, cell);
            }
        }

        /// <summary>칸 하나의 레일 바닥 — 앞뒤 칸을 보고 가로 · 세로를 고른다.</summary>
        private void LayRailBed(Vector2[] pts, int i, bool loop, float cell)
        {
            var prev = i > 0 ? pts[i - 1] : loop ? pts[pts.Length - 1] : pts[i];
            var next = i < pts.Length - 1 ? pts[i + 1] : loop ? pts[0] : pts[i];
            bool horiz = Mathf.Abs(next.x - prev.x) >= Mathf.Abs(next.y - prev.y);
            var sprite = EnvSprite(horiz ? "obj_rail_bed_h" : "obj_rail_bed_v");

            var go = new GameObject("RailBed", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_unitLayer, false);
            go.transform.SetAsFirstSibling();   // 바닥에 깔린 것 — 사람 · 물건보다 아래
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pts[i];
            rt.sizeDelta = horiz ? new Vector2(cell, cell * 0.5f) : new Vector2(cell * 0.5f, cell);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = sprite != null ? Color.white : new Color(0.55f, 0.55f, 0.6f, 0.9f);
            img.raycastTarget = false;
            _railBeds.Add(go);
        }

        private void SpawnRailSaw(Vector2[] pts, bool loop, float length, float phase, float cell)
        {
            float size = cell * RailSawCellScale;
            var go = new GameObject("Obj_RAIL_SAW", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_unitLayer, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = pts[0];
            var img = go.GetComponent<Image>();
            var frames = EnvFrames("obj_blade", 4);
            img.sprite = frames != null && frames.Length > 0 ? frames[0] : null;
            img.color = img.sprite != null ? Color.white : new Color(0.78f, 0.78f, 0.84f, 0.95f);
            img.raycastTarget = false;

            // 판정은 톱 크기보다 조금 작게 — 원 그림의 귀퉁이까지 아프면 억울하다
            float hit = size * 0.7f;
            var rect = new Rect(pts[0].x - hit * 0.5f, pts[0].y - hit * 0.5f, hit, hit);
            _obstacles.Add(new Obstacle
            {
                Kind = "RAIL_SAW", View = go, Img = img, Frames = frames,
                Bounds = rect, ShotBounds = rect, Home = rect,
                BlocksMove = false, BlocksShot = false, BlocksEnemyShot = false,
                IsHazard = true, Damage = RailSawDamage, Tick = RailSawTick,
                Track = pts, TrackLoop = loop, TrackLength = length, Phase = phase,
                BaseColor = img.color,
            });
        }

        /// <summary>한 프레임 — 레일 위 자리로 옮기고 돌린다. `TickMovingObstacles` 가 부른다.</summary>
        private void TickRailSaw(Obstacle o, RectTransform rt)
        {
            if (o.Track == null || o.Track.Length < 2 || o.TrackLength <= 0f) return;
            float travel = Meters(RailSawSpeedMeters) * Time.time + o.Phase * o.TrackLength * (o.TrackLoop ? 1f : 2f);
            float d = o.TrackLoop
                ? Mathf.Repeat(travel, o.TrackLength)
                : Mathf.PingPong(travel, o.TrackLength);
            MoveObstacle(o, rt, PointOnTrack(o, d));
            rt.localRotation = Quaternion.Euler(0f, 0f, -Time.time * 720f);
        }

        private static Vector2 PointOnTrack(Obstacle o, float d)
        {
            var pts = o.Track;
            int segs = o.TrackLoop ? pts.Length : pts.Length - 1;
            for (int i = 0; i < segs; i++)
            {
                var a = pts[i];
                var b = pts[(i + 1) % pts.Length];
                float len = Vector2.Distance(a, b);
                if (d <= len || i == segs - 1) return len > 0f ? Vector2.Lerp(a, b, Mathf.Clamp01(d / len)) : a;
                d -= len;
            }
            return pts[0];
        }

        /// <summary>방을 나갈 때 — 레일 바닥을 치운다(톱은 장애물이라 `ClearObstacles` 가 치운다).</summary>
        private void ClearRailBeds()
        {
            for (int i = 0; i < _railBeds.Count; i++)
                if (_railBeds[i] != null) Destroy(_railBeds[i]);
            _railBeds.Clear();
        }
    }
}
