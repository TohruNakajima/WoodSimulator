using UnityEngine;

namespace WoodSimulator
{
    /// <summary>
    /// WASD+マウスによるシンプルなフライカメラ。
    /// LODテストシーンでの移動用。
    /// </summary>
    public class SimpleFlyCamera : MonoBehaviour
    {
        public float moveSpeed = 20f;
        public float fastMultiplier = 3f;
        public float lookSpeed = 2f;

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

            transform.position += move.normalized * speed * Time.deltaTime;
        }
    }
}
