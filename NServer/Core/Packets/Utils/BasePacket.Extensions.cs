﻿using System;
using System.Text;

using NServer.Core.Session;
using NServer.Core.Packets.Enums;

namespace NServer.Core.Packets
{
    internal partial class BasePacket
    {
        /// <summary>
        /// Phương thức để đặt lại gói tin về trạng thái ban đầu.
        /// </summary>
        public void Reset()
        {
            Flags = PacketFlags.NONE;
            Command = (short)0;
            Payload = Memory<byte>.Empty;
        }

        /// <summary>
        /// Thêm dữ liệu vào Payload.
        /// </summary>
        /// <param name="additionalData">Dữ liệu thêm vào.</param>
        public void AppendToPayload(byte[] additionalData)
        {
            var combined = new byte[_payload.Length + additionalData.Length];
            _payload.Span.CopyTo(combined);
            additionalData.CopyTo(combined.AsSpan(_payload.Length));

            Payload = new Memory<byte>(combined);
        }

        /// <summary>
        /// Chuyển đổi gói tin thành chuỗi JSON.
        /// </summary>
        /// <returns>Chuỗi JSON đại diện cho gói tin.</returns>
        public string ToJson()
        {
            var json = new
            {
                Timestamp,
                Flags,
                Command,
                PayloadLength = _payload.Length,
                Payload = Encoding.UTF8.GetString(Payload.ToArray())
            };

            return System.Text.Json.JsonSerializer.Serialize(json);
        }

        /// <summary>
        /// Phương thức để thêm ID 
        /// </summary>
        public void SetID(SessionID id) => Id = id;

        /// <summary>
        /// Phương thức để thêm cờ trạng thái
        /// </summary>
        public void SetFlag(PacketFlags flag) =>
            Flags |= flag;

        /// <summary>
        /// Phương thức để loại bỏ cờ trạng thái
        /// </summary>
        public void RemoveFlag(PacketFlags flag) =>
            Flags &= ~flag;

        /// <summary>
        /// Kiểm tra xem flag có tồn tại hay không.
        /// </summary>
        public bool HasFlag(PacketFlags flag) => Flags.HasFlag(flag);

        /// <summary>
        /// Set Command mới cho gói tin.
        /// </summary>
        public void SetCommand(short command) => Command = command;

        /// <summary>
        /// Thiết lập giá trị lệnh từ một đối tượng enum bất kỳ.
        /// </summary>
        /// <param name="command">Đối tượng enum cần thiết lập.</param>
        public void SetCommand(object command) =>
            Command = command is Enum enumCommand ? Convert.ToInt16(enumCommand) : (short)0;

        /// <summary>
        /// Set Payload mới cho gói tin.
        /// </summary>
        public bool TrySetPayload(ReadOnlySpan<byte> newPayload)
        {
            if (newPayload.IsEmpty) return false;
            Payload = new Memory<byte>(newPayload.ToArray());
            return true;
        }

        /// <summary>
        /// Set Payload mới cho gói tin từ chuỗi.
        /// </summary>
        public void SetPayload(string newPayload) =>
            Payload = new Memory<byte>(System.Text.Encoding.UTF8.GetBytes(newPayload));

        /// <summary>
        /// Cập nhật payload với Span<byte> để tối ưu hóa hiệu suất.
        /// </summary>
        /// <param name="newPayload">Dữ liệu mới của payload.</param>
        public void SetPayload(Span<byte> newPayload)
        {
            if (newPayload.Length > int.MaxValue - _headerSize)
                throw new ArgumentOutOfRangeException(nameof(newPayload), "Payload quá lớn.");

            Payload = new Memory<byte>(newPayload.ToArray());
        }
    }
}