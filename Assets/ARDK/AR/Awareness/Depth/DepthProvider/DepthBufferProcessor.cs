using Niantic.ARDK.AR.ARSessionEventArgs;

using UnityEngine;

namespace Niantic.ARDK.AR.Awareness.Depth
{
  public class DepthBufferProcessor: 
    AwarenessBufferProcessor<IDepthBuffer>,
    IDepthBufferProcessor
  {
    private IARSession _session;
    private readonly UnityEngine.Camera _camera;

    #region Public API

    public DepthBufferProcessor(UnityEngine.Camera camera)
    {
      _camera = camera;
      ARSessionFactory.SessionInitialized += OnARSessionInitialized;
    }

    public float MinDepth
    {
      get => AwarenessBuffer?.NearDistance ?? float.PositiveInfinity;
    }

    public float MaxDepth
    {
      get => AwarenessBuffer?.FarDistance ?? float.PositiveInfinity;
    }

    /// Returns the eye depth of the specified pixel.
    /// @param screenX Horizontal coordinate in screen space.
    /// @param screenY Vertical coordinate in screen space.
    /// @returns The perpendicular depth from the camera plane if exists or
    ///   float.PositiveInfinity if the depth information is unavailable.
    public float GetDepth(int screenX, int screenY)
    {
      var depthBuffer = AwarenessBuffer;
      if (depthBuffer == null)
        return float.PositiveInfinity;

      var x = screenX + 0.5f;
      var y = screenY + 0.5f;
      var uv = new Vector4(x / Screen.width, y / Screen.height, 1.0f, 1.0f);

      // Sample the depth buffer
      // The sampler transform may contain re-projection. We do this because
      // we need the depth value at the pixel predicted with interpolation.
      return depthBuffer.Sample(uv, SamplerTransform);
    }

    /// Returns the distance of the specified pixel from the camera origin.
    /// @param screenX Horizontal coordinate in screen space.
    /// @param screenY Vertical coordinate in screen space.
    /// @returns The distance from the camera if exists or float.PositiveInfinity if the
    ///   depth information is unavailable.
    public float GetDistance(int screenX, int screenY)
    {
      var depthBuffer = AwarenessBuffer;
      if (depthBuffer == null)
        return float.PositiveInfinity;

      var x = screenX + 0.5f;
      var y = screenY + 0.5f;
      var uv = new Vector4(x / Screen.width, y / Screen.height, 1.0f, 1.0f);

      // Sample the depth buffer
      // The sampler transform may contain re-projection. We do this because
      // we need the depth value at the pixel predicted with interpolation.
      var depth = depthBuffer.Sample(uv, SamplerTransform);

      // Retrieve point in camera space
      var pointRelativeToCamera = depth * BackProjectionTransform.MultiplyPoint(uv);

      // Calculate distance
      return pointRelativeToCamera.magnitude;
    }

    /// Returns the world position of the specified pixel.
    /// @param screenX Horizontal coordinate in screen space.
    /// @param screenY Vertical coordinate in screen space.
    /// @returns World position if exists or Vector3.zero if the depth information is unavailable.
    public Vector3 GetWorldPosition(int screenX, int screenY)
    {
      var depthBuffer = AwarenessBuffer;
      if (depthBuffer == null)
        return Vector3.zero;

      var x = screenX + 0.5f;
      var y = screenY + 0.5f;
      var uv = new Vector4(x / Screen.width, y / Screen.height, 1.0f, 1.0f);

      // Sample the depth buffer
      // The sampler transform may contain re-projection. We do this because
      // we need the depth value at the pixel predicted with interpolation.
      var depth = depthBuffer.Sample(uv, SamplerTransform);

      // Retrieve point in camera space
      var pointRelativeToCamera = depth * BackProjectionTransform.MultiplyPoint(uv);

      // Transform to world coordinates
      return CameraToWorldTransform.MultiplyPoint(pointRelativeToCamera);
    }

    /// Returns the surface normal of the specified pixel.
    /// @param screenX Horizontal coordinate in screen space.
    /// @param screenY Vertical coordinate in screen space.
    /// @returns Normal if exists or Vector3.up if the depth information is unavailable.
    public Vector3 GetSurfaceNormal(int screenX, int screenY)
    {
      var depthBuffer = AwarenessBuffer;
      if (depthBuffer == null)
        return Vector3.up;

      var sMax = Mathf.Max(Screen.width, Screen.height);
      var bMax = Mathf.Max((int)depthBuffer.Width, (int)depthBuffer.Height);
      var screenDelta = Mathf.CeilToInt((float)sMax / bMax) + 1;

      var a = GetWorldPosition(screenX, screenY);
      var b = GetWorldPosition(screenX + screenDelta, screenY);
      var c = GetWorldPosition(screenX, screenY + screenDelta);

      return Vector3.Cross(a - b, c - a).normalized;
    }
    
    public void CopyToAlignedTextureARGB32(ref Texture2D texture, ScreenOrientation orientation)
    {
      // Get a typed buffer
      IDepthBuffer depthBuffer = AwarenessBuffer;
      float max = depthBuffer.FarDistance;
      float min = depthBuffer.NearDistance;
      
      // Acquire the affine transform for the buffer
      var transform = SamplerTransform;

      // Call base method
      CreateOrUpdateTextureARGB32
      (
        ref texture,
        orientation,
        
        // The sampler function needs to be defined such that given a destination
        // texture coordinate, what color needs to be written to that position?
        sampler: uv =>
        {
          // Sample raw depth from the buffer
          var depth = depthBuffer.Sample(uv, transform);
          
          // Normalize depth
          var val = (depth - min) / (max - min);
          
          // Copy to value to color channels
          return new Color(val, val, val, 1.0f);
        }
      );
    }
    
    public void CopyToAlignedTextureRFloat(ref Texture2D texture, ScreenOrientation orientation)
    {
      // Get a typed buffer
      IDepthBuffer depthBuffer = AwarenessBuffer;

      // Acquire the affine transform for the buffer
      var transform = SamplerTransform;

      // Call base method
      CreateOrUpdateTextureRFloat
      (
        ref texture,
        orientation,
        
        // The sampler function needs to be defined such that given a destination
        // texture coordinate, what value needs to be written to that position?
        sampler: uv => depthBuffer.Sample(uv, transform)
      );
    }

    protected override void Dispose(bool disposing)
    {
      base.Dispose(disposing);
      ARSessionFactory.SessionInitialized -= OnARSessionInitialized;
      if (_session != null)
        _session.FrameUpdated -= OnFrameUpdated;
    }

  #endregion

  #region Implementation

    private void OnARSessionInitialized(AnyARSessionInitializedArgs args)
    {
      if (_session != null)
        _session.FrameUpdated -= OnFrameUpdated;

      _session = args.Session;
      _session.FrameUpdated += OnFrameUpdated;
    }

    private void OnFrameUpdated(FrameUpdatedArgs args)
    {
      var frame = args.Frame;

      ProcessFrame
      (
        buffer: frame.Depth,
        arCamera: frame.Camera,
        unityCamera: _camera
      );
    }

  #endregion
  }
}