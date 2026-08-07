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
        public int SlowPercent { get; private set; }
        public int LifestealPercent { get; private set; }

        public bool HasHit(Unit u) => _alreadyHit.Contains(u);
        public void MarkHit(Unit u) => _alreadyHit.Add(u);

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
                         float angleOffsetDeg = 0f)
        {
            Pierce = pierce;
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
    }
}
