using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class RangeSliderViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial double SimpleLowerValue { get; set; } = 125;

	[ObservableProperty]
	public partial double SimpleUpperValue { get; set; } = 175;

	[ObservableProperty]
	public partial double LowerValue2 { get; set; } = 125;

	[ObservableProperty]
	public partial double UpperValue2 { get; set; } = 175;

	[ObservableProperty]
	public partial double LowerValue3 { get; set; } = 125;

	[ObservableProperty]
	public partial double UpperValue3 { get; set; } = 175;
}