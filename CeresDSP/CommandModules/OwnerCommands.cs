﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

using Microsoft.CodeAnalysis.CSharp.Scripting;

using System;
using System.Diagnostics;

using static Microsoft.CodeAnalysis.Scripting.ScriptOptions;

namespace CeresDSP.CommandModules
{
    public class OwnerCommands : BaseCommandModule
    {
        [Command("eval"), RequireOwner]
        public async Task Eval(CommandContext ctx, [RemainingText] string input)
        {
            Globals globals = new()
            {
                ctx = ctx
            };

            try
            {
                object evaluation = await CSharpScript.EvaluateAsync(input.Trim('`', '\'', '"'), Default.WithImports("System", "System.Diagnostics"), globals);

                if (evaluation is not null && evaluation.GetType().IsArray)
                {
                    string messageFromArray = string.Empty;
                    Array.ForEach((object[])evaluation, arrayObject => messageFromArray += $"{arrayObject}\n");
                    await ctx.RespondAsync(messageFromArray);
                }
                else if (evaluation is not null && !evaluation.GetType().IsArray)
                    await ctx.RespondAsync(evaluation.ToString());
            }
            catch (Exception ex)
            {
                await ctx.RespondAsync($"{ex.Message}\n*(0x{ex.HResult:x8})*");
                GC.WaitForPendingFinalizers();
                GC.Collect();
                throw;
            }
            
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public class Globals
        {
            public CommandContext ctx;
        }
    }
}
