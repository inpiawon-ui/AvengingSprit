using Game.Module.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 유령 연출 (기획 2026-10-02) — 세 장면에 이펙트를 얹는다.
    ///
    ///   · **빠져나오기** — 몸을 잃거나 버릴 때. 가슴에서 혼불이 터지고, 유령이 혼줄을 달고 솟는다.
    ///   · **되살리기** — 옮겨 탈 몸이 없을 때(긴급 호스트). 쓰러진 내 몸 자리에 혼불이 피어
    ///     「돌아갈 곳」을 알리고, 유령이 그리로 빨려 들어가 빛기둥 속에서 다시 일어선다.
    ///   · **비추기** — 유령일 때 탈 수 있는 몸. 방이 잠깐 어두워지고 그 몸에 빛줄기가 내려온다.
    ///
    /// ⚠ **캐릭터 동작은 원작 그림 그대로다**(IP). 쓰러짐은 원작 die 두 장, 빨려 들어가기는
    ///   원작 축소 세 장을 그대로 쓴다. 여기서 더하는 것은 그 위의 이펙트와 자리 · 색 · 시간뿐이다.
    ///
    /// ⚠ **값은 안 건드린다.** 몸을 잃는 값 · 긴급 호스트 값과 시작 체력은 `GameConfig` 그대로다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        // ── 빠져나오기 ──────────────────────────────────────────

        private const float SoulOutSize = 170f;
        /// <summary>혼불이 터지는 자리 — 발이 아니라 가슴께.</summary>
        private const float SoulOutLift = 26f;
        private const float SoulRiseSeconds = 0.32f;
        private const float SoulRiseSpeed = 150f;
        /// <summary>혼줄은 유령이 다 솟은 뒤에도 잠깐 남았다 끊긴다.</summary>
        private const float SoulCordSeconds = 0.62f;
        private const float SoulCordFadeSeconds = 0.22f;
        private const float SoulCordThickness = 34f;

        private float _soulRise;
        private float _soulCordLeft;
        private Vector2 _soulFrom;
        private Impact _soulCord;

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

            var chest = pos + Vector2.up * SoulOutLift;
            SpawnFx("soulout", chest + Vector2.up * (SoulOutSize * 0.2f), SoulOutSize);
            if (_pfx != null)
            {
                _pfx.Sparkle(chest, ParticleElement.Ice, 1.2f);
                _pfx.Embers(chest, ParticleElement.Ice, 5, 40f);
            }

            _soulFrom = chest;
            _soulRise = SoulRiseSeconds;
            _soulCordLeft = SoulCordSeconds;
            StopSoulCord();
        }

        private void TickSoulOut(float dt)
        {
            if (_soulCordLeft <= 0f) return;
            _soulCordLeft -= dt;

            // 몸에 들어갔거나 들어가는 중이면 줄을 거둔다 — 끈이 새 몸에 걸려 있으면 안 된다.
            if (_host != null || IsChanneling || _ghost == null || _soulCordLeft <= 0f)
            {
                _soulRise = 0f;
                _soulCordLeft = 0f;
                StopSoulCord();
                if (_ghost != null && _host == null && !IsChanneling)
                    _ghost.transform.localScale = Vector3.one;
                return;
            }

            if (_soulRise > 0f)
            {
                _soulRise -= dt;
                float t = 1f - Mathf.Clamp01(_soulRise / SoulRiseSeconds);
                // 쑥 뽑혀 나온다 — 작게 시작해 솟으며 제 크기가 된다.
                var p = _ghost.Position + Vector2.up * (SoulRiseSpeed * (1f - t) * 2f * dt);
                p.y = Mathf.Min(p.y, -24f);
                _ghost.Position = p;
                _ghost.transform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, 1f - (1f - t) * (1f - t));
                if (_soulRise <= 0f) _ghost.transform.localScale = Vector3.one;
            }

            var frames = FxFrames("soulcord");
            if (frames == null) return;
            if (_soulCord == null) _soulCord = FreeImpact(SoulCordThickness);
            if (_soulCord == null) return;

            _soulCord.PlayBeam(_soulFrom, _ghost.Position + Vector2.down * 6f, frames, SoulCordThickness);
            if (!_soulCord.IsActive) { _soulCord = null; return; }
            _soulCord.SetTint(new Color(1f, 1f, 1f, Mathf.Clamp01(_soulCordLeft / SoulCordFadeSeconds)));
        }

        private void StopSoulCord()
        {
            if (_soulCord != null && _soulCord.IsActive) _soulCord.Stop();
            _soulCord = null;
        }

        /// <summary>붙들어 둔 몸을 놓는다 — 그때부터 평소처럼 옅어지며 사라진다.</summary>
        private void ReleaseCorpse()
        {
            if (_corpse != null) _corpse.SetCorpseHold(false);
            _corpse = null;
        }

        // ── 되살리기 ────────────────────────────────────────────

        private const float HomeFlameSize = 96f;
        private const float RevivePillarSize = 300f;
        private const float ReviveChannelSeconds = 0.36f;
        private const float RevivePillarFrameSeconds = 0.1f;

        private Impact _homeFlame;
        /// <summary>되살리는 중이면 그 몸의 시작 체력(%). 0 이면 평소 빙의다.</summary>
        private int _reviveHpPercent;

        /// <summary>되살아날 자리 — 이 방에서 몸이 쓰러진 곳. 없으면 유령이 선 곳.</summary>
        private Vector2 ReviveSpot => _hasFall ? _fallPos : _ghost.Position;

        /// <summary>
        /// 긴급 호스트를 **쓰러진 몸 자리에서** 일으킨다. 유령이 원작 그림대로 줄어들며
        /// 그 자리로 빨려 들어가고, 끝나면 `TickPossessChannel` 이 몸을 세운다.
        /// </summary>
        private void BeginRevive(Game.Character.HostEntry entry)
        {
            var from = _ghost.Position;
            var to = ReviveSpot;

            _reviveHpPercent = _config.EmergencyHostHpPercent;
            _channelBody = null;
            _channelFrom = from;
            _channelTo = to;
            _channelEntry = entry;
            _channelKey = entry.HostKey;
            _channelName = entry.DisplayName;
            _channelTotal = _channel = ReviveChannelSeconds;
            if ((to - from).sqrMagnitude > 1f) _ghost.SetFacing(to - from);

            StopHomeFlame();
            // 빛기둥은 발밑에서 솟는다 — 그림 아래 가운데가 발밑이다.
            var im = PlayFx("revive", to + Vector2.up * (RevivePillarSize * 0.5f - 30f),
                            RevivePillarSize, loop: false);
            if (im != null) im.SetFrameSeconds(RevivePillarFrameSeconds);
        }

        /// <summary>몸이 선 직후. 붙들어 둔 옛 몸을 치우고 불티를 올린다.</summary>
        private void FinishRevive(string hostKey)
        {
            _reviveHpPercent = 0;
            if (_pfx != null && _host != null)
            {
                _pfx.Sparkle(_host.Position + Vector2.up * SoulOutLift, ParticleElement.Ice, 1.4f);
                _pfx.Embers(_host.Position, ParticleElement.Ice, 8, 60f);
            }
            _bus.Publish(new EmergencyHostEvent
            {
                HostKey = hostKey, GhostCost = _config.EmergencyGhostCost,
            });
        }

        /// <summary>몸을 입는 순간(<see cref="EnterHost"/> 첫머리)에 지난 흔적을 거둔다.</summary>
        private void SoulFxOnEnterHost()
        {
            if (_reviveHpPercent > 0 && _corpse != null)
            {
                // 되살린 몸이 그 자리에 선다 — 누운 몸이 같이 보이면 둘이 된다.
                _dying.Remove(_corpse);
                Destroy(_corpse.gameObject);
                _corpse = null;
            }
            else ReleaseCorpse();

            _hasFall = false;
            _soulCordLeft = 0f;
            _soulRise = 0f;
            StopSoulCord();
            StopHomeFlame();
        }

        private void TickHomeFlame()
        {
            // 긴급 호스트를 기다리는 동안만 — 「여기로 돌아간다」.
            bool waiting = _emergencyWait > 0f && _host == null && !IsChanneling
                           && _ghost != null && _ghostHp > _config.EmergencyGhostCost;
            if (!waiting) { StopHomeFlame(); return; }

            var at = ReviveSpot + Vector2.up * (HomeFlameSize * 0.5f - 22f);
            if (_homeFlame == null || !_homeFlame.IsActive)
            {
                _homeFlame = TakeLoopFx("soulflame", at, HomeFlameSize);
                if (_homeFlame != null) _homeFlame.SetFrameSeconds(0.11f);
            }
            else _homeFlame.MoveTo(at);
        }

        private void StopHomeFlame()
        {
            if (_homeFlame != null && _homeFlame.IsActive) _homeFlame.Stop();
            _homeFlame = null;
        }

        // ── 비추기 ──────────────────────────────────────────────
        //
        // 어둠은 **구멍 뚫린 한 장**(fx_spotdark)과 그 둘레를 메우는 네 장으로 만든다.
        // 유닛 층 위에 통째로 덮으면 비출 몸도 같이 어두워진다 — 구멍으로 그 몸만 남긴다.

        private const float SpotWidth = 176f;
        private const float SpotHeight = 352f;
        /// <summary>그림에서 바닥 타원의 중심 높이(아래에서, 0~1).</summary>
        private const float SpotPivotY = 56f / 512f;
        /// <summary>유닛 자리(몸 가운데)에서 발밑까지.</summary>
        private const float SpotFootDrop = 34f;
        private const float SpotFill = 3000f;

        /// <summary>처음에는 진하게 어두워졌다가(「저기다」) 곧 옅어진다 — 싸움을 가리면 안 된다.</summary>
        // ⚠ 0.58 / 0.26 으로는 **안 보였다** — 바닥이 원래 어두운 남색이라 그 정도는 묻힌다.
        private const float SpotDarkPeak = 0.82f;
        private const float SpotDarkRest = 0.5f;
        private const float SpotDarkPeakSeconds = 0.9f;
        private const float SpotFadeSpeed = 6f;
        private const float SpotBeamAlpha = 0.85f;

        private static readonly Color SpotDarkColor = new(0.02f, 0.03f, 0.09f, 1f);

        private RectTransform _spotRoot;
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
                _spotRoot.anchoredPosition = target.Position + Vector2.down * SpotFootDrop;
                _spotShown = Mathf.MoveTowards(_spotShown, 1f, SpotFadeSpeed * dt);
            }
            else
            {
                if (_spotRoot == null) return;
                _spotUnit = null;
                _spotShown = Mathf.MoveTowards(_spotShown, 0f, SpotFadeSpeed * dt);
            }

            bool on = _spotShown > 0.001f;
            if (_spotRoot.gameObject.activeSelf != on) _spotRoot.gameObject.SetActive(on);
            if (!on) return;

            float settle = Mathf.Clamp01((_spotAge - SpotDarkPeakSeconds) / 0.5f);
            float dark = Mathf.Lerp(SpotDarkPeak, SpotDarkRest, settle) * _spotShown;
            var dc = new Color(SpotDarkColor.r, SpotDarkColor.g, SpotDarkColor.b, dark);
            _spotHole.color = dc;
            for (int i = 0; i < _spotFill.Length; i++) _spotFill[i].color = dc;

            // 빛줄기는 숨쉬듯 — 가만히 있으면 붙여 놓은 그림이다.
            float breathe = 0.85f + 0.15f * Mathf.Sin(Time.time * 4.2f);
            _spotBeam.color = new Color(1f, 1f, 1f, SpotBeamAlpha * breathe * _spotShown);
        }

        private bool MakeSpotlight()
        {
            var beam = GetSprite("fx_spotbeam");
            var hole = GetSprite("fx_spotdark");
            if (beam == null || hole == null || _shotLayer == null) return false;

            var go = new GameObject("SoulSpotlight", typeof(RectTransform));
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
            _spotBeam = SpotImage("Beam", beam, new Vector2(0.5f, SpotPivotY),
                                  Vector2.zero, new Vector2(SpotWidth, SpotHeight));
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
        }

        /// <summary>방을 넘어갈 때. 지난 방의 자리 · 몸 · 표시를 들고 가지 않는다.</summary>
        private void ClearSoulFx()
        {
            ReleaseCorpse();
            _hasFall = false;
            _soulRise = 0f;
            _soulCordLeft = 0f;
            _reviveHpPercent = 0;
            StopSoulCord();
            StopHomeFlame();
            _spotUnit = null;
            _spotShown = 0f;
            if (_spotRoot != null) _spotRoot.gameObject.SetActive(false);
            if (_ghost != null && _host == null && !IsChanneling)
                _ghost.transform.localScale = Vector3.one;
        }
    }
}
