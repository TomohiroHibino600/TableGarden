#if (UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX)

using System.IO;

using UnityEditor.Android;

class UnityGradleErrorHackWorkaround:
  IPostGenerateGradleAndroidProject
{

  public int callbackOrder
  {
    get
    {
      return 0;
    }
  }

  public void OnPostGenerateGradleAndroidProject(string path)
  {
    File.WriteAllText(Path.Combine(path, "settings.gradle"), "");
  }
}
#endif
