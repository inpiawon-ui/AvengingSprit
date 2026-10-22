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
    /// 소환 연출 「영혼 배양실」(PD 2026-10-22) — 박자 정본 motion-library `gacha_reveal_1`
    /// (`WA-Unity-Game-MotionEff/motion-library/refs/gacha_reveal_1`, 가이드 `Projects/AVSR/_exchange/guide_summon_lab.md`).
    ///
    /// 1회: 배양조 속 빈 몸 실루엣이 떠오름 → 왼쪽 아래 관에서 유령(몸빛 = 등급 예고)이 곡선으로 가속해 가슴에 쏙(0.95)
    /// → 흰 번쩍 · 고리 → 등급만큼 떨며 유리에 금(B 0.45 · A 0.65 · 대박/픽업 0.95초) → 가슴 코어 점등 → (대박 흰 플래시)
    /// → **유리가 깨지며 컷 공개**(세로 1.18 늘었다 OutBack) → 0.5초 뒤 이름 판 → 탭.
    /// 10회: 작은 배양조 10개 · 유령 0.07초 간격 → 마지막 빙의 +0.35 에 9개 물결 공개 → 최고 1명만 0.75초 더 떨다 단독 공개.
    /// 언제든 누르면 공개 직후로 건너뛴다. 노드는 처음 쓸 때 코드가 세운다. 그림은 메뉴 「소환 — 공개 연출 그림 꽂기」.
    /// </summary>
    public sealed class SummonReveal : MonoBehaviour
    {
        public enum Rarity { R, SR, SSR }

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
        /// <summary>공개 빛 — 몸 뒤 빛줄기 루프(검은 바탕 가산). 대박 = 받은 금빛 원본 · SR 보라 · R 하늘(원본 색상만 돌린 판).</summary>
        [SerializeField] private Sprite[] _revealLight = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _revealLightSr = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _revealLightR = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _crack = Array.Empty<Sprite>();
        /// <summary>
        /// 유리 조각 6종 시트(3×2, `lab_glass_shards_sheet`) · 반짝이(`lab_spark`) — **파티클 시스템**으로 뿌린다.
        /// PD 2026-10-22 「깨지는 이펙트 이미지가 너무 지저분해」 → 「저런 식으로 리소스를 뽑고 파티클 시스템으로 이펙트 뿌리면 되잖아」.
        /// </summary>
        [SerializeField] private Texture2D _shardSheet, _spark;
        [SerializeField] private Sprite[] _ring = Array.Empty<Sprite>();
        [SerializeField] private Sprite _stampNew, _ribbonPickup;
        [SerializeField] private Material _additive, _silhouette;
        /// <summary>둥근 부드러운 빛 — 그림 없이 셰이더가 그린다(`UISoftGlow.mat`, UI/AdditiveLight 고리 반지름 0).</summary>
        [SerializeField] private Material _softGlow;
        [SerializeField] private TMP_FontAsset _font;

        // ── 박자 (gacha_reveal_1 prototype.html singleTimeline · multiTimeline) ──
        private const float GhostIn0 = 0.12f, GhostIn1 = 0.35f, Fly0 = 0.35f, Impact = 0.95f;
        private const float MultiFlyGap = 0.07f, MultiFly = 0.5f, MultiCommon = 0.35f, MultiWave = 0.04f, MultiBestHold = 0.75f;
        private static readonly float[] Struggle = { 0.45f, 0.65f, 0.95f };
        private static readonly float[] Amp = { 2f, 3.5f, 6f };                 // px @ 640 높이
        private static readonly Color[] RarityColor =
            { new(0.35f, 0.82f, 1f), new(0.73f, 0.55f, 1f), new(1f, 0.79f, 0.3f) };   // #5ad1ff · #b98bff · #ffc94d
        private static readonly Color SilhouetteColor = new(0.04f, 0.05f, 0.13f, 1f);
        private const float InkOfCell = 76f / 128f, FootFromBottom = 24f / 128f;

        private sealed class Slot
        {
            public Entry E;
            public Sprite Art, Ghost;
            public RectTransform Root;
            public Image Tank, Broken, Light, Body, White, Glass, Crack, Core, GhostImg, GhostGlow, Ring;
            public Vector2 GlassCenter, GlassSize;
            public bool ImpactFx, RevealFx;
            public Image[] Trail;
            public Vector2 Feet, Chest, From, Ctrl;
            public float FlyStart, FlyEnd, ImpactT, EyesT, FlashT = -1f, RevealT, Cell;
            public bool Best;
        }

        private RectTransform _root, _shake;
        // 파티클은 Overlay 캔버스 위에 안 그려진다 — 배양실만 메인 카메라 캔버스로 따로 띄우고, 도는 동안 로비 캔버스를 잠깐 숨긴다
        private Canvas _canvas, _lobbyCanvas;
        private ParticleSystem _shardPs, _sparkPs;
        private Image _dim, _bg, _flash, _plateImg, _tagImg;
        private TextMeshProUGUI _tagText, _nameText, _gradeText, _tapText;
        private readonly List<Slot> _slots = new();
        private bool _tapped;
        private int _run;   // 새 연출이 시작되면 앞의 것은 그 자리에서 멈춘다(같은 판을 둘이 그리지 않게)

        /// <summary>뽑은 몸들을 공개하고 탭을 기다린다(1명 = 큰 배양조 하나, 여럿 = 5×2). 큰 몸 그림이 없는 몸은 초상 없이 건너뛴다.</summary>
        public async UniTask PlayAsync(IReadOnlyList<Entry> entries, CancellationToken ct)
        {
            if (entries == null || entries.Count == 0 || !CoreModule.TryGet<IResourceManager>(out var res)) return;
            int run = ++_run;
            // 앞 판이 끝나기 전에 새로 부르면 앞 판의 빛 · 이름판이 그림을 불러오는 동안 남는다
            if (_root != null) _root.gameObject.SetActive(false);
            if (_shardPs != null) _shardPs.Clear();
            if (_sparkPs != null) _sparkPs.Clear();
            var loaded = new List<string>();
            try
            {
                var ghostAtlas = await Load(res, "ghost", loaded);
                var ghost = ghostAtlas != null ? ghostAtlas.GetSprite("unitL_ghost_se") : null;
                Build();
                ClearSlots();
                int best = 0;
                for (int i = 0; i < entries.Count; i++)
                {
                    var atlas = await Load(res, entries[i].HostKey, loaded);
                    var art = atlas != null ? atlas.GetSprite($"unitL_{entries[i].HostKey}_se") : null;
                    if (art == null) continue;
                    _slots.Add(MakeSlot(entries[i], art, ghost));
                    if (entries[i].Rarity > _slots[best].E.Rarity) best = _slots.Count - 1;
                }
                if (_slots.Count == 0) return;
                Timeline(best);
                if (run != _run) return;
                await RunAsync(run, ct);
            }
            finally
            {
                if (_root != null && run == _run)
                {
                    _root.gameObject.SetActive(false);
                    if (_shardPs != null) _shardPs.Clear();
                    if (_sparkPs != null) _sparkPs.Clear();
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

        // ── 배치 · 박자 ──────────────────────────────────────

        private float H => _root.rect.height;
        private float U => Mathf.Min(_root.rect.width, H * 720f / 1280f);   // 설계 폭(720 기준) — 4:3 · 20:9 에서도 같은 판

        private void Timeline(int best)
        {
            bool single = _slots.Count == 1;
            if (single)
            {
                var s = _slots[0];
                s.Best = true;
                int r = (int)s.E.Rarity;
                s.FlyStart = Fly0; s.FlyEnd = Impact; s.ImpactT = Impact;
                s.EyesT = Impact + Struggle[r];
                if (s.E.Rarity == Rarity.SSR) s.FlashT = s.EyesT + 0.15f;
                s.RevealT = s.EyesT + (s.FlashT > 0f ? 0.3f : 0.2f);
                Place(s, new Vector2(0f, -H * 0.185f), H * 0.5f, H * 0.27f);   // 배양조 밑이 무대 받침대(위에서 77%)에 앉게
                s.From = new Vector2(-U * 0.40f, -H * 0.38f);   // 무대 왼쪽 아래 관 끝
                s.Ctrl = new Vector2(-U * 0.45f, H * 0.05f);
            }
            else
            {
                for (int i = 0; i < _slots.Count; i++)
                {
                    var s = _slots[i];
                    s.FlyStart = Fly0 + i * MultiFlyGap; s.FlyEnd = s.FlyStart + MultiFly; s.ImpactT = s.FlyEnd;
                    int c = i % 5, row = i / 5;
                    Place(s, new Vector2((-2 + c) * U * 0.189f, H * (0.08f - row * 0.27f)), H * 0.2f, H * 0.11f);
                    s.From = new Vector2(0f, -H * 0.42f);
                    s.Ctrl = new Vector2(s.Feet.x * 0.3f, -H * 0.2f);
                }
                float common = _slots[_slots.Count - 1].ImpactT + MultiCommon;
                for (int i = 0; i < _slots.Count; i++)
                {
                    var s = _slots[i];
                    if (i == best) continue;
                    s.EyesT = common - 0.15f + i * MultiWave;
                    s.RevealT = common + i * MultiWave;
                }
                var b = _slots[best];
                b.Best = true;
                b.Root.SetAsLastSibling();   // 최고 칸의 빛이 옆 칸 배양조에 가리지 않게
                b.EyesT = common + MultiBestHold;
                if (b.E.Rarity == Rarity.SSR) b.FlashT = b.EyesT + 0.15f;
                b.RevealT = b.EyesT + (b.FlashT > 0f ? 0.3f : 0.2f);
            }
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
            foreach (var img in new[] { s.Tank, s.Broken, s.Glass, s.Crack })
            {
                img.rectTransform.sizeDelta = new Vector2(tw, tankH);
                img.rectTransform.anchoredPosition = tankCenter;
            }
            // 유리창 = 배양조 그림 안 가로 21~79% · 세로 21~85%(lab_tank_glass 실측) — 조각이 여기서 튄다
            s.GlassSize = new Vector2(tw * 0.58f, tankH * 0.64f);
            s.GlassCenter = tankCenter + new Vector2(0f, tankH * (0.5f - 0.53f));
            foreach (var img in new[] { s.Body, s.White })
            {
                img.rectTransform.sizeDelta = new Vector2(s.Cell, s.Cell);
                img.rectTransform.anchoredPosition = feet - new Vector2(0f, s.Cell * FootFromBottom);
            }
            float core = figure * 0.35f;
            s.Core.rectTransform.sizeDelta = new Vector2(core, core);
            s.Core.rectTransform.anchoredPosition = s.Chest;
            s.Ring.rectTransform.sizeDelta = new Vector2(H * 0.22f, H * 0.11f);
            s.Ring.rectTransform.anchoredPosition = s.Chest;
            float g = figure * 0.32f;
            s.GhostImg.rectTransform.sizeDelta = new Vector2(g, g);
            s.GhostGlow.rectTransform.sizeDelta = new Vector2(g * 2f, g * 2f);
            foreach (var t in s.Trail) t.rectTransform.sizeDelta = new Vector2(g, g);
        }

        // ── 진행 ─────────────────────────────────────────────

        private async UniTask RunAsync(int run, CancellationToken ct)
        {
            _canvas.gameObject.SetActive(true);
            if (_lobbyCanvas != null && _canvas.renderMode == RenderMode.ScreenSpaceCamera) _lobbyCanvas.enabled = false;   // 위에 그려지지 않게
            _root.gameObject.SetActive(true);
            _tapped = false;
            var best = _slots.Find(x => x.Best);
            int br = (int)best.E.Rarity;
            Color bc = RarityColor[br];
            float end = best.RevealT + 1.0f;
            float start = Time.unscaledTime;
            bool[] cued = new bool[4];
            GameSound.Cue("ui.play");
            while (!ct.IsCancellationRequested && run == _run)
            {
                float t = Time.unscaledTime - start;
                if (_tapped)
                {
                    _tapped = false;
                    if (t >= end) break;
                    if (t < best.RevealT + 0.25f) { start = Time.unscaledTime - (best.RevealT + 0.25f); continue; }   // 공개 직후로
                }
                if (!cued[0] && t >= best.ImpactT) { cued[0] = true; GameSound.Cue("hit.reflect"); }
                if (!cued[1] && t >= best.RevealT) { cued[1] = true; GameSound.Cue(best.E.Rarity == Rarity.SSR ? "run.card" : "run.gold"); }

                float revealU = Seg(t, best.RevealT, best.RevealT + 0.3f);
                // 화면: 딤 0.35(0.25s) → 공개 때 걷힘 · 배경 등급색 번짐 · 대박 회전 광선 · 흰 플래시
                _dim.color = new Color(0f, 0f, 0f, 0.35f * Seg(t, 0f, 0.25f) * (1f - revealU));
                RevealLight(t, best, revealU);
                Flash(t, best);
                // 대박 화면 흔들림(빙의 ~ 공개)
                float shake = br == 2 && t > best.ImpactT && t < best.RevealT ? 3f * (H / 640f) * Seg(t, best.ImpactT, best.EyesT) : 0f;
                _shake.anchoredPosition = new Vector2(Mathf.Sin(t * 71f) * shake, Mathf.Cos(t * 53f) * shake);

                foreach (var s in _slots) Draw(s, t);
                Banner(t - (best.RevealT + 0.5f), best);
                _tapText.gameObject.SetActive(t >= best.RevealT + 1.0f);
                if (_tapText.gameObject.activeSelf) _tapText.alpha = 0.55f + 0.45f * Mathf.Sin(t * 5f);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        private void Draw(Slot s, float t)
        {
            int r = (int)s.E.Rarity;
            Color c = RarityColor[r];
            float px = H / 640f;
            // 빈 몸 · 배양조 등장(0~0.35, 0.9→1.0 OutCubic · 10회는 0.05초 간격)
            int idx = _slots.IndexOf(s);
            float in0 = _slots.Count == 1 ? 0f : 0.05f * idx;
            float inU = Ease.OutCubic(Seg(t, in0, in0 + (_slots.Count == 1 ? 0.35f : 0.3f)));
            bool revealed = t >= s.RevealT;

            s.Root.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, inU);
            Show(s.Tank, _tank, revealed && _tankBroken != null ? 0f : inU);
            Show(s.Broken, _tankBroken, revealed ? 1f : 0f);
            Show(s.Glass, _tankGlass, revealed ? 0f : inU * 0.9f);
            s.Glass.material = _additive;

            // 몸 — 빙의 전 짙은 실루엣, 빙의 뒤 떨림, 공개 = 컷 + 세로 늘어남
            float shake = 0f, sx = 1f, sy = 1f;
            if (t >= s.ImpactT && !revealed)
                shake = Mathf.Sin(t * 60f) * Amp[r] * px * (0.6f + 0.4f * Seg(t, s.ImpactT, s.EyesT));
            if (revealed)
            {
                float u = Seg(t, s.RevealT, s.RevealT + 0.25f);
                sy = u < 0.3f ? Mathf.Lerp(1f, 1.18f, u / 0.3f) : Mathf.Lerp(1.18f, 1f, Ease.OutBack((u - 0.3f) / 0.7f));
                sx = 2f - sy;
                float grow = Mathf.Lerp(1f, RevealGrow, Ease.OutCubic(u));
                sx *= grow; sy *= grow;
            }
            s.Body.enabled = true;
            s.Body.sprite = s.Art;
            s.Body.material = revealed ? null : _silhouette;
            // 실루엣은 빙의 뒤 등급색 테두리 기운이 맥동한다(rim glow 대신 몸빛을 살짝 섞음)
            float rim = t >= s.ImpactT && !revealed ? 0.18f * (0.5f + 0.5f * Mathf.Sin(t * 18f)) : 0f;
            s.Body.color = revealed ? Color.white : WithA(Color.Lerp(SilhouetteColor, c, rim), inU);
            var br = s.Body.rectTransform;
            br.pivot = new Vector2(0.5f, FootFromBottom);
            br.anchoredPosition = s.Feet + new Vector2(shake, 0f);
            br.localScale = new Vector3(sx, sy, 1f);

            // 흰 번쩍(빙의 순간 1→0, 0.12s)
            float white = t >= s.ImpactT ? 1f - Seg(t, s.ImpactT, s.ImpactT + 0.12f) : 0f;
            s.White.enabled = white > 0f;
            if (s.White.enabled)
            {
                s.White.sprite = s.Art;
                s.White.material = _silhouette;
                s.White.color = WithA(Color.white, white);
                var wr = s.White.rectTransform;
                wr.pivot = br.pivot; wr.anchoredPosition = br.anchoredPosition; wr.localScale = br.localScale;
            }

            // 유리 금 — 버팀 동안 3단계로 번짐
            int crack = _crack.Length == 0 || t < s.ImpactT || revealed ? -1
                      : Mathf.Min(_crack.Length - 1, Mathf.FloorToInt(Seg(t, s.ImpactT, s.EyesT) * _crack.Length));
            s.Crack.enabled = crack >= 0;
            if (crack >= 0) { s.Crack.sprite = _crack[crack]; s.Crack.rectTransform.anchoredPosition = s.Tank.rectTransform.anchoredPosition + new Vector2(shake * 0.4f, 0f); }

            // 빙의 · 공개 순간 파티클(한 번씩) — 유리 조각은 사방으로 튀었다 떨어지고, 반짝이는 위로 흩어진다
            if (!s.ImpactFx && t >= s.ImpactT) { s.ImpactFx = true; Sparks(s.Chest, c, s.Best ? ImpactSparks[r] : 4, 0.6f); }
            if (!s.RevealFx && revealed) { s.RevealFx = true; Shatter(s, c); }

            // 가슴 코어 점등(눈 대신 — 몸마다 눈 자리가 달라 가슴 빛으로, gacha_reveal_1 0-1)
            float eyes = Seg(t, s.EyesT, s.EyesT + 0.12f) * (revealed ? 1f - Seg(t, s.RevealT, s.RevealT + 0.15f) : 1f);
            s.Core.enabled = eyes > 0f && _softGlow != null;
            if (s.Core.enabled) s.Core.color = WithA(c, eyes);

            // 빙의 고리(0→0.11H, 0.5s OutCubic) · 대박/희귀 공개 때 한 번 더 크게
            float ringU = Seg(t, s.ImpactT, s.ImpactT + 0.5f);
            float ringU2 = 0f;   // 공개 때 고리는 시안에 없다 — 빛줄기 · 조각만
            bool ringOn = _ring.Length > 0 && ((ringU > 0f && ringU < 1f) || (ringU2 > 0f && ringU2 < 1f));
            s.Ring.enabled = ringOn;
            if (ringOn)
            {
                float u = ringU2 > 0f ? ringU2 : ringU;
                float k = Ease.OutCubic(u) * (ringU2 > 0f ? 1.6f : 1f);
                s.Ring.sprite = _ring[Mathf.FloorToInt(u * _ring.Length) % _ring.Length];
                s.Ring.rectTransform.localScale = Vector3.one * Mathf.Max(0.01f, k);
                s.Ring.color = WithA(c, 1f - u);
            }

            // 유령 — 관 끝에서 둥실 → 2차 곡선 · u^1.8 가속 → 가슴에 쏙 · 꼬리 잔상 5개(0.03s)
            float gA = Seg(t, _slots.Count == 1 ? GhostIn0 : 0.1f, _slots.Count == 1 ? GhostIn1 : 0.3f) * (1f - Seg(t, s.ImpactT - 0.04f, s.ImpactT));
            for (int k = s.Trail.Length; k >= 1; k--)
            {
                var tr = s.Trail[k - 1];
                float ta = gA * 0.12f * (6 - k) * Seg(t, s.FlyStart, s.FlyStart + 0.1f);
                tr.enabled = ta > 0.01f && s.Ghost != null;
                if (!tr.enabled) continue;
                tr.sprite = s.Ghost;
                tr.color = WithA(c, ta);
                tr.rectTransform.anchoredPosition = GhostPos(s, t - k * 0.03f);
                tr.rectTransform.localScale = Vector3.one * (1f - k * 0.11f);
            }
            var gp = GhostPos(s, t);
            s.GhostImg.enabled = gA > 0.01f && s.Ghost != null;
            if (s.GhostImg.enabled) { s.GhostImg.sprite = s.Ghost; s.GhostImg.color = WithA(Color.white, gA); s.GhostImg.rectTransform.anchoredPosition = gp; }
            s.GhostGlow.enabled = s.GhostImg.enabled && _softGlow != null;
            if (s.GhostGlow.enabled) { s.GhostGlow.color = WithA(c, gA * 0.9f); s.GhostGlow.rectTransform.anchoredPosition = gp; }
        }

        // 알갱이 수 — 규칙 08 1-1(한 번에 40 이하). 빙의 반짝이 = gacha_reveal_1 입자 10/16/26 에서 조각 몫을 뺀 값
        private static readonly int[] ImpactSparks = { 8, 12, 18 };
        private static readonly int[] RevealShards = { 12, 16, 22 };
        private const int SmallShards = 6;

        /// <summary>깨짐 — 유리창 자리 곳곳에서 조각이 바깥 · 위로 튀고 중력으로 떨어진다(0.8초) + 반짝이.</summary>
        private void Shatter(Slot s, Color c)
        {
            if (_shardPs == null) return;
            int r = (int)s.E.Rarity;
            int n = s.Best ? RevealShards[r] : SmallShards;
            float tankH = s.GlassSize.y / 0.64f;
            for (int i = 0; i < n; i++)
            {
                var p = s.GlassCenter + new Vector2((UnityEngine.Random.value - 0.5f) * s.GlassSize.x, (UnityEngine.Random.value - 0.5f) * s.GlassSize.y);
                var dir = (p - s.GlassCenter + new Vector2(0f, tankH * 0.12f)).normalized;
                var e = new ParticleSystem.EmitParams
                {
                    position = World(p),
                    velocity = World(p + dir * tankH * UnityEngine.Random.Range(1.1f, 2.3f)) - World(p),
                    startSize = WorldLen(tankH * UnityEngine.Random.Range(0.13f, 0.24f)),
                    startLifetime = UnityEngine.Random.Range(0.6f, 0.9f),
                    rotation = UnityEngine.Random.Range(0f, 360f),
                    angularVelocity = UnityEngine.Random.Range(-450f, 450f),
                    startColor = Color.white,
                };
                _shardPs.Emit(e, 1);
            }
            // 반짝이는 몸 둘레 바깥 양옆 · 머리 위에서 — 몸(글자 · 얼굴) 위를 덮지 않게
            float side = s.Cell * 0.42f;
            int k = s.Best ? 5 : 1;
            Sparks(s.Feet + new Vector2(-side, s.Cell * 0.3f), c, k, 0.8f);
            Sparks(s.Feet + new Vector2(side, s.Cell * 0.3f), c, k, 0.8f);
            Sparks(s.Feet + new Vector2(0f, s.Cell * 0.75f), c, k, 0.8f);
        }

        /// <summary>반짝이 — 가운데에서 사방 · 조금 위로 흩어진다(가산, 등급색).</summary>
        private void Sparks(Vector2 at, Color c, int n, float power)
        {
            if (_sparkPs == null) return;
            var tint = Color.Lerp(c, Color.white, 0.35f);
            for (int i = 0; i < n; i++)
            {
                float a = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.8f + 0.25f);
                var e = new ParticleSystem.EmitParams
                {
                    position = World(at),
                    velocity = World(at + dir * H * UnityEngine.Random.Range(0.12f, 0.3f) * power) - World(at),
                    startSize = WorldLen(H * UnityEngine.Random.Range(0.018f, 0.04f)),
                    startLifetime = UnityEngine.Random.Range(0.5f, 0.9f),
                    startColor = tint,
                };
                _sparkPs.Emit(e, 1);
            }
        }

        private Vector3 World(Vector2 local) => _shake.TransformPoint(local);
        private float WorldLen(float local) => local * _shake.lossyScale.x;

        private ParticleSystem NewParticles(string name, Texture2D tex, Material baseMat, int tilesX, int tilesY, float gravityPerH)
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
            main.maxParticles = 120;
            // 중력 — 화면 높이 기준(캔버스가 카메라 앞 월드에 있어 월드 단위로 바꾼다)
            main.gravityModifier = gravityPerH * (H * _shake.lossyScale.y) / 9.81f;
            var emission = ps.emission; emission.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true; limit.drag = 1.2f;
            limit.multiplyDragByParticleSize = false; limit.multiplyDragByParticleVelocity = false;
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
            if (t < s.FlyStart) return s.From + new Vector2(0f, Mathf.Sin(t * 6f) * 6f * (H / 640f));
            float u = Mathf.Pow(Seg(t, s.FlyStart, s.FlyEnd), 1.8f);
            var a = Vector2.Lerp(s.From, s.Ctrl, u);
            var b = Vector2.Lerp(s.Ctrl, s.Chest, u);
            return Vector2.Lerp(a, b, u);
        }

        private const float RevealGrow = 1.3f;

        // 시안(mock_summon_lab_free_peak) 실측에 맞춘 값 — 선형 색 공간 합성으로 재서 정했다(2026-10-22).
        // 예전엔 금빛 그림에 금색을 또 곱해 주황(38°)이 됐고, 0.75H 로 화면 전체를 물들였다. 시안은 레몬 노랑(51°) · 흰 중심 · 배양조 둘레만.
        private const float LightSize = 0.58f, LightLift = 0.41f;
        private static readonly Color SsrLightTint = new Color(0.93f, 1f, 1f);

        /// <summary>공개 빛 — 깨진 배양조 위 · 몸 뒤에서 발 자리부터 위로 퍼지는 빛줄기 루프(6장 · 0.1초).</summary>
        private void RevealLight(float t, Slot best, float revealU)
        {
            var frames = best.E.Rarity == Rarity.SSR ? _revealLight : best.E.Rarity == Rarity.SR ? _revealLightSr : _revealLightR;
            if (frames.Length == 0) frames = _revealLight;
            var img = best.Light;
            bool on = frames.Length > 0 && revealU > 0f;
            img.enabled = on;
            if (!on) return;
            img.sprite = frames[Mathf.FloorToInt((t - best.RevealT) / 0.1f) % frames.Length];
            float k = best.E.Rarity == Rarity.SSR ? 1f : best.E.Rarity == Rarity.SR ? 0.8f : 0.6f;
            img.color = WithA(best.E.Rarity == Rarity.SSR ? SsrLightTint : Color.white, k * revealU);
            // 10회는 배양조가 0.2H(1회 0.5H) — 같은 비율로 줄인다
            float size = H * LightSize * (_slots.Count == 1 ? 1f : 0.4f) * (0.85f + 0.15f * revealU);
            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = best.Feet + new Vector2(0f, size * LightLift);
        }

        private void Flash(float t, Slot best)
        {
            float f = 0f;
            if (best.FlashT > 0f)
                f = t < best.FlashT ? Seg(t, best.FlashT - 0.08f, best.FlashT) : 1f - Seg(t, best.FlashT, best.RevealT + 0.1f);
            _flash.enabled = f > 0f;
            if (_flash.enabled) _flash.color = WithA(Color.white, 0.9f * f);
        }

        /// <summary>이름 판 — 왼쪽 밖에서 OutBack 0.35s(1회). 10회는 최고 몸 이름.</summary>
        private void Banner(float u, Slot best)
        {
            bool on = u >= 0f && _plate != null;
            _plateImg.gameObject.SetActive(on);
            if (!on) return;
            float k = Ease.OutBack(Seg(u, 0f, 0.35f));
            float y = _slots.Count == 1 ? -H * 0.3f : -H * 0.4f;
            _plateImg.rectTransform.anchoredPosition = new Vector2(Mathf.LerpUnclamped(-H * 0.5625f, 0f, k), y);
            _tagText.text = Localize.Get(best.E.TagKey);
            _nameText.text = best.E.Name;
            _gradeText.text = best.E.GradeLabel;
            _tagImg.sprite = best.E.TagKey == "ui.summon.reveal.pickup" ? _ribbonPickup : best.E.TagKey == "ui.summon.reveal.new" ? _stampNew : null;
            _tagImg.enabled = _tagImg.sprite != null;
        }

        // ── 노드 ─────────────────────────────────────────────

        private Slot MakeSlot(Entry e, Sprite art, Sprite ghost)
        {
            var s = new Slot { E = e, Art = art, Ghost = ghost };
            s.Root = Layer($"Slot{_slots.Count}", _shake, true);
            s.Tank = Img("Tank", s.Root, null);
            s.Broken = Img("Broken", s.Root, null);
            // 시안처럼 빛은 깨진 배양조 위에서 뿜어 나온다 — 배양조 뒤에 깔면 쇠 몸통에 가려 둘레만 물든다
            s.Light = Img("Light", s.Root, _additive);
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
                if (_shardSheet != null) _shardPs = NewParticles("ShardParticles", _shardSheet, Canvas.GetDefaultCanvasMaterial(), 3, 2, 3.2f);
                if (_spark != null && _additive != null) _sparkPs = NewParticles("SparkParticles", _spark, _additive, 1, 1, -0.15f);
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

        /// <summary>한 번 재생 — 끝나면 끈다. 재생 중이면 true.</summary>
        private static bool Once(Image img, Sprite[] frames, float t, float frameSec)
        {
            int i = Mathf.FloorToInt(t / frameSec);
            bool on = frames.Length > 0 && t >= 0f && i < frames.Length;
            img.enabled = on;
            if (on) img.sprite = frames[i];
            return on;
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
