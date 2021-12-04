// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections;

using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Niantic.ARDK.VirtualStudio.AR.Mock
{
  /// Attach this component to a GameObject in a mock environment scene and adjust the `center` and
  /// `rotation` values so the Gizmo lies flat on top of the plane. A mock `IARPlaneAnchor` will
  /// become discovered and raised through the `IARSession.AnchorsAdded` event after
  /// '_timeToDiscovery' seconds have passed.
  /// @note
  ///   Mock plane anchors are one-sided, so make sure to configure the `rotation` value so the
  ///   plane's normal vector (indicated by the arrow) points in the correct direction.
  public sealed class MockPlaneAnchor:
    MockAnchorBase
  {
    [SerializeField]
    private Vector3 _center = Vector3.zero;

    [Tooltip("All values should be increments of 90")]
    [SerializeField]
    private Vector3 _rotation = Vector3.zero;

    [SerializeField]
    private PlaneAlignment _planeAlignment = default(PlaneAlignment);

    [Header("Plane Classification Options")]
    [SerializeField]
    private bool _shouldSuccessfullyClassify = true;

    [SerializeField]
    private PlaneClassification _planeClassification = default(PlaneClassification);

    // Time (in seconds) it takes for this anchor's PlaneClassificationStatus to settle after
    //   it is discovered.
    [SerializeField]
    private float _timeToClassify = 1f;

    private _SerializableARPlaneAnchor _anchor;

    private Vector3 _localScale = new Vector3(1, 0.001f, 1);

    protected override bool Initialize()
    {
      if (_planeAlignment == PlaneAlignment.Unknown)
      {
        ARLog._Error("MockPlaneAnchors with Unknown plane alignments will not be discovered.");
        return false;
      }

      return true;
    }

    internal override void CreateAndAddAnchorToSession(_IMockARSession arSession)
    {
      if (_anchor == null)
      {
        var localTransform =
          Matrix4x4.TRS
          (
            _center,
            Quaternion.Euler(_rotation).normalized,
            _localScale
          );

        var worldTransform = transform.localToWorldMatrix * localTransform;

        var anchorTransform =
          Matrix4x4.TRS
          (
            worldTransform.ToPosition(),
            worldTransform.ToRotation(),
            Vector3.one
          );

        var anchorScale = new Vector3(worldTransform.lossyScale.x, 0, worldTransform.lossyScale.z);

        _anchor =
          new _SerializableARPlaneAnchor
          (
            anchorTransform,
            Guid.NewGuid(),
            _planeAlignment,
            PlaneClassification.None,
            PlaneClassificationStatus.Undetermined,
            Vector3.zero,
            anchorScale
          );
      }

      if (arSession.AddAnchor(_anchor))
        StartCoroutine(ClassifyPlaneInSession(arSession));
    }

    internal override void RemoveAnchorFromSession(_IMockARSession arSession)
    {
      arSession.RemoveAnchor(_anchor);
    }

    private IEnumerator ClassifyPlaneInSession(_IMockARSession arSession)
    {
      yield return new WaitForSeconds(_timeToClassify);

      if (_anchor == null)
        yield break;

      var classification =
        _shouldSuccessfullyClassify ? _planeClassification : PlaneClassification.None;

      var classificationStatus =
        _shouldSuccessfullyClassify
          ? PlaneClassificationStatus.Known
          : PlaneClassificationStatus.Unknown;

      var updatedAnchor =
        new _SerializableARPlaneAnchor
        (
          _anchor.Transform,
          _anchor.Identifier,
          _anchor.Alignment,
          classification,
          classificationStatus,
          _anchor.Center,
          _anchor.Extent
        );

      _anchor = updatedAnchor;
      arSession.UpdateAnchor(_anchor);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
      Gizmos.matrix = transform.localToWorldMatrix;

      var orientation = Quaternion.Euler(_rotation);
      Gizmos.DrawWireCube(_center, orientation * _localScale);

      var worldCenter = transform.position;
      Handles.ArrowHandleCap
      (
        0,
        worldCenter,
        transform.rotation * orientation * Quaternion.LookRotation(Vector3.up),
        .25f,
        EventType.Repaint
      );
    }
#endif
  }
}
