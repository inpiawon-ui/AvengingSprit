using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 기본 공격 투사체. 발사 시점의 대상을 향해 날아가 명중하면 피해를 넘긴다.
    ///
    /// 유도(호밍)하지 않는다 — 발사 방향을 그대로 유지한다. 오토어택 게임에서
    /// 전탄 명중이 보장되면 이동으로 회피할 여지가 사라져 조작이 무의미해진다.
    ///
    /// 풀링해서 재사용한다. 매 발마다 GameObject 를 만들면 교전 중 GC 가 튄다.
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        private RectTransform _rect;
        private Image _image;

        private Vector2 _dir;
        private float _speed;
        private float _life;
        private int _damage;
        private Unit _target;
        private bool _fromPlayer;

        /// <summary>관통탄은 맞아도 사라지지 않는다. 같은 대상을 두 번 때리지 않도록 기록한다.</summary>
        private readonly List<Unit> _alreadyHit = new();

        public bool IsActive { get; private set; }
        public bool FromPlayer => _fromPlayer;
        public int Damage => _damage;
        public Unit Target => _target;
        public Vector2 Position => _rect.anchoredPosition;

        public bool Pierce { get; private set; }

        /// <summary>남은 도탄 횟수. 0 이면 벽에 닿는 순간 사라진다.</summary>
        public int BouncesLeft { get; private set; }

        /// <summary>도탄을 한 번이라도 했는가 (정본 BUF_T05 뱅크 샷의 조건).</summary>
        public bool HasBounced { get; private set; }
        public int SlowPercent { get; private set; }
        public int LifestealPercent { get; private set; }

        public bool HasHit(Unit u) => _alreadyHit.Contains(u);
        public void MarkHit(Unit u) => _alreadyHit.Add(u);

        /// <summary>
        /// 탄 그림을 갈아 끼운다. 탄은 풀에서 돌려 쓰므로 태어날 때 정한 그림을
        /// 계속 쓰면 **직전에 쏜 무기의 탄이 그대로 나간다.** 발사할 때마다 정한다.
        /// </summary>
        public void SetSprite(Sprite sprite)
        {
            if (sprite != null && _image != null) _image.sprite = sprite;
        }

        public void Init(Sprite sprite)
        {
            _rect = (RectTransform)transform;
            _rect.anchorMin = _rect.anchorMax = new Vector2(0f, 1f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _image = GetComponent<Image>();
            if (_image == null) _image = gameObject.AddComponent<Image>();
            _image.sprite = sprite;
            _image.raycastTarget = false;
            gameObject.SetActive(false);
        }

        public void Fire(Vector2 from, Vector2 to, float speed, int damage,
                         bool fromPlayer, Unit target, float size, Color color, float life,
                         bool pierce = false, int slowPercent = 0, int lifestealPercent = 0,
                         float angleOffsetDeg = 0f, int bounces = 0)
        {
            Pierce = pierce;
            BouncesLeft = bounces;
            HasBounced = false;
            SlowPercent = slowPercent;
            LifestealPercent = lifestealPercent;
            _alreadyHit.Clear();
            _rect.anchoredPosition = from;
            _rect.sizeDelta = new Vector2(size, size);
            var d = to - from;
            _dir = d.sqrMagnitude < 0.0001f ? Vector2.up : d.normalized;
            if (Mathf.Abs(angleOffsetDeg) > 0.01f)
            {
                float r = angleOffsetDeg * Mathf.Deg2Rad;
                float cos = Mathf.Cos(r), sin = Mathf.Sin(r);
                _dir = new Vector2(_dir.x * cos - _dir.y * sin, _dir.x * sin + _dir.y * cos);
            }
            // 탄환 스프라이트가 가로로 길어서, 돌려주지 않으면 위로 쏴도 오른쪽을 본다
            _rect.localEulerAngles =
                new Vector3(0f, 0f, Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg);
            _speed = speed;
            _damage = damage;
            _fromPlayer = fromPlayer;
            _target = target;
            _life = life;
            _image.color = color;
            IsActive = true;
            gameObject.SetActive(true);
        }

        public void Despawn()
        {
            IsActive = false;
            _target = null;
            gameObject.SetActive(false);
        }

        /// <summary>이동시킨다. 수명이 다하면 false.</summary>
        public bool Tick(float dt)
        {
            _rect.anchoredPosition += _dir * _speed * dt;
            _life -= dt;
            return _life > 0f;
        }

        /// <summary>
        /// 적 탄을 **내 탄으로 돌린다** (슬러거 그랜드 슬램).
        /// 방향을 뒤집고 주인을 바꾼다 — 지우는 것과 달리 화면의 탄이 그대로 자산이 된다.
        /// </summary>
        public void TurnFriendly(float damageMul)
        {
            _fromPlayer = true;
            _dir = -_dir;
            _damage = Mathf.Max(1, Mathf.RoundToInt(_damage * damageMul));
            _alreadyHit.Clear();
            _rect.localEulerAngles =
                new Vector3(0f, 0f, Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg);
        }

        /// <summary>
        /// 벽에 튕긴다. 남은 횟수가 없으면 false — 부르는 쪽이 없앤다.
        /// <paramref name="normal"/> 은 부딪힌 면의 바깥 방향이다.
        ///
        /// 튕긴 뒤 **맞은 목록을 비운다.** 안 그러면 되돌아온 탄이 방금 지나친 적을
        /// 그냥 통과한다 — 도탄의 재미는 왔던 길을 다시 훑는 데 있다.
        /// </summary>
        public bool Bounce(Vector2 normal)
        {
            if (BouncesLeft <= 0) return false;
            BouncesLeft--;
            HasBounced = true;
            _dir = Vector2.Reflect(_dir, normal).normalized;
            _rect.localEulerAngles =
                new Vector3(0f, 0f, Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg);
            _alreadyHit.Clear();
            return true;
        }
    }
}
