using UnityEngine;

public sealed class AlbumCardVisualFallback : MonoBehaviour
{
    [SerializeField] private Material sourceMaterial;
    [SerializeField] private Texture2D cardTexture;
    [SerializeField] private Vector2 size = new Vector2(0.2f, 0.3f);
    [SerializeField] private bool forceInBuild = true;

    private GameObject fallbackObject;
    private Mesh fallbackMesh;
    private Material fallbackMaterial;

    private void Awake()
    {
        if (Application.isEditor && forceInBuild)
        {
            return;
        }

        CreateFallbackVisual();
    }

    private void OnDestroy()
    {
        if (fallbackMaterial != null)
        {
            Destroy(fallbackMaterial);
        }

        if (fallbackMesh != null)
        {
            Destroy(fallbackMesh);
        }
    }

    private void CreateFallbackVisual()
    {
        if (fallbackObject != null)
        {
            return;
        }

        fallbackObject = new GameObject("Card Runtime Visual");
        fallbackObject.transform.SetParent(FindExistingVisualParent(), false);

        MeshFilter meshFilter = fallbackObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = fallbackObject.AddComponent<MeshRenderer>();

        fallbackMesh = CreateCardMesh();
        fallbackMesh.name = "Card Runtime Quad";
        meshFilter.sharedMesh = fallbackMesh;

        fallbackMaterial = CreateCardMaterial();
        meshRenderer.sharedMaterial = fallbackMaterial;
    }

    private Mesh CreateCardMesh()
    {
        float halfWidth = Mathf.Max(0.001f, size.x) * 0.5f;
        float halfHeight = Mathf.Max(0.001f, size.y) * 0.5f;

        Mesh mesh = new Mesh();
        mesh.vertices = new[]
        {
            new Vector3(0f, -halfHeight, -halfWidth),
            new Vector3(0f, -halfHeight, halfWidth),
            new Vector3(0f, halfHeight, halfWidth),
            new Vector3(0f, halfHeight, -halfWidth),
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private Material CreateCardMaterial()
    {
        Material material;
        if (sourceMaterial != null)
        {
            material = new Material(sourceMaterial);
        }
        else
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Texture");
            }

            if (shader == null)
            {
                return null;
            }

            material = new Material(shader);
        }

        if (cardTexture != null)
        {
            material.SetTexture("_BaseMap", cardTexture);
            material.SetTexture("_MainTex", cardTexture);
        }

        material.SetColor("_BaseColor", Color.white);
        material.SetColor("_Color", Color.white);
        material.SetFloat("_Cull", 0f);
        return material;
    }

    private Transform FindExistingVisualParent()
    {
        MeshRenderer existingRenderer = GetComponentInChildren<MeshRenderer>(true);
        return existingRenderer != null ? existingRenderer.transform : transform;
    }
}
