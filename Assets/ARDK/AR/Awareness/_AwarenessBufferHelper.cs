// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Niantic.ARDK.Utilities.Logging;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;

namespace Niantic.ARDK.AR.Awareness
{
  /// <summary>
  /// Common functions to be used for inference buffers used by the native and serialized code.
  /// NOTE:
  /// - if these functions are templated, iOS increases in 10% cpu usage
  /// - if raw pointers (unsafe) is used instead of arrays, memory usage significantly increases
  /// </summary>
  internal static class _AwarenessBufferHelper
  {
    // Buffer used to prepare values for depth textures
    [ThreadStatic]
    private static Color[] _bufferCache;

    internal static NativeArray<T> RotateToScreenOrientation<T>
    (
      NativeArray<T> src,
      int srcWidth,
      int srcHeight,
      out int newWidth,
      out int newHeight
    )
    where T: struct, IComparable
    {
      newWidth = srcWidth;
      newHeight = srcHeight;

      // Rotate and/or crop

      Func<int, int, int, int, int> dstIdxFn;
      switch (Screen.orientation)
      {
        case ScreenOrientation.Portrait:
          // CW
          dstIdxFn = (w, _, x, y) => x * w + (w - 1 - y);
          newWidth = srcHeight;
          newHeight = srcWidth;
          break;

        case ScreenOrientation.PortraitUpsideDown:
          // CCW
          dstIdxFn = (w, h, x, y) => (h - 1 - x) * w + y;
          newWidth = srcHeight;
          newHeight = srcWidth;
          break;

        case ScreenOrientation.LandscapeLeft:
          // 180a
          dstIdxFn = (w, h, x, y) => (h - 1 - y) * w + (w - 1 - x);
          break;

        default:
          return new NativeArray<T>(src, Allocator.Persistent);
      }

      var rotatedData =
        new NativeArray<T>
        (
          newWidth * newHeight,
          Allocator.Persistent,
          NativeArrayOptions.UninitializedMemory
        );

      for (var y = 0; y < srcHeight; y++)
      {
        for (var x = 0; x < srcWidth; x++)
        {
          var srcIdx = y * srcWidth + x;
          var dstIdx = dstIdxFn(newWidth, newHeight, x, y);
          rotatedData[dstIdx] = src[srcIdx];
        }
      }

      return rotatedData;
    }

    internal static NativeArray<T> _FitToViewport<T>
    (
      NativeArray<T> src,
      int srcWidth,
      int srcHeight,
      int viewportWidth,
      int viewportHeight,
      out int newWidth,
      out int newHeight
    )
    where T: struct, IComparable
    {
      // Ideally this code is shared between native and serializeable
      var srcRatio = srcWidth / (float)srcHeight;
      var trgRatio = viewportWidth / (float)viewportHeight;

      int cropStartX = 0, cropStartY = 0;

      newWidth = (int)srcWidth;
      newHeight = (int)srcHeight;

      if (srcRatio > trgRatio)
      {
        // Portrait: crop along the width
        newWidth = Mathf.FloorToInt(srcHeight * trgRatio);
        if (newWidth < srcWidth)
          cropStartX = Mathf.FloorToInt((srcWidth - newWidth) / 2f);
      }
      else if (srcRatio != trgRatio)
      {
        // Landscape: crop along the height
        newHeight = Mathf.FloorToInt(srcWidth / trgRatio);
        if (newHeight < srcHeight)
          cropStartY = Mathf.FloorToInt((srcHeight - newHeight) / 2f);
      }

      var newData =
        new NativeArray<T>
        (
          newWidth * newHeight,
          Allocator.Persistent,
          NativeArrayOptions.UninitializedMemory
        );

#if ENABLE_UNITY_COLLECTIONS_CHECKS
      NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref newData, AtomicSafetyHandle.Create());
#endif

      // Crop by copying data with offset
      for (var y = 0; y < newHeight; y++)
      {
        for (var x = 0; x < newWidth; x++)
        {
          var srcIdx = (y + cropStartY) * srcWidth + x + cropStartX;
          var dstIdx = (newHeight - 1 - y) * newWidth + x;

          newData[dstIdx] = src[srcIdx];
        }
      }

      return newData;
    }

    /// <summary>
    /// Returns a boolean if input texture has a copy of the observation image buffer.
    /// The texture if successfully copied needs to be deallocated.
    /// Only call on Unity thread.
    /// </summary>
    internal static bool _CreateOrUpdateTextureARGB32
    (
      NativeArray<float> src,
      int width,
      int height,
      ref Texture2D texture,
      FilterMode filterMode,
      Func<float, float> valueConverter = null
    )
    {
      if (width * height != src.Length)
      {
        ARLog._Error("The specified pixel buffer must match the size of the texture.");
        return false;
      }

      if (texture != null && texture.format != TextureFormat.ARGB32)
      {
        ARLog._Error("The texture has already been allocated with a different pixel format.");
        return false;
      }
      
      if (texture == null)
      {
        texture = new Texture2D(width, height, TextureFormat.ARGB32, false, false)
        {
          filterMode = filterMode, wrapMode = TextureWrapMode.Clamp, anisoLevel = 0
        };
      }
      
      if (texture.width != width || texture.height != height)
        texture.Resize(width, height);

      if (texture.filterMode != filterMode)
        texture.filterMode = filterMode;

      // Copy to texture
      _SetColorBuffer(ref _bufferCache, src, valueConverter);
      texture.SetPixels(_bufferCache, 0);

      // Push top GPU
      texture.Apply(false);

      // Success
      return true;
    }

    /// <summary>
    /// Returns a boolean if input texture has a copy of the observation image buffer.
    /// The texture if successfully copied needs to be deallocated.
    /// Only call on Unity thread.
    /// </summary>
    internal static bool _CreateOrUpdateTextureRFloat
    (
      NativeArray<float> src,
      int width,
      int height,
      ref Texture2D texture,
      FilterMode filterMode
    )
    {
      if (width * height != src.Length)
      {
        ARLog._Error("The specified pixel buffer must match the size of the texture.");
        return false;
      }

      if (texture != null && texture.format != TextureFormat.RFloat)
      {
        ARLog._Error("The texture has already been allocated with a different pixel format.");
        return false;
      }

      if (texture == null)
      {
        texture = new Texture2D(width, height, TextureFormat.RFloat, false, false)
        {
          filterMode = filterMode, wrapMode = TextureWrapMode.Clamp, anisoLevel = 0
        };
      }
      
      if (texture.width != width || texture.height != height)
        texture.Resize(width, height);
      
      if (texture.filterMode != filterMode)
        texture.filterMode = filterMode;

      // Copy to texture
      texture.SetPixelData(src, 0);
      
      // Push top GPU
      texture.Apply(false);

      // Success
      return true;
    }

    /// <summary>
    /// Returns a boolean if input texture has a copy of the observation image buffer.
    /// The texture if successfully copied needs to be deallocated.
    /// Only call on Unity thread.
    /// </summary>
    internal static bool _CreateOrUpdateTextureARGB32
    (
      NativeArray<UInt32> src,
      int width,
      int height,
      ref Texture2D texture,
      FilterMode filterMode,
      Func<UInt32, float> valueConverter = null
    )
    {
      if (width * height != src.Length)
      {
        ARLog._Error("The specified pixel buffer must match the size of the texture.");
        return false;
      }

      if (texture != null && texture.format != TextureFormat.ARGB32)
      {
        ARLog._Error("The texture has already been allocated with a different pixel format.");
        return false;
      }

      if (texture == null)
      {
        texture = new Texture2D(width, height, TextureFormat.ARGB32, false, false)
        {
          filterMode = filterMode, wrapMode = TextureWrapMode.Clamp, anisoLevel = 0
        };
      }

      if (texture.width != width || texture.height != height)
        texture.Resize(width, height);

      if (texture.filterMode != filterMode)
        texture.filterMode = filterMode;

      // 32-bit pixel size, in this case use the value converter
      // or fall back to the default ushort to float conversion
      _SetColorBuffer(ref _bufferCache, src, valueConverter);
      texture.SetPixels(_bufferCache, 0);

      // Push top GPU
      texture.Apply(false);

      // Success
      return true;
    }

    /// <summary>
    /// Returns a boolean if input texture has a copy of the observation image buffer.
    /// The texture if successfully copied needs to be deallocated.
    /// Only call on Unity thread.
    /// </summary>
    internal static bool _CreateOrUpdateTextureRFloat
    (
      NativeArray<UInt32> src,
      int width,
      int height,
      ref Texture2D texture,
      FilterMode filterMode
    )
    {
      if (width * height != src.Length)
      {
        ARLog._Error("The specified pixel buffer must match the size of the texture.");
        return false;
      }

      if (texture != null && texture.format != TextureFormat.RFloat)
      {
        ARLog._Error("The texture has already been allocated with a different pixel format.");
        return false;
      }

      if (texture == null)
      {
        texture = new Texture2D(width, height, TextureFormat.RFloat, false, false)
        {
          filterMode = filterMode, wrapMode = TextureWrapMode.Clamp, anisoLevel = 0
        };
      }

      if (texture.width != width || texture.height != height)
        texture.Resize(width, height);

      if (texture.filterMode != filterMode)
        texture.filterMode = filterMode;

      // 32-bit pixel size, copy straight to texture
      texture.SetPixelData(src, 0);

      // Push top GPU
      texture.Apply(false);

      // Success
      return true;
    }

    /// <summary>
    /// Sets the internal color color cache used for texture creation using a source buffer of type Float32.
    /// </summary>
    private static void _SetColorBuffer
    (
      ref Color[] destination,
      NativeArray<float> source,
      Func<float, float> valueConverter = null
    )
    {
      var length = source.Length;
      if (destination == null || destination.Length != length)
        destination = new Color[length];

      var isConversionDefined = valueConverter != null;
      for (var idx = 0; idx < length; idx++)
      {
        var val = isConversionDefined ? valueConverter(source[idx]) : source[idx];
        destination[idx] = new Color(val, val, val, 1);
      }
    }

    /// <summary>
    /// Sets the internal color color cache used for texture creation using a source buffer of type Int16.
    /// If valueConverter is defined, it'll be used to convert the values to 32 bit, otherwise
    /// the method falls back to Convert.ToSingle().
    /// </summary>
    private static void _SetColorBuffer
    (
      ref Color[] destination,
      NativeArray<UInt32> source,
      Func<UInt32, float> valueConverter = null
    ) 
    {
      var length = source.Length;
      if (destination == null || destination.Length != length)
        destination = new Color[length];

      var isConversionDefined = valueConverter != null;
      for (var idx = 0; idx < length; idx++)
      {
        var val = isConversionDefined 
          ? valueConverter(source[idx]) 
          : Convert.ToInt32(source[idx]);
        destination[idx] = new Color(val, val, val, 1);
      }
    }

    /// Produces a matrix that transforms a 3D point from the awareness buffer's
    /// local coordinate space to the world.
    /// @note
    /// The Unity camera's local coordinate system ("camera space") rotates with the phone UI's
    /// orientation. This means, that the x axis of the Unity camera's local coordinate system is
    /// pointing to the right, both
    /// - if the phone UI is in LandscapeLeft (camera left, charge port right), and
    /// - if the phone UI is in Portrait (camera up, charge port down).
    /// The matrix returned by this call is essentially the same matrix as the
    /// camera.cameraToWorldMatrix, but the UI rotation is excluded.
    public static Matrix4x4 CalculateCameraToWorldTransform
    (
      this IAwarenessBuffer forBuffer,
      UnityEngine.Camera camera,
      ScreenOrientation viewOrientation
    )
    {
      // Infer buffer orientation
      var bufferOrientation = forBuffer.Width > forBuffer.Height
        ? ScreenOrientation.LandscapeLeft
        : ScreenOrientation.Portrait;

      // Since the view matrices are rotated to match the interface orientation,
      // we need to rotate them back to match the buffer's orientation.
      var rotation = Matrix4x4.Rotate
      (
        // @note This angle gets inverted later
        Quaternion.AngleAxis
          ((float)GetAngle(bufferOrientation, viewOrientation) * Mathf.Rad2Deg, Vector3.forward)
      );
      
      var rotatedView = rotation * camera.worldToCameraMatrix;
      
      // The buffer's native coordinate system is upside down compared to
      // Unity's 2D coordinate space.
      InvertVerticalAxis(ref rotatedView);
      
      // A Unity camera's forward is pointing towards the user, but we are expecting
      // to use this matrix with points that face the opposite direction
      InvertForwardAxis(ref rotatedView);

      // Invert to produce a matrix that transforms from camera to world
      return rotatedView.inverse; 
    }

    /// Calculates a display resolution for the buffer to preserve square pixels.
    /// The result is the smallest resolution to fit the buffer and keep the aspect
    /// ratio defined by the viewport resolution.
    /// @returns
    ///  The display resolution for the buffer, adjusted to viewport orientation.
    ///  The result might be a cropped or padded resolution.
    public static Resolution CalculateDisplayFrame
    (
      this IAwarenessBuffer forBuffer,
      float viewportWidth,
      float viewportHeight
    )
    {
      // Infer viewport orientation
      var orientation = viewportWidth > viewportHeight
        ? ScreenOrientation.LandscapeLeft
        : ScreenOrientation.Portrait;
      
      // Infer buffer orientation
      var bufferOrientation = forBuffer.Width > forBuffer.Height
        ? ScreenOrientation.LandscapeLeft
        : ScreenOrientation.Portrait;

      Vector2 target = GetRotatedContainer
      (
        viewportWidth,
        viewportHeight,
        orientation,
        bufferOrientation
      );

      // Calculate scaling
      var s = bufferOrientation == ScreenOrientation.Portrait
        ? new Vector2(target.x / (target.y / forBuffer.Height * forBuffer.Width), 1.0f)
        : new Vector2(1.0f, target.y / (target.x / forBuffer.Width * forBuffer.Height));

      return new Resolution
      {
        width = Mathf.FloorToInt(forBuffer.Width * s.x),
        height = Mathf.FloorToInt(forBuffer.Height * s.y)
      };
    }

    /// Calculates an affine transformation matrix to fit the specified buffer to the viewport
    /// @note The buffer's container can be landscape or portrait, but the content of the
    ///       buffer needs to be sensor oriented.
    /// @param forBuffer The buffer to fit to screen.
    /// @param camera The rendering camera.
    /// @param viewOrientation The orientation of the viewport.
    /// @returns An affine matrix to be applied to normalized image coordinates.
    public static Matrix4x4 CalculateDisplayTransform
    (
      this IAwarenessBuffer forBuffer,
      UnityEngine.Camera camera,
      ScreenOrientation viewOrientation
    )
    {
      // Infer buffer orientation
      var bufferOrientation = forBuffer.Width > forBuffer.Height
        ? ScreenOrientation.LandscapeLeft
        : ScreenOrientation.Portrait;

      // Similar to the camera image's display transform, we need to
      // invert the y coordinate (y' = 1 - y) because Unity's texture
      // coordinates start from the bottom rather than from the top.
      return AffineInvertVertical() *
        AffineFit
        (
          forBuffer.Width,
          forBuffer.Height,
          bufferOrientation,
          camera.pixelWidth,
          camera.pixelHeight,
          viewOrientation
        );
    }
    
    /// Calculates an affine transformation matrix to fit the specified buffer to the specified resolution
    /// @note The buffer's container can be landscape or portrait, but the content of the
    ///       buffer needs to be sensor oriented.
    /// @param forBuffer The buffer to fit to screen.
    /// @param width Target width.
    /// @param height Target height.
    /// @param invertVertically Whether vertical flipping is required (e.g. when the target is the screen).
    /// @returns An affine matrix to be applied to normalized image coordinates.
    public static Matrix4x4 CalculateDisplayTransform
    (
      this IAwarenessBuffer forBuffer,
      int width,
      int height,
      bool invertVertically = false
    )
    {
      // Infer buffer orientation
      var bufferOrientation = forBuffer.Width > forBuffer.Height
        ? ScreenOrientation.LandscapeLeft
        : ScreenOrientation.Portrait;

      // Infer target orientation
      var orientation = width > height
        ? ScreenOrientation.LandscapeLeft
        : ScreenOrientation.Portrait;

      // Whether we need to invert vertically
      var invert = invertVertically ? AffineInvertVertical() : Matrix4x4.identity;

      return invert *
        AffineFit
        (
          forBuffer.Width,
          forBuffer.Height,
          bufferOrientation,
          width,
          height,
          orientation
        );
    }

    /// Calculates a transformation matrix for the buffer to synchronize its contents
    /// with the camera pose. The transformation produced by this function is agnostic
    /// to the presentation parameters. To also fit the buffer to the rendering viewport,
    /// combine this transform with the one returned by CalculateDisplayTransform().
    /// @param forBuffer The buffer to fit to screen.
    /// @param camera The rendering camera.
    /// @param viewOrientation The orientation of the viewport.
    /// @param backProjectionDistance The normalized distance between the near and far view.
    /// @returns An projective matrix to be applied to normalized image coordinates.
    public static Matrix4x4 CalculateInterpolationTransform
    (
      this IAwarenessBuffer forBuffer,
      UnityEngine.Camera camera,
      ScreenOrientation viewOrientation,
      float backProjectionDistance = 0.9f
    )
    {
      // Inspect buffer
      var aspectRatio = (float)forBuffer.Width / forBuffer.Height;
      var bufferOrientation = aspectRatio > 1.0f
        ? ScreenOrientation.LandscapeLeft
        : ScreenOrientation.Portrait;

      // Calculate fov
      var fov = 2.0f *
        Mathf.Atan(forBuffer.Height / (2.0f * forBuffer.Intrinsics.FocalLength.y)) *
        Mathf.Rad2Deg;
      
      #if UNITY_EDITOR
      // Default to 60 until mock intrinsics is fixed in the editor
      fov = 60.0f;
      #endif
      
      // To keep the homography agnostic to the screen, we need to create a
      // projection with a view aspect ratio being the same as the buffer container.
      var projection = Matrix4x4.Perspective
      (
        fov,
        aspectRatio,
        AwarenessParameters.DefaultNear,
        AwarenessParameters.DefaultFar
      );

      // Since the view matrices are rotated to match the interface orientation,
      // we need to rotate them back to match the buffer's orientation.
      var rotation = Matrix4x4.Rotate
      (
        Quaternion.AngleAxis
          ((float)GetAngle(bufferOrientation, viewOrientation) * Mathf.Rad2Deg, Vector3.forward)
      );

      // The view matrix for the buffer is converted with NARConversions.FromNARToUnity()
      // which is incorrect for view matrices, since those represent an exception in Unity.
      // To truly convert the matrix to Unity's convention, we additionally have to invert 
      // the Z axis as well. We do this in ConvertMatrixToUnitySystem().
      // Then, we rotate both matrices to match the original coordinate system of the image,
      // which is landscape, sensor to the left, home button to the right.
      var referencePose = rotation * ConvertMatrixToUnitySystem(forBuffer.ViewMatrix);
      var targetPose = rotation * camera.worldToCameraMatrix;
      
      // Additionally, we need to flip the vertical axis, because Unity's 2D coordinate
      // system starts from the bottom rather than from the top, and we flip the Y
      // coordinate in the display transform matrix. We could avoid this step if we flip
      // the contents of the buffer during texture creation, but then this interpolation
      // matrix could only be used in the shader and not on the CPU buffer.
      InvertVerticalAxis(ref referencePose);
      InvertVerticalAxis(ref targetPose);
      
      return CalculateHomography
      (
        referencePose,
        targetPose,
        projection,
        backProjectionDistance
      );
    }

    /// Calculates a projective transformation to sync normalized image coordinates
    /// with the target pose.
    /// @param referencePose The original pose associated with the image.
    /// @param targetPose The the pose to synchronize with.
    /// @param backProjectionPlane The normalized distance of the back-projection plane
    ///        between the near and far clipping planes. Lower values make translations
    ///        more perceptible.
    /// @returns A projective transformation matrix to be applied to normalized image coordinates.
    private static Matrix4x4 CalculateHomography
    (
      Matrix4x4 referencePose,
      Matrix4x4 targetPose,
      Matrix4x4 projection,
      float backProjectionPlane
    )
    {
      var worldPosition00 = ViewToWorld(Vector2.zero, referencePose, projection, backProjectionPlane);
      var worldPosition01 = ViewToWorld(Vector2.up, referencePose, projection, backProjectionPlane);
      var worldPosition11 = ViewToWorld(Vector2.one, referencePose, projection, backProjectionPlane);
      var worldPosition10 = ViewToWorld(Vector2.right, referencePose, projection, backProjectionPlane);
      
      var p00 = WorldToView(worldPosition00, targetPose, projection);
      var p01 = WorldToView(worldPosition01, targetPose, projection);
      var p11 = WorldToView(worldPosition11, targetPose, projection);
      var p10 = WorldToView(worldPosition10, targetPose, projection);

      var a = p10.x - p11.x;
      var b = p01.x - p11.x;
      var c = p00.x - p01.x - p10.x + p11.x;
      var d = p10.y - p11.y;
      var e = p01.y - p11.y;
      var f = p00.y - p01.y - p10.y + p11.y;

      var g = (c * d - a * f) / (b * d - a * e);
      var h = (c * e - b * f) / (a * e - b * d);

      return new Matrix4x4
      (
        new Vector4(p10.x - p00.x + h * p10.x, p10.y - p00.y + h * p10.y, h),
        new Vector4(p01.x - p00.x + g * p01.x, p01.y - p00.y + g * p01.y, g),
        new Vector4(p00.x, p00.y, 1.0f),
        new Vector4(0, 0, 0, 1)
      ).inverse;
    }

    /// Transforms a single viewport coordinate to world space.
    /// @param viewPosition Coordinate to transform.
    /// @param view View matrix.
    /// @param projection Projection matrix.
    /// @param distanceNormalized Defines how far the transformed point should be from the view.
    /// @returns The point in world space.
    private static Vector3 ViewToWorld
    (
      Vector2 viewPosition,
      Matrix4x4 view,
      Matrix4x4 projection,
      float distanceNormalized
    )
    {
      var clipCoordinates = new Vector4
      (
        viewPosition.x * 2.0f - 1.0f,
        viewPosition.y * 2.0f - 1.0f,
        distanceNormalized * 2.0f - 1.0f,
        1.0f
      );

      var viewProjectionInverted = (projection * view).inverse;
      var result = viewProjectionInverted * clipCoordinates;

      if (Mathf.Approximately(result.w, 0.0f))
      {
        return Vector3.zero;
      }

      result.w = 1.0f / result.w;
      result.x *= result.w;
      result.y *= result.w;
      result.z *= result.w;

      return result;
    }

    /// Projects the specified world position to view space.
    /// @param worldPosition Position to transform.
    /// @param view View matrix.
    /// @param projection Projection matrix.
    /// @returns The world position in view space.
    private static Vector2 WorldToView(Vector3 worldPosition, Matrix4x4 view, Matrix4x4 projection)
    {
      var position = new Vector4(worldPosition.x, worldPosition.y, worldPosition.z, 1.0f);
      var transformed = view * position;
      var projected = projection * transformed;

      if (Mathf.Approximately(projected.w, 0.0f))
      {
        return Vector2.zero;
      }

      projected.w = 1.0f / projected.w;
      projected.x *= projected.w;
      projected.y *= projected.w;
      projected.z *= projected.w;

      return new Vector2(projected.x * 0.5f + 0.5f, projected.y * 0.5f + 0.5f);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix4x4 ConvertMatrixToUnitySystem(Matrix4x4 matrix)
    {
#if UNITY_EDITOR
      return matrix;
#else
      var result = matrix;
      InvertForwardAxis(ref result);
      return result;
#endif
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InvertVerticalAxis(ref Matrix4x4 matrix)
    {
      matrix.m10 *= -1.0f;
      matrix.m11 *= -1.0f;
      matrix.m12 *= -1.0f;
      matrix.m13 *= -1.0f;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InvertForwardAxis(ref Matrix4x4 matrix)
    {
      matrix.m20 *= -1.0f;
      matrix.m21 *= -1.0f;
      matrix.m22 *= -1.0f;
      matrix.m23 *= -1.0f;
    }
    
    /// Calculates the angle to rotate from one screen orientation to another in radians.
    /// @param from Original orientation.
    /// @param to Target orientation.
    /// @returns Angle to rotate to get from one orientation to the other. 
    private static double GetAngle(ScreenOrientation from, ScreenOrientation to)
    {
      const double rotationUnit = Math.PI / 2.0;
      return (ScreenOrientationLookup[to] - ScreenOrientationLookup[from]) * rotationUnit;
    }

    private static readonly IDictionary<ScreenOrientation, int> ScreenOrientationLookup =
      new Dictionary<ScreenOrientation, int>
      {
        {
          ScreenOrientation.LandscapeLeft, 0
        },
        {
          ScreenOrientation.Portrait, 1
        },
        {
          ScreenOrientation.LandscapeRight, 2
        },
        {
          ScreenOrientation.PortraitUpsideDown, 3
        }
      };
    
    /// Calculates an affine transformation to rotate from one screen orientation to another
    /// around the pivot.
    /// @param from Original orientation.
    /// @param to Target orientation.
    /// @returns An affine matrix to be applied to normalized image coordinates.
    private static Matrix4x4 GetRotation(ScreenOrientation from, ScreenOrientation to)
    {
      // Rotate around the center
      var pivot = new Vector2(0.5f, 0.5f);
      return AffineTranslation(pivot) * 
        AffineRotation(GetAngle(from, to)) *
        AffineTranslation(-pivot);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix4x4 AffineRotation(double rad)
    {
      return new Matrix4x4
      (
        new Vector4((float)Math.Cos(rad), (float)Math.Sin(rad), 0, 0),
        new Vector4((float)-Math.Sin(rad), (float)Math.Cos(rad), 0, 0),
        new Vector4(0, 0, 1, 0),
        new Vector4(0, 0, 0, 1)
      );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix4x4 AffineTranslation(Vector2 translation)
    {
      return new Matrix4x4
      (
        new Vector4(1, 0, 0, 0),
        new Vector4(0, 1, 0, 0),
        new Vector4(0, 0, 1, 0),
        new Vector4(translation.x, translation.y, 0, 1)
      );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix4x4 AffineInvertHorizontal()
    {
      return new Matrix4x4
      (
        new Vector4(-1, 0, 0, 0),
        new Vector4(0, 1, 0, 0),
        new Vector4(0, 0, 1, 0),
        new Vector4(1, 0, 0, 1)
      );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix4x4 AffineInvertVertical()
    {
      return new Matrix4x4
      (
        new Vector4(1, 0, 0, 0),
        new Vector4(0, -1, 0, 0),
        new Vector4(0, 0, 1, 0),
        new Vector4(0, 1, 0, 1)
      );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix4x4 AffineScale(Vector2 scale)
    {
      return new Matrix4x4
      (
        new Vector4(scale.x, 0, 0, 0),
        new Vector4(0, scale.y, 0, 0),
        new Vector4(0, 0, 1, 0),
        new Vector4(0, 0, 0, 1)
      );
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector2 GetRotatedContainer
    (
      float sourceWidth,
      float sourceHeight,
      ScreenOrientation sourceOrientation,
      ScreenOrientation targetOrientation
    )
    {
      if (sourceOrientation == ScreenOrientation.LandscapeLeft)
      {
        return 
          targetOrientation == ScreenOrientation.LandscapeLeft ||
          targetOrientation == ScreenOrientation.LandscapeRight
            ? new Vector2(sourceWidth, sourceHeight)
            : new Vector2(sourceHeight, sourceWidth);
      }

      return 
        targetOrientation == ScreenOrientation.Portrait ||
        targetOrientation == ScreenOrientation.PortraitUpsideDown
          ? new Vector2(sourceWidth, sourceHeight)
          : new Vector2(sourceHeight, sourceWidth);
    }

    // Returns an affine transformation such that if multiplied
    // with normalized coordinates of the target coordinate frame,
    // the results are normalized coordinates in the source
    // coordinate frame.
    // @notes
    //  E.g. if source is defined by an awareness buffer and
    //  target is defined by the viewport, normalized viewport
    //  coordinates multiplied with this transform will result
    //  in normalized coordinates in the awareness buffer.
    //  Further more, if source is defined by the AR image and
    //  the target is the viewport, this matrix will be equivalent
    //  to the display transform provided by the ARKit framework.
    private static Matrix4x4 AffineFit
    (
      float sourceWidth,
      float sourceHeight,
      ScreenOrientation sourceOrientation,
      float targetWidth,
      float targetHeight,
      ScreenOrientation targetOrientation
    )
    {
      var rotatedContainer = GetRotatedContainer
        (sourceWidth, sourceHeight, sourceOrientation, targetOrientation);
      
      // Calculate scaling
      var targetRatio = targetWidth / targetHeight;
      var s = targetRatio < 1.0f
        ? new Vector2(targetWidth / (targetHeight / rotatedContainer.y * rotatedContainer.x), 1.0f)
        : new Vector2(1.0f, targetHeight / (targetWidth / rotatedContainer.x * rotatedContainer.y));
      
      var rotate = GetRotation(from: sourceOrientation, to: targetOrientation);
      var scale = AffineScale(s);
      var translate = AffineTranslation(new Vector2((1.0f - s.x) * 0.5f, (1.0f - s.y) * 0.5f));
      
      return rotate * translate * scale;
    }
  }
}
