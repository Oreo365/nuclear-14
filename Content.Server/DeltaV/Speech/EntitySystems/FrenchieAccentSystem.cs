using Content.Server.DeltaV.Speech.Components;
using Content.Server.Speech;
using Content.Server.Speech.EntitySystems;
using System.Text.RegularExpressions;

namespace Content.Server.DeltaV.Speech.EntitySystems;

public sealed class FrenchieAccentSystem : EntitySystem
{
[Dependency]
private readonly ReplacementAccentSystem _replacement = default!;

private static readonly Regex RegexThe = new(@"\bthe\b", RegexOptions.IgnoreCase);
private static readonly Regex RegexThis = new(@"\bthis\b", RegexOptions.IgnoreCase);
private static readonly Regex RegexThat = new(@"\bthat\b", RegexOptions.IgnoreCase);
private static readonly Regex RegexThere = new(@"\bthere\b", RegexOptions.IgnoreCase);
private static readonly Regex RegexThey = new(@"\bthey\b", RegexOptions.IgnoreCase);

private static readonly Regex RegexThSoft = new(@"th", RegexOptions.IgnoreCase);
private static readonly Regex RegexHStart = new(@"\bh", RegexOptions.IgnoreCase);
private static readonly Regex RegexIng = new(@"ing\b", RegexOptions.IgnoreCase);
private static readonly Regex RegexR = new(@"r", RegexOptions.IgnoreCase);

public override void Initialize()
{
    base.Initialize();
    SubscribeLocalEvent<FrenchieAccentComponent, AccentGetEvent>(OnAccentGet);
}

public string Accentuate(string message, FrenchieAccentComponent component)
{
    var words = message.Split(' ');
    var accentuatedWords = new List<string>();

    foreach (var word in words)
    {
        // Apply FTL dictionary replacements first.
        var accentuatedWord = _replacement.ApplyReplacements(word, "Frenchie");

        // If the word was not replaced by the dictionary, apply Frenchie-style phonetics.
        if (accentuatedWord == word)
        {
            accentuatedWord = ApplyRegexReplacements(accentuatedWord);
        }

        accentuatedWords.Add(accentuatedWord);
    }

    var result = string.Join(" ", accentuatedWords);

    // Add occasional Frenchie filler to the END of the sentence.
    var roll = Random.Shared.NextDouble();

    if (roll < 0.03)
    {
        result += ", oui";
    }
    else if (roll < 0.06)
    {
        result += ", mon ami";
    }
    else if (roll < 0.09)
    {
        result += ", n'est-ce pas";
    }

    return result;
}

private string ApplyRegexReplacements(string word)
{
    // Common Frenchie-accent word sounds.
    word = RegexThe.Replace(word, "ze");
    word = RegexThis.Replace(word, "zis");
    word = RegexThat.Replace(word, "zat");
    word = RegexThere.Replace(word, "zere");
    word = RegexThey.Replace(word, "zey");

    // General sound changes.
    word = RegexThSoft.Replace(word, "z");
    word = RegexHStart.Replace(word, "'");
    word = RegexIng.Replace(word, "eeng");

    // Light Frenchie-style rhotic flavor.
    word = RegexR.Replace(word, "rr");

    return word;
}

private void OnAccentGet(EntityUid uid, FrenchieAccentComponent component, AccentGetEvent args)
{
    args.Message = Accentuate(args.Message, component);
}
}
