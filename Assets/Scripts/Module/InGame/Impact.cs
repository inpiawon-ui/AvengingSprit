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
            _frames = frames;
            _index = 0;
            _image.sprite = frames[0];
            _image.color = Color.white;
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

        /// <summary>돌고 있는 표시를 몸을 따라 옮긴다.</summary>
        public void MoveTo(Vector2 at)
        {
            if (IsActive) _rect.anchoredPosition = at;
        }

        /// <summary>상태가 풀렸다. 돌던 것을 세우고 자리를 풀에 돌려준다.</summary>
        public void Stop()
        {
            _loop = false;
            gameObject.SetActive(false);
        }

        public void Tick(float dt)
        {
            if (!IsActive) return;
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
