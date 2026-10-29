using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Common;
using Game.Module.Common.UI;
using Game.User;
using GameFramework.Core.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 호스트 해금 연출 — PD 고른 시안 3-가 `_exchange/in/mock1029_host_unlock.png`(2026-10-29 「조각이 되는 순간 캐릭터 해금되었습니다 연출」).
    /// 박자: 어두워짐(약 0.25초) → 조각 셋이 가장자리에서 가운데로 모임(중) → 금빛 터짐 · 빛살 · 몸 등장(강, 힘주는 순간)
    ///       → 제목 · 등급 · 이름 차례로(중) → 빛살이 천천히 돌고 고리가 숨쉰다(약, 머묾) → 단추.
    /// 좌표는 시안 720 × 1280 판의 px 상자. 그림은 RbOutFx 에 꽂힌 `rbfx_unlock_*`(빛은 검정 바탕을 알파로 바꾼 것).
    /// </summary>
    public sealed class RbUnlockReveal : MonoBehaviour
    {
        private RbLobbyUI _shell;
        private RbOutFx _fx;
        private TMP_Text _like;
        private RectTransform _board;
        private string _key;
        private CancellationTokenSource _cts;

        public bool IsOpen => gameObject.activeSelf;

        public static RbUnlockReveal Create(RbLobbyUI shell, RbOutFx fx, TMP_Text like)
        {
            var go = new GameObject("RbUnlockReveal", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(shell.transform, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var r = go.AddComponent<RbUnlockReveal>();
            r._shell = shell;
            r._fx = fx;
            r._like = like;
            go.SetActive(false);
            return r;
        }

        public void Show(IPlayerDataService player, string hostKey)
        {
            var e = player.GetHost(hostKey);
            if (e == null) return;
            _key = hostKey;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            Build(e);
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            PlayAsync(_cts.Token).Forget();   // fire-and-forget: 연출 — 창이 닫히면 토큰으로 멈춘다
            GameSound.Cue("run.card");
        }

        private void OnDisable()
        {
            _cts?.Cancel();
            _cts = null;
        }

        // ── 세우기 ──────────────────────────────────────────

        private Image _dim, _rays, _ring, _gradePlate, _namePlate;
        private RectTransform _art, _title, _grade, _name, _buttons;
        private readonly RectTransform[] _shards = new RectTransform[3];
        private readonly Vector2[] _shardFrom = new Vector2[3];
        private static readonly MBoxV BoardBox = new(0, 0, 720, 1280);

        private void Build(HostEntry e)
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            _dim = GetComponent<Image>();
            _dim.color = new Color(0f, 0f, 0f, 0f);
            _dim.raycastTarget = true;

            var root = (RectTransform)transform;
            _board = New("Board", root, new Vector2(720f, 1280f));
            // 가로를 시안 판(720)에 맞춘다 — 세로까지 재면 긴 화면에서 판이 작아졌다(검수 2026-10-29: 단추가 시안의 0.85배)
            float scale = root.rect.width > 0f ? root.rect.width / 720f : 1f;
            _board.localScale = Vector3.one * scale;

            // 몸 뒤 빛 — 빛살(시안 60~680 × 120~900) · 고리
            _rays = Img("Rays", BoardBox, new MBoxV(110, 200, 610, 700), _fx != null ? _fx.UnlockRays : null);
            _ring = Img("Ring", BoardBox, new MBoxV(150, 260, 570, 680), _fx != null ? _fx.UnlockRing : null);
            if (_fx != null && _fx.Additive != null) { _rays.material = _fx.Additive; _ring.material = _fx.Additive; }

            // 몸 — 시안 (130,380)-(600,820), 발이 이름 판 위
            var artImg = Img("Art", BoardBox, new MBoxV(150, 390, 570, 820), null);
            _art = artImg.rectTransform;
            var show = artImg.gameObject.AddComponent<UnitShowcase>();
            show.Show(e.HostKey, Color.white);

            // 조각 셋 — 가장자리에서 몸으로 모인다
            Sprite[] shard = _fx != null ? _fx.UnlockShards : null;
            Vector2[] from = { new(-300f, 120f), new(300f, 60f), new(-220f, -260f) };
            for (int i = 0; i < 3; i++)
            {
                var s = Img($"Shard{i}", BoardBox, new MBoxV(338, 578, 382, 622), shard != null && i < shard.Length ? shard[i] : null);
                _shards[i] = s.rectTransform;
                _shardFrom[i] = from[i];
            }

            // 제목 · 등급 · 이름
            _title = Txt("Title", new MBoxV(60, 180, 660, 296), Localize.FromTable("ui.rb.unlock.title", "새 호스트 해금!"), 66,
                         new Color(1f, 0.9f, 0.28f), 0.16f, new Color32(60, 26, 0, 255)).rectTransform;
            // 시안 제목은 굵은 금빛 글자 + 짙은 테 — 테를 두껍게 하면 글자 안이 파여 흐려 보였다(검수 2026-10-29)
            _title.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            _gradePlate = Img("GradePlate", BoardBox, new MBoxV(262, 300, 458, 386), _fx != null ? _fx.UnlockGradePlate : null);
            _gradePlate.preserveAspect = true;
            _grade = Txt("Grade", new MBoxV(262, 306, 458, 380), HostGradeLabel.Of(e.Grade), 40, Color.white, 0.2f,
                         new Color32(40, 0, 70, 255)).rectTransform;
            _namePlate = Img("NamePlate", BoardBox, new MBoxV(204, 820, 516, 906), _fx != null ? _fx.UnlockNamePlate : null);
            _namePlate.preserveAspect = true;
            _name = Txt("Name", new MBoxV(230, 828, 490, 898), e.DisplayName, 40, Color.white, 0.2f, new Color32(30, 15, 0, 255)).rectTransform;

            // 단추 — 확인 · 성장으로
            _buttons = New("Buttons", _board, new Vector2(720f, 110f));
            _buttons.anchoredPosition = new Vector2(0f, 640f - 985f);
            var bb = new MBoxV(0, 930, 720, 1040);
            Button(_buttons, bb, new MBoxV(56, 936, 346, 1034), _fx != null ? _fx.UnlockButtonBlue : null,
                   Localize.FromTable("ui.rb.unlock.ok", "확인"), Close);
            Button(_buttons, bb, new MBoxV(374, 936, 664, 1034), _fx != null ? _fx.UnlockButtonGold : null,
                   Localize.FromTable("ui.rb.unlock.growth", "성장으로"), ToGrowth);

            if (CoreModule.TryGet<ILanguageService>(out var lang) && lang.IsReady) lang.ApplyFonts(transform);
        }

        // ── 연출 ──────────────────────────────────────────

        private async UniTaskVoid PlayAsync(CancellationToken ct)
        {
            SetAlpha(_rays, 0f); SetAlpha(_ring, 0f);
            Hide(_art); Hide(_title); Hide(_grade); Hide(_name); Hide(_buttons);
            _gradePlate.enabled = _namePlate.enabled = false;
            for (int i = 0; i < 3; i++) _shards[i].anchoredPosition = _shardFrom[i];

            // ① 어두워짐(약)
            // 로비 캔버스는 선형 색이라 0.92 도 뒤 화면이 비쳤다(검수 2026-10-29) — 시안처럼 거의 안 보이게 0.97
            await Tween(0.25f, t => _dim.color = new Color(0f, 0f, 0f, 0.97f * t), ct);
            // ② 조각이 모인다(중) — 돌면서 가운데로
            await Tween(0.55f, t =>
            {
                float k = Ease.InQuad(t);
                for (int i = 0; i < 3; i++)
                {
                    _shards[i].anchoredPosition = Vector2.Lerp(_shardFrom[i], Vector2.zero + new Vector2(0f, 40f), k);
                    _shards[i].localRotation = Quaternion.Euler(0f, 0f, 360f * t * (i % 2 == 0 ? 1f : -1f));
                    _shards[i].localScale = Vector3.one * (1f - 0.4f * k);
                }
            }, ct);
            for (int i = 0; i < 3; i++) _shards[i].gameObject.SetActive(false);
            // ③ 힘주는 순간(강) — 빛살 · 고리가 확 터지고 몸이 튀어나온다 · 조각이 사방으로 흩어져 둘레를 떠돈다(시안)
            Show(_art);
            Scatter();
            await Tween(0.35f, t =>
            {
                float k = Ease.OutBack(t);
                _art.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, k);
                _rays.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.15f, Ease.OutCubic(t));
                SetAlpha(_rays, Mathf.Lerp(0f, 0.8f, t * 2f));
                SetAlpha(_ring, Mathf.Lerp(0f, 1f, t * 2f));
            }, ct);
            // ④ 제목 · 등급 · 이름 차례로(중)
            Show(_title);
            await Tween(0.22f, t => _title.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, Ease.OutCubic(t)), ct);
            _gradePlate.enabled = true; Show(_grade);
            await Tween(0.18f, t => { _gradePlate.rectTransform.localScale = _grade.localScale = Vector3.one * Ease.OutBack(t); }, ct);
            _namePlate.enabled = true; Show(_name);
            await Tween(0.18f, t => { _namePlate.rectTransform.localScale = _name.localScale = Vector3.one * Ease.OutBack(t); }, ct);
            Show(_buttons);
            await Tween(0.2f, t => _buttons.anchoredPosition = new Vector2(0f, Mathf.Lerp(640f - 1040f, 640f - 985f, Ease.OutCubic(t))), ct);
            // ⑤ 머묾(약) — 빛살이 천천히 돌고 고리가 숨쉰다
            float time = 0f;
            while (!ct.IsCancellationRequested)
            {
                time += Time.unscaledDeltaTime;
                _rays.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -time * 8f);
                _rays.rectTransform.localScale = Vector3.one * (1.1f + 0.04f * Mathf.Sin(time * 2f));
                SetAlpha(_ring, 0.75f + 0.25f * Mathf.Sin(time * 2.6f));
                DriftFloaters(time);
                if (await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow()) return;
            }
        }

        // 떠도는 조각 — 터진 뒤 몸 둘레 고리 자리에 흩어져 천천히 떠돌며 돈다(시안 3-가 둘레 결정 조각)
        private const int FloaterCount = 12;
        private readonly RectTransform[] _floaters = new RectTransform[FloaterCount];
        private readonly Vector2[] _floatHome = new Vector2[FloaterCount];

        private void Scatter()
        {
            var shard = _fx != null ? _fx.UnlockShards : null;
            if (shard == null || shard.Length == 0) return;
            for (int i = 0; i < FloaterCount; i++)
            {
                float a = (i / (float)FloaterCount) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
                float r = Random.Range(220f, 300f);
                var home = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r * 1.15f + 40f);
                var img = Img($"Floater{i}", BoardBox, new MBoxV(340, 600, 380, 640), shard[i % shard.Length]);
                img.rectTransform.SetSiblingIndex(_art.GetSiblingIndex());   // 몸 뒤
                float sz = Random.Range(26f, 46f);
                img.rectTransform.sizeDelta = new Vector2(sz, sz);
                _floaters[i] = img.rectTransform;
                _floatHome[i] = home;
            }
        }

        private void DriftFloaters(float time)
        {
            for (int i = 0; i < FloaterCount; i++)
            {
                var f = _floaters[i];
                if (f == null) continue;
                float ph = i * 1.37f;
                f.anchoredPosition = _floatHome[i] + new Vector2(Mathf.Sin(time * 0.9f + ph) * 10f, Mathf.Cos(time * 1.1f + ph) * 14f);
                f.localRotation = Quaternion.Euler(0f, 0f, time * (i % 2 == 0 ? 40f : -30f) + ph * 50f);
            }
        }

        private static async UniTask Tween(float seconds, System.Action<float> step, CancellationToken ct)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                step(Mathf.Clamp01(t / seconds));
                if (await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow()) return;
            }
            step(1f);
        }

        private void Close()
        {
            gameObject.SetActive(false);
            _shell.AfterUnlockReveal(null);
        }

        private void ToGrowth()
        {
            gameObject.SetActive(false);
            _shell.AfterUnlockReveal(_key);
        }

        // ── 도구 ──────────────────────────────────────────

        private readonly struct MBoxV
        {
            public readonly float X0, Y0, X1, Y1;
            public MBoxV(float x0, float y0, float x1, float y1) { X0 = x0; Y0 = y0; X1 = x1; Y1 = y1; }
        }

        private static RectTransform New(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.sizeDelta = size;
            return rt;
        }

        private static RectTransform At(string name, RectTransform parent, MBoxV pb, MBoxV b)
        {
            var rt = New(name, parent, new Vector2(b.X1 - b.X0, b.Y1 - b.Y0));
            rt.anchoredPosition = new Vector2((b.X0 + b.X1 - pb.X0 - pb.X1) * 0.5f, (pb.Y0 + pb.Y1 - b.Y0 - b.Y1) * 0.5f);
            return rt;
        }

        private Image Img(string name, MBoxV pb, MBoxV b, Sprite sprite) => Img(name, _board, pb, b, sprite);

        private static Image Img(string name, RectTransform parent, MBoxV pb, MBoxV b, Sprite sprite)
        {
            var img = At(name, parent, pb, b).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.enabled = sprite != null || name == "Art";
            img.raycastTarget = false;
            return img;
        }

        private TextMeshProUGUI Txt(string name, MBoxV b, string s, float size, Color c, float outline, Color32 outlineColor)
            => Txt(name, _board, BoardBox, b, s, size, c, outline, outlineColor);

        private TextMeshProUGUI Txt(string name, RectTransform parent, MBoxV pb, MBoxV b, string s, float size, Color c,
                                    float outline, Color32 outlineColor)
        {
            var t = At(name, parent, pb, b).gameObject.AddComponent<TextMeshProUGUI>();
            if (_like != null) { t.font = _like.font; t.fontSharedMaterial = _like.fontSharedMaterial; }
            t.text = s;
            t.fontSize = size;
            t.enableAutoSizing = true;
            t.fontSizeMax = size;
            t.fontSizeMin = 14f;
            t.color = c;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            if (outline > 0f) { t.outlineWidth = outline; t.outlineColor = outlineColor; }
            return t;
        }

        private void Button(RectTransform parent, MBoxV pb, MBoxV b, Sprite sprite, string label, UnityEngine.Events.UnityAction act)
        {
            var img = Img("Button", parent, pb, b, sprite);
            img.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            img.enabled = true;
            if (sprite == null) img.color = new Color(0.2f, 0.4f, 0.8f);
            img.raycastTarget = true;
            var btn = img.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(act);
            Txt("Label", img.rectTransform, b, b, label, 34, Color.white, 0.22f, new Color32(10, 20, 40, 255));
        }

        private static void SetAlpha(Image i, float a) { if (i != null) { var c = i.color; c.a = a; i.color = c; } }
        private static void Hide(RectTransform r) => r.gameObject.SetActive(false);
        private static void Show(RectTransform r) => r.gameObject.SetActive(true);
    }
}
