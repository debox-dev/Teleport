# Teleport
### A fast, lightweight, pure C# Unity realtime-game-networking framework.

Teleport is a real-time multiplayer framework for Unity.

## Installation instructions
- Open your project manifest file (`MyProject/Packages/manifest.json`).
- Add `"com.debox.teleport": "https://github.com/debox-dev/Teleport.git"` to the `dependencies` list.
- Open or focus on Unity Editor to resolve packages.


## Requirements
- Unity 2013 or higher.

## Features
* Easy to set-up
* Unlimited client connections (As much as your network can handle)
* Up to 4 channels per Transport
* Multiple transports per project (If you need more than 4 channels)
* CRC checking
* Sequencing
* Retransmitting missing packets
* Packet buffering
* Built-in Server-Time synchronization to clients
* Client-side delayed interpolation
* Prefab spawning mechanism
* State synchronization mechanism
* Tests

## Simple setup
1. Open your project
2. Add `"com.debox.teleport": "https://github.com/debox-dev/Teleport.git"` to the `dependencies` list. of your `manifest.json`, see "Installation instructions" for more information
3. Add an empty GameObject to your scene and name it "TeleportManager"
4. Add the "TeleportManager" componend to the empty GameObject
5. Choose a prefab you'd like to synchronize. Any prefab will do. There is no need to add anything to the prefab.
6. Drag the prefab to the **Prefab Spawners** list of the `TeleportManager` component
7. Create a connection script
```
using System.Collections;
using UnityEngine;
using DeBox.Teleport.Unity;
using DeBox.Teleport;

public class NetworkStarter : MonoBehaviour
{
   [SerializeField] private GameObject _spawnedPrefab = null;

   private IEnumerator Start()
   {
        yield return new WaitForSeconds(1);
	TeleportManager.Main.StartServer();
        TeleportManager.Main.ConnectClient();
        Debug.Log("Waiting for client to authenticate...");
        while (TeleportManager.Main.ClientState != TeleportClientProcessor.StateType.Connected)
        {
             yield return null;
        }
        Debug.Log("Client to authenticated!");
        for (var i = 0; i < 10; i++)
        {
            var spawnPosition = Vector3.one * i;        
            TeleportManager.Main.ServerSideSpawn(_spawnedPrefab, spawnPosition, null);
        }
   }
}
```
8. Create a new empty GameObject in your scene and name it "NetworkStarter"
9. Place the `NetworkStarter` component on the "NetworkStarter" GameObject
10. Drage your prefab to the `NetworkStarter` component in the inspector
11. Press play to see your prefab spawned for both server and client


Refer to the [DeBOX](http://deboxdev.com/).
