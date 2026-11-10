using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;
using System.Collections;
public class ManualRelocate : MonoBehaviour
{
    [SerializeField] private GameObject positionHotSpot;
    [SerializeField] private GameObject pokeStart;
    [SerializeField] private GameObject director0;
    [SerializeField] private GameObject nivel0;


    
    private PlayableDirector directorStart;
    private void Awake()
    {
        directorStart = director0.GetComponent<PlayableDirector>();
    }

    public void ManualRelocation()
    {
        StartCoroutine(WaitToStartDirector());
    }
    private IEnumerator WaitToStartDirector()
    {
        nivel0.gameObject.SetActive(true);
        yield return new WaitForSeconds(5F);
        positionHotSpot.SetActive(false);
        pokeStart.SetActive(false);
        yield return new WaitForSeconds(0.5f);
        directorStart.Play();
        yield return null;
    }
}
