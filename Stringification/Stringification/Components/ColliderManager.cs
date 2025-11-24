using System.Collections.Generic;
using UnityEngine;

namespace Stringification.Components
{
    public class ColliderManager
    {
        private GameObject? cachedPlayer;

        private readonly List<BoxColliderState> boxColliders = new List<BoxColliderState>();
        private readonly List<CapsuleHandler> capsuleHandlers = new List<CapsuleHandler>();
        private readonly List<SphereColliderState> sphereColliders = new List<SphereColliderState>();
        private readonly List<ReflectionFieldHandler> reflectionHandlers = new List<ReflectionFieldHandler>();
        private CharacterControllerState? characterControllerState;

        private Transform? damageReceiver;

        public void Reset()
        {
            cachedPlayer = null;
            damageReceiver = null;
            boxColliders.Clear();
            foreach (var handler in capsuleHandlers) handler.Cleanup();
            capsuleHandlers.Clear();
            sphereColliders.Clear();
            reflectionHandlers.Clear();
            characterControllerState = null;
        }

        public void SyncPlayer(GameObject? player, Transform? model, Transform? damageReceiver)
        {
            if (player == null)
            {
                Reset();
                return;
            }

            if (player == cachedPlayer && (boxColliders.Count > 0 || capsuleHandlers.Count > 0 || sphereColliders.Count > 0 || characterControllerState != null))
            {
                return;
            }

            cachedPlayer = player;
            this.damageReceiver = damageReceiver;
            CacheColliders(player);
        }

        public void ApplyStringification(bool active, float thicknessRatio, Transform? targetModel)
        {
            if (!active)
            {
                RestoreAll();
                return;
            }

            if (cachedPlayer == null) return;

            float ratio = Mathf.Clamp(thicknessRatio, 0.01f, 1f);

            foreach (var state in boxColliders) state.Apply(ratio);
            foreach (var handler in capsuleHandlers) handler.Apply(ratio, targetModel);
            foreach (var state in sphereColliders) state.Apply(ratio);
            foreach (var handler in reflectionHandlers) handler.Apply(ratio);
            characterControllerState?.Apply(ratio);
        }

        public void ResolveCollisions()
        {
            if (cachedPlayer == null) return;
            foreach (var handler in capsuleHandlers) handler.ResolveCollision(cachedPlayer.transform);
        }

        private void RestoreAll()
        {
            foreach (var state in boxColliders) state.Restore();
            foreach (var handler in capsuleHandlers) handler.Restore();
            foreach (var state in sphereColliders) state.Restore();
            foreach (var handler in reflectionHandlers) handler.Restore();
            characterControllerState?.Restore();
        }

        private void CacheColliders(GameObject player)
        {
            boxColliders.Clear();
            foreach (var handler in capsuleHandlers) handler.Cleanup();
            capsuleHandlers.Clear();
            sphereColliders.Clear();
            reflectionHandlers.Clear();
            characterControllerState = null;

            var colliders = player.GetComponentsInChildren<Collider>(true);
            
            foreach (var collider in colliders)
            {
                if (collider is CharacterController controller)
                {
                    characterControllerState = new CharacterControllerState(controller);
                    continue;
                }

                if (collider is BoxCollider box)
                {
                    boxColliders.Add(new BoxColliderState(box));
                }
                else if (collider is CapsuleCollider capsule)
                {
                    // Skip DamageReceiver as requested
                    if ((damageReceiver != null && capsule.transform == damageReceiver) || 
                        capsule.gameObject.name == "DamageReceiver")
                    {
                        continue;
                    }

                    capsuleHandlers.Add(new CapsuleHandler(capsule, damageReceiver));
                }
                else if (collider is SphereCollider sphere)
                {
                    sphereColliders.Add(new SphereColliderState(sphere));
                }
            }

            CacheReflectionFields(player);
            
            Debug.Log($"[Stringification] Cached Colliders for {player.name}: CC={characterControllerState != null}, Box={boxColliders.Count}, Capsule={capsuleHandlers.Count}, Sphere={sphereColliders.Count}, Reflection={reflectionHandlers.Count}");
        }

        private void CacheReflectionFields(GameObject player)
        {
            var components = player.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var comp in components)
            {
                if (comp == null) continue;
                var type = comp.GetType();
                
                if (type.Name == "CharacterMovement")
                {
                    var field = type.GetField("_radius", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    if (field != null && field.FieldType == typeof(float))
                    {
                        reflectionHandlers.Add(new ReflectionFieldHandler(comp, field));
                    }
                }

                /*
                if (type.Name == "CharacterModel")
                {
                    var field = type.GetField("damageReceiverRadius", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    if (field != null && field.FieldType == typeof(float))
                    {
                        reflectionHandlers.Add(new ReflectionFieldHandler(comp, field));
                    }
                }
                */
            }
        }

        private class ReflectionFieldHandler
        {
            private readonly object component;
            private readonly System.Reflection.FieldInfo fieldInfo;
            private readonly float originalValue;

            public ReflectionFieldHandler(object component, System.Reflection.FieldInfo fieldInfo)
            {
                this.component = component;
                this.fieldInfo = fieldInfo;
                this.originalValue = (float)fieldInfo.GetValue(component);
            }

            public void Apply(float ratio)
            {
                if (component == null) return;
                float newValue = Mathf.Max(originalValue * ratio, 0.01f);
                float currentValue = (float)fieldInfo.GetValue(component);
                if (Mathf.Abs(currentValue - newValue) > 0.001f)
                {
                    fieldInfo.SetValue(component, newValue);
                }
            }

            public void Restore()
            {
                if (component == null) return;
                fieldInfo.SetValue(component, originalValue);
            }
        }

        private readonly struct BoxColliderState
        {
            private readonly BoxCollider collider;
            private readonly Vector3 size;
            private readonly Vector3 center;

            public BoxColliderState(BoxCollider collider)
            {
                this.collider = collider;
                size = collider.size;
                center = collider.center;
            }

            public void Apply(float ratio)
            {
                if (collider == null) return;
                Vector3 newSize = size;
                newSize.z = Mathf.Max(size.z * ratio, 0.01f);
                if (collider.size != newSize) collider.size = newSize;
                collider.center = center;
            }

            public void Restore()
            {
                if (collider == null) return;
                collider.size = size;
                collider.center = center;
            }
        }

        private class CapsuleHandler
        {
            private readonly CapsuleCollider capsule;
            private readonly float radius;
            private readonly float height;
            private BoxCollider? box;
            private readonly Vector3 originalCenter;
            private readonly int direction;
            
            private Transform? originalParent;
            private Vector3 originalLocalPos;
            private Quaternion originalLocalRot;
            private readonly bool isDamageReceiver;
            private readonly int originalLayer;

            public CapsuleHandler(CapsuleCollider capsule, Transform? damageReceiver)
            {
                this.capsule = capsule;
                radius = capsule.radius;
                height = capsule.height;
                originalCenter = capsule.center;
                direction = capsule.direction;
                originalLayer = capsule.gameObject.layer;
                
                isDamageReceiver = (damageReceiver != null && capsule.transform == damageReceiver) || 
                                   (capsule.gameObject.name == "DamageReceiver");
            }

            public void Apply(float ratio, Transform? targetModel)
            {
                if (capsule == null) return;
                
                if (isDamageReceiver)
                {
                    ApplyToDamageReceiver(ratio, targetModel);
                }
                else
                {
                    ApplyToRootProxy(ratio, targetModel);
                }
            }

            private void ApplyToRootProxy(float ratio, Transform? targetModel)
            {
                if (!capsule.enabled) capsule.enabled = true;
                capsule.radius = 0.05f; 

                if (targetModel == null) return;

                if (box == null)
                {
                    GameObject proxyObj = new GameObject("StringificationRootHitbox");
                    proxyObj.transform.SetParent(targetModel, false);
                    proxyObj.transform.localPosition = Vector3.zero;
                    proxyObj.transform.localRotation = Quaternion.identity;
                    
                    if (targetModel.GetComponent<Rigidbody>() != null)
                    {
                        Debug.LogWarning($"[Stringification] TargetModel {targetModel.name} has a Rigidbody! This might interfere with the Root Rigidbody.");
                    }

                    box = proxyObj.AddComponent<BoxCollider>();
                    box.isTrigger = true; 
                    if (capsule.sharedMaterial != null) box.sharedMaterial = capsule.sharedMaterial;
                    proxyObj.layer = originalLayer;
                    
                    UpdateBoxDimensions(box, ratio, targetModel);
                    Debug.Log($"[Stringification] Created Root Proxy on {targetModel.name}");
                }
                else
                {
                    if (box != null) UpdateBoxDimensions(box, ratio, targetModel);
                }
                
                if (box != null && !box.enabled) box.enabled = true;
            }

            private void ApplyToDamageReceiver(float ratio, Transform? targetModel)
            {
                if (targetModel == null) return;

                if (capsule.transform.parent != targetModel)
                {
                    originalParent = capsule.transform.parent;
                    originalLocalPos = capsule.transform.localPosition;
                    originalLocalRot = capsule.transform.localRotation;
                    
                    capsule.transform.SetParent(targetModel, true);
                    capsule.transform.localRotation = Quaternion.identity;
                    capsule.transform.localPosition = Vector3.zero; 
                }

                if (capsule.enabled) capsule.enabled = false;

                if (box == null)
                {
                    box = capsule.gameObject.AddComponent<BoxCollider>();
                    box.isTrigger = true; 
                    if (capsule.sharedMaterial != null) box.sharedMaterial = capsule.sharedMaterial;
                    
                    // Ensure layer collides with environment (use Root's layer if possible, or keep original)
                    // For DamageReceiver, we usually want it to be hit by bullets, so keep original layer?
                    // Or if we want it to collide with walls?
                    // Previous logic changed it to root layer. Let's keep that if we have access to root.
                    if (capsule.transform.root != null)
                    {
                        capsule.gameObject.layer = capsule.transform.root.gameObject.layer;
                    }
                    
                    UpdateBoxDimensions(box, ratio, targetModel); // targetModel is parent now
                    Debug.Log($"[Stringification] Created Trigger BoxCollider on DamageReceiver");
                }
                else
                {
                     UpdateBoxDimensions(box, ratio, targetModel);
                }
                
                if (!box.enabled) box.enabled = true;
                box.isTrigger = true;
            }

            private void UpdateBoxDimensions(BoxCollider box, float ratio, Transform targetModel)
            {
                Vector3 parentScale = targetModel.lossyScale;
                float sx = Mathf.Abs(parentScale.x) > 0.001f ? parentScale.x : 1f;
                float sy = Mathf.Abs(parentScale.y) > 0.001f ? parentScale.y : 1f;
                float sz = Mathf.Abs(parentScale.z) > 0.001f ? parentScale.z : 1f;
                
                float width = radius * 2;
                float length = height;
                float thickness = width * ratio;
                
                Vector3 worldSize;
                if (direction == 1) worldSize = new Vector3(width, length, thickness);
                else if (direction == 0) worldSize = new Vector3(length, width, thickness);
                else worldSize = new Vector3(width, width, thickness);
                
                box.size = new Vector3(worldSize.x / sx, worldSize.y / sy, worldSize.z / sz);
                
                // Center compensation only needed if proxy is separate object (RootProxy)
                // For DamageReceiver (same object), center is just originalCenter?
                // But DamageReceiver is reparented to TargetModel and zeroed out.
                // So box.center should be relative to TargetModel.
                // originalCenter was relative to original parent (Root).
                // If we want the box to be centered on the model, we should probably use Vector3.zero or adjust.
                // For RootProxy, we calculated center based on originalCenter.
                // Let's use the same logic for consistency if we assume originalCenter was "correct" relative to the model's pivot.
                box.center = new Vector3(originalCenter.x / sx, originalCenter.y / sy, originalCenter.z / sz);
            }

            public void Restore()
            {
                if (capsule == null) return;

                if (isDamageReceiver)
                {
                    if (originalParent != null && capsule.transform.parent != originalParent)
                    {
                        capsule.transform.SetParent(originalParent, true);
                        capsule.transform.localPosition = originalLocalPos;
                        capsule.transform.localRotation = originalLocalRot;
                    }

                    capsule.gameObject.layer = originalLayer;

                    if (box != null)
                    {
                        Object.Destroy(box);
                        box = null;
                    }

                    capsule.enabled = true;
                    capsule.radius = radius;
                }
                else
                {
                    capsule.radius = radius;
                    capsule.enabled = true;
                    
                    if (box != null)
                    {
                        Object.Destroy(box.gameObject);
                        box = null;
                    }
                }
            }

            public void ResolveCollision(Transform rootTransform)
            {
                if (isDamageReceiver || box == null || !box.enabled) return;

                int rootLayer = rootTransform.gameObject.layer;
                int layerMask = ~(1 << rootLayer | 1 << 2); 

                Vector3 worldCenter = box.transform.TransformPoint(box.center);
                Vector3 worldHalfExtents = Vector3.Scale(box.size, box.transform.lossyScale) * 0.5f;
                Quaternion worldRotation = box.transform.rotation;

                Collider[] hits = Physics.OverlapBox(worldCenter, worldHalfExtents, worldRotation, layerMask, QueryTriggerInteraction.Ignore);

                foreach (var hit in hits)
                {
                    if (hit.transform.root == rootTransform) continue;
                    if (hit.isTrigger) continue;

                    if (ShouldIgnoreCollision(hit)) continue;

                    Vector3 direction;
                    float distance;
                    
                    if (Physics.ComputePenetration(
                        box, box.transform.position, box.transform.rotation,
                        hit, hit.transform.position, hit.transform.rotation,
                        out direction, out distance))
                    {
                        Vector3 separation = direction * distance;
                        Vector3 horizSeparation = Vector3.ProjectOnPlane(separation, Vector3.up);
                        
                        if (horizSeparation.sqrMagnitude > 0.000001f)
                        {
                            rootTransform.position += horizSeparation;
                        }
                    }
                }
            }

            private bool ShouldIgnoreCollision(Collider hit)
            {
                if (hit.name.Contains("GoVolume") || hit.name.Contains("SpecialAttachment")) return true;
                
                Transform t = hit.transform;
                while (t != null)
                {
                    if (t.name.Contains("Character") || t.name.Contains("AIController")) return true;
                    if (t.GetComponent<CharacterController>() != null) return true;
                    t = t.parent;
                }
                return false;
            }

            public void Cleanup()
            {
                if (box != null)
                {
                    if (isDamageReceiver) Object.Destroy(box);
                    else Object.Destroy(box.gameObject);
                    box = null;
                }
                
                if (isDamageReceiver && originalParent != null && capsule != null && capsule.transform.parent != originalParent)
                {
                     capsule.transform.SetParent(originalParent, true);
                     capsule.transform.localPosition = originalLocalPos;
                     capsule.transform.localRotation = originalLocalRot;
                }
            }
        }

        private readonly struct SphereColliderState
        {
            private readonly SphereCollider collider;
            private readonly float radius;
            private readonly Vector3 center;

            public SphereColliderState(SphereCollider collider)
            {
                this.collider = collider;
                radius = collider.radius;
                center = collider.center;
            }

            public void Apply(float ratio)
            {
                if (collider == null) return;
                float newRadius = Mathf.Max(radius * ratio, 0.01f);
                if (Mathf.Abs(collider.radius - newRadius) > 0.001f)
                {
                    collider.radius = newRadius;
                }
                collider.center = center;
            }

            public void Restore()
            {
                if (collider == null) return;
                collider.radius = radius;
                collider.center = center;
            }
        }

        private sealed class CharacterControllerState
        {
            private readonly CharacterController controller;
            private readonly float height;
            private readonly float radius;
            private readonly Vector3 center;

            public CharacterControllerState(CharacterController controller)
            {
                this.controller = controller;
                height = controller.height;
                radius = controller.radius;
                center = controller.center;
            }

            public void Apply(float ratio)
            {
                controller.radius = Mathf.Max(radius * ratio, 0.05f);
                controller.height = height;
                controller.center = center;
            }

            public void Restore()
            {
                controller.radius = radius;
                controller.height = height;
                controller.center = center;
            }
        }
    }
}
