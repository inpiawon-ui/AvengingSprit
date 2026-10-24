using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Module.Common.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// RE:BORN 아웃게임 연출(2026-10-24) — 코덱스 자유 시안(_exchange/in/mock_rbfx_*_free_*)이 목표, 가이드 _exchange/guide_reborn_fx.md.
    /// 모양(링 · 별 터짐 · 상자 열림 · 금화)은 받은 도트 프레임, 빛은 가산 그림 + 반짝 알갱이(UI 그림 알갱이 — 캔버스 깊이를 따른다).
    /// 등급 계단: 크기는 같게 · 위로 갈수록 알갱이 수와 겹을 더한다(은 &lt; 금 &lt; 백금, 6성 &lt; 10성).
    /// 시간은 게임 시간과 무관(unscaled) — 로비는 멈춤이 없지만 창 연출이 Time.timeScale 에 묶이지 않게.
    /// </summary>
    public sealed class RbOutFx : MonoBehaviour
    {
        [SerializeField] private Material _additive;
        [SerializeField] private Sprite[] _ring6 = System.Array.Empty<Sprite>(), _ring10 = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _star = System.Array.Empty<Sprite>(), _flash = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _chestLight = System.Array.Empty<Sprite>(), _coin = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _chestSilver = System.Array.Empty<Sprite>(), _chestGold = System.Array.Empty<Sprite>(),
                                          _chestPlatinum = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite _shine, _band, _rays;

        private static readonly Color Cyan = new(0.24f, 0.9f, 1f), Gold = new(1f, 0.79f, 0.24f), Green = new(0.29f, 0.88f, 0.54f);

        // 상자 등급표 — 크기는 같고 알갱이 · 겹만 는다(fx-tier-leveling). 빛 그림 첨자: 0 은 · 1 금 · 2 백금
        private struct ChestTier
        {
            public int Light, Sparks, Idle;
            public Color Tint;
            public bool Rise, Second;
        }

        private static readonly Dictionary<string, ChestTier> Tiers = new()
        {
            ["silver"] = new ChestTier { Light = 0, Sparks = 16, Idle = 1, Tint = new Color(0.78f, 0.86f, 1f) },
            ["gold"] = new ChestTier { Light = 1, Sparks = 24, Idle = 2, Tint = Gold },
            ["platinum"] = new ChestTier { Light = 2, Sparks = 32, Idle = 3, Tint = new Color(1f, 0.93f, 1f), Rise = true, Second = true },
        };

        private readonly Stack<Image> _pool = new();
        private readonly Dictionary<RectTransform, CancellationTokenSource> _loops = new();

        // ── 강화 성공 틱(약) ─────────────────────────────────────
        // 칸 위 초록 빛 띠가 한 번 맥박 + 알갱이 둘이 오르고 · 값 글자가 톡

        public void UpgradeTick(RectTransform tile, RectTransform value)
        {
            if (tile == null) return;
            UpgradeTickAsync(tile, value, tile.GetCancellationTokenOnDestroy()).Forget();   // fire-and-forget: 0.3초 맥박
        }

        private async UniTaskVoid UpgradeTickAsync(RectTransform tile, RectTransform value, CancellationToken ct)
        {
            // 칸 판 위 · 글자 아래(08_fx 3절 — 빛이 숫자를 덮으면 안 된다): 칸의 첫 자식으로
            var band = Get(tile, "FxBand", _band, tile.rect.size, true);
            band.rectTransform.anchoredPosition = tile.rect.center;
            band.rectTransform.SetSiblingIndex(0);
            band.preserveAspect = false;
            band.color = Color.clear;
            Sparks(tile, new Vector2(0f, tile.rect.height * 0.2f), 2, Green, 60f, 0.6f, true, 14f);
            const float dur = 0.3f;
            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
            {
                float u = t / dur;
                band.color = new Color(1f, 1f, 1f, 0.55f * Mathf.Sin(u * Mathf.PI));   // 약 — 초록 다음 값 글자가 묻히지 않게
                if (value != null) value.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(Mathf.Min(1f, u * 1.5f) * Mathf.PI));
                if (await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow()) return;
            }
            if (value != null) value.localScale = Vector3.one;
            Release(band);
        }

        // ── 별 채움(중 → 강) ─────────────────────────────────────

        public void StarFill(RectTransform star)
        {
            if (star == null) return;
            StarFillAsync(star, star.GetCancellationTokenOnDestroy()).Forget();   // fire-and-forget: 0.9초 한 번
        }

        private async UniTaskVoid StarFillAsync(RectTransform star, CancellationToken ct)
        {
            float s = Mathf.Max(star.rect.width, star.rect.height);
            // 시안 star_fill — 터짐이 별 서너 개 폭으로 크게(첫 캡처는 별 크기라 안 보였다)
            var burst = Layer(star, "FxStar", null, Vector2.one * s * 5.5f, true, true);
            Sparks(star, Vector2.zero, 8, Gold, 180f, 0.8f, false, 24f);
            if (await Frames(burst, _star, 18f, ct)) return;
            Release(burst);
            // 머묾 — 채워진 별이 한 번 숨쉰다
            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                star.localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(t / 0.5f * Mathf.PI));
                if (await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow()) return;
            }
            star.localScale = Vector3.one;
        }

        // ── 버튼 빛 띠(약 · 상시) ─────────────────────────────────
        // 주요 버튼 위를 3초마다 왼 → 오 한 번. 버튼 안(RectMask2D)에서만 보인다

        public void ButtonShine(RectTransform button)
        {
            if (button == null || _shine == null || _loops.ContainsKey(button)) return;
            var cts = CancellationTokenSource.CreateLinkedTokenSource(button.GetCancellationTokenOnDestroy());
            _loops[button] = cts;
            ButtonShineAsync(button, cts.Token).Forget();   // fire-and-forget: 버튼이 사라질 때까지 되풀이
        }

        private async UniTaskVoid ButtonShineAsync(RectTransform button, CancellationToken ct)
        {
            var mask = button.Find("FxShineMask") as RectTransform;
            if (mask == null)
            {
                mask = new GameObject("FxShineMask", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
                mask.SetParent(button, false);
                mask.anchorMin = Vector2.zero; mask.anchorMax = Vector2.one; mask.sizeDelta = Vector2.zero;
                mask.SetSiblingIndex(1);   // 버튼 판 위 · 글자 아래
            }
            // 가산 셰이더는 RectMask2D 자르기를 받지 않아 버튼 밖으로 삐져나갔다 — 빛 띠는 보통 섞기(그림 투명도 = 밝기)
            var shine = Get(mask, "FxShine", _shine, new Vector2(button.rect.height * 2f, button.rect.height * 1.4f), false);
            shine.rectTransform.anchoredPosition = Vector2.zero;
            const float pass = 0.6f, wait = 3f;
            while (!ct.IsCancellationRequested)
            {
                float w = button.rect.width * 0.5f + shine.rectTransform.sizeDelta.x;
                for (float t = 0f; t < pass; t += Time.unscaledDeltaTime)
                {
                    float u = Ease.InOutSine(t / pass);
                    shine.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-w, w, u), 0f);
                    shine.color = new Color(1f, 1f, 1f, 0.75f * Mathf.Sin(u * Mathf.PI));
                    if (await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow()) return;
                }
                shine.color = Color.clear;
                if (await UniTask.Delay(System.TimeSpan.FromSeconds(wait), DelayType.UnscaledDeltaTime, cancellationToken: ct).SuppressCancellationThrow()) return;
            }
        }

        // ── 새 몸(강) · 6성 각성(강) · 10성 각성(가장 강) ──────────────
        // 몸 뒤: 새 몸 = 시안 빛살 + 섬광 · 6성 = 시안 링 폭발 · 10성 = 금 링 폭발 + 금 빛살(6성보다 한 겹 더, 크기는 같게).
        // 머묾: 빛살이 천천히 돌고(새 몸 · 10성) 링이 숨쉬며 알갱이가 오른다 — 쪽이 닫히면 멈춘다

        public void Reveal(RectTransform art, int rank)
        {
            if (art == null) return;
            Stop(art);
            var cts = CancellationTokenSource.CreateLinkedTokenSource(art.GetCancellationTokenOnDestroy());
            _loops[art] = cts;
            RevealAsync(art, rank, cts.Token).Forget();   // fire-and-forget: 등장 → 쪽이 닫힐 때까지 머묾
        }

        private async UniTaskVoid RevealAsync(RectTransform art, int rank, CancellationToken ct)
        {
            bool fresh = rank < RbRevealPage.AwakenStart, full = rank >= RbRevealPage.AwakenFull;
            float s = Mathf.Max(art.rect.width, art.rect.height) * 1.5f;
            Color tint = full ? Gold : Cyan;
            Image rays = null, ring = null;
            if (fresh || full)
            {
                rays = Layer(art, "FxRays", _rays, Vector2.one * s * (fresh ? 1.25f : 1.1f), true, false);
                rays.color = new Color(tint.r, tint.g, tint.b, 0f);
            }
            if (!fresh) ring = Layer(art, "FxRing", null, Vector2.one * s, true, false);
            // 섬광도 몸 뒤 — 앞에 두면 얼굴이 하얗게 덮였다(첫 캡처)
            var flash = Layer(art, "FxFlash", null, Vector2.one * s * 0.8f, true, false);
            flash.color = fresh ? Cyan : Color.white;
            Sparks(art, Vector2.zero, fresh ? 18 : full ? 30 : 22, tint, 260f, 0.9f, false, 20f);
            // 힘주는 순간 — 링 프레임 · 섬광 · 빛살이 0.6 → 1 로 퍼진다
            var ringTask = ring != null ? Frames(ring, full ? _ring10 : _ring6, 14f, ct, holdLast: false) : UniTask.FromResult(false);
            var flashTask = Frames(flash, _flash, 16f, ct);
            for (float t = 0f; t < 0.6f; t += Time.unscaledDeltaTime)
            {
                float u = Ease.OutCubic(t / 0.6f);
                if (rays != null)
                {
                    rays.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, u);
                    rays.color = new Color(tint.r, tint.g, tint.b, Mathf.Lerp(0f, 1f, u));
                }
                if (await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow()) { Clear(rays, ring, flash); return; }
            }
            bool ringStopped = await ringTask, flashStopped = await flashTask;
            if (ringStopped || flashStopped) { Clear(rays, ring, flash); return; }
            Release(flash);
            // 머묾 — 시안 머무는 장면: 6성은 발밑에 납작한 시안 링, 10성은 몸 둘레 깔끔한 금 두 겹 링 + 도는 빛살.
            // 터짐 뒷장(파편 많은 장)을 그대로 두면 지저분했다(첫 캡처) — 링이 또렷한 장을 고른다
            if (ring != null)
            {
                if (full && _ring10.Length > 4) ring.sprite = _ring10[4];
                else if (!full && _ring6.Length > 0)
                {
                    ring.sprite = _ring6[0];
                    ring.rectTransform.localScale = new Vector3(1.1f, 0.32f, 1f);
                    ring.rectTransform.anchoredPosition += new Vector2(0f, -art.rect.height * 0.42f);
                }
            }
            float next = 0f;
            for (float t = 0f; art.gameObject.activeInHierarchy; t += Time.unscaledDeltaTime)
            {
                float breathe = 0.5f + 0.5f * Mathf.Sin(t * 2.2f);
                if (ring != null) ring.color = new Color(1f, 1f, 1f, 0.28f + 0.22f * breathe);
                if (rays != null)
                {
                    rays.rectTransform.localEulerAngles = new Vector3(0f, 0f, -t * 12f);
                    rays.color = new Color(tint.r, tint.g, tint.b, 0.45f + 0.2f * breathe);
                }
                if (t >= next)
                {
                    next = t + (full ? 0.35f : 0.55f);
                    Sparks(art, new Vector2(Random.Range(-0.35f, 0.35f) * s * 0.6f, -s * 0.25f), 1, tint, 50f, 1.4f, true, 14f);
                }
                if (await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow()) break;
            }
            Clear(rays, ring, null);
        }

        // ── 상자 열기(등급 계단) ──────────────────────────────────
        // 머묾: 닫힌 상자가 가끔 흔들리고 틈으로 빛 알갱이가 샌다 · 힘주는 순간: 뚜껑이 열리며 빛이 솟고 알갱이가 튄다(백금은 0.5초 뒤 한 번 더)

        public void ChestIdle(Image chest, string grade)
        {
            if (chest == null) return;
            Stop(chest.rectTransform);
            var cts = CancellationTokenSource.CreateLinkedTokenSource(chest.GetCancellationTokenOnDestroy());
            _loops[chest.rectTransform] = cts;
            ChestIdleAsync(chest, Tier(grade), cts.Token).Forget();   // fire-and-forget: 열기 전까지 되풀이
        }

        private async UniTaskVoid ChestIdleAsync(Image chest, ChestTier tier, CancellationToken ct)
        {
            var rt = chest.rectTransform;
            // 시안 머무는 장면 — 상자 둘레 등급 빛이 옅게 숨쉰다(첫 캡처는 알갱이뿐이라 시안보다 약했다)
            var aura = Layer(rt, "FxChestAura", tier.Light < _chestLight.Length ? _chestLight[tier.Light] : null, rt.rect.size * 1.5f, true, false);
            aura.rectTransform.pivot = new Vector2(0.5f, 0.3f);
            float next = 0.4f;
            for (float t = 0f; chest.gameObject.activeInHierarchy; t += Time.unscaledDeltaTime)
            {
                aura.color = new Color(1f, 1f, 1f, 0.22f + 0.12f * Mathf.Sin(t * 2.4f));
                float cycle = t % 1.6f;   // 1.6초마다 0.3초 흔들림
                rt.localEulerAngles = new Vector3(0f, 0f, cycle < 0.3f ? Mathf.Sin(cycle * 60f) * 4f * (1f - cycle / 0.3f) : 0f);
                if (t >= next)
                {
                    next = t + 0.5f / tier.Idle;
                    Sparks(rt, new Vector2(Random.Range(-0.3f, 0.3f) * rt.rect.width, rt.rect.height * 0.1f), 1, tier.Tint, 70f, 1.1f, true, 16f);
                }
                if (await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow()) break;
            }
            rt.localEulerAngles = Vector3.zero;
            Release(aura);
        }

        /// <summary>뚜껑이 열리는 순간 — 끝나면(0.8초) 돌아온다. 상자 그림은 열린 장으로 바뀐 채 남는다.</summary>
        public async UniTask ChestOpenAsync(Image chest, string grade, CancellationToken ct)
        {
            if (chest == null) return;
            Stop(chest.rectTransform);
            var rt = chest.rectTransform;
            rt.localEulerAngles = Vector3.zero;
            var tier = Tier(grade);
            var frames = grade == "platinum" ? _chestPlatinum : grade == "silver" ? _chestSilver : _chestGold;
            var light = Layer(rt, "FxChestLight", tier.Light < _chestLight.Length ? _chestLight[tier.Light] : null,
                              rt.rect.size * 1.9f, true, false);
            light.rectTransform.pivot = new Vector2(0.5f, 0.2f);
            light.color = Color.clear;
            // 열림 4장(0.33초) — 마지막 장에서 빛이 솟는다.
            // 열림 칸은 192 · 키트 닫힌 상자는 144 칸 — 같은 도트 크기로 보이게 바닥 기준으로 192/144 배(rbfx_import fit_to_kit 가 바닥을 맞춰 둠)
            if (frames.Length > 0)
            {
                var keep = chest.sprite;
                chest.preserveAspect = true;
                SetPivotKeep(rt, new Vector2(0.5f, KitChestBottom));
                rt.localScale = Vector3.one * (192f / 144f);
                for (int i = 0; i < frames.Length; i++)
                {
                    chest.sprite = frames[i];
                    if (await UniTask.Delay(System.TimeSpan.FromSeconds(1f / 12f), DelayType.UnscaledDeltaTime, cancellationToken: ct).SuppressCancellationThrow())
                    { chest.sprite = keep; Release(light); return; }
                }
            }
            Sparks(rt, new Vector2(0f, rt.rect.height * 0.15f), tier.Sparks, tier.Tint, 320f, 1f, false, 22f);
            if (_coin.Length > 0) Coins(rt, tier.Sparks / 4);
            for (float t = 0f; t < 0.8f; t += Time.unscaledDeltaTime)
            {
                float u = t / 0.8f;
                light.rectTransform.localScale = new Vector3(Mathf.Lerp(0.5f, 1f, Ease.OutCubic(Mathf.Min(1f, u * 2f))), Mathf.Lerp(0.3f, 1.1f, Ease.OutCubic(u)), 1f);
                light.color = new Color(1f, 1f, 1f, u < 0.5f ? 1f : 1f - (u - 0.5f) * 1.4f);
                if (tier.Second && Mathf.Abs(t - 0.5f) < Time.unscaledDeltaTime * 0.6f)
                    Sparks(rt, new Vector2(0f, rt.rect.height * 0.4f), 10, Color.white, 220f, 0.8f, false, 20f);
                if (await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow()) break;
            }
            Release(light);
        }

        // ── 보상 칸 열림 · 금화 날아감(중) ────────────────────────
        // 칸이 0.1초 간격으로 톡 열리며 흰 섬광 · 0.6초에 금화가 위 막대 골드 칩으로 날아가 칩이 톡(돈 → 물건 방향)

        public void RewardCells(IReadOnlyList<RectTransform> cells, RectTransform goldCell, RectTransform goldChip)
        {
            if (cells == null || cells.Count == 0) return;
            RewardCellsAsync(cells, goldCell, goldChip, cells[0].GetCancellationTokenOnDestroy()).Forget();   // fire-and-forget: 1.4초 한 번
        }

        private async UniTaskVoid RewardCellsAsync(IReadOnlyList<RectTransform> cells, RectTransform goldCell, RectTransform goldChip, CancellationToken ct)
        {
            // 칸마다 제 크기(넷이면 줄인 칸)로 돌아오게 먼저 기억한다
            var sizes = new float[cells.Count];
            for (int i = 0; i < cells.Count; i++)
                if (cells[i] != null) { sizes[i] = cells[i].localScale.x; cells[i].localScale = Vector3.zero; }
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] == null || !cells[i].gameObject.activeInHierarchy) continue;
                CellPopAsync(cells[i], sizes[i], ct).Forget();   // fire-and-forget: 칸마다 0.25초
                if (await UniTask.Delay(System.TimeSpan.FromSeconds(0.1f), DelayType.UnscaledDeltaTime, cancellationToken: ct).SuppressCancellationThrow()) return;
            }
            if (goldCell == null || goldChip == null || !goldCell.gameObject.activeInHierarchy) return;
            if (await UniTask.Delay(System.TimeSpan.FromSeconds(0.3f), DelayType.UnscaledDeltaTime, cancellationToken: ct).SuppressCancellationThrow()) return;
            await FlyCoinsAsync(goldCell, goldChip, 5, ct);
        }

        private async UniTaskVoid CellPopAsync(RectTransform cell, float size, CancellationToken ct)
        {
            // 칸 뒤 후광 — 앞에 두면 숫자가 하얗게 덮였다(첫 캡처)
            var flash = Layer(cell, "FxCellFlash", null, Vector2.one * Mathf.Max(cell.rect.width, cell.rect.height) * 1.6f * size, true, false);
            Sparks(cell, Vector2.zero, 4, Color.white, 110f, 0.6f, false, 14f);
            var flashTask = Frames(flash, _flash, 20f, ct);
            for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
            {
                cell.localScale = Vector3.one * (size * Ease.OutBack(t / 0.25f));
                if (await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow()) { cell.localScale = Vector3.one * size; Release(flash); return; }
            }
            cell.localScale = Vector3.one * size;
            await flashTask;
            Release(flash);
        }

        private async UniTask FlyCoinsAsync(RectTransform from, RectTransform to, int count, CancellationToken ct)
        {
            var layer = (RectTransform)transform;
            var coins = new List<(Image img, Vector3 a, Vector3 c, float delay)>();
            Vector3 start = layer.InverseTransformPoint(from.position), end = layer.InverseTransformPoint(to.position);
            for (int i = 0; i < count; i++)
            {
                var img = Get(layer, "FxCoin", _coin.Length > 0 ? _coin[0] : null, new Vector2(40f, 40f), false);
                var ctrl = Vector3.Lerp(start, end, 0.5f) + new Vector3(Random.Range(-160f, 160f), Random.Range(40f, 140f), 0f);
                coins.Add((img, start + (Vector3)Random.insideUnitCircle * 20f, ctrl, i * 0.07f));
            }
            const float dur = 0.6f;
            float total = dur + count * 0.07f;
            for (float t = 0f; t < total; t += Time.unscaledDeltaTime)
            {
                foreach (var (img, a, c, delay) in coins)
                {
                    float u = Mathf.Clamp01((t - delay) / dur);
                    float e = u * u;   // 빨려 들어가듯
                    var p = Vector3.Lerp(Vector3.Lerp(a, c, e), Vector3.Lerp(c, end, e), e);
                    img.rectTransform.localPosition = p;
                    img.color = new Color(1f, 1f, 1f, u >= 1f ? 0f : 1f);
                    if (_coin.Length > 0) img.sprite = _coin[(int)(t * 16f) % _coin.Length];
                }
                if (await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow()) break;
            }
            foreach (var c in coins) Release(c.img);
            // 칩이 톡
            for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
            {
                to.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(t / 0.2f * Mathf.PI));
                if (await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow()) break;
            }
            to.localScale = Vector3.one;
        }

        private void Coins(RectTransform at, int n)
        {
            // 상자에서 튀어 오르는 금화 — 포물선으로 떨어지며 사라진다
            for (int i = 0; i < n; i++)
                Particle(at, new Vector2(0f, at.rect.height * 0.2f), _coin.Length > 0 ? _coin[i % _coin.Length] : null, Color.white,
                         new Vector2(Random.Range(-260f, 260f), Random.Range(380f, 640f)), 1.1f, 34f, 900f, false);
        }

        // ── 바탕 도구 ───────────────────────────────────────────

        public void Stop(RectTransform target)
        {
            if (target == null || !_loops.TryGetValue(target, out var cts)) return;
            cts.Cancel();
            cts.Dispose();
            _loops.Remove(target);
        }

        // 키트 닫힌 상자(rb_icon_chest_*_144) 바닥이 칸 아래에서 24/144 — 열림 그림으로 바꿀 때 이 점을 고정한다
        private const float KitChestBottom = 24f / 144f;

        /// <summary>상자를 닫힌 키트 그림 자리 · 크기로 되돌린다(다음 상자를 열기 전에).</summary>
        public void ResetChest(Image chest)
        {
            if (chest == null) return;
            Stop(chest.rectTransform);
            // 확대를 먼저 풀고 기준점을 되돌린다 — 반대로 하면 열 때(배율 1)와 다른 배율로 계산돼 상자가 열 때마다 위로 밀렸다
            chest.rectTransform.localScale = Vector3.one;
            SetPivotKeep(chest.rectTransform, new Vector2(0.5f, 0.5f));
            chest.rectTransform.localEulerAngles = Vector3.zero;
        }

        private static void SetPivotKeep(RectTransform rt, Vector2 pivot)
        {
            var d = pivot - rt.pivot;
            rt.pivot = pivot;
            rt.anchoredPosition += new Vector2(d.x * rt.rect.width * rt.localScale.x, d.y * rt.rect.height * rt.localScale.y);
        }

        private static ChestTier Tier(string grade) => Tiers.TryGetValue(grade ?? "", out var t) ? t : Tiers["gold"];

        /// <summary>대상 바로 앞(뒤면 behind=false) 형제로 그림 한 장 — 대상과 같은 자리 · 앵커.</summary>
        private Image Layer(RectTransform target, string name, Sprite sprite, Vector2 size, bool additive, bool front)
        {
            var parent = (RectTransform)target.parent;
            var img = Get(parent, name, sprite, size, additive);
            var rt = img.rectTransform;
            rt.anchorMin = target.anchorMin; rt.anchorMax = target.anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            // 대상 가운데(피벗과 무관하게)
            rt.position = target.TransformPoint(target.rect.center);
            rt.SetSiblingIndex(front ? target.GetSiblingIndex() + 1 : target.GetSiblingIndex());
            return img;
        }

        private Image Get(RectTransform parent, string name, Sprite sprite, Vector2 size, bool additive)
        {
            Image img = null;
            while (_pool.Count > 0 && img == null) img = _pool.Pop();
            if (img == null)
            {
                img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                img.raycastTarget = false;
            }
            img.name = name;
            img.gameObject.SetActive(true);
            img.rectTransform.SetParent(parent, false);
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            img.rectTransform.sizeDelta = size;
            img.rectTransform.localScale = Vector3.one;
            img.rectTransform.localEulerAngles = Vector3.zero;
            img.sprite = sprite;
            img.preserveAspect = true;
            img.material = additive ? _additive : null;
            img.color = Color.white;
            return img;
        }

        private void Release(Image img)
        {
            if (img == null) return;
            img.gameObject.SetActive(false);
            img.rectTransform.SetParent(transform, false);
            _pool.Push(img);
        }

        private void Clear(Image a, Image b, Image c) { Release(a); Release(b); Release(c); }

        /// <summary>프레임을 차례로 — 취소되면 true. holdLast 면 마지막 장을 남긴다.</summary>
        private static async UniTask<bool> Frames(Image img, Sprite[] frames, float fps, CancellationToken ct, bool holdLast = false)
        {
            if (frames == null || frames.Length == 0) { img.color = Color.clear; return false; }
            for (int i = 0; i < frames.Length; i++)
            {
                img.sprite = frames[i];
                if (await UniTask.Delay(System.TimeSpan.FromSeconds(1f / fps), DelayType.UnscaledDeltaTime, cancellationToken: ct).SuppressCancellationThrow())
                    return true;
            }
            if (!holdLast) img.color = Color.clear;
            return false;
        }

        /// <summary>반짝 알갱이 — 별 터짐 첫 장(작은 도트 십자)을 가산으로. 한 번에 40 개 이하(08_fx 1-1).</summary>
        private void Sparks(RectTransform at, Vector2 offset, int count, Color color, float speed, float life, bool up, float size)
        {
            var sprite = _star.Length > 0 ? _star[0] : null;
            count = Mathf.Min(count, 40);
            for (int i = 0; i < count; i++)
            {
                var v = up ? new Vector2(Random.Range(-0.3f, 0.3f), 1f) * speed * Random.Range(0.6f, 1.2f)
                           : Random.insideUnitCircle.normalized * speed * Random.Range(0.4f, 1f);
                Particle(at, offset, sprite, color, v, life * Random.Range(0.7f, 1.1f), size * Random.Range(0.7f, 1.3f), up ? 0f : -120f, true);
            }
        }

        private void Particle(RectTransform at, Vector2 offset, Sprite sprite, Color color, Vector2 velocity, float life, float size, float gravity, bool additive)
        {
            var img = Get((RectTransform)at.parent, "FxSpark", sprite, Vector2.one * size, additive);
            img.rectTransform.position = at.TransformPoint(at.rect.center);
            img.rectTransform.anchoredPosition += offset;
            img.rectTransform.SetSiblingIndex(at.GetSiblingIndex() + 1);
            img.color = color;
            ParticleAsync(img, velocity, life, gravity, img.GetCancellationTokenOnDestroy()).Forget();   // fire-and-forget: 수명이 다하면 풀로
        }

        private async UniTaskVoid ParticleAsync(Image img, Vector2 v, float life, float gravity, CancellationToken ct)
        {
            var rt = img.rectTransform;
            var c = img.color;
            for (float t = 0f; t < life; t += Time.unscaledDeltaTime)
            {
                float dt = Time.unscaledDeltaTime;
                v.y -= gravity * dt;
                v *= 1f - 1.2f * dt;   // 끌림
                rt.anchoredPosition += v * dt;
                float u = t / life;
                img.color = new Color(c.r, c.g, c.b, c.a * (u < 0.6f ? 1f : 1f - (u - 0.6f) / 0.4f));
                if (await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow()) return;
            }
            Release(img);
        }
    }
}
