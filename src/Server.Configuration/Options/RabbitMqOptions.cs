﻿namespace Server.Options
{
    public record RabbitMqOptions
    {
        public string Host { get; init; }
        public string Username { get; init; }
        public string Password { get; init; }
    }
}