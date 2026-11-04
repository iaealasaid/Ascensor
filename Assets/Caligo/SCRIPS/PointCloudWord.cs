using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PointCloudWord : MonoBehaviour
{
    [Header("Generación")]
    public Mesh sourceMesh;               // mesh de las letras (FBX de Blender)
    [Range(500, 20000)] public int pointCount = 6000;
    public int seed = 1;

    [Header("Mano")]
    public Transform[] handPoints;        // puntas de los dedos o esfera de prueba
    public float touchRadius = 0.15f;     // metros

    Vector3[] points;
    Bounds touchBounds;
    MeshRenderer rend;
    Material mat;
    bool triggered;
    float endTime;

    static readonly int HitPos = Shader.PropertyToID("_HitPos");
    static readonly int HitTime = Shader.PropertyToID("_HitTime");

    void Awake()
    {
        rend = GetComponent<MeshRenderer>();
        mat = rend.material;
        Generate();
    }

    void OnEnable()
    {
        // Al reactivar el escenario, la palabra vuelve a aparecer
        if (mat == null) return;
        triggered = false;
        rend.enabled = true;
        mat.SetFloat(HitTime, 1000000f);
    }

    void Generate()
    {
        var src = sourceMesh.vertices;
        var tris = sourceMesh.triangles;
        int triCount = tris.Length / 3;

        var cumulative = new float[triCount];
        float total = 0f;
        for (int i = 0; i < triCount; i++)
        {
            Vector3 a = src[tris[i * 3]], b = src[tris[i * 3 + 1]], c = src[tris[i * 3 + 2]];
            total += Vector3.Cross(b - a, c - a).magnitude * 0.5f;
            cumulative[i] = total;
        }

        var rng = new System.Random(seed);
        points = new Vector3[pointCount];
        var verts = new Vector3[pointCount * 3];
        var corner = new Vector2[pointCount * 3];
        var rnd = new Vector2[pointCount * 3];
        var idx = new int[pointCount * 3];

        // Triángulo que contiene un círculo de radio 1
        Vector2[] c3 = { new(0f, 2f), new(-1.732f, -1f), new(1.732f, -1f) };

        for (int p = 0; p < pointCount; p++)
        {
            int t = System.Array.BinarySearch(cumulative, (float)rng.NextDouble() * total);
            if (t < 0) t = ~t;
            t = Mathf.Min(t, triCount - 1);

            float u = (float)rng.NextDouble(), v = (float)rng.NextDouble();
            if (u + v > 1f) { u = 1f - u; v = 1f - v; }
            Vector3 a = src[tris[t * 3]], b = src[tris[t * 3 + 1]], c = src[tris[t * 3 + 2]];
            Vector3 pos = a + u * (b - a) + v * (c - a);
            points[p] = pos;

            var s = new Vector2((float)rng.NextDouble(), (float)rng.NextDouble());
            for (int k = 0; k < 3; k++)
            {
                int i = p * 3 + k;
                verts[i] = pos; corner[i] = c3[k]; rnd[i] = s; idx[i] = i;
            }
        }

        var mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.uv = corner;
        mesh.uv2 = rnd;
        mesh.triangles = idx;
        var bounds = sourceMesh.bounds;
        bounds.Expand(3f); // los puntos vuelan, evita culling
        mesh.bounds = bounds;
        GetComponent<MeshFilter>().sharedMesh = mesh;

        touchBounds = sourceMesh.bounds;
        touchBounds.Expand(touchRadius * 2f / Mathf.Max(0.0001f, transform.lossyScale.x));
    }

    void Update()
    {
        if (triggered)
        {
            if (rend.enabled && Time.timeSinceLevelLoad > endTime) rend.enabled = false;
            return;
        }

        if (handPoints == null) return;

        float scale = Mathf.Max(0.0001f, transform.lossyScale.x);
        float r = touchRadius / scale;
        float r2 = r * r;

        foreach (var hp in handPoints)
        {
            if (hp == null) continue;

            Vector3 local = transform.InverseTransformPoint(hp.position);
            if (!touchBounds.Contains(local)) continue;

            for (int i = 0; i < points.Length; i++)
            {
                if ((points[i] - local).sqrMagnitude < r2)
                {
                    Trigger(hp.position);
                    return;
                }
            }
        }
    }

    void Trigger(Vector3 worldPos)
    {
        triggered = true;
        mat.SetVector(HitPos, worldPos);
        mat.SetFloat(HitTime, Time.timeSinceLevelLoad);

        // Calcula cuándo termina la animación para apagar el renderer
        float maxDist = 0f;
        for (int i = 0; i < points.Length; i += 8)
        {
            float d = Vector3.Distance(transform.TransformPoint(points[i]), worldPos);
            if (d > maxDist) maxDist = d;
        }
        endTime = Time.timeSinceLevelLoad + maxDist * mat.GetFloat("_Delay") + mat.GetFloat("_FadeTime") + 0.2f;
    }

    // Muestra el radio de contacto en la ventana Scene
    void OnDrawGizmosSelected()
    {
        if (handPoints == null) return;
        Gizmos.color = Color.yellow;
        foreach (var hp in handPoints)
            if (hp != null) Gizmos.DrawWireSphere(hp.position, touchRadius);
    }
}