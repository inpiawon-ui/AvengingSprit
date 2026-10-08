using Cysharp.Threading.Tasks;
using Game.Module.Common.UI;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Resource;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 결과창 금화 더미 · 상자의 빛 (PD 2026-10-09).
    ///
    /// 「모양은 도트 그림, 광원은 셰이더 · 파티클」을 겹친다 — PD 「그림만으로는 툰 느낌, 게임 녹화의 광원은 괜찮았다」.
    /// 박자 · 강약은 연출 가이드 `Projects/AVSR/_exchange/guide_result_fx.md` — 금화 완성 · 상자 착지 = 강, 머묾 = 약.
    ///
    /// 금화(코덱스 자유 시안 `mock_result_codex_*` — PD 「골드는 광원이 괜찮다」, 그대로 둔다)
    ///   광원 = 빛 셰이더 번짐 · 빛살 = 도트 그림 터짐 8장 → 반복 8장 · 앞 = 반짝 별 그림 반복 + 유령 빛 알갱이 파티클
    /// 상자(새 시안 `mock_chest2_free_*` — 예전 상자 부품은 「광원에 아웃라인이 들어가 부자연스럽다」로 새로 받았다)
    ///   광원 = 빛 셰이더 번짐(등급색, 착지에 확 밝아졌다 숨쉬기)
    ///   착지 = 바닥에서 솟는 도트 빛살 그림 2장(번갈아 깜빡) — 곡선으로 솟구쳤다 사라짐
    ///   파티클 = 반짝 별 · 도트 알갱이 그림 2종 — 착지에 위로 튀고, 머묾에 알갱이가 천천히 떠오르고 별이 가끔 반짝
    ///   흰 그림에 등급색을 곱한다(모양은 같고 색만 다르다)
    ///
    /// 깊이: 광원 · 빛살 = 물건 그림 바로 앞 형제(물건에 가려짐) · 앞 그림 = 물건 바로 뒤 형제 · 파티클 = 결과창 캔버스 + 2.
    /// 상자가 자르기 틀(RectMask2D) 안에 있으면 틀 바깥 형제로 둔다 — 빛까지 틀에 잘리지 않게.
    /// 시간은 실제 시간 — 결과창은 게임 시간이 서 있어도 돈다.
    /// </summary>
    public sealed class ResultLightFx : MonoBehaviour
    {
        private const int Frames = 8;
        private const float BurstStep = 0.06f;    // 터짐 8장 = 0.48초
        private const float LoopStep = 0.13f;     // 머묾 반복 8장 = 1.04초
        private const float SparkleStep = 0.11f;
        private const float LoopAlpha = 0.8f;     // 머묾의 도트 빛살은 조금 낮춰 툰 느낌을 덜고 광원이 살게
        // 그림 칸(2배 440x300 · 480x300)을 줄 칸(126 높이) 안에 들게 줄였다 — 반 크기(220x150 · 240x150)는 줄 밖으로 넘치고
        // 금화 쪽은 오른쪽 「+1,240」 글자에 반짝 별이 걸렸다(녹화 vE)
        private static readonly Vector2 GoldSize = new(184f, 125f);
        // 2배 칸 480x300 을 0.62 배 — 시안처럼 빛살이 상자 위까지 솟아 보이게(반 크기는 상자에 거의 가려졌다, 녹화 vG). 바닥 줄은 아래에서 10%
        private static readonly Vector2 RiseSize = new(298f, 186f);
        private const float RiseFlickerStep = 0.06f;
        private static readonly Vector2 GoldGlowSize = new(210f, 140f);
        private static readonly Vector2 ChestGlowSize = new(214f, 158f);   // 시안 머묾처럼 상자 둘레로 광원이 보이게 — 210x140 은 상자에 거의 가려졌다(녹화 vI)
        private static readonly Color GoldGlowColor = new Color32(0xFF, 0xA8, 0x30, 0xFF);

        /// <summary>
        /// 상자 등급별 이펙트 단계 — 은 = 기본, 금 · 백금으로 한 겹씩 더한다(PD 2026-10-09 「가장 안 좋은 상자가 너무 좋으면
        /// 다음 등급들이 걷잡을 수 없게 된다」, 브리프 3-1). 최상위(백금) 몫을 남겨 두고, 새 등급이 생기면 이 표에 한 줄을 더한다.
        /// </summary>
        private struct ChestTier
        {
            public int Burst;         // 착지 때 튀는 알갱이 수
            public int BurstStars;    // 착지 때 같이 터지는 반짝 별 수
            public bool SecondBurst;  // 0.5초 뒤 반짝임 한 번 더
            public float MoteGap;     // 머묾 알갱이 간격(초)
            public float StarGap;     // 머묾 반짝 별 간격(초)
            public bool Rising;       // 머묾에 위로 흘러오르는 빛 알갱이
        }

        // 은 = 시안(mock_chest2_free_*) 수준이 기본 — 은을 시안보다 낮추지 않는다(PD 10-09 「은상자는 저 정도는 들어가야」).
        // 위 등급은 **크기가 아니라 파티클 수 · 이펙트 종류를 더한다**(PD 10-09 「힘을 더 준다는 건 크기를 키우라는 게 아니라
        // 파티클 객체 수를 약간 더 늘리고 이펙트를 다양하게」). 후광 · 빛살 · 광원 크기는 세 등급이 같다.
        private static readonly ChestTier[] Tiers =
        {
            new() { Burst = 12, BurstStars = 0, SecondBurst = false, MoteGap = 0.25f, StarGap = 1.2f, Rising = false },   // 은
            new() { Burst = 16, BurstStars = 5, SecondBurst = false, MoteGap = 0.19f, StarGap = 0.7f, Rising = false },   // 금
            new() { Burst = 20, BurstStars = 8, SecondBurst = true, MoteGap = 0.15f, StarGap = 0.45f, Rising = true },    // 백금
        };

        private ChestTier _tier = Tiers[0];

        private Image _goldGlow, _goldBack, _goldFront, _chestGlow, _chestRise, _chestHalo;
        private Sprite[] _chestHaloFrames;
        private static readonly Vector2 HaloSize = new(240f, 150f);   // 2배 칸 480x300 의 반 — 상자(140x96)가 가운데
        private Sprite[] _goldBurst, _goldLoop, _goldSparkle, _chestRiseFrames, _moteSprites;
        private Color _grade = Color.white;
        private bool _hasChest;
        private float _goldAt, _landAt, _clock = -1f;
        private float _nextGoldMote, _nextChestMote, _nextChestStar;
        private bool _chestBurstDone, _secondBurstDone;
        private float _nextRising;

        private Material _lightSource;   // 번들 ParticleFx/glight — 빛 셰이더 원본
        private Material _moteMaterial;  // 번들 ParticleFx/ghostmote — 유령 빛 알갱이
        private bool _loading;
        private ParticleSystem _motes;       // 금화 — 유령 빛 알갱이
        private ParticleSystem _chestMotes;  // 상자 — 반짝 별 · 도트 알갱이(그림 2종)
        private Material _additive;
        private Image _pile, _chest;

        private void Update()
        {
            if (_clock < 0f) return;
            _clock += Time.unscaledDeltaTime;
            EnsureLight();

            float g = _clock - _goldAt;
            Glow(_goldGlow, GoldGlowColor, g, 0.42f, 0.12f, 2.2f);
            Frames2(_goldBack, _goldFront, _goldBurst, _goldLoop, _goldSparkle, g, Color.white);
            if (g >= 0.3f && _clock >= _nextGoldMote)
            {
                _nextGoldMote = _clock + Random.Range(0.45f, 0.75f);
                Mote(_pile, new Vector2(Random.Range(-0.38f, 0.38f), Random.Range(-0.1f, 0.25f)),
                     Color.Lerp(GoldGlowColor, Color.white, 0.5f));
            }

            if (!_hasChest) return;
            float c = _clock - _landAt;
            Glow(_chestGlow, _grade, c, 0.45f, 0.1f, 2.0f);   // 넓고 옅은 받침 — 상자 모양 빛은 후광 그림이 맡는다
            Rise(c);
            Halo(c);
            if (c < 0f) return;
            var light = Color.Lerp(_grade, Color.white, 0.45f);
            if (!_chestBurstDone && _chestMotes != null)
            {
                // 착지(강) — 별 · 알갱이가 바닥에서 위로 확 튄다
                _chestBurstDone = true;
                Burst(_tier.Burst, _tier.BurstStars, light);
            }
            if (_tier.SecondBurst && !_secondBurstDone && c >= 0.5f)
            {
                _secondBurstDone = true;
                Burst(8, 4, Color.Lerp(light, Color.white, 0.3f));
            }
            if (_tier.Rising && _clock >= _nextRising)
            {
                // 백금 머묾 — 상자 둘레에서 빛 알갱이가 위로 흘러오른다(더 빠르고 길게)
                _nextRising = _clock + Random.Range(0.2f, 0.35f);
                ChestMote(new Vector2(Random.Range(-0.5f, 0.5f), -0.35f), new Vector2(0f, Random.Range(40f, 60f)),
                          Random.Range(8f, 11f), Random.Range(0.9f, 1.2f), Color.Lerp(light, Color.white, 0.3f), 1);
            }
            if (_clock >= _nextChestMote)
            {
                // 머묾(약) — 도트 알갱이가 상자 둘레에서 천천히 떠오른다
                _nextChestMote = _clock + _tier.MoteGap * Random.Range(0.8f, 1.3f);
                ChestMote(new Vector2(Random.Range(-0.55f, 0.55f), Random.Range(-0.45f, 0.3f)),
                          new Vector2(Random.Range(-4f, 4f), Random.Range(14f, 26f)),
                          Random.Range(13f, 19f), Random.Range(1.1f, 1.6f), Color.Lerp(_grade, Color.white, 0.2f), 1);
            }
            if (_clock >= _nextChestStar)
            {
                // 머묾(약) — 상자 모서리 · 둘레에서 반짝 별이 하나씩
                _nextChestStar = _clock + _tier.StarGap * Random.Range(0.8f, 1.3f);
                ChestMote(new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.1f, 0.45f)), Vector2.zero,
                          Random.Range(14f, 20f), Random.Range(0.4f, 0.55f), Color.Lerp(light, Color.white, 0.4f), 0);
            }
        }

        /// <summary>착지 터짐 — 알갱이 <paramref name="motes"/> 개 + 반짝 별 <paramref name="stars"/> 개가 바닥에서 위로 튄다.</summary>
        private void Burst(int motes, int stars, Color light)
        {
            for (int i = 0; i < motes + stars; i++)
            {
                float deg = Random.Range(-65f, 65f) + 90f;
                var dir = new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));
                bool star = i >= motes;
                ChestMote(new Vector2(Random.Range(-0.4f, 0.4f), star ? Random.Range(-0.3f, 0.2f) : -0.42f),
                          dir * Random.Range(70f, 160f), star ? Random.Range(14f, 20f) : Random.Range(9f, 16f),
                          Random.Range(0.55f, 0.85f), light, star ? 0 : 1);
            }
        }

        /// <summary>
        /// 머묾 후광 — 시안 머묾처럼 상자 모양을 따라 감싸는 부드러운 빛(그림 한 장, 윤곽선 없음).
        /// 착지에 0.25초 동안 밝게 차오르고 이후 숨쉰다(2.0초). 등급 단계가 진하기 · 크기를 올린다.
        /// </summary>
        private void Halo(float since)
        {
            if (_chestHalo == null || _chestHaloFrames == null || _chestHaloFrames.Length == 0) return;
            if (since < 0f) { _chestHalo.enabled = false; return; }
            float inK = Ease.OutCubic(Mathf.Clamp01(since / 0.25f));
            float breathe = 0.5f - 0.5f * Mathf.Cos(since * Mathf.PI * 2f / 2.0f);
            float a = inK * Mathf.Lerp(0.8f, 1f, breathe);
            _chestHalo.sprite = _chestHaloFrames[0];
            _chestHalo.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, inK) * Mathf.Lerp(1f, 1.04f, breathe);
            _chestHalo.color = new Color(_grade.r, _grade.g, _grade.b, a);
            _chestHalo.enabled = true;
        }

        /// <summary>착지(강): 바닥 빛살이 OutBack 으로 솟구쳤다(0.28초) 0.9초에 걸쳐 사라진다. 두 장을 번갈아 깜빡여 일렁이게.</summary>
        private void Rise(float since)
        {
            if (_chestRise == null || _chestRiseFrames == null || _chestRiseFrames.Length == 0) return;
            if (since < 0f || since > 0.95f)
            {
                _chestRise.enabled = false;
                return;
            }
            float up = Ease.OutBack(Mathf.Clamp01(since / 0.28f));
            float fade = 1f - Mathf.Clamp01((since - 0.35f) / 0.6f);
            _chestRise.rectTransform.localScale = new Vector3(Mathf.Lerp(0.7f, 1f, up), Mathf.LerpUnclamped(0.15f, 1f, up), 1f);
            _chestRise.sprite = _chestRiseFrames[(int)(since / RiseFlickerStep) % _chestRiseFrames.Length];
            // 등급색 그대로 — 흰색을 섞으면 시안의 선명한 파란 빛살이 하얗게 뿌옇게 됐다(녹화 vH)
            _chestRise.color = new Color(_grade.r, _grade.g, _grade.b, fade);
            _chestRise.enabled = true;
        }

        /// <summary>
        /// 결과창이 뜰 때 한 번. 시각은 창이 뜬 때부터의 초(금화가 다 쌓인 때 · 상자가 닿는 때).
        /// <paramref name="tier"/> = 상자 등급 단계(0 은 · 1 금 · 2 백금).
        /// </summary>
        public void Begin(PopupFxPlayer fx, Material additive, Image pile, Image chest, Color grade, int tier,
                          bool hasChest, float goldAt, float landAt)
        {
            _tier = Tiers[Mathf.Clamp(tier, 0, Tiers.Length - 1)];
            if (fx == null) return;
            _goldBurst = fx.FramesOf("result_gold_burst");
            _goldLoop = fx.FramesOf("result_gold_glow");
            _goldSparkle = fx.FramesOf("result_gold_sparkle");
            _chestRiseFrames = fx.FramesOf("result_chest_rise");
            _chestHaloFrames = fx.FramesOf("result_chest_halo");
            _moteSprites = fx.FramesOf("result_mote");
            _additive = additive;
            _grade = grade;
            _pile = pile;
            _chest = chest;

            // Unity 객체라 ??= 를 쓰지 않는다(파괴된 객체를 null 로 못 본다 — coding_conventions 5절)
            if (_goldBack == null) _goldBack = Layer(pile, "ResultGoldBack", additive, GoldSize, behind: true);
            if (_goldFront == null) _goldFront = Layer(pile, "ResultGoldFront", additive, GoldSize, behind: false);
            if (_chestRise == null)
            {
                _chestRise = Layer(chest, "ResultChestRise", additive, RiseSize, behind: true);
                if (_chestRise != null)
                {
                    // 그림의 바닥 줄(아래에서 15/150)을 상자 밑변(가운데에서 48 아래)에 — 아래에서 위로 솟게 피벗을 바닥 줄에
                    var rt = _chestRise.rectTransform;
                    rt.pivot = new Vector2(0.5f, 0.1f);
                    rt.anchoredPosition += new Vector2(0f, -48f);
                }
            }
            if (_chestHalo == null) _chestHalo = Layer(chest, "ResultChestHalo", additive, HaloSize, behind: true);
            Hide(_goldGlow); Hide(_goldBack); Hide(_goldFront); Hide(_chestGlow); Hide(_chestRise); Hide(_chestHalo);
            if (_motes != null) _motes.Clear();
            if (_chestMotes != null) _chestMotes.Clear();
            _chestBurstDone = false;
            _secondBurstDone = false;
            _nextRising = landAt + 0.4f;

            _hasChest = hasChest;
            _goldAt = goldAt;
            _landAt = landAt;
            _nextGoldMote = goldAt + 0.4f;
            _nextChestMote = landAt + 0.5f;
            _nextChestStar = landAt + 0.7f;
            _clock = 0f;
            if (_lightSource == null && !_loading) LoadAsync().Forget();   // fire-and-forget: 오기 전엔 광원 · 알갱이 없이 돈다
        }

        // ── 1. 광원 ──────────────────────────────────────────────

        /// <summary>힘줄 박자: 0.35초에 0.6 → 1.45 배 · 알파 0 → 0.95 / 머묾: 1 배 근처 · <paramref name="rest"/> ± <paramref name="swing"/> 숨쉬기.</summary>
        private static void Glow(Image img, Color color, float since, float rest, float swing, float period)
        {
            if (img == null) return;
            if (since < 0f) { img.enabled = false; return; }
            float burst = Mathf.Clamp01(since / 0.35f);
            float settle = Mathf.Clamp01((since - 0.35f) / 0.5f);
            float breathe = 0.5f - 0.5f * Mathf.Cos(since * Mathf.PI * 2f / period);
            float scale = since < 0.35f ? Mathf.Lerp(0.6f, 1.45f, Ease.OutCubic(burst))
                                        : Mathf.Lerp(1.45f, 1f + 0.06f * breathe, Ease.OutCubic(settle));
            float alpha = since < 0.35f ? Mathf.Lerp(0f, 0.95f, Ease.OutCubic(burst))
                                        : Mathf.Lerp(0.95f, rest + swing * breathe, Ease.OutCubic(settle));
            img.rectTransform.localScale = Vector3.one * scale;
            img.color = new Color(color.r, color.g, color.b, alpha);
            img.enabled = true;
        }

        // ── 2 · 3. 금화 도트 빛살 · 반짝 별 ───────────────────────────

        /// <summary>터짐(강) 8장 → 머묾(약) 반복. 앞 반짝 별은 터짐 중반부터.</summary>
        private static void Frames2(Image back, Image front, Sprite[] burst, Sprite[] loop, Sprite[] sparkle, float since, Color tint)
        {
            if (since < 0f)
            {
                Hide(back);
                Hide(front);
                return;
            }
            float burstLen = BurstStep * Frames;
            if (since < burstLen) Show(back, burst, (int)(since / BurstStep), Color.white);
            else Show(back, loop, (int)((since - burstLen) / LoopStep), new Color(1f, 1f, 1f, LoopAlpha));

            float sinceSparkle = since - burstLen * 0.5f;
            if (sinceSparkle < 0f) Hide(front);
            else Show(front, sparkle, (int)(sinceSparkle / SparkleStep), tint);
        }

        private static void Show(Image img, Sprite[] frames, int index, Color color)
        {
            if (img == null || frames == null || frames.Length == 0) return;
            img.sprite = frames[index % frames.Length];
            img.color = color;
            img.enabled = true;
        }

        private static void Hide(Image img)
        {
            if (img != null) img.enabled = false;
        }

        // ── 3. 파티클 빛 알갱이 ────────────────────────────────────

        /// <summary>칸 안 비율 자리(가운데 0, ±0.5, y 위로 +)에서 알갱이 한 알이 천천히 떠오른다.</summary>
        private void Mote(Image node, Vector2 at, Color color)
        {
            if (_motes == null || node == null || !node.gameObject.activeInHierarchy) return;
            var r = node.rectTransform.rect;
            var local = new Vector2(r.center.x + at.x * r.width, r.center.y + at.y * r.height);
            var p = new ParticleSystem.EmitParams
            {
                position = node.rectTransform.TransformPoint(local),
                velocity = new Vector3(Random.Range(-6f, 6f), Random.Range(22f, 34f), 0f) * transform.lossyScale.x,
                startSize = Random.Range(9f, 13f),
                startLifetime = Random.Range(0.8f, 1.1f),
                startColor = new Color(color.r, color.g, color.b, 0.85f),
                applyShapeToPosition = false,
            };
            _motes.Emit(p, 1);
        }

        // ── 만들기 ───────────────────────────────────────────────

        private async UniTaskVoid LoadAsync()
        {
            _loading = true;
            var res = CoreModule.Get<IResourceManager>();
            try { _lightSource = await res.LoadAsync<Material>("ParticleFx/glight"); }
            catch (System.Exception e) { Debug.LogWarning($"[결과창] 빛 셰이더 재질 없음 — 광원 없이 돈다. {e.Message}"); }
            try { _moteMaterial = await res.LoadAsync<Material>("ParticleFx/ghostmote"); }
            catch (System.Exception e) { Debug.LogWarning($"[결과창] 빛 알갱이 재질 없음 — 알갱이 없이 돈다. {e.Message}"); }
            _loading = false;
        }

        /// <summary>재질이 오면 한 번 — 광원은 도트 빛살 층 바로 뒤, 알갱이는 결과창 캔버스 + 2.</summary>
        private void EnsureLight()
        {
            if (_lightSource != null && _goldGlow == null && _goldBack != null)
                _goldGlow = GlowUnder(_goldBack, "ResultGoldGlow", GoldGlowSize, Vector2.zero);
            if (_lightSource != null && _chestGlow == null && _chestRise != null)
                _chestGlow = GlowUnder(_chestRise, "ResultChestGlow", ChestGlowSize, new Vector2(0f, 48f), 0.42f, 0.5f);   // 시안 머묾처럼 상자 가장자리 바로 바깥이 가장 밝게 — 넓고 옅은 번짐은 안 보였다(녹화 vJ)   // 빛살 피벗(상자 밑변)에서 상자 가운데로
            if (_moteMaterial != null && _motes == null) _motes = NewMotes(BandOrder() + 2);
            if (_chestMotes == null && _additive != null && _moteSprites != null && _moteSprites.Length > 0)
                _chestMotes = NewChestMotes(BandOrder() + 2);
        }

        private Image GlowUnder(Image layer, string name, Vector2 size, Vector2 offset, float core = 0.10f, float soft = 0.42f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            var src = layer.rectTransform;
            rt.SetParent(src.parent, false);
            rt.SetSiblingIndex(src.GetSiblingIndex());   // 도트 빛살 층 바로 뒤
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = src.anchoredPosition + offset;
            rt.sizeDelta = size;
            var m = new Material(_lightSource) { hideFlags = HideFlags.DontSave };
            m.SetFloat("_Shape", 1f);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_RingRadius", 0f);
            // 심 + 번짐이 상자(uv) 반지름 안에서 끝나야 둥글다 — 0.5 + 0.8 은 모서리까지 꽉 차 네모 판이 됐다(녹화 vC)
            m.SetFloat("_RingWidth", core);
            m.SetFloat("_RingGlow", soft);   // 0.55 는 착지 순간 1.45 배로 커질 때 네모 모서리가 드러났다(녹화 vE)
            m.SetFloat("_RingWhite", 0.3f);
            var img = go.AddComponent<Image>();
            img.material = m;
            img.raycastTarget = false;
            img.enabled = false;
            return img;
        }

        private ParticleSystem NewMotes(int order)
        {
            var go = new GameObject("ResultMotes");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.useUnscaledTime = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startSpeed = 0f;
            main.maxParticles = 24;
            var emission = ps.emission;
            emission.enabled = false;            // 하나씩 직접 쏜다
            var shape = ps.shape;
            shape.enabled = false;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.4f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.3f)));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.alignment = ParticleSystemRenderSpace.Local;   // 캔버스 평면에 붙인다
            var c = GetComponentInParent<Canvas>();
            if (c != null) r.sortingLayerID = c.sortingLayerID;
            r.sortingOrder = order;
            r.sharedMaterial = _moteMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        /// <summary>
        /// 상자 파티클 한 알 — 칸 안 비율 자리(가운데 0, ±0.5, y 위로 +), 속도는 캔버스 픽셀/초.
        /// <paramref name="kind"/> 0 = 반짝 별 · 1 = 도트 알갱이(그림 시트의 칸 번호).
        /// </summary>
        private void ChestMote(Vector2 at, Vector2 velocityPx, float sizePx, float life, Color color, int kind)
        {
            if (_chestMotes == null || _chest == null) return;
            var r = _chest.rectTransform.rect;
            var local = new Vector2(r.center.x + at.x * r.width, r.center.y + at.y * r.height);
            var p = new ParticleSystem.EmitParams
            {
                position = _chest.rectTransform.TransformPoint(local),
                velocity = new Vector3(velocityPx.x, velocityPx.y, 0f) * transform.lossyScale.x,
                startSize = sizePx,
                startLifetime = life,
                startColor = color,
                applyShapeToPosition = false,
            };
            // 그림 시트에서 칸 고르기 — 시트 애니메이션은 시작 칸에 멈춰 있으므로 쏘기 직전에 시작 칸을 정한다
            var sheet = _chestMotes.textureSheetAnimation;
            sheet.startFrame = new ParticleSystem.MinMaxCurve(kind);
            _chestMotes.Emit(p, 1);
        }

        /// <summary>상자 파티클 — 반짝 별 · 도트 알갱이 그림 2종(popupfx 아틀라스)을 가산 재질로. 쏜 직후 빠르고 금방 선다.</summary>
        private ParticleSystem NewChestMotes(int order)
        {
            var go = new GameObject("ResultChestMotes");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.useUnscaledTime = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startSpeed = 0f;
            main.maxParticles = 96;   // 백금은 터짐 28 + 두 번째 12 + 머묾 알갱이가 겹친다
            var emission = ps.emission;
            emission.enabled = false;            // 하나씩 직접 쏜다
            var shape = ps.shape;
            shape.enabled = false;
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.drag = 3f;
            limit.multiplyDragByParticleSize = false;
            limit.multiplyDragByParticleVelocity = false;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.3f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0.2f)));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var sheet = ps.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Sprites;
            for (int i = 0; i < _moteSprites.Length; i++) sheet.AddSprite(_moteSprites[i]);
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.alignment = ParticleSystemRenderSpace.Local;   // 캔버스 평면에 붙인다
            var c = GetComponentInParent<Canvas>();
            if (c != null) r.sortingLayerID = c.sortingLayerID;
            r.sortingOrder = order;
            // 아틀라스 그림을 쓰려면 재질의 텍스처가 그 아틀라스 페이지여야 한다
            r.sharedMaterial = new Material(_additive) { hideFlags = HideFlags.DontSave, mainTexture = _moteSprites[0].texture };
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        private int BandOrder()
        {
            var canvases = GetComponentsInParent<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
                if (canvases[i].overrideSorting || canvases[i].isRootCanvas) return canvases[i].sortingOrder;
            return 0;
        }

        /// <summary>
        /// 물건 그림 바로 앞(뒤 층) 또는 바로 뒤(앞 층) 형제로 빛 한 장. 물건이 자르기 틀 안이면 틀 바깥에 둔다.
        /// 자리는 물건 그림 가운데 — 월드를 거쳐 옮긴다(틀 · 줄 · 칸 앵커가 서로 달라도 맞다).
        /// </summary>
        private static Image Layer(Image node, string name, Material additive, Vector2 size, bool behind)
        {
            if (node == null || !(node.transform.parent is RectTransform holder)) return null;
            var src = node.rectTransform;
            Transform anchor = src;
            if (holder.GetComponent<RectMask2D>() != null && holder.parent is RectTransform up)
            {
                anchor = holder;
                holder = up;
            }
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(holder, false);
            rt.SetSiblingIndex(behind ? anchor.GetSiblingIndex() : anchor.GetSiblingIndex() + 1);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            var center = (Vector2)holder.InverseTransformPoint(src.TransformPoint(src.rect.center));
            rt.anchoredPosition = center - holder.rect.center;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.material = additive;
            img.raycastTarget = false;
            img.enabled = false;
            return img;
        }
    }
}
