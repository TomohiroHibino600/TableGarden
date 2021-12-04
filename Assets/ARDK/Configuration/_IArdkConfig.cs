// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Networking;

namespace Niantic.ARDK.Configuration
{
  internal interface _IArdkConfig
  {
    bool SetDbowUrl(string url);

    string GetDbowUrl();

    string GetContextAwarenessUrl();

    bool SetContextAwarenessUrl(string url);

    bool SetApiKey(string key);

    string GetAuthenticationUrl();

    bool SetAuthenticationUrl(string url);

    NetworkingErrorCode VerifyApiKeyWithFeature(string feature);
  }
}
