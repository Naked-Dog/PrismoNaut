using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CollectablesUIController : MonoBehaviour
{
    [SerializeField] private GameObject[] gems;
    [SerializeField] private GameObject gemsContainer;
    [SerializeField] private GameObject prismsContainer;
    [SerializeField] private TextMeshPro prismText;
    private float COLLECTABLES_SHOW_TIME = 5f;

    private int currentGems;
    private int currentPrisms;

    private bool isGemsUIOpen = false;
    private bool isPrismsUIOpen = false;

    public void UpdateGemsUI(int currentGems)
    {
        this.currentGems = currentGems;
        for (int i = 0; i < gems.Length; i++)
        {
            gems[i].SetActive(i + 1 <= currentGems);
        }
        if (!isGemsUIOpen) StartCoroutine(ShowGemsUI());
    }

    public void UpdatePrismsUI(int currentPrisms)
    {
        this.currentPrisms = currentPrisms;
        prismText.text = currentPrisms.ToString();
        if (!isPrismsUIOpen) StartCoroutine(ShowPrismsUI());
    }

    public void InitUI(int currentGems, int currentPrisms)
    {
        this.currentGems = currentGems;
        this.currentPrisms = currentPrisms;
        foreach (GameObject gem in gems)
        {
            gem.SetActive(false);
        }
        prismText.text = currentPrisms.ToString();
        gemsContainer.SetActive(false);
        prismsContainer.SetActive(false);
    }

    private IEnumerator ShowGemsUI()
    {
        isGemsUIOpen = true;
        gemsContainer.SetActive(true);
        yield return new WaitForSeconds(COLLECTABLES_SHOW_TIME);
        gemsContainer.SetActive(false);
        isGemsUIOpen = false;
    }

    private IEnumerator ShowPrismsUI()
    {
        isPrismsUIOpen = true;
        prismsContainer.SetActive(true);
        yield return new WaitForSeconds(COLLECTABLES_SHOW_TIME);
        prismsContainer.SetActive(false);
        isPrismsUIOpen = false;
    }
}
