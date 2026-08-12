using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 탄이 맞은 자리에서 터지는 그림. 두 장을 짧게 넘기고 사라진다.
    ///
    /// 맞았다는 것이 숫자로만 나오면 어디서 맞았는지가 안 보인다 —
    /// 탄이 사라지는 것과 피해 숫자가 뜨는 것 사이에 아무 일도 안 일어나서,
    /// 탄이 그냥 없어진 것처럼 읽힌다.
    ///
    /// 그림이 없으면 아무것도 하지 않는다. 한 종씩 채워 넣을 수 있어야 한다.
    /// </summary>
    public sealed class Impact : MonoBehaviour
    {
        private const float FrameSeconds = 0.06f;

        private RectTransform _rect;
        private Image _image;
        private Sprite _second;
        private float _timer;
        private bool _swapped;

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

        public void Play(Vector2 at, Sprite first, Sprite second)
        {
            if (first == null) return;
            _rect.anchoredPosition = at;
            _image.sprite = first;
            _image.color = Color.white;
            _second = second;
            _timer = FrameSeconds;
            _swapped = false;
            gameObject.SetActive(true);
        }

        public void Tick(float dt)
        {
            if (!IsActive) return;
            _timer -= dt;
            if (_timer > 0f) return;

            // 둘째 장이 없으면 한 장짜리로 끝낸다 — 반쪽만 온 납품에도 깨지지 않는다
            if (!_swapped && _second != null)
            {
                _swapped = true;
                _image.sprite = _second;
                _timer = FrameSeconds;
                return;
            }
            gameObject.SetActive(false);
        }
    }
}
