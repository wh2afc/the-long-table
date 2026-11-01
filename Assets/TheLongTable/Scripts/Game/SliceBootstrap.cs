using System;
using LongTable.Core;
using UnityEngine;

namespace LongTable.Game
{
    /// <summary>
    /// The only component the scene needs. Loads the slice data, builds the
    /// village green, the HUD and the director.
    /// Command line: -tltAutoplay sincere|transactional plays a reference day.
    /// </summary>
    public sealed class SliceBootstrap : MonoBehaviour
    {
        [Tooltip("Skip all waits, fades and walking. For tests.")]
        public bool instant;

        public SliceDirector Director { get; private set; }

        private void Start()
        {
            var data = SliceData.LoadFromResources();

            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
            }
            ConfigureCamera(cam);

            var world = VillageBuilder.Build(transform, data, cam);
            var hud = SliceHud.Create(transform, cam);

            Director = gameObject.AddComponent<SliceDirector>();
            Director.Init(data, world, hud, cam, instant);

            string play = Arg("-tltAutoplay");
            if (!string.IsNullOrEmpty(play)) Director.StartAutoplay(play);
        }

        public static void ConfigureCamera(Camera cam)
        {
            cam.orthographic = true;
            cam.orthographicSize = 5.2f;
            cam.transform.position = new Vector3(0f, 3.1f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.14f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
        }

        private static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }
    }
}
