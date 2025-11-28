using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class RangeSliderViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial double SimpleLowerValue { get; set; } = 25;

	[ObservableProperty]
	public partial double SimpleUpperValue { get; set; } = 75;

	[ObservableProperty]
	public partial double LowerValue2 { get; set; } = 25;

	[ObservableProperty]
	public partial double UpperValue2 { get; set; } = 75;
}