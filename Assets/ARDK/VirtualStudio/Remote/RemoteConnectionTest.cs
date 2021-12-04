// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections;
using System.Text;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VirtualStudio.Remote;
using Niantic.ARDK.VirtualStudio.Remote.Data;

using UnityEngine;
using UnityEngine.UI;

using Random = System.Random;

namespace Niantic.ARDK.VirtualStudio.Remote
{
    /// <summary>
    /// Handles the mobile display logic of Remote Connection.
    /// </summary>
    public class RemoteConnectionTest : MonoBehaviour
    {
        private int pinLength = 6;

        private bool _hasSelectedMode;
        private IARSession _activeARSession;

        private Random _random = new Random();

        private float count = 2f;

        private void Awake()
        {
            SubscribeToLifecycleEvents();

            // Setup selection stage.
            //Camera.main.backgroundColor = Color.black;

            _RemoteConnection.Deinitialized += Reset;
        }

        private void StartConnection(_RemoteConnection.ConnectionMethod connectionMethod)
        {
            string pin = null;
            if (connectionMethod == _RemoteConnection.ConnectionMethod.Internet)
            {
                // Build a pin.
                var pinBuilder = new StringBuilder();

                for (var i = 0; i < pinLength; i++)
                {
                    var nextChar = (char)_random.Next('A', 'Z');
                    pinBuilder.Append(nextChar);
                }

                pin = pinBuilder.ToString();
            }

            _hasSelectedMode = true;

            // Connect using settings.
            _RemoteConnection.InitIfNone(connectionMethod);
            _RemoteConnection.Connect(pin);
        }

        private void Reset()
        {
            _hasSelectedMode = false;
            //Camera.main.backgroundColor = Color.blue;
        }

        private void Update()
        {
            if (!_hasSelectedMode)
                return;

            // UI is not visible when camera feed is rendering
            if (_activeARSession != null && _activeARSession.State == ARSessionState.Running)
                return;

            //connection
            count -= Time.deltaTime;
            if (!_RemoteConnection.IsConnected & count <= 0f) {
                StartConnection(_RemoteConnection.ConnectionMethod.USB);
                count = 2.0f;
            }

            // Update connection info.
            /**if (_RemoteConnection.IsConnected)
            {
                Camera.main.backgroundColor = Color.black;
            }
            else if (_RemoteConnection.IsReady)
            {
                Camera.main.backgroundColor = Color.blue;
            }
            else
            {
                Camera.main.backgroundColor = Color.magenta;
            }**/
        }

        private void OnDestroy()
        {
            _RemoteConnection.Deinitialize();
        }

        private void SubscribeToLifecycleEvents()
        {
            ARSessionFactory.SessionInitialized +=
              args =>
              {
                  ARLog._Debug("[Remote] ARSession Initialized: " + args.Session.StageIdentifier);
                  _activeARSession = args.Session;
                  _activeARSession.Deinitialized += _ => _activeARSession = null;

                  args.Session.Deinitialized +=
              deinitializedArgs =>
                  {
                      ARLog._Debug("[Remote] ARSession Deinitialized.");
                  };
              };

            MultipeerNetworkingFactory.NetworkingInitialized +=
              args =>
              {
                  ARLog._Debug("[Remote] MultipeerNetworking Initialized: " + args.Networking.StageIdentifier);
                  UpdateNetworkingsCount();

                  args.Networking.Deinitialized +=
              deinitializedArgs =>
                  {
                      ARLog._Debug("[Remote] MultipeerNetworking Deinitialized.");

                      var networkingsCount = UpdateNetworkingsCount();
                  };
              };

            ARNetworkingFactory.ARNetworkingInitialized +=
              args =>
              {
                  ARLog._Debug("[Remote] ARNetworking Initialized: " + args.ARNetworking.ARSession.StageIdentifier);

                  args.ARNetworking.Deinitialized +=
              deinitializedArgs =>
                  {
                      ARLog._Debug("[Remote] ARNetworking Deinitialized.");
                  };
              };
        }

        private readonly Color FADED_WHITE = new Color(1, 1, 1, 0.5f);
        private void UpdateStatusVisual(Text statusText, bool isConstructed)
        {
            if (statusText != null)
            {
                statusText.fontStyle = isConstructed ? FontStyle.Bold : FontStyle.Normal;
                statusText.color = isConstructed ? Color.white : FADED_WHITE;
            }
        }

        private int UpdateNetworkingsCount()
        {
            var networkingsCount = MultipeerNetworkingFactory.Networkings.Count;
            return networkingsCount;
        }
    }
}

