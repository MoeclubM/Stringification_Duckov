using UnityEngine;
using System;

namespace Stringification.Utils
{
    public static class PhysicsUtils
    {
        /// <summary>
        /// Check if a hit represents a valid external collider (not the player itself or a trigger)
        /// 检查命中是否代表有效的外部碰撞体（不是玩家本身或触发器）
        /// </summary>
        public static bool IsExternalCollider(RaycastHit hit, Transform self)
        {
            if (self == null) return false;
            return hit.transform.root != self.root && !hit.collider.isTrigger;
        }

        /// <summary>
        /// Check if the player is grounded using CC and Physics Casts
        /// 使用 CC 和物理投射检查玩家是否着地
        /// </summary>
        public static bool IsGrounded(GameObject player, float checkDistance = 0.6f, bool useSphereCast = true, bool ignoreCC = false)
        {
            if (player == null) return false;
            
            // 1. CC Check (Optional)
            if (!ignoreCC)
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null && cc.isGrounded) return true;
            }

            Vector3 origin = player.transform.position + Vector3.up * 0.5f;
            Vector3 direction = Vector3.down;

            RaycastHit[] hits;
            if (useSphereCast)
            {
                hits = Physics.SphereCastAll(origin, 0.3f, direction, checkDistance);
            }
            else
            {
                hits = Physics.RaycastAll(origin, direction, checkDistance);
            }

            foreach (var hit in hits)
            {
                if (IsExternalCollider(hit, player.transform)) return true;
            }

            return false;
        }

        /// <summary>
        /// Check for obstacles in a direction
        /// 检查指定方向的障碍物
        /// </summary>
        public static bool CheckObstacle(Vector3 origin, Vector3 direction, float distance, Transform self, out RaycastHit hitInfo)
        {
            hitInfo = default;
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance);
            
            // Sort to find closest valid hit
            // 排序以找到最近的有效命中
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (IsExternalCollider(hit, self))
                {
                    hitInfo = hit;
                    return true;
                }
            }
            return false;
        }
    }
}
