namespace Snap.Net.Avalonia.Enums;

public enum EAppTheme
{
    Fluent,
#if THEMING_SUPPORT    
    Simple,
    Classic,
    Material,
    Semi
#endif
}