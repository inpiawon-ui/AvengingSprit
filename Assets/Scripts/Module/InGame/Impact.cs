using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 탄이 맞은 자리에서 터지는 그림. 짧게 넘기고 사라진다.
    ///
    /// 맞았다는 것이 숫자로만 나오면 어디서 맞았는지가 안 보인다 —
    /// 탄이 사라지는 것과 피해 숫자가 뜨는 것 사이에 아무 일도 안 일어나서,
    /// 탄이 그냥 없어진 것처럼 읽힌다.
    ///
    /// 장 수는 종류마다 다르다. 총알 자국은 두 장이면 되지만 수류탄 폭발은
    /// 원작이 다섯 장을 쓴다 — 불덩이가 부풀고, 하얗게 타고, 흩어진다.
    /// 그림이 없으면 아무것도 하지 않는다. 한 종씩 채워 넣을 수 있어야 한다.
    /// </summary>
    public sealed class Impact : MonoBehaviour
    {
        /// <summary>터짐 한 장의 시간. 짧게 튀어야 타격으로 읽힌다.</summary>
        private const float FrameSeconds = 0.06f;

        /// <summary>
        /// **상태 표시**(쉴드·스턴) 한 장의 시간.
        ///
        /// 터짐과 같은 0.06 초로 돌리면 초당 16장이라 화면이 정신없다. 이건 터지는 게
        /// 아니라 **켜져 있다**를 알리는 것이라, 느리게 숨 쉬듯 도는 편이 읽힌다.
        /// </summary>
        private const float LoopFrameSeconds = 0.45f;

        /// <summary>
        /// 밖에서 정한 한 장의 시간. 0 이면 위의 기본값을 쓴다.
        ///
        /// 바닥이 갈라지는 예고처럼 **정해진 시간에 걸쳐** 넘겨야 하는 그림이 있다.
        /// 0.06 초로 돌리면 예고가 1.15 초인데 그림은 0.24 초에 끝나 버려,
        /// 남은 0.9 초 동안 아무 일도 안 일어난 것처럼 보인다.
        /// </summary>
        private float _step;

        /// <summary>마지막 장에서 꺼지지 않고 **그대로 남는다.** 뚫린 구멍은 계속 뚫려 있어야 한다.</summary>
        private bool _hold;

        private float Step => _step > 0f ? _step : (_loop ? LoopFrameSeconds : FrameSeconds);

        private RectTransform _rect;
        private Image _image;
        private Sprite[] _frames;
        private float _timer;
        private int _index;

        /// <summary>
        /// 상태 표시(스턴 회전·쉴드 깜빡임)는 **끝나지 않는다.** 상태가 풀릴 때까지
        /// 돌아야 하므로 마지막 장에서 꺼지지 않고 첫 장으로 돌아간다.
        /// 터짐(`impact_*`)은 한 번 보여 주고 끝이라 기본값은 false 다.
        /// </summary>
        private bool _loop;

        public bool IsActive => gameObject.activeSelf;

        // ── 숨쉬는 크기 ────────────────────────────────────────
        //
        // 돌아가는 표시(결계·얼음·표적)는 **가만히 있으면 붙여 놓은 그림처럼 보인다.**
        // 크기를 천천히 오르내리면 살아 있는 것으로 읽힌다.
        // 폭은 사장님 지정값 0.8 ~ 1.0 이 기본이다 — 더 벌리면 커졌다 작아지는 것이
        // 아니라 튀는 것으로 보인다(기획 2026-09-15).

        private float _pulseMin, _pulseMax, _pulseSeconds, _pulseTime;
        private float _baseSize;

        /// <summary>돌아가는 동안 크기를 <paramref name="min"/>~<paramref name="max"/> 사이로 천천히 오간다.</summary>
        public void SetPulse(float min, float max, float seconds)
        {
            _pulseMin = Mathf.Max(0.05f, min);
            _pulseMax = Mathf.Max(_pulseMin, max);
            _pulseSeconds = Mathf.Max(0.1f, seconds);
            _pulseTime = 0f;
        }

        private void ClearPulse() => _pulseSeconds = 0f;

        public void Cache(RectTransform parent, float size)
        {
            _rect = (RectTransform)transform;
            _rect.SetParent(parent, false);
            _rect.anchorMin = _rect.anchorMax = new Vector2(0f, 1f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = new Vector2(size, size);

            _image = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            _image.raycastTarget = false;
            _image.preserveAspect = true;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// <paramref name="size"/> 는 화면에 그려질 상자 크기다. 폭발은 피해 반경만큼
        /// 커야 한다 — 그림이 반경보다 작으면 "안 맞았는데 맞았다" 로 읽힌다.
        /// </summary>
        public void Play(Vector2 at, Sprite[] frames, float size, bool loop = false)
        {
            if (frames == null || frames.Length == 0 || frames[0] == null) return;
            _rect.anchoredPosition = at;
            _rect.sizeDelta = new Vector2(size, size);
            // ⚠ 풀에서 돌려 쓰는 자리다. 앞서 쓰던 맥박·줄기가 남으면
            //   엉뚱한 그림이 숨을 쉬거나 기울어진 채로 뜬다.
            _baseSize = size;
            ClearPulse();
            if (_isBeam)
            {
                _isBeam = false;
                _image.preserveAspect = true;
                _rect.localEulerAngles = Vector3.zero;
            }
            _frames = frames;
            _index = 0;
            _image.sprite = frames[0];
            _image.color = Color.white;
            _tint = Color.white;
            _spin = 0f;
            _life = 0f;
            _rect.localEulerAngles = Vector3.zero;
            _loop = loop;
            _step = 0f;
            _hold = false;
            _timer = Step;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 여러 장을 <paramref name="seconds"/> 에 걸쳐 넘기고 **마지막 장에서 멈춰 선다.**
        /// 예고 내내 자라야 하는 그림(바닥 균열)에 쓴다. <see cref="Stop"/> 로 거둔다.
        /// </summary>
        public void PlayOver(Vector2 at, Sprite[] frames, float size, float seconds)
        {
            Play(at, frames, size, loop: false);
            if (!IsActive) return;
            _step = Mathf.Max(0.02f, seconds / Mathf.Max(1, frames.Length));
            _hold = true;
            _timer = _step;
        }

        // ── 두 점을 잇는 줄기 ──────────────────────────────────
        //
        // 번개·연쇄 방전은 **어디서 어디로** 갔는지가 보여야 한다.
        // 제자리에 한 덩이를 띄우면 "레이저는 저기 있는데 딴 놈이 맞는" 그림이 된다
        // (기획 2026-09-15). 그림은 가로로 그려져 있고, 여기서 **늘이고 돌린다.**

        /// <summary>
        /// <paramref name="from"/> 에서 <paramref name="to"/> 까지 줄기를 그린다.
        /// 그림은 가로 방향으로 그려져 있어야 한다 — 길이만큼 늘이고 각도만큼 돌린다.
        /// </summary>
        public void PlayBeam(Vector2 from, Vector2 to, Sprite[] frames, float thickness)
        {
            if (frames == null || frames.Length == 0 || frames[0] == null) return;
            var d = to - from;
            float len = d.magnitude;
            if (len < 1f) return;

            Play((from + to) * 0.5f, frames, thickness);
            if (!IsActive) return;

            // 줄기는 정사각형이 아니다. `Play` 가 맞춰 둔 정사각형을 여기서 덮어쓴다.
            _image.preserveAspect = false;
            _rect.sizeDelta = new Vector2(len, thickness);
            _rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            _isBeam = true;
        }

        /// <summary>줄기였던 자리를 되돌린다. 풀에서 돌려 쓰므로 반드시 필요하다.</summary>
        private bool _isBeam;

        // ── 스킬 시전 연출용 ───────────────────────────────────
        //
        // 시전 그림은 흰색으로 받아 **몸마다 색을 입힌다.** 마법진은 한 장이라
        // 가만히 두면 붙여 놓은 스티커다 — 천천히 돌리고, 정해진 시간 뒤 흐려지며 꺼진다.
        // 모두 `Play` 가 되돌린다(풀에서 돌려 쓰는 자리).

        private Color _tint = Color.white;
        private float _spin;        // 초당 도는 각도
        private float _life;        // 0 이면 수명 없음
        private float _fade;
        private float _age;

        /// <summary>그림에 색을 입힌다. <see cref="Play"/> 뒤에 부른다.</summary>
        public void SetTint(Color color)
        {
            _tint = color;
            if (_image != null) _image.color = color;
        }

        /// <summary>한 장의 시간을 정한다. <see cref="Play"/> 뒤에 부른다.</summary>
        public void SetFrameSeconds(float seconds)
        {
            _step = Mathf.Max(0.02f, seconds);
            _timer = _step;
        }

        /// <summary>그림을 초당 <paramref name="degreesPerSecond"/> 만큼 돌린다.</summary>
        public void SetSpin(float degreesPerSecond) => _spin = degreesPerSecond;

        /// <summary>
        /// <paramref name="seconds"/> 뒤에 꺼진다. 마지막 <paramref name="fadeSeconds"/> 동안 흐려진다.
        /// 한 장짜리를 loop 로 틀어 두고 이걸로 거둔다.
        /// </summary>
        public void SetLife(float seconds, float fadeSeconds)
        {
            _life = Mathf.Max(0.01f, seconds);
            _fade = Mathf.Clamp(fadeSeconds, 0.01f, _life);
            _age = 0f;
        }

        /// <summary>돌고 있는 표시를 몸을 따라 옮긴다.</summary>
        public void MoveTo(Vector2 at)
        {
            if (IsActive) _rect.anchoredPosition = at;
        }

        /// <summary>상태가 풀렸다. 돌던 것을 세우고 자리를 풀에 돌려준다.</summary>
        public void Stop()
        {
            _loop = false;
            ClearPulse();
            gameObject.SetActive(false);
        }

        public void Tick(float dt)
        {
            if (!IsActive) return;

            if (_spin != 0f && !_isBeam)
                _rect.localEulerAngles = new Vector3(0f, 0f, _rect.localEulerAngles.z + _spin * dt);

            if (_life > 0f)
            {
                _age += dt;
                if (_age >= _life) { Stop(); return; }
                float k = Mathf.Clamp01((_life - _age) / _fade);
                _image.color = new Color(_tint.r, _tint.g, _tint.b, _tint.a * k);
            }

            if (_pulseSeconds > 0f && !_isBeam)
            {
                _pulseTime += dt;
                // 0 → 1 → 0 을 왕복. 사인이 아니라 삼각파라 등속으로 오간다 —
                // 사인은 양끝에서 멈칫해 「숨」이 아니라 「멈춤」으로 보인다.
                float t = Mathf.PingPong(_pulseTime / _pulseSeconds * 2f, 1f);
                float k = Mathf.Lerp(_pulseMin, _pulseMax, t);
                _rect.sizeDelta = new Vector2(_baseSize * k, _baseSize * k);
            }

            _timer -= dt;
            if (_timer > 0f) return;

            _index++;
            if (_frames == null || _index >= _frames.Length || _frames[_index] == null)
            {
                // 다 갈라진 바닥은 그 자리에 남는다 — 거두는 것은 부른 쪽의 몫이다.
                if (_hold) { _index = Mathf.Max(0, (_frames?.Length ?? 1) - 1); _timer = Step; return; }

                if (!_loop || _frames == null || _frames.Length == 0 || _frames[0] == null)
                {
                    gameObject.SetActive(false);
                    return;
                }
                _index = 0;   // 상태 표시는 첫 장으로 돌아가 계속 돈다
            }
            _image.sprite = _frames[_index];
            _timer += Step;
        }
    }
}
