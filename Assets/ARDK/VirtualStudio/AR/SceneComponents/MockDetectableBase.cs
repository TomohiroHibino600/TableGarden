// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections;
using System.Collections.Generic;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.VirtualStudio.AR.Networking.Mock;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.AR.Mock
{
  public abstract class MockDetectableBase:
    MonoBehaviour
  {
    private readonly object _activeSessionsLock = new object();

    private sealed class _SessionHelper
    {
      public _IMockARSession Session { get; }
      public bool IsLocal { get; }

      private readonly MockDetectableBase _owner;
      private bool _ran;

      private Coroutine _discoveryCoroutine;

      internal _SessionHelper(MockDetectableBase owner, _IMockARSession session, bool isLocal)
      {
        _owner = owner;
        Session = session;
        IsLocal = isLocal;

        Session.Ran += _OnSessionRan;
        Session.Paused += OnSessionPaused;
        Session.Deinitialized += OnSessionDeinitialized;
      }

      internal void _Dispose()
      {
        Session.Ran -= _OnSessionRan;
        Session.Paused -= OnSessionPaused;
      }

      private void StopDiscoveryCoroutine()
      {
        if (_discoveryCoroutine != null)
        {
          if (_owner != null)
            _owner.StopCoroutine(_discoveryCoroutine);

          _discoveryCoroutine = null;
        }
      }

      private void _OnSessionRan(ARSessionRanArgs args)
      {
        if (_ran)
          _owner.OnSessionRanAgain(Session);

        _ran = true;

        StopDiscoveryCoroutine();
        _discoveryCoroutine = _owner.StartCoroutine(nameof(WaitToBeDiscovered), this);
      }

      private void OnSessionPaused(ARSessionPausedArgs args)
      {
        StopDiscoveryCoroutine();
      }

      private void OnSessionDeinitialized(ARSessionDeinitializedArgs args)
      {
        StopDiscoveryCoroutine();
      }
    }

    private Dictionary<IARSession, _SessionHelper> _activeSessionHelpers =
      new Dictionary<IARSession, _SessionHelper>(_ReferenceComparer<IARSession>.Instance);

    [SerializeField]
    protected float _timeToDiscovery = 2.0f;

    // Method provided for implementations to override for initialization steps,
    // should be used instead of Awake or Start.
    // @returns True if initialization was successful
    protected virtual bool Initialize()
    {
      return true;
    }

    // Method called `_timeToDiscovery` seconds after the specified arSession is initialized.
    internal abstract void BeDiscovered(_IMockARSession arSession, bool sessionIsLocal);

    internal abstract void OnSessionRanAgain(_IMockARSession arSession);

    // Declared as private method here to prevent implementations from overriding
    private void Awake()
    {
    }

    // Uses Start instead of Awake to give PlayMode tests a frame to initialize variables
    private void Start()
    {
      if (!gameObject.activeSelf)
        return;

      if (!Initialize())
        return;

      ARSessionFactory.SessionInitialized += OnSessionInitialized;
      ARSessionFactory._NonLocalSessionInitialized += OnSessionInitialized;
    }

    private void OnDestroy()
    {
      ARSessionFactory.SessionInitialized -= OnSessionInitialized;
      ARSessionFactory._NonLocalSessionInitialized -= OnSessionInitialized;

      Dictionary<IARSession, _SessionHelper> helpers;
      lock (_activeSessionsLock)
      {
        helpers = _activeSessionHelpers;
        _activeSessionHelpers = null;
      }

      foreach (var helper in helpers.Values)
        helper._Dispose();
    }

    private void OnSessionInitialized(AnyARSessionInitializedArgs args)
    {
      var session = args.Session;

      if (!(session is _IMockARSession mockSession))
      {
        ARLog._Error("Mock objects can only be detected by mock ARSessions.");
        return;
      }

      var helper = new _SessionHelper(this, mockSession, args._IsLocal);
      lock (_activeSessionsLock)
        _activeSessionHelpers.Add(session, helper);

      session.Deinitialized +=
        (_) =>
        {
          // When the session dies, we only need to remove it from our session list, no need
          // to "dispose" the helper, as no more events will come from the session.
          lock (_activeSessionsLock)
          {
            var helpers = _activeSessionHelpers;
            if (helpers != null)
              helpers.Remove(session);
          }
        };
    }

    private IEnumerator WaitToBeDiscovered(_SessionHelper sessionHelper)
    {
      //      Debug.LogFormat
      //      (
      //        "{0}: Waiting to be discovered by ARSession {1}",
      //        gameObject.name,
      //        sessionHelper.Session.StageIdentifier
      //      );

      if (_timeToDiscovery > 0)
        yield return new WaitForSeconds(_timeToDiscovery);

      BeDiscovered(sessionHelper.Session, sessionHelper.IsLocal);
    }
  }
}
