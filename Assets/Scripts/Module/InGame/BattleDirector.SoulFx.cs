using System.Collections.Generic;
using Game.Module.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 유령 연출 (기획 2026-10-02) — 세 장면에 이펙트를 얹는다.
    ///
    ///   · **빠져나오기** — 몸을 잃거나 버릴 때. 발밑에 물결이 퍼지고 등 뒤로 혼불이 솟으며,
    ///     유령이 혼줄을 달고 천천히 뽑혀 나온다. 줄은 잠깐 팽팽했다가 끊긴다.
    ///   · **되살리기** — 옮겨 탈 몸이 없을 때(긴급 호스트). 쓰러진 내 몸 위에 혼불이 피어
    ///     「돌아갈 곳」을 알리고, 유령이 그리로 빨려 들어간 **뒤에** 빛기둥이 솟고 몸이 일어선다.
    ///   · **비추기** — 유령일 때 지금 들어갈 몸 하나. 그 몸 뒤로 빛줄기가 내려온다(방은 그대로 둔다).
    ///
    /// ⚠ **캐릭터 동작은 원작 그림 그대로다**(IP). 쓰러짐은 원작 die 두 장, 빨려 들어가기는
    ///   원작 축소 세 장을 그대로 쓴다. 여기서 더하는 것은 그 위의 이펙트와 자리 · 시간뿐이다.
    ///
    /// ⚠ **큰 이펙트는 캐릭터 뒤에 깐다.** 처음에는 전부 캐릭터 위에 그렸는데 쓰러지는 동작과
    ///   빙의 동작을 통째로 가렸다(반려 2026-10-02). 뒤(`_fieldLayer`)에는 물결 · 혼불 · 빛기둥 · 빛줄기,
    ///   앞(`_shotLayer`)에는 캐릭터 자리를 비운 것(혼줄 · 몸 둘레 기운 · 어둠)만 둔다.
    ///
    /// ⚠ **서두르지 않는다.** 0.3초짜리 연출은 싸움 한복판에서 아무도 못 본다(같은 날 반려).
    ///   빠져나오기 1.1초 · 되살리기 2초에 걸쳐 차례로 보여 준다.
    ///
    /// ⚠ **값은 안 건드린다.** 몸을 잃는 값 · 긴급 호스트 값과 시작 체력은 `GameConfig` 그대로다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        // ── 이펙트 한 장 ────────────────────────────────────────
        //
        // `Impact` 를 안 쓴다 — 그건 정사각형 한 장을 탄 층 위에 짧게 터뜨리는 것이다.
        // 여기 것은 길쭉하고(기둥 · 줄), 층을 고르고, 늦게 시작하고, 커지며 옅어져야 한다.

        private sealed class SoulFx
        {
            public RectTransform Rect;
            public Image Image;
            public Sprite[] Frames;
            public float Delay, Age, Life, FrameSeconds, FadeIn, FadeOut, ScaleFrom, ScaleTo, Alpha;
            public bool Loop;
            /// <summary>따라다닐 몸. 몸에 붙은 기운은 몸이 걸어가면 같이 간다 — 제자리에 남으면 빈 바닥이 탄다.</summary>
            public Unit Follow;
            public Vector2 FollowOffset;
            public bool IsPlaying => Life > 0f;

            public void Stop()
            {
                Life = 0f;
                if (Rect != null) Rect.gameObject.SetActive(false);
            }

            /// <summary>남은 시간과 상관없이 지금부터 옅어지며 끝난다.</summary>
            public void FadeAway(float seconds)
            {
                if (!IsPlaying) return;
                FadeOut = Mathf.Max(0.01f, seconds);
                Delay = 0f;
                Life = Mathf.Min(Life, Age + FadeOut);
            }

            public void Tick(float dt)
            {
                if (Life <= 0f) return;
                if (Delay > 0f)
                {
                    Delay -= dt;
                    if (Delay > 0f) return;
                    Rect.gameObject.SetActive(true);
                }
                Age += dt;
                if (Age >= Life) { Stop(); return; }
                if (Follow != null) Rect.anchoredPosition = Follow.Position + FollowOffset;

                int n = Frames.Length;
                int i = Mathf.FloorToInt(Age / FrameSeconds);
                i = Loop ? i % n : Mathf.Min(i, n - 1);
                if (Image.sprite != Frames[i]) Image.sprite = Frames[i];

                float a = Alpha;
                if (FadeIn > 0f) a *= Mathf.Clamp01(Age / FadeIn);
                if (FadeOut > 0f) a *= Mathf.Clamp01((Life - Age) / FadeOut);
                Image.color = new Color(1f, 1f, 1f, a);

                if (ScaleFrom != ScaleTo)
                {
                    float k = Age / Life;
                    // 빨리 퍼지고 천천히 멎는다 — 등속으로 커지면 고무풍선이다
                    float s = Mathf.Lerp(ScaleFrom, ScaleTo, 1f - (1f - k) * (1f - k));
                    Rect.localScale = new Vector3(s, s, 1f);
                }
            }
        }

        private readonly List<SoulFx> _soulFx = new();
        private const int MaxSoulFx = 24;

        /// <summary>
        /// 이펙트 한 장을 튼다. <paramref name="behind"/> 이면 캐릭터 뒤(바닥 층), 아니면 앞(탄 층).
        /// <paramref name="pivot"/> 은 그림 안에서 <paramref name="at"/> 에 놓일 점이다.
        /// </summary>
        private SoulFx PlaySoulFx(string name, bool behind, Vector2 at, Vector2 size, Vector2 pivot,
                                  float life, float frameSeconds, bool loop = false,
                                  float delay = 0f, float fadeIn = 0f, float fadeOut = 0.15f,
                                  float scaleFrom = 1f, float scaleTo = 1f, float alpha = 1f,
                                  int firstFrame = 1, int frameCount = 0)
        {
            var all = FxFrames(name);
            if (all == null) return null;
            int first = Mathf.Clamp(firstFrame - 1, 0, all.Length - 1);
            int count = frameCount <= 0 ? all.Length - first : Mathf.Min(frameCount, all.Length - first);

            var layer = behind ? _fieldLayer : _shotLayer;
            if (layer == null) return null;

            SoulFx fx = null;
            for (int i = 0; i < _soulFx.Count; i++)
                if (!_soulFx[i].IsPlaying) { fx = _soulFx[i]; break; }
            if (fx == null)
            {
                if (_soulFx.Count >= MaxSoulFx) return null;
                var go = new GameObject("SoulFx", typeof(RectTransform), typeof(Image));
                fx = new SoulFx { Rect = (RectTransform)go.transform, Image = go.GetComponent<Image>() };
                fx.Image.raycastTarget = false;
                fx.Rect.anchorMin = fx.Rect.anchorMax = new Vector2(0f, 1f);
                _soulFx.Add(fx);
            }

            if (fx.Frames == null || fx.Frames.Length != count) fx.Frames = new Sprite[count];
            for (int i = 0; i < count; i++) fx.Frames[i] = all[first + i];

            fx.Rect.SetParent(layer, false);
            fx.Rect.SetAsLastSibling();
            fx.Rect.pivot = pivot;
            fx.Rect.sizeDelta = size;
            fx.Rect.anchoredPosition = at;
            fx.Rect.localEulerAngles = Vector3.zero;
            fx.Rect.localScale = new Vector3(scaleFrom, scaleFrom, 1f);
            fx.Image.sprite = fx.Frames[0];
            fx.Image.color = new Color(1f, 1f, 1f, fadeIn > 0f ? 0f : alpha);
            fx.Follow = null;
            fx.Delay = delay;
            fx.Age = 0f;
            fx.Life = Mathf.Max(0.02f, life);
            fx.FrameSeconds = Mathf.Max(0.02f, frameSeconds);
            fx.Loop = loop;
            fx.FadeIn = fadeIn;
            fx.FadeOut = fadeOut;
            fx.ScaleFrom = scaleFrom;
            fx.ScaleTo = scaleTo;
            fx.Alpha = alpha;
            fx.Rect.gameObject.SetActive(delay <= 0f);
            return fx;
        }

        // 그림 안의 기준점(아래에서, 0~1) — 발주서의 「기준점」 과 같은 값이다.
        private static readonly Vector2 PivotCenter = new(0.5f, 0.5f);
        // 자르는 쪽(`soulfx_fit.py`)이 밑동을 칸 아래 20 px 에 맞춰 놓는다. 거기에 바닥 고리의 반 높이를 더한다.
        private static readonly Vector2 PivotWisp = new(0.5f, 20f / 512f);
        private static readonly Vector2 PivotPillar = new(0.5f, 34f / 512f);
        private static readonly Vector2 PivotFlame = new(0.5f, 28f / 512f);
        private static readonly Vector2 PivotAura = new(0.5f, 26f / 512f);

        // ⚠ **절제한다.** 캐릭터 키가 90 px 남짓이다. 처음에는 기둥을 416 px 로 세웠다가
        //   「크다고 다 멋있는 게 아니다」로 반려됐다(2026-10-02). 캐릭터 곁에 딱 맞게 붙인다.
        //   칸 그림은 192 × 256 (전부 같다) — 여기 값은 그 칸을 화면에 그리는 크기다.
        //
        //   아래 값은 코덱스 디자인 검수(`_exchange/review_soulfx.md`)를 거쳐 한 번 더 줄인 것이다 —
        //   물결 0.8배 · 기둥은 캐릭터 키의 1.5배 · 몸 둘레 기운은 키의 0.9배.
        private static readonly Vector2 RippleSize = new(132f, 176f);    // 물결 지름 ≈ 112
        private static readonly Vector2 WispSize = new(117f, 156f);      // 가닥 높이 ≈ 100
        private static readonly Vector2 PillarSize = new(101f, 135f);    // 기둥 높이 ≈ 135
        private static readonly Vector2 FlameSize = new(105f, 140f);
        private static readonly Vector2 AuraSize = new(100f, 98f);       // 키의 0.9배

        /// <summary>빛기둥 · 기운의 짙기. 흰 심이 꽉 차면 그 앞에 선 몸까지 하얗게 날린다.</summary>
        // 0.68 은 방을 어둡게 하던 때의 값이다. 평소 밝기의 바닥에서는 묻혀 안 보였다(2차 검수) — 올린다.
        private const float PillarAlpha = 0.88f;
        private const float AuraAlpha = 0.7f;

        /// <summary>유닛 자리(몸 가운데)에서 발밑까지.</summary>
        private const float SoulFootDrop = 34f;
        /// <summary>유닛 자리에서 가슴께까지.</summary>
        private const float SoulChestLift = 14f;

        private const float SoulFrameSeconds = 0.075f;   // 8 장 = 0.6초

        /// <summary>발밑에 퍼지는 물결. 캐릭터 뒤(바닥)에 깐다.</summary>
        private void PlaySoulRipple(Vector2 unitPos, float delay = 0f, float scale = 1f, float alpha = 1f)
            => PlaySoulFx("soulripple", behind: true, unitPos + Vector2.down * SoulFootDrop,
                          RippleSize * scale, PivotCenter,
                          life: SoulFrameSeconds * 8f, frameSeconds: SoulFrameSeconds, delay: delay,
                          fadeOut: 0.12f, alpha: alpha);

        /// <summary>
        /// 평소 빙의의 물결은 더 작다(지름 ≈ 캐릭터 키의 0.55배). 들어가는 순간에는 원작의
        /// 줄어드는 유령과 빼앗기는 몸이 주인공이다 — 그 자리에서 크게 번지면 그 동작을 가린다(검수).
        /// </summary>
        private const float PossessRippleScale = 0.5f;
        private const float PossessRippleAlpha = 0.8f;

        /// <summary>
        /// 몸 둘레에 피어오르는 기운. **몸 뒤에 깐다** — 가운데가 빈 그림이라 앞에 그려도 될 줄 알았는데,
        /// 캐릭터 크기에 맞게 줄이면 그 빈자리가 몸보다 좁아져 몸통을 덮었다(검수 반려).
        /// </summary>
        private void PlaySoulAura(Unit unit, float scale = 1f, float alpha = AuraAlpha)
        {
            if (unit == null) return;
            var fx = PlaySoulFx("soulaura", behind: true, unit.Position + Vector2.down * SoulFootDrop,
                                AuraSize * scale, PivotAura, life: SoulFrameSeconds * 8f,
                                frameSeconds: SoulFrameSeconds, fadeOut: 0.12f, alpha: alpha);
            if (fx == null) return;
            fx.Follow = unit;
            fx.FollowOffset = Vector2.down * SoulFootDrop;
        }

        // ── 빠져나오기 ──────────────────────────────────────────

        private const float SoulRiseSeconds = 0.6f;
        /// <summary>유령이 몸 위로 떠오르는 높이. 혼줄이 길게 늘어져 보이려면 이만큼은 떠야 한다.</summary>
        private const float SoulRiseHeight = 96f;
        /// <summary>혼줄은 유령이 다 솟은 뒤에도 잠깐 팽팽하게 남았다가 끊긴다.</summary>
        private const float SoulCordSeconds = 1.0f;
        private const float SoulSnapSeconds = 0.3f;
        private const float SoulCordThickness = 18f;
        /// <summary>줄이 이보다 길어지면 끊긴다. 유령이 솟는 높이(96)보다 조금 길다.</summary>
        private const float SoulCordMaxLength = 130f;
        /// <summary>줄이 일렁이는 빠르기. 빠르면 굽이가 장마다 튀어 줄이 아니라 깜빡이로 보인다.</summary>
        private const float SoulCordFrameSeconds = 0.18f;
        private const float SoulTrailInterval = 0.1f;

        private float _soulRise;
        private float _soulCordLeft;      // 팽팽한 시간 + 끊기는 시간
        private float _soulTrail;
        private Vector2 _soulFrom;        // 혼줄이 걸린 가슴
        private Vector2 _soulRiseFrom, _soulRiseTo;
        private bool _soulSnapped;
        private RectTransform _soulCordRect;
        private Image _soulCordImage;

        /// <summary>
        /// **매여 있다** — 옮겨 탈 몸이 없어 곧 이 몸으로 되돌아간다.
        /// 줄이 끊기지 않고, 유령은 몸 바로 위에 떠서 기다린다. 다른 데로 갔다가 돌아오면
        /// 「어디 갔다 오는 거냐」가 된다(반려 2026-10-02) — 나온 자리에서 그대로 다시 들어간다.
        /// </summary>
        private bool _soulTether;

        /// <summary>쓰러진 내 몸. 유령이 다른 몸에 들거나 되살릴 때까지 바닥에 남는다.</summary>
        private Unit _corpse;
        private Vector2 _fallPos;
        private bool _hasFall;

        /// <summary>지금 몸을 잃으면 되살리기(긴급 호스트)로 이어지는가.</summary>
        private bool WillReviveHere()
            => _enemies.Count > 0 && !HasFutureHost()
               && _ghostHp > _config.EmergencyGhostCost && PickPlayerHost() != null;

        /// <summary>
        /// 몸에서 유령이 나온다. <paramref name="body"/> 는 방금 쓰러뜨린 몸(없어도 된다).
        /// 유령을 켜고 자리를 잡은 **뒤에** 부른다.
        /// </summary>
        private void BeginSoulOut(Unit body, Vector2 pos)
        {
            ReleaseCorpse();
            _fallPos = pos;
            _hasFall = true;
            if (body != null && body.IsDying)
            {
                body.SetCorpseHold(true);
                _corpse = body;
            }
            _soulTether = _corpse != null && WillReviveHere();

            PlaySoulRipple(pos);
            // 혼불은 몸 **뒤**에서 머리 위로 솟는다 — 쓰러지는 동작을 가리지 않는다.
            PlaySoulFx("soulwisp", behind: true, pos + Vector2.up * 4f, WispSize, PivotWisp,
                       life: SoulFrameSeconds * 8f, frameSeconds: SoulFrameSeconds, fadeOut: 0.12f);

            _soulFrom = pos + Vector2.up * SoulChestLift;
            _soulRiseFrom = pos;
            _soulRiseTo = pos + Vector2.up * SoulRiseHeight;
            _soulRiseTo.y = Mathf.Min(_soulRiseTo.y, -30f);     // 방 윗변을 넘지 않는다
            _soulRise = SoulRiseSeconds;
            _soulCordLeft = SoulCordSeconds + SoulSnapSeconds;
            _soulTrail = 0f;
            _soulSnapped = false;
            if (_ghost != null) _ghost.transform.localScale = Vector3.one * 0.35f;
        }

        private static float SoulEase(float k) => 1f - (1f - k) * (1f - k);

        private void TickSoulOut(float dt)
        {
            if (_soulCordLeft <= 0f && !_soulTether) return;
            if (!_soulTether) _soulCordLeft -= dt;

            // 다른 몸에 들어갔으면 줄을 거둔다 — 끈이 새 몸에 걸려 있으면 안 된다.
            // 매여 있을 때는 되돌아가는 길(채널)에도 줄이 남는다.
            bool done = _host != null || _ghost == null
                        || (!_soulTether && (IsChanneling || _soulCordLeft <= 0f));
            if (done) { EndSoulOut(); return; }

            if (_soulRise > 0f)
            {
                float before = SoulEase(1f - Mathf.Clamp01(_soulRise / SoulRiseSeconds));
                _soulRise -= dt;
                float now = SoulEase(1f - Mathf.Clamp01(_soulRise / SoulRiseSeconds));
                // 쑥 뽑혀 나온다 — 처음에 빠르고 끝에서 멎는다.
                // 매여 있으면 제자리로 못 박고, 아니면 이동분만 더해 조작과 겹치게 한다.
                //
                // ⚠ 그냥 자리를 더해 올리면 **기둥 속으로 밀려 들어가 갇힌다**(실제로 기둥 두 개
                //   사이에 끼어 못 나왔다). 걸어서 가는 것과 같은 길 — 막힌 것은 타고 미끄러진다.
                _ghost.Position = _soulTether
                    ? Vector2.Lerp(_soulRiseFrom, _soulRiseTo, now)
                    : ClampedInField(_ghost, SlideMove(_ghost, _ghost.Position,
                                                       (_soulRiseTo - _soulRiseFrom) * (now - before)));
                _ghost.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, 1f, now);

                // 솟는 길에 잔상을 남긴다(원작 대시 잔상과 같은 것).
                _soulTrail -= dt;
                if (_soulTrail <= 0f)
                {
                    _soulTrail = SoulTrailInterval;
                    var img = RentAfterimage();
                    var sprite = _ghost.BodySprite;
                    if (img != null && sprite != null)
                        img.Play(sprite, _ghost.Position, _ghost.GetComponent<RectTransform>().sizeDelta
                                 * _ghost.transform.localScale.x, _ghost.BodyFlipX, 0.35f, 0.24f);
                }
                if (_soulRise <= 0f) _ghost.transform.localScale = Vector3.one;
            }
            else if (_soulTether && !IsChanneling)
            {
                // 매여 있는 동안은 몸 위에 떠 있는다 — 살짝 까딱이며.
                _ghost.Position = _soulRiseTo + Vector2.up * (Mathf.Sin(Time.time * 5f) * 2.5f);
                // 되살릴 조건이 깨졌으면(잡몹이 다 죽었다 등) 줄을 놓는다.
                if (!WillReviveHere()) { _soulTether = false; _soulCordLeft = SoulSnapSeconds; }
            }

            DrawSoulCord();
        }

        private void DrawSoulCord()
        {
            var frames = FxFrames("soulcord");
            if (frames == null || frames.Length < 4 || _shotLayer == null) return;

            if (_soulCordRect == null)
            {
                var go = new GameObject("SoulCord", typeof(RectTransform), typeof(Image));
                _soulCordRect = (RectTransform)go.transform;
                _soulCordRect.SetParent(_shotLayer, false);
                _soulCordRect.anchorMin = _soulCordRect.anchorMax = new Vector2(0f, 1f);
                _soulCordRect.pivot = new Vector2(0.5f, 0.5f);
                _soulCordImage = go.GetComponent<Image>();
                _soulCordImage.raycastTarget = false;
            }

            var from = _soulFrom;
            var to = _ghost.Position + Vector2.down * 12f * _ghost.transform.localScale.x;   // 유령의 꼬리
            var d = to - from;
            float len = d.magnitude;
            bool show = len > 10f;
            if (_soulCordRect.gameObject.activeSelf != show) _soulCordRect.gameObject.SetActive(show);
            if (!show) return;

            // 유령이 멀리 가 버리면 줄이 방을 가로지르는 선이 된다 — 그 전에 끊는다.
            if (!_soulTether && len > SoulCordMaxLength && _soulCordLeft > SoulSnapSeconds)
                _soulCordLeft = SoulSnapSeconds;

            bool snap = !_soulTether && _soulCordLeft <= SoulSnapSeconds;
            if (snap && !_soulSnapped)
            {
                _soulSnapped = true;
                if (_pfx != null) _pfx.Sparkle((from + to) * 0.5f, ParticleElement.Ice, 0.8f);
            }

            // 그림은 가로다(왼쪽 = 몸, 오른쪽 = 유령). 길이만큼 늘이고 각도만큼 돌린다.
            _soulCordRect.anchoredPosition = (from + to) * 0.5f;
            _soulCordRect.sizeDelta = new Vector2(len, SoulCordThickness);
            _soulCordRect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            _soulCordImage.sprite = snap
                ? frames[3]
                : frames[Mathf.FloorToInt(Time.time / SoulCordFrameSeconds) % 3];
            _soulCordImage.color = new Color(1f, 1f, 1f,
                snap ? Mathf.Clamp01(_soulCordLeft / SoulSnapSeconds) : 1f);
        }

        private void EndSoulOut()
        {
            _soulRise = 0f;
            _soulCordLeft = 0f;
            _soulTether = false;
            if (_soulCordRect != null) _soulCordRect.gameObject.SetActive(false);
            if (_ghost != null && _host == null && !IsChanneling)
                _ghost.transform.localScale = Vector3.one;
        }

        /// <summary>붙들어 둔 몸을 놓는다 — 그때부터 평소처럼 옅어지며 사라진다.</summary>
        private void ReleaseCorpse()
        {
            if (_corpse != null) _corpse.SetCorpseHold(false);
            _corpse = null;
        }

        // ── 되살리기 ────────────────────────────────────────────
        //
        // 순서가 연출이다. 한꺼번에 터뜨리면 무슨 일인지 안 읽힌다.
        //   (기다리는 동안 — 유령은 혼줄에 매여 쓰러진 몸 바로 위에 떠 있다)
        //   0.00  유령이 원작 그림대로 줄어들며 **줄을 따라** 몸으로 내려간다 (0.5초)
        //   0.50  몸에 닿았다 — 발밑에 물결, 몸 **뒤**로 빛기둥이 솟는다
        //   1.00  몸이 일어선다(여기서 실제로 몸을 입는다). 몸 둘레에 기운
        //   1.55  빛기둥이 실 몇 가닥으로 풀려 올라간다

        /// <summary>유령이 몸에 닿기까지. 채널 전체 가운데 이만큼만 유령이 움직인다.</summary>
        private const float ReviveFlySeconds = 0.5f;
        /// <summary>닿은 뒤 빛기둥이 솟는 동안. 이게 끝나면 몸이 선다.</summary>
        private const float ReviveBuildSeconds = 0.3f;
        // 기둥 전체 노출 ≈ 0.57초(솟기 0.18 · 타오르기 0.25 · 풀리기 0.14) — 검수 기준 0.55.
        private const float PillarFrameSeconds = 0.06f;
        /// <summary>기둥이 가장 밝게 서 있는 시간. 길면 내려온 유령 · 솟는 기둥 · 일어서는 몸이 한 덩어리가 된다.</summary>
        private const float PillarBurnSeconds = 0.25f;

        private SoulFx _homeFlame;
        /// <summary>되살리는 중이면 그 몸의 시작 체력(%). 0 이면 평소 빙의다.</summary>
        private int _reviveHpPercent;

        /// <summary>
        /// 채널 가운데 유령이 움직이는 몫(0~1). 평소 빙의는 1 — 끝까지 날아가 들어간다.
        /// 되살리기는 앞쪽만 쓰고, 남은 시간은 유령이 몸 안에 든 채 빛기둥이 솟는다.
        /// </summary>
        private float _channelGhostSpan = 1f;

        /// <summary>되살아날 자리 — 이 방에서 몸이 쓰러진 곳. 없으면 유령이 선 곳.</summary>
        private Vector2 ReviveSpot => _hasFall ? _fallPos : _ghost.Position;

        /// <summary>
        /// 긴급 호스트를 **쓰러진 몸 자리에서** 일으킨다. 유령이 원작 그림대로 줄어들며
        /// 그 자리로 빨려 들어가고, 빛기둥이 솟은 뒤 `TickPossessChannel` 이 몸을 세운다.
        /// </summary>
        private void BeginRevive(Game.Character.HostEntry entry)
        {
            var from = _ghost.Position;
            var to = ReviveSpot;

            // 매여 있지 않았으면(떠돌다 돌아오는 길) 줄은 이미 끊겼다 — 거둔다.
            if (!_soulTether) EndSoulOut();
            _soulRise = 0f;
            _ghost.transform.localScale = Vector3.one;

            _reviveHpPercent = _config.EmergencyHostHpPercent;
            _channelBody = null;
            _channelFrom = from;
            _channelTo = to;
            _channelEntry = entry;
            _channelKey = entry.HostKey;
            _channelName = entry.DisplayName;
            _channelTotal = _channel = ReviveFlySeconds + ReviveBuildSeconds;
            _channelGhostSpan = ReviveFlySeconds / _channelTotal;
            if ((to - from).sqrMagnitude > 1f) _ghost.SetFacing(to - from);

            // 「돌아갈 곳」 혼불은 유령이 닿을 때까지 남는다.
            if (_homeFlame != null) _homeFlame.FadeAway(ReviveFlySeconds);
            _homeFlame = null;

            var feet = to + Vector2.down * SoulFootDrop;
            float t0 = ReviveFlySeconds;
            PlaySoulRipple(to, delay: t0);
            // 빛기둥 — 솟고(1~3장) · 타오르고(4~6장 되풀이) · 풀린다(7 · 8장). 전부 몸 뒤다.
            PlaySoulFx("soulpillar", behind: true, feet, PillarSize, PivotPillar,
                       life: PillarFrameSeconds * 3f, frameSeconds: PillarFrameSeconds, delay: t0,
                       fadeIn: 0.05f, fadeOut: 0f, alpha: PillarAlpha, firstFrame: 1, frameCount: 3);
            PlaySoulFx("soulpillar", behind: true, feet, PillarSize, PivotPillar,
                       life: PillarBurnSeconds, frameSeconds: PillarBurnSeconds / 3f, loop: true,
                       delay: t0 + PillarFrameSeconds * 3f, fadeOut: 0f, alpha: PillarAlpha,
                       firstFrame: 4, frameCount: 3);
            PlaySoulFx("soulpillar", behind: true, feet, PillarSize, PivotPillar,
                       life: 0.14f, frameSeconds: 0.07f,
                       delay: t0 + PillarFrameSeconds * 3f + PillarBurnSeconds,
                       fadeOut: 0.07f, alpha: PillarAlpha, firstFrame: 7, frameCount: 2);
        }

        /// <summary>몸이 선 직후. 몸 둘레에 기운을 올리고 알린다.</summary>
        private void FinishRevive(string hostKey)
        {
            _reviveHpPercent = 0;
            _channelGhostSpan = 1f;
            PlaySoulAura(_host);
            _bus.Publish(new EmergencyHostEvent
            {
                HostKey = hostKey, GhostCost = _config.EmergencyGhostCost,
            });
        }

        /// <summary>몸을 입는 순간(<see cref="EnterHost"/> 첫머리)에 지난 흔적을 거둔다.</summary>
        private void SoulFxOnEnterHost(Vector2 pos)
        {
            if (_reviveHpPercent > 0)
            {
                if (_corpse != null)
                {
                    // 되살린 몸이 그 자리에 선다 — 누운 몸이 같이 보이면 둘이 된다.
                    _dying.Remove(_corpse);
                    Destroy(_corpse.gameObject);
                    _corpse = null;
                }
            }
            else
            {
                ReleaseCorpse();
                // 평소 빙의 — 영혼이 깃든 순간. 발밑 물결과 몸 둘레 기운.
                if (_running)
                {
                    PlaySoulRipple(pos, scale: PossessRippleScale, alpha: PossessRippleAlpha);
                    _soulAuraPending = true;   // 몸은 아직 안 섰다 — 선 뒤에 그 몸에 붙인다
                }
            }

            _hasFall = false;
            EndSoulOut();
            StopHomeFlame();
        }

        private void TickHomeFlame()
        {
            // 긴급 호스트를 기다리는 동안만 — 「여기로 돌아간다」.
            // 매여 있을 때는 혼줄이 이미 그 말을 하고 있다. 떠돌다 돌아올 때만 띄운다.
            bool waiting = _emergencyWait > 0f && _host == null && !IsChanneling && !_soulTether
                           && _ghost != null && _ghostHp > _config.EmergencyGhostCost;
            if (!waiting)
            {
                if (!IsChanneling) StopHomeFlame();
                return;
            }

            var at = ReviveSpot + Vector2.down * (SoulFootDrop - 14f);
            if (_homeFlame == null || !_homeFlame.IsPlaying)
                _homeFlame = PlaySoulFx("soulflame", behind: false, at, FlameSize, PivotFlame,
                                        life: 60f, frameSeconds: 0.1f, loop: true, fadeIn: 0.25f);
            else _homeFlame.Rect.anchoredPosition = at;
        }

        private void StopHomeFlame()
        {
            if (_homeFlame != null) _homeFlame.FadeAway(0.15f);
            _homeFlame = null;
        }

        // ── 비추기 ──────────────────────────────────────────────
        //
        // 유령일 때 **지금 버튼을 누르면 들어갈 몸 하나**에만 빛줄기를 내린다.
        // 그 밖의 탈 수 있는 몸은 머리 위 유령 표식(기존 빙의 표식)이 알린다.
        //
        // ⚠ 방을 어둡게 하지 않는다. 처음에는 시안대로 방 전체를 어둡게 하고 못 타는 적까지
        //   가라앉혔는데 **게임이 멈춘 것처럼** 느껴져 반려됐다(2026-10-02). 고르는 동안에도
        //   싸움은 돌아가고 있다 — 평소 화면 그대로 두고 대상에만 가볍게 표시한다.
        // ⚠ 빛줄기는 캐릭터 **뒤**다 — 앞에 덮으면 그 몸의 빙의 동작이 뿌옇게 가린다.

        private static readonly Vector2 SpotSize = new(96f, 144f);     // 바닥 타원 ≈ 캐릭터 폭의 1.35배
        /// <summary>그림에서 바닥 타원의 중심 높이(아래에서, 0~1).</summary>
        private const float SpotPivotY = 170f / 1536f;
        private const float SpotFadeSpeed = 5f;
        // 꽉 찬 빛은 파란 판자처럼 보였다 — 뒤 바닥이 비쳐야 빛줄기다.
        private const float SpotBeamAlpha = 0.62f;

        private Image _spotBeam;
        private float _spotShown;      // 0~1

        private void TickSpotlight(float dt)
        {
            var target = _host == null && !IsChanneling && !_awaitingBuff && _running
                ? _possessTarget : null;

            RefreshTakeMarks(target);
            if (target != null && _spotBeam == null && !MakeSpotlight()) return;
            if (_spotBeam == null) return;

            // 빙의가 시작되면 **바로** 걷는다 — 들어가는 동작 뒤에 빛이 남아 있으면 안 된다.
            _spotShown = target != null ? Mathf.MoveTowards(_spotShown, 1f, SpotFadeSpeed * dt)
                : IsChanneling ? 0f : Mathf.MoveTowards(_spotShown, 0f, SpotFadeSpeed * dt);

            bool visible = _spotShown > 0.001f;
            if (_spotBeam.gameObject.activeSelf != visible) _spotBeam.gameObject.SetActive(visible);
            if (!visible) return;

            // 대상이 사라져 옅어지는 동안에는 마지막 자리에 둔다.
            if (target != null)
                _spotBeam.rectTransform.anchoredPosition = target.Position + Vector2.down * SoulFootDrop;

            // 숨쉬듯 — 가만히 있으면 붙여 놓은 그림이다.
            float breathe = 0.86f + 0.14f * Mathf.Sin(Time.time * 3.2f);
            _spotBeam.color = new Color(1f, 1f, 1f, SpotBeamAlpha * breathe * _spotShown);
        }

        private Sprite _takeSprite;

        /// <summary>
        /// 유령일 때 **탈 수 있는 몸** 머리 위에 유령 아이콘을 띄운다. 지금 들어갈 몸(빛줄기)은 뺀다.
        /// 예전의 과녁 표식(파랑 · 금색 · 자물쇠 · 금지)을 이 한 장이 대신한다.
        /// </summary>
        private void RefreshTakeMarks(Unit picked)
        {
            if (_takeSprite == null) _takeSprite = GetSprite("rps_take");
            bool ghost = _host == null && !IsChanneling && !_awaitingBuff && _running;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null) continue;
                e.SetTakeMark(ghost && e != picked && e.IsPossessable ? _takeSprite : null);
            }
        }

        private bool MakeSpotlight()
        {
            var beam = GetSprite("fx_spotbeam");
            if (beam == null || _fieldLayer == null) return false;

            var go = new GameObject("SoulSpotBeam", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_fieldLayer, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, SpotPivotY);
            rt.sizeDelta = SpotSize;
            _spotBeam = go.GetComponent<Image>();
            _spotBeam.sprite = beam;
            _spotBeam.raycastTarget = false;
            go.SetActive(false);
            return true;
        }

        // ── 공통 ────────────────────────────────────────────────

        private bool _soulAuraPending;

        private void TickSoulFx(float dt)
        {
            if (_soulAuraPending)
            {
                _soulAuraPending = false;
                PlaySoulAura(_host, scale: 0.75f, alpha: 0.5f);   // 평소 빙의는 옅고 작게
            }
            TickSoulOut(dt);
            TickHomeFlame();
            TickSpotlight(dt);
            for (int i = 0; i < _soulFx.Count; i++) _soulFx[i].Tick(dt);
        }

        /// <summary>방을 넘어갈 때. 지난 방의 자리 · 몸 · 표시를 들고 가지 않는다.</summary>
        private void ClearSoulFx()
        {
            ReleaseCorpse();
            _hasFall = false;
            _reviveHpPercent = 0;
            _channelGhostSpan = 1f;
            EndSoulOut();
            _homeFlame = null;
            for (int i = 0; i < _soulFx.Count; i++) _soulFx[i].Stop();
            _spotShown = 0f;
            if (_spotBeam != null) _spotBeam.gameObject.SetActive(false);
        }

        /// <summary>화면(HUD)이 인게임 아틀라스의 그림을 빌려 쓸 때.</summary>
        public Sprite UiSprite(string name) => GetSprite(name);

        // ── 나가기 ──────────────────────────────────────────────

        /// <summary>
        /// 몸을 버리고 나갈 수 있는가 — **옮겨 탈 몸이 방에 있을 때만.**
        /// 탈 몸이 없는데 나가면 유령으로 떠서 시계만 돈다. 그건 선택지가 아니라 함정이다.
        /// </summary>
        private bool CanLeaveHost()
        {
            if (ReapAllowsLeave()) return true;   // 시험판 — 사신의 수확: 거둘 혼이 있으면 나갈 수 있다
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e != null && e.HasPossessCondition) return true;
            }
            return false;
        }
    }
}
