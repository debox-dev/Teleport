# Teleport
### A fast, lightweight, pure C# Unity realtime-game-networking framework.

Teleport is a real-time multiplayer framework for Unity.

## Installation instructions
- Open your project manifest file (`MyProject/Packages/manifest.json`).
- Add `"com.debox.teleport": "https://github.com/debox-dev/Teleport.git"` to the `dependencies` list.
- Open or focus on Unity Editor to resolve packages.


## Requirements
- Unity 2019 or higher.

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
	// Write a compressed Vector3
        writer.Write(Position, FloatCompressionTypeShort.Short_Two_Decimals);
    }

    public override void Deserialize(DeBox.Teleport.Core.TeleportReader reader)
    {
        base.Deserialize(reader);
	// Read a compressed Vector3
        Position = reader.ReadVector3(FloatCompressionTypeShort.Short_Two_Decimals);
    }

    public override void OnArrivalToClient()
    {
        base.OnArrivalToClient();
        Debug.Log("The sent position is " + Position);
        GameObject.FindObjectOfType<Actor>().transform.position = Position;
    } 
}
```

#### Registering messages
On the client
```
TeleportManager.Main.RegisterClientMessage<MyMessage>();
```
On the server
```
TeleportManager.Main.RegisterClientMessage<MyMessage>();
```
On both
```
TeleportManager.Main.RegisterTwoWayMessage<MyMessage>();
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

## Packet structure
#### Packet structure overview
##### 1. Fixed packet prefix (2 bits)
The fixed packet prefix is a fixed value that always prepends the packet header
This value is used by Teleport to identify where a packet may start in case of data consistency errors.

For example - If a packet arrived with a header error, there is no way to know where the next packet starts, so Teleport must search for the fixed prefix to understand where the next packet is. If it finds something that looks like a packet, but is now - we expect it to fail the CRC check and so Teleport will continue to search onward.

##### 2. Channel Id (2 bits)
This is the channel id of the packet. Each channel may process the packet data differently so it is important for the system to know which channel should handle this packet

##### 3. Data CRC (4 bits)
This is the CRC of the data. Teleport sums up the bytes of the data, modded by the number 15 (0b1111) - resulting in a 4 bit number.

##### 4. Header CRC (4 bits) 
The header of the packet is critical as it tells the system how long the packet is. If the packet header is damaged, Teleport may try to read an infinetly long packet.
Because the data CRC depends on reading the entire packet, we CRC the header separately in order to quickly check if we can receive the data, or immediately dispose this packet.

The header CRC is the sum of the bytes of the following items
1. Fixed packet prefix
2. Channel Id
3. Data CRC
4. Data Length (With the Header CRC bits set to zero)

##### 5. Data Length (12 bits, trimmed ushort, max=4096)
This is the length of the data. It is a ushort trimmed to 1.5 bytes (12 bit). This results in a maximum of 4096 bytes (4K) per packet. This is sufficient enougth and even if it is not, this can be resolved by breaking down data to smaller packets at the channel level (See AggregatingTeleportChannel, not yet tested)

##### 6. Actual data (Variable, according to value of Data Length)
This is the actual data of the packet. If the CRC mismatches we will dispose the data altogether

### Illustration of the header structure
```
BYTE 1  +-+[] FIXED PACKET PREFIX (2 bits)
        |  []
        |
        |  [] CHANNEL ID (2 bits)
        |  []
        |
        |  [] DATA CRC (4 bits)
        |  []
        |  []
        +-+[]

BYTE 2  +-+[] HEADER CRC (4 bits)
        |  []
        |  []
        |  []
        |
        |  [] DATA LENGTH (12 bits)
        |  []
        |  []
        +-+[]
BYTE 3  +-+[]
        |  []
        |  []
        |  []
        |  []
        |  []
        |  []
        +-+[]
```

## Float compression

Teleport hands out a simple way to compress your floats, Vector2, Vector3, Vector4 and Quaternions

### Float to Short compression
Use this to shorten 4 byte floats to 2 bytes

#### FloatCompressionTypeShort.Short_One_Decimal
```FloatCompressionTypeShort.Short_One_Decimals``` converts a float to a short with one decimal.
Good for rough numbers that do not require too much accuracy, and can be large (4 digits.)

For example: ```4.12551f => 41 => 4.1```

Minimum Value: -3276.8
Maximum Value: 3276.7

#### FloatCompressionTypeShort.Short_Two_Decimals
```FloatCompressionTypeShort.Short_Two_Decimals``` converts a float to a short with two decimals
The resulting number can be a most three digits long before the decimal.
For example: ```4.12551f => 412 => 4.12```

Minimum Value: -327.68
Maximum Value: 327.67

#### FloatCompressionTypeShort.Short_Three_Decimals
```FloatCompressionTypeShort.Short_Three_Decimals``` converts a float to a short with three decimals.
Good for Vectors that have at most the magnitude of 1; for example directions.

For example: ```4.12551f => 4125 => 4.1245```

Minimum Value: -32.768
Maximum Value: 32.767

### Char compression
Use char compression for really really small floats in order to trim 4 bytes to 1 byte

#### FloatCompressionTypeChar.Char_One_Decimal
```FloatCompressionTypeChar.Char_One_Decimal``` 

Minimum Value: -12.7
Maximum Value: 12.8

#### FloatCompressionTypeChar.Char_Two_Decimals
```FloatCompressionTypeChar.Char_Two_Decimals``` 

Good for Vectors that have at most the magnitude of 1; for example directions.

Minimum Value: -1.27
Maximum Value: 1.28


### Serializing with compression
```
writer.Write(Position, FloatCompressionTypeShort.Short_Two_Decimals);
writer.Write(Rotation, FloatCompressionTypeShort.Short_Two_Decimals);
```

### Deserializing with compression
```
Position = reader.ReadVector3(FloatCompressionTypeShort.Short_Two_Decimals);
Rotation = reader.ReadQuaternion(FloatCompressionTypeShort.Short_Two_Decimals);
```


## Per-client message serialization
You may serialize a message personally for each client

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

    public override DeliveryTargetType GetDeliveryTarget()
    {
        return DeliveryTargetType.PerConnection;
    }

    // We don't have to implement Serialize now that we are PerConnection
    public override void Serialize(DeBox.Teleport.Core.TeleportWriter writer)
    {
        throw new Exception("Don't expect this to be called!");
    }

    public override bool SerializeForClient(TeleportWriter writer, uint clientId)
    {
        // Only the second client
        if (GameUtils.CanPlayerSeeObjects(clientId))
        {
            writer.Write(Position);
            return true;
        }
        return false;
    }

    public override void Deserialize(TeleportReader reader)
    {
        Position = reader.ReadVector3();
    }
}
```

