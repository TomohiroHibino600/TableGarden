// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.AR
{
  internal class _MockCameraController:
    IDisposable
  {
    private const string MouseHorizontalAxis = "Mouse X";
    private const string MouseVerticalAxis = "Mouse Y";

    private IMockARSession _arSession;
    private Transform _cameraTransform;

    public _MockCameraController(Transform cameraTransform)
    {
      ARSessionFactory.SessionInitialized += OnARSessionInitialized;
      _cameraTransform = cameraTransform;
    }

    private bool _isDisposed;
    public void Dispose()
    {
      if (_isDisposed)
        return;

      _isDisposed = true;
      ARSessionFactory.SessionInitialized -= OnARSessionInitialized;
      _UpdateLoop.Tick -= Update;
    }

    private void OnARSessionInitialized(AnyARSessionInitializedArgs args)
    {
      if (!(args.Session is IMockARSession mockSession))
        return;

      _arSession = mockSession;
      _arSession.Deinitialized += _ => _arSession = null;

      _UpdateLoop.Tick += Update;
    }

    private void Update()
    {
      if (_arSession != null && _arSession.State == ARSessionState.Running)
        Move();
    }

    private void Move()
    {
      if (Input.GetMouseButton(1))
      {
        var lookSpeed = _MockCameraConfiguration.LookSpeed;

        var pitchVector = Time.deltaTime * lookSpeed * Input.GetAxis(MouseVerticalAxis);
        _cameraTransform.RotateAround
          (_cameraTransform.position, _cameraTransform.right, -pitchVector);

        var yawVector = Time.deltaTime * lookSpeed * Input.GetAxis(MouseHorizontalAxis);
        _cameraTransform.RotateAround(_cameraTransform.position, Vector3.up, yawVector);
      }

      _cameraTransform.position +=
        Time.deltaTime * _MockCameraConfiguration.MoveSpeed * GetMoveInput();
    }

    private Vector3 GetMoveInput()
    {
      var input = Vector3.zero;

      if (Input.GetKey(KeyCode.W))
        input += _cameraTransform.forward;

      if (Input.GetKey(KeyCode.S))
        input -= _cameraTransform.forward;

      if (Input.GetKey(KeyCode.A))
        input -= _cameraTransform.right;

      if (Input.GetKey(KeyCode.D))
        input += _cameraTransform.right;

      if (Input.GetKey(KeyCode.Q))
        input -= Vector3.up;

      if (Input.GetKey(KeyCode.E))
        input += Vector3.up;

      return input;
    }
  }
}