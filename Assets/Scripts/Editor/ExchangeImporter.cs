using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Game.EditorTools
{
    /// <summary>
    /// `_exchange/in/` 납품 폴더를 프로젝트에 반영한다.
    ///
    /// ── 왜 이 툴이 있나 ──────────────────────────────────────
    /// 예전에는 손으로 파일을 복사했다. 그러면 **복사한 그 순간**만 맞고,
    /// 같은 파일이 다시 납품되면 알 방법이 없다.
    /// 2026-08-24 에 실제로 그렇게 어긋났다 — 문 4장·블록 3장·바닥 3장이
    /// 다시 납품됐는데 프로젝트에는 옛 파일이 그대로 있었고,
    /// 그 옛 파일을 재서 "디자인이 안 왔다"고 반려까지 보냈다.
    ///
    /// 이 툴은 **해시로 대조**한다. 손으로 복사하지 않는다.
    ///   같은 파일          → 건너뛴다
    ///   납품이 더 최신     → 덮어쓴다
    ///   프로젝트가 더 최신 → 건드리지 않고 알린다 (되돌리면 안 되므로)
    /// </summary>
    public static class ExchangeImporter
    {
        private const string InDir = "Projects/AVSR/_exchange/in";
        private const string BaseRes = "Assets/BaseResource";
        private const string UnitRes = "Assets/BaseResource/Unit";
        private const string RoomFloorRes = "Assets/BundleResource/RoomFloor";
        private const string DefaultRes = "Assets/BaseResource/InGameMainUI";
        private const string HostSelectRes = "Assets/BaseResource/HostSelectPanel";
        private const string AtlasDir = "Assets/BundleResource/Atlas";

        [MenuItem("Tools/Game/납품 반영 (_exchange/in)")]
        public static void Apply()
        {
            if (!Directory.Exists(InDir))
            {
                Debug.LogError($"[납품] 폴더 없음: {InDir}");
                return;
            }

            var existing = IndexProject();
            var copied = new List<string>();
            var skippedNewer = new List<string>();
            int same = 0;

            foreach (var src in Directory.GetFiles(InDir, "*.png"))
            {
                var name = Path.GetFileName(src);
                if (name.StartsWith("_")) continue;      // 검증용 임시 파일
                if (IsSuperseded(name)) continue;        // 폐기된 판본

                foreach (var dst in DestinationsFor(name, existing))
                {
                    if (File.Exists(dst))
                    {
                        if (SameFile(src, dst)) { same++; continue; }
                        // ⚠ 프로젝트가 더 최신이면 덮지 않는다. 손으로 고친 것을 되돌릴 수 있다.
                        if (File.GetLastWriteTimeUtc(dst) > File.GetLastWriteTimeUtc(src))
                        { skippedNewer.Add(name); continue; }
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(dst));
                    File.Copy(src, dst, true);
                    copied.Add(dst);
                }
            }

            if (copied.Count == 0)
            {
                Debug.Log($"[납품] 새로 반영할 것 없음 — 동일 {same}개"
                          + (skippedNewer.Count > 0 ? $" · 프로젝트가 더 최신 {skippedNewer.Count}개" : ""));
                return;
            }

            AssetDatabase.Refresh();
            foreach (var p in copied) ApplyImportSettings(p);
            RepackAtlasesFor(copied);

            Debug.Log($"[납품] 반영 {copied.Count}개 · 동일 {same}개"
                      + (skippedNewer.Count > 0 ? $" · 프로젝트가 더 최신이라 건너뜀 {skippedNewer.Count}개" : "")
                      + "\n  " + string.Join("\n  ", copied.Select(Path.GetFileName)));
            if (skippedNewer.Count > 0)
                Debug.LogWarning("[납품] 프로젝트가 더 최신이라 건너뛴 것: "
                                 + string.Join(", ", skippedNewer.Distinct()));
        }

        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// 납품 폴더에 남아 있지만 **이미 폐기된 판본**. 다시 끌어오면 안 된다.
        ///
        /// 납품 폴더는 지우지 않고 쌓이기만 하므로, 정정 전 판본이 그대로 남아 있다.
        /// 그것까지 반영하면 쓰지도 않는 그림이 아틀라스를 불린다 —
        /// 실제로 방향별 빙의 자세 105장이 그렇게 딸려 들어와 유닛 아틀라스가
        /// 40장에서 47장으로 늘었다.
        /// </summary>
        private static bool IsSuperseded(string name)
        {
            // 빙의 자세는 **방향이 없다**(19차 정정). `_s_possess` 같은 방향형은 폐기본이고
            // `_possess1` · `_possess2` 두 장만 쓴다.
            if (name.StartsWith("unit_") && name.Contains("_possess")
                && !name.Contains("_possess1") && !name.Contains("_possess2")) return true;

            // 로딩 배경은 `Loading/loading_bg_1~3` 이 정본이다. 아래 세 장은 그 전 판본으로
            // **코드 어디에서도 안 쓴다.** 720×1280 짜리 세 장이라 인게임 아틀라스를
            // 혼자 두 배로 불린다(8 MB → 32 MB). 지워도 이 폴더에 남아 있어서
            // 툴을 돌릴 때마다 되살아났다 — 여기서 끊는다.
            if (name.StartsWith("loadingbackground")) return true;

            // 방 바닥은 `BundleResource/RoomFloor/` 가 정본이다. `BaseResource` 쪽 사본은
            // 아틀라스에 들어가 자리만 먹는다(720×1260 두 장).
            if (name == "roomfloor_python.png" || name == "roomfloor_robot_snakes.png"
                || name == "roomfloor_demolisher.png" || name == "roomfloor.png") return true;

            // 컨셉 시안은 **고를 때 보는 그림**이지 게임 에셋이 아니다.
            // 고른 것은 `roomfloor_env_*` 로 따로 들어간다.
            if (name.StartsWith("concept_")) return true;

            return false;
        }

        /// <summary>프로젝트에 이미 있는 png 를 **이름으로** 색인한다. 같은 이름이 여러 곳일 수 있다.</summary>
        private static Dictionary<string, List<string>> IndexProject()
        {
            var map = new Dictionary<string, List<string>>();
            foreach (var root in new[] { BaseRes, RoomFloorRes })
            {
                if (!Directory.Exists(root)) continue;
                foreach (var p in Directory.GetFiles(root, "*.png", SearchOption.AllDirectories))
                {
                    var n = Path.GetFileName(p);
                    if (!map.TryGetValue(n, out var list)) map[n] = list = new List<string>();
                    list.Add(p.Replace('\\', '/'));
                }
            }
            return map;
        }

        /// <summary>
        /// 이 파일이 갈 자리. **이미 있는 자리를 최우선**으로 한다 —
        /// 규칙으로 추측하면 같은 파일이 두 군데 생겨 어느 것이 진짜인지 알 수 없게 된다.
        /// </summary>
        private static IEnumerable<string> DestinationsFor(string name,
                                                           Dictionary<string, List<string>> existing)
        {
            if (existing.TryGetValue(name, out var found)) return found;

            // 캐릭터 스프라이트는 슬러그 폴더로 간다. 슬러그에 밑줄이 있으므로
            // (`commando_grenade`) 실제 폴더 이름 중 **가장 긴 것**으로 맞춘다.
            if (name.StartsWith("unit_") && Directory.Exists(UnitRes))
            {
                var body = Path.GetFileNameWithoutExtension(name).Substring(5);
                var slug = Directory.GetDirectories(UnitRes)
                    .Select(Path.GetFileName)
                    .Where(d => body.StartsWith(d + "_") || body == d)
                    .OrderByDescending(d => d.Length)
                    .FirstOrDefault();
                if (slug != null) return new[] { $"{UnitRes}/{slug}/{name}" };
            }

            if (name.StartsWith("roomfloor_")) return new[] { $"{RoomFloorRes}/{name}" };

            // 인게임 HUD 부품·이펙트 — 새 이름은 `existing` 이 못 잡아 기본값으로 흐른다.
            if (name.StartsWith("hud_") || name.StartsWith("fx_"))
                return new[] { $"{DefaultRes}/{name}" };

            // 호스트 초상·액티브 스킬 아이콘은 **로비 화면 폴더**로 간다.
            //
            // ⚠ 이 규칙이 없어서 새 호스트 4장이 인게임 폴더로 떨어졌다.
            //   기존 21장이 이미 로비 폴더에 있으면 `existing` 이 먼저 잡아 주지만,
            //   **처음 오는 이름은 잡을 것이 없어** 기본값(인게임)으로 흘렀다.
            //   로비가 못 찾아 빈칸이 뜨고, 인게임 아틀라스만 커진다.
            if (name.StartsWith("hostportraitimage_") || name.StartsWith("ultimateicon_")
                || name.StartsWith("ultimatecard")
                // 호스트 상세 카드 부품 — 패시브 아이콘·직업 배지·숙련도 바.
                // 전부 로비 화면에서만 쓴다. 기본값(인게임)으로 흘리면
                // 로비가 못 찾아 빈칸이 뜨고 인게임 아틀라스만 커진다.
                || name.StartsWith("passiveicon_") || name.StartsWith("passiveskill")
                || name.StartsWith("jobbadge_") || name.StartsWith("shardbar")
                // 능력치 아이콘 — 새 이름(crit·rng·rate)은 `existing` 이 못 잡는다.
                || name.StartsWith("staticon_")
                || name.StartsWith("icon_"))
                return new[] { $"{HostSelectRes}/{name}" };

            // ⚠ 이름을 모르는 큰 그림은 **아틀라스 폴더로 보내지 않는다.**
            //   UI 아틀라스는 작은 조각을 모으는 곳인데, 720 짜리 배경 한 장이 섞이면
            //   2048 로 안 끝나고 4096 으로 넘어가 용량이 네 배가 된다
            //   (실제로 로딩 배경 3장·컨셉 5장이 그렇게 들어와 8 MB 를 32 MB 로 만들었다).
            //   어디로 가야 할지는 사람이 정한다 — 조용히 아무 데나 넣지 않는다.
            var size = ImageSize(name);
            if (size.w >= 700 || size.h >= 700)
            {
                Debug.LogWarning($"[납품] {name} 은 {size.w}×{size.h} 로 UI 조각이 아니다. "
                                 + "배경이면 `roomfloor_` 로 이름을 주거나 갈 자리를 알려 달라 — 건너뛴다.");
                return System.Array.Empty<string>();
            }
            return new[] { $"{DefaultRes}/{name}" };
        }

        /// <summary>PNG 머리 24바이트만 읽어 크기를 본다. 통째로 열지 않는다.</summary>
        private static (int w, int h) ImageSize(string name)
        {
            try
            {
                using var fs = File.OpenRead(Path.Combine(InDir, name));
                var head = new byte[24];
                if (fs.Read(head, 0, 24) < 24) return (0, 0);
                int W = (head[16] << 24) | (head[17] << 16) | (head[18] << 8) | head[19];
                int H = (head[20] << 24) | (head[21] << 16) | (head[22] << 8) | head[23];
                return (W, H);
            }
            catch { return (0, 0); }
        }

        private static bool SameFile(string a, string b)
        {
            using var md5 = MD5.Create();
            using var fa = File.OpenRead(a);
            using var fb = File.OpenRead(b);
            return md5.ComputeHash(fa).SequenceEqual(MD5.Create().ComputeHash(fb));
        }

        /// <summary>
        /// 도트 그림 공통 설정. **매번 강제로 다시 건다** —
        /// Unity 는 새 png 를 기본 Texture 로 들여오고, 그러면 아틀라스에 0장이 잡힌다.
        /// 이 프로젝트에서 가장 자주 반복된 사고다.
        /// </summary>
        private static void ApplyImportSettings(string path)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return;
            var name = Path.GetFileNameWithoutExtension(path);

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.spritePixelsPerUnit = 100f;
            // 이어 붙여 쓰는 것만 Repeat. 나머지는 Clamp 여야 가장자리가 반대편에서 새지 않는다.
            ti.wrapMode = name.StartsWith("floor_") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            ti.SaveAndReimport();
        }

        /// <summary>
        /// 바뀐 파일이 속한 아틀라스만 다시 묶는다.
        /// 아틀라스 이름은 **폴더 이름**으로 정해진다(`UIAssetPipeline` 과 같은 규칙).
        ///   Assets/BaseResource/Unit/{슬러그}/  →  unit_{슬러그}
        ///   Assets/BaseResource/{폴더}/         →  {폴더 소문자}
        /// </summary>
        private static void RepackAtlasesFor(List<string> paths)
        {
            var names = new HashSet<string>();
            foreach (var p in paths)
            {
                var dir = Path.GetDirectoryName(p).Replace('\\', '/');
                var leaf = Path.GetFileName(dir);
                if (dir.StartsWith(UnitRes + "/")) names.Add("unit_" + leaf);
                else if (dir.StartsWith(BaseRes + "/")) names.Add(leaf.ToLowerInvariant());
            }

            var list = new List<SpriteAtlas>();
            foreach (var n in names)
            {
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>($"{AtlasDir}/{n}.spriteatlasv2");
                if (atlas != null) list.Add(atlas);
                else Debug.LogWarning($"[납품] 아틀라스 없음: {n} — `Build Unit Atlases` 를 한 번 돌려라.");
            }
            if (list.Count == 0) return;

            SpriteAtlasUtility.PackAtlases(list.ToArray(), EditorUserBuildSettings.activeBuildTarget, false);
            Debug.Log("[납품] 아틀라스 재묶음: " + string.Join(", ", names));
        }
    }
}
