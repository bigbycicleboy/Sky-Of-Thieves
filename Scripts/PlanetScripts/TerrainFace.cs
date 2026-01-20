using Unity.Mathematics;
using UnityEngine;
using System.Collections;

public class TerrainFace
{
    ShapeGenerator shapeGenerator;
    Mesh mesh;
    int resolution;
    int currentResolution;
    Vector3 localUp;
    Vector3 axisA;
    Vector3 axisB;
    Vector3 faceCenter;
    
    bool isGenerating = false;
    Coroutine generationCoroutine;
    MonoBehaviour coroutineHost;

    // LOD settings
    public static int maxResolution = 2048;
    public static float[] lodDistances = new float[] { 8500f, 10000f, 12000f, 16000f, 24000f, 40000f };
    public static int[] lodResolutions = new int[] { 2048, 1024, 512, 256, 128, 64, 16 };

    public TerrainFace(ShapeGenerator shapeGenerator, Mesh mesh, int resolution, Vector3 localUp, MonoBehaviour host)
    {
        this.shapeGenerator = shapeGenerator;
        this.mesh = mesh;
        this.resolution = resolution;
        this.currentResolution = -1;
        this.localUp = localUp;
        this.coroutineHost = host;

        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);
        faceCenter = localUp.normalized;
    }

    public void UpdateLOD(Vector3 cameraPosition)
    {
        // Don't start new generation if one is in progress
        if (isGenerating) return;

        float planetRadius = 1f;
        if (shapeGenerator != null)
        {
            planetRadius = shapeGenerator.GetScaledElevation(1f);
        }
        
        Vector3 faceWorldCenter = faceCenter * planetRadius;
        float distance = Vector3.Distance(cameraPosition, faceWorldCenter);
        
        int targetResolution = lodResolutions[lodResolutions.Length - 1];
        
        for (int i = 0; i < lodDistances.Length; i++)
        {
            if (distance < lodDistances[i])
            {
                targetResolution = lodResolutions[i];
                break;
            }
        }

        if (targetResolution != currentResolution)
        {
            if (generationCoroutine != null)
            {
                coroutineHost.StopCoroutine(generationCoroutine);
            }
            generationCoroutine = coroutineHost.StartCoroutine(ConstructMeshAsync(targetResolution));
        }
    }

    IEnumerator ConstructMeshAsync(int res)
    {
        isGenerating = true;
        currentResolution = res;

        Vector3[] vertices = new Vector3[res * res];
        int[] triangles = new int[(res - 1) * (res - 1) * 6];
        Vector2[] uv = new Vector2[res * res];
        int triIndex = 0;

        if (vertices.Length > 65535)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        float minE = float.MaxValue;
        float maxE = float.MinValue;
        float[] elevations = new float[vertices.Length];

        // Pass 1: Sample elevations in chunks
        int chunkSize = 64; // Process 64 vertices per frame
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                int i = x + y * res;

                Vector2 percent = new Vector2(x, y) / (res - 1);
                Vector3 pointOnUnitCube =
                    localUp +
                    (percent.x - .5f) * 2 * axisA +
                    (percent.y - .5f) * 2 * axisB;

                Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;
                float elevation = shapeGenerator.CalculateUnscaledElevation(pointOnUnitSphere);

                elevations[i] = elevation;
                minE = Mathf.Min(minE, elevation);
                maxE = Mathf.Max(maxE, elevation);

                // Yield every chunkSize vertices
                if (i % chunkSize == 0 && i > 0)
                {
                    yield return null;
                }
            }
        }

        shapeGenerator.elevationMinMax.Min = minE;
        shapeGenerator.elevationMinMax.Max = maxE;

        // Pass 2: Build mesh + UVs in chunks
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                int i = x + y * res;

                Vector2 percent = new Vector2(x, y) / (res - 1);
                Vector3 pointOnUnitCube =
                    localUp +
                    (percent.x - .5f) * 2 * axisA +
                    (percent.y - .5f) * 2 * axisB;

                Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;
                float elevation = elevations[i];

                vertices[i] = pointOnUnitSphere * shapeGenerator.GetScaledElevation(elevation);
                uv[i].y = Mathf.InverseLerp(minE, maxE, elevation) * 1.5f;

                if (x != res - 1 && y != res - 1)
                {
                    triangles[triIndex] = i;
                    triangles[triIndex + 1] = i + res + 1;
                    triangles[triIndex + 2] = i + res;

                    triangles[triIndex + 3] = i;
                    triangles[triIndex + 4] = i + 1;
                    triangles[triIndex + 5] = i + res + 1;
                    triIndex += 6;
                }

                // Yield every chunkSize vertices
                if (i % chunkSize == 0 && i > 0)
                {
                    yield return null;
                }
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.uv = uv;

        Planet.Instance.GenerateCollider();
        
        isGenerating = false;
    }

    public void ConstructMesh()
    {
        // For initial synchronous generation
        int res = currentResolution > 0 ? currentResolution : resolution;
        
        Vector3[] vertices = new Vector3[res * res];
        int[] triangles = new int[(res - 1) * (res - 1) * 6];
        Vector2[] uv = new Vector2[res * res];
        int triIndex = 0;

        if (vertices.Length > 65535)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        float minE = float.MaxValue;
        float maxE = float.MinValue;
        float[] elevations = new float[vertices.Length];

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                int i = x + y * res;

                Vector2 percent = new Vector2(x, y) / (res - 1);
                Vector3 pointOnUnitCube =
                    localUp +
                    (percent.x - .5f) * 2 * axisA +
                    (percent.y - .5f) * 2 * axisB;

                Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;
                float elevation = shapeGenerator.CalculateUnscaledElevation(pointOnUnitSphere);

                elevations[i] = elevation;
                minE = Mathf.Min(minE, elevation);
                maxE = Mathf.Max(maxE, elevation);
            }
        }

        shapeGenerator.elevationMinMax.Min = minE;
        shapeGenerator.elevationMinMax.Max = maxE;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                int i = x + y * res;

                Vector2 percent = new Vector2(x, y) / (res - 1);
                Vector3 pointOnUnitCube =
                    localUp +
                    (percent.x - .5f) * 2 * axisA +
                    (percent.y - .5f) * 2 * axisB;

                Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;
                float elevation = elevations[i];

                vertices[i] = pointOnUnitSphere * shapeGenerator.GetScaledElevation(elevation);
                uv[i].y = Mathf.InverseLerp(minE, maxE, elevation) * 1.5f;

                if (x != res - 1 && y != res - 1)
                {
                    triangles[triIndex] = i;
                    triangles[triIndex + 1] = i + res + 1;
                    triangles[triIndex + 2] = i + res;

                    triangles[triIndex + 3] = i;
                    triangles[triIndex + 4] = i + 1;
                    triangles[triIndex + 5] = i + res + 1;
                    triIndex += 6;
                }
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.uv = uv;

        Planet.Instance.GenerateCollider();
    }

    public void UpdateUVs(ColourGenerator colourGenerator)
    {
        Vector2[] uv = mesh.uv;
        int res = currentResolution > 0 ? currentResolution : resolution;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                int i = x + y * res;
                Vector2 percent = new Vector2(x, y) / (res - 1);
                Vector3 pointOnUnitCube = localUp + (percent.x - .5f) * 2 * axisA + (percent.y - .5f) * 2 * axisB;
                Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;

                uv[i].x = colourGenerator.BiomePercentFromPoint(pointOnUnitSphere);
            }
        }
        mesh.uv = uv;
    }
}