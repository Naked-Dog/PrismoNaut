using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChargesUIController : MonoBehaviour
{
    [Serializable]
    private class Charge
    {
        public int maxCharges;
        public Sprite chargeContainer;
        public Sprite chargeFill;
    }

    public GameObject container;
    public Image chargesFill;
    [SerializeField] private Image chargesContainer;
    [SerializeField] private Charge[] charges;

    private Color32[] fillColors = { new(244, 241, 78, 255), new(247, 140, 11, 255), new(255, 255, 255, 255) };

    private Charge setCharge;

    private float currentCharges;

    private float CHARGES_SHOW_TIME = 1f;

    public bool wasUsed = false;

    public void InitChargesUI(int maxCharges)
    {
        SetChargesContainer(maxCharges);
        currentCharges = maxCharges;
        container.SetActive(false);
    }

    public void ResetChargesUI()
    {
        currentCharges = setCharge.maxCharges;
        chargesFill.fillAmount = 1f;
    }

    public void SetChargesContainer(int maxCharges, bool showCharges = true)
    {
        setCharge = Array.Find(charges, charge => charge.maxCharges == maxCharges);
        if (setCharge == null) return;
        chargesContainer.sprite = setCharge.chargeContainer;
        chargesFill.sprite = setCharge.chargeFill;
        ResetChargesUI();
        if(showCharges) StartCoroutine(ShowChargesUI());
    }

    public IEnumerator ShowChargesUI()
    {
        container.SetActive(true);
        yield return new WaitForSeconds(CHARGES_SHOW_TIME);
        container.SetActive(false);
    }

    public IEnumerator WhiteBlink()
    {
        SetColor(2);
        yield return new WaitForSeconds(0.1f);
        wasUsed = false;
        SetColor(0);
    }

    public void SetColor(int index)
    {
        chargesFill.color = fillColors[index];
    }
}
