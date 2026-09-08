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
        private const string CutsceneRes = "Assets/BundleResource/Cutscene";
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
                var name = Normalize(Path.GetFileName(src));
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
        /// 파일명을 프로젝트 규약으로 맞춘다.
        ///
        /// ⚠ 걷기 프레임을 코드는 `walk1`·`walk2` 로 찾는데(`Unit.FrameSuffix`)
        ///   보스 그림이 계속 `move1`·`move2` 로 들어왔다. 잡몹·호스트 150장은
        ///   전부 `walk` 라 아무도 눈치채지 못했고, **보스 걷기 벌이 통째로 null**
        ///   이었다 — 파일은 있는데 코드가 그 이름을 한 번도 찾지 않았다.
        ///   이름 하나 때문에 "리소스는 다 들어왔다" 가 거짓이 되는 자리라
        ///   경계에서 한 번 바로잡는다.
        /// </summary>
        private static string Normalize(string name)
            => name.Replace("_move1.", "_walk1.").Replace("_move2.", "_walk2.");

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
            // ⚠ 부르는 쪽이 `unit_crusher_s.png` 처럼 **확장자를 붙여** 넘긴다.
            //   아래 규칙은 대부분 `StartsWith` 라 그래도 걸렸지만, 이름을 통째로
            //   비교하는 규칙은 조용히 안 걸린다 — 실제로 크러셔가 그렇게 새 나갔다.
            //   여기서 한 번 벗겨 두면 어느 방식으로 적어도 걸린다.
            name = Path.GetFileNameWithoutExtension(name);

            // 컷신 폐기 목록은 `CutsceneImporter` 가 단일 출처다. 여기 또 적지 않는다.
            if (CutsceneImporter.IsRetired(name)) return true;

            // 빙의 자세는 **방향이 없다**(19차 정정). `_s_possess` 같은 방향형은 폐기본이고
            // `_possess1` · `_possess2` 두 장만 쓴다.
            if (name.StartsWith("unit_") && name.Contains("_possess")
                && !name.Contains("_possess1") && !name.Contains("_possess2")) return true;

            // 로딩 배경은 `Loading/loading_bg_1~3` 이 정본이다. 아래 세 장은 그 전 판본으로
            // **코드 어디에서도 안 쓴다.** 720×1280 짜리 세 장이라 인게임 아틀라스를
            // 혼자 두 배로 불린다(8 MB → 32 MB). 지워도 이 폴더에 남아 있어서
            // 툴을 돌릴 때마다 되살아났다 — 여기서 끊는다.
            if (name.StartsWith("loadingbackground")) return true;

            // 61차 크러셔 본체는 **발주가 틀려서** 폐기됐다.
            //
            // 「크러셔는 얼굴이 없다」로 발주했는데, 원작 시트의 `Body` 조각을 확대하면
            // 파란 창 둘 · 격자 물린 주황 아치 입 · 위쪽 주황 램프가 다 있다.
            // 지금 `BaseResource/Unit/crusher/` 에 있는 것이 그것을 정면으로 옮긴 정본이다.
            // 막지 않으면 툴을 돌릴 때마다 **맞는 그림을 틀린 그림이 덮어쓴다.**
            //
            // ⚠ 크러셔 본체를 다시 받게 되면 **이 줄을 지운다.** 이유가 사라지면 규칙도 간다
            //   (`roomfloor_python` 때와 같다 — 아래 참조).
            if (name == "unit_crusher_s") return true;

            // 방 바닥은 `BundleResource/RoomFloor/` 가 정본이다. `BaseResource` 쪽 사본은
            // 아틀라스에 들어가 자리만 먹었다.
            //
            // ⚠ 한때 `roomfloor_python` · `roomfloor_robot_snakes` 도 여기서 막았다.
            //   그때는 그 두 장이 규격 미달(720×1260)인 옛 사본이었기 때문인데,
            //   58차에서 720×936 으로 다시 받으면서 **그 두 장이 정본이 됐다.**
            //   규칙을 남겨 두면 새 정본이 영영 안 들어온다 — 실제로 4장만 반영됐다.
            //   막는 이유가 사라지면 규칙도 같이 지운다.
            // ⚠ 위에서 확장자를 벗기므로 여기도 확장자 없이 비교한다.
            //   `.png` 를 붙여 두면 영영 안 걸린다 — 실제로 한동안 죽어 있었다.
            if (name == "roomfloor_demolisher" || name == "roomfloor") return true;

            // 가디언 피격 5장과 옛 예고 자세는 **쓰지 않기로 했다**(기획 2026-09-03 —
            // "hit 은 그냥 지우고"). 보스는 `PlayHit` 이 첫 줄에서 돌아가므로 영영 안 뜨고,
            // `unit_guardian_s_tell` 은 스킬별 예고 자세 15장으로 대체됐다.
            // 지워도 이 폴더에 남아 있어 **툴을 돌릴 때마다 되살아났다** — 여기서 끊는다.
            if (name.StartsWith("unit_guardian_") && name.EndsWith("_hit")) return true;
            if (name == "unit_guardian_s_tell") return true;

            // 파이썬은 **벽 보스**다(기획 2026-09-04). 방향도 걷기도 근접 공격도 없다 —
            // 벽 구멍에서 머리만 내밀었다 들어가고, 그 그림은 `_s_out1~4` · `_s_in1~4` 뿐이다.
            // 옛 옆모습 시트 40장은 코드가 한 번도 안 부르면서 유닛 아틀라스만 먹었다.
            //
            // ⚠ 남기는 것은 `unit_python_s` 하나. 연출 그림을 놓았을 때 돌아갈
            //   바탕 그림(`Unit._baseSprite`)이라 이것까지 지우면 형제 보스 그림을 빌려 온다.
            if (name.StartsWith("unit_python_") && !IsPythonKeeper(name)) return true;

            // 컨셉 시안은 **고를 때 보는 그림**이지 게임 에셋이 아니다.
            // 고른 것은 `roomfloor_env_*` 로 따로 들어간다.
            if (name.StartsWith("concept_")) return true;

            return false;
        }

        /// <summary>
        /// 파이썬에서 **지금 쓰는** 그림. 이 목록 밖은 전부 옛 옆모습 시트다.
        ///
        /// ⚠ 새 그림을 받으면 **여기부터 늘린다.** 안 늘리면 폐기로 걸러져
        ///   납품이 조용히 사라진다 — 죽는 머리 4장이 그럴 뻔했다.
        /// </summary>
        private static bool IsPythonKeeper(string name)
        {
            if (name == "unit_python_s") return true;
            for (int i = 1; i <= 4; i++)
                if (name == $"unit_python_s_out{i}"
                 || name == $"unit_python_s_in{i}"
                 || name == $"unit_python_s_die{i}") return true;

            // 가로지를 때 쓰는 **옆보기 머리**. 정면 그림을 90° 돌려 만든 것이라
            // 그림 자체는 원본과 같다(옛 옆모습 시트와 이름이 겹치지 않는다).
            if (name == "unit_python_e_cross1") return true;

            return false;
        }

        /// <summary>프로젝트에 이미 있는 png 를 **이름으로** 색인한다. 같은 이름이 여러 곳일 수 있다.</summary>
        private static Dictionary<string, List<string>> IndexProject()
        {
            var map = new Dictionary<string, List<string>>();
            foreach (var root in new[] { BaseRes, RoomFloorRes, CutsceneRes })
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

            // 컷신은 **화면 한 장짜리**라 아틀라스에 넣지 않는다. 640×640 스무 장을 묶으면
            // 오프닝에만 쓰는 그림이 판 내내 메모리에 상주한다. 방 바닥과 같은 규칙 —
            // 한 장씩 불러오고 넘어가면 놓아 준다.
            if (name.StartsWith("cut_")) return new[] { $"{CutsceneRes}/{name}" };

            // 방 크기 무대 장치 — 바닥과 같은 곳으로 간다.
            //
            // ⚠ 파이썬의 벽은 720×144 다. 아래 700px 방어에 걸려 그냥 튕겨 나갔다.
            //   그 방어는 옳다(큰 그림이 UI 아틀라스를 4096 으로 밀어 올린다).
            //   다만 이것은 **갈 자리가 분명한** 그림이라 여기서 먼저 잡아 준다 —
            //   방마다 한 장씩 불러오고 넘어가면 놓아 주는, 방 바닥과 같은 규칙이다.
            if (name.StartsWith("obj_python_wall")) return new[] { $"{RoomFloorRes}/{name}" };

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

            // 상점 팝업 부품 — 인게임 화면에서만 쓴다.
            //
            // ⚠ `shopframe` 은 620×700 이라 **아래 700px 방어에 걸려 튕겨 나갔다.**
            //   그 방어는 720×1280 짜리 로딩 배경을 겨냥한 것이고, 이건 UI 액자다.
            //   실측(2026-09-08): 이 아틀라스는 최대 4096 이고 지금 내용이 8.2M px²,
            //   `shopframe` 은 434K px² 로 4096 한 장의 **2.6%** 다 — 페이지를 늘리지 않는다.
            //   방어 주석이 말한 대로 "어디로 가야 할지는 사람이 정한다". 여기가 그 자리다.
            if (name.StartsWith("shopframe") || name.StartsWith("shopitemslot")
                || name.StartsWith("shopleavebutton") || name.StartsWith("shopdivider")
                || name.StartsWith("buffcat_"))
                return new[] { $"{DefaultRes}/{name}" };

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
