using UnityEngine;

public static class PlatformManager
{
    public static IPlatformService Service { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        //Aici adaugi platformele cu care doresti sa lucrezi
        #if !UNITY_EDITOR && UNITY_SWITCH
        Service = new SwitchPlatformService();
        #else
        Service = new StandalonePlatformService();
        #endif

        Service.Initialize();
    }
}
