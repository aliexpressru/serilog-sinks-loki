using System.Text;

namespace Aerx.Serilog.Sinks.Loki.Extensions;

public static class CaseExtensions
{
    public static string ToSnake(this string name)
    {
        if (name == null)
        {
            return null;
        }

        if (name.Length < 2)
        {
            return name.ToLowerInvariant();
        }

        var sb = new StringBuilder(char.ToLowerInvariant(name[0]).ToString());
        for (var i = 1; i < name.Length; i++)
        {
            sb.Append(char.IsUpper(name[i]) ? $"_{char.ToLowerInvariant(name[i])}" : name[i].ToString());
        }

        return sb.ToString();
    }
}