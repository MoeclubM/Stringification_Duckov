using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Duckov.Modding;

namespace Stringification
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        #region Configuration Constants
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
        
        private const string KEY_OBSTACLE_CHECK_DIST = "ObstacleCheckDist";
        private const string KEY_VISUAL_LERP_SPEED = "VisualLerpSpeed";
        private const string KEY_STRINGIFIED_THICKNESS = "StringifiedThickness";
        private const string KEY_MIN_JUMP_TIME = "MinJumpTime";

        private const KeyCode DEFAULT_TOGGLE_KEY = KeyCode.LeftControl;
        private const KeyCode DEFAULT_JUMP_KEY = KeyCode.X;
        private const float DEFAULT_SINGLE_JUMP_HEIGHT = 1.2f;
        private const float DEFAULT_DOUBLE_JUMP_HEIGHT = 0.8f;
        private const float DEFAULT_JUMP_GRAVITY = 40.0f;
        private const float DEFAULT_FLIGHT_SPEED = 7.5f;
        private const float DEFAULT_DESCENT_RATE = -1.0f;
        private const float DEFAULT_FLIGHT_ACTIVATION_SPEED = 0.5f;
        private const float DEFAULT_FLIGHT_PITCH = 65.0f;
        private const float DEFAULT_STRINGIFICATION_ROTATION = 90.0f;
        
        private const float DEFAULT_OBSTACLE_CHECK_DIST = 1.0f;
        private const float DEFAULT_VISUAL_LERP_SPEED = 15.0f;
        private const float DEFAULT_STRINGIFIED_THICKNESS = 0.05f;
        private const float DEFAULT_MIN_JUMP_TIME = 0.15f;
        #endregion

        // Components
        private Stringification.Components.StringificationVisuals visuals = new Stringification.Components.StringificationVisuals();
        private Stringification.Components.JumpMechanics jump = new Stringification.Components.JumpMechanics();
        private Stringification.Components.FlightMechanics flight = new Stringification.Components.FlightMechanics();
        private Stringification.Components.PlayerManager playerManager = new Stringification.Components.PlayerManager();
        private Stringification.Components.InputManager inputManager = new Stringification.Components.InputManager();
        private Stringification.Components.ColliderManager colliderManager = new Stringification.Components.ColliderManager();

        // State
        private bool isStringified = false;

        private ModInfo modInfo = new ModInfo 
        { 
            name = "Stringification",
            displayName = "Stringification (弦化)",
            description = "Stringificate in Duckorv like Strinova",
        };

        public void Start()
        {
            Debug.Log("Stringification Loaded!");
            InitializeConfig();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            playerManager.Reset();
            inputManager.Reset();
            isStringified = false;
            flight.StopFlight();
            visuals.SetTarget(null, null, null);
            colliderManager.Reset();
        }

        private void InitializeConfig()
        {
            if (ModSettingApiWrapper.Init(modInfo))
            {
                // Keybindings
                ModSettingApiWrapper.AddKeybinding(KEY_TOGGLE, "弦化开关 (Toggle Stringification)", DEFAULT_TOGGLE_KEY, DEFAULT_TOGGLE_KEY, (val) => inputManager.ToggleKey = val);
                if (ModSettingApiWrapper.GetSavedValue<KeyCode>(KEY_TOGGLE, out KeyCode savedToggle)) inputManager.ToggleKey = savedToggle;

                ModSettingApiWrapper.AddKeybinding(KEY_JUMP, "飞行/跳跃键 (Jump/Flight)", DEFAULT_JUMP_KEY, DEFAULT_JUMP_KEY, (val) => inputManager.JumpKey = val);
                if (ModSettingApiWrapper.GetSavedValue<KeyCode>(KEY_JUMP, out KeyCode savedJump)) inputManager.JumpKey = savedJump;

                // Float settings
                ModSettingApiWrapper.AddSlider(KEY_SINGLE_JUMP_HEIGHT, "一段跳高度 (Single Jump Height)", DEFAULT_SINGLE_JUMP_HEIGHT, 0.5f, 2.0f, (val) => jump.SingleJumpHeight = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_SINGLE_JUMP_HEIGHT, out float savedSingle)) jump.SingleJumpHeight = savedSingle;

                ModSettingApiWrapper.AddSlider(KEY_DOUBLE_JUMP_HEIGHT, "二段跳高度 (Double Jump Height)", DEFAULT_DOUBLE_JUMP_HEIGHT, 0.5f, 2.0f, (val) => jump.DoubleJumpHeight = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_DOUBLE_JUMP_HEIGHT, out float savedDouble)) jump.DoubleJumpHeight = savedDouble;

                ModSettingApiWrapper.AddSlider(KEY_JUMP_GRAVITY, "跳跃重力 (Jump Gravity)", DEFAULT_JUMP_GRAVITY, 10.0f, 100.0f, (val) => jump.Gravity = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_JUMP_GRAVITY, out float savedGravity)) jump.Gravity = savedGravity;

                ModSettingApiWrapper.AddSlider(KEY_DESCENT_RATE, "滑翔下落速度 (Descent Rate)", DEFAULT_DESCENT_RATE, -5.0f, 0f, (val) => flight.DescentRate = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_DESCENT_RATE, out float savedDescent)) flight.DescentRate = savedDescent;

                ModSettingApiWrapper.AddSlider(KEY_FLIGHT_SPEED, "滑翔速度 (Flight Speed)", DEFAULT_FLIGHT_SPEED, 1.0f, 20.0f, (val) => flight.FlightSpeed = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_FLIGHT_SPEED, out float savedSpeed)) flight.FlightSpeed = savedSpeed;

                ModSettingApiWrapper.AddSlider(KEY_FLIGHT_ACTIVATION_SPEED, "自动飞行触发速度 (Auto-Flight Speed Threshold)", DEFAULT_FLIGHT_ACTIVATION_SPEED, 0.0f, 10.0f, (val) => inputManager.FlightActivationSpeed = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_FLIGHT_ACTIVATION_SPEED, out float savedActSpeed)) inputManager.FlightActivationSpeed = savedActSpeed;

                ModSettingApiWrapper.AddSlider(KEY_FLIGHT_PITCH, "飞行俯角 (Flight Pitch)", DEFAULT_FLIGHT_PITCH, 0.0f, 90.0f, (val) => flight.FlightPitch = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_FLIGHT_PITCH, out float savedPitch)) flight.FlightPitch = savedPitch;

                ModSettingApiWrapper.AddSlider(KEY_STRINGIFICATION_ROTATION, "弦化旋转角度 (Stringification Rotation)", DEFAULT_STRINGIFICATION_ROTATION, -180.0f, 180.0f, (val) => visuals.VisualRotationAngle = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_STRINGIFICATION_ROTATION, out float savedRotation)) visuals.VisualRotationAngle = savedRotation;

                // Advanced Settings
                ModSettingApiWrapper.AddSlider(KEY_OBSTACLE_CHECK_DIST, "障碍物检测距离 (Obstacle Check Dist)", DEFAULT_OBSTACLE_CHECK_DIST, 0.1f, 5.0f, (val) => flight.ObstacleCheckDistance = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_OBSTACLE_CHECK_DIST, out float savedObsDist)) flight.ObstacleCheckDistance = savedObsDist;

                ModSettingApiWrapper.AddSlider(KEY_VISUAL_LERP_SPEED, "变换速度 (Lerp Speed)", DEFAULT_VISUAL_LERP_SPEED, 1.0f, 50.0f, (val) => visuals.LerpSpeed = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_VISUAL_LERP_SPEED, out float savedLerp)) visuals.LerpSpeed = savedLerp;

                ModSettingApiWrapper.AddSlider(KEY_STRINGIFIED_THICKNESS, "纸片厚度 (Paper Thickness)", DEFAULT_STRINGIFIED_THICKNESS, 0.01f, 0.5f, (val) => visuals.StringifiedThickness = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_STRINGIFIED_THICKNESS, out float savedThickness)) visuals.StringifiedThickness = savedThickness;

                ModSettingApiWrapper.AddSlider(KEY_MIN_JUMP_TIME, "最小跳跃时间 (Min Jump Time)", DEFAULT_MIN_JUMP_TIME, 0.05f, 1.0f, (val) => jump.MinJumpTime = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_MIN_JUMP_TIME, out float savedMinJump)) jump.MinJumpTime = savedMinJump;
            }
        }

        public void Update()
        {
            if (!playerManager.UpdatePlayerReference())
            {
                colliderManager.Reset();
                return;
            }
            
            visuals.SetTarget(playerManager.TargetModel, playerManager.DamageReceiver, playerManager.PlayerRigidbody);
            colliderManager.SyncPlayer(playerManager.PlayerObject, playerManager.TargetModel, playerManager.DamageReceiver);

            bool grounded = IsGrounded(); 

            // 1. Toggle Input
            if (inputManager.CheckToggleInput())
            {
                if (grounded)
                {
                    // Grounded: Toggle with rotation
                    if (!isStringified)
                    {
                        SetStringificationState(true, true);
                        Debug.Log("Stringification: Activated (Grounded)");
                    }                
                    else
                    {
                        SetStringificationState(false);
                        Debug.Log("Stringification: Deactivated");
                    }
                }
                else
                {
                    // Airborne: Toggle without rotation (Flight handles pitch)
                    if (!isStringified)
                    {
                        SetStringificationState(true, false);
                        TryActivateFlight();
                        Debug.Log("Stringification: Activated (Airborne)");
                    }
                    else
                    {
                        SetStringificationState(false);
                        Debug.Log("Stringification: Deactivated (Airborne)");
                    }
                }
            }

            // Reset double jump state if grounded
            if (IsGrounded())
            {
                if (playerManager.PlayerControl != null) playerManager.PlayerControl.enabled = true;
                inputManager.ResetDoubleJump();
            }

            // 3. Jump/Flight Input
            if (inputManager.CheckJumpInput())
            {
                if (isStringified)
                {
                    // Jump key during stringification/flight cancels the state
                    SetStringificationState(false);
                    Debug.Log("Stringification: Cancelled via jump input");
                }
                
                HandleJumpInput();
            }
        }

        private void SetStringificationState(bool active, bool rotate = false)
        {
            isStringified = active;
            visuals.SetStringified(active);
            
            // Initial apply with identity rotation, LateUpdate will handle the rest
            colliderManager.ApplyStringification(active, visuals.StringifiedThickness, Quaternion.identity, playerManager.TargetModel);
            
            if (!active)
            {
                visuals.SetRotation(false);
                if (flight.IsFlying) flight.StopFlight();
            }
            else if (!flight.IsFlying)
            {
                visuals.SetRotation(rotate);
            }
        }

        private void HandleJumpInput()
        {
            // Simply check state: Grounded -> Single Jump, Airborne -> Double Jump
            if (IsGrounded())
            {
                jump.PerformSingleJump(this, playerManager.PlayerObject, playerManager.PlayerRigidbody);
            }
            else if (inputManager.CanDoubleJump(IsGrounded()))
            {
                jump.PerformDoubleJump(this, playerManager.PlayerObject, playerManager.PlayerRigidbody);
            }
        }

        private bool TryActivateFlight()
        {
            if (playerManager.PlayerRigidbody == null) return false;
            
            if (inputManager.ShouldActivateFlight(playerManager.PlayerRigidbody))
            {
                jump.StopJump(this);
                if (playerManager.PlayerObject == null) return false;
                flight.StartFlight(playerManager.PlayerObject, playerManager.PlayerRigidbody, playerManager.PlayerControl, playerManager.TargetModel, playerManager.DamageReceiver);
                return true;
            }

            return false;
        }


        private bool IsGrounded()
        {
            if (playerManager.PlayerObject == null) return false;
            return Stringification.Utils.PhysicsUtils.IsGrounded(playerManager.PlayerObject);
        }

        public void LateUpdate()
        {
            bool wasFlying = flight.IsFlying;
            flight.UpdateLogic(playerManager.PlayerObject, playerManager.PlayerRigidbody);

            if (wasFlying && !flight.IsFlying)
            {
                SetStringificationState(false);
            }

            if (flight.IsFlying)
            {
                visuals.SetTargetRotation(Quaternion.Euler(flight.FlightPitch, 0, 0));
            }

            visuals.LateUpdate();

            if (isStringified)
            {
                // Get the current rotation from the model to sync colliders
                Quaternion currentRotation = Quaternion.identity;
                if (playerManager.TargetModel != null)
                {
                    currentRotation = playerManager.TargetModel.localRotation;
                }
                
                Vector3 velocity = Vector3.zero;
                if (playerManager.PlayerRigidbody != null)
                {
                    velocity = playerManager.PlayerRigidbody.velocity;
                }
                
                colliderManager.ApplyStringification(true, visuals.StringifiedThickness, currentRotation, playerManager.TargetModel);
                colliderManager.ResolveCollisions();
            }
        }
    }
}
