// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.AR;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.AR.Mock
{
  /// Base class for mocked anchors. Mocked anchors are only detected in the local ARSession.
  public abstract class MockAnchorBase:
    MockDetectableBase
  {
    private HashSet<Guid> _discoveredInSessions = new HashSet<Guid>();

    internal abstract void CreateAndAddAnchorToSession(_IMockARSession arSession);

    internal abstract void RemoveAnchorFromSession(_IMockARSession arSession);

    internal sealed override void BeDiscovered(_IMockARSession arSession, bool isLocal)
    {
      if (!_discoveredInSessions.Contains(arSession.StageIdentifier))
      {
        _discoveredInSessions.Add(arSession.StageIdentifier);
        CreateAndAddAnchorToSession(arSession);
      }
    }

    internal override void OnSessionRanAgain(_IMockARSession arSession)
    {
      if ((arSession.RunOptions & ARSessionRunOptions.RemoveExistingAnchors) != 0)
      {
        _discoveredInSessions.Remove(arSession.StageIdentifier);
        RemoveAnchorFromSession(arSession);
      }
    }
  }
}