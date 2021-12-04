// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Configuration;

namespace Niantic.ARDK.Extensions
{
  // Enabling this component while an ARSession is already running
  // will potentially cause the ARSession to re-run.
  public abstract class ARConfigChanger:
    UnityLifecycleDriver
  {
    private _ARSessionChangesCollector _changesCollector;

    internal event Action _ConfigurationChanged;

    protected override void InitializeImpl()
    {
      base.InitializeImpl();

      ARSessionFactory.SessionInitialized += SetConfigChangesCollector;
    }

    protected override void DeinitializeImpl()
    {
      base.DeinitializeImpl();

      ARSessionFactory.SessionInitialized -= SetConfigChangesCollector;
      _changesCollector?.Unregister(this);
    }

    protected void RaiseConfigurationChanged()
    {
      _ConfigurationChanged?.Invoke();
    }

    private void SetConfigChangesCollector(AnyARSessionInitializedArgs args)
    {
      var arSession = (_IARSession) args.Session;
      _changesCollector = arSession._ARSessionChangesCollector;
      _changesCollector.Register(this);

      // The session's _ARSessionChangesCollector is destroyed when the session is disposed.
      arSession.Deinitialized +=
        _ =>
        {
          _changesCollector?.Unregister(this);
          _changesCollector = null;
        };
    }

    internal abstract void _ApplyARConfigurationChange
    (
      _ARSessionChangesCollector._ARSessionRunProperties properties
    );
  }
}
