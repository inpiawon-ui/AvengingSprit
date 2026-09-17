using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Module.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 스킬 시전 연출 — 화면 쪽 (2026-09-17, 시안 `AVSR_SkillCast_draft`).
    ///
    ///   대기     스킬 버튼 테두리가 몸 색으로 숨 쉬듯 빛난다 · 반짝이가 깜빡인다
    ///   0.00     버튼이 눌렸다 튀어 오른다
    ///   0.05~    화면이 어두워지고 가장자리가 몸 색으로 달아오른다 · 컷인 띠(초상 + 스킬 이름)
    ///   ~0.5     띠가 빠지고 화면이 돌아온다
    ///
    /// 필드 쪽 빛 · 링 · 마법진은 `BattleDirector.SkillCast` 가 그린다.
    ///
    /// ⚠ **그림은 전부 발주본이다.** 코드로 도형 · 단색 판을 그리지 않는다(기획 2026-09-17).
    ///   화면 어둡게(`skillcast_dim`) · 가장자리 빛(`skillcast_edge_glow`)은 아직 안 왔다 —
    ///   그림이 없으면 그 겹은 **건너뛴다.** 대신 단색을 깔지 않는다.
    /// ⚠ 실제 시간으로 잰다. 레벨업 창 등으로 전투가 멈춰도 연출이 얼지 않게.
    /// ⚠ 노드는 코드로 만든다. 아틀라스가 온 뒤에 만들어야 그림이 붙는다(`SkinSkillCast`).
    /// </summary>
    public sealed partial class InGameMainUI
    {
        private const float CutInSlideSeconds = 0.09f;
        // 0.34 는 「딱 생기고 바로 없어진다」 — 0.5초 더 세워 둔다(기획 2026-09-17)
        // ⚠ 들어오는 시간 + 이 값 = `BattleDirector.CastFreezeSeconds`. 띠가 빠지는 순간 스킬이 나간다.
        private const float CutInHoldSeconds = 0.54f;   // 0.84 에서 0.2초 → 0.1초 더 뺐다(기획 2026-09-17)
        private const float CutInOutSeconds = 0.13f;
        private const float CutInAngle = 2.5f;
        private const float CutInY = 170f;                 // 화면 가운데에서 위로
        private const float ReadyFrameSeconds = 0.12f;
        private const bool CastEdgeEnabled = false;
        private static readonly bool ReadyGlowOuterEnabled = false;
        private const float CastEdgeOverscanX = 70f;   // 화면 밖으로 내보내는 폭(캔버스 px)
        private const float CastEdgeOverscanY = 110f;
        private const float CastEdgeAlpha = 0.5f;      // 가장 밝을 때도 반만
        private const float ButtonPunchSeconds = 0.24f;

        private RectTransform _castLayer;
        private CanvasGroup _castGroup;
        private Image _castDim;
        private Image _castEdge;
        private RectTransform _cutIn;
        private Image _cutInBand;
        private Image _cutInPortrait;
        private TMP_Text _cutInTag;
        private TMP_Text _cutInName;
        private TMP_Text _cutInNameEn;

        private Image _readyGlow;
        private Image _readyGlowOuter;
        private Image _readySpark;
        private Sprite[] _readyFrames;
        private float _readyTimer;
        private int _readyIndex;
        private Color _castColor = Color.white;

        private CancellationTokenSource _castCts;

        /// <summary>아틀라스가 온 뒤 한 번. 없는 그림은 그 겹을 만들지 않는다.</summary>
        private void SkinSkillCast()
        {
            if (_uiAtlas == null || _castLayer != null) return;

            // ── 스킬 버튼 준비 빛 ──
            var button = _ui.Find("SkillButton") as RectTransform;
            var f1 = _uiAtlas.GetSprite("skillcastready_1");
            if (button != null && f1 != null)
            {
                _readyFrames = new[] { f1, _uiAtlas.GetSprite("skillcastready_2") ?? f1,
                                       _uiAtlas.GetSprite("skillcastready_3") ?? f1 };
                // 바깥 겹 — 같은 그림을 크게 한 장 더 깔아 빛이 멀리 번지게 한다.
                // ⚠ 한 겹이면 밝은 바닥(CH5 민트)에서 빛이 바닥에 묻혔다(기획 2026-09-17).
                // ⚠ 두꺼운 준비 빛(바깥 그림자 포함)을 받은 뒤로는 한 겹이면 된다 — 두 겹이면 과하다.
                if (ReadyGlowOuterEnabled)
                {
                    _readyGlowOuter = MakeImage("SkillReadyGlowOuter", button, f1, new Vector2(171f, 171f) * 1.18f,
                                                Vector2.zero);
                    _readyGlowOuter.transform.SetAsLastSibling();
                }
                // 그림 가운데 구멍(100×112)이 버튼 크기와 같게 잘라 두었다 — 원본 크기 그대로 쓴다
                _readyGlow = MakeImage("SkillReadyGlow", button, f1, new Vector2(171f, 171f), Vector2.zero);
                _readyGlow.transform.SetAsLastSibling();

                var spark = _uiAtlas.GetSprite("skillcastspark");
                if (spark != null)
                    _readySpark = MakeImage("SkillReadySpark", button, spark, new Vector2(34f, 34f),
                                            new Vector2(44f, 50f));
            }

            // ── 화면 겹 ──
            var go = new GameObject("SkillCastLayer", typeof(RectTransform), typeof(CanvasGroup));
            _castLayer = (RectTransform)go.transform;
            _castLayer.SetParent(transform, false);
            _castLayer.anchorMin = Vector2.zero;
            _castLayer.anchorMax = Vector2.one;
            _castLayer.offsetMin = _castLayer.offsetMax = Vector2.zero;
            _castGroup = go.GetComponent<CanvasGroup>();
            _castGroup.blocksRaycasts = false;
            _castGroup.interactable = false;

            var dim = _uiAtlas.GetSprite("skillcast_dim");
            if (dim != null) _castDim = MakeStretch("SkillCastDim", dim);
            var edge = _uiAtlas.GetSprite("skillcast_edge_glow");
            // 가장자리 빛은 **일단 끈다**(기획 2026-09-17 「일단 안 뜨게 해 보고」). 다시 켜려면 true.
            if (edge != null && CastEdgeEnabled)
            {
                _castEdge = MakeStretch("SkillCastEdge", edge);
                // 화면에 딱 맞추면 테두리가 두껍고 과했다(기획 2026-09-17 「은은하고 얇게」).
                // 그림을 화면보다 크게 깔아 바깥쪽 절반은 화면 밖으로 내보낸다 — 안쪽으로 뻗는 두께가 준다.
                var rt = _castEdge.rectTransform;
                rt.offsetMin = new Vector2(-CastEdgeOverscanX, -CastEdgeOverscanY);
                rt.offsetMax = new Vector2(CastEdgeOverscanX, CastEdgeOverscanY);
            }

            var band = _uiAtlas.GetSprite("skillcastband");
            if (band != null)
            {
                var cutGo = new GameObject("SkillCutIn", typeof(RectTransform));
                _cutIn = (RectTransform)cutGo.transform;
                _cutIn.SetParent(_castLayer, false);
                _cutIn.anchorMin = _cutIn.anchorMax = new Vector2(0.5f, 0.5f);
                _cutIn.sizeDelta = band.rect.size;
                _cutIn.localEulerAngles = new Vector3(0f, 0f, CutInAngle);

                _cutInBand = MakeImage("Band", _cutIn, band, band.rect.size, Vector2.zero);
                _cutInPortrait = MakeImage("Portrait", _cutIn, null, new Vector2(150f, 150f), new Vector2(-230f, 8f));
                _cutInPortrait.preserveAspect = true;

                var fontSource = _ui.Get<TMP_Text>("HostNameKrText");
                _cutInTag = MakeText("Tag", _cutIn, fontSource, 17f, new Vector2(60f, 46f), FontStyles.Bold);
                _cutInName = MakeText("Name", _cutIn, fontSource, 46f, new Vector2(60f, 4f), FontStyles.Bold);
                _cutInNameEn = MakeText("NameEn", _cutIn, fontSource, 17f, new Vector2(60f, -40f), FontStyles.Bold);
            }

            _castLayer.SetAsLastSibling();
            _castLayer.gameObject.SetActive(false);
        }

        private Image MakeImage(string name, Transform parent, Sprite sprite, Vector2 size, Vector2 at)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = at;
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        private Image MakeStretch(string name, Sprite sprite)
        {
            var img = MakeImage(name, _castLayer, sprite, Vector2.zero, Vector2.zero);
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return img;
        }

        private TMP_Text MakeText(string name, Transform parent, TMP_Text fontSource, float size, Vector2 at,
                                  FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(440f, size * 1.3f);
            rt.anchoredPosition = at;
            var t = go.GetComponent<TextMeshProUGUI>();
            if (fontSource != null)
            {
                t.font = fontSource.font;
                t.fontSharedMaterial = fontSource.fontSharedMaterial;
            }
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        // ── 대기: 준비 빛 ────────────────────────────────────────

        private void TickSkillReady(float dt)
        {
            if (_readyGlow == null) return;
            bool ready = _battle != null && _battle.SkillCooldownRatio >= 1f && _hasRealHost
                         && _readyGlow.transform.parent.gameObject.activeInHierarchy
                         && !(_battle.IsSkillSealed(_readyHostKey));
            if (_readyGlow.gameObject.activeSelf != ready)
            {
                _readyGlow.gameObject.SetActive(ready);
                if (_readyGlowOuter != null) _readyGlowOuter.gameObject.SetActive(ready);
                if (_readySpark != null) _readySpark.gameObject.SetActive(ready);
            }
            if (!ready) return;

            _readyTimer -= dt;
            if (_readyTimer <= 0f)
            {
                _readyTimer += ReadyFrameSeconds;
                _readyIndex = (_readyIndex + 1) % _readyFrames.Length;
                _readyGlow.sprite = _readyFrames[_readyIndex];
                if (_readyGlowOuter != null) _readyGlowOuter.sprite = _readyFrames[(_readyIndex + 1) % _readyFrames.Length];
            }
            // 숨 쉬듯 — 완전히 꺼지면 「준비됐다가 풀렸다」로 읽힌다. 0.8 아래로 안 내린다
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * 1.1f);
            // 안쪽 겹은 흰빛을 섞어 **밝게**, 바깥 겹은 몸 색 그대로 **넓게** — 어느 바닥에서든 한쪽은 튄다
            var inner = Color.Lerp(_castColor, Color.white, 0.45f);
            _readyGlow.color = new Color(inner.r, inner.g, inner.b, 0.8f + 0.2f * wave);
            _readyGlow.rectTransform.localScale = Vector3.one * (1f + 0.05f * wave);
            if (_readyGlowOuter != null)
            {
                _readyGlowOuter.color = new Color(_castColor.r, _castColor.g, _castColor.b, 0.45f + 0.45f * wave);
                _readyGlowOuter.rectTransform.localScale = Vector3.one * (0.96f + 0.1f * wave);
            }

            if (_readySpark != null)
            {
                float s = Mathf.Repeat(Time.unscaledTime * 0.9f, 1f);
                float a = s < 0.5f ? s * 2f : (1f - s) * 2f;
                _readySpark.color = new Color(1f, 1f, 1f, a);
                _readySpark.rectTransform.localScale = Vector3.one * (0.6f + 0.5f * a);
                _readySpark.rectTransform.localEulerAngles = new Vector3(0f, 0f, s * 90f);
            }
        }

        private string _readyHostKey;

        /// <summary>몸이 바뀌면 준비 빛 색도 그 몸의 것으로.</summary>
        private void SetSkillReadyHost(string hostKey)
        {
            _readyHostKey = hostKey;
            _castColor = _battle != null ? _battle.CastColorOf(hostKey) : Color.white;
        }

        // ── 시전 ─────────────────────────────────────────────────

        private void OnSkillCast(SkillCastEvent e)
        {
            _castCts?.Cancel();
            _castCts?.Dispose();
            _castCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            PlaySkillCastAsync(e, _castCts.Token).Forget();   // fire-and-forget: 연출만 — 다음 시전이 끊는다
        }

        private async UniTaskVoid PlaySkillCastAsync(SkillCastEvent e, CancellationToken ct)
        {
            PunchSkillButtonAsync(ct).Forget();   // fire-and-forget: 띠와 같은 시계로 따로 돈다
            if (_castLayer == null) return;

            var color = e.CastColor;
            _castLayer.gameObject.SetActive(true);
            _castLayer.SetAsLastSibling();
            _castGroup.alpha = 1f;

            if (_cutIn != null)
            {
                // ⚠ 폰트는 **시전 때마다** HUD 글자에서 다시 받는다. 이 노드는 화면이 뜬 뒤에
                //   코드로 만들어서, 언어를 바꿔도 폰트 교체 대상에 안 들어간다 —
                //   일본어 폰트로 한글을 그리면 네모가 뜬다.
                var fontSource = _ui.Get<TMP_Text>("HostNameKrText");
                if (fontSource != null)
                    foreach (var t in new[] { _cutInTag, _cutInName, _cutInNameEn })
                    {
                        t.font = fontSource.font;
                        t.fontSharedMaterial = fontSource.fontSharedMaterial;
                    }
                _cutInPortrait.sprite = _battle != null ? _battle.UnitSprite(e.CastHostKey) : null;
                _cutInPortrait.enabled = _cutInPortrait.sprite != null;
                // 띠 그림은 푸른 빛이 들어 있다 — 몸 색을 반만 섞어 빛이 죽지 않게
                _cutInBand.color = Color.Lerp(Color.white, color, 0.55f);
                _cutInTag.text = "ACTIVE SKILL";
                _cutInTag.color = color;
                _cutInName.text = e.SkillName;
                _cutInName.color = Color.white;
                _cutInNameEn.text = string.IsNullOrEmpty(e.SkillNameEn) ? string.Empty : e.SkillNameEn.ToUpperInvariant();
                _cutInNameEn.color = color;
            }
            if (_castEdge != null) _castEdge.color = new Color(color.r, color.g, color.b, 0f);
            if (_castDim != null) _castDim.color = new Color(1f, 1f, 1f, 0f);

            try
            {
                float total = CutInSlideSeconds + CutInHoldSeconds + CutInOutSeconds;
                float t = 0f;
                while (t < total)
                {
                    t += Time.unscaledDeltaTime;
                    float inK = Mathf.Clamp01(t / CutInSlideSeconds);
                    float outK = Mathf.Clamp01((t - CutInSlideSeconds - CutInHoldSeconds) / CutInOutSeconds);
                    float easeIn = 1f - (1f - inK) * (1f - inK) * (1f - inK);
                    float easeOut = outK * outK;
                    float shown = inK * (1f - outK);

                    if (_castDim != null) _castDim.color = new Color(1f, 1f, 1f, shown);
                    if (_castEdge != null)
                    {
                        // 가장자리는 들어올 때 가장 세게 번쩍이고 가라앉는다
                        float flare = Mathf.Lerp(1f, 0.6f, Mathf.Clamp01((t - CutInSlideSeconds) / CutInHoldSeconds));
                        _castEdge.color = new Color(color.r, color.g, color.b, shown * flare * CastEdgeAlpha);
                    }
                    if (_cutIn != null)
                    {
                        // 오른쪽에서 들어와 가운데 서고, 왼쪽으로 빠진다
                        float x = Mathf.Lerp(820f, 0f, easeIn) + Mathf.Lerp(0f, -820f, easeOut);
                        _cutIn.anchoredPosition = new Vector2(x, CutInY);
                        // 초상은 띠보다 한 박자 늦게 반대쪽에서 들어온다
                        float px = Mathf.Lerp(-120f, 0f, Mathf.Clamp01((t - 0.03f) / CutInSlideSeconds));
                        _cutInPortrait.rectTransform.anchoredPosition = new Vector2(-230f + px, 8f);
                    }
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
            }
            catch (System.OperationCanceledException)
            {
                return;   // 다음 시전이 이어 받는다 — 겹을 끄지 않는다
            }
            _castLayer.gameObject.SetActive(false);
        }

        private async UniTaskVoid PunchSkillButtonAsync(CancellationToken ct)
        {
            var button = _ui.Find("SkillButton");
            if (button == null) return;
            float t = 0f;
            try
            {
                while (t < ButtonPunchSeconds)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / ButtonPunchSeconds);
                    // 0.86 로 눌렸다가 1.08 로 튀고 1 로 앉는다
                    float s = k < 0.3f ? Mathf.Lerp(1f, 0.86f, k / 0.3f)
                            : k < 0.65f ? Mathf.Lerp(0.86f, 1.08f, (k - 0.3f) / 0.35f)
                            : Mathf.Lerp(1.08f, 1f, (k - 0.65f) / 0.35f);
                    button.localScale = Vector3.one * s;
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
            }
            catch (System.OperationCanceledException) { }
            if (button != null) button.localScale = Vector3.one;
        }
    }
}
