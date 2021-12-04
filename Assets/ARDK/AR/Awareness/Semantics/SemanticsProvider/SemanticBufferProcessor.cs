using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK.AR.ARSessionEventArgs;

using UnityEngine;

namespace Niantic.ARDK.AR.Awareness.Semantics
{
  public class SemanticBufferProcessor: 
    AwarenessBufferProcessor<ISemanticBuffer>, 
    ISemanticBufferProcessor
  {
    private IARSession _session;
    private readonly UnityEngine.Camera _camera;

    public SemanticBufferProcessor(UnityEngine.Camera camera)
    {
      _camera = camera;
      ARSessionFactory.SessionInitialized += OnARSessionInitialized;
    }
    
    protected override void Dispose(bool disposing)
    {
      base.Dispose(disposing);
      
      ARSessionFactory.SessionInitialized -= OnARSessionInitialized;
      if (_session != null)
        _session.FrameUpdated -= OnFrameUpdated;
    }
    
    private void OnARSessionInitialized(AnyARSessionInitializedArgs args)
    {
      if (_session != null)
        _session.FrameUpdated -= OnFrameUpdated;

      _session = args.Session;
      _session.FrameUpdated += OnFrameUpdated;
    }

    private void OnFrameUpdated(FrameUpdatedArgs args)
    {
      var frame = args.Frame;
      if (frame == null)
        return;

      ProcessFrame
      (
        buffer: frame.Semantics,
        arCamera: frame.Camera,
        unityCamera: _camera
      );
    }

    /// The number of classes available.
    public uint ChannelCount
    {
      get => AwarenessBuffer?.ChannelCount ?? 0;
    }

    /// Returns the possible semantic classes that a pixel can be interpreted.
    public string[] Channels
    {
      get => AwarenessBuffer?.ChannelNames;
    }

    /// <inheritdoc />
    public uint GetSemantics(int screenX, int screenY)
    {
      var semanticsBuffer = AwarenessBuffer;
      if (semanticsBuffer == null)
        return 0u;
      
      // Get normalized screen coordinates
      var x = screenX + 0.5f;
      var y = screenY + 0.5f;
      var screenUV = new Vector3(x / Screen.width, y / Screen.height, 1.0f);

      return AwarenessBuffer.Sample(screenUV, SamplerTransform);
    }

    /// <inheritdoc />
    public int[] GetChannelIndicesAt(int screenX, int screenY)
    {
      var semanticsBuffer = AwarenessBuffer;
      if (semanticsBuffer == null)
        return new int[0];

      var buffer = AwarenessBuffer;
      int count = (int)ChannelCount;
      var semantics = GetSemantics(screenX, screenY);

      var result = new List<int>(capacity: count);
      for (int i = 0; i < count; i++)
      {
        var mask = buffer.GetChannelTextureMask(i);
        if ((semantics & mask) != 0u)
          result.Add(i);
      }

      return result.ToArray();
    }

    /// <inheritdoc />
    public string[] GetChannelNamesAt(int screenX, int screenY)
    {
      var semanticsBuffer = AwarenessBuffer;
      if (semanticsBuffer == null)
        return new string[0];

      var buffer = AwarenessBuffer;
      var channels = Channels;
      var semantics = GetSemantics(screenX, screenY);

      return (
        from channel in channels
        let mask = buffer.GetChannelTextureMask(channel)
        where (semantics & mask) != 0u
        select channel).ToArray();
    }

    /// <inheritdoc />
    public bool DoesChannelExistAt(int screenX, int screenY, int channelIndex)
    {
      var semantics = GetSemantics(screenX, screenY);
      var bitMask = AwarenessBuffer.GetChannelTextureMask(channelIndex);

      return (semantics & bitMask) != 0u;
    }

    /// <inheritdoc />
    public bool DoesChannelExistAt(int screenX, int screenY, string channelName)
    {
      var semantics = GetSemantics(screenX, screenY);
      var bitMask = AwarenessBuffer.GetChannelTextureMask(channelName);
      
      return (semantics & bitMask) != 0u;
    }
    
    public void CopyToAlignedTextureARGB32(int channel, ref Texture2D texture, ScreenOrientation orientation)
    {
      if (AwarenessBuffer == null)
        return;
      
      // Get a typed buffer
      ISemanticBuffer semanticsBuffer = AwarenessBuffer;

      // Acquire the affine transform for the buffer
      var transform = SamplerTransform;

      // Determine the bit mask for the requested semantic classes
      var bitMask = AwarenessBuffer.GetChannelTextureMask(channel);

      // Call base method
      CreateOrUpdateTextureARGB32
      (
        ref texture,
        orientation,
        
        // The sampler function needs to be defined such that given a destination
        // texture coordinate, what color needs to be written to that position?
        // We sample the typed awareness buffer and apply the channel bitmask.
        // White means the pixel contains at least one of the requested semantic
        // classes. The resulting texture is display aligned, that's why we sample
        // using the buffer's affine matrix.
        sampler: uv => (semanticsBuffer.Sample(uv, transform) & bitMask) != 0u
          ? Color.white
          : Color.clear
      );
    }
    
    public void CopyToAlignedTextureARGB32(int[] channels, ref Texture2D texture, ScreenOrientation orientation)
    {
      if (AwarenessBuffer == null)
        return;
      
      // Get a typed buffer
      ISemanticBuffer semanticsBuffer = AwarenessBuffer;

      // Acquire the affine transform for the buffer
      var transform = SamplerTransform;

      // Determine the bit mask for the requested semantic classes
      var bitMask = AwarenessBuffer.GetChannelTextureMask(channels);

      // Call base method
      CreateOrUpdateTextureARGB32
      (
        ref texture,
        orientation,
        
        // The sampler function needs to be defined such that given a destination
        // texture coordinate, what color needs to be written to that position?
        // We sample the typed awareness buffer and apply the channel bitmask.
        // White means the pixel contains at least one of the requested semantic
        // classes. The resulting texture is display aligned, that's why we sample
        // using the buffer's affine matrix.
        sampler: uv => (semanticsBuffer.Sample(uv, transform) & bitMask) != 0u
          ? Color.white
          : Color.clear
      );
    }
  }
}
