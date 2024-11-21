﻿namespace NServer.Application.Handler
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    internal class CommandAttribute(Cmd command) : System.Attribute
    {
        public Cmd Command { get; } = command;  // Gán giá trị cho thuộc tính Command
    }

    internal enum Cmd : short
    {
        NONE,
        PING,
        PONG,
        SET_KEY,
        GET_KEY,

        REGISTER,
        LOGIN,
        UPDATE_PASSWORD,

        SUCCESS = 100,
        ERROR = 101,
        INVALID_COMMAND = 102,
        TIMEOUT = 103
    }
}