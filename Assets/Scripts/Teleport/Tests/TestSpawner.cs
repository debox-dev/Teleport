
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

        public override GameObject CreateInstance()
        {
            var spawned = base.CreateInstance();
            ApplyColor(Color.red, spawned);
            return spawned;
        }

        public override void ServerSidePreSpawnToClient(TeleportWriter writer, GameObject spawned, object instanceConfig)
        {
            base.ServerSidePreSpawnToClient(writer, spawned, instanceConfig);
            var config = (TestSpawnConfig)instanceConfig;
            writer.Write(Color.green);
        }

        protected override void PostClientSpawn(TeleportReader reader, GameObject spawned)
        {
            base.PostClientSpawn(reader, spawned);
            var color = reader.ReadColor();
            ApplyColor(color, spawned);
        }

        public override object GetConfigForLiveInstance(GameObject instance)
        {
            var config = new TestSpawnConfig();
            config.Color = GetColor(instance);
            return config;
        }

        private Color GetColor(GameObject instance)
        {
            foreach (var objRenderer in instance.GetComponentsInChildren<Renderer>())
            {
                var material = objRenderer.material;
                return material.color;
            }
            return Color.black;
        }

        private void ApplyColor(Color color, GameObject instance)
        {
            foreach (var objRenderer in instance.GetComponentsInChildren<Renderer>())
            {
                var materials = objRenderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i].color = color;
                }
                objRenderer.materials = materials;
            }
        }
    }

}
