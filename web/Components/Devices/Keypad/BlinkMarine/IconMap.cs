using MudBlazor;

namespace web.Components.Devices.Keypad;

public static class IconMap
{
    private static readonly Dictionary<string, string> Icons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Circle"] = MudBlazor.Icons.Material.Filled.Circle,
        ["Power"] = MudBlazor.Icons.Material.Filled.Power,
        ["PowerSettingsNew"] = MudBlazor.Icons.Material.Filled.PowerSettingsNew,
        ["Lightbulb"] = MudBlazor.Icons.Material.Filled.Lightbulb,
        ["FlashlightOn"] = MudBlazor.Icons.Material.Filled.FlashlightOn,
        ["Highlight"] = MudBlazor.Icons.Material.Filled.Highlight,
        ["Warning"] = MudBlazor.Icons.Material.Filled.Warning,
        ["Error"] = MudBlazor.Icons.Material.Filled.Error,
        ["Info"] = MudBlazor.Icons.Material.Filled.Info,
        ["Help"] = MudBlazor.Icons.Material.Filled.Help,
        ["Settings"] = MudBlazor.Icons.Material.Filled.Settings,
        ["Home"] = MudBlazor.Icons.Material.Filled.Home,
        ["Menu"] = MudBlazor.Icons.Material.Filled.Menu,
        ["ArrowUp"] = MudBlazor.Icons.Material.Filled.ArrowUpward,
        ["ArrowDown"] = MudBlazor.Icons.Material.Filled.ArrowDownward,
        ["ArrowLeft"] = MudBlazor.Icons.Material.Filled.ArrowBack,
        ["ArrowRight"] = MudBlazor.Icons.Material.Filled.ArrowForward,
        ["VolumeUp"] = MudBlazor.Icons.Material.Filled.VolumeUp,
        ["VolumeDown"] = MudBlazor.Icons.Material.Filled.VolumeDown,
        ["VolumeMute"] = MudBlazor.Icons.Material.Filled.VolumeMute,
        ["VolumeOff"] = MudBlazor.Icons.Material.Filled.VolumeOff,
        ["PlayArrow"] = MudBlazor.Icons.Material.Filled.PlayArrow,
        ["Pause"] = MudBlazor.Icons.Material.Filled.Pause,
        ["Stop"] = MudBlazor.Icons.Material.Filled.Stop,
        ["SkipNext"] = MudBlazor.Icons.Material.Filled.SkipNext,
        ["SkipPrevious"] = MudBlazor.Icons.Material.Filled.SkipPrevious,
        ["FastForward"] = MudBlazor.Icons.Material.Filled.FastForward,
        ["FastRewind"] = MudBlazor.Icons.Material.Filled.FastRewind,
        ["Camera"] = MudBlazor.Icons.Material.Filled.Camera,
        ["CameraAlt"] = MudBlazor.Icons.Material.Filled.CameraAlt,
        ["Mic"] = MudBlazor.Icons.Material.Filled.Mic,
        ["MicOff"] = MudBlazor.Icons.Material.Filled.MicOff,
        ["Phone"] = MudBlazor.Icons.Material.Filled.Phone,
        ["Call"] = MudBlazor.Icons.Material.Filled.Call,
        ["Bluetooth"] = MudBlazor.Icons.Material.Filled.Bluetooth,
        ["Wifi"] = MudBlazor.Icons.Material.Filled.Wifi,
        ["WifiOff"] = MudBlazor.Icons.Material.Filled.WifiOff,
        ["Lock"] = MudBlazor.Icons.Material.Filled.Lock,
        ["LockOpen"] = MudBlazor.Icons.Material.Filled.LockOpen,
        ["Visibility"] = MudBlazor.Icons.Material.Filled.Visibility,
        ["VisibilityOff"] = MudBlazor.Icons.Material.Filled.VisibilityOff,
        ["Favorite"] = MudBlazor.Icons.Material.Filled.Favorite,
        ["Star"] = MudBlazor.Icons.Material.Filled.Star,
        ["Flag"] = MudBlazor.Icons.Material.Filled.Flag,
        ["Anchor"] = MudBlazor.Icons.Material.Filled.Anchor,
        ["Navigation"] = MudBlazor.Icons.Material.Filled.Navigation,
        ["NearMe"] = MudBlazor.Icons.Material.Filled.NearMe,
        ["MyLocation"] = MudBlazor.Icons.Material.Filled.MyLocation,
        ["Explore"] = MudBlazor.Icons.Material.Filled.Explore,
        ["Map"] = MudBlazor.Icons.Material.Filled.Map,
        ["DirectionsBoat"] = MudBlazor.Icons.Material.Filled.DirectionsBoat,
        ["DirectionsCar"] = MudBlazor.Icons.Material.Filled.DirectionsCar,
        ["LocalGasStation"] = MudBlazor.Icons.Material.Filled.LocalGasStation,
        ["Speed"] = MudBlazor.Icons.Material.Filled.Speed,
        ["Timer"] = MudBlazor.Icons.Material.Filled.Timer,
        ["Thermostat"] = MudBlazor.Icons.Material.Filled.Thermostat,
        ["AcUnit"] = MudBlazor.Icons.Material.Filled.AcUnit,
        ["Waves"] = MudBlazor.Icons.Material.Filled.Waves,
        ["WaterDrop"] = MudBlazor.Icons.Material.Filled.WaterDrop,
        ["Air"] = MudBlazor.Icons.Material.Filled.Air,
        ["Bolt"] = MudBlazor.Icons.Material.Filled.Bolt,
        ["BatteryFull"] = MudBlazor.Icons.Material.Filled.BatteryFull,
        ["Battery"] = MudBlazor.Icons.Material.Filled.Battery0Bar,
        ["Usb"] = MudBlazor.Icons.Material.Filled.Usb,
        ["Cable"] = MudBlazor.Icons.Material.Filled.Cable,
        ["Radar"] = MudBlazor.Icons.Material.Filled.Radar,
        ["Sensors"] = MudBlazor.Icons.Material.Filled.Sensors,
        ["Tune"] = MudBlazor.Icons.Material.Filled.Tune,
        ["Build"] = MudBlazor.Icons.Material.Filled.Build,
        ["Construction"] = MudBlazor.Icons.Material.Filled.Construction,
        ["Handyman"] = MudBlazor.Icons.Material.Filled.Handyman,
    };

    public static string Get(string iconName)
    {
        return Icons.TryGetValue(iconName, out var icon)
            ? icon
            : MudBlazor.Icons.Material.Filled.Circle;
    }

    public static IReadOnlyDictionary<string, string> All => Icons;
}
