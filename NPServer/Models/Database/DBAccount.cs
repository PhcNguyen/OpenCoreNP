﻿using NPServer.Infrastructure.Helper;
using NPServer.Infrastructure.Services;
using System;

namespace NPServer.Models.Database
{
    /// <summary>
    /// Represents an account stored in the account database.
    /// </summary>
    public class DBAccount
    {
        private static readonly IdGenerator IdGenerator = new(IdType.Player, 0);

        public long Id { get; set; }
        public string Email { get; set; }
        public string PlayerName { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] Salt { get; set; }
        public UserRole UserLevel { get; set; }
        public AccountFlags Flags { get; set; }

        public DBPlayer? Player { get; set; }

        // NOTE: init is required for collection properties to be compatible with JSON serialization
        public DBEntityCollection Avatars { get; init; } = [];
        public DBEntityCollection TeamUps { get; init; } = [];
        public DBEntityCollection Items { get; init; } = [];
        public DBEntityCollection ControlledEntities { get; init; } = [];

        /// <summary>
        /// Constructs a <see cref="DBAccount"/> instance with the provided data.
        /// </summary>
        public DBAccount(string email, string playerName, string password, UserRole userLevel = UserRole.User)
        {
            Id = (long)IdGenerator.Generate();
            Email = email;
            PlayerName = playerName;
            PasswordHash = CryptographyHelper.HashPassword(password, out byte[] salt);
            Salt = salt;
            UserLevel = userLevel;
        }

        /// <summary>
        /// Constructs a default <see cref="DBAccount"/> instance with the provided data.
        /// </summary>
        public DBAccount(string playerName)
        {
            // Default account is used when BypassAuth is enabled
            Id = 0x2000000000000001;
            Email = "default@server.com";
            PlayerName = playerName;
            PasswordHash = [];
            Salt = [];
            UserLevel = UserRole.Admin;
        }

        public DBAccount()
        {
        }

        public override string ToString()
        {
            return $"{PlayerName} (0x{Id:X})";
        }

        public void ClearEntities()
        {
            Avatars.Clear();
            TeamUps.Clear();
            Items.Clear();
            ControlledEntities.Clear();
        }
    }
}