﻿using System.Runtime.CompilerServices;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Game;
using Virial.Media;
using Virial.Runtime;
using Virial.Text;

[assembly: InternalsVisibleTo("Nebula")]

namespace Virial;

internal interface INebula
{
    string APIVersion { get; }

    /// <summary>
    /// モジュールを取得します。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T? Get<T>() where T : class;

    // ResourceAPI

    IResourceAllocator NebulaAsset { get; }
    IResourceAllocator InnerslothAsset { get; }
    IResourceAllocator? GetAddonResource(string addonId);
    
    // GameAPI

    Game.Game? CurrentGame { get; }



    // Shortcuts

    Configuration.Configurations Configurations => Get<Configuration.Configurations>()!;
    Media.GUI GUILibrary => Get<Media.GUI>()!;
    Media.Translator Language => Get<Media.Translator>()!;
}

public static class NebulaAPI
{
    static internal INebula instance = null!;
    static internal NebulaPreprocessor? preprocessor = null;

    public static string APIVersion => instance.APIVersion;

    static public IResourceAllocator NebulaAsset => instance.NebulaAsset;
    static public IResourceAllocator InnerslothAsset => instance.InnerslothAsset;
    static public IResourceAllocator? GetAddon(string addonId) => instance.GetAddonResource(addonId);




    /// <summary>
    /// GUIモジュールです。
    /// </summary>
    static public Media.GUI GUI => instance.GUILibrary;

    /// <summary>
    /// 翻訳モジュールです。
    /// </summary>
    static public Media.Translator Language => instance.Language;

    /// <summary>
    /// オプションやゲーム内共有変数に関するモジュールです。
    /// </summary>
    static public Configuration.Configurations Configurations => instance.Configurations;





    /// <summary>
    /// 現在のゲームを取得します。
    /// </summary>
    static public Game.Game? CurrentGame => instance.CurrentGame;

    /// <summary>
    /// プリプロセッサを取得します。
    /// プリプロセス終了後はnullが返ります。
    /// </summary>
    static public NebulaPreprocessor? Preprocessor => preprocessor;
}