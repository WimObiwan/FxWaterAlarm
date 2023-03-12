using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Core.Util;

public static class RandomLinkGenerator
{
    public static string Get()
    {
        var rBytes = RandomNumberGenerator.GetBytes(8);
        var base64 = Convert.ToBase64String(rBytes);
        return Regex.Replace(base64, "[^A-Za-z0-9]", "");
    }
}