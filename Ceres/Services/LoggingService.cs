﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Ceres.Services
{
#pragma warning disable CA1822
    internal class LoggingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;

        private string LogDirectory { get; init; }

        internal LoggingService(DiscordSocketClient discord, CommandService commands)
        {
            LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");

            _discord = discord;
            _commands = commands;

            _discord.Log += OnLogAsync;
            _commands.Log += OnLogAsync;
        }

        internal LoggingService()
        {
            LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        }

        internal async Task OnLogAsync(LogMessage msg)
        {
            string logText = $"{DateTime.UtcNow:s} [{msg.Severity}] [{msg.Source}] {msg.Exception?.ToString() ?? msg.Message}";
            await LogToFile(logText);
            await LogToConsole(msg.Severity, logText);
        }

        // TODO error handling
        private async Task LogToFile(string logText)
        {
            string logFile = Path.Combine(LogDirectory, "ceres.log");
            if (!File.Exists(logFile))
                File.Create(logFile);
            else
            {
                FileInfo logFileInfo = new(logFile);

                if (logFileInfo.Length > 1024 * 1024)
                {
                    logFileInfo.MoveTo($"{DateTime.Now:uMM}_ceres.log");
                }
            }

            using FileStream file = new(logFile, FileMode.Append, FileAccess.Write);
            using StreamWriter sw = new(file, System.Text.Encoding.UTF8);
            await sw.WriteAsync(logText + Environment.NewLine);
        }

        private async Task LogToConsole(LogSeverity severity, string logText)
        {
            switch (severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    await Console.Out.WriteLineAsync(logText);
                    return;

                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    await Console.Out.WriteLineAsync(logText);
                    return;

                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    await Console.Out.WriteLineAsync(logText);
                    return;

                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.Green;
                    await Console.Out.WriteLineAsync(logText);
                    return;

                case LogSeverity.Verbose:
                default:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    await Console.Out.WriteLineAsync(logText);
                    return;
            }
        }
    }
#pragma warning restore CA1822
}
