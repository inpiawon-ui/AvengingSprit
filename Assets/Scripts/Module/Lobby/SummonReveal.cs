using System;
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
    /// 소환 「실루엣 → 공개」 — 새 몸 · 대박 · 픽업일 때만(PD 2026-10-21). 레퍼런스 motion-library `buy_shop_item_popup_2`
    /// (측정 박자, 가이드 `Projects/AVSR/_exchange/guide_summon_reveal.md`).
    ///
    /// t0 0~0.35 어두워짐 → 0.35 영혼 무대 컷 · 빛기둥 · 바닥 고리 → 0.35~2.35 실루엣이 0.25초마다 자세 · 좌우를 바꾸며
    /// 0.11 → 1.0 계단식으로 커짐(2.18 에 1.23 한 번 부풂) → **2.38 컷: 공개 무대 · 빛살 터짐 · 진짜 몸이 오른쪽에서 뛰어들어 착지**
    /// → 2.85 정보 판 0.1초 슉 → 탭하면 끝. 0.4초 뒤부터 누르면 건너뛴다(공개 장면으로 바로).
    ///
    /// 노드는 처음 쓸 때 코드가 세운다(전체 화면 한 겹). 그림 · 재질은 빌더 메뉴 「소환 — 공개 연출 그림 꽂기」가 넣는다.
    /// 몸 그림은 로비 진열과 같은 큰 그림 아틀라스 `atlas/unitl_{키}`.
    /// </summary>
    public sealed class SummonReveal : MonoBehaviour
    {
        public enum Tier { NewBody, Pickup, Jackpot }

        [SerializeField] private Sprite _stage, _revealStage, _plate, _rays;
        [SerializeField] private Sprite[] _pillar = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _ring = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _burst = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _impact = Array.Empty<Sprite>();
        [SerializeField] private Sprite _stampNew, _ribbonPickup;
        [SerializeField] private Material _additive, _silhouette;
        [SerializeField] private TMP_FontAsset _font;

        private const float Dark = 0.35f, Cut = 2.38f, PanelIn = 2.85f, Landed = 3.2f, SkipAfter = 0.4f;
        private const float FigureH = 0.34f;          // 실루엣 · 진짜 몸 키 = 화면 높이 비율(세로 화면)
        private const float InkOfCell = 76f / 128f, FootFromBottom = 24f / 128f;
        private static readonly string[] SpinOrder = { "", "_walk1", "_atk1", "_walk2", "_hit", "_atk2" };
        private static readonly Color SilhouetteColor = new(0.03f, 0.02f, 0.11f, 1f);

        private RectTransform _root;
        private Image _dim, _bg, _pillarA, _pillarB, _ringImg, _burstImg, _raysImg, _impactImg, _body, _plateImg, _tagImg;
        private TextMeshProUGUI _tagText, _nameText, _gradeText, _tapText;
        private bool _tapped;
        private float _start;

        /// <summary>그 몸을 공개한다. 탭으로 닫을 때까지 기다린다. 큰 몸 그림이 없으면 바로 돌아간다(연출 없이 결과 창).</summary>
        public async UniTask PlayAsync(string hostKey, string displayName, string gradeLabel, Tier tier, CancellationToken ct)
        {
            if (!CoreModule.TryGet<IResourceManager>(out var res)) return;
            string address = "atlas/unitl_" + hostKey;
            SpriteAtlas atlas;
            try { atlas = await res.LoadAsync<SpriteAtlas>(address); }
            catch (Exception) { return; }
            if (atlas == null) return;
            try
            {
                var poses = new Sprite[SpinOrder.Length];
                for (int i = 0; i < SpinOrder.Length; i++) poses[i] = atlas.GetSprite($"unitL_{hostKey}_se{SpinOrder[i]}");
                if (poses[0] == null) return;
                Build();
                await RunAsync(poses, displayName, gradeLabel, tier, ct);
            }
            finally
            {
                if (_root != null) _root.gameObject.SetActive(false);
                res.Release(address);
            }
        }

        // ── 진행 ─────────────────────────────────────────────

        private async UniTask RunAsync(Sprite[] poses, string displayName, string gradeLabel, Tier tier, CancellationToken ct)
        {
            Color tint = tier == Tier.Jackpot ? new Color(1f, 0.82f, 0.35f) : tier == Tier.Pickup ? new Color(0.85f, 0.6f, 1f) : new Color(0.6f, 0.9f, 1f);
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            _tapped = false;
            _start = Time.unscaledTime;
            ResetLayers(tint);
            _tagText.text = Localize.Get(tier == Tier.Jackpot ? "ui.summon.reveal.jackpot" : tier == Tier.Pickup ? "ui.summon.reveal.pickup" : "ui.summon.reveal.new");
            _nameText.text = displayName;
            _gradeText.text = gradeLabel;
            _tapText.text = Localize.Get("ui.summon.reveal.tap");
            GameSound.Cue("ui.play");

            float h = _root.rect.height;
            float cell = h * FigureH / InkOfCell;
            bool revealed = false;
            while (!ct.IsCancellationRequested)
            {
                float t = Time.unscaledTime - _start;
                // 0.4초 뒤 누르면 공개 장면으로 건너뛴다 · 공개 뒤 누르면 끝
                if (_tapped)
                {
                    _tapped = false;
                    if (t >= Landed) break;
                    if (t >= SkipAfter && t < Cut) { _start = Time.unscaledTime - Cut; continue; }
                }
                if (t < Dark) Darken(t / Dark);
                else if (t < Cut) Silhouette(t, poses, cell);
                else
                {
                    if (!revealed)
                    {
                        revealed = true;
                        GameSound.Cue(tier == Tier.Jackpot ? "run.card" : "run.gold");
                    }
                    Reveal(t - Cut, poses, cell, tier);
                }
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        private void Darken(float k)
        {
            _dim.enabled = true;
            _dim.color = new Color(0.04f, 0.05f, 0.16f, Mathf.Clamp01(k * 1.2f));
        }

        private void Silhouette(float t, Sprite[] poses, float cell)
        {
            _dim.enabled = false;
            Show(_bg, _stage);
            float u = t - Dark;
            // 빛기둥 둘이 엇갈려 깜빡 · 바닥 고리는 돈다(6장 루프)
            Loop(_pillarA, _pillar, u, 0.1f, 0);
            Loop(_pillarB, _pillar, u, 0.13f, 3);
            _pillarA.color = WithAlpha(_pillarA.color, 0.55f + 0.35f * Mathf.PerlinNoise(u * 3f, 0.1f));
            _pillarB.color = WithAlpha(_pillarB.color, 0.4f + 0.4f * Mathf.PerlinNoise(u * 3f, 0.7f));
            Loop(_ringImg, _ring, u, 0.1f, 0);

            int step = Mathf.FloorToInt(u / 0.25f);
            float s = SilhouetteScale(t, step);
            _body.enabled = true;
            _body.sprite = poses[step % poses.Length] ?? poses[0];
            _body.material = _silhouette;
            _body.color = SilhouetteColor;
            var rt = _body.rectTransform;
            rt.sizeDelta = new Vector2(cell * s, cell * s);
            rt.anchoredPosition = new Vector2(0f, -cell * s * FootFromBottom);
            rt.localScale = new Vector3(step % 2 == 1 ? -1f : 1f, 1f, 1f);   // 0.25초마다 좌우 — 2D 로 「도는」 느낌(레퍼런스 가이드)
        }

        /// <summary>측정 커브: 0.35 → 0.11, 2.0 → 1.0 계단식(0.25초 한 칸), 2.08~2.28 한 번 1.23 부풂, 끝 1.06.</summary>
        private static float SilhouetteScale(float t, int step)
        {
            if (t >= 2.08f && t < 2.28f) return 1f + 0.23f * Mathf.Sin((t - 2.08f) / 0.2f * Mathf.PI);
            if (t >= 2.28f) return 1.06f;
            float stepT = step * 0.25f;
            return Mathf.Lerp(0.11f, 1f, Mathf.Clamp01(stepT / 1.65f));
        }

        private void Reveal(float u, Sprite[] poses, float cell, Tier tier)
        {
            Show(_bg, _revealStage != null ? _revealStage : _stage);
            _pillarA.enabled = _pillarB.enabled = _ringImg.enabled = false;
            float w = _root.rect.width;
            float landX = -w * 0.18f;

            // 빛살 터짐(8장 한 번) → 빛살 그림이 천천히 돈다 — 대박이 가장 세다(등급 계단)
            bool burstOn = Once(_burstImg, _burst, u, tier == Tier.NewBody ? 0.06f : 0.07f);
            _burstImg.rectTransform.anchoredPosition = new Vector2(landX, -_root.rect.height * 0.02f);
            float raysA = tier == Tier.Jackpot ? 0.6f : tier == Tier.Pickup ? 0.45f : 0.3f;
            if (_rays != null)
            {
                _raysImg.enabled = true;
                _raysImg.sprite = _rays;
                _raysImg.color = WithAlpha(_raysImg.color, raysA * Mathf.Clamp01(u / 0.15f));
                _raysImg.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -u * 25f);
                _raysImg.rectTransform.anchoredPosition = new Vector2(landX, cell * 0.25f);
            }

            // 진짜 몸 — 오른쪽 밖 → 포물선으로 왼쪽 가운데 착지(0.8초, 감속) → 대기 숨쉬기
            float x, y;
            Sprite pose;
            const float jump = Landed - Cut;
            if (u < jump)
            {
                float k = u / jump;
                x = Mathf.Lerp(w * 0.5f + cell * 0.5f, landX, Ease.OutCubic(k));
                y = Mathf.Sin(Mathf.PI * k) * cell * 0.45f;
                pose = k < 0.45f ? poses[2] : k < 0.85f ? poses[5] : poses[1];
            }
            else
            {
                x = landX;
                y = 0f;
                pose = Mathf.FloorToInt((u - jump) / 0.5f) % 2 == 0 ? poses[0] : poses[3];
            }
            _body.enabled = true;
            _body.sprite = pose ?? poses[0];
            _body.material = null;
            _body.color = Color.white;
            var rt = _body.rectTransform;
            rt.localScale = Vector3.one;
            rt.sizeDelta = new Vector2(cell, cell);
            rt.anchoredPosition = new Vector2(x, y - cell * FootFromBottom);

            // 착지 충격 — 착지 순간 한 번
            Once(_impactImg, _impact, u - jump + 0.05f, 0.06f);
            _impactImg.rectTransform.anchoredPosition = new Vector2(landX, -cell * 0.08f);

            // 정보 판 — 2.85 에 0.1초 슉 · 작은 넘침
            float pu = u - (PanelIn - Cut);
            bool panel = pu >= 0f && _plate != null;
            _plateImg.gameObject.SetActive(panel);
            if (panel)
            {
                float k = Mathf.Clamp01(pu / 0.1f);
                float over = pu < 0.22f ? 0.06f * Mathf.Sin(Mathf.Clamp01(pu / 0.22f) * Mathf.PI) : 0f;
                float target = w * 0.12f;
                _plateImg.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(w, target, k) - over * w, -_root.rect.height * 0.12f);
                _tagImg.sprite = tier == Tier.Pickup ? _ribbonPickup : _stampNew;
                _tagImg.enabled = _tagImg.sprite != null && tier != Tier.Jackpot;
            }
            _tapText.gameObject.SetActive(u >= Landed - Cut + 0.4f);
            if (!burstOn && _burst.Length == 0) _burstImg.enabled = false;
        }

        // ── 도구 ─────────────────────────────────────────────

        private static void Show(Image img, Sprite s)
        {
            img.enabled = s != null;
            img.sprite = s;
            img.color = Color.white;
        }

        private static void Loop(Image img, Sprite[] frames, float t, float frameSec, int offset)
        {
            img.enabled = frames.Length > 0;
            if (frames.Length == 0) return;
            int i = (Mathf.FloorToInt(t / frameSec) + offset) % frames.Length;
            img.sprite = frames[i];
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

        private static Color WithAlpha(Color c, float a) { c.a = a; return c; }

        private void ResetLayers(Color tint)
        {
            foreach (var img in new[] { _bg, _pillarA, _pillarB, _ringImg, _burstImg, _raysImg, _impactImg, _body }) img.enabled = false;
            foreach (var img in new[] { _pillarA, _pillarB, _ringImg, _burstImg, _raysImg }) img.color = tint;
            _impactImg.color = Color.white;
            _plateImg.gameObject.SetActive(false);
            _tapText.gameObject.SetActive(false);
        }

        private void Build()
        {
            if (_root != null) return;
            // 소환 판은 아래 바 밑에 깔린다 — 공개 무대는 위 막대 · 아래 바까지 덮어야 해서 로비 화면(부모)에 세운다
            _root = Layer("SummonReveal", (transform.parent != null ? transform.parent : transform) as RectTransform, true);
            _root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);   // 누르기 받는 칸
            var btn = _root.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => _tapped = true);
            _dim = Img("RevealDim", true, null);
            _bg = Img("RevealStage", true, null);
            _bg.preserveAspect = false;
            var fit = _bg.gameObject.AddComponent<AspectRatioFitter>();   // 화면을 덮는다(4:3 · 20:9 에서도 빈 띠 없이)
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 720f / 1280f;
            _raysImg = Img("RevealRays", false, _additive, new Vector2(0.62f, 0.62f));
            _pillarA = Img("RevealPillarA", false, _additive, new Vector2(0.28f, 1.1f), new Vector2(-0.02f, 0.05f));
            _pillarB = Img("RevealPillarB", false, _additive, new Vector2(0.2f, 1.0f), new Vector2(0.06f, 0.08f));
            _ringImg = Img("RevealRing", false, _additive, new Vector2(0.62f, 0.16f), new Vector2(0f, -0.005f));
            _burstImg = Img("RevealBurst", false, _additive, new Vector2(1.0f, 0.56f));
            _body = Img("RevealBody", false, null);
            _body.rectTransform.pivot = new Vector2(0.5f, 0f);
            _impactImg = Img("RevealImpact", false, null, new Vector2(0.7f, 0.18f));
            _plateImg = Img("RevealPlate", false, null, new Vector2(0.62f, 0.15f));
            _plateImg.sprite = _plate;
            _plateImg.type = Image.Type.Simple;
            _tagImg = Child(_plateImg.rectTransform, "RevealTagImg", new Vector2(-0.38f, 0.5f), new Vector2(0.3f, 0.5f));
            _tagText = Text(_plateImg.rectTransform, "RevealTagText", new Vector2(-0.18f, 0.22f), new Vector2(0.6f, 0.3f), 30f, new Color(1f, 0.84f, 0.32f));
            _nameText = Text(_plateImg.rectTransform, "RevealNameText", new Vector2(-0.05f, -0.08f), new Vector2(0.8f, 0.36f), 44f, Color.white);
            _gradeText = Text(_plateImg.rectTransform, "RevealGradeText", new Vector2(-0.18f, -0.34f), new Vector2(0.6f, 0.2f), 22f, new Color(0.8f, 0.85f, 0.96f));
            _tapText = Text(_root, "RevealTapText", new Vector2(0f, -0.42f), new Vector2(0.8f, 0.05f), 24f, new Color(0.9f, 0.92f, 1f));
            _tapText.alignment = TextAlignmentOptions.Center;
            Localize.ApplyFonts(_root);
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

        /// <summary>화면 가운데 기준 칸. size · pos 는 화면 높이 비율(세로 화면 기준).</summary>
        private Image Img(string name, bool stretch, Material mat, Vector2 size = default, Vector2 pos = default)
        {
            var rt = Layer(name, _root, stretch);
            if (!stretch)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                float h = Mathf.Max(1f, _root.rect.height);
                rt.sizeDelta = size * h;
                rt.anchoredPosition = pos * h;
            }
            var img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = !stretch;
            img.material = mat;
            img.enabled = false;
            return img;
        }

        private Image Child(RectTransform parent, string name, Vector2 pos, Vector2 size)
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
