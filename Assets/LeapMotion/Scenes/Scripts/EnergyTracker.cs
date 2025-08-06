using UnityEngine;
using UnityEngine.UI;

public class EnergyTracker : MonoBehaviour
{
    public Rigidbody[] cubes;
    public RawImage graphPE;
    public RawImage graphKE;

    private Texture2D texturePE;
    private Texture2D textureKE;
    private Color clearColor = new Color(0, 0, 0, 0);

    private int width = 512;
    private int height = 128;
    private int time = 0;

    void Start()
    {
        texturePE = new Texture2D(width, height, TextureFormat.RGBA32, false);
        textureKE = new Texture2D(width, height, TextureFormat.RGBA32, false);

        ClearGraph(texturePE);
        ClearGraph(textureKE);

        graphPE.texture = texturePE;
        graphKE.texture = textureKE;
    }

    void Update()
    {
        float totalKE = 0f;
        float totalPE = 0f;

        foreach (Rigidbody rb in cubes)
        {
            float ke = 0.5f * rb.mass * rb.velocity.sqrMagnitude;
            float pe = rb.mass * Physics.gravity.magnitude * rb.transform.position.y;

            totalKE += ke;
            totalPE += pe;
        }

        PlotBar(textureKE, totalKE, Color.red);
        PlotBar(texturePE, totalPE, Color.green);

        time = (time + 1) % width;
    }

    void PlotBar(Texture2D texture, float value, Color color)
    {
        int yValue = Mathf.Clamp((int)(value * 2f), 0, height - 1);

        for (int y = 0; y <= yValue; y++)
        {
            texture.SetPixel(time, y, color); // 수직 선형 그래프
        }
        texture.Apply();
    }

    void ClearGraph(Texture2D texture)
    {
        Color[] clearPixels = new Color[width * height];
        for (int i = 0; i < clearPixels.Length; i++) clearPixels[i] = Color.black;
        texture.SetPixels(clearPixels);
        texture.Apply();
    }
}
