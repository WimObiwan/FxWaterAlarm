using System.Globalization;

namespace Core.Util;

public static class HexPayloadParser
{
    public static byte[] Parse(string payloadHex)
    {
        if (string.IsNullOrWhiteSpace(payloadHex))
            throw new ArgumentException("PayloadHex is required.", nameof(payloadHex));

        var compact = new string(payloadHex.Where(char.IsLetterOrDigit).ToArray());
        if (compact.Length == 0 || compact.Length % 2 != 0)
            throw new ArgumentException("PayloadHex must contain an even number of hexadecimal characters.", nameof(payloadHex));

        var bytes = new byte[compact.Length / 2];
        for (var i = 0; i < compact.Length; i += 2)
        {
            if (!byte.TryParse(compact.Substring(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                throw new ArgumentException("PayloadHex contains invalid hexadecimal characters.", nameof(payloadHex));

            bytes[i / 2] = value;
        }

        return bytes;
    }
}
