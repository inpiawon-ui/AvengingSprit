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
        private const float FrameSeconds = 0.06f;

        private RectTransform _rect;
        private Image _image;
        private Sprite[] _frames;
        private float _timer;
        private int _index;

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
        public void Play(Vector2 at, Sprite[] frames, float size)
        {
            if (frames == null || frames.Length == 0 || frames[0] == null) return;
            _rect.anchoredPosition = at;
            _rect.sizeDelta = new Vector2(size, size);
            _frames = frames;
            _index = 0;
            _image.sprite = frames[0];
            _image.color = Color.white;
            _timer = FrameSeconds;
            gameObject.SetActive(true);
        }

        public void Tick(float dt)
        {
            if (!IsActive) return;
            _timer -= dt;
            if (_timer > 0f) return;

            _index++;
            if (_frames == null || _index >= _frames.Length || _frames[_index] == null)
            {
                gameObject.SetActive(false);
                return;
            }
            _image.sprite = _frames[_index];
            _timer += FrameSeconds;
        }
    }
}
