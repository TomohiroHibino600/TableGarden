// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR;
using Niantic.ARDK.Networking;

namespace Niantic.ARDK.Configuration
{
  /// Global configuration class.
  /// Allows developers to setup different configuration values during runtime.
  /// This exists such that in a live production environment, you can obtain the configuration
  /// settings remotely and set them before running the rest of the application.
  public static class ArdkGlobalConfig
  {
    internal const string _DBOW_URL = "https://bowvocab.eng.nianticlabs.com/dbow_b50_l3.bin";

    internal const string _DEFAULT_AUTH_URL =
      "https://us-central1-ar-dev-portal-prod.cloudfunctions.net/auth_token";

    private static _IArdkConfig __impl;

    private static _IArdkConfig _impl
    {
      get
      {
        if (__impl == null)
        {
          // Unless explicitly asking for native, return implementation without native platform
          // dependencies so URLs can be set in Virtual Studio and in tests like NativeARNetworking.
          // Note: It's possible to create a _NativeFeaturePreloader without setting the NativeAccess
          // mode to Native, and then the preloader will download from default URLs instead of
          // ones set in the ArdkGlobalConfig. There's currently no important use case where that's
          // relevant though, so leaving the bug as known but unresolved.
#pragma warning disable CS0162
          if (NativeAccess.Mode == NativeAccess.ModeType.Native)
            __impl = new _NativeArdkConfig();
          else
            __impl = new _SerializeableArdkConfig();
#pragma warning restore CS0162
        }

        return __impl;
      }
    }

    public static bool SetDbowUrl(string url)
    {
      return _impl.SetDbowUrl(url);
    }

    public static string GetDbowUrl()
    {
      return _impl.GetDbowUrl();
    }

    public static string GetContextAwarenessUrl()
    {
      return _impl.GetContextAwarenessUrl();
    }

    public static bool SetContextAwarenessUrl(string url)
    {
      return _impl.SetContextAwarenessUrl(url);
    }

    public static bool SetApiKey(string apiKey)
    {
      return _impl.SetApiKey(apiKey);
    }

    public static string GetAuthenticationUrl()
    {
      return _impl.GetAuthenticationUrl();
    }

    public static bool SetAuthenticationUrl(string url)
    {
      return _impl.SetAuthenticationUrl(url);
    }

    public static NetworkingErrorCode VerifyApiKeyWithFeature(string feature)
    {
      return _impl.VerifyApiKeyWithFeature(feature);
    }
  }
}
