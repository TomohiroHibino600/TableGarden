using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Awareness.Depth;
using Niantic.ARDK.Extensions;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDKExamples
{
  public class LegacyDepthExampleManager: MonoBehaviour
  {
    [SerializeField]
    private ARSessionManager _sessionManager;

    [SerializeField]
    private ARDepthManager _depthManager;

    [SerializeField]
    private Camera _arCamera;

    [Header("UI")]
    [SerializeField]
    private GameObject _toggles = null;

    [SerializeField]
    private Text _toggleViewButtonText = null;
    
    [SerializeField]
    private Text _togglePointCloudButtonText = null;

    [SerializeField]
    private Text _toggleInterpolationButtonText = null;
    
    [SerializeField]
    private Text _toggleDepthButtonText = null;

    [SerializeField]
    private RawImage _image;
    
    private bool _interpolate = true;
    private bool _isShowingDepths = false;
    private bool _isShowingPointCloud = false;

    private IARSession _session;
    private Texture2D _texture;

    private IDepthBuffer _latestDepthBuffer;
    private bool _newBufferAvailable;

    private void Start()
    {
      // Reset UI
      _image.enabled = false;
      if (_toggles != null)
        _toggles.SetActive(false);

      Application.targetFrameRate = 60;
      ARSessionFactory.SessionInitialized += OnSessionInitialized;
    }

    private void OnSessionInitialized(AnyARSessionInitializedArgs args)
    {
      if (_session != null)
        _session.FrameUpdated -= OnFrameUpdated;
      
      _session = args.Session;
      _session.FrameUpdated += OnFrameUpdated;
      
      _toggles.SetActive(true);
    }

    private void OnFrameUpdated(FrameUpdatedArgs args)
    {
      var buffer = args.Frame?.Depth;
      if (buffer == null)
        return;

      // Cache the latest buffer, rotated to the screen
      _latestDepthBuffer?.Dispose();
      _latestDepthBuffer = buffer.RotateToScreenOrientation();
      
      // Indicate that there is a new buffer update ready to process
      _newBufferAvailable = true;
    }

    private void Update()
    {
      // Check whether a depth buffer is available
      if (_latestDepthBuffer == null)
        return;
      
      // Whether the buffer needs to be interpolated
      var canInterpolate = _interpolate && _session?.CurrentFrame?.Camera != null;
      
      // We only run this routine if the buffer has changed or we need to interpolate between keyframes
      var shouldUpdateTexture = _newBufferAvailable || canInterpolate;
      if (!shouldUpdateTexture)
        return;

      // Reset state variable
      _newBufferAvailable = false;
      
      IDepthBuffer interpolatedBuffer = null;
      IDepthBuffer fittedBuffer = null;

      if (canInterpolate)
      {
        interpolatedBuffer = _latestDepthBuffer.Interpolate
        (
          _session.CurrentFrame.Camera,
          _arCamera.pixelWidth,
          _arCamera.pixelHeight
        );

        fittedBuffer = interpolatedBuffer.FitToViewport
        (
          _arCamera.pixelWidth,
          _arCamera.pixelHeight
        );
      }
      else
      {
        fittedBuffer = _latestDepthBuffer.FitToViewport
        (
          _arCamera.pixelWidth,
          _arCamera.pixelHeight
        );
      }
      
      if (_isShowingDepths)
      {
        const float maxViewDisp = 4.0f;
        fittedBuffer.CreateOrUpdateTextureARGB32
        (
          ref _texture,
          valueConverter: depth => (1.0f / depth) / maxViewDisp
        );
        
        _image.texture = _texture;
      }
      
      interpolatedBuffer?.Dispose();
      fittedBuffer?.Dispose();
    }

    private void SetDepthVisibility(bool visible)
    {
      _isShowingDepths = visible;
      _image.enabled = _isShowingDepths;

      // Toggle UI elements
      _toggleViewButtonText.text = _isShowingDepths ? "Show Camera" : "Show Depth";
    }

    public void ToggleInterpolation()
    {
      _interpolate = !_interpolate;
      _toggleInterpolationButtonText.text = "Interpolation: " + (_interpolate ? "ON" : "OFF");
    }
    
    public void ToggleShowDepth()
    {
      SetDepthVisibility(!_isShowingDepths);
    }

    public void ToggleShowPointCloud()
    {
      _isShowingPointCloud = !_isShowingPointCloud;
      
      // Toggle UI elements
      _togglePointCloudButtonText.text =
        _isShowingPointCloud ? "Hide Point Cloud" : "Show Current Point Cloud" ;
    }

    public void ToggleSessionDepthFeatures()
    {
      var isEnabled = !_depthManager.enabled;
      _depthManager.enabled = isEnabled;

      // Toggle UI elements
      _toggleDepthButtonText.text = isEnabled ? "Disable Depth" : "Enable Depth";
      SetDepthVisibility(visible: _isShowingDepths && isEnabled);
    }
  }
}
