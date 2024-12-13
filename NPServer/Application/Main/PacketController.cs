﻿using NPServer.Core.Packets.Utilities;
using NPServer.Core.Interfaces.Session;
using NPServer.Core.Interfaces.Packets;
using NPServer.Core.Memory;
using NPServer.Core.Packets;
using NPServer.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NPServer.Core.Packets.Queue;
using NPServer.Application.Handlers.Packets;

namespace NPServer.Application.Main;

internal sealed class PacketController
{
    private readonly ObjectPool _packetPool;
    private readonly CancellationToken _token;
    private readonly ISessionManager _sessionManager;
    private readonly PacketProcessor _packetProcessor;
    private readonly PacketQueueManager _packetQueueManager;

    public PacketController(CancellationToken token)
    {
        _token = token;
        _packetPool = new ObjectPool();
        _packetQueueManager = new PacketQueueManager();
        _sessionManager = Singleton.GetInstanceOfInterface<ISessionManager>();
        _packetProcessor = new PacketProcessor(_sessionManager);
    }

    public void StartTasks()
    {
        Task.Run(() => StartProcessing(PacketQueueType.Incoming, HandleIncomingPacketBatch), _token);
        Task.Run(() => StartProcessing(PacketQueueType.Outgoing, HandleOutgoingPacketBatch), _token);
    }

    public void EnqueueIncomingPacket(UniqueId id, byte[] data)
    {
        if (!PacketValidation.ValidatePacketStructure(data)) return;

        Packet packet = _packetPool.Get<Packet>();

        packet.SetId(id);
        packet.ParseFromBytes(data);

        _packetQueueManager.GetQueue(PacketQueueType.Incoming).Enqueue(packet);
    }

    private void StartProcessing(PacketQueueType queueType, Action<List<IPacket>> processBatch)
    {
        try
        {
            while (!_token.IsCancellationRequested)
            {
                // Chờ tín hiệu hàng đợi
                _packetQueueManager.WaitForQueue(queueType, _token);

                // Lấy batch gói tin từ hàng đợi
                var packetsBatch = _packetQueueManager
                    .GetQueue(queueType)
                    .DequeueBatch(50);

                // Xử lý batch gói tin
                processBatch(packetsBatch);
            }
        }
        catch (OperationCanceledException)
        {
            // Token đã bị hủy, kết thúc xử lý
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in queue processing ({queueType}): {ex.Message}");
        }
    }

    private void HandleIncomingPacketBatch(List<IPacket> packetsBatch)
    {
        Parallel.ForEach(packetsBatch, packet =>
        {
            try
            {
                _packetProcessor.HandleIncomingPacket(
                    packet,
                    _packetQueueManager.GetQueue(PacketQueueType.Incoming),
                    _packetQueueManager.GetQueue(PacketQueueType.Server)
                );

                _packetPool.Return((Packet)packet);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing incoming packet: {ex.Message}");
            }
        });
    }

    private void HandleOutgoingPacketBatch(List<IPacket> packetsBatch)
    {
        Parallel.ForEach(packetsBatch, packet =>
        {
            try
            {
                _packetProcessor.HandleOutgoingPacket(packet);
                _packetPool.Return((Packet)packet);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing outgoing packet: {ex.Message}");
            }
        });
    }
}