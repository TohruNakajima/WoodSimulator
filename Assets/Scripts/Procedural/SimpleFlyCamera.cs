using UnityEngine;

namespace WoodSimulator
{
    /// <summary>
    /// WASD+マウスによるフライカメラ。
    /// </summary>
    public class SimpleFlyCamera : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 20f;
        public float fastMultiplier = 3f;

        [Header("Mouse Look")]
        public float lookSpeed = 2f;

        // UIボタンからの入力
        private Vector3 uiMoveDirection;

        private float rotX;
        private float rotY;

        private void Start()
        {
            var euler = transform.eulerAngles;
            rotX = euler.y;
            rotY = euler.x;
        }

        private void Update()
        {
            // 右クリックでカメラ回転
            if (Input.GetMouseButton(1))
            {
                rotX += Input.GetAxis("Mouse X") * lookSpeed;
                rotY -= Input.GetAxis("Mouse Y") * lookSpeed;
                rotY = Mathf.Clamp(rotY, -89f, 89f);
                transform.rotation = Quaternion.Euler(rotY, rotX, 0f);
            }

            // WASD移動
            float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? fastMultiplier : 1f);
            Vector3 move = Vector3.zero;

            if (Input.GetKey(KeyCode.W)) move += transform.forward;
            if (Input.GetKey(KeyCode.S)) move -= transform.forward;
            if (Input.GetKey(KeyCode.A)) move -= transform.right;
            if (Input.GetKey(KeyCode.D)) move += transform.right;
            if (Input.GetKey(KeyCode.E)) move += Vector3.up;
            if (Input.GetKey(KeyCode.Q)) move -= Vector3.up;

            // UIボタンからの移動を加算
            if (uiMoveDirection.sqrMagnitude > 0f)
            {
                move += transform.forward * uiMoveDirection.z;
                move += transform.right * uiMoveDirection.x;
                move += Vector3.up * uiMoveDirection.y;
            }

            if (move.sqrMagnitude > 0f)
                transform.position += move.normalized * speed * Time.deltaTime;
        }

        /// <summary>
        /// UIボタンから呼ばれる移動方向設定。
        /// </summary>
        public void SetUIMove(Vector3 direction)
        {
            uiMoveDirection = direction;
        }

        /// <summary>
        /// UIボタンから呼ばれる回転。
        /// </summary>
        public void RotateYaw(float amount)
        {
            rotX += amount;
            transform.rotation = Quaternion.Euler(rotY, rotX, 0f);
        }

        public void RotatePitch(float amount)
        {
            rotY = Mathf.Clamp(rotY + amount, -89f, 89f);
            transform.rotation = Quaternion.Euler(rotY, rotX, 0f);
        }
    }
}
