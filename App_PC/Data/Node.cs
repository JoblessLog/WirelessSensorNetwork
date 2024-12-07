using CommunityToolkit.Mvvm.ComponentModel;

namespace wsn_keboo;

public partial class Node : ObservableObject
{
    [ObservableProperty]
    private string? _id;
    [ObservableProperty]
    private int _priority;
    [ObservableProperty]
    private bool _isSelected;
}








