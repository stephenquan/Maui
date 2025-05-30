﻿using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToPercentYellowConverterTests : BaseOneWayConverterTest<ColorToPercentYellowConverter>
{
	public static readonly TheoryData<float, float, float, float, double> ValidInputData = new()
	{
		{
			0, 0, 0, 0, 0
		},
		{
			0, 0, 0, 1, 0
		},
		{
			0, 0, 1, 0, 0
		},
		{
			0, 0, 1, 1, 0
		},
		{
			0, 1, 0, 0, 1
		},
		{
			0, 1, 0, 1, 1
		},
		{
			0, 1, 1, 0, 0
		},
		{
			0, 1, 1, 1, 0
		},
		{
			1, 0, 0, 0, 1
		},
		{
			1, 0, 0, 1, 1
		},
		{
			1, 0, 1, 0, 0
		},
		{
			1, 0, 1, 1, 0
		},
		{
			1, 1, 0, 0, 1
		},
		{
			1, 1, 0, 1, 1
		},
		{
			1, 1, 1, 0, 0
		},
		{
			1, 1, 1, 1, 0
		},
		{
			0.5f, 0, 0, 1, 1
		},
		{
			0.5f, 0, 0, 0, 1
		},
		{
			0, 0.5f, 0, 1, 1
		},
		{
			0, 0.5f, 0, 0, 1
		},
		{
			0, 0, 0.5f, 1, 0
		},
		{
			0, 0, 0.5f, 0, 0
		},
		{
			0.5f, 0.5f, 0.5f, 1, 0
		},
		{
			0.5f, 0.5f, 0.5f, 0, 0
		},
		{
			0.25f, 0.25f, 0.25f, 1, 0
		},
		{
			0.25f, 0.25f, 0.25f, 0, 0
		},
		{
			0.25f, 0.25f, 1, 1, 0
		},
		{
			0.25f, 0.25f, 1, 0, 0
		},
		{
			0.25f, 1, 0.25f, 1, 0.75
		},
		{
			0.25f, 1, 0.25f, 0, 0.75
		},
		{
			0.75f, 1, 0.25f, 1, 0.75
		},
		{
			0.75f, 1, 0.25f, 0, 0.75
		},
		{
			0.75f, 0, 1, 1, 0
		},
		{
			0.75f, 0, 1, 0, 0
		},
	};

	[Theory]
	[MemberData(nameof(ValidInputData))]
	public void ColorToPercentYellowConverterValidInputTest(float red, float green, float blue, float alpha, double expectedResult)
	{
		var converter = new ColorToPercentYellowConverter();
		var color = new Color(red, green, blue, alpha);

		var resultConvertFrom = converter.ConvertFrom(color);
		var resultConvert = ((ICommunityToolkitValueConverter)converter).Convert(color, typeof(double), null, null);

		Assert.Equal(expectedResult, resultConvertFrom);
		Assert.Equal(expectedResult, resultConvert);
	}

	[Fact]
	public void ColorToPercentYellowCyanConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new ColorToPercentYellowConverter().ConvertFrom(null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToPercentYellowConverter()).Convert(null, typeof(double), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToPercentYellowConverter()).Convert(new Color(), null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}