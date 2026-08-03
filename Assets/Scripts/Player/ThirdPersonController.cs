using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        [Header("Lock On")]
        [Tooltip("视角锁定的索敌半径（米）")]
        public float LockOnRange = 10.0f;

        [Tooltip("敌人所在图层，用于索敌")]
        public LayerMask EnemyLayers;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDAttack;
        private int _animIDBlock;

        private HitboxDetector _hitboxDetector;
        private PlayerHealth _playerHealth;

        // lock on
        private Transform _lockOnTarget;
        private IDamageable _lockOnDamageable;
        private bool _lockOnLayerWarned;



#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }


        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _hasAnimator = TryGetComponent(out _animator);
            if (_hasAnimator) _animator.applyRootMotion = true;
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            _hitboxDetector = GetComponentInChildren<HitboxDetector>();
            _playerHealth = GetComponent<PlayerHealth>();

            // 未配置敌人图层时，自动尝试使用 "Enemy" 图层
            if (EnemyLayers.value == 0)
            {
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer >= 0) EnemyLayers = 1 << enemyLayer;
            }

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            // 死亡后停止全部玩家控制逻辑（受伤/死亡表现由 PlayerHealth 处理）
            if (_playerHealth != null && _playerHealth.IsDead) return;

            JumpAndGravity();
            GroundedCheck();
            Move();
            Attack();
            Block();
            LockOn();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDAttack = Animator.StringToHash("Attack");
            _animIDBlock = Animator.StringToHash("Block");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            // 锁定视角：看向目标身后 2m 处（让怪物居中于画面），忽略玩家鼠标输入
            if (_lockOnTarget != null)
            {
                Vector3 toTarget = _lockOnTarget.position - CinemachineCameraTarget.transform.position;
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    // 视线终点 = 怪物位置 + 沿“相机→怪物”方向再延伸 2m（即怪物身后 2m）
                    Vector3 toLook = toTarget + toTarget.normalized * 2f;
                    toLook.Normalize();
                    _cinemachineTargetYaw = Mathf.Atan2(toLook.x, toLook.z) * Mathf.Rad2Deg;
                    _cinemachineTargetPitch = -Mathf.Asin(Mathf.Clamp(toLook.y, -1f, 1f)) * Mathf.Rad2Deg;
                }
            }
            // if there is an input and camera position is not fixed
            else if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            // 攻击或格挡时跳过代码算速
            if (_hasAnimator && _animator.GetCurrentAnimatorStateInfo(1).IsTag("Attack"))
            {
                return;
            }
            if (_hasAnimator && _animator.GetCurrentAnimatorStateInfo(1).IsTag("Block"))
            {

                return;
            }
            // 受伤硬直中禁止移动
            if (_playerHealth != null && _playerHealth.IsHurt)
            {
                return;
            }

            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // 移动方向始终相对相机（锁定视角时便于横向走位）
            float moveRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                 _mainCamera.transform.eulerAngles.y;

            if (_lockOnTarget != null)
            {
                // 锁定期间面朝目标
                Vector3 faceDir = _lockOnTarget.position - transform.position;
                faceDir.y = 0f;
                if (faceDir.sqrMagnitude > 0.001f)
                {
                    float faceRotation = Mathf.Atan2(faceDir.x, faceDir.z) * Mathf.Rad2Deg;
                    float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, faceRotation,
                        ref _rotationVelocity, RotationSmoothTime);
                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }
            }
            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            else if (_input.move != Vector2.zero)
            {
                _targetRotation = moveRotation;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, moveRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void OnAnimatorMove()
        {
            // 只在攻击状态应用根运动位移，走路/跑步由 Update → Move() 处理
            if (_animator.GetCurrentAnimatorStateInfo(1).IsTag("Attack"))
            {

                _controller.Move(_animator.deltaPosition
                                 + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
            }
            // 非攻击状态：什么也不做，让 Move() 中的代码驱动位移
        }
        private void Attack()
        {
            // 受伤硬直中不能攻击
            if (_playerHealth != null && _playerHealth.IsHurt) return;

            if (_input.attack)
            {
                // 攻击逻辑
                _input.attack = false;  // 消耗输入
                _animator.SetTrigger("Attack");
            }
        }


        private void Block()
        {
            // 受伤硬直中不能格挡
            if (_playerHealth != null && _playerHealth.IsHurt) return;

            if (_input.block)
            {
                
                _input.block = false;  // 消耗输入
                _animator.SetTrigger("Block");
            }
        }

        /// <summary>
        /// 视角锁定：按下中键在索敌范围内寻找最近的存活敌人并锁定；
        /// 已锁定时再按一次取消锁定；目标死亡或超出范围自动解除。
        /// </summary>
        private void LockOn()
        {
            if (_input.ViewpointLocked)
            {
                Debug.Log("LockOn");
                _input.ViewpointLocked = false;  // 消耗输入（边沿触发）
                if (_lockOnTarget != null) ReleaseLock();
                else AcquireLockOnTarget();
            }

            // 目标死亡或超出索敌范围时自动解除锁定
            if (_lockOnTarget != null &&
                (_lockOnDamageable == null || _lockOnDamageable.IsDead ||
                 Vector3.Distance(_lockOnTarget.position, transform.position) > LockOnRange))
            {
                ReleaseLock();
            }
        }

        /// <summary>
        /// 在玩家周围 LockOnRange 米内寻找距离最近的存活敌人作为锁定目标
        /// </summary>
        private void AcquireLockOnTarget()
        {
            int layerMask = EnemyLayers.value;
            if (layerMask == 0)
            {
                // 未配置敌人图层时兜底：检测所有图层的可伤害对象
                layerMask = ~0;
                if (!_lockOnLayerWarned)
                {
                    _lockOnLayerWarned = true;
                    Debug.LogWarning("ThirdPersonController: 未配置 EnemyLayers，已回退为检测所有图层。请在 Inspector 中指定敌人图层。");
                }
            }

            Collider[] hits = Physics.OverlapSphere(transform.position, LockOnRange, layerMask,
                QueryTriggerInteraction.Ignore);
            Transform best = null;
            IDamageable bestDamageable = null;
            float bestSqr = LockOnRange * LockOnRange;

            for (int i = 0; i < hits.Length; i++)
            {
                IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();
                if (damageable == null || damageable.IsDead) continue;
                if (damageable == _playerHealth) continue;  // 排除玩家自身

                float sqr = (hits[i].transform.position - transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = hits[i].transform;
                    bestDamageable = damageable;
                }
            }

            _lockOnTarget = best;
            _lockOnDamageable = bestDamageable;
        }

        private void ReleaseLock()
        {
            _lockOnTarget = null;
            _lockOnDamageable = null;
        }

        private void Reset()
        {
            // 组件重置时自动选中 "Enemy" 图层
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) EnemyLayers = 1 << enemyLayer;
        }
        
        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f && (_playerHealth == null || !_playerHealth.IsHurt))
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            // 死亡后不再播放脚步声
            if (_playerHealth != null && _playerHealth.IsDead) return;
            // 攻击中不播脚步声
            if (_hasAnimator && _animator.GetCurrentAnimatorStateInfo(1).IsTag("Attack"))
                return;

            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    // 脚步音量统一由 AudioManager 的 sfxVolume / 静音管理
                    AudioManager.Instance.PlayRandomSFXAtPoint(FootstepAudioClips,
                        transform.TransformPoint(_controller.center));
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            // 死亡后不再播放落地声
            if (_playerHealth != null && _playerHealth.IsDead) return;

            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                // 落地音量统一由 AudioManager 的 sfxVolume / 静音管理
                AudioManager.Instance.PlaySFXAtPoint(LandingAudioClip,
                    transform.TransformPoint(_controller.center));
            }
        }
        public void PlayAttackSound()
        {
            AudioManager.Instance.PlayRandomSFX(AudioManager.Instance.attackSwingClips);
        }

        /// <summary>
        /// 由 AnimationEvent 调用：转发开启武器命中判定
        /// （AnimationEvent 只能被 Animator 所在物体上的组件接收，武器在子物体上，需转发）
        /// </summary>
        public void EnableHitbox()
        {
            if (_hitboxDetector != null) _hitboxDetector.EnableHitbox();
        }

        /// <summary>
        /// 由 AnimationEvent 调用：转发关闭武器命中判定
        /// </summary>
        public void DisableHitbox()
        {
            if (_hitboxDetector != null) _hitboxDetector.DisableHitbox();
        }
    }
}