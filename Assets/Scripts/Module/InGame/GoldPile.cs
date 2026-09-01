using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 바닥에 떨어진 골드 한 무더기.
    ///
    /// 적을 잡으면 **그 자리에** 떨어져 남아 있고, 방을 다 비우면 흩어져 있던 것이
    /// 한꺼번에 플레이어에게 빨려 들어온다. 방을 비운 순간 숫자만 올리면
    /// **어느 적이 얼마를 줬는지**가 안 보이고, 방을 훑는 재미도 사라진다.
    ///
    /// 세 마디로 산다.
    ///   떨어짐  <see cref="DropSeconds"/>  죽은 자리에서 살짝 튀어 옆에 안착
    ///   기다림  방이 빌 때까지. 제자리에서 오르내린다
    ///   빨림    <see cref="FlySeconds"/>   플레이어에게 모인다 (가속)
    ///
    /// 그림은 HUD 골드 아이콘을 빌려 쓴다 — 새 리소스가 필요 없다.
    /// </summary>
    public sealed class GoldPile : MonoBehaviour
    {
        private const float DropSeconds = 0.34f;
        private const float FlySeconds  = 0.42f;
        private const float HopHeight   = 26f;   // 떨어질 때 뜨는 높이(px)
        private const float BobHeight   = 3f;    // 기다리는 동안 오르내리는 폭
        private const float BobSpeed    = 2.6f;

        private enum Phase { Idle, Dropping, Waiting, Flying }

        private RectTransform _rect;
        private Image _image;

        private Phase _phase = Phase.Idle;
        private Vector2 _from, _land, _target;
        private float _time;
        private float _bobSeed;

        /// <summary>이 무더기가 품고 있는 골드. 도착할 때 부르는 쪽이 가져간다.</summary>
        public int Amount { get; private set; }

        public bool IsActive => _phase != Phase.Idle;

        /// <summary>바닥에 놓여 방이 비기를 기다리는 중인가.</summary>
        public bool IsWaiting => _phase == Phase.Waiting;

        public static GoldPile Create(Transform parent, Sprite sprite, Vector2 size)
        {
            var go = new GameObject("GoldPile", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var g = go.AddComponent<GoldPile>();
            g._rect = (RectTransform)go.transform;
            g._rect.anchorMin = g._rect.anchorMax = new Vector2(0f, 1f);
            g._rect.pivot = new Vector2(0.5f, 0.5f);
            g._rect.sizeDelta = size;
            g._image = go.GetComponent<Image>();
            g._image.sprite = sprite;
            g._image.raycastTarget = false;
            g._image.preserveAspect = true;
            go.SetActive(false);
            return g;
        }

        /// <summary>죽은 자리에서 떨어뜨린다. 좌표는 필드 기준이다.</summary>
        public void Drop(Vector2 at, int amount)
        {
            Amount = amount;
            _from = at;

            // 죽은 자리 정확히 그 점에 쌓으면 겹쳐서 한 개로 보인다.
            // ⚠ 한 마리가 3~4개를 떨구므로 **같은 점에서 여러 개가 나온다.**
            //   흩어지는 폭이 좁으면 그것들이 서로 겹쳐 결국 한 덩어리가 된다.
            _land = at + new Vector2(Random.Range(-34f, 34f), Random.Range(-20f, 14f));
            _bobSeed = Random.Range(0f, 10f);
            _time = 0f;
            _phase = Phase.Dropping;

            _rect.anchoredPosition = at;
            _rect.localScale = Vector3.one;
            gameObject.SetActive(true);
        }

        /// <summary>방이 비었다. 플레이어에게 간다.</summary>
        public void FlyTo(Vector2 target)
        {
            if (_phase != Phase.Waiting && _phase != Phase.Dropping) return;
            _from = _rect.anchoredPosition;
            _target = target;
            _time = 0f;
            _phase = Phase.Flying;
        }

        /// <summary>플레이어에게 닿은 **그 프레임에만** true.</summary>
        public bool Tick(float dt, Vector2 playerAt)
        {
            switch (_phase)
            {
                case Phase.Dropping:
                    _time += dt;
                    float d = Mathf.Clamp01(_time / DropSeconds);
                    // 포물선 — 위로 튀었다가 내려앉는다.
                    var at = Vector2.Lerp(_from, _land, d);
                    at.y += HopHeight * 4f * d * (1f - d);
                    _rect.anchoredPosition = at;
                    if (d >= 1f) { _phase = Phase.Waiting; _time = 0f; }
                    return false;

                case Phase.Waiting:
                    // 제자리에서 천천히 오르내린다. 완전히 멈춰 있으면 바닥 무늬로 보인다.
                    _time += dt;
                    _rect.anchoredPosition = _land
                        + new Vector2(0f, Mathf.Sin((_time + _bobSeed) * BobSpeed) * BobHeight);
                    return false;

                case Phase.Flying:
                    _time += dt;
                    float f = _time / FlySeconds;
                    if (f >= 1f) { Despawn(); return true; }
                    // 목표는 **매 프레임 다시 읽는다.** 플레이어가 걸어가는 중이라
                    // 출발할 때의 자리로 날리면 빈 바닥에 가서 사라진다.
                    _rect.anchoredPosition = Vector2.Lerp(_from, playerAt, f * f);
                    _rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.5f, f);
                    return false;

                default:
                    return false;
            }
        }

        public void Despawn()
        {
            _phase = Phase.Idle;
            Amount = 0;
            gameObject.SetActive(false);
        }
    }
}
