using UnityEngine;
using System.Collections;
using UnityEngine.Playables;

public class ControladorAscensor : MonoBehaviour
{
    [Header("Ascensor")]
    public Transform ascensor;

    [Header("Muros")]
    public Transform[] muros = new Transform[3];
    public float separacionMuros = 7f;

    [Header("Movimiento")]
    [SerializeField] private float velocidad = 2f;
    [SerializeField] private float SecondsToStop = 5f;

    [Header("Aceleración / Frenado")]
    [Tooltip("Segundos que tarda en llegar a la velocidad máxima al arrancar")]
    [SerializeField] private float tiempoAceleracion = 1.5f;

    [Tooltip("Segundos que tarda en frenar al llegar")]
    [SerializeField] private float tiempoFrenado = 1.5f;

    [Tooltip("Curva de arranque")]
    [SerializeField] private AnimationCurve curvaAceleracion = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Curva de frenado")]
    [SerializeField] private AnimationCurve curvaFrenado = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Pausas")]
    [SerializeField] private float esperaAntesDeMover = 1f;
    [SerializeField] private float esperaAntesDeAbrir = 1f;

    [Header("Puerta")]
    [SerializeField] private float tiempoCerrar = 1f;
    [SerializeField] private float tiempoAbrir = 4f;

    [SerializeField] private AnimationCurve curvaCerrar = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve curvaAbrir = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("BlendShapes")]
    [SerializeField] private int blendShapeSubir = 0;
    [SerializeField] private int blendShapeBajar = 1;
    [SerializeField] private int blendShapeDetener = 2;

    [Header("Reja del Ascensor")]
    [SerializeField] private SkinnedMeshRenderer rendererReja;
    [SerializeField] private float tiempoCerrarReja = 2.5f;
    [SerializeField] private float tiempoAbrirReja = 2.5f;
    [SerializeField] private AnimationCurve curvaReja = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Escenarios")]
    public GameObject[] escenarios;
    public PlayableDirector[] directores;
    public float esperaCambio = 3f;

    [Header("AudioSources")]
    [SerializeField] private AudioSource audioSourceButton;
    [SerializeField] private AudioSource audioSourceMoverDePiso;
    [SerializeField] private AudioSource audioSourceAsencorBox;
    [SerializeField] private AudioSource audioSourceReja;
    [SerializeField] private AudioSource audioSourceCerrar;

    [Header("Clips")]
    [SerializeField] private AudioClip audioClipButton;
    [SerializeField] private AudioClip audioclipReja;
    [SerializeField] private AudioClip audioClipDing;
    [SerializeField] private AudioClip audioClipCerrar;
  
    private int direccion = 0;
    private float factorVelocidad = 0f;
    private bool ocupado = false;
    private int indiceEscenario = 0;
    private SkinnedMeshRenderer[] renderersMuros;

    private void Awake()
    {
        renderersMuros = new SkinnedMeshRenderer[muros.Length];

        for (int i = 0; i < muros.Length; i++)
        {
            if (muros[i] != null)
            {
                renderersMuros[i] =
                    muros[i].GetComponentInChildren<SkinnedMeshRenderer>();
            }
        }
    }

    void Start()
    {
       /* // Solo el primer escenario activo al empezar
        for (int i = 0; i < escenarios.Length; i++)
        {
            escenarios[i].SetActive(i == indiceEscenario);
        }

        directores[indiceEscenario].Play(); */
    }

    void Update()
    {
        // Inputs para probar en el editor
        if (Input.GetKeyDown(KeyCode.W)) Subir();
        if (Input.GetKeyDown(KeyCode.S)) Bajar();

        if (direccion == 0) return;

        // Mover únicamente los muros
        float movimiento =
            -direccion * velocidad * factorVelocidad * Time.deltaTime;

        float limite = separacionMuros * 1.5f;
        float ciclo = separacionMuros * muros.Length;

        foreach (Transform muro in muros)
        {
            if (muro == null) continue;

            Vector3 pos = muro.position;
            pos.y += movimiento;

            // Reciclar muros inferiores
            while (pos.y < ascensor.position.y - limite)
            {
                pos.y += ciclo;
            }

            // Reciclar muros superiores
            while (pos.y > ascensor.position.y + limite)
            {
                pos.y -= ciclo;
            }

            muro.position = pos;
        }
    }

    public void Subir()
    {
        audioSourceButton.PlayOneShot(audioClipButton);

        if (!ocupado)
            StartCoroutine(SecuenciaAscensor(1));
    }

    public void Bajar()
    {
        audioSourceButton.PlayOneShot(audioClipButton);

        if (!ocupado)
            StartCoroutine(SecuenciaAscensor(-1));
    }

    IEnumerator SecuenciaAscensor(int nuevaDireccion)
    {
        ocupado = true;

        // NUEVO: cerrar la reja antes de la secuencia
        yield return CerrarReja();

        // 1. Cerrar antes de moverse
        yield return AnimarBlendShapes(
            0f, tiempoCerrar, curvaCerrar
        );

        // 2. Pausa con la puerta cerrada
        if (esperaAntesDeMover > 0f)
            yield return new WaitForSeconds(esperaAntesDeMover);

        // Cambia el escenario
        StartCoroutine(CambiarEscenario());

        // Comenzar movimiento
        direccion = nuevaDireccion;
        audioSourceMoverDePiso.Play();

        // 3. Aceleración gradual
        yield return AnimarFactor(
            curvaAceleracion, tiempoAceleracion, 0f, 1f
        );

        // 4. Velocidad constante
        float tiempoConstante = Mathf.Max(
            0f,
            SecondsToStop - tiempoAceleracion - tiempoFrenado
        );

        factorVelocidad = 1f;
        yield return new WaitForSeconds(tiempoConstante);

        // 5. Frenado gradual
        yield return AnimarFactor(
            curvaFrenado, tiempoFrenado, 1f, 0f
        );

        // Detener movimiento
        factorVelocidad = 0f;
        direccion = 0;

        // 6. Pausa al llegar
        audioSourceMoverDePiso.Stop();
        yield return new WaitForSeconds(.5f);
        audioSourceAsencorBox.PlayOneShot(audioClipDing);
        if (esperaAntesDeAbrir > 0f)
            yield return new WaitForSeconds(esperaAntesDeAbrir);

        // 7. Abrir puertas originales
        audioSourceCerrar.PlayOneShot(audioClipCerrar);
        yield return AnimarBlendShapes(
            100f, tiempoAbrir, curvaAbrir
        );

        // NUEVO: abrir la reja al finalizar
        yield return AbrirReja();

        ocupado = false;
    }

    IEnumerator AnimarFactor(AnimationCurve curva,float duracion,float desde,float hasta)

    {
        if (duracion <= 0f)
        {
            factorVelocidad = hasta;
            yield break;
        }

        float t = 0f;

        while (t < duracion)
        {
            t += Time.deltaTime;

            float progreso = Mathf.Clamp01(t / duracion);

            factorVelocidad = curva.Evaluate(progreso);

            yield return null;
        }

        factorVelocidad = hasta;

    }

    IEnumerator CambiarEscenario()
    {
        yield return new WaitForSeconds(esperaCambio);

        if (indiceEscenario >= escenarios.Length - 1)
            yield break;

        directores[indiceEscenario].Stop();
        escenarios[indiceEscenario].SetActive(false);

        indiceEscenario++;

        escenarios[indiceEscenario].SetActive(true);
        directores[indiceEscenario].Play();
    }

    IEnumerator AnimarBlendShapes(float objetivo,float duracion,AnimationCurve curva)
    {
        int[] indices =
        {
            blendShapeSubir,
            blendShapeBajar,
            blendShapeDetener
        };

        float[][] inicio =
            new float[renderersMuros.Length][];

        for (int r = 0; r < renderersMuros.Length; r++)
        {
            SkinnedMeshRenderer rend = renderersMuros[r];

            if (rend == null || rend.sharedMesh == null)
                continue;

            inicio[r] = new float[indices.Length];

            for (int k = 0; k < indices.Length; k++)
            {
                if (indices[k] < 0 ||
                    indices[k] >= rend.sharedMesh.blendShapeCount)
                    continue;

                inicio[r][k] =
                    rend.GetBlendShapeWeight(indices[k]);
            }
        }

        if (duracion > 0f)
        {
            float t = 0f;

            while (t < duracion)
            {
                t += Time.deltaTime;

                float progreso = curva.Evaluate(
                    Mathf.Clamp01(t / duracion)
                );

                AplicarBlendShapes(
                    inicio, objetivo, progreso, indices
                );

                yield return null;
            }
        }

        AplicarBlendShapes(inicio, objetivo, 1f, indices);
    }

    void AplicarBlendShapes(float[][] inicio,float objetivo, float progreso,int[] indices)
    {
        for (int r = 0; r < renderersMuros.Length; r++)
        {
            SkinnedMeshRenderer rend = renderersMuros[r];

            if (rend == null ||
                rend.sharedMesh == null ||
                inicio[r] == null)
                continue;

            for (int k = 0; k < indices.Length; k++)
            {
                if (indices[k] < 0 ||
                    indices[k] >= rend.sharedMesh.blendShapeCount)
                    continue;

                float valor = Mathf.LerpUnclamped(
                    inicio[r][k],
                    objetivo,
                    progreso
                );

                rend.SetBlendShapeWeight(indices[k], valor);
            }
        }
    }

    public IEnumerator CerrarReja()
    {
        audioSourceReja.PlayOneShot(audioclipReja);
        yield return AnimarReja(0f, tiempoCerrarReja);
    }

    public IEnumerator AbrirReja()
    {
        audioSourceReja.PlayOneShot(audioclipReja);
        yield return AnimarReja(100f, tiempoAbrirReja);
    }


    private IEnumerator AnimarReja(float objetivo, float duracion)
    {
        if (rendererReja == null || rendererReja.sharedMesh == null)
        {
            Debug.LogWarning("No se ha asignado el SkinnedMeshRenderer de la reja.");
            yield break;
        }

        if (rendererReja.sharedMesh.blendShapeCount == 0)
        {
            Debug.LogWarning("La reja no tiene BlendShapes.");
            yield break;
        }

        // Como solo existe un BlendShape, utilizamos el índice 0
        int indice = 0;

        float valorInicial = rendererReja.GetBlendShapeWeight(indice);

        duracion = Mathf.Max(0f, duracion);

        if (duracion <= 0f)
        {
            rendererReja.SetBlendShapeWeight(indice, objetivo);
            yield break;
        }

        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;

            float progreso = Mathf.Clamp01(tiempo / duracion);
            float suavizado = curvaReja.Evaluate(progreso);

            float valor = Mathf.Lerp(valorInicial, objetivo, suavizado);

            rendererReja.SetBlendShapeWeight(indice, valor);

            yield return null;
        }

        rendererReja.SetBlendShapeWeight(indice, objetivo);
    }

    public void EventoCerrarReja()
    {
        StartCoroutine(CerrarReja());
    }

    public void EventoAbrirReja()
    {
        StartCoroutine(AbrirReja());
    }
}
