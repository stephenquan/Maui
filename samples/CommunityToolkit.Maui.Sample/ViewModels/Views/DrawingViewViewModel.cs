﻿using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class DrawingViewViewModel : BaseViewModel
{
	readonly IFileSaver fileSaver;

	public List<DrawingViewOutputOption> AvailableOutputOptions { get; } = [DrawingViewOutputOption.Lines, DrawingViewOutputOption.FullCanvas];

	[ObservableProperty]
	public partial string Logs { get; private set; } = string.Empty;

	[ObservableProperty]
	public partial DrawingViewOutputOption SelectedOutputOption { get; set; } = DrawingViewOutputOption.Lines;

	public double CanvasHeight { get; set; }

	public double CanvasWidth { get; set; }

	public DrawingViewViewModel(IFileSaver fileSaver)
	{
		this.fileSaver = fileSaver;

		DrawingLineStartedCommand = new Command<PointF>(point => Logs = "DrawingLineStartedCommand executed." + Environment.NewLine + $"Point: {point.X}:{point.Y}" + Environment.NewLine + Environment.NewLine + Logs);
		DrawingLineCancelledCommand = new Command(_ => Logs = "DrawingLineCancelledCommand executed." + Environment.NewLine + Environment.NewLine + Logs);
		PointDrawnCommand = new Command<PointF>(point => Logs = "PointDrawnCommand executed." + Environment.NewLine + $"Point: {point.X}:{point.Y}" + Environment.NewLine + Environment.NewLine + Logs);
		DrawingLineCompletedCommand = new Command<IDrawingLine>(line => Logs = "DrawingLineCompletedCommand executed." + Environment.NewLine + $"Line points count: {line.Points.Count}" + Environment.NewLine + Environment.NewLine + Logs);

		ClearLinesCommand = new Command(Lines.Clear);

		AddNewLineCommand = new Command<DrawingView>(drawingView =>
		{
			var width = double.IsNaN(drawingView.Width) || drawingView.Width <= 1 ? 200 : drawingView.Width;
			var height = double.IsNaN(drawingView.Height) || drawingView.Height <= 1 ? 200 : drawingView.Height;

			Lines.Add(new DrawingLine()
			{
				Points = new(GeneratePoints(10, width, height)),
				LineColor = Color.FromRgb(Random.Shared.Next(255), Random.Shared.Next(255), Random.Shared.Next(255)),
				LineWidth = 10,
				ShouldSmoothPathWhenDrawn = true,
				Granularity = 5
			});
		});
	}

	public ObservableCollection<IDrawingLine> Lines { get; } = [];

	public ICommand PointDrawnCommand { get; }
	public ICommand DrawingLineStartedCommand { get; }
	public ICommand DrawingLineCancelledCommand { get; }
	public ICommand DrawingLineCompletedCommand { get; }
	public ICommand ClearLinesCommand { get; }
	public ICommand AddNewLineCommand { get; }

	public static IEnumerable<PointF> GeneratePoints(int count, double viewWidth, double viewHeight)
	{
		var paddedViewWidth = Math.Clamp(viewWidth - 10, 1, viewWidth);
		var paddedViewHeight = Math.Clamp(viewHeight - 10, 1, viewHeight);

		var maxWidthInt = (int)Math.Round(paddedViewWidth, MidpointRounding.AwayFromZero);
		var maxHeightInt = (int)Math.Round(paddedViewHeight, MidpointRounding.AwayFromZero);

		for (var i = 0; i < count; i++)
		{
			yield return new PointF(Random.Shared.Next(1, maxWidthInt), Random.Shared.Next(1, maxHeightInt));
		}
	}

	[RelayCommand]
	async Task Save(CancellationToken cancellationToken)
	{
		try
		{
			var options = SelectedOutputOption == DrawingViewOutputOption.Lines
				? ImageLineOptions.JustLines(Lines.ToList(), new Size(1920, 1080), Brush.Blue)
				: ImageLineOptions.FullCanvas(Lines.ToList(), new Size(1920, 1080), Brush.Blue, new Size(CanvasWidth, CanvasHeight));

			await using var stream = await DrawingView.GetImageStream(options, cancellationToken);

			await Permissions.RequestAsync<Permissions.StorageRead>().WaitAsync(cancellationToken);
			await Permissions.RequestAsync<Permissions.StorageWrite>().WaitAsync(cancellationToken);

			await fileSaver.SaveAsync("drawing.png", stream, cancellationToken);
		}
		catch (PermissionException e)
		{
			await Toast.Make($"Save Failed: {e.Message}").Show(cancellationToken);
		}
		catch (InvalidOperationException)
		{
			await Toast.Make("Save Failed: No Lines Drawn").Show(cancellationToken);
		}
	}
}