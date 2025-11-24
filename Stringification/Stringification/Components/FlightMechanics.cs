using UnityEngine;

namespace Stringification.Components
{
    public class FlightMechanics
    {
        public float FlightSpeed { get; set; } = 7.5f;
        public float DescentRate { get; set; } = 2.0f;
        public float FlightPitch { get; set; } = 75.0f;
        
        // Steering Settings
        public bool EnableSteering { get; set; } = true;
        public float SteeringSpeed { get; set; } = 45.0f; // Degrees per second

        // Advanced Settings
        public float ObstacleCheckDistance { get; set; } = 1.0f;
        public float VisualLerpSpeed { get; set; } = 15.0f;

        private bool isFlying = false;
        private float currentVerticalSpeed = 0f;
        private CharacterMainControl? activeControl;
        private Transform? activeModel;
        private Transform? activeDamageReceiver;
        private Vector3 flightDirection;

        public bool IsFlying => isFlying;

        public void StartFlight(GameObject? player, Rigidbody? rb, CharacterMainControl? control, Transform? model, Transform? damageReceiver)
        {
            if (player == null || rb == null) return;

            isFlying = true;
            activeControl = control;
            activeModel = model;
            activeDamageReceiver = damageReceiver;
            
            // 优先使用模型前方向
            Transform forwardSource = activeModel != null ? activeModel : player.transform;
            flightDirection = forwardSource.forward;
            
            // 扁平化方向
            flightDirection.y = 0;
            flightDirection.Normalize();

            // 禁用主控制以防止冲突
            if (activeControl != null) activeControl.enabled = false;

            currentVerticalSpeed = Mathf.Min(0f, rb.velocity.y);
            
            Debug.Log("Stringification: Flight Mode Activated!");
        }

        public void StopFlight()
        {
            if (isFlying)
            {
                isFlying = false;
                // 重新启用主控制
                if (activeControl != null) activeControl.enabled = true;
                activeControl = null;
                activeModel = null;
                activeDamageReceiver = null;
                Debug.Log("Stringification: Flight Mode Deactivated.");
            }
        }

        public void UpdateLogic(GameObject? player, Rigidbody? rb, float horizontalInput)
        {
            if (!isFlying || player == null || rb == null) return;

            // 飞行物理 - 纯位置模拟
            
            // 0. 转向逻辑
            if (EnableSteering && Mathf.Abs(horizontalInput) > 0.01f)
            {
                float rotationAmount = horizontalInput * SteeringSpeed * Time.deltaTime;
                Quaternion turn = Quaternion.Euler(0, rotationAmount, 0);
                flightDirection = turn * flightDirection;
                
                // 同时旋转角色模型以匹配飞行方向
                player.transform.rotation = turn * player.transform.rotation;
            }

            // 1. 确定飞行方向：使用启动时锁定的方向 (现在可以被转向修改)
            Vector3 moveDir = flightDirection;
            
            // 2. 计算飞行速度
            Vector3 flightVel = moveDir * FlightSpeed;
            
            // 3. 应用重力
            float descentPerSecond = Mathf.Abs(DescentRate);
            currentVerticalSpeed -= descentPerSecond * Time.deltaTime;
            currentVerticalSpeed = Mathf.Min(currentVerticalSpeed, 0f);
            flightVel.y = currentVerticalSpeed;

            // 4. 障碍物检测 (碰撞解除弦化)
            // 检测前方 1.0 米 (或根据速度动态调整)
            float checkDistance = ObstacleCheckDistance;
            // 稍微抬高检测点，避免检测到地面上的小凸起
            Vector3 checkOrigin = player.transform.position + Vector3.up * 0.5f;
            
            if (Stringification.Utils.PhysicsUtils.CheckObstacle(checkOrigin, moveDir, checkDistance, player.transform, out RaycastHit hit))
            {
                Debug.Log($"Stringification: Hit obstacle '{hit.collider.name}'. Cancelling flight.");
                StopFlight();
                return;
            }

            // 5. 应用位移
            // Apply movement directly to transform to force execution
            Vector3 displacement = flightVel * Time.deltaTime;
            
            player.transform.position += displacement;
            
            // 6. 落地检测
            // Landing check
            bool landed = false;
            if (currentVerticalSpeed < 0)
            {
                // Use PhysicsUtils for ground detection
                // 使用 PhysicsUtils 进行地面检测
                if (Stringification.Utils.PhysicsUtils.IsGrounded(player, 0.6f, false, true)) 
                {
                    landed = true;
                }
            }

            if (landed)
            {
                StopFlight();
                Debug.Log("Stringification: Landed.");
            }
        }
    }
}
