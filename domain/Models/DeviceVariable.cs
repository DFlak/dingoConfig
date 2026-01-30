namespace domain.Models;

public class DeviceVariable
{
    public string FunctionName { get; set; } = string.Empty;
    public int FunctionIndex { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public int VariableIndex { get; set; }
    public string DataType { get; set; } = string.Empty;
}