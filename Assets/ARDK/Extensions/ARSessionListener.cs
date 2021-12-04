// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;

namespace Niantic.ARDK.Extensions
{
  /// Helper for UnityLifecycleDrivers that need to listen to ARSession events.
  /// Callbacks needed to be added to the IARSession instance whenever:
  ///   The IARSession is initialized
  ///   AND
  ///   The UnityLifecycleDriver is enabled
  /// And the callbacks need to be removed whenever the UnityLifecycleDriver is disabled (but not
  /// when the session is deinitialized, because that automatically cleans up the callbacks on its
  /// own).
  /// Implement ListenToSession to add callbacks to events, and StopListeningToSession to remove the
  /// callbacks. Both guarantee that _arSession will be a valid non-null ARSession.
  public abstract class ARSessionListener: 
    ARConfigChanger
  {
    /// The latest initialized ARSession, reset to null whenever the session is deinitialized.
    /// It is not necessarily running.
    protected IARSession _arSession = null;

    /// Implement this method to add callbacks to _arSession.
    protected abstract void ListenToSession();
    /// Implement this method to remove any callbacks added to _arSession in ListenToSession.
    protected abstract void StopListeningToSession();
    /// Override this method with any cleanup behaviour for session deinitialization. 
    protected virtual void OnSessionDeinitialized() {}

    protected override void InitializeImpl()
    {
      ARSessionFactory.SessionInitialized += OnSessionInitialized;
      
      base.InitializeImpl();
    }

    protected override void DeinitializeImpl()
    {
      if (AreFeaturesEnabled && _arSession != null)
        StopListeningToSession();

      ARSessionFactory.SessionInitialized -= OnSessionInitialized;
      if(_arSession != null)
        _arSession.Deinitialized -= _OnSessionDeinitialized;
      
      _arSession = null;
      
      base.DeinitializeImpl();
    }

    private void OnSessionInitialized(AnyARSessionInitializedArgs args)
    {
      if (_arSession != null)
        _arSession.Deinitialized -= _OnSessionDeinitialized;
      
      _arSession = args.Session;
      _arSession.Deinitialized += _OnSessionDeinitialized;

      // _arSession is guaranteed to not be null, so check the other condition.
      if (AreFeaturesEnabled)
        ListenToSession();
    }

    private void _OnSessionDeinitialized(ARSessionDeinitializedArgs args)
    {
      OnSessionDeinitialized();
      _arSession = null;
    }

    protected override void EnableFeaturesImpl()
    {
      base.EnableFeaturesImpl();
      
      // Features are guaranteed to be enabled, so check the other condition.
      if (_arSession != null)
        ListenToSession();
    }

    protected override void DisableFeaturesImpl()
    {
      base.DisableFeaturesImpl();
      
      if (_arSession != null)
        StopListeningToSession();
    }
  }
}
