using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Module.Common;
using Game.Module.Common.UI;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Resource;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 소환 연출 「영혼 배양실」 — 징조 시험판 2(`Projects/AVSR/_exchange/proto_summon/index.html`, PD 2026-10-27 「시험판 2 흐름 그대로」).
    ///
    /// 흐름은 꽝(R) · 좋음(SR 이상) 둘로 가르고, 세기(색 · 깨짐 양 · 빛 · 반짝이 · 리치 길이)는 몸 등급 R · SR · SSR 세 단계로 오른다
    /// (PD 2026-10-27 「등급에 따라 화려하게 — 지금은 등급이랑 상관없이 랜덤으로 뿌리는 느낌」). 대박은 SSR 연출.
    /// 1회: 관에서 유령이 나와 배양조로 — 꽝은 그냥 곡선으로 쏙, 좋음은 금빛으로 배양조를 한 바퀴 휘감아(나선) 쏙 →
    ///      배양조 안 실루엣이 여러 몸으로 바뀌며 돌다가(룰렛) 점점 느려져 진짜에서 멈춤(좋음이 더 길게 망설인다) → 금 → 깨짐 →
    ///      등장 자세 · 도는 빛살 + 제자리 반짝 · 이름 판 → 탭.
    /// 10회: 5 × 2 배양조 실루엣이 슬롯 릴처럼 돌고 왼쪽부터 멈추며 그 자리에서 깨짐. 좋은 칸 차례엔 「리치!」 — 릴이 느려지고 떨리다 그 자리 공개.
    ///      칸 밑에 이름 · 조각 수 · 새 몸. 결과는 이 화면에서 끝(「확인 · 10회 더」).
    /// 실루엣은 공개 전까지 정체를 숨긴다(PD 2026-10-23 「까기 전부터 뭔지 알고 하는 거고」).
    /// </summary>
    public sealed class SummonReveal : MonoBehaviour
    {
        /// <summary>뽑기 등급 — 연출은 꽝(Normal) · 좋음(나머지) 둘로만 가른다. 이름 판 꼬리표는 따로(TagKey).</summary>
        public enum Rarity { Normal, Rare, Super, Jackpot }

        public readonly struct Entry
        {
            public readonly string HostKey, Name, GradeLabel, TagKey;
            public readonly Rarity Rarity;
            /// <summary>칸 밑 「조각 ×n」(다 키운 몸이면 정수 ×n — Essence 가 0 보다 크다).</summary>
            public readonly int Shards, Essence;
            public readonly bool IsNew;
            /// <summary>몸 등급 0 · 1 · 2(표 B · A · S = 화면 R · SR · SSR).</summary>
            public readonly int Grade;
            public Entry(string hostKey, string name, string gradeLabel, string tagKey, Rarity rarity, int grade, int shards = 0, int essence = 0, bool isNew = false)
            {
                HostKey = hostKey; Name = name; GradeLabel = gradeLabel; TagKey = tagKey; Rarity = rarity; Grade = grade;
                Shards = shards; Essence = essence; IsNew = isNew;
            }
            /// <summary>연출 단계 — 몸 등급(PD 2026-10-27 「등급에 따라 화려하게」). 대박은 최상위 연출.</summary>
            public int Tier => Rarity == Rarity.Jackpot ? 2 : Mathf.Clamp(Grade, 0, 2);
            /// <summary>SR 이상 — 나선 입장 · 긴 룰렛 · 리치.</summary>
            public bool Good => Tier >= 1;
        }

        // 등급 단계표(R · SR · SSR) — 색 · 깨짐 양 · 빛 · 유령 반짝이 · 10회 리치 길이. 흩어진 if 대신 한 표에서 읽는다
        private static readonly Color[] TierColor = { new(0.35f, 0.82f, 1f), new(0.73f, 0.55f, 1f), new(1f, 0.79f, 0.3f) };
        private static readonly float[] BurstOne = { 0.45f, 0.75f, 1f }, BurstTen = { 0.12f, 0.3f, 0.5f };
        private static readonly float[] LightOne = { 0.22f, 0.8f, 1f }, LightTen = { 0f, 0.6f, 1f };
        private static readonly float[] GhostSparks = { 0f, 15f, 35f }, ReachDur = { 0f, 1.2f, 1.8f };
        private static readonly float[] AuraOne = { 0.12f, 0.2f, 0.28f };

        [SerializeField] private Sprite _stage, _tank, _tankBroken, _plate;
        /// <summary>공개 빛 — 6장 루프(검은 바탕 가산). 1장은 도는 빛살로, 6장은 제자리 반짝으로 쓴다.</summary>
        [SerializeField] private Sprite[] _revealLight = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _crack = Array.Empty<Sprite>();
        /// <summary>
        /// 깨짐 알갱이 시트 — 유리 판(큰 · 중간) · 유리 부스러기 · 배양액 물방울, 반짝이(`lab_spark`). **파티클 시스템**으로 뿌린다.
        /// 검은 바탕 그림이면 가산(속이 비쳐 뒤 빛이 보인다).
        /// </summary>
        [SerializeField] private Texture2D _shardSheet, _chipSheet, _splashSheet, _spark;
        [SerializeField] private Vector2Int _shardGrid = new Vector2Int(3, 2), _chipGrid = new Vector2Int(1, 1), _splashGrid = new Vector2Int(3, 2);
        [SerializeField] private bool _shardAdditive;
        [SerializeField] private Sprite _stampNew, _ribbonPickup;
        /// <summary>10회 칸 밑 이름판(꽝 · 좋음) · 아래 버튼(확인 · 10회 더) — RE:BORN 키트 판.</summary>
        [SerializeField] private Sprite _labelPlate, _labelPlateGood, _btnOk, _btnAgain;
        [SerializeField] private Material _additive, _silhouette;
        /// <summary>둥근 부드러운 빛 — 그림 없이 셰이더가 그린다(`UISoftGlow.mat`).</summary>
        [SerializeField] private Material _softGlow;
        [SerializeField] private TMP_FontAsset _font;

        // ── 박자(시험판 2) ──
        // 1회: 배양조 0~0.45 떠오름 · 유령 0.45 부터(꽝 0.85초 · 좋음 1.5초) · 룰렛 +0.05 · 금 = 룰렛 끝 +0.25 · 깨짐 = 금 +0.45
        private const float TankIn = 0.45f, FlyAt = 0.45f, FlyBad = 0.85f, FlyGood = 1.5f, RoulGap = 0.05f, CrackGap = 0.25f, CrackDur = 0.45f;
        // 룰렛 — 바뀌는 간격이 0.06 초에서 점점 늘어 진짜에서 멈춘다(꽝 0.9초 · ×1.18 / 좋음 1.9초 · ×1.12)
        private const float ReelStart = 0.06f;
        // 10회: 0.9 부터 왼쪽부터 — R 칸 0.26 간격, SR · SSR 칸은 리치(ReachDur) 뒤 멈추고 0.35 쉼. 다 멈추면 +0.5 에 아래 버튼
        private const float TenStart = 0.9f, TenGap = 0.26f, TenAfterGood = 0.35f, TenEnd = 0.5f, TenReel = 0.07f;
        private static readonly Color Gold = new(1f, 0.79f, 0.3f);
        private static readonly Color SilhouetteColor = new(0.04f, 0.05f, 0.13f, 1f);
        private const float InkOfCell = 76f / 128f, FootFromBottom = 24f / 128f;
        private const int RouletteExtras = 6;   // 룰렛에 섞을 다른 몸 수 — 큰 몸 그림(아틀라스)을 이만큼 더 불러온다

        private sealed class Slot
        {
            public Entry E;
            public Sprite Art;
            /// <summary>등장 자세 — 전용 그림(unitL_{키}_se_appear)이 있으면 그것, 없으면 원작 공격 자세 atk1 → atk2.</summary>
            public Sprite[] Pose;
            /// <summary>룰렛에 도는 실루엣들 — 진짜는 멈출 때만.</summary>
            public Sprite[] Seed;
            public RectTransform Root, GhostLayer, Label;
            public Image Aura, Tank, Broken, Rays, Rays2, Light, LightB, Body, White, Crack, Trail0, GhostGlow, GhostImg, LabelImg, LabelNew;
            public Image[] Trail;
            public TextMeshProUGUI ReachText, LabelName, LabelCount;
            public Vector2 GlassCenter, GlassSize, Feet, Chest, Pipe;
            public float Cell, TankW, TankH;
            // 1회
            public float FlyStart, FlyEnd, Roul, CrackT, RevealT = float.MaxValue;
            public float[] ReelTimes = Array.Empty<float>();
            public float ReelDur;
            // 10회
            public float Reach = -1f, ReelPos, LastT, SparkAcc;
            public bool Ten, RevealFx, Behind;
        }

        private RectTransform _root, _shake, _footer;
        // 파티클은 Overlay 캔버스 위에 안 그려진다 — 배양실만 메인 카메라 캔버스로 따로 띄우고, 도는 동안 로비 캔버스를 잠깐 숨긴다
        private Canvas _canvas, _lobbyCanvas;
        private ParticleSystem _shardPs, _chipPs, _splashPs, _sparkPs;
        private Image _dim, _bg, _flash, _plateImg, _tagImg;
        private TextMeshProUGUI _tagText, _nameText, _gradeText, _tapText, _againText;
        private readonly List<Slot> _slots = new();
        private bool _tapped, _again, _ok;
        private int _run;   // 새 연출이 시작되면 앞의 것은 그 자리에서 멈춘다(같은 판을 둘이 그리지 않게)

        /// <summary>
        /// 뽑은 몸들을 공개한다. 1명 = 큰 배양조 하나(유령 입장 · 룰렛 · 탭 대기). 여럿 = 10칸 슬롯 릴 — 이 화면에서 끝난다.
        /// 돌려주는 값: 10회 화면에서 「10회 더」 를 눌렀는가(1회는 늘 false).
        /// 큰 몸 그림이 없는 몸은 건너뛴다.
        /// </summary>
        public async UniTask<bool> PlayAsync(IReadOnlyList<Entry> entries, string againLabel, CancellationToken ct)
        {
            if (entries == null || entries.Count == 0 || !CoreModule.TryGet<IResourceManager>(out var res)) return false;
            int run = ++_run;
            // 앞 판이 끝나기 전에 새로 부르면 앞 판의 빛 · 이름판이 그림을 불러오는 동안 남는다
            if (_root != null) _root.gameObject.SetActive(false);
            ClearParticles();
            var loaded = new List<string>();
            try
            {
                var ghostAtlas = await Load(res, "ghost", loaded);
                var ghost = ghostAtlas != null ? ghostAtlas.GetSprite("unitL_ghost_se") : null;
                var arts = new List<(Entry e, Sprite art, Sprite[] pose)>();
                var pool = new List<Sprite>();
                for (int i = 0; i < entries.Count; i++)
                {
                    var atlas = await Load(res, entries[i].HostKey, loaded);
                    var art = atlas != null ? atlas.GetSprite($"unitL_{entries[i].HostKey}_se") : null;
                    if (art == null) continue;
                    arts.Add((entries[i], art, PoseOf(atlas, entries[i].HostKey)));
                    if (!pool.Contains(art)) pool.Add(art);
                }
                // 룰렛에 섞을 다른 몸 — 숨은 몸(사신 등)은 빼고 무작위로 몇 명 더
                foreach (var key in ExtraHosts(entries))
                {
                    var atlas = await Load(res, key, loaded);
                    var art = atlas != null ? atlas.GetSprite($"unitL_{key}_se") : null;
                    if (art != null && !pool.Contains(art)) pool.Add(art);
                }
                if (arts.Count == 0 || run != _run) return false;
                Build();
                ClearSlots();
                if (entries.Count > 1)
                {
                    foreach (var a in arts) _slots.Add(MakeSlot(a.e, a.art, a.pose, ghost, pool, true));
                    TenTimeline();
                    _againText.text = againLabel ?? string.Empty;
                }
                else
                {
                    _slots.Add(MakeSlot(arts[0].e, arts[0].art, arts[0].pose, ghost, pool, false));
                    OneTimeline();
                }
                await RunAsync(run, ct);
                return _again;
            }
            finally
            {
                if (_root != null && run == _run)
                {
                    _root.gameObject.SetActive(false);
                    ClearParticles();
                    _canvas.gameObject.SetActive(false);
                    if (_lobbyCanvas != null) _lobbyCanvas.enabled = true;
                }
                foreach (var a in loaded) res.Release(a);
            }
        }

        private void OnDestroy()
        {
            if (_lobbyCanvas != null) _lobbyCanvas.enabled = true;
            if (_canvas != null) Destroy(_canvas.gameObject);
        }

        private static async UniTask<SpriteAtlas> Load(IResourceManager res, string key, List<string> loaded)
        {
            string address = "atlas/unitl_" + key;
            try
            {
                var a = await res.LoadAsync<SpriteAtlas>(address);
                if (a != null) loaded.Add(address);
                return a;
            }
            catch (Exception) { return null; }
        }

        /// <summary>룰렛에 섞을 다른 몸 키 — 뽑힌 몸 · 유령 · 숨은 몸을 빼고 무작위로.</summary>
        private static List<string> ExtraHosts(IReadOnlyList<Entry> entries)
        {
            var keys = new List<string>();
            if (!CoreModule.TryGet<IPlayerDataService>(out var player)) return keys;
            var all = new List<string>();
            foreach (var h in player.PlayableHosts)
            {
                if (h == null || h.IsGhost || PlayerDataService.IsHiddenHost(h.HostKey)) continue;
                bool pulled = false;
                for (int i = 0; i < entries.Count && !pulled; i++) pulled = entries[i].HostKey == h.HostKey;
                if (!pulled) all.Add(h.HostKey);
            }
            while (keys.Count < RouletteExtras && all.Count > 0)
            {
                int i = UnityEngine.Random.Range(0, all.Count);
                keys.Add(all[i]);
                all.RemoveAt(i);
            }
            return keys;
        }

        // ── 배치 · 박자 ──────────────────────────────────────

        private float H => _root.rect.height;
        private float U => Mathf.Min(_root.rect.width, H * 720f / 1280f);   // 설계 폭(720 기준) — 4:3 · 20:9 에서도 같은 판
        /// <summary>시험판 1280 기준 px → 지금 판.</summary>
        private float P(float px) => px * H / 1280f;

        private void OneTimeline()
        {
            var s = _slots[0];
            bool good = s.E.Good;
            s.FlyStart = FlyAt;
            s.FlyEnd = FlyAt + (good ? FlyGood : FlyBad);
            MakeReel(s, good ? 1.9f : 0.9f, good ? 1.12f : 1.18f);
            s.Roul = s.FlyEnd + RoulGap;
            s.CrackT = s.Roul + s.ReelDur + CrackGap;
            s.RevealT = s.CrackT + CrackDur;
            // 시험판 tankGeo(360, 877, 640) — 배양조 밑이 무대 받침대에 앉게
            Place(s, new Vector2(0f, P(640f - 877f)), P(640f), P(640f) * 0.54f);
            s.Pipe = new Vector2(P(72f) * U / H * 1280f / 720f - U * 0.5f, P(640f - 1110f));   // 관 끝(무대 왼쪽 아래)
        }

        private void TenTimeline()
        {
            float t = TenStart;
            for (int i = 0; i < _slots.Count; i++)
            {
                var s = _slots[i];
                s.Ten = true;
                if (s.E.Good) { s.Reach = t; t += ReachDur[s.E.Tier]; s.RevealT = t; t += TenAfterGood; }
                else { s.RevealT = t; t += TenGap; }
                // 시험판 tankGeo(360 + (c - 2) × 136, 520 + 줄 × 360, 256)
                int c = i % 5, row = i / 5;
                Place(s, new Vector2((c - 2) * U * 136f / 720f, P(640f - 520f - row * 360f)), P(256f), P(256f) * 0.54f);
            }
        }

        private float TenEndTime() => _slots[_slots.Count - 1].RevealT + (_slots[_slots.Count - 1].E.Good ? TenAfterGood : TenGap) + TenEnd;

        /// <summary>룰렛 — 바뀌는 시각들(간격이 곱으로 늘어난다). 시험판 makeReel.</summary>
        private static void MakeReel(Slot s, float dur, float grow)
        {
            var times = new List<float>();
            float t = 0f, iv = ReelStart;
            while (t < dur) { times.Add(t); t += iv; iv *= grow; }
            s.ReelTimes = times.ToArray();
            s.ReelDur = t;
        }

        /// <summary>배양조 · 몸 자리. feet = 몸 발(배양조 유리 바닥), tankH = 배양조 높이, figure = 몸 키.</summary>
        private void Place(Slot s, Vector2 feet, float tankH, float figure)
        {
            s.Feet = feet;
            s.Cell = figure / InkOfCell;
            s.Chest = feet + new Vector2(0f, figure * 0.62f);
            s.TankH = tankH;
            s.TankW = _tank != null ? tankH * _tank.rect.width / _tank.rect.height : tankH * 0.6f;
            // 배양조 그림의 유리창 바닥 = 그림 아래에서 15% — 몸 발이 그 위에 선다(lab_tank 실측 2026-10-22)
            var tankCenter = feet + new Vector2(0f, tankH * 0.33f);
            foreach (var img in new[] { s.Tank, s.Broken, s.Crack })
            {
                img.rectTransform.sizeDelta = new Vector2(s.TankW, tankH);
                img.rectTransform.anchoredPosition = tankCenter;
            }
            // 유리창 = 배양조 그림 안 가로 21~79% · 세로 21~85%(lab_tank_glass 실측) — 조각이 여기서 나온다
            s.GlassSize = new Vector2(s.TankW * 0.58f, tankH * 0.64f);
            s.GlassCenter = feet + new Vector2(0f, tankH * 0.30f);
            s.Aura.preserveAspect = false;
            float glowR = s.Ten ? P(170f) : P(230f);
            s.Aura.rectTransform.sizeDelta = new Vector2(glowR * 2f, glowR * 2f);
            s.Aura.rectTransform.anchoredPosition = s.GlassCenter;
            foreach (var img in new[] { s.Body, s.White })
            {
                img.rectTransform.pivot = new Vector2(0.5f, FootFromBottom);
                img.rectTransform.sizeDelta = new Vector2(s.Cell, s.Cell);
                img.rectTransform.anchoredPosition = feet;
            }
            float g = P(104f);
            s.GhostImg.rectTransform.sizeDelta = new Vector2(g, g);
            s.GhostGlow.rectTransform.sizeDelta = new Vector2(g * 3f, g * 3f);
            foreach (var t in s.Trail) t.rectTransform.sizeDelta = new Vector2(g, g);
            if (s.ReachText != null) s.ReachText.rectTransform.anchoredPosition = tankCenter + new Vector2(0f, tankH * 0.5f - P(34f));
            if (s.Label != null) s.Label.anchoredPosition = feet - new Vector2(0f, P(52f + 26f));
        }

        // ── 진행 ─────────────────────────────────────────────

        private async UniTask RunAsync(int run, CancellationToken ct)
        {
            _canvas.gameObject.SetActive(true);
            if (_lobbyCanvas != null && _canvas.renderMode == RenderMode.ScreenSpaceCamera) _lobbyCanvas.enabled = false;   // 위에 그려지지 않게
            _root.gameObject.SetActive(true);
            _plateImg.gameObject.SetActive(false);
            _tapText.gameObject.SetActive(false);
            _footer.gameObject.SetActive(false);
            _flash.enabled = false;
            _tapped = _again = _ok = false;
            bool ten = _slots[0].Ten;
            var one = _slots[0];
            float end = ten ? TenEndTime() : one.RevealT + 1.3f;
            float start = Time.unscaledTime;
            bool cuedIn = false, cuedReveal = false;
            int stopped = 0;
            GameSound.Cue("ui.play");
            while (!ct.IsCancellationRequested && run == _run)
            {
                float t = Time.unscaledTime - start;
                if (_tapped)
                {
                    _tapped = false;
                    if (ten)
                    {
                        // 10회 — 누르면 다 멈춘 끝으로(아래 버튼이 나오면 버튼만 받는다)
                        if (t < end) { start = Time.unscaledTime - end; t = end; ClearParticles(); foreach (var s in _slots) s.RevealFx = true; }
                    }
                    else if (t >= end) break;
                    else if (t < one.RevealT) { start = Time.unscaledTime - one.RevealT; t = one.RevealT; }   // 공개로 건너뛰기
                }
                if (_ok || _again) break;
                if (!ten && !cuedIn && t >= one.FlyEnd) { cuedIn = true; GameSound.Cue("hit.reflect"); }
                if (!ten && !cuedReveal && t >= one.RevealT) { cuedReveal = true; GameSound.Cue(one.E.Good ? "run.card" : "run.gold"); }
                if (ten)
                    while (stopped < _slots.Count && t >= _slots[stopped].RevealT) { GameSound.Cue(_slots[stopped].E.Good ? "run.card" : "ui.tap"); stopped++; }

                // 화면: 1회 딤 0.32 → 공개 때 0.6초에 걷힘 · 10회 0.38 그대로
                float dim = ten ? 0.38f : 0.32f * (1f - Seg(t, one.RevealT, one.RevealT + 0.6f));
                _dim.color = new Color(0f, 0f, 0f, dim);
                if (!ten) Flash(t, one);
                foreach (var s in _slots) Draw(s, t);
                if (!ten)
                {
                    Banner(t - (one.RevealT + 0.6f), one);
                    _tapText.gameObject.SetActive(t >= one.RevealT + 1.3f);
                    if (_tapText.gameObject.activeSelf) _tapText.alpha = 0.6f + 0.4f * Mathf.Sin(t * 2.5f);
                }
                else Footer(t - end);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        private void Draw(Slot s, float t)
        {
            float dt = Mathf.Clamp(t - s.LastT, 0f, 0.1f);
            s.LastT = t;
            bool good = s.E.Good, revealed = t >= s.RevealT;
            int tier = s.E.Tier;
            var c = TierColor[tier];
            float px = H / 1280f;

            // 배양조 — 1회 0~0.45 · 10회 0.05~0.4 떠오름(OutCubic), 공개되면 깨진 배양조
            float inU = s.Ten ? Ease.OutCubic(Seg(t, 0.05f, 0.4f)) : Ease.OutCubic(Seg(t, 0f, TankIn));
            Show(s.Tank, _tank, revealed ? 0f : inU);
            Show(s.Broken, _tankBroken, revealed ? 1f : 0f);

            // 공개 빛(좋음 · 1회는 꽝도 옅게) — 깨진 배양조 앞 · 몸 바로 뒤
            float lightA = !revealed ? 0f : s.Ten ? Seg(t, s.RevealT, s.RevealT + 0.4f) * LightTen[tier]
                                                  : Seg(t, s.RevealT, s.RevealT + 0.5f) * LightOne[tier];
            float lightSize = s.Ten ? P(300f) : P(760f) * (0.85f + 0.15f * Seg(t, s.RevealT, s.RevealT + 0.5f));
            RevealLight(s, t - s.RevealT, lightSize, lightA);

            // 몸 — 공개 전엔 룰렛 실루엣 · 공개 = 컷 + 세로 늘어남 → 등장 자세 → 숨쉬기
            float shake = 0f, sx = 1f, sy = 1f;
            bool inside = s.Ten ? t >= 0.4f : t >= s.FlyEnd;
            Sprite body = null;
            float reachU = s.Ten && s.Reach >= 0f && t >= s.Reach && !revealed ? Seg(t, s.Reach, s.RevealT) : -1f;
            if (!revealed && inside)
            {
                if (s.Ten)
                {
                    // 슬롯 릴 — 0.07 초 간격, 리치 땐 간격이 0.06 → 0.36 초로 늘어 점점 느려진다
                    float iv = reachU >= 0f ? 0.06f + 0.3f * reachU : TenReel;
                    if (reachU >= 0f) s.ReelPos += dt / iv; else s.ReelPos = (t - 0.4f) / iv;
                    body = s.Seed[Mathf.FloorToInt(Mathf.Max(0f, s.ReelPos)) % s.Seed.Length];
                    if (reachU >= 0f) shake = Mathf.Sin(t * 40f) * 3f * px * reachU;
                }
                else
                {
                    body = ReelSprite(s, t - s.Roul);
                    if (t >= s.CrackT) shake = Mathf.Sin(t * 50f) * (2f + 1.5f * tier) * px * Seg(t, s.CrackT, s.RevealT);
                }
            }
            else if (revealed)
            {
                float tr = t - s.RevealT, u = Seg(tr, 0f, 0.3f);
                sy = u < 0.3f ? Mathf.Lerp(1f, 1.15f, u / 0.3f) : Mathf.Lerp(1.15f, 1f, Ease.OutBack((u - 0.3f) / 0.7f));
                sx = 2f - sy;
                float grow = 1f + (s.Ten ? 0.05f + 0.05f * tier : 0.3f) * Ease.OutCubic(u);
                float breathe = !s.Ten && tr > PoseEnd ? Mathf.Sin((tr - PoseEnd) * Mathf.PI * 2f / 1.8f) : 0f;
                sx *= grow * (1f - 0.01f * breathe); sy *= grow * (1f + 0.02f * breathe);
                body = !s.Ten || good ? PoseSprite(s, tr) : s.Art;
            }
            s.Body.enabled = body != null;
            if (s.Body.enabled)
            {
                s.Body.sprite = body;
                s.Body.material = revealed ? null : _silhouette;
                s.Body.color = revealed ? Color.white : WithA(SilhouetteColor, inU);
                s.Body.rectTransform.anchoredPosition = s.Feet + new Vector2(shake, 0f);
                s.Body.rectTransform.localScale = new Vector3(sx, sy, 1f);
            }
            s.White.enabled = false;

            // 배양액 빛 — 유령이 들어간 뒤 실루엣 뒤에서(좋음 금 0.25 · 꽝 하늘 0.12), 리치 땐 금빛이 맥박 친다
            float aura = 0f;
            if (!revealed && inside)
                aura = s.Ten ? (reachU >= 0f ? 0.45f + 0.3f * reachU + 0.15f * Mathf.Sin(t * 8f) : 0f) : AuraOne[tier];
            s.Aura.enabled = aura > 0.01f && _softGlow != null;
            if (s.Aura.enabled) s.Aura.color = WithA(c, aura);
            if (s.ReachText != null) s.ReachText.gameObject.SetActive(reachU >= 0f);

            // 금 — 깨지기 전 3단계
            int crack = s.Ten || _crack.Length == 0 || t < s.CrackT || revealed ? -1
                      : Mathf.Min(_crack.Length - 1, Mathf.FloorToInt(Seg(t, s.CrackT, s.RevealT) * _crack.Length));
            s.Crack.enabled = crack >= 0;
            if (crack >= 0) s.Crack.sprite = _crack[crack];

            // 깨질 때 파티클(한 번) — 1회 좋음 1 · 꽝 0.45, 10회 좋음 0.5 · 꽝 0.12
            if (!s.RevealFx && revealed) { s.RevealFx = true; Shatter(s, c, s.Ten ? BurstTen[tier] : BurstOne[tier]); }

            // 10회 칸 밑 이름판 — 멈추고 0.15 초 뒤 OutBack 0.3초
            if (s.Label != null)
            {
                float k = revealed ? Ease.OutBack(Seg(t - s.RevealT, 0.15f, 0.45f)) : 0f;
                s.Label.gameObject.SetActive(k > 0f);
                if (k > 0f) s.Label.localScale = Vector3.one * k;
            }

            if (!s.Ten) DrawGhost(s, t, dt);
        }

        /// <summary>룰렛 실루엣 — 바뀌는 시각을 따라가다 끝나면 진짜. 시험판 reelKey.</summary>
        private static Sprite ReelSprite(Slot s, float t)
        {
            if (t < 0f) return s.Seed[0];
            if (t >= s.ReelDur) return s.Art;
            int i = 0;
            while (i + 1 < s.ReelTimes.Length && t >= s.ReelTimes[i + 1]) i++;
            return s.Seed[i % s.Seed.Length];
        }

        // ── 유령(1회) — 관 → 꽝: 곡선으로 쏙 / 좋음: 배양조를 한 바퀴 휘감으며(나선) 가슴으로 ──

        private Vector2 GhostPos(Slot s, float t, out bool behind)
        {
            behind = false;
            var c = s.Chest;
            float u = Seg(t, s.FlyStart, s.FlyEnd);
            if (!s.E.Good)
            {
                float k = Ease.InOutSine(u);
                var ctrl = new Vector2((s.Pipe.x + c.x) * 0.5f - P(40f), Mathf.Max(s.Pipe.y, c.y) + P(200f));
                return Bezier(s.Pipe, ctrl, c, k);
            }
            // 앞 35%: 관에서 배양조 왼쪽으로 / 뒤 65%: 반지름이 줄며 한 바퀴(나선) 돌아 가슴으로 — 끊김 없이 이어진다
            float r0 = s.TankW * 0.62f;
            const float split = 0.35f;
            if (u < split)
            {
                float k = u / split, e = k * (2f - k);   // 감속 없이 나선으로 이어지게 끝 속도 유지
                return Bezier(s.Pipe, new Vector2(s.Pipe.x + P(40f), c.y - P(60f)), new Vector2(c.x - r0, c.y), e);
            }
            float q = (u - split) / (1f - split), a = Mathf.PI + q * Mathf.PI * 2f, r = r0 * Mathf.Pow(1f - q, 1.1f);
            // 시험판은 화면 좌표(아래가 +) — 여기는 위가 + 라 세로를 뒤집는다. 배양조 뒤로 도는 구간은 배양조 뒤에 그린다
            // 배양조 폭 안에 들어왔을 때만 뒤로 — 옆에 있을 때 뒤로 보내면 유령 빛이 배양조 테두리에 일자로 잘려 네모처럼 보인다
            behind = Mathf.Sin(a) < -0.05f && q < 0.92f && Mathf.Abs(Mathf.Cos(a) * r) < s.TankW * 0.4f;
            return new Vector2(c.x + Mathf.Cos(a) * r, c.y - Mathf.Sin(a) * r * 0.5f + Mathf.Sin(q * Mathf.PI) * P(30f));
        }

        private void DrawGhost(Slot s, float t, float dt)
        {
            bool good = s.E.Good;
            float a = Seg(t, s.FlyStart - 0.25f, s.FlyStart) * (1f - Seg(t, s.FlyEnd - 0.08f, s.FlyEnd));
            bool on = a > 0.01f && s.GhostImg.sprite != null;
            s.GhostLayer.gameObject.SetActive(on);
            if (!on) return;
            var p = GhostPos(s, Mathf.Max(t, s.FlyStart), out bool behind);
            // 배양조 뒤로 도는 구간 — 유령 묶음을 배양조 앞 · 뒤로 옮긴다
            if (behind != s.Behind)
            {
                s.Behind = behind;
                if (behind) s.GhostLayer.SetSiblingIndex(0);
                else s.GhostLayer.SetAsLastSibling();
            }
            int tier = s.E.Tier;
            var tc = TierColor[tier];
            float size = good ? 1f + 0.06f * Mathf.Sin(t * 9f) : 1f;
            // 잔상 — 좋음은 금빛 6개 · 진하게, 꽝은 하늘빛 3개 · 옅게
            int n = good ? 6 : 3;
            var trailColor = Color.Lerp(tc, Color.white, 0.25f);
            for (int i = 0; i < s.Trail.Length; i++)
            {
                var tr = s.Trail[i];
                tr.enabled = i < n;
                if (!tr.enabled) continue;
                int k = i + 1;
                var q = GhostPos(s, Mathf.Max(s.FlyStart, t - k * 0.045f), out _);
                tr.color = WithA(trailColor, a * (good ? 0.32f : 0.12f) * (n + 1 - k) / n);
                tr.rectTransform.anchoredPosition = q;
                tr.rectTransform.localScale = Vector3.one * size * (1f - k * 0.07f);
            }
            s.GhostGlow.enabled = _softGlow != null;
            if (s.GhostGlow.enabled)
            {
                s.GhostGlow.color = WithA(Color.Lerp(tc, Color.white, good ? 0f : 0.5f), a * (good ? 0.45f + 0.15f * tier : 0.18f));
                s.GhostGlow.rectTransform.anchoredPosition = p;
                s.GhostGlow.rectTransform.localScale = Vector3.one * (good ? 1f : 0.6f);
            }
            s.GhostImg.color = WithA(Color.white, a);
            s.GhostImg.rectTransform.anchoredPosition = p;
            s.GhostImg.rectTransform.localScale = Vector3.one * size;
            // SR 이상 — 꼬리에 등급색 · 흰 반짝이(SR 초당 15 · SSR 35)
            if (GhostSparks[tier] > 0f)
            {
                s.SparkAcc += GhostSparks[tier] * dt;
                int m = Mathf.FloorToInt(s.SparkAcc);
                if (m > 0) { s.SparkAcc -= m; Sparks(p, UnityEngine.Random.value < 0.5f ? tc : Color.white, m, 0.3f); }
            }
        }

        private static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, float k)
            => (1f - k) * (1f - k) * a + 2f * (1f - k) * k * b + k * k * c;

        // ── 공개 빛 — 빛살은 한 방향으로 천천히 돌고(두 장을 마주 붙여 원), 반짝이 장은 제자리에서 넘긴다(PD 「왔다 갔다 금지」) ──

        private const float RayDegPerSec = 9f, RayAlpha = 0.55f, SparkleAlpha = 0.75f, SparkleFrame = 0.3f;
        // 빛 그림의 빛 뿌리는 아래에서 7% — 빛살은 가슴(발에서 0.36 크기 위), 반짝이는 발 자리
        private const float LightRoot = 0.07f, RayLift = 0.36f;

        private void RevealLight(Slot s, float tr, float size, float a)
        {
            bool on = _revealLight.Length > 0 && a > 0f;
            // 등급마다 겹을 하나씩 더한다 — 빛살은 다 같이, 반짝이 장은 SSR 만(SR 과 SSR 이 세기만 다르면 구분이 안 된다)
            bool sparkle = on && s.E.Tier >= 2;
            s.Rays.enabled = s.Rays2.enabled = on;
            s.Light.enabled = s.LightB.enabled = sparkle;
            if (!on) return;
            float rot = tr * RayDegPerSec;
            foreach (var (img, off) in new[] { (s.Rays, 0f), (s.Rays2, 180f) })
            {
                img.sprite = _revealLight[0];
                img.color = WithA(Color.white, a * RayAlpha);
                PlaceLight(img, s.Feet + new Vector2(0f, size * RayLift), size, rot + off);
            }
            if (!sparkle) return;
            float f = tr / SparkleFrame;
            int i0 = Mathf.FloorToInt(f) % _revealLight.Length;
            float blend = f - Mathf.Floor(f);
            s.Light.sprite = _revealLight[i0];
            s.Light.color = WithA(Color.white, a * SparkleAlpha * (1f - blend));
            PlaceLight(s.Light, s.Feet, size, 0f);
            s.LightB.sprite = _revealLight[(i0 + 1) % _revealLight.Length];
            s.LightB.color = WithA(Color.white, a * SparkleAlpha * blend);
            PlaceLight(s.LightB, s.Feet, size, 0f);
        }

        private static void PlaceLight(Image img, Vector2 root, float size, float angle)
        {
            var rt = img.rectTransform;
            rt.pivot = new Vector2(0.5f, LightRoot);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = root;
            rt.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        // 등장 자세 박자 — 늘어남(0~0.3) 뒤 0.25초에 자세 1 → 0.35초에 자세 2 를 잡고 1.1초까지 버틴다
        private const float PoseStart = 0.25f, PoseMid = 0.35f, PoseEnd = 1.1f;

        private static Sprite PoseSprite(Slot s, float tr)
        {
            if (s.Pose == null || s.Pose.Length == 0 || tr < PoseStart || tr >= PoseEnd) return s.Art;
            return s.Pose.Length == 1 || tr >= PoseMid ? s.Pose[s.Pose.Length - 1] : s.Pose[0];
        }

        private static Sprite[] PoseOf(SpriteAtlas atlas, string key)
        {
            if (atlas == null) return Array.Empty<Sprite>();
            var appear = atlas.GetSprite($"unitL_{key}_se_appear");
            if (appear != null) return new[] { appear };
            var a1 = atlas.GetSprite($"unitL_{key}_se_atk1");
            var a2 = atlas.GetSprite($"unitL_{key}_se_atk2");
            if (a1 == null || a2 == null) return Array.Empty<Sprite>();
            return new[] { a1, a2 };
        }

        /// <summary>화면 하양 — 1회 SSR · 대박만, 깨지기 0.15초 전부터 공개 뒤 0.25초까지(시험판).</summary>
        private void Flash(float t, Slot s)
        {
            float f = s.E.Tier < 2 ? 0f : t < s.RevealT ? Seg(t, s.RevealT - 0.15f, s.RevealT) : 1f - Seg(t, s.RevealT, s.RevealT + 0.25f);
            _flash.enabled = f > 0f;
            if (_flash.enabled) _flash.color = WithA(Color.white, 0.75f * f);
        }

        /// <summary>이름 판 — 왼쪽 밖에서 OutBack 0.45s.</summary>
        private void Banner(float u, Slot best)
        {
            bool on = u >= 0f && _plate != null;
            _plateImg.gameObject.SetActive(on);
            if (!on) return;
            float k = Ease.OutBack(Seg(u, 0f, 0.45f));
            _plateImg.rectTransform.anchoredPosition = new Vector2(Mathf.LerpUnclamped(-H * 0.5625f, 0f, k), -H * 0.3f);
            _tagText.text = Localize.Get(best.E.TagKey);
            _nameText.text = best.E.Name;
            _gradeText.text = best.E.GradeLabel;
            _tagImg.sprite = best.E.TagKey == "ui.summon.reveal.pickup" ? _ribbonPickup : best.E.TagKey == "ui.summon.reveal.new" ? _stampNew : null;
            _tagImg.enabled = _tagImg.sprite != null;
        }

        /// <summary>10회 아래 버튼 「확인 · 10회 더」 — 다 멈춘 뒤 0.4초에 밑에서 올라온다(시험판 footer).</summary>
        private void Footer(float u)
        {
            bool on = u >= 0f;
            _footer.gameObject.SetActive(on);
            if (!on) return;
            float k = Ease.OutCubic(Seg(u, 0f, 0.4f));
            _footer.anchoredPosition = new Vector2(0f, -H * 0.375f - P(80f) * (1f - k));
            if (_footer.TryGetComponent<CanvasGroup>(out var g)) g.alpha = k;
        }

        // ── 깨짐 파티클 ─────────────────────────────────────

        /// <summary>깨짐 알갱이 한 종류의 뿌리는 값. 길이 단위는 배양조 높이(tankH), 중력은 화면 높이(H)/초².</summary>
        private readonly struct BurstSpec
        {
            public readonly int Count;
            public readonly Vector2 Size, Speed, Life, SpawnX, Angle, Up;
            public readonly float Spin, FloorShare;
            public readonly Vector2 FloorAngle, FloorSize;

            public BurstSpec(int count, Vector2 size, Vector2 speed, Vector2 life, float spin, Vector2 spawnX, Vector2 angle, Vector2 up,
                             float floorShare = 0f, Vector2 floorAngle = default, Vector2 floorSize = default)
            {
                Count = count; Size = size; Speed = speed; Life = life; Spin = spin; SpawnX = spawnX; Angle = angle; Up = up;
                FloorShare = floorShare; FloorAngle = floorAngle; FloorSize = floorSize;
            }
        }

        // 시안(mock_summon_lab_free_peak) 공개 순간을 같은 자로 재서 맞춘 수 · 크기(2026-10-22, PD 「깨진 개수 차이도 크고 다르잖아」).
        // 큰 판은 유리창 가장자리에서 바깥으로 나가 몸을 덮지 않고, 부스러기 · 물방울이 몸 둘레를 채운다.
        // 터진 뒤 중력으로 떨어지고 끝(PD 10-23 「자연스럽게 내려오면 끝」) · 회전 · 떨어짐은 느리게(PD 10-23 「싸구려처럼 빠르게」).
        // 규칙 08 1-1 「한 번에 40 이하」를 넘는다 — 1회 좋음 공개 한 박자(가이드 guide_summon_lab.md 에 이유). 꽝 · 10회 칸은 그 비율만큼 줄인다
        private static readonly BurstSpec PlateSpec = new BurstSpec(26, new Vector2(0.07f, 0.19f), new Vector2(1.0f, 1.8f), new Vector2(1.6f, 2.1f), 50f,
                                                                    new Vector2(0.3f, 0.5f), new Vector2(10f, 75f), new Vector2(0.5f, 1f));
        private static readonly BurstSpec ChipSpec = new BurstSpec(75, new Vector2(0.012f, 0.035f), new Vector2(0.5f, 1.8f), new Vector2(1.3f, 1.8f), 90f,
                                                                   new Vector2(0f, 0.5f), new Vector2(5f, 88f), new Vector2(0.3f, 1f));
        private static readonly BurstSpec SplashSpec = new BurstSpec(60, new Vector2(0.03f, 0.09f), new Vector2(0.4f, 1.5f), new Vector2(1.1f, 1.5f), 30f,
                                                                     new Vector2(0f, 0.5f), new Vector2(10f, 80f), new Vector2(0.3f, 1f),
                                                                     0.3f, new Vector2(40f, 85f), new Vector2(0.05f, 0.1f));

        /// <summary>깨짐 — 유리 판 · 부스러기 · 배양액이 바깥 · 위로 튀었다가 중력으로 천천히 떨어진다 + 반짝이.</summary>
        private void Shatter(Slot s, Color c, float scale)
        {
            Burst(_shardPs, s, PlateSpec, scale);
            Burst(_chipPs, s, ChipSpec, scale);
            Burst(_splashPs, s, SplashSpec, scale);
            // 반짝이는 몸 둘레 바깥 양옆 · 머리 위에서 천천히 떠오른다 — 몸(글자 · 얼굴) 위를 덮지 않게
            float side = s.Cell * 0.42f;
            int k = Mathf.Max(1, Mathf.RoundToInt(4f * scale));
            Sparks(s.Feet + new Vector2(-side, s.Cell * 0.3f), c, k, 0.8f);
            Sparks(s.Feet + new Vector2(side, s.Cell * 0.3f), c, k, 0.8f);
            Sparks(s.Feet + new Vector2(0f, s.Cell * 0.75f), c, k, 0.8f);
        }

        private void Burst(ParticleSystem ps, Slot s, in BurstSpec q, float scale)
        {
            if (ps == null) return;
            int n = Mathf.Max(1, Mathf.RoundToInt(q.Count * scale));
            float tankH = s.TankH;
            for (int i = 0; i < n; i++)
            {
                float side = UnityEngine.Random.value < 0.5f ? -1f : 1f;
                bool floor = UnityEngine.Random.value < q.FloorShare;
                Vector2 p, d;
                if (floor)
                {
                    // 바닥 물보라 — 발 자리 받침에서 튀어 오른다
                    p = new Vector2(s.Feet.x + (UnityEngine.Random.value - 0.5f) * s.GlassSize.x, s.Feet.y + tankH * 0.02f);
                    float a = UnityEngine.Random.Range(q.FloorAngle.x, q.FloorAngle.y) * Mathf.Deg2Rad;
                    d = new Vector2(side * Mathf.Sin(a), Mathf.Cos(a));
                }
                else
                {
                    p = s.GlassCenter + new Vector2(side * UnityEngine.Random.Range(q.SpawnX.x, q.SpawnX.y) * s.GlassSize.x,
                                                    (UnityEngine.Random.value - 0.5f) * s.GlassSize.y);
                    float a = UnityEngine.Random.Range(q.Angle.x, q.Angle.y) * Mathf.Deg2Rad;   // 수직에서 바깥으로 기운 각
                    d = new Vector2(side * Mathf.Sin(a), Mathf.Cos(a) * UnityEngine.Random.Range(q.Up.x, q.Up.y));
                }
                var size = floor ? q.FloorSize : q.Size;
                var e = new ParticleSystem.EmitParams
                {
                    position = World(p),
                    velocity = World(p + d.normalized * tankH * UnityEngine.Random.Range(q.Speed.x, q.Speed.y)) - World(p),
                    startSize = WorldLen(tankH * UnityEngine.Random.Range(size.x, size.y)),
                    startLifetime = UnityEngine.Random.Range(q.Life.x, q.Life.y),
                    rotation = UnityEngine.Random.Range(0f, 360f),
                    angularVelocity = UnityEngine.Random.Range(-q.Spin, q.Spin),
                    startColor = Color.white,
                };
                ps.Emit(e, 1);
            }
        }

        private void ClearParticles()
        {
            if (_shardPs != null) _shardPs.Clear();
            if (_chipPs != null) _chipPs.Clear();
            if (_splashPs != null) _splashPs.Clear();
            if (_sparkPs != null) _sparkPs.Clear();
        }

        /// <summary>반짝이 — 사방으로 조금 퍼졌다가 천천히 떠오르며 사라진다(가산).</summary>
        private void Sparks(Vector2 at, Color c, int n, float power)
        {
            if (_sparkPs == null) return;
            var tint = Color.Lerp(c, Color.white, 0.35f);
            for (int i = 0; i < n; i++)
            {
                float a = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.6f + 0.4f);
                var e = new ParticleSystem.EmitParams
                {
                    position = World(at),
                    velocity = World(at + dir * H * UnityEngine.Random.Range(0.04f, 0.1f) * power) - World(at),
                    startSize = WorldLen(H * UnityEngine.Random.Range(0.012f, 0.026f)),
                    startLifetime = UnityEngine.Random.Range(0.6f, 1.2f),
                    startColor = tint,
                };
                _sparkPs.Emit(e, 1);
            }
        }

        private Vector3 World(Vector2 local) => _shake.TransformPoint(local);
        private float WorldLen(float local) => local * _shake.lossyScale.x;

        private ParticleSystem NewParticles(string name, Texture2D tex, Material baseMat, Vector2Int tiles, float gravityPerH, float drag, int max, float fadeFrom)
        {
            var go = new GameObject(name);
            go.layer = _root.gameObject.layer;
            go.transform.SetParent(_root, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.useUnscaledTime = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.maxParticles = max;
            // 중력 — 화면 높이 기준(캔버스가 카메라 앞 월드에 있어 월드 단위로 바꾼다)
            main.gravityModifier = gravityPerH * (H * _shake.lossyScale.y) / 9.81f;
            var emission = ps.emission; emission.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, fadeFrom), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true; limit.drag = drag;
            limit.multiplyDragByParticleSize = false; limit.multiplyDragByParticleVelocity = false;
            int tilesX = tiles.x, tilesY = tiles.y;
            if (tilesX * tilesY > 1)
            {
                var sheet = ps.textureSheetAnimation;
                sheet.enabled = true;
                sheet.mode = ParticleSystemAnimationMode.Grid;
                sheet.numTilesX = tilesX; sheet.numTilesY = tilesY;
                sheet.animation = ParticleSystemAnimationType.WholeSheet;
                sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
                sheet.startFrame = new ParticleSystem.MinMaxCurve(0f, (tilesX * tilesY - 0.01f) / (tilesX * tilesY));
            }
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.alignment = ParticleSystemRenderSpace.View;
            rend.sortingLayerID = _canvas.sortingLayerID;
            rend.sortingOrder = _canvas.sortingOrder + 1;   // 배양실 그림 위 · 이름 판도 위(조각은 판이 들어오기 전에 사라진다)
            rend.sharedMaterial = new Material(baseMat) { hideFlags = HideFlags.DontSave, mainTexture = tex };
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            ps.Play();
            return ps;
        }

        // ── 노드 ─────────────────────────────────────────────

        private Slot MakeSlot(Entry e, Sprite art, Sprite[] pose, Sprite ghost, List<Sprite> pool, bool ten)
        {
            var s = new Slot { E = e, Art = art, Pose = pose, Ten = ten };
            // 룰렛 실루엣 — 다른 몸들을 섞고, 첫 칸이 진짜가 아니게
            var seed = new List<Sprite>();
            for (int i = 0; i < 40; i++)
            {
                Sprite pick = pool[UnityEngine.Random.Range(0, pool.Count)];
                if (pool.Count > 1 && i == 0) while (pick == art) pick = pool[UnityEngine.Random.Range(0, pool.Count)];
                seed.Add(pick);
            }
            s.Seed = seed.ToArray();
            s.Root = Layer($"Slot{_slots.Count}", _shake, true);
            s.Tank = Img("Tank", s.Root, null);
            s.Broken = Img("Broken", s.Root, null);
            // 공개 빛 — 깨진 배양조 앞 · 몸 바로 뒤(빛살 두 장 + 제자리 반짝 두 장)
            s.Rays = Img("Rays", s.Root, _additive);
            s.Rays2 = Img("Rays2", s.Root, _additive);
            s.Light = Img("Light", s.Root, _additive);
            s.LightB = Img("LightB", s.Root, _additive);
            s.Aura = Img("Aura", s.Root, _softGlow);
            s.Body = Img("Body", s.Root, null);
            s.White = Img("White", s.Root, null);
            s.Crack = Img("Crack", s.Root, null);
            if (ten)
            {
                s.ReachText = Text(s.Root, "Reach", Vector2.zero, new Vector2(0.18f, 0.03f), 32f, Gold);
                s.ReachText.alignment = TextAlignmentOptions.Center;
                s.ReachText.text = Localize.FromTable("ui.summon.reveal.reach", "리치!");
                s.ReachText.gameObject.SetActive(false);
                MakeLabel(s);
            }
            else
            {
                // 유령 묶음 — 배양조 뒤로 돌 땐 맨 뒤로 옮긴다(나선)
                s.GhostLayer = Layer("GhostLayer", s.Root, true);
                s.Trail = new Image[6];
                for (int i = s.Trail.Length - 1; i >= 0; i--) { s.Trail[i] = Img($"Trail{i}", s.GhostLayer, _additive); s.Trail[i].sprite = ghost; }
                s.GhostGlow = Img("GhostGlow", s.GhostLayer, _softGlow);
                s.GhostImg = Img("Ghost", s.GhostLayer, null);
                s.GhostImg.sprite = ghost;
                s.GhostImg.enabled = true;
                s.GhostLayer.gameObject.SetActive(false);
            }
            if (s.GhostImg == null)
            {
                // 10회 칸엔 유령이 없다 — 같은 자리 계산(Place)이 쓰는 빈 그림만 둔다
                s.GhostImg = Img("Ghost", s.Root, null);
                s.GhostGlow = Img("GhostGlow", s.Root, null);
                s.Trail = Array.Empty<Image>();
            }
            return s;
        }

        /// <summary>10회 칸 밑 이름판 — 이름 · 조각 ×n(정수 ×n) · 새 몸 도장. 시험판 label(128 × 52).</summary>
        private void MakeLabel(Slot s)
        {
            var plate = Img("Label", s.Root, null);
            s.Label = plate.rectTransform;
            s.LabelImg = plate;
            plate.preserveAspect = false;
            plate.sprite = s.E.Good ? _labelPlateGood : _labelPlate;
            plate.type = plate.sprite != null && plate.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            plate.enabled = plate.sprite != null;
            s.Label.sizeDelta = new Vector2(P(128f), P(52f));
            s.LabelName = Text(s.Label, "Name", new Vector2(0f, 0.2f), new Vector2(0.92f, 0.42f), P(15f), Color.white);
            s.LabelName.alignment = TextAlignmentOptions.Center;
            s.LabelName.text = s.E.Name;
            s.LabelCount = Text(s.Label, "Count", new Vector2(0f, -0.2f), new Vector2(0.92f, 0.42f), P(15f), TierColor[s.E.Tier]);
            s.LabelCount.alignment = TextAlignmentOptions.Center;
            s.LabelCount.text = s.E.Essence > 0 ? string.Format(Localize.FromTable("ui.rb.summon.card_essence", "정수 ×{0}"), s.E.Essence)
                                                : string.Format(Localize.FromTable("ui.rb.summon.card_shards", "조각 ×{0}"), s.E.Shards);
            var stamp = Child(s.Label, "New", new Vector2(0.34f, 0.5f), new Vector2(0.39f, 0.63f));
            stamp.sprite = _stampNew;
            stamp.enabled = s.E.IsNew && _stampNew != null;
            s.LabelNew = stamp;
            s.Label.gameObject.SetActive(false);
        }

        private void ClearSlots()
        {
            foreach (var s in _slots) if (s.Root != null) Destroy(s.Root.gameObject);
            _slots.Clear();
        }

        private void Build()
        {
            if (_root != null) return;
            // 파티클 시스템은 Overlay 캔버스에 안 그려진다 — 배양실만 메인 카메라 캔버스(Screen Space - Camera)로 따로 세운다.
            // 크기 기준은 로비 캔버스와 같게(720×1280 · 다 들어오는 쪽으로 맞춤, 규칙 05).
            _lobbyCanvas = GetComponentInParent<Canvas>() != null ? GetComponentInParent<Canvas>().rootCanvas : null;
            var cgo = new GameObject("SummonRevealCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.layer = gameObject.layer;
            _canvas = cgo.GetComponent<Canvas>();
            var cam = Camera.main;
            _canvas.renderMode = cam != null ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
            _canvas.worldCamera = cam;
            _canvas.planeDistance = cam != null ? Mathf.Max(cam.nearClipPlane + 1f, 5f) : 100f;
            _canvas.sortingOrder = 50;
            var scaler = cgo.GetComponent<CanvasScaler>();
            var from = _lobbyCanvas != null ? _lobbyCanvas.GetComponent<CanvasScaler>() : null;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = from != null ? from.referenceResolution : new Vector2(720f, 1280f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            float sw = Screen.width / scaler.referenceResolution.x, sh = Screen.height / scaler.referenceResolution.y;
            scaler.matchWidthOrHeight = sh < sw ? 1f : 0f;
            Canvas.ForceUpdateCanvases();
            _root = Layer("SummonReveal", (RectTransform)cgo.transform, true);
            _root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 1f);   // 바탕 + 누르기 받는 칸
            var btn = _root.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => _tapped = true);
            _shake = Layer("Shake", _root, true);
            _bg = Img("LabStage", _shake, null);
            _bg.preserveAspect = false;
            var bgr = _bg.rectTransform;
            bgr.anchorMin = Vector2.zero; bgr.anchorMax = Vector2.one; bgr.offsetMin = bgr.offsetMax = Vector2.zero;
            var fit = _bg.gameObject.AddComponent<AspectRatioFitter>();   // 화면을 덮는다(4:3 · 20:9 빈 띠 없이)
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 720f / 1280f;
            _bg.sprite = _stage; _bg.enabled = _stage != null;
            _dim = Img("Dim", _shake, null);
            var dr = _dim.rectTransform; dr.anchorMin = Vector2.zero; dr.anchorMax = Vector2.one; dr.offsetMin = dr.offsetMax = Vector2.zero;
            _dim.enabled = true;
            _flash = Img("Flash", _root, null);
            var fr = _flash.rectTransform; fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = fr.offsetMax = Vector2.zero;
            _flash.sprite = null;

            _plateImg = Img("Plate", _root, null);
            _plateImg.sprite = _plate;
            _plateImg.enabled = true;
            float ph = H * 0.13f;
            _plateImg.rectTransform.sizeDelta = _plate != null ? new Vector2(ph * _plate.rect.width / _plate.rect.height, ph) : new Vector2(ph * 3.3f, ph);
            var pr = _plateImg.rectTransform;
            _tagImg = Child(pr, "TagImg", new Vector2(0.4f, 0.42f), new Vector2(0.17f, 0.42f));
            _tagText = Text(pr, "TagText", new Vector2(-0.285f, 0.36f), new Vector2(0.27f, 0.2f), 30f, new Color(1f, 0.84f, 0.32f));
            _tagText.alignment = TextAlignmentOptions.Center;
            _nameText = Text(pr, "NameText", new Vector2(-0.04f, -0.04f), new Vector2(0.78f, 0.34f), 46f, Color.white);
            _gradeText = Text(pr, "GradeText", new Vector2(-0.2f, -0.32f), new Vector2(0.46f, 0.17f), 22f, new Color(0.8f, 0.85f, 0.96f));
            _plateImg.gameObject.SetActive(false);
            _tapText = Text(_root, "TapText", new Vector2(0f, -0.44f), new Vector2(0.8f, 0.04f), 24f, new Color(0.88f, 0.86f, 0.98f));
            _tapText.alignment = TextAlignmentOptions.Center;
            _tapText.text = Localize.Get("ui.summon.reveal.tap");
            BuildFooter();
            Localize.ApplyFonts(_root);
            if (_canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                // 검은 바탕 유리 · 물 그림은 가산 — 속이 비쳐 뒤 금빛이 보인다(시안). 중력 · 회전은 느리게(PD 10-23)
                var glassMat = _shardAdditive && _additive != null ? _additive : Canvas.GetDefaultCanvasMaterial();
                if (_shardSheet != null) _shardPs = NewParticles("ShardParticles", _shardSheet, glassMat, _shardGrid, 0.8f, 1.2f, 160, 0.85f);
                if (_chipSheet != null) _chipPs = NewParticles("ChipParticles", _chipSheet, glassMat, _chipGrid, 0.8f, 1.2f, 240, 0.85f);
                if (_splashSheet != null && _additive != null) _splashPs = NewParticles("SplashParticles", _splashSheet, _additive, _splashGrid, 1.0f, 1.2f, 200, 0.85f);
                // 반짝이 — 위로 천천히 떠오른다(유령 꼬리 · 깨짐)
                if (_spark != null && _additive != null) _sparkPs = NewParticles("SparkParticles", _spark, _additive, Vector2Int.one, -0.08f, 0.8f, 300, 0.5f);
            }
        }

        /// <summary>10회 아래 버튼 — 확인(남색) · 10회 더(노랑, 값은 소환 쪽이 넘긴다). 시험판 footer 자리(위에서 1120 · 두 버튼 170 · 550).</summary>
        private void BuildFooter()
        {
            _footer = Layer("Footer", _root, false);
            _footer.anchorMin = _footer.anchorMax = new Vector2(0.5f, 0.5f);
            _footer.sizeDelta = new Vector2(U, P(92f));
            _footer.gameObject.AddComponent<CanvasGroup>();
            var ok = FooterButton("Ok", _btnOk, -U * 190f / 720f, () => _ok = true);
            var okText = Text(ok, "Text", Vector2.zero, new Vector2(0.9f, 0.7f), P(32f), Color.white);
            okText.alignment = TextAlignmentOptions.Center;
            okText.text = Localize.Get("ui.common.ok");
            var again = FooterButton("Again", _btnAgain, U * 190f / 720f, () => _again = true);
            _againText = Text(again, "Text", Vector2.zero, new Vector2(0.9f, 0.7f), P(32f), new Color(0.19f, 0.13f, 0.03f));
            _againText.alignment = TextAlignmentOptions.Center;
            _footer.gameObject.SetActive(false);
        }

        private RectTransform FooterButton(string name, Sprite sprite, float x, UnityEngine.Events.UnityAction onClick)
        {
            var rt = Layer(name, _footer, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(U * 300f / 720f, P(92f));
            rt.anchoredPosition = new Vector2(x, 0f);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(onClick);
            b.onClick.AddListener(() => GameSound.Cue("ui.tap"));
            return rt;
        }

        // ── 도구 ─────────────────────────────────────────────

        private static float Seg(float t, float a, float b) => b <= a ? (t >= b ? 1f : 0f) : Mathf.Clamp01((t - a) / (b - a));
        private static Color WithA(Color c, float a) { c.a = a; return c; }

        private static void Show(Image img, Sprite s, float alpha)
        {
            img.enabled = s != null && alpha > 0f;
            if (!img.enabled) return;
            img.sprite = s;
            img.color = WithA(Color.white, alpha);
        }

        private static RectTransform Layer(string name, RectTransform parent, bool stretch)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            if (stretch) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
            return rt;
        }

        private static Image Img(string name, RectTransform parent, Material mat)
        {
            var rt = Layer(name, parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            var img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            img.material = mat;
            img.enabled = false;
            return img;
        }

        private static Image Child(RectTransform parent, string name, Vector2 pos, Vector2 size)
        {
            var rt = Layer(name, parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.Scale(size, parent.sizeDelta);
            rt.anchoredPosition = Vector2.Scale(pos, parent.sizeDelta);
            var img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            return img;
        }

        private TextMeshProUGUI Text(RectTransform parent, string name, Vector2 pos, Vector2 size, float fontSize, Color c)
        {
            var rt = Layer(name, parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            var basis = parent == _root || parent.sizeDelta == Vector2.zero ? new Vector2(_root.rect.width, _root.rect.height) : parent.sizeDelta;
            rt.sizeDelta = Vector2.Scale(size, basis);
            rt.anchoredPosition = Vector2.Scale(pos, basis);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (_font != null) t.font = _font;
            t.fontSize = fontSize;
            t.fontSizeMin = fontSize * 0.5f;
            t.fontSizeMax = fontSize;
            t.enableAutoSizing = true;
            t.color = c;
            t.alignment = TextAlignmentOptions.Left;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.gameObject.AddComponent<HeavyText>();
            return t;
        }
    }
}
