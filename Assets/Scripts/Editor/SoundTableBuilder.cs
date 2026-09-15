using System;
using System.Collections.Generic;
using System.IO;
using Game.Module.Common;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 사운드 표를 만든다 — `sounds` 그룹 · 음원 주소 35개 · 임포트 설정 · 루프 지점 · 적용표.
    ///
    /// 근거: `Projects/AVSR/AVSR_SoundPlan.md` (4절 적용표 · 5절 결정 D1~D8은 제안안 그대로).
    /// 루프 지점은 매니페스트에서 읽는다 — 손으로 옮겨 적으면 한 자리만 틀려도 이음매가 튄다.
    /// </summary>
    public static class SoundTableBuilder
    {
        private const string SoundDir = "Assets/BundleResource/Sounds";
        private const string TablePath = "Assets/BundleResource/TableData/SoundTable.asset";
        private const string ManifestPath = "Projects/AVSR/_sound_extract/sound_manifest.json";
        private const string SoundGroup = "sounds";
        private const string SoundLabel = "label_sound";
        private const string TableGroup = "tabledata";
        private const string TableLabel = "label_tabledata";
        private const float MusicVolume = 0.7f;   // D8
        private const float EffectVolume = 1f;    // D8
        private const float MusicQuality = 0.7f;

        // 루프 지점이 없는데 화면에 오래 깔리는 곡 — 통째로 되풀이한다(D1 로비).
        private static readonly HashSet<string> WholeLoop = new() { "bgm_11" };

        [Serializable] private sealed class Manifest { public ManifestItem[] items; }

        [Serializable]
        private sealed class ManifestItem
        {
            public string dst;
            public int sampleRate;
            public ManifestLoop loop;
        }

        [Serializable]
        private sealed class ManifestLoop
        {
            public int startSamples;
            public int loopSamples;
        }

        [MenuItem("Tools/Game/사운드/사운드 표 만들기")]
        public static void Build()
        {
            if (!File.Exists(ManifestPath))
            {
                Debug.LogError($"[SoundTable] 매니페스트가 없다: {ManifestPath}");
                return;
            }
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[SoundTable] Addressable 설정이 없다");
                return;
            }

            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
            var group = settings.FindGroup(SoundGroup) ?? settings.CreateGroup(
                SoundGroup, false, false, true, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            if (!settings.GetLabels().Contains(SoundLabel)) settings.AddLabel(SoundLabel);

            var clips = new List<SoundClipEntry>();
            int reimported = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var it in manifest.items)
                {
                    if (it == null || string.IsNullOrEmpty(it.dst)) continue;
                    bool music = it.dst.StartsWith("BGM/", StringComparison.Ordinal);
                    bool effect = it.dst.StartsWith("SFX/", StringComparison.Ordinal);
                    // Samples 는 원작 사운드 CPU 가 조합하는 원재료다 — 등록하지 않는다(D7)
                    if (!music && !effect) continue;

                    string path = $"{SoundDir}/{it.dst}";
                    if (AssetImporter.GetAtPath(path) is not AudioImporter importer)
                    {
                        Debug.LogWarning($"[SoundTable] 음원이 없다: {path}");
                        continue;
                    }
                    if (ApplyImport(importer, music)) reimported++;

                    string key = Path.GetFileNameWithoutExtension(it.dst);
                    var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group);
                    entry.address = $"Sounds/{key}";
                    entry.SetLabel(SoundLabel, true);

                    bool hasLoop = it.loop != null && it.loop.loopSamples > 0;
                    clips.Add(new SoundClipEntry(
                        key, entry.address, music, WholeLoop.Contains(key),
                        it.sampleRate > 0 ? it.sampleRate : 48000,
                        hasLoop ? it.loop.startSamples : 0,
                        hasLoop ? it.loop.loopSamples : 0,
                        1f));
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            EditorUtility.SetDirty(settings);

            var table = AssetDatabase.LoadAssetAtPath<SoundTable>(TablePath);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<SoundTable>();
                AssetDatabase.CreateAsset(table, TablePath);
            }
            var cues = Cues();
            table.Fill(MusicVolume, EffectVolume, clips.ToArray(), cues, ChapterMid());
            EditorUtility.SetDirty(table);
            RegisterTable(settings);
            AssetDatabase.SaveAssets();

            // 큐가 가리키는 음원이 실제로 있는지 — 없으면 그 순간이 조용해진다
            int missing = 0;
            foreach (var c in cues)
            {
                if (table.FindClip(c.ClipKey) == null) { missing++; Debug.LogWarning($"[SoundTable] 없는 음원: {c.Cue} → {c.ClipKey}"); }
                if (!string.IsNullOrEmpty(c.ThenClipKey) && table.FindClip(c.ThenClipKey) == null)
                { missing++; Debug.LogWarning($"[SoundTable] 없는 음원: {c.Cue} then → {c.ThenClipKey}"); }
            }
            Debug.Log($"[SoundTable] 음원 {clips.Count} · 큐 {cues.Length} · 임포트 변경 {reimported} · 빠진 음원 {missing}");
        }

        /// <summary>계획서 2절 임포트. 이미 같으면 다시 임포트하지 않는다 — BGM 인코딩이 오래 걸린다.</summary>
        private static bool ApplyImport(AudioImporter importer, bool music)
        {
            var s = importer.defaultSampleSettings;
            var want = s;
            if (music)
            {
                // 인트로+루프를 샘플 단위로 이어 붙이려면 스트리밍보다 메모리 쪽이 안정적이다
                want.loadType = AudioClipLoadType.CompressedInMemory;
                want.compressionFormat = AudioCompressionFormat.Vorbis;
                want.quality = MusicQuality;
            }
            else
            {
                // 짧고 자주 난다 — 재생 순간 지연이 없어야 한다
                want.loadType = AudioClipLoadType.DecompressOnLoad;
                want.compressionFormat = AudioCompressionFormat.ADPCM;
            }
            // 루프 지점이 48 kHz 기준이다 — 표본율을 건드리지 않는다
            want.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;

            if (s.loadType == want.loadType && s.compressionFormat == want.compressionFormat &&
                Mathf.Approximately(s.quality, want.quality) && s.sampleRateSetting == want.sampleRateSetting)
                return false;

            importer.defaultSampleSettings = want;
            importer.SaveAndReimport();
            return true;
        }

        private static void RegisterTable(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings settings)
        {
            var group = settings.FindGroup(TableGroup) ?? settings.CreateGroup(
                TableGroup, false, false, true, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            if (!settings.GetLabels().Contains(TableLabel)) settings.AddLabel(TableLabel);
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(TablePath), group);
            entry.address = "TableData/SoundTable";
            entry.SetLabel(TableLabel, true);
            EditorUtility.SetDirty(settings);
        }

        // ── 적용표 (계획서 4절) ──────────────────────────────────

        private static SoundCueEntry[] Cues()
        {
            var list = new List<SoundCueEntry>
            {
                // 4.1 화면 곡
                new("screen.title", "bgm_01"),
                new("screen.opening", "bgm_11"),
                new("screen.lobby", "bgm_11"),               // D1 스토리 테마
                new("stage.clear", "bgm_07"),
                new("stage.fail", "bgm_10", "bgm_12"),       // D2 실패 곡 뒤 로비까지 기록 화면 곡

                // 4.2 챕터 곡 (CH1 로봇 스네이크 · CH2 크러셔 · CH3 파이썬 · CH4 슬러지 · CH5 가디언 · CH6 킹핀)
                new("chapter.1.normal", "bgm_01"), new("chapter.1.boss", "bgm_02"),
                new("chapter.2.normal", "bgm_03"), new("chapter.2.boss", "bgm_02"),
                new("chapter.3.normal", "bgm_01"), new("chapter.3.boss", "bgm_02"),
                new("chapter.4.normal", "bgm_04"), new("chapter.4.boss", "bgm_02"),
                new("chapter.5.normal", "bgm_03"), new("chapter.5.boss", "bgm_02"),
                new("chapter.6.normal", "bgm_05"), new("chapter.6.boss", "bgm_06"),

                // 4.3 시스템
                new("ui.play", "sfx_17"),
                new("run.gold", "sfx_23"),
                new("run.card", "sfx_24"),                   // D4
                new("run.shop", "sfx_24"),                   // D4
                new("hit.enemy", "sfx_19"),
                new("hit.reflect", "sfx_27"),
                new("boss.down", "sfx_20", null, 1.5f),      // 원작 대폭발 6초가 이상하게 오래 간다 → 1.5초에서 줄여 끔(기획 2026-09-15)

                // ── 원작에 소리가 없던 순간 — 「없는 것보다 있는 게 낫다」(기획 2026-09-15) ──
                //   원작이 한 번도 안 부른 음원을 파형(길이 · 밝기 · 모양)으로 골랐다. 귀로 확인 전이다.
                new("event.possess", "sfx_30"),              // 밝게 끄는 1.1초
                new("event.exit", "sfx_26"),                 // 아주 밝게 끄는 0.8초
                new("event.bossphase", "sfx_25"),            // 길게 끄는 2.1초
                new("event.levelup", "sfx_28"),              // 밝은 0.98초
                new("event.offer", "sfx_16"),                // 0.1초 짧은 음정음
                new("event.emergency", "sfx_21"),            // 치고 줄어드는 0.65초

                // 4.5 보스 패턴 — 사용처 문서 MAME 실측 확정본(7d261296) 기준
                new("boss.RailLaser", "sfx_32"),             // 로봇 스네이크 공격
                new("boss.BurrowStrike", "sfx_32"),          // 로봇 스네이크 공격
                new("boss.WreckingBall", "sfx_29"),          // 크러셔 쇠구슬 착지
                new("boss.HeadLunge", "sfx_36"),             // 파이썬 머리 솟는 공격
                new("boss.Emerge", "sfx_36"),                // 슬러지 솟구침
                new("boss.MissileSalvo", "sfx_38"),          // 킹핀 비행 기계 공격
                new("boss.StrafingRun", "sfx_38"),
                new("boss.BoosterDrop", "sfx_38"),

                // ── 원작에 소리가 없던 패턴 채움 (가디언은 원작 무음 확정이지만 기획이 채우기로) ──
                new("boss.Crush", "sfx_29"),                 // 크러셔 압착 — 충격
                new("boss.Conveyor", "sfx_31"),              // 컨베이어 — 저음
                new("boss.ShieldUp", "sfx_21"),              // 방패 전개
                new("boss.RamCharge", "sfx_29"),             // 돌진 — 충격
                new("boss.SegmentThrust", "sfx_22"),         // 가디언 마디 돌진 — 짧은 파열
                new("boss.SegmentLaunch", "sfx_32"),         // 마디 사출
                new("boss.CoilWall", "sfx_31"),              // 똬리 — 저음
                new("boss.HeadBite", "sfx_21"),              // 머리 물기
                new("boss.VenomCloud", "sfx_24"),            // 파이썬 독 뱉기 — 브레스
                new("boss.BrickFall", "sfx_29"),             // 벽돌 낙하 — 충격
                new("boss.BodyShove", "sfx_31"),             // 몸통 밀기 — 저음
                new("boss.ExecutionLock", "sfx_16"),         // 킹핀 처형 조준 — 짧은 음정음
                new("boss.DebrisFall", "sfx_29"),            // 로봇 스네이크 천장 파편 — 충격
                new("boss.Spit", "sfx_24"),                  // 슬러지 뱉기 — 브레스
                new("boss.CeilingCling", "sfx_31"),          // 천장 붙기 — 저음
                new("boss.CeilingSpread", "sfx_36"),         // 천장 확산 — 솟구침과 같은 계열
            };

            // 4.4 호스트 — 공격음 · 피격음
            Hosts(list, "sfx_33", "sfx_35", "gangster", "thug", "hopper", "hopper_smg", "commando_mg", "commando_laser");
            Hosts(list, "sfx_32", "sfx_35", "commando_missile");
            Hosts(list, "sfx_38", "sfx_35", "commando_grenade", "robot");
            Hosts(list, "sfx_24", "sfx_35", "salamander", "dragon_blue", "dragoon");
            // ⚠ sfx_37 은 박쥐탄이 아니라 **닌자(사슬) 사슬 공격음**이다(MAME 실측 확정 7d261296).
            //   흡혈귀는 20분 빙의 동안 박쥐탄이 무음이었다.
            Hosts(list, "sfx_37", "sfx_35", "ninja_chain");
            // ── 원작에 전용 공격음이 없는 몸 — 「없는 것보다 있는 게 낫다」(기획 2026-09-15) ──
            //   원작이 한 번도 안 부른 음원을 파형으로 골라 채웠다. 적중 때 `hit.enemy` 도 그대로 난다.
            Hosts(list, "sfx_22", "sfx_34", "amazon", "amazon_elite");     // 0.2초 짧은 파열 — 휘두름
            Hosts(list, "sfx_26", "sfx_34", "white_wizard", "medium");      // 아주 밝게 끄는 0.8초 — 마법
            Hosts(list, "sfx_30", "sfx_34", "snowwoman");                    // 밝게 끄는 1.1초 — 얼음
            Hosts(list, "sfx_22", "sfx_35", "ninja", "baseball", "death");   // death 피격은 D5
            Hosts(list, "sfx_21", "sfx_35", "guru");                         // 치고 줄어드는 0.65초 — 파동
            Hosts(list, "sfx_16", "sfx_35", "vampire");                      // 0.1초 짧은 음정음 — 박쥐

            // 4.6 액티브 스킬 — 계열이 같은 원작음을 빌린다(D6). 나머지는 무음
            list.Add(new SoundCueEntry("skill.commando_grenade", "sfx_38"));
            list.Add(new SoundCueEntry("skill.commando_missile", "sfx_32"));
            list.Add(new SoundCueEntry("skill.dragon_blue", "sfx_32"));
            list.Add(new SoundCueEntry("skill.commando_laser", "sfx_32"));
            list.Add(new SoundCueEntry("skill.baseball", "sfx_27"));
            list.Add(new SoundCueEntry("skill.ninja_chain", "sfx_37"));   // 사슬 묶기 — 사슬 공격음

            // 원작 무음 스킬 채움 (기획 2026-09-15). 아마존 정예는 스킬이 보류라 아무 일도 없으므로 무음 그대로.
            list.Add(new SoundCueEntry("skill.amazon", "sfx_29"));        // 도약 내려찍기 — 충격
            list.Add(new SoundCueEntry("skill.death", "sfx_31"));         // 사신 — 저음
            list.Add(new SoundCueEntry("skill.guru", "sfx_30"));          // 수호 결계 — 밝게 끄는 소리
            list.Add(new SoundCueEntry("skill.thug", "sfx_33"));          // 난사 — 총격
            list.Add(new SoundCueEntry("skill.hopper_smg", "sfx_22"));    // 멀리 뛰기 — 짧은 파열
            list.Add(new SoundCueEntry("skill.commando_mg", "sfx_21"));   // 방벽
            list.Add(new SoundCueEntry("skill.dragoon", "sfx_24"));       // 불바다 — 브레스
            list.Add(new SoundCueEntry("skill.salamander", "sfx_24"));    // 독 뱉기 — 브레스
            list.Add(new SoundCueEntry("skill.snowwoman", "sfx_26"));     // 얼음 껍질 — 아주 밝은 소리
            list.Add(new SoundCueEntry("skill.ninja", "sfx_21"));         // 분신
            list.Add(new SoundCueEntry("skill.vampire", "sfx_25"));       // 박쥐 떼 — 길게 끄는 소리
            list.Add(new SoundCueEntry("skill.gangster", "sfx_16"));      // 전원 표식 — 짧은 음정음
            list.Add(new SoundCueEntry("skill.hopper", "sfx_28"));        // 치명 고조 — 밝은 소리
            list.Add(new SoundCueEntry("skill.medium", "sfx_31"));        // 골렘 소환 — 저음
            list.Add(new SoundCueEntry("skill.white_wizard", "sfx_26"));  // 마법 부채꼴
            list.Add(new SoundCueEntry("skill.robot", "sfx_29"));         // 포탑 설치 — 충격

            return list.ToArray();
        }

        private static void Hosts(List<SoundCueEntry> list, string attack, string hurt, params string[] hostKeys)
        {
            // ⚠ **평타 소리는 뺀다**(기획 2026-09-15 — 공격할 때 이상한 소리가 난다).
            //   표에 적어 둔 공격음은 원작 대응 기록으로만 남긴다. 적중음(`hit.enemy`)과 피격음은 그대로다.
            _ = attack;
            foreach (var key in hostKeys)
            {
                if (hurt != null) list.Add(new SoundCueEntry($"host.{key}.hurt", hurt));
            }
        }

        /// <summary>D3 — 원작 가운데 구역을 방 번호로.</summary>
        private static ChapterMidMusicEntry[] ChapterMid() => new[]
        {
            new ChapterMidMusicEntry(2, 5, 9, "bgm_08"),
            new ChapterMidMusicEntry(6, 4, 6, "bgm_08"),
            new ChapterMidMusicEntry(6, 7, 9, "bgm_09"),
        };
    }
}
