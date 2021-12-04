// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using Niantic.ARDK.AR.PlaneGeometry;
using UnityEngine;

namespace Niantic.ARDK.AR.Anchors
{
  [Serializable]
  internal sealed class _SerializableARPlaneAnchor:
    _SerializableARAnchor,
    IARPlaneAnchor
  {
    public _SerializableARPlaneAnchor
    (
      Matrix4x4 transform,
      Guid identifier,
      PlaneAlignment alignment,
      PlaneClassification classification,
      PlaneClassificationStatus classificationStatus,
      Vector3 center,
      Vector3 extent
    ):
      base(transform, identifier)
    {
      Alignment = alignment;
      Classification = classification;
      ClassificationStatus = classificationStatus;
      Center = center;
      Extent = extent;
    }

    public override AnchorType AnchorType
    {
      get { return AnchorType.Plane; }
    }

    public PlaneAlignment Alignment { get; private set; }
    public PlaneClassification Classification { get; private set; }
    public PlaneClassificationStatus ClassificationStatus { get; private set; }
    public Vector3 Center { get; private set; }
    public Vector3 Extent { get; private set; }
    public _SerializableARPlaneGeometry Geometry { get; private set; }

    IARPlaneGeometry IARPlaneAnchor.Geometry
    {
      get
      {
        return Geometry;
      }
    }
  }
}
