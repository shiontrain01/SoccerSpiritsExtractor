using AngleSharp;
using AngleSharp.Dom;
using Extractor.Application;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Text;

namespace Extractor.Infrastructure;

public sealed class CharacterPageParser : ICharacterPageParser
{
    public ParsedCharacterPage Parse(string html, string sourceUrl)
    {
        var ctx = BrowsingContext.New(Configuration.Default);
        var doc = ctx.OpenAsync(req => req.Content(html)).GetAwaiter().GetResult();

        var name = ExtractName(doc) ?? "Unknown";

        var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Attribute"] = ExtractTableValue(doc, "Attribute"),
            ["Rarity"] = ExtractTableValue(doc, "Rarity"),
            ["Type"] = ExtractTableValue(doc, "Type"),
            ["Team"] = ExtractTableValue(doc, "Team"),
            ["Gender"] = ExtractTableValue(doc, "Gender"),
        };

        var stats = ExtractStats(doc);

        var skillsText = ExtractSkillsText(doc);

        // normaliza
        fields["Attribute"] = fields["Attribute"]?.Trim();
        fields["Rarity"] = fields["Rarity"]?.Trim();
        fields["Type"] = fields["Type"]?.Trim();
        fields["Team"] = fields["Team"]?.Replace("\n", " ").Replace("\r", " ").Trim();

        return new ParsedCharacterPage(
            Name: name.Trim(),
            Fields: fields,
            Stats: stats,
            SkillsText: skillsText
        );
    }

    private static string? ExtractName(IDocument doc)
    {
        // Normalmente tem BoxHeader com <b>William</b>
        var boxHeader = doc.QuerySelector(".BoxHeader b");
        if (boxHeader is not null)
            return boxHeader.TextContent;

        // fallback: <title>William | Soccer Spirits Wiki</title>
        return doc.Title?.Split('|')[0].Trim();
    }

    private static string? ExtractTableValue(IDocument doc, string label)
    {
        // Procura por <td><b>Attribute</b></td> e pega o <td> seguinte
        foreach (var b in doc.QuerySelectorAll("td > b"))
        {
            if (!string.Equals(b.TextContent.Trim(), label, StringComparison.OrdinalIgnoreCase))
                continue;

            var td = b.ParentElement;
            var tr = td?.Closest("tr");
            var tds = tr?.QuerySelectorAll("td");
            if (tds is null || tds.Length < 2) continue;

            // o valor costuma estar no 2o td
            return CleanText(tds[1].TextContent);
        }
        return null;
    }

    private static Dictionary<string, decimal?> ExtractStats(IDocument doc)
    {
        var stats = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);

        // Pega pares tipo: <div><b>Power</b></div> <div>461</div>
        var bolds = doc.QuerySelectorAll("div b");
        foreach (var b in bolds)
        {
            var key = b.TextContent.Trim();
            if (string.IsNullOrWhiteSpace(key)) continue;

            // tenta pegar o próximo "table-cell" (estrutura da wiki)
            var row = b.Closest("div[style*='display:table-row']");
            if (row is null) continue;

            var cells = row.QuerySelectorAll("div[style*='display:table-cell']");
            if (cells.Length < 2) continue;

            var valueText = CleanText(cells[1].TextContent);

            if (TryParseDecimal(valueText, out var num))
            {
                // Filtra só chaves de stats comuns (evita capturar qualquer coisa)
                if (IsStatKey(key))
                    stats[key] = num;
            }
        }

        // Também existem stats em tabelas com <b>Dribble</b> etc
        foreach (var tdBold in doc.QuerySelectorAll("div[style*='display:table-cell'] > b"))
        {
            var key = tdBold.TextContent.Trim();
            if (!IsStatKey(key)) continue;

            var row = tdBold.ParentElement?.ParentElement; // row container
            var nextCell = row?.QuerySelectorAll("div[style*='display:table-cell']").Skip(1).FirstOrDefault();
            if (nextCell is null) continue;

            var valueText = CleanText(nextCell.TextContent);
            if (TryParseDecimal(valueText, out var num))
                stats[key] = num;
        }

        return stats;
    }

    private static bool IsStatKey(string key)
    {
        // lista “base” (você pode expandir depois)
        return key is "Power" or "Technique" or "Vitality" or "Speed"
            or "Dribble" or "Steal" or "Defense" or "Reflex" or "MAX HP"
            or "Critical Rate" or "Pass Effect" or "Action Speed"
            or "Critical Damage" or "Penetration" or "Damage Dealt"
            or "Receiving Pass Effect" or "Penetration Resist"
            or "Counterattack Resistance" or "Critical Resistance"
            or "Critical Damage Resist" or "Incoming Damage Down"
            or "Cooperative Defense";
    }

    private static string ExtractSkillsText(IDocument doc)
    {
        // Na página, existe um bloco com header "Skill"
        // Vamos pegar o texto a partir do header "Skill" e juntar conteúdo das tabelas seguintes
        var sb = new StringBuilder();

        var skillHeader = doc.QuerySelectorAll(".BoxHeader b")
            .FirstOrDefault(x => x.TextContent.Trim().Equals("Skill", StringComparison.OrdinalIgnoreCase));

        if (skillHeader is null)
        {
            // fallback: pega qualquer texto contendo "Ace Skill:" etc
            var body = doc.Body?.TextContent ?? "";
            return SafeSlice(body, 8000);
        }

        // tenta pegar o container mais próximo (BoxOuter)
        var box = skillHeader.Closest(".BoxOuter") ?? skillHeader.ParentElement?.ParentElement;
        var text = box?.TextContent ?? doc.Body?.TextContent ?? "";
        sb.AppendLine(CleanText(text));

        return SafeSlice(sb.ToString(), 12000);
    }

    private static string SafeSlice(string s, int max)
    {
        s = s.Replace("\r", " ").Trim();
        return s.Length <= max ? s : s[..max];
    }

    private static string CleanText(string s)
        => string.Join(" ", s.Split(new[] { ' ', '\n', '\t', '\r' }, StringSplitOptions.RemoveEmptyEntries));

    private static bool TryParseDecimal(string raw, out decimal value)
    {
        raw = raw.Trim().Replace("%", "");
        return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out value)
            || decimal.TryParse(raw, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out value);
    }
}
