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

    [Header("BlendShapes")]
    [SerializeField] private int blendShapeSubir = 0;
    [SerializeField] private int blendShapeBajar = 1;
    [SerializeField] private int blendShapeDetener = 2;

    [SerializeField] private float velocidadCerrar = 100f;
    [SerializeField] private float velocidadAbrir = 25f;

    private int direccion = 0;
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

        // Mover únicamente los muros
        float movimiento = -direccion * velocidad * Time.deltaTime;

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

        //Cambia el escenario
        direccion = nuevaDireccion;
        StartCoroutine(CambiarEscenario());

        // Comenzar movimiento
        direccion = nuevaDireccion;

        // Esperar el tiempo del recorrido
        yield return new WaitForSeconds(SecondsToStop);

        // Detener movimiento
        direccion = 0;

        //  Abrir al terminar
        yield return AnimarBlendShapes(100f, velocidadAbrir);

        ocupado = false;
    }
    IEnumerator CambiarEscenario()
    {
        // Espera 3 segundos desde que se aprieta el botón
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