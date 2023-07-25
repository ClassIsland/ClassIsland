using System.Globalization;
using System.Windows.Controls;

namespace ClassIsland.Validations;

public class SameStringValidationRule : ValidationRule
{
    public string ErrorMessage
    {
        get;
        set;
    } = "";

    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        var s = ((string)value).Split('\\');
        return s[0] == s[1] ? new ValidationResult(true, null) : new ValidationResult(false, ErrorMessage);
    }
}