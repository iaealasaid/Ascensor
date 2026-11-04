using System.Collections;
using UnityEngine;

public class PrenderBotonesInicio : MonoBehaviour
{
    [SerializeField] private GameObject pokeArriba;
    [SerializeField] private GameObject pokeAbajo;
    [SerializeField] private float timeToWait;
    private void Awake()
    {
        pokeArriba.gameObject.SetActive(false);
        pokeAbajo.gameObject.SetActive(false);
    }
    private void Update()
    {
        StartCoroutine(Prender(timeToWait));
    }
    
    IEnumerator Prender(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        ActivationPokes();
        yield return null;
    }

    private void ActivationPokes()
    {
        pokeArriba.gameObject.SetActive(true);
        pokeAbajo.gameObject.SetActive(true) ;
    }
}
