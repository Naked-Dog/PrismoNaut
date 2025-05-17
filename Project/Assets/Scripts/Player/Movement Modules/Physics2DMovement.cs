using UnityEngine;
using System.Collections.Generic;
using CameraSystem;
using Cinemachine;

namespace PlayerSystem
{
    public class CollisionSnapshot
    {
        public Collider2D otherCollider;
        public List<ContactPoint2D> contacts = new();
        public Vector2 relativeVelocity;
        public float timestamp;

        public CollisionSnapshot(Collision2D collision)
        {
            otherCollider = collision.collider;
            contacts.AddRange(collision.contacts); // store all contact points
            relativeVelocity = collision.relativeVelocity;
            timestamp = Time.time;
        }
    }

    public class Physics2DMovement : PlayerMovement
    {
        private Rigidbody2D rb2d;
        private PlayerState playerState;
        private Dictionary<Collider2D, CollisionSnapshot> collisions = new();
        private bool jumpRequested = false;
        private float requestedMovement = 0f;
        private float jumpCooldown = 0f;
        private float landingMoveCooldown = 0f;
        private float groundedGraceTimer = 0f;

        readonly float maxJumpCooldown = 0.2f;
        readonly float maxLandingBreakCooldown = 0.1f;
        private bool pauseMovement = false;

        public Physics2DMovement(
            EventBus eventBus,
            PlayerState playerState,
            Rigidbody2D rb2d)
            : base(eventBus)
        {
            this.playerState = playerState;
            this.rb2d = rb2d;

            eventBus.Subscribe<OnHorizontalInput>(OnHorizontalInput);
            eventBus.Subscribe<OnJumpInput>(OnJumpInput);
            eventBus.Subscribe<OnCollisionEnter2D>(AddCollisionSnapshot);
            eventBus.Subscribe<OnCollisionStay2D>(UpdateCollisionSnapshot);
            eventBus.Subscribe<OnCollisionExit2D>(RemoveCollisionSnapshot);
            eventBus.Subscribe<OnFixedUpdate>(OnFixedUpdate);
            eventBus.Subscribe<RequestMovementPause>(RequestMovementPause);
            eventBus.Subscribe<RequestMovementResume>(RequestMovementResume);
            eventBus.Subscribe<RequestGravityOff>(RequestGravityOff);
            eventBus.Subscribe<RequestGravityOn>(RequestGravityOn);
            eventBus.Subscribe<RequestOppositeReaction>(RequestOppositeReaction);
        }

        private void RequestMovementPause(RequestMovementPause e)
        {
            eventBus.Unsubscribe<OnHorizontalInput>(OnHorizontalInput);
            eventBus.Unsubscribe<OnJumpInput>(OnJumpInput);
            pauseMovement = true;
        }

        private void RequestMovementResume(RequestMovementResume e)
        {
            eventBus.Subscribe<OnHorizontalInput>(OnHorizontalInput);
            eventBus.Subscribe<OnJumpInput>(OnJumpInput);
            pauseMovement = false;
        }

        private void AddCollisionSnapshot(OnCollisionEnter2D e)
        {
            var snapshot = new CollisionSnapshot(e.collision);
            collisions[e.collision.collider] = snapshot;
        }

        void UpdateCollisionSnapshot(OnCollisionStay2D e)
        {
            collisions[e.collision.collider] = new CollisionSnapshot(e.collision);
        }

        private void RemoveCollisionSnapshot(OnCollisionExit2D e)
        {
            collisions.Remove(e.collision.collider);
        }

        private void DoGroundCheck()
        {
            bool hasGroundedContact = false;
            foreach (CollisionSnapshot snapshot in collisions.Values)
            {
                foreach (ContactPoint2D contact in snapshot.contacts)
                {
                    if (0.9f < contact.normal.y && 0.01f < contact.normalImpulse)
                    {
                        hasGroundedContact = true;
                        break;
                    }
                }
                if (hasGroundedContact) break;
            }

            if (hasGroundedContact)
            {
                groundedGraceTimer = movementConstants.groundedGracePeriod;
            }
            else
            {
                groundedGraceTimer -= Time.deltaTime;
                if (groundedGraceTimer < 0f) playerState.groundState = GroundState.Airborne;
            }

            if (!playerState.groundState.Equals(GroundState.Grounded) && hasGroundedContact) PerformLanding();
        }

        protected override void OnHorizontalInput(OnHorizontalInput e)
        {
            if (e.amount == 0f) return;
            if (isMovementDisabled) return;
            if (requestedMovement != 0f) return;
            if (0 < landingMoveCooldown) return;
            requestedMovement = e.amount;
        }

        protected override void OnJumpInput(OnJumpInput e)
        {
            if (!playerState.groundState.Equals(GroundState.Grounded)) return;
            if (isJumpingDisabled) return;
            if (jumpRequested) return;
            if (0f < jumpCooldown) return;
            jumpRequested = true;
            AudioManager.Instance.Play2DSound(PlayerSoundsEnum.Jump);
        }

        private void OnFixedUpdate(OnFixedUpdate e)
        {
            playerState.velocity = rb2d.linearVelocity;
            playerState.rotation = rb2d.rotation;
            DoGroundCheck();
            if (jumpRequested) PerformJump();
            if (pauseMovement) return;
            if (requestedMovement != 0f) PerformMovement();
            else PerformHorizontalBreak();
            PerformVerticalBreak();
        }

        private void PerformJump()
        {
            Vector2 impulseVector = new Vector2(0, movementConstants.jumpForce - rb2d.linearVelocity.y);
            rb2d.AddForce(impulseVector, ForceMode2D.Impulse);
            playerState.groundState = GroundState.Airborne;
            jumpCooldown = maxJumpCooldown;
            eventBus.Subscribe<OnUpdate>(ReduceJumpCooldown);
            jumpRequested = false;
        }

        private void PerformMovement()
        {
            requestedMovement *= playerState.groundState == GroundState.Grounded ?
                movementConstants.horizontalGroundedForce :
                movementConstants.horizontalAirborneForce;
            float forceToApply;
            float velocityDirection = Mathf.Sign(rb2d.linearVelocity.x * requestedMovement);
            if (0f < velocityDirection)
            {
                float lerpAmount = Mathf.Abs(rb2d.linearVelocity.x) / movementConstants.maxHorizontalVelocity;
                forceToApply = velocityDirection < 0f ? requestedMovement : Mathf.Lerp(requestedMovement, 0f, lerpAmount);
            }
            else
            {
                forceToApply = requestedMovement;
            }
            rb2d.AddForce(forceToApply * Vector2.right);
            if (Mathf.Sign(requestedMovement * rb2d.linearVelocity.x) < 0) PerformHorizontalBreak();
            eventBus.Publish(new OnHorizontalMovement(requestedMovement));
            requestedMovement = 0f;
        }

        private void PerformHorizontalBreak()
        {
            float breakModifier = playerState.groundState == GroundState.Airborne ? movementConstants.airBreak : movementConstants.groundBreak;
            float breakForce = -rb2d.linearVelocity.x * breakModifier;
            rb2d.AddForce(Vector2.right * breakForce, ForceMode2D.Force);
        }

        private void PerformVerticalBreak() {
            float threshold = Mathf.Abs(movementConstants.maxFallingVelocity);
            float excessVelocity = -rb2d.linearVelocity.y - threshold;
            if (excessVelocity <= 0) return;
            rb2d.AddForce(Vector2.up * excessVelocity, ForceMode2D.Impulse);
            var fallingCamera = CameraManager.Instance.SearchCamera(CineCameraType.Falling);
            CameraManager.Instance.ChangeCamera(fallingCamera);
            //AudioManager.Instance.Play2DSound(PlayerSoundsEnum.LoopWindFall, 1, true);
            //Debug.Log("Falling");
        }

        private void PerformLanding()
        {
            playerState.groundState = GroundState.Grounded;
            AudioManager.Instance.Stop(PlayerSoundsEnum.LoopWindFall);
            AudioManager.Instance.Play2DSound(PlayerSoundsEnum.Land);
            if (0 < requestedMovement * rb2d.linearVelocity.x) return;
            rb2d.AddForce(Vector2.right * -rb2d.linearVelocity.x * 0.75f, ForceMode2D.Impulse);
            landingMoveCooldown = maxLandingBreakCooldown;
            eventBus.Subscribe<OnUpdate>(ReduceLandingMoveCooldown);
            var standingCamera = CameraManager.Instance.SearchCamera(CineCameraType.Regular);
            CameraManager.Instance.ChangeCamera(standingCamera);
        }

        private void ReduceJumpCooldown(OnUpdate e)
        {
            jumpCooldown -= Time.deltaTime;
            if (jumpCooldown <= 0f) eventBus.Unsubscribe<OnUpdate>(ReduceJumpCooldown);
        }

        private void ReduceLandingMoveCooldown(OnUpdate e)
        {
            landingMoveCooldown -= Time.deltaTime;
            if (landingMoveCooldown <= 0f) eventBus.Unsubscribe<OnUpdate>(ReduceLandingMoveCooldown);
        }

        private void RequestGravityOff(RequestGravityOff e)
        {
            rb2d.gravityScale = 0f;
        }
        private void RequestGravityOn(RequestGravityOn e)
        {
            rb2d.gravityScale = movementConstants.gravityScale;
        }

        private void RequestOppositeReaction(RequestOppositeReaction e)
        {
            rb2d.linearVelocity = Vector2.zero;
            rb2d.AddForce(e.direction * e.forceAmount, ForceMode2D.Impulse);          
        }
    }
}