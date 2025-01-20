using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct PlayerResultData : IEquatable<PlayerResultData>, INetworkSerializable
{
    public ulong clientId;
    public int capturedTiles;

    public bool Equals(PlayerResultData other)
    {
        return
            clientId == other.clientId &&
            capturedTiles == other.capturedTiles;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref capturedTiles);
    }

}