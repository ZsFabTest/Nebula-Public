﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Command;
using Virial;
using System.Runtime.CompilerServices;

namespace Nebula.Commands.Tokens;


/// <summary>
/// 文字列のトークンです。
/// </summary>
public class StringCommandToken : ICommandToken
{
    internal bool CanSubstitute { get; private set; } = true;
    private string myStr { get; init; }

    public string Token => myStr;

    /// <summary>
    /// 文字列トークンを生成します。
    /// </summary>
    /// <param name="text"></param>
    public StringCommandToken(string text) : this(text, true) { }

    internal StringCommandToken(string text, bool canSubstitute)
    {
        myStr = text;
        CanSubstitute = canSubstitute;
    }

    CoTask<ICommandToken> ICommandToken.EvaluateHere(CommandEnvironment env)
    {
        return new CoImmediateTask<ICommandToken>(env.ArgumentTable.ApplyTo(this));
    }

    CoTask<IEnumerable<ICommandToken>> ICommandToken.AsEnumerable(CommandEnvironment env)
    {
        return new CoImmediateTask<IEnumerable<ICommandToken>>([env.ArgumentTable.ApplyTo(this)]);
    }

    CoTask<T> ICommandToken.AsValue<T>(CommandEnvironment env)
    {
        var substituted = env.ArgumentTable.ApplyTo(this);
        if (substituted != this) return substituted.AsValue<T>(env);

        var type = typeof(T);

        if (type == typeof(ICommandToken))
        {
            return new CoImmediateTask<T>(Unsafe.As<ICommandToken, T>(ref substituted));
        }
        else if (type == typeof(int))
        {
            if (int.TryParse(myStr, out var val)) return new CoImmediateTask<T>(Unsafe.As<int, T>(ref val));
            if (float.TryParse(myStr, out var valFloat))
            {
                int valInt = (int)valFloat;
                return new CoImmediateTask<T>(Unsafe.As<int, T>(ref valInt));
            }
            return new CoImmediateErrorTask<T>(env.Logger);
        }
        else if (type == typeof(float))
        {
            if (float.TryParse(myStr, out var val)) return new CoImmediateTask<T>(Unsafe.As<float, T>(ref val));
            return new CoImmediateErrorTask<T>(env.Logger);
        }
        else if (type == typeof(bool))
        {
            if (bool.TryParse(myStr, out var val)) return new CoImmediateTask<T>(Unsafe.As<bool, T>(ref val));
            return new CoImmediateErrorTask<T>(env.Logger);
        }
        else if (type == typeof(string))
        {
            var temp = myStr;
            return new CoImmediateTask<T>(Unsafe.As<string, T>(ref temp));
        }
        else if (type == typeof(Virial.Game.Player))
        {
            var temp = NebulaAPI.CurrentGame?.GetAllPlayers().FirstOrDefault(p => p.Name == myStr);
            if (temp != null) return new CoImmediateTask<T>(Unsafe.As<Virial.Game.Player, T>(ref temp));
        }
        else if (type == typeof(GameData.PlayerOutfit))
        {
            var temp = NebulaAPI.CurrentGame?.GetAllPlayers().FirstOrDefault(p => p.Name == myStr)?.Unbox().DefaultOutfit;
            if (temp != null) return new CoImmediateTask<T>(Unsafe.As<GameData.PlayerOutfit, T>(ref temp));
        }

        return new CoImmediateErrorTask<T>(env.Logger);
    }

    IExecutable? ICommandToken.ToExecutable(CommandEnvironment env)
    {
        var substituted = env.ArgumentTable.ApplyTo(this);
        if (substituted != this) return substituted.ToExecutable(env);

        return null;
    }
}

