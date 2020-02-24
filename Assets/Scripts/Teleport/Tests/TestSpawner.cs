
using UnityEngine;

using DeBox.Teleport.Unity;
using DeBox.Teleport.Core;

namespace DeBox.Teleport.Tests
{

    public class TestSpawner : BasicTeleportObjectSpawner
    {
        public class TestSpawnConfig
        {
            public Color Color;
        }

        public override void OnServerSpawn(TeleportWriter writer, GameObject spawned, object instanceConfig)
        {
            base.OnServerSpawn(writer, spawned, instanceConfig);
            var config = (TestSpawnConfig)instanceConfig;
            writer.Write(config.Color);
            ApplyColor(config.Color, spawned);
        }

        public override void OnClientSpawn(TeleportReader reader, GameObject spawned)
        {
            base.OnClientSpawn(reader, spawned);
            var color = reader.ReadColor();
            ApplyColor(color, spawned);
        }

        private void ApplyColor(Color color, GameObject instance)
        {
            foreach (var objRenderer in instance.GetComponentsInChildren<Renderer>())
            {
                var material = objRenderer.material;
                material.color = color;
                objRenderer.material = material;
            }
        }
    }

}
