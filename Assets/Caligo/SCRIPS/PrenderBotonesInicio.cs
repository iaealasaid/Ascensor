using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PrenderBotonesInicio : MonoBehaviour
{
    [Header("Botones")]
    [SerializeField] private GameObject pokeArriba;
    [SerializeField] private GameObject pokeAbajo;
    [SerializeField] private float timeToWait = 1f;

    [Header("Cámara y Posición")]
    [SerializeField] private GameObject camPosition;

    [Header("Efecto de Carga (Global Volume)")]
    [SerializeField] private Volume globalVolume;
    [SerializeField] private float fadeDuration = 1.0f;

    [Header("Director")]
    [SerializeField] private GameObject director0;

    private ColorAdjustments colorAdjustments;

    private void Awake()
    {
        pokeArriba.SetActive(false);
        pokeAbajo.SetActive(false);
        
        // Obtener el Color Adjustments del Global Volume y asegurar pantalla negra
        if (globalVolume != null && globalVolume.profile.TryGet(out colorAdjustments))
        {
            colorAdjustments.colorFilter.overrideState = true;
            colorAdjustments.colorFilter.value = Color.black;
        }
    }

    private void Start()
    {
        StartCoroutine(RutinaInicial());
    }

    IEnumerator RutinaInicial()
    {
        // 1. Esperar 1 segundo con la pantalla negra mientras Quest estabiliza el tracking
        yield return new WaitForSeconds(1.0f);

        // 2. Ubicar al jugador mientras nadie ve el salto
        if (camPosition != null)
        {
            Vector3 nuevaPos = new Vector3(-0.305f, 0.077f, 0.095f);
            Quaternion nuevaRot = Quaternion.Euler(0, -90, 0);
            camPosition.transform.SetPositionAndRotation(nuevaPos, nuevaRot);
        }

        // 3. Desvanecer de negro a visión normal (Color.white = sin filtro oscuro)
        if (colorAdjustments != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                colorAdjustments.colorFilter.value = Color.Lerp(Color.black, Color.white, elapsed / fadeDuration);
                yield return null;
            }
            colorAdjustments.colorFilter.value = Color.white;
        }
       // ActivarDirectorInicial();

        // 4. Esperar el tiempo restante si timeToWait es mayor al segundo inicial
        float tiempoRestante = Mathf.Max(0, timeToWait - 1.0f);
        if (tiempoRestante > 0)
        {
            yield return new WaitForSeconds(tiempoRestante);
        }

        // 5. Activar los botones
        ActivationPokes();
    }
    /*
    private void ActivarDirectorInicial()
    {
        director0.gameObject.SetActive(true);
        director0.GetComponent<PlayableDirector>().Play();

    }*/
    private void ActivationPokes()
    {
        pokeArriba.SetActive(true);
        pokeAbajo.SetActive(true);
    }
}