using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 바닥에 놓는 자동 포탑.
    ///
    /// 정본에서 마지막까지 남아 있던 구멍이다 — 버프 2종(스마트 배치·화염 펌웨어),
    /// 시너지 2종(네이팜 터렛·도탄 터렛), 로봇의 정체성이 전부 여기 매여 있었다.
    ///
    /// ⚠ 스스로 쏘지 않는다. 탄 풀·피해·죽음 처리가 전부 BattleDirector 에 있으므로
    ///    **쏠 때가 됐는지만 알려준다.** 장판·상태이상에서 같은 이유로 같은 선택을 했다.
    /// </summary>
    public sealed class Deployable : MonoBehaviour
    {
        private const float FadeSeconds = 0.5f;

        private RectTransform _rect;
        private Image _image;
        private float _life;
        private float _fireTimer;

        public Vector2 Position { get; private set; }
        public float Range { get; private set; }
        public int Damage { get; private set; }
        public float FireInterval { get; private set; }
        public bool IsActive => _life > 0f;

        public void Init(Sprite sprite, Vector2 size)
        {
            _rect = (RectTransform)transform;
            _rect.anchorMin = _rect.anchorMax = new Vector2(0f, 1f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = size;
            _image = GetComponent<Image>();
            if (_image == null) _image = gameObject.AddComponent<Image>();
            _image.sprite = sprite;
            _image.preserveAspect = true;
            _image.raycastTarget = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// ⚠ 그림을 놓을 때마다 정한다. 포탑도 풀에서 돌려 쓰므로 태어날 때 정한 그림을
        ///    계속 쓰면, 로봇 아틀라스가 아직 안 올라온 판에서 만들어진 포탑이
        ///    **끝까지 흰 네모**로 남는다. 스프라이트가 없으면 아예 감춘다 —
        ///    흰 네모는 "설치물" 이 아니라 "덜 만든 티" 로 보인다.
        /// </summary>
        public void SetSprite(Sprite sprite)
        {
            _image.sprite = sprite;
            _image.enabled = sprite != null;
        }

        public void Spawn(Vector2 at, float seconds, float range, int damage, float fireInterval)
        {
            Position = at;
            _rect.anchoredPosition = new Vector2(Mathf.Round(at.x), Mathf.Round(at.y));
            _life = seconds;
            Range = range;
            Damage = damage;
            FireInterval = fireInterval;
            // 놓자마자 한 발 나간다. 첫 발을 기다리게 하면 "설치했는데 아무 일도 없다" 가 된다.
            _fireTimer = 0f;
            _image.color = Color.white;
            gameObject.SetActive(true);
        }

        /// <summary>시간을 흘린다. 이번 프레임에 쏠 차례면 true.</summary>
        public bool Tick(float dt)
        {
            if (_life <= 0f) return false;
            _life -= dt;
            if (_life <= 0f) { Despawn(); return false; }

            // 사라지기 전에 흐려진다 — 언제까지 믿고 있어도 되는지 보여야 한다
            if (_life < FadeSeconds)
            {
                var c = _image.color;
                c.a = _life / FadeSeconds;
                _image.color = c;
            }

            _fireTimer -= dt;
            if (_fireTimer > 0f) return false;
            _fireTimer = FireInterval;
            return true;
        }

        public void Despawn()
        {
            _life = 0f;
            gameObject.SetActive(false);
        }
    }
}
