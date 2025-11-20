using UnityEngine;

namespace Stringification.Components
{
    public class FlightMechanics
    {
        public float SpeedMult { get; set; } = 1.5f;
        public float DescentRate { get; set; } = 2.0f;
        
        // Advanced Settings
        public float BaseSpeed { get; set; } = 5.0f;
        public float ObstacleCheckDistance { get; set; } = 1.0f;

        private bool isFlying = false;
        private float currentVerticalSpeed = 0f;
        private CharacterMainControl? activeControl;
        private Transform? activeModel;
        private Vector3 flightDirection;

        public bool IsFlying => isFlying;

        public void StartFlight(GameObject? player, Rigidbody? rb, CharacterMainControl? control, Transform? model)
        {
            if (player == null || rb == null) return;

            isFlying = true;
            activeControl = control;
            activeModel = model;
            
            // Determine flight direction using the model forward if available
            // 优先使用模型前方向
            Transform forwardSource = activeModel != null ? activeModel : player.transform;
            flightDirection = forwardSource.forward;
            
            // Flatten direction
            // 扁平化方向
            flightDirection.y = 0;
            flightDirection.Normalize();

            // Disable main control to prevent fighting
            // 禁用主控制以防止冲突
            if (activeControl != null) activeControl.enabled = false;

            // Start with current downward velocity if any, otherwise zero (never upward)
            currentVerticalSpeed = Mathf.Min(0f, rb.velocity.y);
            
            Debug.Log("Stringification: Flight Mode Activated!");
        }

        public void StopFlight()
        {
            if (isFlying)
            {
                isFlying = false;
                // Re-enable main control
                // 重新启用主控制
                if (activeControl != null) activeControl.enabled = true;
                activeControl = null;
                activeModel = null;
                Debug.Log("Stringification: Flight Mode Deactivated.");
            }
        }

        public void UpdateLogic(GameObject? player, Rigidbody? rb)
        {
            if (!isFlying || player == null || rb == null) return;

            // 飞行物理 - 纯位置模拟
            // Flight Physics - Pure Simulation (Transform based)
            
            // 1. 确定飞行方向：使用启动时锁定的方向
            // Direction: Use the direction locked at start
            Vector3 moveDir = flightDirection;
            
            // 2. 计算飞行速度
            // Calculate flight velocity
            Vector3 flightVel = moveDir * (BaseSpeed * SpeedMult);
            
            // 3. 应用重力
            // Apply Gravity/Lift
            float descentPerSecond = Mathf.Abs(DescentRate);
            currentVerticalSpeed -= descentPerSecond * Time.deltaTime;
            currentVerticalSpeed = Mathf.Min(currentVerticalSpeed, 0f);
            flightVel.y = currentVerticalSpeed;

            // 4. 障碍物检测 (碰撞解除弦化)
            // Obstacle Detection: Cancel stringification if hitting a wall
            // 检测前方 1.0 米 (或根据速度动态调整)
            // Check 1.0 meter ahead (or adjust dynamically based on speed)
            float checkDistance = ObstacleCheckDistance;
            // 稍微抬高检测点，避免检测到地面上的小凸起
            // Raise the check point slightly to avoid detecting small bumps on the ground
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
