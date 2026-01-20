using System;
using UnityEngine;

public class Planet : MonoBehaviour
{
    public static Planet Instance;
    public int resolution = 10;
    public bool autoUpdate = true;
    public enum FaceRenderMask { All, Top, Bottom, Left, Right, Front, Back };
    public FaceRenderMask faceRenderMask;

    public ShapeSettings shapeSettings;
    public ColourSettings colourSettings;

    public Gradient defaultOceanGradient;

    public Texture2D[] possibleColorTextures; 
    public Texture2D[] possibleNormalTextures;
    public Texture2D[] possibleOcclusionTextures;

    [HideInInspector]
    public bool shapeSettingsFoldout;
    [HideInInspector]
    public bool colourSettingsFoldout;

    ShapeGenerator shapeGenerator = new ShapeGenerator();
    ColourGenerator colourGenerator = new ColourGenerator();

    [SerializeField, HideInInspector]
    MeshFilter[] meshFilters;
    TerrainFace[] terrainFaces;
    MeshCollider meshCollider;
    MaterialPropertyBlock propertyBlock;
    MeshRenderer[] renderers;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Initialize()
    {
        // ---------------- RANDOM SHAPE ----------------
        var noise = shapeSettings.noiseLayers[0].noiseSettings.simpleNoiseSettings;
        noise.strength = UnityEngine.Random.Range(0.03f, 0.08f);
        noise.numLayers = UnityEngine.Random.Range(3, 8);
        noise.baseRoughness = UnityEngine.Random.Range(0.7f, 1f);
        noise.roughness = UnityEngine.Random.Range(0.7f, 1f) + noise.baseRoughness / 2;
        noise.persistence = UnityEngine.Random.Range(0.55f, 0.75f);
        noise.minValue = UnityEngine.Random.Range(0.6f, 1f);

        // ---------------- RANDOM BIOME ----------------
        colourSettings.biomeColourSettings.blendAmount = UnityEngine.Random.Range(0.1f, 1f);
        colourSettings.biomeColourSettings.noiseOffset = UnityEngine.Random.Range(0f, 5f);
        colourSettings.biomeColourSettings.noiseStrength = UnityEngine.Random.Range(0.05f, 0.35f);

        var biomeGradient = CreateRandomGradient();
        colourSettings.biomeColourSettings.biomes[0].gradient = biomeGradient;

        // ---------------- SHADER RANDOMIZATION ----------------
        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        propertyBlock.Clear();

        int seed = UnityEngine.Random.Range(0, possibleColorTextures.Length);

        propertyBlock.SetFloat("_PlanetSeed", seed);
        propertyBlock.SetTexture("_PlanetTexture", possibleColorTextures[seed]);
        propertyBlock.SetTexture("_PlanetNormalMap", possibleNormalTextures[seed]);
        propertyBlock.SetTexture("_PlanetAmbientOcclusion", possibleOcclusionTextures[seed]);

        // ---------------- MESH SETUP ----------------
        if (meshFilters == null || meshFilters.Length == 0)
            meshFilters = new MeshFilter[6];

        terrainFaces = new TerrainFace[6];

        Vector3[] directions =
        {
            Vector3.up, Vector3.down,
            Vector3.left, Vector3.right,
            Vector3.forward, Vector3.back
        };

        for (int i = 0; i < 6; i++)
        {
            if (meshFilters[i] == null)
            {
                GameObject meshObj = new GameObject("mesh");
                meshObj.transform.parent = transform;
                meshObj.transform.localPosition = Vector3.zero;
                meshObj.transform.localRotation = Quaternion.identity;

                var renderer = meshObj.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = colourSettings.planetMaterial;

                meshFilters[i] = meshObj.AddComponent<MeshFilter>();
                meshFilters[i].sharedMesh = new Mesh();
            }

            terrainFaces[i] =
                new TerrainFace(shapeGenerator, meshFilters[i].sharedMesh, resolution, directions[i], this);

            bool renderFace = faceRenderMask == FaceRenderMask.All || (int)faceRenderMask - 1 == i;
            meshFilters[i].gameObject.SetActive(renderFace);
        }

        // ---------------- CACHE RENDERERS ----------------
        if (renderers == null || renderers.Length == 0)
        {
            renderers = new MeshRenderer[6];
            for (int i = 0; i < 6; i++)
                renderers[i] = meshFilters[i].GetComponent<MeshRenderer>();
        }

        foreach (var r in renderers)
        {
            r.SetPropertyBlock(propertyBlock);
        }

        // ---------------- UPDATE GENERATORS ----------------
        shapeGenerator.UpdateSettings(shapeSettings);
        colourGenerator.UpdateSettings(colourSettings);
    }

    void Start()
    {
        GeneratePlanet();
    }

    void Update()
    {
        Vector3 cameraPos = Camera.main.transform.position;
        
        foreach (TerrainFace face in terrainFaces)
        {
            face.UpdateLOD(cameraPos);
        }
    }
    
    [ContextMenu("Generate Planet")]
    public void GeneratePlanet()
    {
        Initialize();
        GenerateMesh();
        GenerateColours();
    }

    public void OnShapeSettingsUpdated()
    {
        if (autoUpdate)
        {
            Initialize();
            GenerateMesh();
        }
    }

    public void OnColourSettingsUpdated()
    {
        if (autoUpdate)
        {
            Initialize();
            GenerateColours();
        }
    }

    void GenerateMesh()
    {
        for (int i = 0; i < 6; i++)
        {
            if (meshFilters[i].gameObject.activeSelf)
            {
                terrainFaces[i].ConstructMesh();
            }
        }

        colourGenerator.UpdateElevation(shapeGenerator.elevationMinMax);
    }

    void GenerateColours()
    {
        colourGenerator.UpdateColours();
        for (int i = 0; i < 6; i++)
        {
            if (meshFilters[i].gameObject.activeSelf)
            {
                terrainFaces[i].UpdateUVs(colourGenerator);
            }
        }
    }

    public void GenerateCollider()
    {
        if (meshCollider == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }

        Mesh combinedMesh = new Mesh();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        for (int i = 0; i < meshFilters.Length; i++)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
        }

        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        combinedMesh.CombineMeshes(combine);
        meshCollider.sharedMesh = combinedMesh;
    }

    Gradient CreateRandomGradient()
    {
        Gradient gradient = new Gradient();

        int keyCount = UnityEngine.Random.Range(2, 6);

        GradientColorKey[] colorKeys = new GradientColorKey[keyCount];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[keyCount];

        for (int i = 0; i < keyCount; i++)
        {
            float time = i / (float)(keyCount - 1);

            colorKeys[i] = new GradientColorKey(
                UnityEngine.Random.ColorHSV(
                    0f, 0.65f,     //hue
                    0.1f, 0.5f,   //saturation
                    0.1f, 0.85f    //value
                ),
                time
            );

            alphaKeys[i] = new GradientAlphaKey(1f, time);
        }

        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }
}