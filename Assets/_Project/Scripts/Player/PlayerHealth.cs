using System;
using Game.Core;
using UnityEngine;

namespace Game.Player
{
    /// <summary>플레이어는 1회 피격 즉사. 사망 이벤트로 조작 차단 등을 알린다.</summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] SpriteRenderer body;
        [SerializeField] Color deadColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        [SerializeField] CameraShake shake;
        [SerializeField] float flashSeconds = 0.25f;
        [SerializeField] Color flashColor = new Color32(0xE0, 0x2A, 0x2A, 0x90);

        float _flashT;

        public bool IsDead { get; private set; }
        public event Action Died;

        public void TakeHit(HitInfo hit)
        {
            if (IsDead) return;
            IsDead = true;
            if (body != null) body.color = deadColor;
            if (shake != null) shake.Shake();
            _flashT = flashSeconds;
            Died?.Invoke();
        }

        void Update()
        {
            if (_flashT > 0f) _flashT = Mathf.Max(0f, _flashT - Time.unscaledDeltaTime);
        }

        void OnGUI()
        {
            if (_flashT <= 0f) return;
            var c = flashColor;
            c.a *= _flashT / flashSeconds;
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;
        }
    }
}
