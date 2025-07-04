using System;

namespace ConfigGenerator.ConfigInfrastructure;

public static class ParseUtils
{
    public static bool TryParseInt(string value, out int result)
    {
        if (value.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) // Двійкова
        {
            return int.TryParse(value[2..], System.Globalization.NumberStyles.AllowLeadingWhite,
                       System.Globalization.CultureInfo.InvariantCulture, out result) && 
                   (result = Convert.ToInt32(value[2..], 2)) >= 0;
        }
        if (value.StartsWith("0o", StringComparison.OrdinalIgnoreCase)) // Вісімкова
        {
            return int.TryParse(value[2..], System.Globalization.NumberStyles.AllowLeadingWhite,
                       System.Globalization.CultureInfo.InvariantCulture, out result) &&
                   (result = Convert.ToInt32(value[2..], 8)) >= 0;
        }
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || value.StartsWith("#")) // Шістнадцяткова (0x... або #...)
        {
            string hexValue = value.StartsWith("#") ? value[1..] : value[2..];
            return int.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out result);
        }
        return int.TryParse(value, out result); // Десяткова
    }
}