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
        private const string KEY_ENABLE_FLIGHT_STEERING = "EnableFlightSteering";
        private const string KEY_FLIGHT_STEERING_SPEED = "FlightSteeringSpeed";
        private const string KEY_STRINGIFICATION_ROTATION = "StringificationRotation";
        private const string KEY_ALLOW_FIRING = "AllowFiring";
        
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
        private const float DEFAULT_DESCENT_RATE = -0.8f;
        private const float DEFAULT_FLIGHT_ACTIVATION_SPEED = 0.5f;
        private const float DEFAULT_FLIGHT_PITCH = 65.0f;
        private const bool DEFAULT_ENABLE_FLIGHT_STEERING = true;
        private const float DEFAULT_FLIGHT_STEERING_SPEED = 65.0f;
        private const float DEFAULT_STRINGIFICATION_ROTATION = 90.0f;
        private const bool DEFAULT_ALLOW_FIRING = false;
        
        private const float DEFAULT_OBSTACLE_CHECK_DIST = 1.5f;
        private const float DEFAULT_VISUAL_LERP_SPEED = 15.0f;
        private const float DEFAULT_STRINGIFIED_THICKNESS = 0.1f;
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
        private bool allowFiring = DEFAULT_ALLOW_FIRING;

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
                // 1. Keybindings
                ModSettingApiWrapper.AddKeybinding(KEY_TOGGLE, "弦化开关 (Toggle Stringification)", DEFAULT_TOGGLE_KEY, DEFAULT_TOGGLE_KEY, (val) => inputManager.ToggleKey = val);
                if (ModSettingApiWrapper.GetSavedValue<KeyCode>(KEY_TOGGLE, out KeyCode savedToggle)) inputManager.ToggleKey = savedToggle;

                ModSettingApiWrapper.AddKeybinding(KEY_JUMP, "飞行/跳跃键 (Jump/Flight)", DEFAULT_JUMP_KEY, DEFAULT_JUMP_KEY, (val) => inputManager.JumpKey = val);
                if (ModSettingApiWrapper.GetSavedValue<KeyCode>(KEY_JUMP, out KeyCode savedJump)) inputManager.JumpKey = savedJump;

                // 2. Core Mechanics (Stringification)
                ModSettingApiWrapper.AddSlider(KEY_STRINGIFICATION_ROTATION, "弦化旋转角度 (Stringification Rotation)", DEFAULT_STRINGIFICATION_ROTATION, -180.0f, 180.0f, (val) => visuals.VisualRotationAngle = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_STRINGIFICATION_ROTATION, out float savedRotation)) visuals.VisualRotationAngle = savedRotation;

                ModSettingApiWrapper.AddSlider(KEY_STRINGIFIED_THICKNESS, "纸片厚度 (Paper Thickness)", DEFAULT_STRINGIFIED_THICKNESS, 0.01f, 0.5f, (val) => visuals.StringifiedThickness = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_STRINGIFIED_THICKNESS, out float savedThickness)) visuals.StringifiedThickness = savedThickness;

                ModSettingApiWrapper.AddToggle(KEY_ALLOW_FIRING, "弦化时允许开火 (Allow Firing)", DEFAULT_ALLOW_FIRING, (val) => allowFiring = val);
                if (ModSettingApiWrapper.GetSavedValue<bool>(KEY_ALLOW_FIRING, out bool savedAllowFiring)) allowFiring = savedAllowFiring;

                // 3. Flight Settings
                ModSettingApiWrapper.AddSlider(KEY_FLIGHT_SPEED, "滑翔速度 (Flight Speed)", DEFAULT_FLIGHT_SPEED, 1.0f, 20.0f, (val) => flight.FlightSpeed = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_FLIGHT_SPEED, out float savedSpeed)) flight.FlightSpeed = savedSpeed;

                ModSettingApiWrapper.AddSlider(KEY_DESCENT_RATE, "滑翔下落速度 (Descent Rate)", DEFAULT_DESCENT_RATE, -5.0f, 0f, (val) => flight.DescentRate = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_DESCENT_RATE, out float savedDescent)) flight.DescentRate = savedDescent;

                ModSettingApiWrapper.AddSlider(KEY_FLIGHT_PITCH, "飞行俯角 (Flight Pitch)", DEFAULT_FLIGHT_PITCH, 0.0f, 90.0f, (val) => flight.FlightPitch = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_FLIGHT_PITCH, out float savedPitch)) flight.FlightPitch = savedPitch;

                ModSettingApiWrapper.AddToggle(KEY_ENABLE_FLIGHT_STEERING, "允许空中转向 (Enable Flight Steering)", DEFAULT_ENABLE_FLIGHT_STEERING, (val) => flight.EnableSteering = val);
                if (ModSettingApiWrapper.GetSavedValue<bool>(KEY_ENABLE_FLIGHT_STEERING, out bool savedSteering)) flight.EnableSteering = savedSteering;

                ModSettingApiWrapper.AddSlider(KEY_FLIGHT_STEERING_SPEED, "空中转向速度 (Flight Steering Speed)", DEFAULT_FLIGHT_STEERING_SPEED, 10.0f, 180.0f, (val) => flight.SteeringSpeed = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_FLIGHT_STEERING_SPEED, out float savedSteeringSpeed)) flight.SteeringSpeed = savedSteeringSpeed;

                ModSettingApiWrapper.AddSlider(KEY_FLIGHT_ACTIVATION_SPEED, "自动飞行触发速度 (Auto-Flight Speed Threshold)", DEFAULT_FLIGHT_ACTIVATION_SPEED, 0.0f, 10.0f, (val) => inputManager.FlightActivationSpeed = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_FLIGHT_ACTIVATION_SPEED, out float savedActSpeed)) inputManager.FlightActivationSpeed = savedActSpeed;

                // 4. Jump Settings
                ModSettingApiWrapper.AddSlider(KEY_SINGLE_JUMP_HEIGHT, "一段跳高度 (Single Jump Height)", DEFAULT_SINGLE_JUMP_HEIGHT, 0.5f, 2.0f, (val) => jump.SingleJumpHeight = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_SINGLE_JUMP_HEIGHT, out float savedSingle)) jump.SingleJumpHeight = savedSingle;

                ModSettingApiWrapper.AddSlider(KEY_DOUBLE_JUMP_HEIGHT, "二段跳高度 (Double Jump Height)", DEFAULT_DOUBLE_JUMP_HEIGHT, 0.5f, 2.0f, (val) => jump.DoubleJumpHeight = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_DOUBLE_JUMP_HEIGHT, out float savedDouble)) jump.DoubleJumpHeight = savedDouble;

                ModSettingApiWrapper.AddSlider(KEY_JUMP_GRAVITY, "跳跃重力 (Jump Gravity)", DEFAULT_JUMP_GRAVITY, 10.0f, 100.0f, (val) => jump.Gravity = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_JUMP_GRAVITY, out float savedGravity)) jump.Gravity = savedGravity;

                // 5. Advanced / Visuals
                ModSettingApiWrapper.AddSlider(KEY_VISUAL_LERP_SPEED, "变换速度 (Lerp Speed)", DEFAULT_VISUAL_LERP_SPEED, 1.0f, 50.0f, (val) => visuals.LerpSpeed = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_VISUAL_LERP_SPEED, out float savedLerp)) visuals.LerpSpeed = savedLerp;

                ModSettingApiWrapper.AddSlider(KEY_OBSTACLE_CHECK_DIST, "障碍物检测距离 (Obstacle Check Dist)", DEFAULT_OBSTACLE_CHECK_DIST, 0.1f, 5.0f, (val) => flight.ObstacleCheckDistance = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_OBSTACLE_CHECK_DIST, out float savedObsDist)) flight.ObstacleCheckDistance = savedObsDist;

                ModSettingApiWrapper.AddSlider(KEY_MIN_JUMP_TIME, "最小跳跃时间 (Min Jump Time)", DEFAULT_MIN_JUMP_TIME, 0.05f, 1.0f, (val) => jump.MinJumpTime = val);
                if (ModSettingApiWrapper.GetSavedValue<float>(KEY_MIN_JUMP_TIME, out float savedMinJump)) jump.MinJumpTime = savedMinJump;

                ModSettingApiWrapper.AddButton("ResetConfig", "重置设置 (Reset Settings)", "重置 (Reset)", ResetConfig);
            }
        }

        private void ResetConfig()
        {
            // Reset local variables
            inputManager.ToggleKey = DEFAULT_TOGGLE_KEY;
            inputManager.JumpKey = DEFAULT_JUMP_KEY;
            visuals.VisualRotationAngle = DEFAULT_STRINGIFICATION_ROTATION;
            visuals.StringifiedThickness = DEFAULT_STRINGIFIED_THICKNESS;
            allowFiring = DEFAULT_ALLOW_FIRING;
            flight.FlightSpeed = DEFAULT_FLIGHT_SPEED;
            flight.DescentRate = DEFAULT_DESCENT_RATE;
            flight.FlightPitch = DEFAULT_FLIGHT_PITCH;
            flight.EnableSteering = DEFAULT_ENABLE_FLIGHT_STEERING;
            flight.SteeringSpeed = DEFAULT_FLIGHT_STEERING_SPEED;
            inputManager.FlightActivationSpeed = DEFAULT_FLIGHT_ACTIVATION_SPEED;
            jump.SingleJumpHeight = DEFAULT_SINGLE_JUMP_HEIGHT;
            jump.DoubleJumpHeight = DEFAULT_DOUBLE_JUMP_HEIGHT;
            jump.Gravity = DEFAULT_JUMP_GRAVITY;
            visuals.LerpSpeed = DEFAULT_VISUAL_LERP_SPEED;
            flight.ObstacleCheckDistance = DEFAULT_OBSTACLE_CHECK_DIST;
            jump.MinJumpTime = DEFAULT_MIN_JUMP_TIME;

            // Update UI
            ModSettingApiWrapper.SetValue(KEY_TOGGLE, DEFAULT_TOGGLE_KEY);
            ModSettingApiWrapper.SetValue(KEY_JUMP, DEFAULT_JUMP_KEY);
            ModSettingApiWrapper.SetValue(KEY_STRINGIFICATION_ROTATION, DEFAULT_STRINGIFICATION_ROTATION);
            ModSettingApiWrapper.SetValue(KEY_STRINGIFIED_THICKNESS, DEFAULT_STRINGIFIED_THICKNESS);
            ModSettingApiWrapper.SetValue(KEY_ALLOW_FIRING, DEFAULT_ALLOW_FIRING);
            ModSettingApiWrapper.SetValue(KEY_FLIGHT_SPEED, DEFAULT_FLIGHT_SPEED);
            ModSettingApiWrapper.SetValue(KEY_DESCENT_RATE, DEFAULT_DESCENT_RATE);
            ModSettingApiWrapper.SetValue(KEY_FLIGHT_PITCH, DEFAULT_FLIGHT_PITCH);
            ModSettingApiWrapper.SetValue(KEY_ENABLE_FLIGHT_STEERING, DEFAULT_ENABLE_FLIGHT_STEERING);
            ModSettingApiWrapper.SetValue(KEY_FLIGHT_STEERING_SPEED, DEFAULT_FLIGHT_STEERING_SPEED);
            ModSettingApiWrapper.SetValue(KEY_FLIGHT_ACTIVATION_SPEED, DEFAULT_FLIGHT_ACTIVATION_SPEED);
            ModSettingApiWrapper.SetValue(KEY_SINGLE_JUMP_HEIGHT, DEFAULT_SINGLE_JUMP_HEIGHT);
            ModSettingApiWrapper.SetValue(KEY_DOUBLE_JUMP_HEIGHT, DEFAULT_DOUBLE_JUMP_HEIGHT);
            ModSettingApiWrapper.SetValue(KEY_JUMP_GRAVITY, DEFAULT_JUMP_GRAVITY);
            ModSettingApiWrapper.SetValue(KEY_VISUAL_LERP_SPEED, DEFAULT_VISUAL_LERP_SPEED);
            ModSettingApiWrapper.SetValue(KEY_OBSTACLE_CHECK_DIST, DEFAULT_OBSTACLE_CHECK_DIST);
            ModSettingApiWrapper.SetValue(KEY_MIN_JUMP_TIME, DEFAULT_MIN_JUMP_TIME);
            
            Debug.Log("[Stringification] Configuration reset to defaults.");
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

            // 2. Fire Input Check
            if (isStringified && !allowFiring && inputManager.CheckFireInput())
            {
                SetStringificationState(false);
                Debug.Log("Stringification: Cancelled via fire input");
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
            colliderManager.ApplyStringification(active, visuals.StringifiedThickness, playerManager.TargetModel);
            
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
            float horizontalInput = inputManager.GetHorizontalInput();
            flight.UpdateLogic(playerManager.PlayerObject, playerManager.PlayerRigidbody, horizontalInput);

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
                colliderManager.ApplyStringification(true, visuals.StringifiedThickness, playerManager.TargetModel);
                colliderManager.ResolveCollisions();
            }
        }
    }
}
