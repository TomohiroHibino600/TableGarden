using Niantic.ARDK.AR.HitTest;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;
using Niantic.ARDK.Networking.HLAPI.Authority;
using Niantic.ARDK.Networking.HLAPI.Object.Unity;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;

using UnityEngine;
using UnityEngine.UI;

public class ArNetworkingStarter : MonoBehaviour
{
  [SerializeField]
  private ARHitTestResultType _hitTestType = ARHitTestResultType.ExistingPlaneUsingExtent;

  [Header("Scene Objects")]
  [SerializeField]
  private Button _connectButton = null;

  [SerializeField]
  private InputField _sessionInputField = null;

  [SerializeField]
  private Camera _arCamera = null;

  [Header("Spawner Configuration")]
  [SerializeField]
  private NetworkedUnityObject _spawnableCubePrefab = null;

  private IARNetworking _arNetworking;

  private void Awake()
  {
    ARNetworkingFactory.ARNetworkingInitialized += OnARNetworkingInitialized;
  }

  private void OnARNetworkingInitialized(AnyARNetworkingInitializedArgs initializedArgs)
  {
    _connectButton.gameObject.SetActive(false);
    _sessionInputField.gameObject.SetActive(false);

    _arNetworking = initializedArgs.ARNetworking;
    var networking = _arNetworking.Networking;

    networking.Connected +=
      args => Debug.LogFormat("Connection succeeded. Joined as {0}.", args.IsHost ? "host" : "peer");

    networking.ConnectionFailed +=
      args => Debug.Log("Connection failed with error: " + args.ErrorCode);

    networking.PeerDataReceived += OnNetworkingReceivedDataFromPeer;
  }


  private void OnNetworkingReceivedDataFromPeer(PeerDataReceivedArgs args)
  {
    Debug.LogFormat
    (
      "Received from {0}: tag {1}, length {2}",
      args.Peer.Identifier,
      tag,
      args.DataLength
    );
  }

  private void Update()
  {
    if (_arNetworking == null)
      return;

    if (PlatformAgnosticInput.touchCount <= 0)
      return;

    var touch = PlatformAgnosticInput.GetTouch(0);
    if (touch.phase != TouchPhase.Began)
      return;

    if (!TryChangeColor(touch))
      TrySpawnOnPlane(touch);
  }

  // Use a Unity raycast to see if player tapped on the color-changing cube
  private bool TryChangeColor(Touch touch)
  {
    var worldRay = _arCamera.ScreenPointToRay(touch.position);
    RaycastHit hit;

    if (Physics.Raycast(worldRay, out hit, 1000f))
    {
      var colorChanger = hit.transform.GetComponentInParent<ColorChanger>();

      if (colorChanger != null)
      {
        var selfRole = colorChanger.Owner.Auth.RoleOfPeer(_arNetworking.Networking.Self);
        if (selfRole == Role.Authority)
        {
          colorChanger.ChangeToRandomColor();
          return true;
        }
      }
    }

    return false;
  }

  // Use the ARFrame's hittest to see if player tapped on a plane
  private void TrySpawnOnPlane(Touch touch)
  {
    var currentFrame = _arNetworking.ARSession.CurrentFrame;
    if (currentFrame == null)
      return;

    // Hit test to see if player tapped on a detected plane
    var results =
      currentFrame.HitTest
      (
        _arCamera.pixelWidth,
        _arCamera.pixelHeight,
        touch.position,
        _hitTestType
      );

    if (results.Count == 0)
      return;

    // Use the closest result
    var hitTransform = results[0].WorldTransform;

    Debug.Log("Hit: " + hitTransform.ToPosition().ToString("F4"));

    // The position y-value offset needed to spawn your prefab at the
    // correct height (not intersecting with the plane) will depend on
    // your prefab. Here we're spawning the cube so that it's bottom face
    // rests on the plane.
    var hitPosition = hitTransform.ToPosition();
    hitPosition.y += _spawnableCubePrefab.transform.localScale.y / 2.0f;

    // Network spawn and claim authority. When claiming authority like this,
    // make sure OwnedByHost is false on the prefab's AuthBehaviour component.
    _spawnableCubePrefab.NetworkSpawn
    (
      hitPosition,
      hitTransform.ToRotation(),
      Role.Authority
    );
  }

  private void OnDestroy()
  {
    ARNetworkingFactory.ARNetworkingInitialized -= OnARNetworkingInitialized;
  }
}