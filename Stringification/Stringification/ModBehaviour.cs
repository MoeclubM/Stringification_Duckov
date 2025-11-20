using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Duckov.Modding;

namespace Stringification
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        #region Configuration Constants
        // Keys
        private const string KEY_TOGGLE = "ToggleKey";
        private const string KEY_JUMP = "JumpKey";
        private const string KEY_SINGLE_JUMP_HEIGHT = "SingleJumpHeightInt";
        private const string KEY_DOUBLE_JUMP_HEIGHT = "DoubleJumpHeightInt";
        private const string KEY_JUMP_GRAVITY = "JumpGravity";
        private const string KEY_FLIGHT_SPEED = "FlightSpeedInt";
        private const string KEY_DESCENT_RATE = "DescentRateInt";
        private const string KEY_FLIGHT_ACTIVATION_SPEED = "FlightActivationSpeed";
        private const string KEY_FLIGHT_PITCH = "FlightPitch";
        private const string KEY_STRINGIFICATION_ROTATION = "StringificationRotation";
        
        // Advanced Settings Keys
        private const string KEY_FLIGHT_BASE_SPEED = "FlightBaseSpeed";
        private const string KEY_OBSTACLE_CHECK_DIST = "ObstacleCheckDist";
        private const string KEY_VISUAL_LERP_SPEED = "VisualLerpSpeed";
        private const string KEY_STRINGIFIED_THICKNESS = "StringifiedThickness";
        private const string KEY_MIN_JUMP_TIME = "MinJumpTime";

        // Defaults
        private const KeyCode DEFAULT_TOGGLE_KEY = KeyCode.LeftControl;
        private const KeyCode DEFAULT_JUMP_KEY = KeyCode.X;
        private const float DEFAULT_SINGLE_JUMP_HEIGHT = 1.2f;
        private const float DEFAULT_DOUBLE_JUMP_HEIGHT = 0.8f;
        private const float DEFAULT_JUMP_GRAVITY = 40.0f;
        private const float DEFAULT_FLIGHT_SPEED = 1.5f;
        private const float DEFAULT_DESCENT_RATE = -0.5f;
        private const float DEFAULT_FLIGHT_ACTIVATION_SPEED = 0.5f;
        private const float DEFAULT_FLIGHT_PITCH = 75.0f;
        private const float DEFAULT_STRINGIFICATION_ROTATION = 90.0f;
        
        // Advanced Defaults
        private const float DEFAULT_FLIGHT_BASE_SPEED = 5.0f;
        private const float DEFAULT_OBSTACLE_CHECK_DIST = 1.0f;
        private const float DEFAULT_VISUAL_LERP_SPEED = 15.0f;
        private const float DEFAULT_STRINGIFIED_THICKNESS = 0.05f;
        private const float DEFAULT_MIN_JUMP_TIME = 0.15f;
        #endregion

        // Runtime Configuration Variables
        private KeyCode toggleKey = DEFAULT_TOGGLE_KEY;
        private KeyCode jumpKey = DEFAULT_JUMP_KEY;
        private float flightActivationSpeed = DEFAULT_FLIGHT_ACTIVATION_SPEED;

        // Components
        // 组件引用
        private Stringification.Components.StringificationVisuals visuals = new Stringification.Components.StringificationVisuals();
        private Stringification.Components.JumpMechanics jump = new Stringification.Components.JumpMechanics();
        private Stringification.Components.FlightMechanics flight = new Stringification.Components.FlightMechanics();

        // State
        // 状态变量
        private bool isStringified = false;
        private float lastJumpKeyPressTime = 0f;
        private float nextPlayerSearchTime = 0f;

        // References
        // 游戏对象引用
        private GameObject? playerObject;
        private CharacterMainControl? playerControl;
        private Transform? targetModel;
        private Rigidbody? playerRigidbody;

        private ModInfo modInfo = new ModInfo 
        { 
            name = "Stringification",
            displayName = "Stringification (弦化)",
            description = "Stringificate in Duckorv like Strinova",
        };

        public void Start()
        {
            Debug.Log("Stringification Mod Loaded!");
            InitializeConfig();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            playerObject = null;
            playerControl = null;
            targetModel = null;
            playerRigidbody = null;
            isStringified = false;
            flight.StopFlight();
            visuals.SetTarget(null, null, null);
        }

        private void InitializeConfig()
        {
            if (ModSettingApiWrapper.Init(modInfo))
            {
                // Keybindings
                // 按键绑定
                ModSettingApiWrapper.AddKeybinding(KEY_TOGGLE, "弦化开关 (Toggle Stringification)", DEFAULT_TOGGLE_KEY, DEFAULT_TOGGLE_KEY, (val) => toggleKey = val);
                if (ModSettingApiWrapper.GetSavedValue<KeyCode>(KEY_TOGGLE, out KeyCode savedToggle)) toggleKey = savedToggle;

                ModSettingApiWrapper.AddKeybinding(KEY_JUMP, "飞行/跳跃键 (Jump/Flight)", DEFAULT_JUMP_KEY, DEFAULT_JUMP_KEY, (val) => jumpKey = val);
                if (ModSettingApiWrapper.GetSavedValue<KeyCode>(KEY_JUMP, out KeyCode savedJump)) jumpKey = savedJump;

                // Float settings
                // 浮点数设置
                ModSettingApiWrapper.AddSlider(KEY_SINGLE_JUMP_HEIGHT, "一段跳高度 (Single Jump Height)", DEFAULT_SINGLE_JUMP_HEIGHT, 0.5f, 2.0f, (val) => jump.SingleJumpHeight = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_SINGLE_JUMP_HEIGHT, out float savedSingle)) jump.SingleJumpHeight = savedSingle;

                ModSettingApiWrapper.AddSlider(KEY_DOUBLE_JUMP_HEIGHT, "二段跳高度 (Double Jump Height)", DEFAULT_DOUBLE_JUMP_HEIGHT, 0.5f, 2.0f, (val) => jump.DoubleJumpHeight = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_DOUBLE_JUMP_HEIGHT, out float savedDouble)) jump.DoubleJumpHeight = savedDouble;

                ModSettingApiWrapper.AddSlider(KEY_JUMP_GRAVITY, "跳跃重力 (Jump Gravity)", DEFAULT_JUMP_GRAVITY, 10.0f, 100.0f, (val) => jump.Gravity = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_JUMP_GRAVITY, out float savedGravity)) jump.Gravity = savedGravity;

                ModSettingApiWrapper.AddSlider(KEY_DESCENT_RATE, "滑翔下落速度 (Descent Rate)", DEFAULT_DESCENT_RATE, -5.0f, 5.0f, (val) => flight.DescentRate = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_DESCENT_RATE, out float savedDescent)) flight.DescentRate = savedDescent;

                ModSettingApiWrapper.AddSlider(KEY_FLIGHT_SPEED, "滑翔速度倍率 (Flight Speed Multiplier)", DEFAULT_FLIGHT_SPEED, 0.5f, 10.0f, (val) => flight.SpeedMult = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_FLIGHT_SPEED, out float savedSpeed)) flight.SpeedMult = savedSpeed;

                ModSettingApiWrapper.AddSlider(KEY_FLIGHT_ACTIVATION_SPEED, "自动飞行触发速度 (Auto-Flight Speed Threshold)", DEFAULT_FLIGHT_ACTIVATION_SPEED, 0.0f, 10.0f, (val) => flightActivationSpeed = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_FLIGHT_ACTIVATION_SPEED, out float savedActSpeed)) flightActivationSpeed = savedActSpeed;

                ModSettingApiWrapper.AddSlider(KEY_FLIGHT_PITCH, "飞行倾角 (Flight Pitch)", DEFAULT_FLIGHT_PITCH, 0.0f, 90.0f, (val) => visuals.FlightPitch = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_FLIGHT_PITCH, out float savedPitch)) visuals.FlightPitch = savedPitch;

                ModSettingApiWrapper.AddSlider(KEY_STRINGIFICATION_ROTATION, "弦化旋转角度 (Stringification Rotation)", DEFAULT_STRINGIFICATION_ROTATION, 0.0f, 180.0f, (val) => visuals.VisualRotationAngle = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_STRINGIFICATION_ROTATION, out float savedRotation)) visuals.VisualRotationAngle = savedRotation;

                // Advanced Settings
                ModSettingApiWrapper.AddSlider(KEY_FLIGHT_BASE_SPEED, "基础飞行速度 (Base Flight Speed)", DEFAULT_FLIGHT_BASE_SPEED, 1.0f, 20.0f, (val) => flight.BaseSpeed = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_FLIGHT_BASE_SPEED, out float savedBaseSpeed)) flight.BaseSpeed = savedBaseSpeed;

                ModSettingApiWrapper.AddSlider(KEY_OBSTACLE_CHECK_DIST, "障碍物检测距离 (Obstacle Check Dist)", DEFAULT_OBSTACLE_CHECK_DIST, 0.1f, 5.0f, (val) => flight.ObstacleCheckDistance = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_OBSTACLE_CHECK_DIST, out float savedObsDist)) flight.ObstacleCheckDistance = savedObsDist;

                ModSettingApiWrapper.AddSlider(KEY_VISUAL_LERP_SPEED, "视觉变换速度 (Visual Lerp Speed)", DEFAULT_VISUAL_LERP_SPEED, 1.0f, 50.0f, (val) => visuals.LerpSpeed = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_VISUAL_LERP_SPEED, out float savedLerp)) visuals.LerpSpeed = savedLerp;

                ModSettingApiWrapper.AddSlider(KEY_STRINGIFIED_THICKNESS, "纸片厚度 (Paper Thickness)", DEFAULT_STRINGIFIED_THICKNESS, 0.01f, 0.5f, (val) => visuals.StringifiedThickness = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_STRINGIFIED_THICKNESS, out float savedThickness)) visuals.StringifiedThickness = savedThickness;

                ModSettingApiWrapper.AddSlider(KEY_MIN_JUMP_TIME, "最小跳跃时间 (Min Jump Time)", DEFAULT_MIN_JUMP_TIME, 0.05f, 1.0f, (val) => jump.MinJumpTime = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_MIN_JUMP_TIME, out float savedMinJump)) jump.MinJumpTime = savedMinJump;
            }
        }

        public void Update()
        {
            // 1. Toggle Input
            // 1. 切换弦化状态输入
            if (Input.GetKeyDown(toggleKey))
            {
                bool groundedForToggle = playerObject == null || IsGrounded();
                if (!isStringified)
                {
                    isStringified = true;
                    visuals.SetStringified(true, groundedForToggle);
                    Debug.Log("Stringification: Activated");
                    UpdatePlayerReference();

                    if (!groundedForToggle)
                    {
                        TryStartFlightIfMoving();
                    }
                }
                else
                {
                    if (!groundedForToggle)
                    {
                        // In-air toggle: switch between flight and normal stringification
                        if (flight.IsFlying)
                        {
                            flight.StopFlight();
                            wasFlying = false;
                        }
                        else
                        {
                            TryStartFlightIfMoving();
                        }
                    }
                    else
                    {
                        isStringified = false;
                        visuals.SetStringified(false, true);
                        flight.StopFlight();
                        Debug.Log("Stringification: Deactivated");
                    }
                }
            }

            // Update Visuals State (Flying status)
            // 更新视觉状态（飞行状态）
            visuals.SetFlying(flight.IsFlying);

            // 2. Maintain Reference (Always run this to get model immediately)
            // 2. 维护引用（始终运行以立即获取模型）
            if (playerObject == null || !playerObject.activeInHierarchy || targetModel == null)
            {
                if (Time.time >= nextPlayerSearchTime)
                {
                    UpdatePlayerReference();
                    nextPlayerSearchTime = Time.time + 1.0f;
                }
            }

            // Reset double jump state if grounded
            // 如果着地，重置二段跳状态
            if (IsGrounded())
            {
                hasDoubleJumped = false;
            }

            // 3. Jump/Flight Input
            // 3. 跳跃/飞行输入
            if (Input.GetKeyDown(jumpKey))
            {
                if (isStringified || flight.IsFlying)
                {
                    // Jump key during stringification cancels the state
                    // 弦化或飞行期间按跳跃键将恢复正常状态
                    if (flight.IsFlying)
                    {
                        flight.StopFlight();
                        wasFlying = false;
                    }
                    if (isStringified)
                    {
                        isStringified = false;
                        visuals.SetStringified(false, true);
                        Debug.Log("Stringification: Cancelled via jump input");
                    }
                    lastJumpKeyPressTime = Time.time;
                    return;
                }

                if (Time.time - lastJumpKeyPressTime < 0.3f) // Double tap
                {
                    TryHandleJumpAction();
                }
                else
                {
                    // Single Tap
                    // 单击
                    if (IsGrounded())
                    {
                        jump.PerformSingleJump(this, playerObject, playerRigidbody);
                    }
                    else if (!hasDoubleJumped)
                    {
                        // Allow double jump on single tap if in air (as requested)
                        // 如果在空中，允许单击触发二段跳（按要求）
                        jump.PerformDoubleJump(this, playerObject, playerRigidbody);
                        hasDoubleJumped = true;
                    }
                }
                lastJumpKeyPressTime = Time.time;
            }

            if (!isStringified)
            {
                // Even if not stringified, we might need to run LateUpdate for transition out
                // But visuals.LateUpdate handles that if targetModel is set
                // 即使未弦化，我们也可能需要运行 LateUpdate 进行过渡
                // 但如果设置了 targetModel，visuals.LateUpdate 会处理它
                return;
            }
        }

        private bool hasDoubleJumped = false;

        private bool TryStartFlightIfMoving()
        {
            if (playerObject == null || playerRigidbody == null) return false;

            bool isMoving = false;

            Vector3 horizVel = new Vector3(playerRigidbody.velocity.x, 0, playerRigidbody.velocity.z);
            if (horizVel.magnitude > flightActivationSpeed)
            {
                isMoving = true;
            }

            if (!isMoving)
            {
                float h = Input.GetAxis("Horizontal");
                float v = Input.GetAxis("Vertical");
                if (new Vector2(h, v).magnitude > 0.1f) isMoving = true;
            }

            if (isMoving)
            {
                jump.StopJump(this);
                flight.StartFlight(playerObject, playerRigidbody, playerControl, targetModel);
                return true;
            }

            return false;
        }

        private void TryHandleJumpAction()
        {
            // Flight trigger moved to Toggle logic as requested.
            // Double tap now only triggers double jump if not grounded.
            // 飞行触发已按要求移至切换逻辑。
            // 双击现在仅在未着地时触发二段跳。
            
            if (!IsGrounded() && !hasDoubleJumped)
            {
                jump.PerformDoubleJump(this, playerObject, playerRigidbody);
                hasDoubleJumped = true;
            }
        }

        private bool IsGrounded()
        {
            if (playerObject == null) return false;
            return Stringification.Utils.PhysicsUtils.IsGrounded(playerObject);
        }

        public void LateUpdate()
        {
            // Always run visuals update to handle transitions
            visuals.LateUpdate();

            // Run flight physics in LateUpdate to override any Kinematic/CharacterController snapping
            // 在 LateUpdate 中运行飞行物理，以覆盖任何 Kinematic/CharacterController 捕捉
            flight.UpdateLogic(playerObject, playerRigidbody);
            
            // Check if flight just ended to disable stringification
            // 检查飞行是否刚刚结束以禁用弦化
            if (wasFlying && !flight.IsFlying)
            {
                isStringified = false;
                visuals.SetStringified(false, true);
                Debug.Log("Stringification: Flight Landed -> Unstringified");
            }
            wasFlying = flight.IsFlying;
        }

        private bool wasFlying = false;

        private void UpdatePlayerReference()
        {
            if (playerObject != null && targetModel != null) return;

            // Find Player
            playerObject = null;
            playerControl = null;
            var characterControls = FindObjectsOfType<CharacterMainControl>();
            foreach (var cc in characterControls)
            {
                if (IsValidPlayer(cc))
                {
                    playerObject = cc.gameObject;
                    playerControl = cc;
                    break;
                }
            }

            // Find Model
            if (playerObject != null)
            {
                playerRigidbody = playerObject.GetComponent<Rigidbody>();
                
                Transform modelRoot = playerObject.transform.Find("ModelRoot");
                if (modelRoot != null)
                {
                    // Find the actual mesh container under ModelRoot (skipping HiderPoints)
                    foreach (Transform child in modelRoot)
                    {
                        if (!child.name.Contains("HiderPoints"))
                        {
                            targetModel = child;
                            break;
                        }
                    }
                    // Fallback to ModelRoot itself if no suitable child found
                    if (targetModel == null) targetModel = modelRoot;
                }

                // Find DamageReceiver
                Transform damageReceiver = playerObject.transform.Find("DamageReceiver");

                if (targetModel != null)
                {
                    Debug.Log($"Stringification: Found Target '{targetModel.name}' on Player '{playerObject.name}'");
                    if (damageReceiver != null) Debug.Log($"Stringification: Found DamageReceiver on Player '{playerObject.name}'");
                    
                    // Update components
                    visuals.SetTarget(targetModel, damageReceiver, playerRigidbody);
                }
            }
        }

        private bool IsValidPlayer(CharacterMainControl cc)
        {
            // 1. Root Name Check
            if (cc.name.Contains("Dummy") || cc.name.Contains("AI")) return false;

            // 2. Deep Check for AI indicators
            // GetComponentsInChildren includes the parent and all children
            foreach (var comp in cc.GetComponentsInChildren<MonoBehaviour>())
            {
                // Check Component Type
                if (comp.GetType().Name.Contains("AIController")) return false;

                // Check GameObject Name (e.g. "DummyAIController(Clone)")
                if (comp.gameObject.name.Contains("DummyAIController") || comp.gameObject.name.Contains("PetAIController")) return false;
            }

            return true;
        }
    }
}
