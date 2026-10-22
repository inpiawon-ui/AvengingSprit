using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 손으로 그린 전투방 66개(`Projects/AVSR/Rooms/rooms90_ch*.txt`)를 읽는다.
    ///
    /// 사람이 고치는 것은 글자 지도(.txt)뿐이다. `Tools/rooms90_build.py` 가 그것을 검사하고
    /// 이 표(`rooms90.tsv`)로 굽는다 — 좌표·발자국·막힘·해저드가 전부 거기서 정해져 온다.
    /// 여기서는 **한 글자도 다시 셈하지 않는다.** 파이썬과 C# 이 같은 규칙을 두 번 들고 있으면
    /// 한쪽이 반드시 낡는다.
    ///
    /// 표는 탭으로 나눈 줄이다.
    ///   ROOM   id  이름  설명
    ///   SIZE   폭m  높이m  칸m                         (원작 방만 — 없으면 10 × 16 · 1 m)
    ///   OBJ    종류  x  y  폭  높이  이동막음  탄막음  적탄막음  해저드종류  해저드피해  해저드간격
    ///   SPAWN  배우  x  y  빼앗을수있음
    ///   TRACK  고리(0/1)  톱수  x,y;x,y;…              (원작 레일 — 톱이 칸 차례대로 따라간다)
    ///
    /// ── 원작 30방 (3단계, 2026-10-22) ──────────────────────────
    /// 궁수의 전설 원작 방을 그대로 옮긴 표 `rooms30.tsv`(`Tools/origin_rooms_bake.py`)가 있으면 **그것만** 읽는다.
    /// 방 번호가 30방 기준이라 15방 손배치 표(`rooms90.tsv`)와 섞으면 번호가 어긋난다.
    /// </summary>
    public static class Rooms90Hand
    {
        public const string Path = "Projects/AVSR/Rooms/rooms90.tsv";
        public const string OriginPath = "Projects/AVSR/Rooms/rooms30.tsv";

        /// <summary>원작 30방 표가 있으면 30방 체계로 굽는다.</summary>
        public static bool HasOrigin => File.Exists(OriginPath);

        public sealed class Obj
        {
            public string Kind;
            public float X, Y, W, H;
            public bool BlocksMove, BlocksShot, BlocksEnemyShot;
            public string Hazard;
            public int HazardDamage;
            public float HazardTick;
        }

        public sealed class Spawn
        {
            public string Actor;
            public float X, Y;
            public bool Host;
        }

        public sealed class Track
        {
            public bool Loop;
            public int Saws;
            public readonly List<Vector2> Points = new();
        }

        public sealed class Room
        {
            public string Id, Name, Note;
            /// <summary>SIZE 줄이 없으면 0 — 굽는 쪽이 기본 규격을 쓴다.</summary>
            public float Width, Height, Cell;
            public readonly List<Obj> Objects = new();
            public readonly List<Spawn> Spawns = new();
            public readonly List<Track> Tracks = new();
        }

        /// <summary>방 ID → 방. 원작 30방 표가 있으면 그것을, 없으면 15방 손배치 표를 읽는다.</summary>
        public static Dictionary<string, Room> Load() => Load(HasOrigin ? OriginPath : Path);

        public static Dictionary<string, Room> Load(string path)
        {
            var map = new Dictionary<string, Room>();
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[90방] 손배치 표가 없다: {path} — rooms90_build.py 를 먼저 돌린다");
                return map;
            }

            Room cur = null;
            var inv = CultureInfo.InvariantCulture;
            foreach (var raw in File.ReadAllLines(path))
            {
                if (raw.Length == 0 || raw[0] == '#') continue;
                var t = raw.Split('\t');
                switch (t[0])
                {
                    case "ROOM":
                        cur = new Room { Id = t[1], Name = t.Length > 2 ? t[2] : "", Note = t.Length > 3 ? t[3] : "" };
                        map[cur.Id] = cur;
                        break;
                    case "OBJ":
                        cur?.Objects.Add(new Obj
                        {
                            Kind = t[1],
                            X = float.Parse(t[2], inv), Y = float.Parse(t[3], inv),
                            W = float.Parse(t[4], inv), H = float.Parse(t[5], inv),
                            BlocksMove = t[6] == "1", BlocksShot = t[7] == "1", BlocksEnemyShot = t[8] == "1",
                            Hazard = t[9], HazardDamage = int.Parse(t[10], inv), HazardTick = float.Parse(t[11], inv),
                        });
                        break;
                    case "SPAWN":
                        cur?.Spawns.Add(new Spawn
                        {
                            Actor = t[1], X = float.Parse(t[2], inv), Y = float.Parse(t[3], inv), Host = t[4] == "1",
                        });
                        break;
                    case "SIZE":
                        if (cur == null) break;
                        cur.Width = float.Parse(t[1], inv);
                        cur.Height = float.Parse(t[2], inv);
                        cur.Cell = float.Parse(t[3], inv);
                        break;
                    case "TRACK":
                        if (cur == null) break;
                        var tr = new Track { Loop = t[1] == "1", Saws = int.Parse(t[2], inv) };
                        foreach (var pair in t[3].Split(';'))
                        {
                            var xy = pair.Split(',');
                            tr.Points.Add(new Vector2(float.Parse(xy[0], inv), float.Parse(xy[1], inv)));
                        }
                        cur.Tracks.Add(tr);
                        break;
                }
            }
            return map;
        }
    }
}
