using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

/// <inheritdoc cref="IExpander"/>
[BindableProperty<IView>("Header", PropertyChangedMethodName = nameof(OnHeaderPropertyChanged))]
[BindableProperty<IView>("Content", PropertyChangedMethodName = nameof(OnContentPropertyChanged))]
[BindableProperty<bool>("IsExpanded", PropertyChangedMethodName = nameof(OnIsExpandedPropertyChanged))]
[BindableProperty<object>("CommandParameter")]
[BindableProperty<ICommand>("Command")]
[ContentProperty(nameof(Content))]
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
[RequiresUnreferencedCode("Calls Microsoft.Maui.Controls.Binding.Binding(String, BindingMode, IValueConverter, Object, String, Object)")]
public partial class Expander : ContentView, IExpander
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="Direction"/> property.
	/// </summary>
	public static readonly BindableProperty DirectionProperty
		= BindableProperty.Create(nameof(Direction), typeof(ExpandDirection), typeof(Expander), ExpandDirection.Down, propertyChanged: OnDirectionPropertyChanged);

	readonly WeakEventManager tappedEventManager = new();
	readonly Grid contentGrid;
	readonly ContentView headerContentView;
	readonly VerticalStackLayout bodyLayout;
	readonly ContentView bodyContentView;

	/// <summary>
	/// Initialize a new instance of <see cref="Expander"/>.
	/// </summary>
	public Expander()
	{
		HandleHeaderTapped = ResizeExpanderInItemsView;
		HeaderTapGestureRecognizer.Tapped += OnHeaderTapGestureRecognizerTapped;

		base.Content = contentGrid = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			},
			Children =
			{
				(headerContentView = new ContentView()),
				(bodyLayout = new VerticalStackLayout()
				{
					HeightRequest = 1,
					MinimumHeightRequest = 1,
					Padding = new Thickness(0, 1, 0, 0),
					Children = { (bodyContentView = new ContentView()) }
				})
			}
		};

		contentGrid.SetRow(headerContentView, 0);
		contentGrid.SetRow(bodyLayout, 1);

		headerContentView.GestureRecognizers.Add(HeaderTapGestureRecognizer);

		bodyContentView.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName == nameof(ContentView.Height))
			{
				if (IsExpanded)
				{
					ContentHeight = bodyContentView.Height + 1;
				}
				OnPropertyChanged(nameof(MaximumContentHeight));
			}
		};
	}

	/// <summary>
	/// Controls the visibility of the content inside the <see cref="Expander"/>.
	/// </summary>
	public double ContentHeight
	{
		get => bodyLayout.HeightRequest;
		set
		{
			double newHeight = Math.Max(Math.Min(value, MaximumContentHeight), MinimumContentHeight);
			if (bodyLayout.Height != newHeight)
			{
				bodyLayout.HeightRequest = newHeight;
				OnPropertyChanged(nameof(ContentHeight));
			}
		}
	}

	/// <summary>
	/// The height of the content inside the <see cref="Expander"/> when it is collapsed.
	/// </summary>
	public double MinimumContentHeight => bodyLayout.MinimumHeightRequest;

	/// <summary>
	/// The height of the content inside the <see cref="Expander"/> when it is expanded.
	/// </summary>
	public double MaximumContentHeight => bodyContentView.Height + 1;

	/// <summary>
	/// Animates the expanding or collapsing of the content inside the <see cref="Expander"/>.
	/// </summary>
	/// <param name="value">The final height of the content.</param>
	/// <param name="length">The time, in milliseconds, over which to animate the transition. The default is 250.</param>
	/// <param name="easing">The easing function to use for the animation.</param>
	/// <returns>A <see cref="Task"/> containing a <see cref="bool"/> value which indicates whether the animation was canceled. <see langword="true"/> indicates that the animation was canceled. <see langword="false"/> indicates that the animation ran to completion.</returns>
	public Task<bool> ContentHeightTo(double value, uint length = 250, Easing? easing = null)
	{
		if (easing == null)
		{
			easing = Easing.Linear;
		}
		var tcs = new TaskCompletionSource<bool>();
		var animation = new Animation(v => ContentHeight = v, ContentHeight, value, easing);
		animation.Commit(this, nameof(ContentHeightTo), 16, length, finished: (f, a) => tcs.SetResult(a));
		return tcs.Task;
	}

	/// <summary>
	///	Triggered when the value of <see cref="IsExpanded"/> changes
	/// </summary>
	public event EventHandler<ExpandedChangedEventArgs> ExpandedChanged
	{
		add => tappedEventManager.AddEventHandler(value);
		remove => tappedEventManager.RemoveEventHandler(value);
	}

	internal TapGestureRecognizer HeaderTapGestureRecognizer { get; } = new();

	/// <summary>
	/// The Action that fires when <see cref="Header"/> is tapped.
	/// By default, this <see cref="Action"/> runs <see cref="ResizeExpanderInItemsView(TappedEventArgs)"/>.
	/// </summary>
	/// <remarks>
	/// Warning: Overriding this <see cref="Action"/> may cause <see cref="Expander"/> to work improperly when placed inside a <see cref="CollectionView"/> and placed inside a <see cref="ListView"/>.
	/// </remarks>
	public Action<TappedEventArgs>? HandleHeaderTapped { get; set; }

	/// <inheritdoc />
	public ExpandDirection Direction
	{
		get => (ExpandDirection)GetValue(DirectionProperty);
		set
		{
			if (!Enum.IsDefined(value))
			{
				throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(ExpandDirection));
			}

			SetValue(DirectionProperty, value);
		}
	}

	static void OnContentPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var expander = (Expander)bindable;
		if (newValue is View view)
		{
			expander.bodyContentView.Content = view;
		}
	}

	static void OnHeaderPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var expander = (Expander)bindable;
		if (newValue is View view)
		{
			expander.headerContentView.Content = view;
		}
	}

	static void OnIsExpandedPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IExpander)bindable).ExpandedChanged(((IExpander)bindable).IsExpanded);
	}

	static void OnDirectionPropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
		((Expander)bindable).HandleDirectionChanged((ExpandDirection)newValue);

	void HandleDirectionChanged(ExpandDirection expandDirection)
	{
		switch (expandDirection)
		{
			case ExpandDirection.Down:
				contentGrid.SetRow(headerContentView, 0);
				contentGrid.SetRow(bodyLayout, 1);
				break;

			case ExpandDirection.Up:
				contentGrid.SetRow(headerContentView, 1);
				contentGrid.SetRow(bodyLayout, 0);
				break;

			default:
				throw new NotSupportedException($"{nameof(ExpandDirection)} {expandDirection} is not yet supported");
		}
	}

	void OnHeaderTapGestureRecognizerTapped(object? sender, TappedEventArgs tappedEventArgs)
	{
		IsExpanded = !IsExpanded;
		HandleHeaderTapped?.Invoke(tappedEventArgs);
	}

	void ResizeExpanderInItemsView(TappedEventArgs tappedEventArgs)
	{
		Element element = this;
#if WINDOWS
		var size = IsExpanded
					? Measure(double.PositiveInfinity, double.PositiveInfinity)
					: headerContentView.Measure(double.PositiveInfinity, double.PositiveInfinity);
#endif
		while (element is not null)
		{
#if IOS || MACCATALYST
			if (element is ListView listView)
			{
				(listView.Handler?.PlatformView as UIKit.UITableView)?.ReloadData();
			}
#endif

#if WINDOWS
			if (element.Parent is ListView listView && element is Cell cell)
			{
				cell.ForceUpdateSize();
			}
			else if (element is CollectionView collectionView)
			{
				var tapLocation = tappedEventArgs.GetPosition(collectionView);
				ForceUpdateCellSize(collectionView, size, tapLocation);
			}
#endif

			element = element.Parent;
		}
	}

	void IExpander.ExpandedChanged(bool isExpanded)
	{
		ContentHeight = isExpanded ? MaximumContentHeight : MinimumContentHeight;

		if (Command?.CanExecute(CommandParameter) is true)
		{
			Command.Execute(CommandParameter);
		}

		tappedEventManager.HandleEvent(this, new ExpandedChangedEventArgs(isExpanded), nameof(ExpandedChanged));
	}
}
