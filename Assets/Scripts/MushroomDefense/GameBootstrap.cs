using UnityEngine;

namespace MushroomDefense
{
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindObjectOfType<MushroomGameController>() != null)
            {
                return;
            }

            var root = new GameObject("MushroomGame");
            root.AddComponent<MushroomGameController>();
        }
    }
}
