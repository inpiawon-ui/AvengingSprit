using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Common.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 보상 연출 — 「무엇을 얻었는지」를 캐릭터 위에서 보여 준다 (PD 2026-10-08 반려 뒤 다시 짬).
    ///
    /// ── 흐름(모든 보상 창이 같다) ─────────────────────────────
    ///   고른다 → 고른 것에 포커스 → 창이 닫힌다 → 전투가 잠깐 선다 →
    ///   캐릭터 몸에 효과에 맞는 이펙트(회복 · 공격 · 속도 · 체력 상한 · 저주 · 카드) +
    ///   머리 위에 아이콘 · 이름 · 효과 한 줄 → 사라지며 전투가 다시 돈다.
    ///
    /// ⚠ 색만 바꾼 공통 빛을 쓰지 않는다 — 효과마다 그림이 다르다(fx_present_*).
    /// ⚠ 연출 중 다른 창(카드 3택1 등)이 열리려 하면 끝난 뒤에 연다(<see cref="SetPanel"/>).
    /// </summary>
    public sealed partial class InGameMainUI
    {
        // ── 시간(초) — 곡선은 Ease 의 OutBack · OutCubic · InOutSine ──
        private const float PresentIconIn = 0.32f;
        private const float PresentHold = 1.05f;
        private const float PresentOut = 0.28f;
        private const float PresentHeadGap = 150f;   // 몸 가운데에서 머리 위 띠까지(px)

        private RectTransform _presentLayer;
        private LevelUpFx _levelUpFx;
        private bool _presenting;
        private readonly Queue<string> _panelsAfterPresent = new();

        /// <summary>어떤 효과인가 — 캐릭터에 띄울 그림을 고른다.</summary>
        private enum GainKind { Card, Heal, Power, Speed, Vital, Curse, Range }

        private struct GainShow
        {
            public Sprite Icon;
            public string Title;
            public string Sub;
            public Color SubColor;
            public string Sub2;          // 둘째 줄(악마의 거래: 대가) — 비면 안 띄운다
            public Color Sub2Color;
            public GainKind Kind;
            public Color Tint;           // 카드 흡수 그림에 곱할 색(등급색)
            public CardRarity Rarity;    // 카드 — 등급별 연출 단계(LevelUpFx)
        }

        private static string FxOf(GainKind k) => k switch
        {
            GainKind.Heal => "present_heal_burst",
            GainKind.Power => "present_power_up",
            GainKind.Speed => "present_speed_up",
            GainKind.Vital => "present_vital_up",
            GainKind.Curse => "present_curse",
            _ => "present_card_absorb",
        };

        // ── 진입점 ────────────────────────────────────────────────

        /// <summary>전투를 세우고 캐릭터 위에 보여 준 뒤 다시 돌린다. 기다리는 쪽이 없으면 fire-and-forget.</summary>
        private async UniTask PresentGainAsync(GainShow g)
        {
            var layer = EnsurePresentLayer();
            var avatar = _battle != null ? _battle.AvatarTransform : null;
            if (layer == null || avatar == null) return;

            _presenting = true;
            float total = PresentIconIn + PresentHold + PresentOut;
            if (_battle != null) _battle.HoldForPresentation(total + 0.1f);

            // ① 몸에 이펙트 — 몸 자리에 붙는다(FromAvatar)
            // 카드는 LevelUpFx 가 맡는다 — 카드 문양이 몸 둘레를 돌고 광원 · 빛기둥 · 고리(시안 mock_lvgain_free_peak)
            if (g.Kind == GainKind.Card && _levelUpFx != null)
                _levelUpFx.Gain(layer, PopupFxPlayer.PanelPoint(layer, avatar), avatar, g.Icon, g.Rarity, g.Tint, total);
            else if (_fx != null)
            {
                var fx = new PopupFxLayer
                {
                    Frames = FxOf(g.Kind), Phase = "accept", W = 230f, H = 230f, Step = 0.11f, T0 = 0f, T1 = 0f,
                    Loop = false, Additive = g.Kind != GainKind.Curse, Tint = g.Kind == GainKind.Card ? g.Tint : Color.white,
                    Alpha = 1f, Fade = 0.08f, FromAvatar = true,
                };
                _fx.Play("present", layer, new[] { fx }, "accept", PopupAdditiveMaterial(), Vector2.zero);
            }

            // ② 머리 위 띠 — 아이콘이 위에서 내려와 앉고(OutBack) 이름 · 효과가 뒤따른다
            var banner = BuildBanner(layer, g);
            var at = PopupFxPlayer.PanelPoint(layer, avatar);
            float x = Mathf.Clamp(at.x, 150f, layer.rect.width - 150f);
            // 둘째 줄(악마의 대가)이 있으면 그만큼 올린다 — 몸 위 HP 바에 붙지 않게
            float gap = PresentHeadGap + (string.IsNullOrEmpty(g.Sub2) ? 0f : 26f);
            float y = Mathf.Max(at.y - gap, 250f);
            var token = this.GetCancellationTokenOnDestroy();

            float t = 0f;
            while (t < total)
            {
                if (this == null || banner == null) break;
                t += Time.unscaledDeltaTime;
                float inK = Mathf.Clamp01(t / PresentIconIn);
                float outK = Mathf.Clamp01((t - PresentIconIn - PresentHold) / PresentOut);
                // 내려앉기 · 커지기는 OutBack, 퇴장은 위로 떠오르며 흐려진다(OutCubic)
                float s = Mathf.LerpUnclamped(0.55f, 1f, Ease.OutBack(inK));
                float rise = 26f * Ease.OutCubic(outK);
                banner.Root.anchoredPosition = new Vector2(x, -(y - 24f * (1f - Ease.OutCubic(inK)) - rise));
                banner.Icon.localScale = Vector3.one * s;
                // 글자는 아이콘이 반쯤 내려온 뒤에 들어온다
                float textK = Mathf.Clamp01((t - PresentIconIn * 0.5f) / 0.22f);
                banner.Group.alpha = Mathf.Min(Ease.OutCubic(Mathf.Clamp01(t / 0.12f)), 1f - outK);
                banner.Texts.alpha = Ease.OutCubic(textK);
                if (await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow()) return;
            }
            if (banner != null && banner.Root != null) Destroy(banner.Root.gameObject);

            _presenting = false;
            if (_battle != null) _battle.ReleasePresentation();
            while (_panelsAfterPresent.Count > 0) SetPanel(_panelsAfterPresent.Dequeue(), true);
        }

        // ── 띠 ───────────────────────────────────────────────────

        private sealed class Banner
        {
            public RectTransform Root;
            public RectTransform Icon;
            public CanvasGroup Group;
            public CanvasGroup Texts;
        }

        private Banner BuildBanner(RectTransform layer, GainShow g)
        {
            var root = NewRect("GainBanner", layer, new Vector2(320f, 150f));
            root.anchorMin = root.anchorMax = new Vector2(0f, 1f);   // 연출 층 좌표(왼쪽 위 0, 아래로 +)로 놓는다
            var b = new Banner { Root = root, Group = root.gameObject.AddComponent<CanvasGroup>() };
            b.Group.blocksRaycasts = false;

            // 아이콘 — 얻은 것 그 자체(카드 그림 · 제단 상징 · 상점 물건)
            var icon = NewRect("GainIcon", root, new Vector2(84f, 84f));
            icon.anchoredPosition = new Vector2(0f, 40f);
            var img = icon.gameObject.AddComponent<Image>();
            img.sprite = g.Icon;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.enabled = g.Icon != null;
            b.Icon = icon;

            var texts = NewRect("GainTexts", root, new Vector2(320f, 80f));
            texts.anchoredPosition = new Vector2(0f, -30f);
            b.Texts = texts.gameObject.AddComponent<CanvasGroup>();

            // 글자 모양은 창 제목 · 안내와 같게(언어별 폰트 · 굵은 외곽선) — 창에 이미 서 있는 글자를 본뜬다
            var titleStyle = _ui.Get<TMP_Text>("EventTitleText");
            var subStyle = _ui.Get<TMP_Text>("EventRewardText");
            NewText(texts, "GainTitle", g.Title, titleStyle, 28f, Color.white, new Vector2(0f, 14f));
            NewText(texts, "GainSub", g.Sub, subStyle, 18f, g.SubColor, new Vector2(0f, -14f));
            if (!string.IsNullOrEmpty(g.Sub2))
                NewText(texts, "GainSub2", g.Sub2, subStyle, 18f, g.Sub2Color, new Vector2(0f, -38f));
            return b;
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            return rt;
        }

        private static void NewText(RectTransform parent, string name, string text, TMP_Text style, float size,
                                    Color color, Vector2 pos)
        {
            if (string.IsNullOrEmpty(text)) return;
            var rt = NewRect(name, parent, new Vector2(320f, 30f));
            rt.anchoredPosition = pos;
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (style != null)
            {
                t.font = style.font;
                t.fontSharedMaterial = style.fontSharedMaterial;
            }
            t.text = text;
            t.fontSize = size;
            t.enableAutoSizing = true;
            t.fontSizeMin = 12f;
            t.fontSizeMax = size;
            t.color = color;
            // 창 바탕 없이 전장 바닥 위에 서는 글자 — 어두운 외곽선이 있어야 읽힌다
            t.outlineWidth = 0.22f;
            t.outlineColor = new Color32(10, 12, 16, 255);
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.raycastTarget = false;
        }

        /// <summary>창들과 같은 자리 · 크기의 맨 위 층(정렬 밴드 HUD + 2) — 창 덮개(+1) 위에 띠가 선다.</summary>
        private RectTransform EnsurePresentLayer()
        {
            if (_presentLayer != null) return _presentLayer;
            var like = _ui.Find("EventPanel") as RectTransform;
            if (like == null) return null;
            var go = new GameObject("RewardPresentLayer", typeof(RectTransform));
            _presentLayer = (RectTransform)go.transform;
            _presentLayer.SetParent(like.parent, false);
            _presentLayer.anchorMin = like.anchorMin;
            _presentLayer.anchorMax = like.anchorMax;
            _presentLayer.pivot = like.pivot;
            _presentLayer.anchoredPosition = like.anchoredPosition;
            _presentLayer.sizeDelta = like.sizeDelta;
            _presentLayer.SetAsLastSibling();
            LiftToBand(_presentLayer, HudBandOrder + 2);
            return _presentLayer;
        }

        // ── 날아가는 것(제단 회복 구슬 · 상점 금화 · 산 물건) ─────────────

        /// <summary>
        /// 그림 한 장을 a → b 로 휘어 날린다(위로 볼록한 곡선, InOutSine). 다 오면 지운다.
        /// 좌표는 연출 층 기준(왼쪽 위 0, 아래로 +).
        /// </summary>
        private async UniTask FlyAsync(Sprite[] frames, Vector2 a, Vector2 b, float size, float delay, float seconds,
                                       float arc, Color color, bool shrinkAtEnd, bool faceMotion = false)
        {
            var layer = EnsurePresentLayer();
            if (layer == null || frames == null || frames.Length == 0) return;
            var token = this.GetCancellationTokenOnDestroy();
            if (delay > 0f && await UniTask.Delay(System.TimeSpan.FromSeconds(delay), ignoreTimeScale: true,
                    cancellationToken: token).SuppressCancellationThrow()) return;
            var rt = NewRect("Fly", layer, new Vector2(size, size));
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            var img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            img.color = color;
            var mid = (a + b) * 0.5f + new Vector2(0f, -arc);
            float t = 0f;
            while (t < seconds)
            {
                if (this == null || rt == null) return;
                t += Time.unscaledDeltaTime;
                float k = Ease.InOutSine(Mathf.Clamp01(t / seconds));
                // 2차 베지어 — 곧게 날지 않고 위로 휘어 날아간다
                var p = (1 - k) * (1 - k) * a + 2 * (1 - k) * k * mid + k * k * b;
                var prev = rt.anchoredPosition;
                rt.anchoredPosition = new Vector2(p.x, -p.y);
                // 꼬리 달린 그림(회복 구슬)은 나는 쪽을 본다 — 그림의 머리는 오른쪽(+x)
                var v = rt.anchoredPosition - prev;
                if (faceMotion && t > Time.unscaledDeltaTime && v.sqrMagnitude > 0.01f)
                    rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg);
                img.sprite = frames[(int)(t / 0.07f) % frames.Length];
                float s = shrinkAtEnd ? Mathf.Lerp(1f, 0.5f, Mathf.Clamp01((k - 0.75f) / 0.25f)) : 1f;
                rt.localScale = Vector3.one * s;
                if (await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow()) return;
            }
            if (rt != null) Destroy(rt.gameObject);
        }

        /// <summary>노드 가운데를 연출 층 좌표로.</summary>
        private Vector2 LayerPointOf(string node)
        {
            var layer = EnsurePresentLayer();
            var t = _ui.Find(node);
            return layer != null && t != null ? PopupFxPlayer.PanelPoint(layer, t) : Vector2.zero;
        }

        private Vector2 AvatarLayerPoint()
        {
            var layer = EnsurePresentLayer();
            var a = _battle != null ? _battle.AvatarTransform : null;
            return layer != null && a != null ? PopupFxPlayer.PanelPoint(layer, a) : new Vector2(360f, 700f);
        }

        // ── 고른 것에 포커스 ─────────────────────────────────────

        /// <summary>
        /// 고른 칸은 커지며(OutBack) <paramref name="toCenterX"/> 로 오고(제자리면 그대로), 나머지는 작아지며 흐려진다.
        /// 원래 자리 · 크기는 다음에 창을 열 때 <see cref="ResetFocus"/> 가 되돌린다.
        /// </summary>
        private async UniTask FocusAsync(string picked, string[] all, float toCenterX, float seconds,
                                         float othersAlpha = 0.25f)
        {
            var token = this.GetCancellationTokenOnDestroy();
            var p = _ui.Find(picked) as RectTransform;
            if (p == null) return;
            p.SetAsLastSibling();
            var start = p.anchoredPosition;
            var others = new List<CanvasGroup>();
            var otherT = new List<Transform>();
            foreach (var n in all)
            {
                if (n == picked || !(_ui.Find(n) is Transform o)) continue;
                var g = o.GetComponent<CanvasGroup>();
                if (g == null) g = o.gameObject.AddComponent<CanvasGroup>();
                others.Add(g);
                otherT.Add(o);
            }
            float t = 0f;
            while (t < seconds)
            {
                if (this == null || p == null) return;
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                float back = Ease.OutBack(k);
                p.localScale = Vector3.one * Mathf.LerpUnclamped(1f, 1.12f, back);
                p.anchoredPosition = new Vector2(Mathf.LerpUnclamped(start.x, toCenterX, Ease.OutCubic(k)), start.y);
                for (int i = 0; i < others.Count; i++)
                {
                    // 비키는 쪽은 앞 40% 안에 다 빠진다 — 고른 카드가 지나갈 때 글자가 겹쳐 보이지 않게
                    others[i].alpha = Mathf.Lerp(1f, othersAlpha, Ease.OutCubic(Mathf.Clamp01(k / 0.4f)));
                    otherT[i].localScale = Vector3.one * Mathf.Lerp(1f, 0.92f, Ease.OutCubic(k));
                }
                if (await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow()) return;
            }
        }

        // ── HP 바가 차오르는 것이 보이게 ─────────────────────────
        //
        // 회복(제단 · 상점 회복)을 받으면 바가 한 번에 바뀌지 않고 0.5초 동안 차오른다 — 「회복을 받았다」가 바에서도 읽힌다.
        // 줄어드는 쪽(맞음)은 예전처럼 바로 바뀐다.

        private const float FillRiseSeconds = 0.5f;
        private readonly Dictionary<string, float> _fillShown = new();
        private readonly Dictionary<string, int> _fillRun = new();

        private void SetFillSmooth(string node, float ratio, float width)
        {
            float from = _fillShown.TryGetValue(node, out var f) ? f : ratio;
            _fillShown[node] = ratio;
            int run = _fillRun.TryGetValue(node, out var r) ? r + 1 : 1;
            _fillRun[node] = run;
            if (ratio <= from + 0.001f) { _ui.SetFill(node, ratio, width); return; }
            FillRiseAsync(node, from, ratio, width, run).Forget();   // fire-and-forget: 바 차오름은 기다릴 것이 없다
        }

        private async UniTaskVoid FillRiseAsync(string node, float from, float to, float width, int run)
        {
            var token = this.GetCancellationTokenOnDestroy();
            for (float t = 0f; t < FillRiseSeconds; t += Time.unscaledDeltaTime)
            {
                if (this == null || _fillRun[node] != run) return;
                _ui.SetFill(node, Mathf.Lerp(from, to, Ease.OutCubic(t / FillRiseSeconds)), width);
                if (await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow()) return;
            }
            if (this != null && _fillRun[node] == run) _ui.SetFill(node, to, width);
        }

        private readonly Dictionary<string, Vector2> _focusHome = new();

        /// <summary>창을 다시 열 때 — 포커스로 옮긴 자리 · 크기 · 흐림을 되돌린다.</summary>
        private void ResetFocus(string[] all)
        {
            foreach (var n in all)
            {
                if (!(_ui.Find(n) is RectTransform rt)) continue;
                if (!_focusHome.TryGetValue(n, out var home)) _focusHome[n] = home = rt.anchoredPosition;
                rt.anchoredPosition = home;
                rt.localScale = Vector3.one;
                var g = rt.GetComponent<CanvasGroup>();
                if (g != null) g.alpha = 1f;
            }
        }
    }
}
