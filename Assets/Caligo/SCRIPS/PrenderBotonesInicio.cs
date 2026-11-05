using System.Collections;
using UnityEngine;

public class PrenderBotonesInicio : MonoBehaviour
{
    [SerializeField] private GameObject pokeArriba;
    [SerializeField] private GameObject pokeAbajo;
    [SerializeField] private float timeToWait;

    [SerializeField] private GameObject camPosition;

    private Transform camPos;
    private void Awake()
    {
        Vector3 nuevaPos = new Vector3(-0.24f, 0.046f, 0);
        Quaternion nuevaRot = Quaternion.Euler(0, -90, 0);
        pokeArriba.gameObject.SetActive(false);
        pokeAbajo.gameObject.SetActive(false);
        camPosition.transform.SetPositionAndRotation(nuevaPos, nuevaRot);
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
