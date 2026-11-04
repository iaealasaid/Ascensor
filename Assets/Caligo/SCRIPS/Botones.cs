using UnityEngine;

public class Botones : MonoBehaviour
{
    public float velocidad = 2;
    public float altura = 7;

    float tiempo = 0;
    float direccion = 0;
    float posInicial;

    void Start()
    {
        posInicial = transform.position.y;
    }

    public void Bajar(float segundos)
    {
        direccion = -1;
        tiempo = segundos;
    }

    public void Subir(float segundos)
    {
        direccion = 1;
        tiempo = segundos;
    }

    void Update()
    {
        if (tiempo > 0)
        {
            tiempo = tiempo - Time.deltaTime;

            transform.Translate(0, direccion * velocidad * Time.deltaTime, 0);

            if (Mathf.Abs(transform.position.y - posInicial) >= altura)
            {
                Vector3 pos = transform.position;
                pos.y = posInicial;
                transform.position = pos;
            }
        }
    }
}
