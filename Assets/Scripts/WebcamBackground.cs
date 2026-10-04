using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// On the phone, ARCameraBackground already draws the real camera behind the game.
/// When no AR session is running (Unity Editor / desktop) this shows the computer's
/// webcam behind the game instead, so the game is always played over a real camera image.
/// </summary>
public class WebcamBackground : MonoBehaviour
{
    public Camera targetCamera;
    [Tooltip("Seconds to wait for an AR session before falling back to the webcam.")]
    public float arStartTimeout = 2f;

    WebCamTexture _webcam;
    RawImage _image;
    AspectRatioFitter _fitter;
    GameObject _canvasObject;

    public static bool UsingWebcam { get; private set; }

    IEnumerator Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        float t = 0f;
        while (t < arStartTimeout && ARSession.state < ARSessionState.Ready) { t += Time.unscaledDeltaTime; yield return null; }
        if (ARSession.state >= ARSessionState.Ready || targetCamera == null) yield break; // real AR camera feed is active

        // Ask for permission where the platform needs it.
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        if (WebCamTexture.devices.Length == 0)
        {
            Debug.LogWarning("WebcamBackground: no AR session and no webcam found, so the background stays plain.");
            yield break;
        }

        string device = WebCamTexture.devices[0].name;
        foreach (var d in WebCamTexture.devices) if (!d.isFrontFacing) { device = d.name; break; }
        _webcam = new WebCamTexture(device, 1280, 720, 30);
        _webcam.Play();
        BuildBackground();
        UsingWebcam = true;
    }

    void BuildBackground()
    {
        // A screen-space canvas placed just inside the far clip plane renders behind every game object.
        _canvasObject = new GameObject("WebcamBackgroundCanvas", typeof(Canvas), typeof(CanvasScaler));
        var canvas = _canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = targetCamera;
        canvas.planeDistance = targetCamera.farClipPlane * 0.95f;
        canvas.sortingOrder = -1000;

        var imageObject = new GameObject("Webcam", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        imageObject.transform.SetParent(_canvasObject.transform, false);
        _image = imageObject.GetComponent<RawImage>();
        _image.texture = _webcam;
        _image.raycastTarget = false;
        _fitter = imageObject.GetComponent<AspectRatioFitter>();
        _fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        var rect = (RectTransform)imageObject.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    void Update()
    {
        if (_webcam == null || _image == null || _webcam.width < 32) return;
        _fitter.aspectRatio = (float)_webcam.width / _webcam.height;
        _image.rectTransform.localEulerAngles = new Vector3(0f, 0f, -_webcam.videoRotationAngle);
        _image.uvRect = _webcam.videoVerticallyMirrored ? new Rect(0, 1, 1, -1) : new Rect(0, 0, 1, 1);
    }

    void OnDestroy()
    {
        if (_webcam != null) _webcam.Stop();
        if (_canvasObject != null) Destroy(_canvasObject);
        UsingWebcam = false;
    }
}
