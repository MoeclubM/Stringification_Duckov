using UnityEngine;

namespace Stringification.Components
{
    public class StringificationVisuals
    {
        private Transform? targetModel;
        private Transform? damageReceiver;
        private Rigidbody? playerRigidbody;
        private Vector3 originalScale;
        private Vector3 originalDamageReceiverScale;
        private Quaternion lastWorldRotation;
        private Vector3 lastCharacterPosition;
        
        private bool isStringified = false;
        private bool isFlying = false;
        private bool wasFlying = false;
        
        public float FlightPitch { get; set; } = 75.0f;
        public float VisualRotationAngle { get; set; } = 90.0f;
        
        // Advanced Settings
        public float LerpSpeed { get; set; } = 15.0f;
        public float StringifiedThickness { get; set; } = 0.05f;

        public void SetTarget(Transform? model, Transform? damageReceiver, Rigidbody? rb)
        {
            targetModel = model;
            this.damageReceiver = damageReceiver;
            playerRigidbody = rb;
            if (targetModel != null)
            {
                if (targetModel.localScale.z > 0.1f)
                {
                    originalScale = targetModel.localScale;
                }
                if (targetModel.parent != null)
                {
                    lastCharacterPosition = targetModel.parent.position;
                }
            }
            if (this.damageReceiver != null && this.damageReceiver.localScale.z > 0.1f)
            {
                originalDamageReceiverScale = this.damageReceiver.localScale;
            }
        }

        public void SetStringified(bool active, bool isGrounded)
        {
            bool wasStringified = isStringified;
            isStringified = active;
            
            if (active && !wasStringified)
            {
                if (isGrounded)
                {
                    if (targetModel != null)
                    {
                        targetModel.localRotation = Quaternion.Euler(0, VisualRotationAngle, 0);
                        lastWorldRotation = targetModel.rotation;
                    }
                    if (damageReceiver != null)
                    {
                        damageReceiver.localRotation = Quaternion.Euler(0, VisualRotationAngle, 0);
                    }
                }
                else
                {
                    if (targetModel != null)
                    {
                        lastWorldRotation = targetModel.rotation;
                    }
                }
            }
        }

        public void SetFlying(bool flying)
        {
            wasFlying = isFlying;
            isFlying = flying;
        }

        public void LateUpdate()
        {
            if (targetModel == null) return;

            // 1. Scale Logic
            // 1. 缩放逻辑
            Vector3 targetScale = isStringified ? new Vector3(originalScale.x, originalScale.y, StringifiedThickness) : originalScale;
            targetModel.localScale = Vector3.Lerp(targetModel.localScale, targetScale, Time.deltaTime * LerpSpeed);

            if (damageReceiver != null)
            {
                Vector3 targetDamageScale = isStringified ? new Vector3(originalDamageReceiverScale.x, originalDamageReceiverScale.y, StringifiedThickness) : originalDamageReceiverScale;
                damageReceiver.localScale = Vector3.Lerp(damageReceiver.localScale, targetDamageScale, Time.deltaTime * LerpSpeed);
            }

            // 2. Rotation Logic
            // 2. 旋转逻辑
            Quaternion targetRotation = Quaternion.identity;

            if (isFlying)
            {
                // Paper Plane mode: Face forward and tilt up (pitch)
                // 纸飞机模式：面向前方并向上倾斜（俯仰角）
                // User requested: "Lock direction (same as idle logic)"
                // 用户请求：“锁定方向（与空闲逻辑相同）”
                // Idle logic uses lastWorldRotation.
                // 空闲逻辑使用 lastWorldRotation。
                
                // If we are just entering flight, we should capture the rotation?
                // 如果我们刚刚进入飞行状态，我们应该捕捉旋转吗？
                // But we want to maintain the "Paper Plane" orientation.
                // 但我们想保持“纸飞机”的方向。
                // If we lock world rotation, the plane won't turn visually.
                // 如果我们锁定世界旋转，飞机在视觉上就不会转动。
                
                if (lastWorldRotation != Quaternion.identity && lastWorldRotation != new Quaternion(0,0,0,0))
                {
                    // We want to apply the Pitch to the Locked World Rotation.
                    // 我们想将俯仰角应用到锁定的世界旋转上。
                    // lastWorldRotation is the "Flat" rotation (usually).
                    // lastWorldRotation 是“扁平”旋转（通常）。
                    // If we just use lastWorldRotation, it's flat (90 deg Y).
                    // 如果我们只使用 lastWorldRotation，它是扁平的（Y轴90度）。
                    // We want to pitch it.
                    // 我们想让它倾斜。
                    
                    // But wait, if we lock world rotation, we ignore the player's rotation.
                    // 等等，如果我们锁定世界旋转，我们就忽略了玩家的旋转。
                    // The player isn't rotating anymore (I removed it in FlightMechanics).
                    // 玩家不再旋转了（我在 FlightMechanics 中移除了它）。
                    // So local rotation is fine?
                    // 所以局部旋转是可以的？
                    
                    // If player root is NOT rotating, then Local Rotation relative to Root is effectively World Rotation relative to Root's initial orientation.
                    // 如果玩家根节点没有旋转，那么相对于根节点的局部旋转实际上就是相对于根节点初始方向的世界旋转。
                    // So we can just set local rotation.
                    // 所以我们可以直接设置局部旋转。
                    
                    targetRotation = Quaternion.Euler(FlightPitch, 0, 0); 
                    
                    targetModel.localRotation = Quaternion.Slerp(targetModel.localRotation, targetRotation, Time.deltaTime * LerpSpeed);
                    if (damageReceiver != null) damageReceiver.localRotation = Quaternion.Slerp(damageReceiver.localRotation, targetRotation, Time.deltaTime * LerpSpeed);
                }
            }
            else if (isStringified)
            {
                if (wasFlying)
                {
                    targetModel.localRotation = Quaternion.Euler(0, VisualRotationAngle, 0);
                    if (damageReceiver != null) damageReceiver.localRotation = Quaternion.Euler(0, VisualRotationAngle, 0);
                    lastWorldRotation = targetModel.rotation;
                    wasFlying = false;
                }

                // Check if moving using position delta for robustness
                // 使用位置增量检查是否移动，以提高鲁棒性
                bool isMoving = false;
                Transform characterTransform = targetModel.parent;
                if (characterTransform != null)
                {
                    Vector3 currentPos = characterTransform.position;
                    Vector3 displacement = currentPos - lastCharacterPosition;
                    displacement.y = 0; // Ignore vertical movement for idle check // 忽略垂直移动以进行空闲检查
                    
                    // Check speed (units per second)
                    // 检查速度（单位/秒）
                    float speed = displacement.magnitude / Time.deltaTime;
                    isMoving = speed > 0.1f;
                    
                    lastCharacterPosition = currentPos;
                }
                else if (playerRigidbody != null)
                {
                    // Fallback to Rigidbody if parent is null (unlikely)
                    // 如果父节点为空（不太可能），则回退到 Rigidbody
                    Vector3 vel = playerRigidbody.velocity;
                    vel.y = 0;
                    isMoving = vel.magnitude > 0.1f;
                }

                if (isMoving)
                {
                    targetRotation = Quaternion.Euler(0, VisualRotationAngle, 0);
                }
                
                // Apply Rotation
                if (!isMoving)
                {
                    // Idle: Lock world rotation
                    if (lastWorldRotation != Quaternion.identity && lastWorldRotation != new Quaternion(0,0,0,0))
                    {
                        targetModel.rotation = lastWorldRotation;
                        if (damageReceiver != null) damageReceiver.rotation = lastWorldRotation;
                    }
                    else
                    {
                        targetModel.localRotation = Quaternion.Euler(0, VisualRotationAngle, 0);
                        if (damageReceiver != null) damageReceiver.localRotation = Quaternion.Euler(0, VisualRotationAngle, 0);
                        lastWorldRotation = targetModel.rotation;
                    }
                }
                else
                {
                    // Moving: Follow parent rotation with offset
                    targetModel.localRotation = Quaternion.Slerp(targetModel.localRotation, targetRotation, Time.deltaTime * LerpSpeed);
                    if (damageReceiver != null) damageReceiver.localRotation = Quaternion.Slerp(damageReceiver.localRotation, targetRotation, Time.deltaTime * LerpSpeed);
                    lastWorldRotation = targetModel.rotation;
                }
            }
            else
            {
                targetModel.localRotation = Quaternion.Slerp(targetModel.localRotation, Quaternion.identity, Time.deltaTime * LerpSpeed);
                if (damageReceiver != null) damageReceiver.localRotation = Quaternion.Slerp(damageReceiver.localRotation, Quaternion.identity, Time.deltaTime * LerpSpeed);
            }
        }
    }
}
