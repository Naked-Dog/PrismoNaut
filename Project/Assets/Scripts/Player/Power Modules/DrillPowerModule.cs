using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PlayerSystem
{
    public class DrillPowerModule
    {
        private EventBus eventBus;
        private PlayerState playerState;
        private Rigidbody2D rb2d;
        private PhysicsEventsRelay drillPhysicsRelay;
        private PhysicsEventsRelay drillExitPhysicsRelay;
        FixedJoint2D drillJoint;
        private PlayerPowersScriptable powersConstants;
        private PlayerBaseModule baseModule;

        private Vector2 inputDirection = Vector2.zero;
        private Vector2 drillDir;
        private float currentSpeed = 0f;
        private float rotationVelocity;
        private bool isSecondStage;
        private bool isInside = false;
        private float damageTimer = 0f;
        private float steerReturnTimer;

        private Collider2D playerCollider;
        private Collider2D enemyCollider;
        private Collider2D heavyTilemapCollider;
        private Collider2D heavyCompositeCollider;
        private Rigidbody2D lightObjectRigidBody;
        private BullHealth enemyHealth;

        private bool isFacingRight => playerState.facingDirection == Direction.Right;

        public DrillPowerModule(
            EventBus eventBus,
            PlayerState playerState,
            Rigidbody2D rb2d,
            PhysicsEventsRelay drillPhysicsRelay,
            PhysicsEventsRelay drillExitPhysicsRelay,
            FixedJoint2D drillJoint,
            PlayerBaseModule baseModule)
        {
            this.eventBus = eventBus;
            this.playerState = playerState;
            this.rb2d = rb2d;
            this.drillPhysicsRelay = drillPhysicsRelay;
            this.drillExitPhysicsRelay = drillExitPhysicsRelay;
            this.drillJoint = drillJoint;
            this.baseModule = baseModule;

            powersConstants = GlobalConstants.Get<PlayerPowersScriptable>();
            playerCollider = rb2d.gameObject.transform.GetChild(1).GetComponent<Collider2D>();

            this.drillJoint.enabled = false;
            this.drillJoint.autoConfigureConnectedAnchor = false;

            eventBus.Subscribe<OnTrianglePowerInput>(OnTrianglePowerInput);
            eventBus.Subscribe<OnHorizontalInput>(TakeHorizontalInputDirection);
            eventBus.Subscribe<OnVerticalInput>(TakeVerticalInputDirection);
        }

        private void OnTrianglePowerInput(OnTrianglePowerInput e)
        {
            if (playerState.activePower == Power.Drill)
            {
                if (isSecondStage) Deactivate();
                return;
            }

            if (playerState.activePower != Power.None) return;

            Activate();
        }

        private void Activate()
        {
            if (playerState.currentCharges < 1f) return;
            baseModule.StartChargeRegeneration();

            AudioManager.Instance.Play2DSound(PlayerSoundsEnum.DrillTrans, 1f, true);
            playerState.activePower = Power.Drill;
            playerState.powerTimeLeft = powersConstants.drillFirstPowerDuration;
            isSecondStage = false;
            steerReturnTimer = powersConstants.steerReturnTimer;

            drillDir = inputDirection.sqrMagnitude > 0.1f
                ? inputDirection.normalized
                : (isFacingRight ? Vector2.right : Vector2.left);

            currentSpeed = powersConstants.drillMinimalFirstVelocity;
            rb2d.linearVelocity = drillDir * currentSpeed;

            eventBus.Publish(new RequestMovementPause());
            eventBus.Publish(new RequestGravityOff());
            eventBus.Subscribe<OnUpdate>(ReduceTimeLeft);
            eventBus.Subscribe<OnFixedUpdate>(Steer);
            drillPhysicsRelay.OnTriggerEnter2DAction.AddListener(ConfirmDrillCollision);
            eventBus.Subscribe<OnCollisionEnter2D>(CheckPlayerCollision);
        }

        private void CheckPlayerCollision(OnCollisionEnter2D e)
        {
            if(e.collision.gameObject.CompareTag("Spike"))
            {
                Deactivate();
            }
        }

        private void TakeHorizontalInputDirection(OnHorizontalInput e)
        {
            inputDirection.x = e.amount;
        }

        private void TakeVerticalInputDirection(OnVerticalInput e)
        {
            inputDirection.y = e.amount;
        }

        private void Steer(OnFixedUpdate e)
        {
            if (!isSecondStage)
            {
                FirstStageSteer();
            }
            else
            {
                SecondStageSteer();
            }
        }

        private void FirstStageSteer()
        {
            if (inputDirection.sqrMagnitude > 0.1f)
            {
                drillDir = inputDirection.normalized;
            }
            float targetAngle = Mathf.Atan2(drillDir.y, drillDir.x) * Mathf.Rad2Deg - 90f;
            float smoothTime = powersConstants.drillFirstSmoothTime;
            float newAngle = Mathf.SmoothDampAngle(
                rb2d.rotation,
                targetAngle,
                ref rotationVelocity,
                smoothTime
            );
            rb2d.MoveRotation(newAngle);
            drillPhysicsRelay.transform.rotation = Quaternion.Euler(0, 0, newAngle);
            if (rb2d.linearVelocity.magnitude > powersConstants.drillMaxFirstVelocity)
            {
                rb2d.linearVelocity = rb2d.linearVelocity.normalized * powersConstants.drillMaxFirstVelocity;
            }
            currentSpeed = Mathf.MoveTowards(currentSpeed, powersConstants.drillMaxFirstVelocity, powersConstants.drillAceleration * Time.fixedDeltaTime);
            rb2d.linearVelocity = drillDir * currentSpeed;
        }

        private void SecondStageSteer()
        {
            if (inputDirection.magnitude > 0.1f)
            {
                float angleDiff = Vector2.SignedAngle(drillDir, inputDirection);

                float maxSteerSpeed = powersConstants.drillSecondSteeringAmount;
                float maxDelta = maxSteerSpeed * Time.fixedDeltaTime;
                float clampedDelta = Mathf.Clamp(angleDiff, -maxDelta, +maxDelta);
                drillDir = Quaternion.Euler(0, 0, clampedDelta) * drillDir;
                drillDir.Normalize();
            }
            float targetAngle = Mathf.Atan2(drillDir.y, drillDir.x) * Mathf.Rad2Deg - 90f;
            float newAngle = Mathf.MoveTowardsAngle(
                rb2d.rotation,
                targetAngle,
                powersConstants.drillSecondSteeringAmount * Time.fixedDeltaTime
            );
            rb2d.MoveRotation(newAngle);
            rb2d.linearVelocity = drillDir * powersConstants.drillMinimalSecondVelocity;
        }

        private void ConfirmDrillCollision(Collider2D other)
        {
            GameObject go = other.gameObject;

            if (other.GetComponent<DirtBallScript>())
            {
                playerState.activePower = Power.LightDrill;
                EnterSecondStage(go);
            }
            else if (go.CompareTag("Enemy"))
            {
                playerState.activePower = Power.HeavyDrill;
                DrillHeavyEnemy(go);
            }
            else if (go.CompareTag("HeavyTerrain"))
            {
                playerState.activePower = Power.HeavyDrill;
                DrillHeavyTerrain(go);
            }
            else if (go.CompareTag("Ground") || go.CompareTag("Spike"))
            {
                DrillObstacle();
            }
            else if(go.CompareTag("Slime"))
            {
                Deactivate();
            }
        }

        private void EnterSecondStage(GameObject other)
        {
            playerState.powerTimeLeft = powersConstants.drillSecondPowerDuration;
            drillPhysicsRelay.OnTriggerEnter2DAction.RemoveListener(ConfirmDrillCollision);
            AttachObjectToDrill(other);
            drillPhysicsRelay.OnTriggerExit2DAction.AddListener(ConfirmDrillExit);
            if (other.CompareTag("Enemy"))
            {
                eventBus.Subscribe<OnUpdate>(DamageTimer);
            }
        }

        private void DrillHeavyEnemy(GameObject other)
        {
            enemyCollider = other.GetComponent<Collider2D>();
            enemyHealth = other.GetComponent<Bull>().bullHealth;
            Physics2D.IgnoreCollision(playerCollider, enemyCollider, true);
            eventBus.Unsubscribe<OnUpdate>(ReduceTimeLeft);
            isInside = true;
            drillPhysicsRelay.OnTriggerExit2DAction.AddListener(ConfirmDrillExit);
            eventBus.Subscribe<OnUpdate>(DamageTimer);
        }

        private void DrillHeavyTerrain(GameObject other)
        {
            heavyTilemapCollider = other.GetComponent<TilemapCollider2D>();
            heavyCompositeCollider = other.GetComponent<CompositeCollider2D>();
            Physics2D.IgnoreCollision(playerCollider, heavyTilemapCollider, true);
            Physics2D.IgnoreCollision(playerCollider, heavyCompositeCollider, true);
            eventBus.Unsubscribe<OnUpdate>(ReduceTimeLeft);
            isInside = true;
            drillPhysicsRelay.OnTriggerExit2DAction.AddListener(ConfirmDrillExit);
            AudioManager.Instance.Play2DSound(LevelEventsSoundsEnum.EartThrumbling, 1, true);
        }

        private void DrillObstacle()
        {
            if (isInside)
            {
                eventBus.Unsubscribe<OnHorizontalInput>(TakeHorizontalInputDirection);
                eventBus.Unsubscribe<OnVerticalInput>(TakeVerticalInputDirection);
                eventBus.Subscribe<OnUpdate>(SteerControlReturn);
                inputDirection = Vector2.zero;
                drillDir = -drillDir;
            }
            else
            {
                eventBus.Publish(new RequestOppositeReaction(-drillDir, powersConstants.drillOppositeForce));
                eventBus.Publish(new OnCancelPower());
                Deactivate(true);
            }
        }

        private void ConfirmDrillExit(Collider2D other)
        {
            if (other.gameObject.CompareTag("Enemy") || other.gameObject.CompareTag("HeavyTerrain"))
            {
                eventBus.Unsubscribe<OnFixedUpdate>(Steer);
                eventBus.Unsubscribe<OnUpdate>(DamageTimer);
                drillExitPhysicsRelay.OnTriggerExit2DAction.AddListener(ConfirmPlayerExit);
            }
        }

        private void ConfirmPlayerExit(Collider2D other)
        {
            if (other.gameObject.CompareTag("Enemy") || other.gameObject.CompareTag("HeavyTerrain"))
            {
                if (enemyCollider != null)
                {
                    Physics2D.IgnoreCollision(playerCollider, enemyCollider, false);
                }

                if (heavyCompositeCollider != null && heavyTilemapCollider != null)
                {
                    Physics2D.IgnoreCollision(playerCollider, heavyTilemapCollider, false);
                    Physics2D.IgnoreCollision(playerCollider, heavyCompositeCollider, false);
                }

                rb2d.linearVelocity = Vector2.zero;
                rb2d.AddForce(drillDir * powersConstants.heavyExitForceImpulse, ForceMode2D.Impulse);
                Deactivate();
            }
        }

        private void AttachObjectToDrill(GameObject gameObject)
        {
            Debug.Log("AttachObjectToDrill");
            AudioManager.Instance.Play2DSound(LevelEventsSoundsEnum.EartThrumbling,1 ,true);
            lightObjectRigidBody = gameObject.GetComponent<Rigidbody2D>();
            lightObjectRigidBody.simulated = false;
            Transform lightTransform = gameObject.transform;
            Vector2 worldTip = drillPhysicsRelay.transform.GetChild(0).position;
            Vector2 lightScale = lightTransform.lossyScale / 2f;
            lightTransform.position = worldTip + (lightScale * drillDir);

            lightTransform.SetParent(rb2d.transform, true);
            // lightObjectRigidBody = gameObject.GetComponent<Rigidbody2D>();
            // Vector2 worldTip = drillPhysicsRelay.transform.GetChild(0).position;

            // drillJoint.autoConfigureConnectedAnchor = false;
            // drillJoint.connectedBody = lightObjectRigidBody;

            // Vector2 localTip = rb2d.transform.InverseTransformPoint(worldTip);
            // drillJoint.anchor = localTip;

            // Vector2 lightLocalTip = lightObjectRigidBody.transform.InverseTransformPoint(worldTip);
            // drillJoint.connectedAnchor = lightLocalTip;
            // drillJoint.enabled = true;

            isSecondStage = true;
        }


        private void ReduceTimeLeft(OnUpdate e)
        {
            playerState.powerTimeLeft -= Time.deltaTime;
            if (playerState.powerTimeLeft > 0) return;
            eventBus.Unsubscribe<OnUpdate>(ReduceTimeLeft);
            Deactivate();
        }

        private void SteerControlReturn(OnUpdate e)
        {
            steerReturnTimer -= Time.deltaTime;
            if (steerReturnTimer > 0) return;
            eventBus.Subscribe<OnHorizontalInput>(TakeHorizontalInputDirection);
            eventBus.Subscribe<OnVerticalInput>(TakeVerticalInputDirection);
            eventBus.Unsubscribe<OnUpdate>(SteerControlReturn);
            steerReturnTimer = 0.4f;
        }

        private void DamageTimer(OnUpdate e)
        {
            if (damageTimer <= 0)
            {
                enemyHealth?.TakeDamage(powersConstants.heavyDrillDamagePerSecond);
                
                damageTimer += 1f;
            }

            damageTimer -= Time.deltaTime;
        }

        public void Deactivate(bool force = false)
        {

            if (lightObjectRigidBody != null)
            {
                lightObjectRigidBody.transform.SetParent(null, true);
                lightObjectRigidBody.simulated = true;
                lightObjectRigidBody.linearVelocity = Vector2.zero;
                lightObjectRigidBody.AddForce(drillDir * powersConstants.lightObjectExitForce, ForceMode2D.Impulse);
                rb2d.linearVelocity = Vector2.up * powersConstants.lightPlayerExitForce;
            }

            AudioManager.Instance.Stop(PlayerSoundsEnum.DrillTrans);
            AudioManager.Instance.Stop(LevelEventsSoundsEnum.EartThrumbling);

            rb2d.MoveRotation(0f);
            if(!force) playerState.activePower = Power.None;
            drillJoint.enabled = false;
            drillPhysicsRelay.transform.rotation = Quaternion.Euler(0, 0, 0);
            currentSpeed = 0f;
            damageTimer = 0f;
            isInside = false;
            lightObjectRigidBody = null;
            eventBus.Publish(new RequestMovementResume());
            eventBus.Publish(new RequestGravityOn());
            eventBus.Unsubscribe<OnFixedUpdate>(Steer);
            drillPhysicsRelay.OnTriggerEnter2DAction.RemoveListener(ConfirmDrillCollision);
            drillPhysicsRelay.OnTriggerExit2DAction.RemoveListener(ConfirmDrillExit);
            drillExitPhysicsRelay.OnTriggerExit2DAction.RemoveListener(ConfirmPlayerExit);
            eventBus.Unsubscribe<OnCollisionEnter2D>(CheckPlayerCollision);
        }
    }
}