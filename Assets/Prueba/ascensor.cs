using UnityEngine;

public class ascensor : MonoBehaviour
{
    // Puertas
    public SkinnedMeshRenderer reja;
    public SkinnedMeshRenderer puerta;

    // Escenarios y personajes
    public GameObject escenario1;
    public GameObject nina;
    public GameObject escenario2;
    public GameObject personaje2;

    // Puntos por donde camina el personaje 2
    public Transform puntoInicio;
    public Transform puntoFrente;
    public Transform puntoFinal;

    // Velocidades
    public float velocidadPuertas = 30f;
    public float velocidadPersonaje = 1.5f;

    float apertura = 0;      // 0 = cerradas, 100 = abiertas
    float objetivo = 0;      // a donde queremos llegar
    float tiempo = 0;        // cronómetro
    int paso = 0;            // en qué parte de la historia estamos

    void Start()
    {
        escenario1.SetActive(true);
        nina.SetActive(false);
        escenario2.SetActive(false);
    }

    void Update()
    {
        tiempo = tiempo + Time.deltaTime;

        // Mueve las puertas poco a poco hacia el objetivo
        apertura = Mathf.MoveTowards(apertura, objetivo, velocidadPuertas * Time.deltaTime);
        reja.SetBlendShapeWeight(0, apertura);
        puerta.SetBlendShapeWeight(0, apertura);

        // Historia paso a paso
        if (paso == 0 && tiempo > 2)
        {
            objetivo = 100;          // abrir puertas
            paso = 1;
        }
        else if (paso == 1 && apertura == 100)
        {
            nina.SetActive(true);    // aparece la niña
            tiempo = 0;
            paso = 2;
        }
        else if (paso == 2 && tiempo > 3)
        {
            objetivo = 0;            // cerrar puertas
            paso = 3;
        }
        else if (paso == 3 && apertura == 0)
        {
            // Cambio de escenario con todo cerrado
            nina.SetActive(false);
            escenario1.SetActive(false);
            escenario2.SetActive(true);
            personaje2.transform.position = puntoInicio.position;
            tiempo = 0;
            paso = 4;
        }
        else if (paso == 4 && tiempo > 1)
        {
            objetivo = 100;          // abrir puertas
            paso = 5;
        }
        else if (paso == 5 && apertura == 100)
        {
            paso = 6;
        }
        else if (paso == 6)
        {
            // Camina hasta quedar frente al ascensor
            if (Caminar(puntoFrente))
            {
                tiempo = 0;
                paso = 7;
            }
        }
        else if (paso == 7 && tiempo > 3)
        {
            paso = 8;                // ya esperó 3 segundos
        }
        else if (paso == 8)
        {
            // Camina hacia la derecha y se va
            if (Caminar(puntoFinal))
            {
                personaje2.SetActive(false);
                tiempo = 0;
                paso = 9;
            }
        }
        else if (paso == 9 && tiempo > 1)
        {
            objetivo = 0;            // cerrar puertas y fin
            paso = 10;
        }
    }

    // Mueve al personaje hacia un punto. Devuelve true cuando llega.
    bool Caminar(Transform destino)
    {
        personaje2.transform.position = Vector3.MoveTowards(
            personaje2.transform.position,
            destino.position,
            velocidadPersonaje * Time.deltaTime);

        return personaje2.transform.position == destino.position;
    }
}