using System;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.HLAPI.Data;
using Niantic.ARDK.Networking.HLAPI.Object;
using Niantic.ARDK.Networking.HLAPI.Object.Unity;
using UnityEngine;

public class ColorChanger : NetworkedBehaviour
  {
    private Material _mat;

    // The value that we want replicated (ie changes are propagated across all connected
    //   peers, and all peers are notified when a change occurs).
    private INetworkedField<Color> _currColor;

    public void ChangeToRandomColor()
    {
      _currColor.Value = ColorFromGuid(Guid.NewGuid());
    }

    private static Color ColorFromGuid(Guid id)
    {
      var colorGen = new System.Random(id.GetHashCode());

      return new Color
      (
        (float) colorGen.NextDouble(),
        (float) colorGen.NextDouble(),
        (float) colorGen.NextDouble()
      );
    }

    protected override void SetupSession(out Action initializer, out int order)
    {
      // The SetupSession method on all sibling components is called when the parent
      // NetworkedUnityObject is spawned. Setting the order defines the order in which the
      // initializer callback on this component will be invoked compared to that for its siblings.
      // Relative order doesn't matter for this component's initializer, so we just set it to 0.
      order = 0;

      initializer = () =>
      {
        var descriptor = Owner.Auth.AuthorityToObserverDescriptor(TransportType.ReliableOrdered);

        // A "ColorSerializer" has already been provided in ARDK/Utilities/BinarySerialization/ItemSerializers
        //   and registered to the GlobalSerializer, so this works automatically. If you want to
        //   serialize a type that is not provided, extend BaseItemSerializer<T> and register that
        //   serializer to the GlobalSerializer.
        _currColor = new NetworkedField<Color>("currColor", descriptor, Owner.Group);
        _currColor.ValueChanged += OnCurrColorChanged;
      };
    }

    private void OnCurrColorChanged(NetworkedFieldValueChangedArgs<Color> newColor)
    {
      if (!newColor.Value.HasValue)
        return;

      if (_mat == null)
        _mat = GetComponent<Renderer>().material;

      _mat.color = newColor.Value.Value;
    }
  }