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

## Simple Setup
Teleport allows us to test the client and server simultaneously in the same scene, we will use this ability to quickly set up a working network scene

1. Open your project
2. Add `"com.debox.teleport": "https://github.com/debox-dev/Teleport.git"` to the `dependencies` list. of your `manifest.json`, see "Installation instructions" for more information
3. Add an empty GameObject to your scene and name it "TeleportManager"
4. Add the "TeleportManager" componend to the empty GameObject
5. Choose a prefab you'd like to synchronize. Any prefab will do. There is no need to add anything to the prefab.
6. Drag the prefab to the **Prefab Spawners** list of the `TeleportManager` component
7. Create a connection script `NetworkStarter.cs`
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

## Server Time Prediction
Teleport automagically syncs the server time to the clients
#### Server time on the server
```
Debug.Log("Actual server time is " + TeleportManager.Main.ServerSideTime);
```
#### Server time on the clients
```
Debug.Log("Predicted server time is " + TeleportManager.Main.ClientSideServerTime);
```

## Simple Data Messages
You can and should create your own data messages. Teleport supplies an API for common events in the lifetime of the message
Use the `DeBox.Teleport.BaseTeleportMessage` class

**Example**
```
public class MyMessage : BaseTeleportMessage
{
    public Vector3 Position { get; private set; }
    public override byte MsgTypeId { get { return TeleportMsgTypeIds.Highest + 1; } }

    // Deserialization constructor
    public MyMessage() {}

    // Actual constructor
    public MyMessage(Vector3 position)
    {
        Position = position;
    }

    public override void Serialize(DeBox.Teleport.Core.TeleportWriter writer)
    {
        base.Serialize(writer);
        writer.Write(Position);
    }

    public override void Deserialize(DeBox.Teleport.Core.TeleportReader reader)
    {
        base.Deserialize(reader);
        Position = reader.ReadVector3();
    }

    public override void OnArrivalToClient()
    {
        base.OnArrivalToClient();
        Debug.Log("The sent position is " + Position);
        GameObject.FindObjectOfType<Actor>().transform.position = Position;
    } 
}
```

#### Message events
* PreSendServer - Called before the server sends the message, use this for setup of the message
* PreSendClient - Called beofer the client sends the message, use this for setup of the message
* PostSendServer - Called after the server sent the message, use this for sending follow up messages or any clean-up actions
* PostSendClient - Called after the client sent the message, use this for sending follow up messages or any clean-up actions
* OnArrivalToClient - Called as soon as the client receives the message, after it was deserialized
* OnArrivalToServer - Called as soon as the server receives the message, after it was deserialized

#### Sending Messages Sever => Client
1. To all clients
```
TeleportManager.Main.SendToAllClients(new MyMessage(Vector3.one));
```
2. To a specific client
```
TeleportManager.Main.SendToClient(clientId, new MyMessage(Vector3.one));
```
3. To multiple, specific clients
```
TeleportManager.Main.SendToClients(new MyMessage(Vector3.one), channelId: 0, clientIdsArray);
```
4. To all clients, except specific clients
```
TeleportManager.Main.SendToAllExcept(new MyMessage(Vector3.one), channelId: 0, clientIdsArray);
```

#### Sending Messages Client => Server
```
TeleportManager.Main.SendToServer(new MyMessage(Vector3.one), channelId: 0);
```
