using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace WoodSimulator
{
    /// <summary>
    /// カメラ操作用UIパネル。
    /// ボタン押下中にカメラが移動・回転する。
    /// </summary>
    public class CameraControlUI : MonoBehaviour
    {
        public SimpleFlyCamera flyCamera;

        [Header("Move Buttons")]
        public Button forwardButton;
        public Button backButton;
        public Button leftButton;
        public Button rightButton;
        public Button upButton;
        public Button downButton;

        [Header("Rotate Buttons")]
        public Button rotLeftButton;
        public Button rotRightButton;
        public Button rotUpButton;
        public Button rotDownButton;

        [Header("Settings")]
        public float rotateSpeed = 60f;

        private Vector3 currentMove;
        private float currentYaw;
        private float currentPitch;

        private void Start()
        {
            SetupHoldButton(forwardButton, () => currentMove.z = 1f, () => currentMove.z = 0f);
            SetupHoldButton(backButton, () => currentMove.z = -1f, () => currentMove.z = 0f);
            SetupHoldButton(leftButton, () => currentMove.x = -1f, () => currentMove.x = 0f);
            SetupHoldButton(rightButton, () => currentMove.x = 1f, () => currentMove.x = 0f);
            SetupHoldButton(upButton, () => currentMove.y = 1f, () => currentMove.y = 0f);
            SetupHoldButton(downButton, () => currentMove.y = -1f, () => currentMove.y = 0f);

            SetupHoldButton(rotLeftButton, () => currentYaw = -1f, () => currentYaw = 0f);
            SetupHoldButton(rotRightButton, () => currentYaw = 1f, () => currentYaw = 0f);
            SetupHoldButton(rotUpButton, () => currentPitch = -1f, () => currentPitch = 0f);
            SetupHoldButton(rotDownButton, () => currentPitch = 1f, () => currentPitch = 0f);
        }

        private void Update()
        {
            if (flyCamera == null) return;

            flyCamera.SetUIMove(currentMove);

            if (Mathf.Abs(currentYaw) > 0f)
                flyCamera.RotateYaw(currentYaw * rotateSpeed * Time.deltaTime);
            if (Mathf.Abs(currentPitch) > 0f)
                flyCamera.RotatePitch(currentPitch * rotateSpeed * Time.deltaTime);
        }

        private void SetupHoldButton(Button button, System.Action onPress, System.Action onRelease)
        {
            if (button == null) return;

            var trigger = button.gameObject.AddComponent<EventTrigger>();

            var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDown.callback.AddListener(_ => onPress());
            trigger.triggers.Add(pointerDown);

            var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            pointerUp.callback.AddListener(_ => onRelease());
            trigger.triggers.Add(pointerUp);

            var pointerExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            pointerExit.callback.AddListener(_ => onRelease());
            trigger.triggers.Add(pointerExit);
        }
    }
}
