using domain.Interfaces;
using domain.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using web.Components.Dialogs;

namespace web.Components.Devices.dingoPdm;

public abstract class PdmFunctionComponentBase<TDevice> : ComponentBase
    where TDevice : IDeviceConfigurable
{
    [Parameter, EditorRequired] public TDevice Device { get; set; } = default!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;

    protected async Task OpenVariableSelectorAsync(string datatype, Action<int> setter)
    {
        var parameters = new DialogParameters<VarMapSelectionDialog>
        {
            { x => x.Device, Device },
            { x => x.Datatype, datatype }
        };

        var dialog = await DialogService.ShowAsync<VarMapSelectionDialog>("Select Variable", parameters);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: (DeviceVariable variable) })
            setter(variable.VariableIndex);
    }
    
    protected string GetSelectedVarText(int index)
    {
        var variable = Device.VarMap.Find(p => p.VariableIndex == index);

        if (variable == null) return "Not found";
        
        if(variable.FunctionName.Length == 0)
            return "Select Variable";

        if (variable.SingleVariable)
            return $"{variable.FunctionName}.{variable.PropertyName}";

        return $"{variable.FunctionName}{variable.FunctionIndex}.{variable.PropertyName}";
    }
}
