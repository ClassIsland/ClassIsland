using System.Globalization;
using System.Windows.Controls;

namespace ClassIsland.Validations;

public class NotNegativeIndexValidationRule : ValidationRule
{
    public override ValidationResult Validate(object? value, CultureInfo cultureInfo)
    {
        if (value is not int v)
        {
            return new ValidationResult(false, $"无效的输入类型：{value?.GetType()}");
        }

        if (v < 0)
        {
            return new ValidationResult(false, "选择不能为空。");
        }
        return ValidationResult.ValidResult;
    }
}