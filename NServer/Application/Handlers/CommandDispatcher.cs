﻿using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

using NServer.Core.Packets;
using NServer.Infrastructure.Logging;
using NServer.Core.Interfaces.Packets;
using NServer.Application.Handlers.Packets;

namespace NServer.Application.Handlers
{
    internal class CommandDispatcher
    {
        private static readonly BindingFlags BindingFlags = (
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance
        );

        private static readonly Dictionary<Cmd, MethodInfo> CommandCache = LoadMethodsWithCommandAttribute(
            new string[] { "NServer.Application.Handler.Client" }
        );

        private static readonly Dictionary<Cmd, Func<byte[], Task<Packet>>> CommandDelegateCache = [];

        // Đảm bảo rằng các phương thức được load ngay khi ứng dụng bắt đầu
        static CommandDispatcher()
        {
            // Tải tất cả các delegate vào bộ nhớ khi ứng dụng khởi động
            foreach (var command in CommandCache)
            {
                // Tạo delegate cho mỗi phương thức và cache lại
                var func = CreateCommandDelegate(command.Value);
                CommandDelegateCache[command.Key] = func;
            }
        }

        private static Dictionary<Cmd, MethodInfo> LoadMethodsWithCommandAttribute(string[] targetNamespaces)
        {
            var assembly = Assembly.GetExecutingAssembly();

            return assembly.GetTypes()
                           .Where(t => targetNamespaces.Contains(t.Namespace))
                           .SelectMany(t => t.GetMethods(BindingFlags))
                           .Where(m => m.GetCustomAttribute<CommandAttribute>() != null)
                           .ToDictionary(
                               m => m.GetCustomAttribute<CommandAttribute>()!.Command,
                               m => m
                           );
        }

        public static async Task<IPacket> HandleCommand(IPacket packet)
        {
            if (packet.Cmd == (short)Cmd.NONE)
            {
                return Utils.Response(Cmd.ERROR, "Invalid command: Command is null or invalid.");
            }

            var command = (Cmd)packet.Cmd;

            if (!CommandCache.TryGetValue(command, out var method))
            {
                return Utils.Response(Cmd.ERROR, $"Unknown command: {command}");
            }

            try
            {
                // Lấy delegate từ cache đã được load trước
                if (!CommandDelegateCache.TryGetValue(command, out var func))
                {
                    NLog.Instance.Info($"Creating delegate for command: {command}");
                    func = CreateCommandDelegate(method);
                    CommandDelegateCache[command] = func;
                }

                byte[] payloadArray = packet.Payload.Span.ToArray();
                Packet result = await func(payloadArray);

                result.SetId(packet.Id);
                return result;
            }
            catch (Exception ex)
            {
                NLog.Instance.Error($"Error executing command: {command}. Exception: {ex.Message}");
                return Utils.Response(Cmd.ERROR, $"Error executing command: {command}");
            }
        }

        private static Func<byte[], Task<Packet>> CreateCommandDelegate(MethodInfo method)
        {
            if (method.ReturnType != typeof(Task<Packet>))
            {
                throw new ArgumentException("Method must return Task<Packet>", nameof(method));
            }

            var parameters = method.GetParameters();
            if (parameters.Length != 1 || parameters[0].ParameterType != typeof(byte[]))
            {
                throw new ArgumentException("Method must have a single parameter of type byte[]", nameof(method));
            }

            return (Func<byte[], Task<Packet>>)method.CreateDelegate(
                typeof(Func<byte[], Task<Packet>>),
                method.IsStatic ? null : new CommandDispatcher()
            );
        }
    }
}