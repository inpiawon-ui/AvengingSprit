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
    ///   · **비추기** — 유령일 때 탈 수 있는 몸. 방이 어두워지고 그 몸 뒤로 빛줄기가 내려온다.
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
        private static readonly Vector2 PivotBottom = new(0.5f, 0.02f);
        private static readonly Vector2 PivotCenter = new(0.5f, 0.5f);
        private static readonly Vector2 PivotPillar = new(0.5f, 96f / 1024f);
        private static readonly Vector2 PivotFlame = new(0.5f, 64f / 1024f);
        private static readonly Vector2 PivotAura = new(0.5f, 96f / 512f);

        private static readonly Vector2 RippleSize = new(250f, 167f);
        private static readonly Vector2 WispSize = new(120f, 320f);
        private static readonly Vector2 PillarSize = new(156f, 416f);
        private static readonly Vector2 FlameSize = new(104f, 277f);
        private static readonly Vector2 AuraSize = new(250f, 167f);

        /// <summary>유닛 자리(몸 가운데)에서 발밑까지.</summary>
        private const float SoulFootDrop = 34f;
        /// <summary>유닛 자리에서 가슴께까지.</summary>
        private const float SoulChestLift = 14f;

        /// <summary>발밑에 퍼지는 물결. 캐릭터 뒤(바닥)에 깐다.</summary>
        private void PlaySoulRipple(Vector2 unitPos, float delay = 0f)
            => PlaySoulFx("soulripple", behind: true, unitPos + Vector2.down * SoulFootDrop, RippleSize, PivotCenter,
                          life: 0.68f, frameSeconds: 0.17f, delay: delay, fadeOut: 0.2f,
                          scaleFrom: 0.75f, scaleTo: 1.1f);

        /// <summary>몸 둘레에 피어오르는 기운. 가운데가 비어 있어 캐릭터 앞에 그려도 안 가린다.</summary>
        private void PlaySoulAura(Vector2 unitPos, float delay = 0f)
            => PlaySoulFx("soulaura", behind: false, unitPos + Vector2.down * SoulFootDrop, AuraSize, PivotAura,
                          life: 0.72f, frameSeconds: 0.18f, delay: delay, fadeOut: 0.22f);

        // ── 빠져나오기 ──────────────────────────────────────────

        private const float SoulRiseSeconds = 0.55f;
        private const float SoulRiseHeight = 58f;
        /// <summary>혼줄은 유령이 다 솟은 뒤에도 잠깐 팽팽하게 남았다가 끊긴다.</summary>
        private const float SoulCordSeconds = 0.85f;
        private const float SoulSnapSeconds = 0.26f;
        private const float SoulCordThickness = 60f;
        private const float SoulCordFrameSeconds = 0.09f;
        private const float SoulTrailInterval = 0.09f;

        private float _soulRise;
        private float _soulCordLeft;      // 팽팽한 시간 + 끊기는 시간
        private float _soulTrail;
        private Vector2 _soulFrom;
        private bool _soulSnapped;
        private RectTransform _soulCordRect;
        private Image _soulCordImage;

        /// <summary>쓰러진 내 몸. 유령이 다른 몸에 들거나 되살릴 때까지 바닥에 남는다.</summary>
        private Unit _corpse;
        private Vector2 _fallPos;
        private bool _hasFall;

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

            PlaySoulRipple(pos);
            // 혼불은 몸 **뒤**에서 머리 위로 솟는다 — 쓰러지는 동작을 가리지 않는다.
            PlaySoulFx("soulwisp", behind: true, pos + Vector2.down * 6f, WispSize, PivotBottom,
                       life: 0.8f, frameSeconds: 0.2f, fadeOut: 0.2f);

            _soulFrom = pos + Vector2.up * SoulChestLift;
            _soulRise = SoulRiseSeconds;
            _soulCordLeft = SoulCordSeconds + SoulSnapSeconds;
            _soulTrail = 0f;
            _soulSnapped = false;
            if (_ghost != null) _ghost.transform.localScale = Vector3.one * 0.35f;
        }

        private void TickSoulOut(float dt)
        {
            if (_soulCordLeft <= 0f) return;
            _soulCordLeft -= dt;

            // 몸에 들어갔거나 들어가는 중이면 줄을 거둔다 — 끈이 새 몸에 걸려 있으면 안 된다.
            if (_host != null || IsChanneling || _ghost == null || _soulCordLeft <= 0f)
            {
                EndSoulOut();
                return;
            }

            if (_soulRise > 0f)
            {
                float before = 1f - Mathf.Clamp01(_soulRise / SoulRiseSeconds);
                _soulRise -= dt;
                float t = 1f - Mathf.Clamp01(_soulRise / SoulRiseSeconds);
                // 쑥 뽑혀 나온다 — 처음에 빠르고 끝에서 멎는다. 이동분만 더해 조작과 겹치게 한다.
                float eased(float k) => 1f - (1f - k) * (1f - k);
                var p = _ghost.Position + Vector2.up * (SoulRiseHeight * (eased(t) - eased(before)));
                p.y = Mathf.Min(p.y, -24f);
                _ghost.Position = p;
                _ghost.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, 1f, eased(t));

                // 솟는 길에 잔상을 남긴다(원작 대시 잔상과 같은 것).
                _soulTrail -= dt;
                if (_soulTrail <= 0f)
                {
                    _soulTrail = SoulTrailInterval;
                    var img = RentAfterimage();
                    var sprite = _ghost.BodySprite;
                    if (img != null && sprite != null)
                        img.Play(sprite, _ghost.Position, _ghost.GetComponent<RectTransform>().sizeDelta
                                 * _ghost.transform.localScale.x, _ghost.BodyFlipX, 0.35f, 0.22f);
                }
                if (_soulRise <= 0f) _ghost.transform.localScale = Vector3.one;
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
            var to = _ghost.Position + Vector2.down * 14f;   // 유령의 꼬리
            var d = to - from;
            float len = d.magnitude;
            bool show = len > 8f;
            if (_soulCordRect.gameObject.activeSelf != show) _soulCordRect.gameObject.SetActive(show);
            if (!show) return;

            bool snap = _soulCordLeft <= SoulSnapSeconds;
            if (snap && !_soulSnapped)
            {
                _soulSnapped = true;
                if (_pfx != null) _pfx.Sparkle((from + to) * 0.5f, ParticleElement.Ice, 1.1f);
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
        //   0.00  유령이 원작 그림대로 줄어들며 쓰러진 몸으로 날아간다 (0.55초)
        //   0.55  몸에 닿았다 — 발밑에 물결, 몸 **뒤**로 빛기둥이 솟는다
        //   1.05  몸이 일어선다(여기서 실제로 몸을 입는다). 몸 둘레에 기운
        //   1.60  빛기둥이 실 몇 가닥으로 풀려 올라간다

        /// <summary>유령이 몸에 닿기까지. 채널 전체 가운데 이만큼만 유령이 움직인다.</summary>
        private const float ReviveFlySeconds = 0.55f;
        /// <summary>닿은 뒤 빛기둥이 솟는 동안. 이게 끝나면 몸이 선다.</summary>
        private const float ReviveBuildSeconds = 0.5f;
        private const float RevivePillarBurnSeconds = 1.05f;
        private const float RevivePillarFadeSeconds = 0.45f;

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

            EndSoulOut();
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
            PlaySoulRipple(to, delay: ReviveFlySeconds);
            // 빛기둥 — 솟고(1장) · 타오르고(2 · 3장 번갈아) · 풀린다(4장). 전부 몸 뒤다.
            PlaySoulFx("soulpillar", behind: true, feet, PillarSize, PivotPillar,
                       life: 0.2f, frameSeconds: 0.2f, delay: ReviveFlySeconds,
                       fadeIn: 0.08f, fadeOut: 0f, firstFrame: 1, frameCount: 1);
            PlaySoulFx("soulpillar", behind: true, feet, PillarSize, PivotPillar,
                       life: RevivePillarBurnSeconds, frameSeconds: 0.1f, loop: true,
                       delay: ReviveFlySeconds + 0.2f, fadeOut: 0f, firstFrame: 2, frameCount: 2);
            PlaySoulFx("soulpillar", behind: true, feet, PillarSize, PivotPillar,
                       life: RevivePillarFadeSeconds, frameSeconds: RevivePillarFadeSeconds,
                       delay: ReviveFlySeconds + 0.2f + RevivePillarBurnSeconds,
                       fadeOut: RevivePillarFadeSeconds, scaleFrom: 1f, scaleTo: 1.08f,
                       firstFrame: 4, frameCount: 1);
        }

        /// <summary>몸이 선 직후. 몸 둘레에 기운을 올리고 알린다.</summary>
        private void FinishRevive(string hostKey)
        {
            _reviveHpPercent = 0;
            _channelGhostSpan = 1f;
            if (_host != null)
            {
                PlaySoulAura(_host.Position);
                if (_pfx != null) _pfx.Embers(_host.Position, ParticleElement.Ice, 8, 60f);
            }
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
                    PlaySoulRipple(pos);
                    PlaySoulAura(pos);
                }
            }

            _hasFall = false;
            EndSoulOut();
            StopHomeFlame();
        }

        private void TickHomeFlame()
        {
            // 긴급 호스트를 기다리는 동안만 — 「여기로 돌아간다」.
            bool waiting = _emergencyWait > 0f && _host == null && !IsChanneling
                           && _ghost != null && _ghostHp > _config.EmergencyGhostCost;
            if (!waiting)
            {
                if (!IsChanneling) StopHomeFlame();
                return;
            }

            var at = ReviveSpot + Vector2.down * (SoulFootDrop - 14f);
            if (_homeFlame == null || !_homeFlame.IsPlaying)
                _homeFlame = PlaySoulFx("soulflame", behind: false, at, FlameSize, PivotFlame,
                                        life: 60f, frameSeconds: 0.13f, loop: true, fadeIn: 0.25f);
            else _homeFlame.Rect.anchoredPosition = at;
        }

        private void StopHomeFlame()
        {
            if (_homeFlame != null) _homeFlame.FadeAway(0.15f);
            _homeFlame = null;
        }

        // ── 비추기 ──────────────────────────────────────────────
        //
        // 빛줄기는 캐릭터 **뒤**에 깐다 — 앞에 덮으면 그 몸의 빙의 동작이 뿌옇게 가린다.
        // 어둠은 **구멍 뚫린 한 장**(fx_spotdark)과 그 둘레를 메우는 네 장으로 앞에 덮는다.
        // 유닛 층 위에 통째로 덮으면 비출 몸도 같이 어두워진다 — 구멍으로 그 몸만 남긴다.

        private const float SpotWidth = 230f;
        private const float SpotHeight = 345f;
        /// <summary>그림에서 바닥 타원의 중심 높이(아래에서, 0~1).</summary>
        private const float SpotPivotY = 170f / 1536f;
        private const float SpotFill = 3000f;

        /// <summary>처음에는 진하게 어두워졌다가(「저기다」) 곧 옅어진다 — 싸움을 가리면 안 된다.</summary>
        // ⚠ 0.58 / 0.26 으로는 **안 보였다** — 바닥이 원래 어두운 남색이라 그 정도는 묻힌다.
        private const float SpotDarkPeak = 0.82f;
        private const float SpotDarkRest = 0.5f;
        private const float SpotDarkPeakSeconds = 0.9f;
        private const float SpotFadeSpeed = 4f;
        // 꽉 찬 빛은 파란 판자처럼 보였다 — 뒤 바닥이 비쳐야 빛줄기다.
        private const float SpotBeamAlpha = 0.6f;

        private static readonly Color SpotDarkColor = new(0.02f, 0.03f, 0.09f, 1f);

        private RectTransform _spotRoot;
        private RectTransform _spotBeamRect;
        private Image _spotBeam, _spotHole;
        private readonly Image[] _spotFill = new Image[4];
        private float _spotShown;      // 0~1
        private float _spotAge;
        private Unit _spotUnit;

        private void TickSpotlight(float dt)
        {
            var target = _host == null && !IsChanneling && !_awaitingBuff && _repossessLock <= 0f
                ? _possessTarget : null;

            if (target != null)
            {
                if (_spotRoot == null && !MakeSpotlight()) return;
                // 다른 몸으로 옮겨 가면 다시 한 번 어두워진다 — 「이번엔 저기」.
                if (target != _spotUnit) { _spotUnit = target; _spotAge = 0f; }
                _spotAge += dt;
                var feet = target.Position + Vector2.down * SoulFootDrop;
                _spotRoot.anchoredPosition = feet;
                _spotBeamRect.anchoredPosition = feet;
                _spotShown = Mathf.MoveTowards(_spotShown, 1f, SpotFadeSpeed * dt);
            }
            else
            {
                if (_spotRoot == null) return;
                _spotUnit = null;
                // 빙의가 시작되면 **바로** 걷는다 — 들어가는 동작 위에 빛이 남아 있으면 안 된다.
                _spotShown = IsChanneling ? 0f : Mathf.MoveTowards(_spotShown, 0f, SpotFadeSpeed * dt);
            }

            bool on = _spotShown > 0.001f;
            if (_spotRoot.gameObject.activeSelf != on) _spotRoot.gameObject.SetActive(on);
            if (_spotBeamRect.gameObject.activeSelf != on) _spotBeamRect.gameObject.SetActive(on);
            if (!on) return;

            float settle = Mathf.Clamp01((_spotAge - SpotDarkPeakSeconds) / 0.6f);
            float dark = Mathf.Lerp(SpotDarkPeak, SpotDarkRest, settle) * _spotShown;
            var dc = new Color(SpotDarkColor.r, SpotDarkColor.g, SpotDarkColor.b, dark);
            _spotHole.color = dc;
            for (int i = 0; i < _spotFill.Length; i++) _spotFill[i].color = dc;

            // 빛줄기는 숨쉬듯 — 가만히 있으면 붙여 놓은 그림이다.
            float breathe = 0.86f + 0.14f * Mathf.Sin(Time.time * 3.2f);
            _spotBeam.color = new Color(1f, 1f, 1f, SpotBeamAlpha * breathe * _spotShown);
        }

        private bool MakeSpotlight()
        {
            var beam = GetSprite("fx_spotbeam");
            var hole = GetSprite("fx_spotdark");
            if (beam == null || hole == null || _shotLayer == null || _fieldLayer == null) return false;

            // 빛줄기 — 캐릭터 뒤
            var beamGo = new GameObject("SoulSpotBeam", typeof(RectTransform), typeof(Image));
            _spotBeamRect = (RectTransform)beamGo.transform;
            _spotBeamRect.SetParent(_fieldLayer, false);
            _spotBeamRect.anchorMin = _spotBeamRect.anchorMax = new Vector2(0f, 1f);
            _spotBeamRect.pivot = new Vector2(0.5f, SpotPivotY);
            _spotBeamRect.sizeDelta = new Vector2(SpotWidth, SpotHeight);
            _spotBeam = beamGo.GetComponent<Image>();
            _spotBeam.sprite = beam;
            _spotBeam.raycastTarget = false;
            beamGo.SetActive(false);

            // 어둠 — 캐릭터 앞
            var go = new GameObject("SoulSpotDark", typeof(RectTransform));
            _spotRoot = (RectTransform)go.transform;
            _spotRoot.SetParent(_shotLayer, false);
            _spotRoot.anchorMin = _spotRoot.anchorMax = new Vector2(0f, 1f);
            _spotRoot.pivot = new Vector2(0.5f, 0.5f);
            _spotRoot.sizeDelta = Vector2.zero;
            _spotRoot.SetAsFirstSibling();   // 탄 · 터짐 그림은 어둠 위에 그대로 보인다

            float below = SpotHeight * SpotPivotY;
            float above = SpotHeight - below;
            float half = SpotWidth * 0.5f;

            _spotHole = SpotImage("Dark", hole, new Vector2(0.5f, SpotPivotY),
                                  Vector2.zero, new Vector2(SpotWidth, SpotHeight));
            // 구멍 둘레 — 왼 · 오른 · 위 · 아래
            _spotFill[0] = SpotImage("DarkL", null, new Vector2(1f, SpotPivotY),
                                     new Vector2(-half, 0f), new Vector2(SpotFill, SpotHeight));
            _spotFill[1] = SpotImage("DarkR", null, new Vector2(0f, SpotPivotY),
                                     new Vector2(half, 0f), new Vector2(SpotFill, SpotHeight));
            _spotFill[2] = SpotImage("DarkT", null, new Vector2(0.5f, 0f),
                                     new Vector2(0f, above), new Vector2(SpotFill * 2f + SpotWidth, SpotFill));
            _spotFill[3] = SpotImage("DarkB", null, new Vector2(0.5f, 1f),
                                     new Vector2(0f, -below), new Vector2(SpotFill * 2f + SpotWidth, SpotFill));
            go.SetActive(false);
            return true;
        }

        private Image SpotImage(string name, Sprite sprite, Vector2 pivot, Vector2 at, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_spotRoot, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = pivot;
            rt.anchoredPosition = at;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        // ── 공통 ────────────────────────────────────────────────

        private void TickSoulFx(float dt)
        {
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
            _spotUnit = null;
            _spotShown = 0f;
            if (_spotRoot != null) _spotRoot.gameObject.SetActive(false);
            if (_spotBeamRect != null) _spotBeamRect.gameObject.SetActive(false);
        }
    }
}
