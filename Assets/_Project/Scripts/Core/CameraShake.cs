using Unity.Cinemachine;
using UnityEngine;

namespace Game.Core
{
    /// <summary>피격 등 임팩트 순간 카메라를 짧게 흔든다(Cinemachine Impulse 사용).</summary>
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class CameraShake : MonoBehaviour
    {
        CinemachineImpulseSource _source;

        void Awake() => _source = GetComponent<CinemachineImpulseSource>();

        public void Shake(float force = 1f) => _source.GenerateImpulse(force);
    }
}
