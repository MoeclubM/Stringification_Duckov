using UnityEngine;

namespace Stringification.Components
{
    public class StringificationVisuals
    {
        private Transform? targetModel;
        private Transform? damageReceiver;
        private Rigidbody? playerRigidbody;
        private Vector3 originalScale = Vector3.one;
        private Vector3 originalDamageReceiverScale = Vector3.one;
        
        private bool isStringified = false;
        private bool isRecovering = false;
        private bool shouldRotate = false;
        private Quaternion targetRotation = Quaternion.identity;
        
        public float VisualRotationAngle { get; set; } = 90.0f;
        
        // Advanced Settings
        public float LerpSpeed { get; set; } = 15.0f;
        public float StringifiedThickness { get; set; } = 0.1f;

        public void SetTarget(Transform? model, Transform? damageReceiver, Rigidbody? rb)
        {
            targetModel = model;
            this.damageReceiver = damageReceiver;
            playerRigidbody = rb;
            originalScale = Vector3.one;
            originalDamageReceiverScale = Vector3.one;
        }

        /// <summary>
        /// 设置弦化状态（仅控制压缩）
        /// </summary>
        public void SetStringified(bool active)
        {
            if (active && !isStringified)
            {
                // Do not capture scale, assume 1
            }
            else if (!active && isStringified)
            {
                isRecovering = true;
            }
            isStringified = active;
        }

        /// <summary>
        /// 设置旋转状态（通过动画控制视觉旋转角度）
        /// </summary>
        public void SetRotation(bool rotate)
        {
            shouldRotate = rotate;
            if (rotate)
            {
                // 设置目标旋转以进行动画
                targetRotation = Quaternion.Euler(0, VisualRotationAngle, 0);
            }
            else
            {
                // 返回初始状态
                targetRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// 直接设置目标旋转，覆盖布尔状态
        /// </summary>
        public void SetTargetRotation(Quaternion rotation)
        {
            targetRotation = rotation;
            shouldRotate = false; // Reset flag as we are manually controlling
        }

        public void LateUpdate()
        {
            if (targetModel == null) return;

            bool allRecovered = true;

            // Update Target Model
            UpdateTransform(targetModel, originalScale, ref allRecovered);

            // Update Damage Receiver
            if (damageReceiver != null)
            {
                UpdateTransform(damageReceiver, originalDamageReceiverScale, ref allRecovered);
            }

            if (isRecovering && allRecovered)
            {
                isRecovering = false;
            }
        }

        private void UpdateTransform(Transform transform, Vector3 origScale, ref bool allRecovered)
        {
            // 缩放逻辑：应用弦化压缩
            if (isStringified)
            {
                Vector3 targetScale = new Vector3(origScale.x, origScale.y, StringifiedThickness);
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * LerpSpeed);
            }
            else if (isRecovering)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, origScale, Time.deltaTime * LerpSpeed);
                if (Vector3.Distance(transform.localScale, origScale) > 0.001f)
                {
                    allRecovered = false;
                }
                else
                {
                    transform.localScale = origScale; // Snap to target
                }
            }
            // 如果既不是弦化也不是恢复中，则不触碰缩放以允许外部修改

            // 旋转逻辑：平滑动画到目标旋转
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * LerpSpeed);
        }
    }
}
