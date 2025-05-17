using System.Collections.Generic;
using System.Resources;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerSystem
{
    public class PlayerBaseModule : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D avatarRigidbody2D;
        [SerializeField] private Animator animator;
        [SerializeField] private PhysicsEventsRelay drillPhysicsRelay;
        [SerializeField] private PhysicsEventsRelay drillExitPhysicsRelay;
        [SerializeField] private FixedJoint2D drillJoint;
        [SerializeField] private PhysicsEventsRelay shieldPhysicsRelay;
        [SerializeField] private InputActionAsset playerInputAsset;
        [SerializeField] private GameObject interactSign;
        [SerializeField] private Collider2D dodgeCollider;
        [SerializeField] private Collider2D playerMainCollider;
        
        public static PlayerBaseModule Instance;
        public ChargesUIController chargesUIController;
        public Knockback knockback;
        public PlayerState state;
        private EventBus eventBus;
        private Dictionary<Direction, TriggerEventHandler> triggers;

        private PlayerInput inputModule;
        private PlayerMovement movementModule;
        public PlayerAnimations animationsModule;
        public PlayerPowersModule powersModule;
        public PlayerHealthModule healthModule;
        private PlayerAudioModule audioModule;
        public PlayerInteractionModule interactionModule;
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            state = new PlayerState();
            eventBus = new EventBus();
        }

        protected void Start()
        {
            inputModule = new PlayerInput(eventBus, playerInputAsset);
            movementModule = new Physics2DMovement(eventBus, state, avatarRigidbody2D);
            animationsModule = new PlayerAnimations(eventBus, state, animator);
            powersModule = new PlayerPowersModule(eventBus, state, avatarRigidbody2D, drillPhysicsRelay, drillExitPhysicsRelay, drillJoint, shieldPhysicsRelay, dodgeCollider, playerMainCollider, this);
            healthModule = new PlayerHealthModule(eventBus, state, avatarRigidbody2D, this);
            interactionModule = new PlayerInteractionModule(eventBus, gameObject.GetComponent<PhysicsEventsRelay>(), interactSign, state);

            avatarRigidbody2D.GetComponent<PhysicsEventsRelay>()?.OnCollisionEnter2DAction.AddListener(OnCollisionEnter2D);
            avatarRigidbody2D.GetComponent<PhysicsEventsRelay>()?.OnCollisionStay2DAction.AddListener(OnCollisionStay2D);
            avatarRigidbody2D.GetComponent<PhysicsEventsRelay>()?.OnCollisionExit2DAction.AddListener(OnCollisionExit2D);

            HealthUIController.Instance.InitUI(state.currentHealth, state.healthPerBar, state.currentHealthBars);
            MenuController.Instance?.setEvents(eventBus);
            DialogueController.Instance?.SetEventBus(eventBus);
            animator.GetComponent<PlayerAnimationEvents>()?.SetEventBus(eventBus);

            GameDataManager.Instance?.SavePlayerPosition(avatarRigidbody2D.position);
            state.lastSafeGroundLocation = avatarRigidbody2D.position;
            chargesUIController.InitChargesUI(state.maxCharges);
        }

        protected void Update()
        {
            eventBus.Publish(new OnUpdate());
            if (state.currentCharges < state.maxCharges)
            {
                chargesUIController.SetColor(1);
                chargesUIController.container.SetActive(true);
                chargesUIController.wasUsed = true;
                state.currentCharges += Time.deltaTime / state.chargeCooldown;
                chargesUIController.StopAllCoroutines();
            }
            else
            {
                chargesUIController.SetColor(0);
                if (chargesUIController.wasUsed)
                {
                    chargesUIController.StartCoroutine(chargesUIController.WhiteBlink());
                    chargesUIController.StartCoroutine(chargesUIController.ShowChargesUI());
                }
            }

            chargesUIController.chargesFill.fillAmount = state.currentCharges / state.maxCharges;
        }

        protected void FixedUpdate()
        {
            eventBus.Publish(new OnFixedUpdate());
        }

        protected void LateUpdate()
        {
            eventBus.Publish(new OnLateUpdate());
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            eventBus.Publish(new OnCollisionEnter2D(collision));

            if(collision.gameObject.CompareTag("Enemy"))
            {
                healthModule.Damage(2);
                eventBus.Publish(new RequestOppositeReaction(Vector2.up, 10f));
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            eventBus.Publish(new OnCollisionStay2D(collision));
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            eventBus.Publish(new OnCollisionExit2D(collision));
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.layer == 8)
            {
                state.lastSafeGroundLocation = avatarRigidbody2D.position;
            }
        }

        private void OnDestroy()
        {
            inputModule.Dispose();
        }

        public void StartChargeRegeneration()
        {
            state.currentCharges -= 1f;
        }

        public void GetPrism()
        {
            state.maxCharges++;
            state.currentCharges = state.maxCharges;
            GameManager.Instance.GetPrism();
            chargesUIController.SetChargesContainer(state.maxCharges);
        }

        public void ShowDiegeticInfoPrism() 
        {
            DiegeticInfoType diegeticInfoType = new()
            {
                time = 3f,
                keyboardText = $"The Prism provided an additional Form Change: {state.maxCharges - 1} => {state.maxCharges}"
            };
            DiegeticInfo.Instance.ShowDiegeticInfo(diegeticInfoType);
        }

        public void SetCharges(int chargeAmount = 1)
        {
            state.maxCharges = chargeAmount;
            state.currentCharges = state.maxCharges;
            chargesUIController.SetChargesContainer(state.maxCharges, false);
        }
    }
}
