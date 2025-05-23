using System;
using System.Collections;
using UnityEngine;

namespace PlayerSystem
{
    // Use this class to gatekeep powers
    // If the player is not supposed to have a power, then don't instantiate it
    public class PlayerHealthModule : IDamageable
    {
        private EventBus eventBus;
        private PlayerState playerState;
        private Rigidbody2D rb2d;
        private MonoBehaviour mb;
        private Coroutine hpRegenCoroutine;
        private PlayerHealthScriptable healthConstans;
        private float hurtTime = 0f;

        public PlayerHealthModule(EventBus eventBus, PlayerState playerState, Rigidbody2D rb2d, MonoBehaviour mb)
        {
            this.eventBus = eventBus;
            this.playerState = playerState;
            this.rb2d = rb2d;
            this.mb = mb;

            healthConstans = GlobalConstants.Get<PlayerHealthScriptable>();
            SetValues();
            
            eventBus.Subscribe<RequestRespawn>(Respawn);
            eventBus.Subscribe<OnDamageReceived>(DamageReceived);
            eventBus.Subscribe<OnDeath>(Death);
        }

        private void SetValues()
        {
            playerState.maxHealthBars = healthConstans.maxHealthBars;
            playerState.currentHealthBars = healthConstans.currentHealthBars;
            playerState.healthPerBar = healthConstans.healthPerBar;
            playerState.currentHealth = healthConstans.currentHealth;
            playerState.hpRegenRate = healthConstans.hpRegenRate;
        }

        public bool Damage(int damageAmount)
        {
            if (playerState.healthState == HealthState.Stagger || playerState.healthState == HealthState.Death) return false;
            if (playerState.activePower != Power.Square)
            {
                if (hpRegenCoroutine != null) mb.StopCoroutine(hpRegenCoroutine);
                playerState.currentHealth -= damageAmount;
                if (playerState.currentHealth <= 0)
                {
                    if (playerState.currentHealthBars == 1)
                    {
                        HealthUIController.Instance.UpdateHealthUI(0, playerState.healthPerBar, 1);
                        Die();
                        return true;
                    }
                    else
                    {
                        playerState.currentHealthBars--;
                        playerState.currentHealth = playerState.healthPerBar;
                        HealthUIController.Instance.UpdateCurrentHealthBar(playerState.currentHealthBars);
                    }
                }
                HealthUIController.Instance.UpdateHealthUI(playerState.currentHealth, playerState.healthPerBar, playerState.currentHealthBars);
                eventBus.Publish(new OnDamageReceived());
            }
            StartHPRegen();
            return false;
        }

        public void SpikeDamage(int spikeDmg = 0, bool willWarp = true)
        {
            if (playerState.healthState == HealthState.Stagger || playerState.healthState == HealthState.Death) return;
            if (hpRegenCoroutine != null) mb.StopCoroutine(hpRegenCoroutine);

            playerState.currentHealth -= spikeDmg > 0 ? spikeDmg : 1;

            HealthUIController.Instance.UpdateHealthUI(playerState.currentHealth, playerState.healthPerBar, playerState.currentHealthBars);

            if (playerState.currentHealth <= 0)
            {
                if (playerState.currentHealthBars == 1)
                {
                    HealthUIController.Instance.UpdateHealthUI(0, playerState.healthPerBar, 1);
                    Die();
                    return;
                }
                else
                {
                    playerState.currentHealthBars--;
                    playerState.currentHealth = playerState.healthPerBar;
                    HealthUIController.Instance.UpdateCurrentHealthBar(playerState.currentHealthBars);
                }
                HealthUIController.Instance.UpdateHealthUI(playerState.currentHealth, playerState.healthPerBar, playerState.currentHealthBars);
            }
            StartHPRegen();
            if(willWarp) WarpPlayerToSafeGround();
        }

        public void WarpPlayerToSafeGround()
        {
            rb2d.position = playerState.lastSafeGroundLocation;
        }

        public void Die()
        {
            HealthUIController.Instance.SetDeadPortraitImage();
            playerState.healthState = HealthState.Death;
            eventBus.Publish(new OnDeath());
            eventBus.Publish(new RequestPause());
        }

        public void Respawn(RequestRespawn e)
        {
            MenuController.Instance?.ResetScene();
        }

        private void ResetHealthValues()
        {
            playerState.healthState = HealthState.Undefined;
            playerState.currentHealthBars = healthConstans.maxHealthBars;
            playerState.currentHealth = healthConstans.healthPerBar;
            HealthUIController.Instance.ResetHealthUI();
        }

        private void SetRespawnPosition()
        {
            Vector3 savedPosition = GameDataManager.Instance.GetSavedPlayerPosition();
            rb2d.position = savedPosition;
        }

        private IEnumerator RespawnSequence()
        {
            yield return mb.StartCoroutine(MenuController.Instance.FadeInSolidPanel());

            ResetHealthValues();
            SetRespawnPosition();

            yield return new WaitForSeconds(0.2f);

            yield return mb.StartCoroutine(MenuController.Instance.FadeOutSolidPanel());

            eventBus.Publish(new RequestUnpause());
        }

        public void StartHPRegen()
        {
            if (playerState.currentHealth >= playerState.healthPerBar)
            {
                hpRegenCoroutine = null;
                return;
            }
            hpRegenCoroutine = mb.StartCoroutine(HPRegen());
        }

        public IEnumerator HPRegen()
        {
            yield return new WaitForSeconds(playerState.hpRegenRate);
            playerState.currentHealth += 1;
            HealthUIController.Instance.UpdateHealthUI(playerState.currentHealth, playerState.healthPerBar, playerState.currentHealthBars);
            if (playerState.currentHealth < playerState.healthPerBar)
            {
                StartHPRegen();
            }
            else
            {
                hpRegenCoroutine = null;
            }
        }

        private void DamageReceived(OnDamageReceived e)
        {
            playerState.healthState = HealthState.TakingDamage;
            hurtTime = healthConstans.hurtTime;
            eventBus.Publish(new RequestMovementPause());
            eventBus.Subscribe<OnUpdate>(ReduceHurtTimer);
        }

        private void ReduceHurtTimer(OnUpdate e)
        {
            hurtTime -= Time.deltaTime;
            if(hurtTime <= 0f)
            {
                playerState.healthState = HealthState.Undefined;
                eventBus.Publish(new RequestMovementResume());
                eventBus.Unsubscribe<OnUpdate>(ReduceHurtTimer);
            }
        }

        private void Death(OnDeath e)
        {
            playerState.healthState = HealthState.Death;
            eventBus.Publish(new RequestMovementPause());
            eventBus.Publish(new RequestGravityOff());
        }
    }
}