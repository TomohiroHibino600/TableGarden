using System;

using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.HLAPI.Data;
using Niantic.ARDK.Networking.HLAPI.Object;
using Niantic.ARDK.Networking.HLAPI.Object.Unity;

using UnityEngine;

namespace TableGarden
{
    [RequireComponent(typeof(AuthBehaviour))]
    public class CosmosBehaviour : NetworkedBehaviour
    {
        // Flags for whether the game has started and if the local player is the host
        private bool _gameStart;
        private bool _isHost;

        private IMultipeerNetworking _networking;

        // Set up the initial conditions
        internal void GameStart(bool isHost)
        {
            _isHost = isHost;
            _gameStart = true;

            if (!_isHost)
                return;
        }

        protected override void SetupSession(out Action initializer, out int order)
        {
            initializer = () =>
            {
                var auth = Owner.Auth;
                var descriptor = auth.AuthorityToObserverDescriptor(TransportType.UnreliableUnordered);

                new UnreliableBroadcastTransformPacker
                (
                  "netTransform",
                  transform,
                  descriptor,
                  TransformPiece.Position,
                  Owner.Group
                );
            };

            order = 0;
        }
    }
}