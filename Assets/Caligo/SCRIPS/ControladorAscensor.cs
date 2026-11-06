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
    [Tooltip("Curva de arranque: eje X = progreso (0-1), eje Y = % de velocidad (0-1)")]
    [SerializeField] private AnimationCurve curvaAceleracion = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Curva de frenado: eje X = progreso (0-1), eje Y = % de velocidad (1 a 0)")]
    [SerializeField] private AnimationCurve curvaFrenado = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Pausas")]
    [Tooltip("Segundos que espera con la puerta cerrada antes de empezar a moverse")]
    [SerializeField] private float esperaAntesDeMover = 1f;
    [Tooltip("Segundos que espera después de llegar antes de abrir la puerta")]
    [SerializeField] private float esperaAntesDeAbrir = 1f;

    [Header("BlendShapes")]
    [SerializeField] private int blendShapeSubir = 0;
    [SerializeField] private int blendShapeBajar = 1;
    [SerializeField] private int blendShapeDetener = 2;

    [SerializeField] private float velocidadCerrar = 100f;
    [SerializeField] private float velocidadAbrir = 25f;

    private int direccion = 0;
    private float factorVelocidad = 0f; // 0 = detenido, 1 = velocidad máxima
    private bool ocupado = false;

    [Header("Escenarios")]
    public GameObject[] escenarios;
    public PlayableDirector[] directores;
    public float esperaCambio = 3f;

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
        // Solo el primer escenario activo al empezar
        for (int i = 0; i < escenarios.Length; i++)
        {
            escenarios[i].SetActive(i == indiceEscenario);
        }

        directores[indiceEscenario].Play();
    }

    void Update()
    {
        // Inputs para probar en el editor
        if (Input.GetKeyDown(KeyCode.W)) Subir();
        if (Input.GetKeyDown(KeyCode.S)) Bajar();

        if (direccion == 0) return;

        // Mover únicamente los muros (la velocidad depende del factor)
        float movimiento = -direccion * velocidad * factorVelocidad * Time.deltaTime;

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
        if (!ocupado)
            StartCoroutine(SecuenciaAscensor(1));
    }

    public void Bajar()
    {
        if (!ocupado)
            StartCoroutine(SecuenciaAscensor(-1));
    }

    IEnumerator SecuenciaAscensor(int nuevaDireccion)
    {
        ocupado = true;

        // 1. Cerrar antes de moverse
        yield return AnimarBlendShapes(0f, velocidadCerrar);

        // 2. Pausa con la puerta cerrada
        if (esperaAntesDeMover > 0f)
            yield return new WaitForSeconds(esperaAntesDeMover);

        // Cambia el escenario (el contador arranca junto con el movimiento)
        StartCoroutine(CambiarEscenario());

        // Comenzar movimiento
        direccion = nuevaDireccion;

        // 3. Aceleración gradual
        yield return AnimarFactor(curvaAceleracion, tiempoAceleracion, 0f, 1f);

        // 4. Velocidad constante
        float tiempoConstante = Mathf.Max(0f, SecondsToStop - tiempoAceleracion - tiempoFrenado);
        factorVelocidad = 1f;
        yield return new WaitForSeconds(tiempoConstante);

        // 5. Frenado gradual
        yield return AnimarFactor(curvaFrenado, tiempoFrenado, 1f, 0f);

        // Detener movimiento
        factorVelocidad = 0f;
        direccion = 0;

        // 6. Pausa al llegar, con la puerta todavía cerrada
        if (esperaAntesDeAbrir > 0f)
            yield return new WaitForSeconds(esperaAntesDeAbrir);

        // 7. Abrir
        yield return AnimarBlendShapes(100f, velocidadAbrir);

        ocupado = false;
    }

    // Anima factorVelocidad siguiendo una curva durante 'duracion' segundos
    IEnumerator AnimarFactor(AnimationCurve curva, float duracion, float desde, float hasta)
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

            // La curva devuelve un valor 0-1 (o 1-0 en el frenado)
            factorVelocidad = curva.Evaluate(progreso);
            yield return null;
        }

        factorVelocidad = hasta;
    }

    IEnumerator CambiarEscenario()
    {
        // Espera desde que el ascensor empieza a moverse
        yield return new WaitForSeconds(esperaCambio);

        // Si ya es el último escenario, no hace nada
        if (indiceEscenario >= escenarios.Length - 1)
            yield break;

        // Apaga el escenario actual
        directores[indiceEscenario].Stop();
        escenarios[indiceEscenario].SetActive(false);

        // Pasa al siguiente
        indiceEscenario++;

        // Enciende el nuevo y lanza su timeline
        escenarios[indiceEscenario].SetActive(true);
        directores[indiceEscenario].Play();
    }

    IEnumerator AnimarBlendShapes(float objetivo, float velocidadCambio)
    {
        if (velocidadCambio <= 0f)
            yield break;

        bool terminado = false;

        while (!terminado)
        {
            terminado = true;

            foreach (SkinnedMeshRenderer renderer in renderersMuros)
            {
                if (renderer == null || renderer.sharedMesh == null)
                    continue;

                int[] indices = {
                    blendShapeSubir,
                    blendShapeBajar,
                    blendShapeDetener
                };

                foreach (int indice in indices)
                {
                    if (indice < 0 ||
                        indice >= renderer.sharedMesh.blendShapeCount)
                        continue;

                    float actual = renderer.GetBlendShapeWeight(indice);

                    float nuevo = Mathf.MoveTowards(
                        actual,
                        objetivo,
                        velocidadCambio * Time.deltaTime
                    );

                    renderer.SetBlendShapeWeight(indice, nuevo);

                    if (!Mathf.Approximately(nuevo, objetivo))
                        terminado = false;
                }
            }

            if (!terminado)
                yield return null;
        }
    }
}