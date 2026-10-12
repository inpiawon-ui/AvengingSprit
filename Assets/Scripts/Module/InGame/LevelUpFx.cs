using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Common.UI;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Resource;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 레벨업 연출 — 카드 등장 · 머묾 · 고르는 순간 · 몸에 깃드는 순간 (등급 사다리, PD 2026-10-12 「적용해서 보여줘」).
    ///
    /// 정답 시안(`Projects/AVSR/_exchange/in/`) · 가이드 `guide_tiers_fx.md`:
    ///   (광택이 카드를 훑던 것은 뺐다 — PD 2026-10-12 「팝업 시작할 때 옆으로 지나가는 빗금 없애, 안 이뻐」)
    ///   등장 · 머묾 = 카드 테두리 바깥으로 등급색 빛이 은은히 번진다(카드 바로 뒤 부드러운 사각 빛) —
    ///          COMMON 없음 / RARE 옅게 / EPIC 알파 0.7 ~ 1 / LEGENDARY 0.7 ~ 1 더 넓게 + 반짝 별 — 1초에 한 번 숨쉼
    ///          (PD 2026-10-12 「카드 등급 표기 겉에 은은하게 블러, 빛줄기는 이상해」 · 「알파 0.7~1, 1초」 ·
    ///           「타이틀에만 강조하는 게 아니라」 · 「타이틀에 저렇게 하는 거 제거, 촌스러워」 — 위로 내려오던 빛기둥 · 명판 고리 빛은 뺐다)
    ///          + 창 둘레 금빛 알갱이(mock_lvpopup_free_idle, 등급 무관)
    ///   고름 = mock_lvpopup_free_peak — 고른 카드 둘레 등급색 빛살 + 알갱이(그대로)
    ///   깃듦 = mock_lvgain_free_peak 그대로(광원 · 빛기둥 · 바닥 고리 · 솟는 알갱이) — 새 시안의 가는 섬광으로 바꿨다가
    ///          PD 2026-10-12 「유령에 적용되는 이펙트는 전에께 더 이쁘고 좋았다, 굳이 퀄을 떨굴 필요 없다」로 되돌렸다
    /// 테두리 빛은 카드 모양 그대로 카드보다 조금만 크다 — 카드 사이 틈을 메우지 않는다
    /// (PD 2026-10-10 「카드 뒤에 있는 이펙트들 지저분해 보인다」 = 카드 뒤로 크게 번지던 둥근 광원이었다).
    /// 겹: 테두리 빛 · 깃듦 광원 · 빛기둥 · 고리 = 빛 셰이더(`ParticleFx/glight`) · 빛살 = `fx_levelup_card_rays` ·
    ///     반짝 별 · 알갱이 = 파티클(`fx_result_mote_1/2`).
    /// 크기는 모든 등급이 같고, 위로 갈수록 이펙트 종류 · 파티클 수를 더한다. 값은 <see cref="Tiers"/> 표 하나에.
    /// 시간은 실제 시간 — 레벨업 창 · 보상 연출은 게임 시간이 서 있어도 돈다.
    /// </summary>
    public sealed class LevelUpFx : MonoBehaviour
    {
        private struct Tier
        {
            public float Halo;          // 등장 · 머묾 — 카드 테두리 바깥 빛 가장 밝을 때 알파(0 = 없음)
            public float HaloLow;       // 숨쉬기 가장 어두울 때 알파
            public float HaloWide;      // 카드 밖으로 번지는 폭(px, 한쪽)
            public int AppearStars;     // 등장 — 테두리 빛이 차오를 때 터지는 반짝 별 수
            public float IdleStarGap;   // 머묾 — 카드 모서리 반짝 별 간격(초, 0 = 없음)
            public int PickBurst;       // 고름 — 튀는 알갱이 수
            public int PickStars;       // 고름 — 같이 터지는 반짝 별 수
            public bool PickSecond;     // 고름 — 0.25초 뒤 반짝임 한 번 더
            public int GainMotes;       // 깃듦 — 솟는 알갱이 수(초당)
            public bool GainSecondRing; // 깃듦 — 바닥 고리 두 번째 파동
            public bool GainStars;      // 깃듦 — 몸 둘레 반짝 별
        }

        // COMMON = 기본(테두리만), 위 등급은 이펙트 종류 · 파티클 수를 한 겹씩 — 크기는 같다(스킬 fx-art-pipeline 2-1)
        // 테두리 빛은 계단식 — 셋이 같은 세기(0.7 ~ 1)였더니 「딱 봤을 때 좋은 거 안 좋은 거 구분이 안 된다」(PD 2026-10-12).
        // 레어 옅게(0.3 ~ 0.45) · 에픽 0.7 ~ 1 · 전설 0.7 ~ 1 에 더 넓게 + 반짝 별
        private static readonly Tier[] Tiers =
        {
            new() { Halo = 0f, HaloLow = 0f, HaloWide = 0f, AppearStars = 0, IdleStarGap = 0f, PickBurst = 10, PickStars = 3, PickSecond = false, GainMotes = 10, GainSecondRing = false, GainStars = false },     // COMMON
            new() { Halo = 0.45f, HaloLow = 0.3f, HaloWide = 14f, AppearStars = 0, IdleStarGap = 0f, PickBurst = 14, PickStars = 5, PickSecond = false, GainMotes = 14, GainSecondRing = false, GainStars = true },       // RARE
            new() { Halo = 1f, HaloLow = 0.7f, HaloWide = 18f, AppearStars = 0, IdleStarGap = 0f, PickBurst = 18, PickStars = 7, PickSecond = true, GainMotes = 18, GainSecondRing = true, GainStars = true },     // EPIC
            new() { Halo = 1f, HaloLow = 0.7f, HaloWide = 26f, AppearStars = 8, IdleStarGap = 0.9f, PickBurst = 24, PickStars = 10, PickSecond = true, GainMotes = 24, GainSecondRing = true, GainStars = true }, // LEGENDARY
        };

        private const int CardCount = 3;
        // 빛살 그림(520x760)의 안쪽 구멍 실측 176x421 을 카드 162x320 에 맞춘 크기(가로 0.88 · 세로 0.74)
        private static readonly Vector2 RaysSize = new(458f, 562f);
        private const float AppearAt = 0.22f;      // 명판 빛이 가장 밝아지는 때(등장, 강)
        private const float HaloPulseSeconds = 1f;   // 테두리 빛 숨쉬기 1초(PD 「알파 0.7~1, 1초 정도」 — 세기는 표)

        private PopupFxPlayer _fx;
        private Material _light;   // 번들 ParticleFx/glight — 깃듦 광원 · 빛기둥 · 고리
        private bool _loading;
        private Material _additive;
        private Sprite[] _rays, _motes;

        // ── 카드(창) ──
        private readonly RectTransform[] _cards = new RectTransform[CardCount];
        private readonly Color[] _cardColor = new Color[CardCount];
        private readonly Tier[] _cardTier = new Tier[CardCount];
        private readonly float[] _nextIdleStar = new float[CardCount];
        private readonly Image[] _cardHalo = new Image[CardCount];
        private Material _haloMaterial;
        private readonly bool[] _appearDone = new bool[CardCount];
        private Image _rays0, _rays1;   // 같은 빛살 두 장을 겹쳐 더한다 — 한 장으로는 시안보다 흐렸다(녹화 vP)
        private ParticleSystem _panelParticles;
        private float _idleClock = -1f, _pickClock = -1f, _ambientNext;
        private int _picked = -1;
        private bool _pickSecondDone;

        // ── 깃듦(몸) ──
        private RectTransform _gainRoot;      // 몸 자리 표시(보상 연출 층) — 그리는 것은 모두 뒤 층
        private RectTransform _gainBackRoot;  // 뒤 — 광원 · 빛기둥 · 고리 · 알갱이(유령과 같은 층, 유령 바로 뒤)
        private Transform _gainAvatar;
        private Image _gainGlow, _gainPillar, _gainRing, _gainRing2;
        private float _gainClock = -1f, _gainLength, _gainNextMote, _gainNextStar;
        private Color _gainColor;
        private Tier _gainTier;
        private bool _gainBurstDone;

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_idleClock >= 0f) { _idleClock += dt; TickCards(); }
            if (_pickClock >= 0f) { _pickClock += dt; TickPick(); }
            if (_gainClock >= 0f) { _gainClock += dt; TickGain(); }
        }

        /// <summary>한 번 — 재질 · 그림을 받아 둔다.</summary>
        public void Init(PopupFxPlayer fx, Material additive)
        {
            _fx = fx;
            _additive = additive;
            RefreshFrames();
            if (_light == null && !_loading) LoadAsync().Forget();   // fire-and-forget: 오기 전엔 깃듦 광원 없이 돈다
        }

        /// <summary>연출 아틀라스는 늦게 온다 — 아직 없으면 쓸 때마다 다시 찾는다.</summary>
        private void RefreshFrames()
        {
            if (_fx == null) return;
            if (_rays == null) _rays = _fx.FramesOf("levelup_card_rays");
            if (_motes == null) _motes = _fx.FramesOf("result_mote");
        }

        // ── 1. 등장 · 머묾 ─────────────────────────────────────

        /// <summary>창이 열릴 때 — 등급마다 등장 연출을 건다.</summary>
        public void ShowCards(RectTransform[] cards, CardRarity[] rarities, Color[] colors)
        {
            RefreshFrames();
            _picked = -1;
            _pickClock = -1f;
            for (int i = 0; i < CardCount; i++)
            {
                _cards[i] = i < cards.Length ? cards[i] : null;
                _appearDone[i] = false;
                if (_cards[i] == null) continue;
                _cardColor[i] = colors[i];
                _cardTier[i] = Tiers[Mathf.Clamp((int)rarities[i], 0, Tiers.Length - 1)];
                _nextIdleStar[i] = AppearAt + Random.Range(0.4f, 0.9f);
                if (_cardHalo[i] != null) _cardHalo[i].enabled = false;
            }
            if (_rays0 == null && Has(_rays) && _cards[0] != null)
                _rays0 = Behind(_cards[0], "CardRays", _additive, RaysSize);
            if (_rays1 == null && _rays0 != null) _rays1 = Behind(_cards[0], "CardRays2", _additive, RaysSize);
            if (_rays0 != null) _rays0.enabled = false;
            if (_rays1 != null) _rays1.enabled = false;
            if (_panelParticles == null && Has(_motes) && _cards[0] != null) _panelParticles = NewParticles(_cards[0].parent, 1);
            if (_panelParticles != null) _panelParticles.Clear();
            _idleClock = 0f;
            _ambientNext = 0.2f;
        }

        /// <summary>창이 닫힐 때.</summary>
        public void HideCards()
        {
            _idleClock = -1f;
            _pickClock = -1f;
            if (_rays0 != null) _rays0.enabled = false;
            if (_rays1 != null) _rays1.enabled = false;
            for (int i = 0; i < CardCount; i++)
            {
                if (_cardHalo[i] != null) _cardHalo[i].enabled = false;
            }
            if (_panelParticles != null) _panelParticles.Clear();
        }

        private void TickCards()
        {
            float t = _idleClock;
            for (int i = 0; i < CardCount; i++)
            {
                var card = _cards[i];
                if (card == null || !card.gameObject.activeInHierarchy) continue;
                var tier = _cardTier[i];
                // 고르는 순간 테두리 빛은 모두 끈다 — 고른 카드는 빛살이 맡고, 카드가 포커스로 반투명해지는 동안
                // 뒤의 빛이 비쳐 카드가 통째로 등급색으로 물들었다(녹화 final_1012g)
                float pickFade = _picked < 0 ? 1f : 0f;
                TickHalo(i, t, tier, pickFade);
                if (_picked >= 0) continue;
                if (!_appearDone[i] && t >= AppearAt)
                {
                    // 등장(강) — 테두리 빛이 차오르는 순간 카드 둘레에서 반짝 별이 튄다
                    _appearDone[i] = true;
                    if (tier.AppearStars > 0)
                        Burst(_panelParticles, card, tier.AppearStars / 2, tier.AppearStars, Color.Lerp(_cardColor[i], Color.white, 0.5f), 0.5f);
                }
                if (tier.IdleStarGap > 0f && t >= _nextIdleStar[i])
                {
                    _nextIdleStar[i] = t + tier.IdleStarGap * Random.Range(0.8f, 1.3f);
                    var corner = new Vector2(Random.Range(-0.55f, 0.55f), Random.Range(0.3f, 0.6f));
                    Emit(_panelParticles, card, corner, Vector2.zero, Random.Range(14f, 19f), Random.Range(0.4f, 0.55f),
                         Color.Lerp(_cardColor[i], Color.white, 0.5f), 0);
                }
            }
            // 창 둘레 금빛 알갱이(자유 시안 머묾) — 등급과 상관없이 은은히
            if (_picked < 0 && t >= _ambientNext && _cards[1] != null)
            {
                _ambientNext = t + Random.Range(0.35f, 0.6f);
                Emit(_panelParticles, _cards[1], new Vector2(Random.Range(-2.1f, 2.1f), Random.Range(-0.7f, 0.9f)),
                     new Vector2(Random.Range(-3f, 3f), Random.Range(10f, 18f)), Random.Range(6f, 9f), Random.Range(1.2f, 1.8f),
                     new Color(1f, 0.82f, 0.4f, 0.8f), 1);
            }
        }

        /// <summary>
        /// 테두리 빛 — 카드 바로 뒤에 카드보다 조금 큰 부드러운 사각 빛. 카드가 가운데를 덮어 테두리 바깥만 번져 보인다.
        /// 등장 때 0 → 1 로 차오르고(<see cref="AppearAt"/>), 그 뒤로 알파가 1 ↔ 0.7 을 1초에 한 번 오간다.
        /// 카드를 따라간다(고른 카드가 커지면 같이).
        /// </summary>
        private void TickHalo(int i, float t, Tier tier, float pickFade)
        {
            if (tier.Halo <= 0f || pickFade <= 0f)
            {
                if (_cardHalo[i] != null) _cardHalo[i].enabled = false;
                return;
            }
            var halo = CardHalo(i);
            if (halo == null) return;
            var card = _cards[i];
            float up = Ease.OutCubic(Mathf.Clamp01(t / AppearAt));
            float pulse = t < AppearAt ? up
                : Mathf.Lerp(tier.HaloLow / tier.Halo, 1f, 0.5f + 0.5f * Mathf.Cos((t - AppearAt) * Mathf.PI * 2f / HaloPulseSeconds));
            var rt = halo.rectTransform;
            Follow(rt, card);
            rt.sizeDelta = card.rect.size + Vector2.one * (tier.HaloWide * 2f);
            rt.localScale = card.localScale;
            halo.color = WithAlpha(_cardColor[i], tier.Halo * pulse * pickFade);
            halo.enabled = true;
        }

        /// <summary>테두리 빛 한 장 — 빛 셰이더 재질이 와야 만든다. 카드 바로 앞 형제(= 카드 뒤에 그려진다).</summary>
        private Image CardHalo(int i)
        {
            if (_cardHalo[i] != null) return _cardHalo[i];
            if (_light == null || _cards[i] == null) return null;
            if (_haloMaterial == null) _haloMaterial = HaloMaterial();
            _cardHalo[i] = Behind(_cards[i], "CardHalo", _haloMaterial, _cards[i].rect.size);
            return _cardHalo[i];
        }

        /// <summary>
        /// 부드러운 사각 빛 — 빛 셰이더의 빛기둥 모양을 위아래 폭 같게, 결 · 심 없이 쓴다.
        /// 가장자리 번짐(가로 0.2 · 세로 0.11)은 카드 밖 18px 안에서 사그라들게 맞춘 값.
        /// </summary>
        private Material HaloMaterial()
        {
            var m = new Material(_light) { hideFlags = HideFlags.DontSave };
            m.SetFloat("_Shape", 0f);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_BottomWidth", 1f); m.SetFloat("_TopWidth", 1f); m.SetFloat("_EdgeSoft", 0.2f);
            m.SetFloat("_Body", 1f); m.SetFloat("_CoreWidth", 0f); m.SetFloat("_CoreBoost", 0f);
            m.SetFloat("_FadeTop", 0.11f); m.SetFloat("_FadeBottom", 0.11f);
            m.SetFloat("_EdgeLine", 0f); m.SetFloat("_Streak", 0f); m.SetFloat("_Scroll", 0f); m.SetFloat("_DashCount", 0f);
            return m;
        }

        // ── 2. 고름 — 카드 둘레 빛살 터짐 ───────────────────────────

        /// <summary>카드를 고른 순간. 카드 포커스(커짐)와 함께 돈다.</summary>
        public void Pick(int slot)
        {
            if (slot < 0 || slot >= CardCount || _cards[slot] == null) return;
            _picked = slot;
            _pickClock = 0f;
            _pickSecondDone = false;
            var tier = _cardTier[slot];
            var light = Color.Lerp(_cardColor[slot], Color.white, 0.35f);
            Burst(_panelParticles, _cards[slot], tier.PickBurst, tier.PickStars, light, 0.5f);
            if (_rays0 != null) _rays0.transform.SetSiblingIndex(_cards[slot].GetSiblingIndex());
            if (_rays1 != null) _rays1.transform.SetSiblingIndex(_cards[slot].GetSiblingIndex());
        }

        private void TickPick()
        {
            if (_picked < 0) return;
            var card = _cards[_picked];
            if (_rays0 != null && Has(_rays) && card != null)
            {
                // 고름(강) — 빛살이 OutBack 으로 확 펼쳐졌다(0.22초) 0.55초에 걸쳐 사라진다
                // 시간은 반으로 (PD 2026-10-12 「연출 시간이 너무 길다, 카드 선택 · 캐릭터에 효과 부여 시간을 반으로」) — 예전 0.22 · 0.3 + 0.55
                float up = Ease.OutBack(Mathf.Clamp01(_pickClock / 0.11f));
                float fade = 1f - Mathf.Clamp01((_pickClock - 0.15f) / 0.28f);
                Follow(_rays0.rectTransform, card);
                _rays0.sprite = _rays[0];
                _rays0.rectTransform.localScale = card.localScale * Mathf.LerpUnclamped(0.7f, 1f, up);
                var rc = Color.Lerp(_cardColor[_picked], Color.white, 0.2f);
                _rays0.color = WithAlpha(rc, fade);
                _rays0.enabled = fade > 0f;
                if (_rays1 != null)
                {
                    Follow(_rays1.rectTransform, card);
                    _rays1.sprite = _rays[0];
                    _rays1.rectTransform.localScale = _rays0.rectTransform.localScale * 1.04f;
                    _rays1.color = WithAlpha(_cardColor[_picked], fade);
                    _rays1.enabled = fade > 0f;
                }
            }
            if (_cardTier[_picked].PickSecond && !_pickSecondDone && _pickClock >= 0.12f && card != null)
            {
                _pickSecondDone = true;
                Burst(_panelParticles, card, 8, 5, Color.Lerp(_cardColor[_picked], Color.white, 0.55f), 0.5f);
            }
        }

        // ── 3. 깃듦 — 몸에 카드의 힘이 ─────────────────────────────

        /// <summary>
        /// 창이 닫힌 뒤 몸에. <paramref name="layer"/> = 보상 연출 층, <paramref name="body"/> = 층 좌표(왼쪽 위 0, 아래로 +)의 몸 가운데.
        /// </summary>
        public void Gain(RectTransform layer, Vector2 body, Transform avatar, CardRarity rarity, Color color, float seconds)
        {
            if (layer == null) return;
            RefreshFrames();
            EnsureGain(layer);
            // 광원 · 빛기둥 · 고리는 유령 **뒤** — 보상 층(위)에 두면 유령을 덮어 하얗게 지웠다(녹화 vQ2)
            if (_gainBackRoot != null && avatar != null && avatar.parent != null)
            {
                if (_gainBackRoot.parent != avatar.parent) _gainBackRoot.SetParent(avatar.parent, false);
                _gainAvatar = avatar;
                KeepBehindAvatar();
                _gainBackRoot.position = avatar is RectTransform art ? art.TransformPoint(art.rect.center) : avatar.position;
                _gainBackRoot.localScale = Vector3.one * (layer.lossyScale.x / Mathf.Max(0.0001f, _gainBackRoot.parent.lossyScale.x));
            }
            _gainTier = Tiers[Mathf.Clamp((int)rarity, 0, Tiers.Length - 1)];
            _gainColor = color;
            _gainLength = seconds;
            _gainClock = 0f;
            _gainBurstDone = false;
            _gainNextMote = 0f;
            _gainNextStar = 0.1f;
            _gainRoot.anchoredPosition = new Vector2(body.x, -body.y);
            ClearBackMotes();
        }

        // ── 깃듦 알갱이 — 유령 뒤 층의 UI 그림(파티클은 캔버스 맨 위라 유령 앞에 그려진다) ──

        private const int BackMoteCount = 72;
        private struct BackMote
        {
            public Image Img;
            public Vector2 Pos, Vel;
            public float Age, Life, Size;
            public Color Color;
        }
        private readonly BackMote[] _backMotes = new BackMote[BackMoteCount];

        private void EmitBack(Vector2 offsetPx, Vector2 velocityPx, float sizePx, float life, Color color, int kind)
        {
            if (_gainBackRoot == null || !Has(_motes)) return;
            for (int i = 0; i < _backMotes.Length; i++)
            {
                ref var m = ref _backMotes[i];
                if (m.Img != null && m.Img.enabled) continue;
                // 광원 · 빛기둥 · 고리보다 나중에 생겨 뒤 층 안에서는 그 위에 그려진다
                if (m.Img == null) m.Img = NewImage("GainMote", _gainBackRoot, _additive, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero);
                m.Img.sprite = _motes[Mathf.Min(kind, _motes.Length - 1)];
                m.Pos = offsetPx;
                m.Vel = velocityPx;
                m.Age = 0f;
                m.Life = life;
                m.Size = sizePx;
                m.Color = color;
                m.Img.enabled = true;
                return;
            }
        }

        /// <summary>파티클과 같은 결 — 끌림 3 · 크기 0.3 → 1 → 0.2 · 알파 0 → 1(15%) → 0.</summary>
        private void TickBackMotes(float dt)
        {
            float drag = Mathf.Exp(-3f * dt);
            for (int i = 0; i < _backMotes.Length; i++)
            {
                ref var m = ref _backMotes[i];
                if (m.Img == null || !m.Img.enabled) continue;
                m.Age += dt;
                float k = m.Age / m.Life;
                if (k >= 1f) { m.Img.enabled = false; continue; }
                m.Vel *= drag;
                m.Pos += m.Vel * dt;
                float size = k < 0.25f ? Mathf.Lerp(0.3f, 1f, k / 0.25f) : Mathf.Lerp(1f, 0.2f, (k - 0.25f) / 0.75f);
                float alpha = k < 0.15f ? k / 0.15f : 1f - (k - 0.15f) / 0.85f;
                var rt = m.Img.rectTransform;
                rt.anchoredPosition = m.Pos;
                rt.sizeDelta = Vector2.one * (m.Size * size);
                m.Img.color = WithAlpha(m.Color, m.Color.a * alpha);
            }
        }

        private void ClearBackMotes()
        {
            for (int i = 0; i < _backMotes.Length; i++)
                if (_backMotes[i].Img != null) _backMotes[i].Img.enabled = false;
        }

        private void EnsureGain(RectTransform layer)
        {
            if (_gainRoot != null) return;
            _gainRoot = NewRect("LevelUpGain", layer, Vector2.zero);
            _gainRoot.anchorMin = _gainRoot.anchorMax = new Vector2(0f, 1f);
            _gainBackRoot = NewRect("LevelUpGainBack", layer, Vector2.zero);
            if (_light != null)
            {
                _gainPillar = NewImage("GainPillar", _gainBackRoot, BeamMaterial(), new Vector2(130f, 340f), new Vector2(0.5f, 0f), new Vector2(0f, -40f));
                _gainRing = NewImage("GainRing", _gainBackRoot, RingMaterial(), new Vector2(190f, 56f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f));
                _gainRing2 = NewImage("GainRing2", _gainBackRoot, RingMaterial(), new Vector2(190f, 56f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f));
                _gainGlow = NewImage("GainGlow", _gainBackRoot, GlowMaterial(0.3f, 0.6f), new Vector2(240f, 240f), new Vector2(0.5f, 0.5f), Vector2.zero);
            }
        }

        /// <summary>
        /// 깃듦 뒤 층을 유령 바로 앞 형제로(= 유령 뒤에 그려진다).
        /// ⚠ 이미 같은 부모의 앞쪽에 있던 것을 유령 번호로 옮기면 유령이 한 칸 당겨져 연출이 유령 **앞**에 선다 —
        ///   첫 카드만 뒤였고 두 번째부터 앞이었다(PD 2026-10-12 「epic 빼고는 다들 뎁스가 안 맞아」). 매 프레임 확인한다(유닛이 늘면 순서가 바뀐다).
        /// </summary>
        private void KeepBehindAvatar()
        {
            if (_gainBackRoot == null || _gainAvatar == null || _gainBackRoot.parent != _gainAvatar.parent) return;
            int avatar = _gainAvatar.GetSiblingIndex();
            int back = _gainBackRoot.GetSiblingIndex();
            if (back == avatar - 1) return;
            _gainBackRoot.SetSiblingIndex(back < avatar ? avatar - 1 : avatar);
        }

        private void TickGain()
        {
            if (_gainRoot == null) return;
            KeepBehindAvatar();
            float t = _gainClock;
            if (t > _gainLength)
            {
                _gainClock = -1f;
                SetEnabled(false);
                ClearBackMotes();
                return;
            }
            // 깃듦 박자도 반으로 (PD 2026-10-12 「연출 시간이 너무 길다, 카드 선택 · 캐릭터에 효과 부여 시간을 반으로」) — 예전 0.3 · 0.35 · 0.25 · 0.4 · 고리 0.6
            float inK = Ease.OutCubic(Mathf.Clamp01(t / 0.15f));
            float outK = Mathf.Clamp01((t - (_gainLength - 0.18f)) / 0.18f);
            float life = inK * (1f - outK);
            var c = _gainColor;

            // 광원 — 깃드는 순간(강) 확 밝아졌다 머묾
            if (_gainGlow != null)
            {
                float flare = t < 0.12f ? Mathf.Lerp(0.6f, 1.35f, Ease.OutCubic(t / 0.12f)) : Mathf.Lerp(1.35f, 1f, Ease.OutCubic(Mathf.Clamp01((t - 0.12f) / 0.2f)));
                _gainGlow.rectTransform.localScale = Vector3.one * flare;
                _gainGlow.color = WithAlpha(c, (t < 0.12f ? 1f : 0.9f) * life);
                _gainGlow.enabled = true;
            }
            // 빛기둥 — 위에서 몸으로 내려와 선다
            if (_gainPillar != null)
            {
                _gainPillar.rectTransform.localScale = new Vector3(1f, Mathf.Lerp(0.2f, 1f, Ease.OutCubic(Mathf.Clamp01(t / 0.12f))), 1f);
                _gainPillar.color = WithAlpha(c, 0.75f * life);
                _gainPillar.enabled = true;
            }
            // 바닥 고리 — 퍼지며 사라진다(위 등급은 한 번 더)
            Ring(_gainRing, t, c, life);
            if (_gainTier.GainSecondRing) Ring(_gainRing2, t - 0.18f, c, life);
            else if (_gainRing2 != null) _gainRing2.enabled = false;
            // 파티클 — 깃드는 순간 터지고, 머무는 동안 위로 솟는다
            var light = Color.Lerp(c, Color.white, 0.4f);
            if (!_gainBurstDone)
            {
                _gainBurstDone = true;
                int stars = _gainTier.GainStars ? 6 : 2;
                for (int i = 0; i < _gainTier.GainMotes + stars; i++)
                {
                    bool star = i >= _gainTier.GainMotes;
                    float deg = Random.Range(0f, 360f);
                    var dir = new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));
                    EmitBack(Vector2.zero, dir * Random.Range(80f, 180f), star ? Random.Range(15f, 22f) : Random.Range(9f, 14f),
                             Random.Range(0.5f, 0.85f), light, star ? 0 : 1);
                }
            }
            if (t < _gainLength - 0.15f && t >= _gainNextMote)
            {
                _gainNextMote = t + 0.5f / Mathf.Max(1, _gainTier.GainMotes);   // 시간이 반이라 두 배로 쏴 알갱이 수는 그대로
                EmitBack(new Vector2(Random.Range(-46f, 46f), Random.Range(-40f, 10f)),
                         new Vector2(Random.Range(-6f, 6f), Random.Range(50f, 90f)), Random.Range(8f, 12f), Random.Range(0.6f, 0.9f), light, 1);
            }
            if (_gainTier.GainStars && t >= _gainNextStar && t < _gainLength - 0.15f)
            {
                _gainNextStar = t + Random.Range(0.09f, 0.15f);
                EmitBack(new Vector2(Random.Range(-60f, 60f), Random.Range(-50f, 60f)), Vector2.zero,
                         Random.Range(14f, 20f), Random.Range(0.35f, 0.5f), Color.Lerp(light, Color.white, 0.4f), 0);
            }
            TickBackMotes(Time.unscaledDeltaTime);
        }

        private static void Ring(Image ring, float t, Color c, float life)
        {
            if (ring == null) return;
            if (t < 0f) { ring.enabled = false; return; }
            float k = Mathf.Clamp01(t / 0.3f);
            ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.3f, Ease.OutCubic(k));
            ring.color = WithAlpha(c, (1f - k) * life);
            ring.enabled = k < 1f;
        }

        private void SetEnabled(bool on)
        {
            if (_gainGlow != null) _gainGlow.enabled = on;
            if (_gainPillar != null) _gainPillar.enabled = on;
            if (_gainRing != null) _gainRing.enabled = on;
            if (_gainRing2 != null) _gainRing2.enabled = on;
        }

        // ── 만들기 ───────────────────────────────────────────────

        private static bool Has(Sprite[] frames) => frames != null && frames.Length > 0;

        private async UniTaskVoid LoadAsync()
        {
            _loading = true;
            try { _light = await CoreModule.Get<IResourceManager>().LoadAsync<Material>("ParticleFx/glight"); }
            catch (System.Exception e) { Debug.LogWarning($"[레벨업] 빛 셰이더 재질 없음 — 광원 없이 돈다. {e.Message}"); }
            _loading = false;
        }

        /// <summary>둥근 번짐 — 빛 셰이더 타원(반지름 0). core · soft 는 심 · 번짐 폭.</summary>
        private Material GlowMaterial(float core, float soft)
        {
            var m = new Material(_light) { hideFlags = HideFlags.DontSave };
            m.SetFloat("_Shape", 1f);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_RingRadius", 0f);
            m.SetFloat("_RingWidth", core);
            m.SetFloat("_RingGlow", soft);
            m.SetFloat("_RingWhite", 0.25f);
            return m;
        }

        /// <summary>바닥 고리 — 유령 빛 고리와 같은 값.</summary>
        private Material RingMaterial()
        {
            var m = new Material(_light) { hideFlags = HideFlags.DontSave };
            m.SetFloat("_Shape", 1f);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_RingRadius", 0.8f);
            m.SetFloat("_RingWidth", 0.05f);
            m.SetFloat("_RingGlow", 0.25f);
            return m;
        }

        /// <summary>빛기둥 — 유령 빛 광막과 같은 결(위가 좁은 사다리꼴, 결이 흐른다).</summary>
        private Material BeamMaterial()
        {
            var m = new Material(_light) { hideFlags = HideFlags.DontSave };
            m.SetFloat("_Shape", 0f);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_BottomWidth", 1f); m.SetFloat("_TopWidth", 0.45f); m.SetFloat("_EdgeSoft", 0.2f);
            m.SetFloat("_Body", 0.4f); m.SetFloat("_CoreWidth", 0.07f); m.SetFloat("_CoreBoost", 1.1f);
            m.SetFloat("_FadeTop", 0.55f); m.SetFloat("_FadeBottom", 0.08f);
            m.SetFloat("_EdgeLine", 0.1f); m.SetFloat("_Streak", 0.2f); m.SetFloat("_Scroll", 0.6f); m.SetFloat("_DashCount", 0f);
            return m;
        }

        /// <summary>카드 바로 앞 형제로 한 장 — 카드에 가려지고 창 틀 위에 선다.</summary>
        private static Image Behind(RectTransform card, string name, Material material, Vector2 size)
        {
            var img = NewImage(name, (RectTransform)card.parent, material, size, new Vector2(0.5f, 0.5f), Vector2.zero);
            img.transform.SetSiblingIndex(card.GetSiblingIndex());
            img.enabled = false;
            return img;
        }

        /// <summary>카드 가운데를 따라간다(포커스로 커지거나 흐려져도).</summary>
        private static void Follow(RectTransform rt, RectTransform card)
        {
            rt.anchorMin = rt.anchorMax = card.anchorMin;
            var center = (Vector2)rt.parent.InverseTransformPoint(card.TransformPoint(card.rect.center));
            var parent = (RectTransform)rt.parent;
            var anchorPoint = parent.rect.min + Vector2.Scale(parent.rect.size, rt.anchorMin);
            rt.anchoredPosition = center - anchorPoint;
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            return rt;
        }

        private static Image NewImage(string name, RectTransform parent, Material material, Vector2 size, Vector2 pivot, Vector2 pos)
        {
            var rt = NewRect(name, parent, size);
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            var img = rt.gameObject.AddComponent<Image>();
            if (material != null) img.material = material;
            img.raycastTarget = false;
            img.enabled = false;
            return img;
        }

        private ParticleSystem NewParticles(Transform parent, int orderAbove)
        {
            var go = new GameObject("LevelUpParticles");
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.useUnscaledTime = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startSpeed = 0f;
            main.maxParticles = 160;
            var emission = ps.emission;
            emission.enabled = false;
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
            for (int i = 0; i < _motes.Length; i++) sheet.AddSprite(_motes[i]);
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.alignment = ParticleSystemRenderSpace.Local;   // 캔버스 평면에 붙인다
            var c = parent.GetComponentInParent<Canvas>();
            if (c != null)
            {
                r.sortingLayerID = c.sortingLayerID;
                r.sortingOrder = c.sortingOrder + orderAbove;
            }
            r.sharedMaterial = new Material(_additive) { hideFlags = HideFlags.DontSave, mainTexture = _motes[0].texture };
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        /// <summary>알갱이 · 별이 바깥으로 튄다. <paramref name="spread"/> &gt; 0 이면 칸 테두리에서, 0 이면 가운데에서.</summary>
        private void Burst(ParticleSystem ps, RectTransform node, int motes, int stars, Color light, float spread)
        {
            for (int i = 0; i < motes + stars; i++)
            {
                bool star = i >= motes;
                float deg = Random.Range(0f, 360f);
                var dir = new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));
                var at = spread > 0f ? new Vector2(dir.x * 0.5f, dir.y * 0.5f) : Vector2.zero;
                Emit(ps, node, at, dir * Random.Range(80f, 180f), star ? Random.Range(15f, 22f) : Random.Range(9f, 14f),
                     Random.Range(0.5f, 0.85f), light, star ? 0 : 1);
            }
        }

        /// <summary>칸 비율 자리(가운데 0, ±0.5, y 위로 +)에서 한 알. kind 0 = 반짝 별, 1 = 알갱이.</summary>
        private void Emit(ParticleSystem ps, RectTransform node, Vector2 at, Vector2 velocityPx, float sizePx, float life, Color color, int kind)
        {
            if (ps == null || node == null) return;
            var r = node.rect;
            var local = new Vector2(r.center.x + at.x * r.width, r.center.y + at.y * r.height);
            EmitWorld(ps, node.TransformPoint(local), velocityPx, sizePx, life, color, kind);
        }

        /// <summary>노드 가운데에서 픽셀 오프셋으로 한 알.</summary>
        private void EmitAt(ParticleSystem ps, RectTransform node, Vector2 offsetPx, Vector2 velocityPx, float sizePx, float life,
                            Color color, int kind)
        {
            if (ps == null || node == null) return;
            var world = node.TransformPoint(node.rect.center + offsetPx);
            EmitWorld(ps, world, velocityPx, sizePx, life, color, kind);
        }

        private void EmitWorld(ParticleSystem ps, Vector3 world, Vector2 velocityPx, float sizePx, float life, Color color, int kind)
        {
            var p = new ParticleSystem.EmitParams
            {
                position = world,
                velocity = new Vector3(velocityPx.x, velocityPx.y, 0f) * ps.transform.lossyScale.x,
                startSize = sizePx,
                startLifetime = life,
                startColor = color,
                applyShapeToPosition = false,
            };
            var sheet = ps.textureSheetAnimation;
            sheet.startFrame = new ParticleSystem.MinMaxCurve(Mathf.Min(kind, _motes.Length - 1));
            ps.Emit(p, 1);
        }

        private static Color WithAlpha(Color c, float a) => new(c.r, c.g, c.b, a);
    }
}
