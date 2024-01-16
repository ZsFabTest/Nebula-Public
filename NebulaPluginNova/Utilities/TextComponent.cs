﻿using Nebula.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Text;

namespace Nebula.Utilities;

public class CombinedComponent : TextComponent
{
    TextComponent[] components;

    public CombinedComponent(params TextComponent[] components)
    {
        this.components = components;
    }

    public string GetString()
    {
        StringBuilder builder = new();
        foreach (var str in components) builder.Append(str.GetString());
        return builder.ToString();
    }
}

public class RawTextComponent : TextComponent
{
    public string RawText { get; set; }
    public string GetString() => RawText;

    public RawTextComponent(string text)
    {
        RawText = text;
    }
}

public class LazyTextComponent : TextComponent
{
    private Func<string> supplier;
    public LazyTextComponent(Func<string> supplier)
    {
        this.supplier = supplier;
    }

    public string GetString() => supplier.Invoke();
}

public class ColorTextComponent : TextComponent
{
    public Color Color { get; set; }
    TextComponent Inner { get; set; }
    public string GetString() => Inner.GetString().Color(Color);
    public ColorTextComponent(Color color, TextComponent inner)
    {
        Color = color;
        Inner = inner;
    }
}

public class TranslateTextComponent : TextComponent
{
    public string TranslationKey { get; set; }
    public string GetString() => Language.Translate(TranslationKey);
    public TranslateTextComponent(string translationKey)
    {
        TranslationKey = translationKey;
    }
}
