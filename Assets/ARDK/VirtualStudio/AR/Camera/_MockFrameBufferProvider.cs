// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Camera;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.AR.Awareness.Depth;
using Niantic.ARDK.AR.Awareness.Semantics;
using Niantic.ARDK.AR.Frame;
using Niantic.ARDK.AR.Image;

using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;
using UnityEngine.Rendering;

#if ARDK_HAS_URP
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.Universal;
using Niantic.ARDK.Rendering.SRP;

using UnityEngine.Rendering.Universal;
#endif

namespace Niantic.ARDK.VirtualStudio.AR.Mock
{
  internal sealed class _MockFrameBufferProvider:
    IDisposable
  {
    // ARSession data
    private readonly _MockARSession _arSession;
    private readonly float _timeBetweenFrames;
    private float _timeSinceLastFrame;
    private _SerializableARCamera _cachedSerializedCamera;
    private readonly Transform _camerasRoot;

    // Image buffer
    private Camera _imageCamera;
    private CameraIntrinsics _imageIntrinsics;
    private Transform _imageTransform;
    private int _imageWidth;
    private int _imageHeight;
    private RenderTexture _imageRT;

    // Depth buffer
    private bool _generateDepth;
    private Camera _depthCamera;
    private CameraIntrinsics _depthIntrinsics;
    private RenderTexture _depthRT;
    private RenderTexture _depthOnlyRT;
    private Shader _depthToDisparityShader;
    private Material _depthToDisparityMaterial;

    // Semantics buffer
    private bool _generateSemantics;
    private Camera _semanticsCamera;
    private CameraIntrinsics _semanticsIntrinsics;
    private Shader _semanticsShader;
    private RenderTexture _semanticsRT;
    private Texture2D _semanticsTex;
    private string[] _channelNames;

    // Awareness Model Params (updated ARDK 0.10):

    // The Width and Height values here are actually switched, because mock depth buffers don't support rotating to screen
    // orientation and assume the given buffer is correctly oriented for a Portrait screen.
    private const int _ModelWidth = 144;
    private const int _ModelHeight = 256;

    private const float _ModelNearDistance = 0.2f;
    private const float _ModelFarDistance = 100f;

    public const string MOCK_LAYER_NAME = "ARDK_MockWorld";

    public const string MOCK_LAYER_MISSING_MSG =
      "Add the ARDK_MockWorld layer to the Layers list (Edit > ProjectSettings > Tags and Layers)" +
      " in order to render in Mock AR sessions.";

    public _MockFrameBufferProvider(_MockARSession mockARSession, Transform camerasRoot)
    {
      _arSession = mockARSession;
      _arSession.Ran += CheckRunConfiguration;
      _timeBetweenFrames = 1f / _MockCameraConfiguration.FPS;

      _camerasRoot = camerasRoot;
      InitializeImageGeneration();

      _UpdateLoop.Tick += Update;
    }

    private void CheckRunConfiguration(ARSessionRanArgs args)
    {
      if (_arSession.Configuration is IARWorldTrackingConfiguration worldTrackingConfiguration)
      {
        _generateDepth = worldTrackingConfiguration.IsDepthEnabled;
        _generateSemantics = worldTrackingConfiguration.IsSemanticSegmentationEnabled;
      }
      else
      {
        _generateDepth = false;
        _generateSemantics = false;
      }

      if (_generateDepth && _depthCamera == null)
        InitializeDepthGeneration();

      if (_generateSemantics && _semanticsCamera == null)
        InitializeSemanticsGeneration();

      if (_depthCamera != null)
      {
        _depthCamera.enabled = _generateDepth;
        _depthCamera.depthTextureMode =
          _generateDepth ? DepthTextureMode.Depth : DepthTextureMode.None;
      }

      if (_semanticsCamera != null)
      {
        _semanticsCamera.enabled = _generateSemantics;
      }
    }

    private void InitializeImageGeneration()
    {
      _imageCamera = CreateCameraBase("Image");

      // Default Unity values
      _imageCamera.nearClipPlane = 0.3f;
      _imageCamera.farClipPlane = 1000f;

      _imageTransform = _imageCamera.transform;
      _imageWidth = _imageCamera.pixelWidth;
      _imageHeight = _imageCamera.pixelHeight;

      _imageCamera.usePhysicalProperties = true;
      var f = _imageCamera.focalLength;
      _imageIntrinsics = new CameraIntrinsics(f, f, _imageWidth / 2f, _imageHeight / 2f);

      var resolution = new Resolution { width = _imageWidth, height = _imageHeight };

      _cachedSerializedCamera =
        new _SerializableARCamera
        (
          TrackingState.Normal,
          TrackingStateReason.None,
          resolution,
          resolution,
          _imageIntrinsics,
          _imageIntrinsics,
          _imageTransform.localToWorldMatrix,
          _imageCamera.projectionMatrix,
          1f,
          _imageCamera.projectionMatrix,
          _imageCamera.worldToCameraMatrix
        );

      // Set up rendering offscreen to render texture.
      _imageRT =
        new RenderTexture
        (
          _imageWidth,
          _imageHeight,
          16,
          RenderTextureFormat.BGRA32
        );

      _imageRT.Create();
      _imageCamera.targetTexture = _imageRT;
    }

    private void InitializeDepthGeneration()
    {
      _depthCamera = CreateAwarenessCamera("Depth");
      _depthCamera.depthTextureMode = DepthTextureMode.Depth;

      _depthRT =
      new RenderTexture
      (
        _ModelWidth,
        _ModelHeight,
        16,
        RenderTextureFormat.Depth
      );

    _depthOnlyRT =
      new RenderTexture
      (
        _ModelWidth,
        _ModelHeight,
        0,
        RenderTextureFormat.RFloat
      );

      _depthToDisparityShader = Resources.Load<Shader>("UnityToMetricDepth");
      _depthToDisparityMaterial = new Material(_depthToDisparityShader);

      var farDividedByNear = _ModelFarDistance / _ModelNearDistance;
      _depthToDisparityMaterial.SetFloat("_ZBufferParams_Z", (-1 + farDividedByNear) / _ModelFarDistance);
      _depthToDisparityMaterial.SetFloat("_ZBufferParams_W", 1 / _ModelFarDistance);

      _depthCamera.targetTexture = _depthRT;

      _depthIntrinsics = CalculateModelIntrinsics
      (
        _imageIntrinsics,
        new Vector2(_imageWidth, _imageHeight),
        new Vector2(_ModelWidth, _ModelHeight)
      );
    }

    private void InitializeSemanticsGeneration()
    {
      _semanticsCamera = CreateAwarenessCamera("Semantics");
      _semanticsCamera.clearFlags = CameraClearFlags.SolidColor;
      _semanticsCamera.backgroundColor = new Color(0, 0, 0, 0);

      _semanticsRT =
        new RenderTexture
        (
          _ModelWidth,
          _ModelHeight,
          16,
          RenderTextureFormat.ARGB32
        );

      _semanticsRT.Create();
      _semanticsCamera.targetTexture = _semanticsRT;

      _semanticsShader = Resources.Load<Shader>("Segmentation");
      _semanticsCamera.SetReplacementShader(_semanticsShader, String.Empty);

      _semanticsTex = new Texture2D(_ModelWidth, _ModelHeight, TextureFormat.ARGB32, false);

      _semanticsIntrinsics = CalculateModelIntrinsics
      (
        _imageIntrinsics,
        new Vector2(_imageWidth, _imageHeight),
        new Vector2(_ModelWidth, _ModelHeight)
      );

      SetupReplacementRenderer();

      _channelNames = Enum.GetNames(typeof(MockSemanticLabel.ChannelName));
    }

    private void SetupReplacementRenderer()
    {
#if ARDK_HAS_URP
      if (!_RenderPipelineInternals.IsUniversalRenderPipelineEnabled)
        return;

      var rendererIndex =
        _RenderPipelineInternals.GetRendererIndex
        (
          _RenderPipelineInternals.REPLACEMENT_RENDERER_NAME
        );

      if (rendererIndex < 0)
      {
        ARLog._Error
        (
          "Cannot generate mock semantic segmentation buffers unless the ArdkUrpAssetRenderer" +
          " is added to the Renderer List."
        );

        return;
      }

      _semanticsCamera.GetUniversalAdditionalCameraData().SetRenderer(rendererIndex);
#endif
    }

    private Camera CreateCameraBase(string name)
    {
      var cameraObject = new GameObject(name);
      cameraObject.transform.SetParent(_camerasRoot);

      var camera = cameraObject.AddComponent<Camera>();
      camera.depth = int.MinValue;

      if (LayerMask.NameToLayer(MOCK_LAYER_NAME) < 0)
      {
        ARLog._Error(MOCK_LAYER_MISSING_MSG);
        return null;
      }

      camera.cullingMask = LayerMask.GetMask(MOCK_LAYER_NAME);

      return camera;
    }

    private Camera CreateAwarenessCamera(string name)
    {
      var camera = CreateCameraBase(name);

      camera.nearClipPlane = _ModelNearDistance;
      camera.farClipPlane = _ModelFarDistance;

      // Aspect ratios are different, so need to change field of view
      // to make sure depth camera's image lines up with the image camera's
      camera.usePhysicalProperties = true;
      var modelAspectRatio = _ModelWidth / (float)_ModelHeight;
      if (modelAspectRatio > _imageCamera.aspect)
        camera.fieldOfView *= modelAspectRatio / _imageCamera.aspect;

      return camera;
    }

    private bool _isDisposed;
    public void Dispose()
    {
      if (_isDisposed)
        return;

      _isDisposed = true;

      _imageRT.Release();

      if (_depthCamera != null)
      {
        GameObject.Destroy(_depthCamera.gameObject);
        _depthRT.Release();
        _depthOnlyRT.Release();
      }

      if (_semanticsCamera != null)
      {
        GameObject.Destroy(_semanticsCamera.gameObject);
        _semanticsRT.Release();
      }
    }

    private void Update()
    {
      if (_arSession != null && _arSession.State == ARSessionState.Running)
      {
        _timeSinceLastFrame += Time.deltaTime;
        if (_timeSinceLastFrame >= _timeBetweenFrames)
        {
          _timeSinceLastFrame = 0;

          _cachedSerializedCamera._estimatedViewMatrix = _imageTransform.worldToLocalMatrix;
          _cachedSerializedCamera.Transform = _imageTransform.localToWorldMatrix;

          var serializedFrame =
            new _SerializableARFrame
            (
              capturedImageBuffer: _GetImageBuffer(),
              depthBuffer: _generateDepth ? _GetDepthBuffer() : null,
              semanticBuffer: _generateSemantics ? _GetSemanticBuffer() : null,
              camera: _cachedSerializedCamera,
              lightEstimate: null,
              anchors: null,
              maps: null,
              worldScale: 1.0f,

              // Image camera's resolution is identical to the Screen's,
              // so just the identity matrix works here
              estimatedDisplayTransform: Matrix4x4.identity
            );

          _arSession.UpdateFrame(serializedFrame);
        }
      }
    }

    private _SerializableImageBuffer _GetImageBuffer()
    {
      var imageData =
        new NativeArray<byte>
        (
          _imageWidth * _imageHeight * 4,
          Allocator.Persistent,
          NativeArrayOptions.UninitializedMemory
        );

#if ENABLE_UNITY_COLLECTIONS_CHECKS
      NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref imageData, AtomicSafetyHandle.Create());
#endif

      AsyncGPUReadback.RequestIntoNativeArray(ref imageData, _imageRT).WaitForCompletion();

      var plane =
        new _SerializableImagePlane
        (
          imageData,
          _imageWidth,
          _imageHeight,
          _imageWidth * 4,
          4
        );

      var buffer =
        new _SerializableImageBuffer
        (
          ImageFormat.BGRA,
          new _SerializableImagePlanes(new[] { plane }),
          75
        );

      return buffer;
    }

    private _SerializableDepthBuffer _GetDepthBuffer()
    {
      Graphics.Blit(_depthRT, _depthOnlyRT, _depthToDisparityMaterial);

      var depthData = new NativeArray<float>
      (
        _ModelWidth * _ModelHeight,
        Allocator.Persistent,
        NativeArrayOptions.UninitializedMemory
      );

#if ENABLE_UNITY_COLLECTIONS_CHECKS
      NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref depthData, AtomicSafetyHandle.Create());
#endif

      AsyncGPUReadback.RequestIntoNativeArray(ref depthData, _depthOnlyRT).WaitForCompletion();

      var buffer = new _SerializableDepthBuffer
      (
        _ModelWidth,
        _ModelHeight,
        true,
        _depthCamera.worldToCameraMatrix,
        depthData,
        _ModelNearDistance,
        _ModelFarDistance,
        _depthIntrinsics
      )
      {
        IsRotatedToScreenOrientation = true
      };

      return buffer;
    }

    private _SerializableSemanticBuffer _GetSemanticBuffer()
    {
       var data = new NativeArray<uint>
       (
         _ModelWidth * _ModelHeight,
         Allocator.Persistent,
         NativeArrayOptions.UninitializedMemory
       );

#if ENABLE_UNITY_COLLECTIONS_CHECKS
      NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref data, AtomicSafetyHandle.Create());
#endif

      // Doing this in the CPU is slower, but I couldn't figure out how to get
      // the correct uint value out of a shader. Performance is sufficient.
      var currRT = RenderTexture.active;
      RenderTexture.active = _semanticsRT;

      _semanticsTex.ReadPixels(new Rect(0, 0, _ModelWidth, _ModelHeight), 0, 0);
      _semanticsTex.Apply();

      RenderTexture.active = currRT;

      var byteArray = _semanticsTex.GetPixels32();
      for (var i = 0; i < byteArray.Length; i++)
      {
        data[i] = MockSemanticLabel.ToInt(byteArray[i]);
      }

      var buffer = new _SerializableSemanticBuffer
      (
        _ModelWidth,
        _ModelHeight,
        true,
        _semanticsCamera.worldToCameraMatrix,
        data,
        _channelNames,
        _semanticsIntrinsics
      )
      {
        IsRotatedToScreenOrientation = true
      };

      return buffer;
    }

    private CameraIntrinsics CalculateModelIntrinsics
    (
      CameraIntrinsics inputIntrinsics,
      Vector2 inputResolution,
      Vector2 outputResolution
    )
    {
      var ratio =
        new Vector2
        (
          outputResolution.x / inputResolution.x,
          outputResolution.y / inputResolution.y
        );

      return
        new CameraIntrinsics
        (
          inputIntrinsics.FocalLength.x * ratio.x,
          inputIntrinsics.FocalLength.y * ratio.y,
          inputIntrinsics.PrincipalPoint.x * ratio.x,
          inputIntrinsics.PrincipalPoint.y * ratio.y
        );
    }
  }
}