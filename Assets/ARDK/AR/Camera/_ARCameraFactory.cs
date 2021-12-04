// Copyright 2021 Niantic, Inc. All Rights Reserved.

using UnityEngine;

namespace Niantic.ARDK.AR.Camera
{
  internal static class _ARCameraFactory
  {
    internal static _SerializableARCamera _AsSerializable(this IARCamera source)
    {
      if (source == null)
        return null;

      var possibleResult = source as _SerializableARCamera;
      if (possibleResult != null)
        return possibleResult;

      var estimatedProjectionMatrix =
        source.CalculateProjectionMatrix
        (
          Screen.orientation,
          Screen.width,
          Screen.height,
          0.001f,
          100.0f
        );

      return
        new _SerializableARCamera
        (
          trackingState: source.TrackingState,
          trackingStateReason: source.TrackingStateReason,
          // Passing scaled image resolution here, instead of native resolution
          // TODO(kmori): will revisit if setting encoding res won't bring side effects
          imageResolution: _VideoStreamHelper._EncodingImageResolution,
          cpuImageResolution: _VideoStreamHelper._EncodingImageResolution,
          transform: source.Transform,
          projectionMatrix: source.ProjectionMatrix,
          intrinsics: source.Intrinsics,
          cpuIntrinsics: source.CPUIntrinsics,
          worldScale: source.WorldScale,
          estimatedProjectionMatrix: estimatedProjectionMatrix,
          estimatedViewMatrix: source.GetViewMatrix(Screen.orientation)
        );
    }
  }
}
