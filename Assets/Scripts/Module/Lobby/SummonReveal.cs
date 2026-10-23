using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Module.Common;
using Game.Module.Common.UI;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Resource;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 소환 연출 「영혼 배양실」 — 가이드 `Projects/AVSR/_exchange/guide_summon_lab_v2.md`(PD 2026-10-23).
    ///
    /// 1회: 영혼이 관 끝에서 천천히 날아와 배양조에 들어감 → 배양액이 하늘색으로 차오름 → 등급만큼 한 단계씩 승격
    /// (기포가 끓는 모으기 → 하얀 번쩍 · 고리 파동과 함께 배양액 · 무대 빛이 통째로 다음 색: 하늘 → 보라 → 금 → 무지개, 레퍼런스 gacha_reveal_2)
    /// → 최종 단계 전엔 기포가 잦아드는 「가짜 뜸」(금 이상) → 금이 가고 깨짐 → 등장 자세 → 이름 판 → 탭.
    /// 10회: 10칸에 영혼이 떨어져 각자 최종 색으로 바로 물듦(승격 없음) → 금 이상 몸만 한 명씩 1회 연출(높은 것부터, 탭으로 다음)
    /// → 끝나면 패널이 다 열린 결과판을 띄운다. 금 이상이 없으면 짧은 공통 연출만.
    /// 빠르게 번쩍이는 것은 승격 순간뿐 — 나머지는 느리게 숨 쉬듯(PD 「싸구려처럼 빠르게 움직이고, 은은하게」).
    /// </summary>
    public sealed class SummonReveal : MonoBehaviour
    {
        /// <summary>뽑기 사다리 4단(PD 2026-10-23) — 하늘 B · 보라 A · 금 A 많은 조각 · 픽업 · 무지개 대박.</summary>
        public enum Rarity { Normal, Rare, Super, Jackpot }

        public readonly struct Entry
        {
            public readonly string HostKey, Name, GradeLabel, TagKey;
            public readonly Rarity Rarity;
            public Entry(string hostKey, string name, string gradeLabel, string tagKey, Rarity rarity)
            {
                HostKey = hostKey; Name = name; GradeLabel = gradeLabel; TagKey = tagKey; Rarity = rarity;
            }
        }

        [SerializeField] private Sprite _stage, _tank, _tankBroken, _tankGlass, _plate;
        /// <summary>단계별 배양액 유리 — 하늘 · 보라 · 금 · 흰금(받은 유리의 색상만 돌린 판, Tools/prep_summon_tiers.py).</summary>
        [SerializeField] private Sprite[] _tankGlassTier = Array.Empty<Sprite>();
        /// <summary>단계별 배양조 — 유리창 안 배양액만 하늘 · 보라 · 금 · 흰금으로 돌린 판(받침 불빛은 그대로). 승격 때 0.2초에 바꿔 낀다.</summary>
        [SerializeField] private Sprite[] _tankTier = Array.Empty<Sprite>();
        /// <summary>공개 빛 — 몸 뒤 빛줄기 루프(검은 바탕 가산). 단계별: 하늘 · 보라 · 금(원본) · 무지개(납품).</summary>
        [SerializeField] private Sprite[] _revealLight = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _revealLightSr = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _revealLightR = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _revealLightRainbow = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _crack = Array.Empty<Sprite>();
        /// <summary>
        /// 깨짐 알갱이 시트 — 유리 판(큰 · 중간) · 유리 부스러기 · 배양액 물방울, 반짝이 · 기포(`lab_spark`). **파티클 시스템**으로 뿌린다.
        /// 검은 바탕 그림이면 가산(속이 비쳐 뒤 빛이 보인다).
        /// </summary>
        [SerializeField] private Texture2D _shardSheet, _chipSheet, _splashSheet, _spark;
        [SerializeField] private Vector2Int _shardGrid = new Vector2Int(3, 2), _chipGrid = new Vector2Int(1, 1), _splashGrid = new Vector2Int(3, 2);
        [SerializeField] private bool _shardAdditive;
        /// <summary>승격 고리 파동 — 흰 고리(등급색으로 물들인다).</summary>
        [SerializeField] private Sprite[] _ring = Array.Empty<Sprite>();
        [SerializeField] private Sprite _stampNew, _ribbonPickup;
        [SerializeField] private Material _additive, _silhouette;
        /// <summary>둥근 부드러운 빛 — 그림 없이 셰이더가 그린다(`UISoftGlow.mat`, UI/AdditiveLight 고리 반지름 0).</summary>
        [SerializeField] private Material _softGlow;
        [SerializeField] private TMP_FontAsset _font;

        // ── 박자 (가이드 v2 박자표) ──
        // 1회: 배양조 떠오름 0.6 · 영혼 비행 0.8~1.8(끝만 가속) · 차오름 0.4 · 뜸 0.6 · 모으기 0.5 · 가짜 뜸 0.5 · 금 0.6 · 눈 0.2
        private const float AppearDur = 0.6f, FlyFrom = 0.8f, FlyTo = 1.8f, FillDur = 0.4f, HoldDur = 0.6f;
        private const float ChargeDur = 0.5f, FakeDur = 0.5f, CrackDur = 0.6f, EyesDur = 0.2f;
        // 10회 겉보기: 영혼이 위에서 칸마다 0.06초 간격으로 떨어짐(0.45초) → 마지막 +0.8 에 끝
        private const float MultiFly0 = 0.3f, MultiGap = 0.06f, MultiFly = 0.45f, MultiHold = 0.8f;
        private static readonly float[] Amp = { 1.5f, 2.5f, 4f, 6f };                 // 모으기 떨림 px @ 640 높이
        private static readonly Color[] TierColor =
            { new(0.35f, 0.82f, 1f), new(0.73f, 0.55f, 1f), new(1f, 0.79f, 0.3f), new(1f, 0.93f, 0.7f) };   // 하늘 · 보라 · 금 · 흰금(무지개 바탕)
        private static readonly Color SilhouetteColor = new(0.04f, 0.05f, 0.13f, 1f);
        private const float InkOfCell = 76f / 128f, FootFromBottom = 24f / 128f;

        private sealed class Slot
        {
            public Entry E;
            public Sprite Art, Ghost;
            /// <summary>등장 자세 — 전용 그림(unitL_{키}_se_appear)이 있으면 그것, 없으면 원작 공격 자세 atk1 → atk2.</summary>
            public Sprite[] Pose;
            public RectTransform Root;
            public Image Aura, Tank, TankB, Broken, Light, LightB, Light2, Body, White, Glass, Crack, Core, GhostImg, GhostGlow, Ring;
            public Vector2 GlassCenter, GlassSize;
            public bool ImpactFx, RevealFx;
            public int UpFx;
            public Image[] Trail;
            public Vector2 Feet, Chest, From, Ctrl;
            public float FlyStart, FlyEnd, ImpactT, CrackT, EyesT, FlashT = -1f, RevealT = float.MaxValue, Cell, LastT, BubbleAcc;
            /// <summary>승격 순간 · 모으기 시작(1 단계부터). 0 단계(하늘)는 차오를 때.</summary>
            public float[] UpT = Array.Empty<float>(), ChargeT = Array.Empty<float>();
            public bool Best, Overview;
        }

        private RectTransform _root, _shake;
        // 파티클은 Overlay 캔버스 위에 안 그려진다 — 배양실만 메인 카메라 캔버스로 따로 띄우고, 도는 동안 로비 캔버스를 잠깐 숨긴다
        private Canvas _canvas, _lobbyCanvas;
        private ParticleSystem _shardPs, _chipPs, _splashPs, _sparkPs;
        private Image _dim, _bg, _flash, _plateImg, _tagImg;
        private TextMeshProUGUI _tagText, _nameText, _gradeText, _tapText;
        private readonly List<Slot> _slots = new();
        private bool _tapped;
        private int _run;   // 새 연출이 시작되면 앞의 것은 그 자리에서 멈춘다(같은 판을 둘이 그리지 않게)

        /// <summary>
        /// 뽑은 몸들을 공개한다. 1명 = 큰 배양조 하나(승격 연출 · 탭 대기).
        /// 여럿 = 10칸 겉보기 뒤 금 이상만 한 명씩 1회 연출(탭 대기) — 나머지는 이 뒤 패널 결과판이 보여 준다.
        /// 큰 몸 그림이 없는 몸은 건너뛴다.
        /// </summary>
        public async UniTask PlayAsync(IReadOnlyList<Entry> entries, CancellationToken ct)
        {
            if (entries == null || entries.Count == 0 || !CoreModule.TryGet<IResourceManager>(out var res)) return;
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
                for (int i = 0; i < entries.Count; i++)
                {
                    var atlas = await Load(res, entries[i].HostKey, loaded);
                    var art = atlas != null ? atlas.GetSprite($"unitL_{entries[i].HostKey}_se") : null;
                    if (art != null) arts.Add((entries[i], art, PoseOf(atlas, entries[i].HostKey)));
                }
                if (arts.Count == 0 || run != _run) return;
                Build();
                if (entries.Count > 1)
                {
                    ClearSlots();
                    foreach (var a in arts) _slots.Add(MakeSlot(a.e, a.art, ghost, a.pose));
                    OverviewTimeline();
                    await RunAsync(run, ct);
                    // 금 이상만 한 명씩 — 높은 것부터(같으면 뽑은 순서)
                    var stars = arts.FindAll(a => a.e.Rarity >= Rarity.Super);
                    stars.Sort((x, y) => y.e.Rarity.CompareTo(x.e.Rarity));
                    foreach (var a in stars)
                    {
                        if (run != _run || ct.IsCancellationRequested) return;
                        await SingleAsync(run, a.e, a.art, a.pose, ghost, ct);
                    }
                }
                else await SingleAsync(run, arts[0].e, arts[0].art, arts[0].pose, ghost, ct);
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

        private async UniTask SingleAsync(int run, Entry e, Sprite art, Sprite[] pose, Sprite ghost, CancellationToken ct)
        {
            ClearSlots();
            ClearParticles();
            _slots.Add(MakeSlot(e, art, ghost, pose));
            SingleTimeline();
            await RunAsync(run, ct);
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

        // ── 배치 · 박자 ──────────────────────────────────────

        private float H => _root.rect.height;
        private float U => Mathf.Min(_root.rect.width, H * 720f / 1280f);   // 설계 폭(720 기준) — 4:3 · 20:9 에서도 같은 판

        private void SingleTimeline()
        {
            var s = _slots[0];
            s.Best = true;
            int tier = (int)s.E.Rarity;
            s.FlyStart = FlyFrom; s.FlyEnd = FlyTo; s.ImpactT = FlyTo;
            float t = FlyTo + FillDur + HoldDur;
            s.UpT = new float[tier + 1];
            s.ChargeT = new float[tier + 1];
            s.UpT[0] = FlyTo;
            for (int k = 1; k <= tier; k++)
            {
                // 금 이상의 마지막 단계 앞에선 기포가 잦아드는 가짜 뜸 — 「끝났나?」 하고 한 번 죽였다 끓어오른다
                if (k == tier && tier >= (int)Rarity.Super) t += FakeDur;
                s.ChargeT[k] = t;
                t += ChargeDur;
                s.UpT[k] = t;
                t += HoldDur;
            }
            s.CrackT = t;
            s.EyesT = t + CrackDur;
            if (s.E.Rarity == Rarity.Jackpot) s.FlashT = s.EyesT + 0.15f;
            s.RevealT = s.EyesT + (s.FlashT > 0f ? 0.3f : EyesDur);
            Place(s, new Vector2(0f, -H * 0.185f), H * 0.5f, H * 0.27f);   // 배양조 밑이 무대 받침대(위에서 77%)에 앉게
            s.From = new Vector2(-U * 0.40f, -H * 0.38f);   // 무대 왼쪽 아래 관 끝
            s.Ctrl = new Vector2(-U * 0.45f, H * 0.05f);
        }

        private void OverviewTimeline()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                var s = _slots[i];
                s.Overview = true;
                s.FlyStart = MultiFly0 + i * MultiGap; s.FlyEnd = s.FlyStart + MultiFly; s.ImpactT = s.FlyEnd;
                // 승격 없이 떨어진 순간 최종 색으로 — 금 이상 칸이 보이면 「뭔가 있다」
                s.UpT = new[] { s.ImpactT };
                int c = i % 5, row = i / 5;
                Place(s, new Vector2((-2 + c) * U * 0.189f, H * (0.08f - row * 0.27f)), H * 0.2f, H * 0.11f);
                s.From = s.Chest + new Vector2(0f, H * 0.55f);
                s.Ctrl = s.Chest + new Vector2(0f, H * 0.25f);
            }
        }

        private float EndTime()
        {
            if (_slots[0].Overview) return _slots[_slots.Count - 1].ImpactT + MultiHold;
            return _slots[0].RevealT + 1.0f;
        }

        /// <summary>배양조 · 몸 자리. feet = 몸 발(배양조 유리 바닥), tankH = 배양조 높이, figure = 몸 키.</summary>
        private void Place(Slot s, Vector2 feet, float tankH, float figure)
        {
            s.Feet = feet;
            s.Cell = figure / InkOfCell;
            s.Chest = feet + new Vector2(0f, figure * 0.62f);
            float tw = _tank != null ? tankH * _tank.rect.width / _tank.rect.height : tankH * 0.6f;
            // 배양조 그림의 유리창 바닥 = 그림 아래에서 15% — 몸 발이 그 위에 선다(lab_tank 실측 2026-10-22)
            var tankCenter = feet + new Vector2(0f, tankH * 0.33f);
            foreach (var img in new[] { s.Tank, s.TankB, s.Broken, s.Glass, s.Crack })
            {
                img.rectTransform.sizeDelta = new Vector2(tw, tankH);
                img.rectTransform.anchoredPosition = tankCenter;
            }
            // 유리창 = 배양조 그림 안 가로 21~79% · 세로 21~85%(lab_tank_glass 실측) — 조각 · 기포가 여기서 나온다
            s.GlassSize = new Vector2(tw * 0.58f, tankH * 0.64f);
            s.GlassCenter = tankCenter + new Vector2(0f, tankH * (0.5f - 0.53f));
            // 배양액 빛 — 유리창 안을 채우는 부드러운 등급색(차오름 · 승격 컷)
            s.Aura.preserveAspect = false;
            s.Aura.rectTransform.sizeDelta = new Vector2(s.GlassSize.x * 1.5f, s.GlassSize.y * 1.25f);
            s.Aura.rectTransform.anchoredPosition = s.GlassCenter;
            foreach (var img in new[] { s.Body, s.White })
            {
                img.rectTransform.sizeDelta = new Vector2(s.Cell, s.Cell);
                img.rectTransform.anchoredPosition = feet - new Vector2(0f, s.Cell * FootFromBottom);
            }
            float core = figure * 0.35f;
            s.Core.rectTransform.sizeDelta = new Vector2(core, core);
            s.Core.rectTransform.anchoredPosition = s.Chest;
            s.Ring.rectTransform.sizeDelta = new Vector2(tankH * 0.44f, tankH * 0.22f);
            s.Ring.rectTransform.anchoredPosition = s.Chest;
            float g = figure * 0.32f;
            s.GhostImg.rectTransform.sizeDelta = new Vector2(g, g);
            s.GhostGlow.rectTransform.sizeDelta = new Vector2(g * 2f, g * 2f);
            foreach (var t in s.Trail) t.rectTransform.sizeDelta = new Vector2(g, g);
        }

        /// <summary>지금 몇 단계 색인가(-1 = 아직 영혼이 안 들어감).</summary>
        private static int TierAt(Slot s, float t)
        {
            int k = -1;
            for (int i = 0; i < s.UpT.Length; i++) if (t >= s.UpT[i]) k = i;
            if (s.Overview && k >= 0) k = (int)s.E.Rarity;
            return k;
        }

        /// <summary>단계 색 — 무지개는 천천히 색이 돈다.</summary>
        private static Color ColorOf(int tier, float t)
        {
            if (tier < (int)Rarity.Jackpot) return TierColor[Mathf.Max(0, tier)];
            // 색을 통째로 돌리면 보라 · 하늘을 지날 때 「단계가 내려간」 것처럼 보인다(녹화 10-23) — 흰금 바탕에 35% 만 섞는다
            return Color.Lerp(TierColor[(int)Rarity.Jackpot], Color.HSVToRGB(Mathf.Repeat(t * 0.15f, 1f), 0.6f, 1f), 0.35f);
        }

        // ── 진행 ─────────────────────────────────────────────

        private async UniTask RunAsync(int run, CancellationToken ct)
        {
            _canvas.gameObject.SetActive(true);
            if (_lobbyCanvas != null && _canvas.renderMode == RenderMode.ScreenSpaceCamera) _lobbyCanvas.enabled = false;   // 위에 그려지지 않게
            _root.gameObject.SetActive(true);
            _plateImg.gameObject.SetActive(false);
            _tapText.gameObject.SetActive(false);
            _flash.enabled = false;
            _tapped = false;
            var best = _slots[0];
            foreach (var s in _slots) if (s.E.Rarity > best.E.Rarity) best = s;
            bool overview = best.Overview;
            float end = EndTime();
            float start = Time.unscaledTime;
            int cuedUp = 0;
            bool cuedImpact = false, cuedReveal = false;
            GameSound.Cue("ui.play");
            while (!ct.IsCancellationRequested && run == _run)
            {
                float t = Time.unscaledTime - start;
                if (_tapped)
                {
                    _tapped = false;
                    if (overview) { if (t >= MultiFly0 + 0.4f) break; }   // 겉보기는 누르면 바로 다음으로
                    else if (t >= end) break;
                    else if (t < best.RevealT + 0.25f) { start = Time.unscaledTime - (best.RevealT + 0.25f); continue; }   // 공개 직후로
                }
                if (overview && t >= end) break;
                if (!cuedImpact && t >= best.ImpactT) { cuedImpact = true; GameSound.Cue("hit.reflect"); }
                while (!overview && cuedUp + 1 < best.UpT.Length && t >= best.UpT[cuedUp + 1]) { cuedUp++; GameSound.Cue("event.levelup"); }
                if (!overview && !cuedReveal && t >= best.RevealT)
                {
                    cuedReveal = true;
                    GameSound.Cue(best.E.Rarity >= Rarity.Super ? "run.card" : "run.gold");
                }

                float revealU = overview ? 0f : Seg(t, best.RevealT, best.RevealT + 0.5f);
                // 화면: 딤 0.35(0.4s) → 공개 때 천천히 걷힘
                _dim.color = new Color(0f, 0f, 0f, 0.35f * Seg(t, 0f, 0.4f) * (1f - revealU));
                if (!overview)
                {
                    RevealLight(t, best, revealU);
                    Flash(t, best);
                }
                // 대박 마지막 모으기 ~ 공개: 화면이 살짝 흔들린다
                float shake = !overview && best.E.Rarity == Rarity.Jackpot && t > best.ChargeT[best.ChargeT.Length - 1] && t < best.RevealT
                    ? 2.5f * (H / 640f) * Seg(t, best.ChargeT[best.ChargeT.Length - 1], best.RevealT) : 0f;
                _shake.anchoredPosition = new Vector2(Mathf.Sin(t * 41f) * shake, Mathf.Cos(t * 33f) * shake);

                foreach (var s in _slots) Draw(s, t);
                if (!overview)
                {
                    Banner(t - (best.RevealT + 0.6f), best);
                    _tapText.gameObject.SetActive(t >= best.RevealT + 1.2f);
                    if (_tapText.gameObject.activeSelf) _tapText.alpha = 0.6f + 0.4f * Mathf.Sin(t * 2.5f);
                }
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        private void Draw(Slot s, float t)
        {
            float dt = Mathf.Clamp(t - s.LastT, 0f, 0.1f);
            s.LastT = t;
            int final = (int)s.E.Rarity;
            int tier = TierAt(s, t);
            Color c = ColorOf(tier, t);
            float px = H / 640f;
            // 빈 몸 · 배양조 등장(1회 0.6s · 10회 0.05초 간격 0.4s, OutCubic)
            int idx = _slots.IndexOf(s);
            float in0 = s.Overview ? 0.05f * idx : 0f;
            float inU = Ease.OutCubic(Seg(t, in0, in0 + (s.Overview ? 0.4f : AppearDur)));
            bool revealed = t >= s.RevealT;

            s.Root.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, inU);
            Show(s.Broken, _tankBroken, revealed ? 1f : 0f);

            // 지금 단계 · 다음 승격까지 — 모으기(끓음) · 가짜 뜸(잦아듦) · 승격 직후 번쩍
            float charge = 0f, sinceUp = 99f, calm = 0f;
            for (int k = 1; k < s.UpT.Length; k++)
            {
                if (t >= s.ChargeT[k] && t < s.UpT[k]) charge = Seg(t, s.ChargeT[k], s.UpT[k]);
                if (k == s.UpT.Length - 1 && final >= (int)Rarity.Super && t >= s.ChargeT[k] - FakeDur && t < s.ChargeT[k])
                    calm = Seg(t, s.ChargeT[k] - FakeDur, s.ChargeT[k]);
            }
            for (int k = 0; k < s.UpT.Length; k++) if (t >= s.UpT[k]) sinceUp = t - s.UpT[k];
            float pop = tier >= 1 && !s.Overview ? 1f - Seg(sinceUp, 0f, 0.5f) : 0f;   // 승격 직후 0.5초 밝아졌다 가라앉음
            float fill = tier >= 0 ? Ease.OutCubic(Seg(t, s.ImpactT, s.ImpactT + (s.Overview ? 0.3f : FillDur))) : 0f;

            // 배양조 — 배양액 색이 통째로 바뀐다(PD 「색 자체가 보라에서 노랑으로」). 아래 판 = 앞 단계, 위 판 = 지금 단계가 차오름
            float tankA = revealed && _tankBroken != null ? 0f : inU;
            Sprite TankOf(int k) => k >= 0 && _tankTier.Length > 0 ? _tankTier[Mathf.Min(k, _tankTier.Length - 1)] : _tank;
            int under = s.Overview || tier <= 0 ? -1 : tier - 1;
            float cross = tier < 0 ? 0f : Seg(sinceUp, 0f, tier == 0 || s.Overview ? 0.35f : 0.2f);
            Show(s.Tank, TankOf(under), tankA);
            Show(s.TankB, TankOf(tier), tier >= 0 ? tankA * cross : 0f);

            // 유리 — 영혼이 들어가면 그 단계 색 유리로(승격 때 컷)
            var glass = tier >= 0 && _tankGlassTier.Length > 0 ? _tankGlassTier[Mathf.Min(tier, _tankGlassTier.Length - 1)] : _tankGlass;
            Show(s.Glass, glass, revealed ? 0f : inU * Mathf.Lerp(0.6f, 0.95f, fill) * (1f + 0.3f * pop));
            s.Glass.material = _additive;
            // 배양액 빛 — 천천히 숨 쉬고(2.2초), 모으기 때 밝아지고, 가짜 뜸에선 사그라든다
            float breathe = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f / 2.2f);
            float aura = revealed ? 0f : fill * (0.28f + 0.08f * breathe + 0.3f * charge + 0.5f * pop) * (1f - 0.7f * calm);
            s.Aura.enabled = aura > 0.01f && _softGlow != null;
            if (s.Aura.enabled) s.Aura.color = WithA(c, aura);

            // 기포 — 배양액이 차면 천천히 올라오고, 모으기 때 끓는다(가짜 뜸에선 거의 멈춘다). 1회만
            if (!s.Overview && tier >= 0 && !revealed)
            {
                float rate = (4f + 26f * charge * charge) * (1f - 0.85f * calm);
                s.BubbleAcc += rate * dt;
                int n = Mathf.FloorToInt(s.BubbleAcc);
                if (n > 0) { s.BubbleAcc -= n; Bubbles(s, c, n); }
            }

            // 몸 — 빙의 전 짙은 실루엣, 모으기 때 떨림, 공개 = 컷 + 세로 늘어남
            float shake = 0f, sx = 1f, sy = 1f;
            if (charge > 0f && !revealed) shake = Mathf.Sin(t * 38f) * Amp[Mathf.Min(final, Amp.Length - 1)] * px * charge;
            if (t >= s.CrackT && !revealed && !s.Overview) shake = Mathf.Sin(t * 45f) * Amp[final] * px * (0.5f + 0.5f * Seg(t, s.CrackT, s.EyesT));
            if (revealed)
            {
                float u = Seg(t, s.RevealT, s.RevealT + 0.3f);
                sy = u < 0.3f ? Mathf.Lerp(1f, 1.15f, u / 0.3f) : Mathf.Lerp(1.15f, 1f, Ease.OutBack((u - 0.3f) / 0.7f));
                sx = 2f - sy;
                float grow = Mathf.Lerp(1f, RevealGrow, Ease.OutCubic(u));
                // 자세를 잡은 뒤엔 숨쉬기(1.8초) — 가만히 서 있지 않게
                float idle = Seg(t, s.RevealT + PoseEnd, s.RevealT + PoseEnd + 0.4f);
                float br0 = Mathf.Sin((t - s.RevealT - PoseEnd) * Mathf.PI * 2f / 1.8f) * idle;
                sx *= grow * (1f - 0.01f * br0); sy *= grow * (1f + 0.02f * br0);
            }
            s.Body.enabled = true;
            s.Body.sprite = revealed ? PoseSprite(s, t - s.RevealT) : s.Art;
            s.Body.material = revealed ? null : _silhouette;
            // 실루엣은 배양액 색이 몸 둘레에 은은하게 스민다(천천히)
            float rim = tier >= 0 && !revealed ? (0.1f + 0.1f * breathe + 0.25f * pop) * fill : 0f;
            s.Body.color = revealed ? Color.white : WithA(Color.Lerp(SilhouetteColor, c, rim), inU);
            var br = s.Body.rectTransform;
            br.pivot = new Vector2(0.5f, FootFromBottom);
            br.anchoredPosition = s.Feet + new Vector2(shake, 0f);
            br.localScale = new Vector3(sx, sy, 1f);

            // 흰 번쩍 — 영혼이 들어갈 때 · 승격할 때만(0.15s)
            float white = t >= s.ImpactT ? 1f - Seg(t, s.ImpactT, s.ImpactT + 0.15f) : 0f;
            if (!s.Overview && tier >= 1) white = Mathf.Max(white, 1f - Seg(sinceUp, 0f, 0.15f));
            s.White.enabled = white > 0f && !revealed;
            if (s.White.enabled)
            {
                s.White.sprite = s.Art;
                s.White.material = _silhouette;
                s.White.color = WithA(Color.white, white * 0.85f);
                var wr = s.White.rectTransform;
                wr.pivot = br.pivot; wr.anchoredPosition = br.anchoredPosition; wr.localScale = br.localScale;
            }

            // 유리 금 — 마지막 뜸 뒤 깨지기 전 0.6초에 3단계로
            int crack = s.Overview || _crack.Length == 0 || t < s.CrackT || revealed ? -1
                      : Mathf.Min(_crack.Length - 1, Mathf.FloorToInt(Seg(t, s.CrackT, s.EyesT) * _crack.Length));
            s.Crack.enabled = crack >= 0;
            if (crack >= 0) { s.Crack.sprite = _crack[crack]; s.Crack.rectTransform.anchoredPosition = s.Tank.rectTransform.anchoredPosition + new Vector2(shake * 0.4f, 0f); }

            // 영혼이 들어갈 때 · 공개할 때 파티클(한 번씩)
            if (!s.ImpactFx && t >= s.ImpactT) { s.ImpactFx = true; if (!s.Overview) Sparks(s.Chest, c, 3, 0.6f); }
            if (!s.RevealFx && revealed) { s.RevealFx = true; Shatter(s, c); }

            // 가슴 코어 점등 — 깨지기 직전
            float eyes = Seg(t, s.EyesT, s.EyesT + 0.15f) * (revealed ? 1f - Seg(t, s.RevealT, s.RevealT + 0.2f) : 1f);
            s.Core.enabled = eyes > 0f && _softGlow != null && !s.Overview;
            if (s.Core.enabled) s.Core.color = WithA(c, eyes);

            // 고리 파동 — 영혼이 들어갈 때 작게, 승격할 때마다 단계만큼 크게(0.7s OutCubic)
            float ringU = -1f, ringK = 1f;
            if (t >= s.ImpactT && t < s.ImpactT + 0.7f) { ringU = Seg(t, s.ImpactT, s.ImpactT + 0.7f); ringK = s.Overview && final >= (int)Rarity.Super ? 1.4f : 1f; }
            if (!s.Overview && tier >= 1 && sinceUp < 0.7f) { ringU = Seg(sinceUp, 0f, 0.7f); ringK = 1.3f + 0.35f * tier; }
            bool ringOn = _ring.Length > 0 && ringU >= 0f && ringU < 1f;
            s.Ring.enabled = ringOn;
            if (ringOn)
            {
                s.Ring.sprite = _ring[Mathf.Min(_ring.Length - 1, Mathf.FloorToInt(ringU * _ring.Length))];
                s.Ring.rectTransform.localScale = Vector3.one * Mathf.Max(0.01f, Ease.OutCubic(ringU) * ringK);
                s.Ring.color = WithA(c, 0.9f * (1f - ringU));
            }

            // 유령 — 1회: 관 끝에서 둥실 → 곡선 · 끝만 가속 → 가슴에 쏙 · 꼬리 잔상. 10회: 위에서 떨어짐
            float gIn0 = s.Overview ? 0.1f : 0.2f, gIn1 = s.Overview ? 0.3f : 0.6f;
            float gA = Seg(t, gIn0, gIn1) * (1f - Seg(t, s.ImpactT - 0.06f, s.ImpactT));
            Color gc = TierColor[0];   // 영혼은 언제나 하늘색으로 시작한다 — 색은 배양조 안에서 올라간다
            for (int k = s.Trail.Length; k >= 1; k--)
            {
                var tr = s.Trail[k - 1];
                float ta = gA * 0.1f * (6 - k) * Seg(t, s.FlyStart, s.FlyStart + 0.15f);
                tr.enabled = ta > 0.01f && s.Ghost != null;
                if (!tr.enabled) continue;
                tr.sprite = s.Ghost;
                tr.color = WithA(gc, ta);
                tr.rectTransform.anchoredPosition = GhostPos(s, t - k * 0.04f);
                tr.rectTransform.localScale = Vector3.one * (1f - k * 0.11f);
            }
            var gp = GhostPos(s, t);
            s.GhostImg.enabled = gA > 0.01f && s.Ghost != null;
            if (s.GhostImg.enabled) { s.GhostImg.sprite = s.Ghost; s.GhostImg.color = WithA(Color.white, gA); s.GhostImg.rectTransform.anchoredPosition = gp; }
            s.GhostGlow.enabled = s.GhostImg.enabled && _softGlow != null;
            if (s.GhostGlow.enabled) { s.GhostGlow.color = WithA(gc, gA * 0.7f); s.GhostGlow.rectTransform.anchoredPosition = gp; }
        }

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
        // 규칙 08 1-1 「한 번에 40 이하」를 넘는다 — 최상위 등급 공개 한 박자(가이드 guide_summon_lab.md 에 이유).
        private static readonly BurstSpec PlateSpec = new BurstSpec(26, new Vector2(0.07f, 0.19f), new Vector2(1.0f, 1.8f), new Vector2(1.6f, 2.1f), 50f,
                                                                    new Vector2(0.3f, 0.5f), new Vector2(10f, 75f), new Vector2(0.5f, 1f));
        private static readonly BurstSpec ChipSpec = new BurstSpec(75, new Vector2(0.012f, 0.035f), new Vector2(0.5f, 1.8f), new Vector2(1.3f, 1.8f), 90f,
                                                                   new Vector2(0f, 0.5f), new Vector2(5f, 88f), new Vector2(0.3f, 1f));
        private static readonly BurstSpec SplashSpec = new BurstSpec(60, new Vector2(0.03f, 0.09f), new Vector2(0.4f, 1.5f), new Vector2(1.1f, 1.5f), 30f,
                                                                     new Vector2(0f, 0.5f), new Vector2(10f, 80f), new Vector2(0.3f, 1f),
                                                                     0.3f, new Vector2(40f, 85f), new Vector2(0.05f, 0.1f));
        // 사다리 계단(하늘 · 보라 · 금 · 무지개)
        private static readonly float[] BurstScale = { 0.35f, 0.55f, 0.8f, 1f };

        /// <summary>깨짐 — 유리 판 · 부스러기 · 배양액이 바깥 · 위로 튀었다가 중력으로 천천히 떨어진다 + 반짝이.</summary>
        private void Shatter(Slot s, Color c)
        {
            float scale = BurstScale[(int)s.E.Rarity];
            Burst(_shardPs, s, PlateSpec, scale);
            Burst(_chipPs, s, ChipSpec, scale);
            Burst(_splashPs, s, SplashSpec, scale);
            // 반짝이는 몸 둘레 바깥 양옆 · 머리 위에서 천천히 떠오른다 — 몸(글자 · 얼굴) 위를 덮지 않게
            float side = s.Cell * 0.42f;
            int k = 1 + (int)s.E.Rarity;
            Sparks(s.Feet + new Vector2(-side, s.Cell * 0.3f), c, k, 0.8f);
            Sparks(s.Feet + new Vector2(side, s.Cell * 0.3f), c, k, 0.8f);
            Sparks(s.Feet + new Vector2(0f, s.Cell * 0.75f), c, k, 0.8f);
        }

        private void Burst(ParticleSystem ps, Slot s, in BurstSpec q, float scale)
        {
            if (ps == null) return;
            int n = Mathf.Max(1, Mathf.RoundToInt(q.Count * scale));
            float tankH = s.GlassSize.y / 0.64f;
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

        /// <summary>반짝이 — 사방으로 조금 퍼졌다가 천천히 떠오르며 사라진다(가산, 단계색).</summary>
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
                    startSize = WorldLen(H * UnityEngine.Random.Range(0.016f, 0.032f)),
                    startLifetime = UnityEngine.Random.Range(1.4f, 2.2f),
                    startColor = tint,
                };
                _sparkPs.Emit(e, 1);
            }
        }

        /// <summary>기포 — 배양액 유리창 아래쪽에서 천천히 떠오른다(단계색 옅게).</summary>
        private void Bubbles(Slot s, Color c, int n)
        {
            if (_sparkPs == null) return;
            var tint = WithA(Color.Lerp(c, Color.white, 0.5f), 0.7f);
            float tankH = s.GlassSize.y / 0.64f;
            for (int i = 0; i < n; i++)
            {
                var p = s.GlassCenter + new Vector2((UnityEngine.Random.value - 0.5f) * s.GlassSize.x * 0.85f, -s.GlassSize.y * UnityEngine.Random.Range(0.3f, 0.48f));
                var e = new ParticleSystem.EmitParams
                {
                    position = World(p),
                    velocity = World(p + new Vector2(UnityEngine.Random.Range(-0.02f, 0.02f), UnityEngine.Random.Range(0.2f, 0.45f)) * tankH) - World(p),
                    startSize = WorldLen(tankH * UnityEngine.Random.Range(0.012f, 0.028f)),
                    startLifetime = UnityEngine.Random.Range(0.9f, 1.4f),
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

        private Vector2 GhostPos(Slot s, float t)
        {
            if (t < s.FlyStart) return s.From + new Vector2(0f, Mathf.Sin(t * 3f) * 6f * (H / 640f));
            // 1회는 천천히 떠올랐다 부드럽게 들어간다(전엔 u^1.8 로 끝에 확 빨려 들어갔다 — PD 「싸구려처럼 빠르게」), 10회는 떨어지듯
            float u = Seg(t, s.FlyStart, s.FlyEnd);
            u = s.Overview ? u * u : Ease.InOutSine(u);
            var a = Vector2.Lerp(s.From, s.Ctrl, u);
            var b = Vector2.Lerp(s.Ctrl, s.Chest, u);
            return Vector2.Lerp(a, b, u);
        }

        private const float RevealGrow = 1.3f;

        // 시안(mock_summon_lab_free_peak) 실측에 맞춘 값 — 선형 색 공간 합성으로 재서 정했다(2026-10-22).
        private const float LightSize = 0.58f, LightLift = 0.41f;
        private static readonly Color GoldLightTint = new Color(0.93f, 1f, 1f);
        // 은은하게(PD 10-23) — 6장 루프를 0.25초마다 앞뒤 장을 겹쳐 넘기고, 두 겹이 ±5° · 8초로 천천히 흔들린다
        private const float LightFrame = 0.25f, LightSwing = 5f, LightPeriod = 8f, LightLayer2 = 0.3f;
        // 빛 그림의 빛 뿌리는 아래에서 7% — 거기를 축으로 돌린다
        private const float LightRoot = 0.07f;

        /// <summary>공개 빛 — 깨진 배양조 위 · 몸 뒤에서 발 자리부터 위로 퍼지는 빛줄기 루프.</summary>
        private void RevealLight(float t, Slot best, float revealU)
        {
            var frames = best.E.Rarity switch
            {
                Rarity.Jackpot => _revealLightRainbow.Length > 0 ? _revealLightRainbow : _revealLight,
                Rarity.Super => _revealLight,
                Rarity.Rare => _revealLightSr,
                _ => _revealLightR,
            };
            if (frames.Length == 0) frames = _revealLight;
            bool on = frames.Length > 0 && revealU > 0f;
            best.Light.enabled = best.LightB.enabled = best.Light2.enabled = on;
            if (!on) return;
            float k = best.E.Rarity switch { Rarity.Jackpot => 1f, Rarity.Super => 1f, Rarity.Rare => 0.8f, _ => 0.6f };
            var tint = best.E.Rarity == Rarity.Super ? GoldLightTint : Color.white;
            float tr = t - best.RevealT;
            float f = tr / LightFrame;
            int i0 = Mathf.FloorToInt(f) % frames.Length;
            float blend = f - Mathf.Floor(f);
            float a = k * revealU;
            // 10회는 배양조가 0.2H(1회 0.5H) — 같은 비율로 줄인다(지금은 1회 무대에서만 쓴다)
            float size = H * LightSize * (0.85f + 0.15f * revealU);
            float swing = LightSwing * Mathf.Sin(tr * Mathf.PI * 2f / LightPeriod);
            best.Light.sprite = frames[i0];
            best.Light.color = WithA(tint, a * (1f - blend));
            PlaceLight(best.Light, best, size, swing);
            best.LightB.sprite = frames[(i0 + 1) % frames.Length];
            best.LightB.color = WithA(tint, a * blend);
            PlaceLight(best.LightB, best, size, swing);
            best.Light2.sprite = frames[(i0 + frames.Length / 2) % frames.Length];
            best.Light2.color = WithA(tint, a * LightLayer2);
            PlaceLight(best.Light2, best, size, -LightSwing * Mathf.Sin(tr * Mathf.PI * 2f / LightPeriod + 1.3f));
        }

        private static void PlaceLight(Image img, Slot best, float size, float angle)
        {
            var rt = img.rectTransform;
            rt.pivot = new Vector2(0.5f, LightRoot);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = best.Feet + new Vector2(0f, size * (LightLift - 0.5f + LightRoot));
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
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

        /// <summary>화면 하양 — 대박만 깨지기 직전. 승격 때 화면 전체를 덮으면 회색 막처럼 보인다(녹화 10-23) — 승격은 몸 · 고리 · 배양액만 번쩍.</summary>
        private void Flash(float t, Slot best)
        {
            float f = 0f;
            if (best.FlashT > 0f)
                f = Mathf.Max(f, 0.9f * (t < best.FlashT ? Seg(t, best.FlashT - 0.1f, best.FlashT) : 1f - Seg(t, best.FlashT, best.RevealT + 0.15f)));
            _flash.enabled = f > 0f;
            if (_flash.enabled) _flash.color = WithA(Color.white, f);
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

        // ── 노드 ─────────────────────────────────────────────

        private Slot MakeSlot(Entry e, Sprite art, Sprite ghost, Sprite[] pose)
        {
            var s = new Slot { E = e, Art = art, Ghost = ghost, Pose = pose };
            s.Root = Layer($"Slot{_slots.Count}", _shake, true);
            s.Tank = Img("Tank", s.Root, null);
            s.TankB = Img("TankB", s.Root, null);
            s.Broken = Img("Broken", s.Root, null);
            // 배양액 빛은 배양조 위(유리창 그림이 불투명이라 뒤에 깔면 안 보인다) · 몸 뒤
            s.Aura = Img("Aura", s.Root, _softGlow);
            // 시안처럼 빛은 깨진 배양조 위에서 뿜어 나온다 — 배양조 뒤에 깔면 쇠 몸통에 가려 둘레만 물든다
            s.Light2 = Img("Light2", s.Root, _additive);
            s.Light = Img("Light", s.Root, _additive);
            s.LightB = Img("LightB", s.Root, _additive);
            s.Body = Img("Body", s.Root, null);
            s.White = Img("White", s.Root, null);
            s.Glass = Img("Glass", s.Root, _additive);
            s.Crack = Img("Crack", s.Root, null);
            s.Core = Img("Core", s.Root, _softGlow);
            s.Ring = Img("Ring", s.Root, _additive);
            s.Trail = new Image[5];
            for (int i = 0; i < 5; i++) s.Trail[i] = Img($"Trail{i}", s.Root, null);
            s.GhostGlow = Img("GhostGlow", s.Root, _softGlow);
            s.GhostImg = Img("Ghost", s.Root, null);
            return s;
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
            Localize.ApplyFonts(_root);
            if (_canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                // 검은 바탕 유리 · 물 그림은 가산 — 속이 비쳐 뒤 금빛이 보인다(시안). 중력 · 회전은 느리게(PD 10-23)
                var glassMat = _shardAdditive && _additive != null ? _additive : Canvas.GetDefaultCanvasMaterial();
                if (_shardSheet != null) _shardPs = NewParticles("ShardParticles", _shardSheet, glassMat, _shardGrid, 0.8f, 1.2f, 80, 0.85f);
                if (_chipSheet != null) _chipPs = NewParticles("ChipParticles", _chipSheet, glassMat, _chipGrid, 0.8f, 1.2f, 120, 0.85f);
                if (_splashSheet != null && _additive != null) _splashPs = NewParticles("SplashParticles", _splashSheet, _additive, _splashGrid, 1.0f, 1.2f, 100, 0.85f);
                // 반짝이 · 기포 — 위로 천천히 떠오른다
                if (_spark != null && _additive != null) _sparkPs = NewParticles("SparkParticles", _spark, _additive, Vector2Int.one, -0.08f, 0.8f, 200, 0.5f);
            }
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
            var basis = parent == _root ? new Vector2(_root.rect.width, _root.rect.height) : parent.sizeDelta;
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
