using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 카메라를 16:9 / 고정 Orthographic Size로 유지한다. 창 비율이 다르면 레터박스로 맞춘다.
    /// '화면 가로 길이' 기준 수치가 흔들리지 않게 하는 것이 목적.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public class FixedCamera : MonoBehaviour
    {
        Camera _camera;
        int _lastWidth, _lastHeight;

        void OnEnable()
        {
            _camera = GetComponent<Camera>();
            Apply();
        }

        void Update()
        {
            if (Screen.width != _lastWidth || Screen.height != _lastHeight) Apply();
        }

        void Apply()
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            _camera.orthographic = true;
            _camera.orthographicSize = GameConstants.CameraOrthographicSize;

            float windowAspect = _lastHeight > 0 ? (float)_lastWidth / _lastHeight : GameConstants.TargetAspect;
            float scale = windowAspect / GameConstants.TargetAspect;
            _camera.rect = scale >= 1f
                ? new Rect((1f - 1f / scale) * 0.5f, 0f, 1f / scale, 1f)
                : new Rect(0f, (1f - scale) * 0.5f, 1f, scale);
        }
    }
}
