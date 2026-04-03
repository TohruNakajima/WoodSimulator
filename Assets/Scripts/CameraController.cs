using UnityEngine;
using UnityEngine.EventSystems;

namespace WoodSimulator
{
    /// <summary>
    /// カメラ制御（俯瞰視点、ズーム/パン/回転）
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("注視点")]
        public Vector3 targetPosition = Vector3.zero;

        [Header("Camera Settings")]
        [Tooltip("初期回転角度（Y軸）")]
        public float initialRotationY = 0f;

        [Header("Zoom Settings")]
        [Tooltip("ズーム速度")]
        public float zoomSpeed = 10f;

        [Tooltip("最小距離")]
        public float minDistance = 50f;

        [Tooltip("最大距離")]
        public float maxDistance = 300f;

        [Header("Pan Settings")]
        [Tooltip("パン速度")]
        public float panSpeed = 0.5f;

        [Header("Rotation Settings")]
        [Tooltip("回転速度")]
        public float rotationSpeed = 100f;

        [Header("Height Limit")]
        [Tooltip("最小高度（地面貫通防止）")]
        public float minHeight = 10f;

        private float initialHeight;
        private float initialDistance;
        private float currentDistance;
        private float currentRotationY;
        private Vector3 currentTargetPosition;

        private void Start()
        {
            // シーン上のカメラ位置から初期値を算出
            Vector3 offset = transform.position - targetPosition;
            initialHeight = offset.y;
            initialDistance = new Vector2(offset.x, offset.z).magnitude;

            currentDistance = initialDistance;
            currentRotationY = initialRotationY;
            currentTargetPosition = targetPosition;
        }

        private void Update()
        {
            HandleZoom();
            HandlePan();
            HandleRotation();

            UpdateCameraPosition();
        }

        /// <summary>
        /// ズーム処理（マウスホイール）
        /// </summary>
        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentDistance -= scroll * zoomSpeed;
                currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
            }
        }

        /// <summary>
        /// パン処理（中ボタンドラッグ）
        /// </summary>
        private void HandlePan()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (Input.GetMouseButton(2)) // 中ボタン
            {
                float h = Input.GetAxis("Mouse X") * panSpeed;
                float v = Input.GetAxis("Mouse Y") * panSpeed;

                Vector3 right = transform.right;
                Vector3 forward = Vector3.Cross(right, Vector3.up).normalized;

                currentTargetPosition -= right * h + forward * v;
            }
        }

        /// <summary>
        /// 回転処理（右ボタンドラッグ）
        /// </summary>
        private void HandleRotation()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (Input.GetMouseButton(1)) // 右ボタン
            {
                float h = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
                currentRotationY += h;
            }
        }

        /// <summary>
        /// カメラ位置更新
        /// </summary>
        private void UpdateCameraPosition()
        {
            // 回転を適用したオフセット計算
            Quaternion rotation = Quaternion.Euler(0, currentRotationY, 0);
            Vector3 offset = rotation * new Vector3(0, initialHeight, -currentDistance);

            Vector3 newPosition = currentTargetPosition + offset;

            // 最小高度制限
            if (newPosition.y < minHeight)
            {
                newPosition.y = minHeight;
            }

            transform.position = newPosition;
            transform.LookAt(currentTargetPosition);
        }

        /// <summary>
        /// 注視点設定
        /// </summary>
        public void SetTarget(Vector3 target)
        {
            currentTargetPosition = target;
        }

        /// <summary>
        /// カメラリセット
        /// </summary>
        public void ResetCamera()
        {
            currentDistance = initialDistance;
            currentRotationY = initialRotationY;
            currentTargetPosition = targetPosition;
            UpdateCameraPosition();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 注視点可視化
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentTargetPosition != Vector3.zero ? currentTargetPosition : targetPosition, 5f);
        }
#endif
    }
}
