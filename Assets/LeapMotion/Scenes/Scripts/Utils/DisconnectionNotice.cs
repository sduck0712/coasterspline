using UnityEngine;
using UnityEngine.UI;
using Leap;

public class DisconnectionNotice : MonoBehaviour
{
    public float fadeInTime = 1.0f;
    public float fadeOutTime = 1.0f;
    public AnimationCurve fade;
    public int waitFrames = 10;
    public Texture2D embeddedReplacementImage;
    public Color onColor = Color.white;

    private Controller leap_controller_;
    private float fadedIn = 0.0f;
    private int frames_disconnected_ = 0;
    private RawImage rawImage;

    void Start()
    {
        leap_controller_ = new Controller();
        rawImage = GetComponent<RawImage>();
        SetAlpha(0.0f);
    }

    void SetAlpha(float alpha)
    {
        if (rawImage != null)
            rawImage.color = Color.Lerp(Color.clear, onColor, alpha);
    }

    bool IsConnected()
    {
        return leap_controller_.IsConnected;
    }

    bool IsEmbedded()
    {
        DeviceList devices = leap_controller_.Devices;
        if (devices.Count == 0)
            return false;
        return devices[0].IsEmbedded;
    }

    void Update()
    {
        if (rawImage == null) return;

        if (embeddedReplacementImage != null && IsEmbedded())
        {
            rawImage.texture = embeddedReplacementImage;
        }

        if (IsConnected())
            frames_disconnected_ = 0;
        else
            frames_disconnected_++;

        if (frames_disconnected_ < waitFrames)
            fadedIn -= Time.deltaTime / fadeOutTime;
        else
            fadedIn += Time.deltaTime / fadeInTime;
        fadedIn = Mathf.Clamp(fadedIn, 0.0f, 1.0f);

        SetAlpha(fade.Evaluate(fadedIn));
    }
}
