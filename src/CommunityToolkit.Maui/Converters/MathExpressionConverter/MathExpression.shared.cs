using System.Globalization;
using System.Text.RegularExpressions;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Converters;

sealed partial class MathExpression
{
	const NumberStyles numberStyle = NumberStyles.Float | NumberStyles.AllowThousands;

	static readonly IFormatProvider formatProvider = new CultureInfo("en-US");

	readonly IReadOnlyList<MathOperator> operators;
	readonly IReadOnlyList<double> arguments;

	internal MathExpression(string expression, IEnumerable<double>? arguments = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(expression, "Expression can't be null or empty.");

		var argumentList = arguments?.ToList() ?? [];

		Expression = expression.ToLower();

		var operators = new List<MathOperator>
		{
			new ("?", 3, MathOperatorPrecedence.Low, x => x[0] != 0d ? x[1] : x[2]),
			new ("==", 2, MathOperatorPrecedence.Low, x => x[0] == x[1] ? 1d : 0d),
			new ("!=", 2, MathOperatorPrecedence.Low, x => x[0] != x[1] ? 1d : 0d),
			new ("&&", 2, MathOperatorPrecedence.Low, x => x[0] != 0d && x[1] != 0d ? 1d : 0d),
			new ("||", 2, MathOperatorPrecedence.Low, x => x[0] != 0d || x[1] != 0d ? 1d : 0d),
			new (">", 2, MathOperatorPrecedence.Low, x => x[0] > x[1] ? 1d : 0d),
			new (">=", 2, MathOperatorPrecedence.Low, x => x[0] >= x[1] ? 1d : 0d),
			new ("<", 2, MathOperatorPrecedence.Low, x => x[0] < x[1] ? 1d : 0d),
			new ("<=", 2, MathOperatorPrecedence.Low, x => x[0] <= x[1] ? 1d : 0d),
			new ("+", 2, MathOperatorPrecedence.Low, x => x[0] + x[1]),
			new ("-", 2, MathOperatorPrecedence.Low, x => x[0] - x[1]),
			new ("*", 2, MathOperatorPrecedence.Medium, x => x[0] * x[1]),
			new ("/", 2, MathOperatorPrecedence.Medium, x => x[0] / x[1]),
			new ("%", 2, MathOperatorPrecedence.Medium, x => x[0] % x[1]),
			new ("abs", 1, MathOperatorPrecedence.Medium, x => Math.Abs(x[0])),
			new ("acos", 1, MathOperatorPrecedence.Medium, x => Math.Acos(x[0])),
			new ("asin", 1, MathOperatorPrecedence.Medium, x => Math.Asin(x[0])),
			new ("atan", 1, MathOperatorPrecedence.Medium, x => Math.Atan(x[0])),
			new ("atan2", 2, MathOperatorPrecedence.Medium, x => Math.Atan2(x[0], x[1])),
			new ("ceiling", 1, MathOperatorPrecedence.Medium, x => Math.Ceiling(x[0])),
			new ("cos", 1, MathOperatorPrecedence.Medium, x => Math.Cos(x[0])),
			new ("cosh", 1, MathOperatorPrecedence.Medium, x => Math.Cosh(x[0])),
			new ("exp", 1, MathOperatorPrecedence.Medium, x => Math.Exp(x[0])),
			new ("floor", 1, MathOperatorPrecedence.Medium, x => Math.Floor(x[0])),
			new ("ieeeremainder", 2, MathOperatorPrecedence.Medium, x => Math.IEEERemainder(x[0], x[1])),
			new ("log", 2, MathOperatorPrecedence.Medium, x => Math.Log(x[0], x[1])),
			new ("log10", 1, MathOperatorPrecedence.Medium, x => Math.Log10(x[0])),
			new ("max", 2, MathOperatorPrecedence.Medium, x => Math.Max(x[0], x[1])),
			new ("min", 2, MathOperatorPrecedence.Medium, x => Math.Min(x[0], x[1])),
			new ("pow", 2, MathOperatorPrecedence.Medium, x => Math.Pow(x[0], x[1])),
			new ("round", 2, MathOperatorPrecedence.Medium, x => Math.Round(x[0], Convert.ToInt32(x[1]))),
			new ("sign", 1, MathOperatorPrecedence.Medium, x => Math.Sign(x[0])),
			new ("sin", 1, MathOperatorPrecedence.Medium, x => Math.Sin(x[0])),
			new ("sinh", 1, MathOperatorPrecedence.Medium, x => Math.Sinh(x[0])),
			new ("sqrt", 1, MathOperatorPrecedence.Medium, x => Math.Sqrt(x[0])),
			new ("tan", 1, MathOperatorPrecedence.Medium, x => Math.Tan(x[0])),
			new ("tanh", 1, MathOperatorPrecedence.Medium, x => Math.Tanh(x[0])),
			new ("truncate", 1, MathOperatorPrecedence.Medium, x => Math.Truncate(x[0])),
			new ("^", 2, MathOperatorPrecedence.High, x => Math.Pow(x[0], x[1])),
			new ("pi", 0, MathOperatorPrecedence.Constant, _ => Math.PI),
			new ("e", 0, MathOperatorPrecedence.Constant, _ => Math.E),
			new ("true", 0, MathOperatorPrecedence.Constant, _ => 1d),
			new ("false", 0, MathOperatorPrecedence.Constant, _ => 0d),
		};

		if (argumentList.Count > 0)
		{
			operators.Add(new MathOperator("x", 0, MathOperatorPrecedence.Constant, _ => argumentList[0]));
		}

		for (var i = 0; i < argumentList.Count; i++)
		{
			var index = i;
			operators.Add(new MathOperator($"x{i}", 0, MathOperatorPrecedence.Constant, _ => argumentList[index]));
		}

		this.operators = operators;
		this.arguments = argumentList;
	}

	internal string Expression { get; }

	internal int CurrentPosition { get; set; } = 0;

	internal string MatchedString { get; set; } = string.Empty;

	internal List<string> RPM { get; } = new();

	public double Calculate()
	{
		if (!ParseExpression())
		{
			throw new ArgumentException("Invalid math expression.");
		}

		var stack = new Stack<double>();

		foreach (var value in RPM)
		{
			if (double.TryParse(value, numberStyle, formatProvider, out var numeric))
			{
				stack.Push(numeric);
				continue;
			}

			var mathOperator = operators.FirstOrDefault(x => x.Name == value) ??
				throw new ArgumentException($"Invalid math expression. Can't find operator or value with name \"{value}\".");

			if (mathOperator.Precedence is MathOperatorPrecedence.Constant)
			{
				stack.Push(mathOperator.CalculateFunc([]));
				continue;
			}

			var operatorNumericCount = mathOperator.NumericCount;

			if (stack.Count < operatorNumericCount)
			{
				throw new ArgumentException("Invalid math expression.");
			}

			var args = new List<double>();
			for (var j = 0; j < operatorNumericCount; j++)
			{
				args.Add(stack.Pop());
			}

			args.Reverse();

			stack.Push(mathOperator.CalculateFunc([.. args]));
		}

		if (stack.Count != 1)
		{
			throw new ArgumentException("Invalid math expression.");
		}

		return stack.Pop();
	}

	bool ParseExpression()
	{
		CurrentPosition = 0;
		RPM.Clear();
		return ParseExpr() && CurrentPosition == Expression.Length;
	}

	bool ParseExpr()
	{
		return ParseConditional();
	}

	[GeneratedRegex("""^(\?)""")]
	private static partial Regex ConditionalStart();

	[GeneratedRegex("""^(\:)""")]
	private static partial Regex ConditionalElse();

	bool ParseConditional()
	{
		if (!ParseEquality())
		{
			return false;
		}

		if (!ParsePattern(ConditionalStart()))
		{
			return true;
		}

		if (!ParseEquality())
		{
			return false;
		}

		if (!ParsePattern(ConditionalElse()))
		{
			return false;
		}

		if (!ParseEquality())
		{
			return false;
		}

		RPM.Add("?");
		return true;
	}

	[GeneratedRegex("""^(==|!=)""")]
	private static partial Regex EqualityOperators();

	bool ParseEquality()
	{
		if (!ParseLogical())
		{
			return false;
		}
		int index = CurrentPosition;
		while (ParsePattern(EqualityOperators()))
		{
			string Operator = new string(MatchedString);
			if (!ParseLogical())
			{
				CurrentPosition = index;
				return false;
			}
			RPM.Add(Operator);
			index = CurrentPosition;
		}
		return true;
	}

	[GeneratedRegex("""^(\&\&|\|\|)""")]
	private static partial Regex LogicalOperators();

	bool ParseLogical()
	{
		if (!ParseCompare())
		{
			return false;
		}
		int index = CurrentPosition;
		while (ParsePattern(LogicalOperators()))
		{
			string Operator = new string(MatchedString);
			if (!ParseCompare())
			{
				CurrentPosition = index;
				return false;
			}
			RPM.Add(Operator);
			index = CurrentPosition;
		}
		return true;
	}

	[GeneratedRegex("""^(\>\=?|\<\=?)""")]
	private static partial Regex CompareOperators();

	bool ParseCompare()
	{
		if (!ParseSum())
		{
			return false;
		}
		int index = CurrentPosition;
		while (ParsePattern(CompareOperators()))
		{
			string Operator = new string(MatchedString);
			if (!ParseSum())
			{
				CurrentPosition = index;
				return false;
			}
			RPM.Add(Operator);
			index = CurrentPosition;
		}
		return true;
	}

	[GeneratedRegex("""^(\+|\-)""")]
	private static partial Regex SumOperators();

	bool ParseSum()
	{
		if (!ParseProduct())
		{
			return false;
		}
		int index = CurrentPosition;
		while (ParsePattern(SumOperators()))
		{
			string Operator = new string(MatchedString);
			if (!ParseProduct())
			{
				CurrentPosition = index;
				return false;
			}
			RPM.Add(Operator);
			index = CurrentPosition;
		}
		return true;
	}

	[GeneratedRegex("""^(\/|\*|\%)""")]
	private static partial Regex ProductOperators();

	bool ParseProduct()
	{
		if (!ParsePower())
		{
			return false;
		}
		int index = CurrentPosition;
		while (ParsePattern(ProductOperators()))
		{
			string Operator = new string(MatchedString);
			if (!ParsePower())
			{
				CurrentPosition = index;
				return false;
			}
			RPM.Add(Operator);
			index = CurrentPosition;
		}
		return true;
	}

	[GeneratedRegex("""^(\^)""")]
	private static partial Regex PowerOperator();

	bool ParsePower()
	{
		if (!ParsePrimary())
		{
			return false;
		}
		int index = CurrentPosition;
		while (ParsePattern(PowerOperator()))
		{
			string Operator = new string(MatchedString);
			if (!ParsePrimary())
			{
				CurrentPosition = index;
				return false;
			}
			RPM.Add(Operator);
			index = CurrentPosition;
		}
		return true;
	}

	[GeneratedRegex("""^(\-?\d+\.\d+|\-?\d+)""")]
	private static partial Regex NumberPattern();

	[GeneratedRegex("""^(\w+)""")]
	private static partial Regex Ident();

	[GeneratedRegex("""^(\()""")]
	private static partial Regex ParenStart();

	[GeneratedRegex("""^(\))""")]
	private static partial Regex ParenEnd();

	bool ParsePrimary()
	{
		if (ParsePattern(NumberPattern()))
		{
			RPM.Add(MatchedString);
			return true;
		}

		if (ParseFunction())
		{
			return true;
		}

		if (ParsePattern(Ident()))
		{
			RPM.Add(MatchedString);
			return true;
		}

		int index = CurrentPosition;
		if (ParsePattern(ParenStart()))
		{
			if (!ParseExpr())
			{
				CurrentPosition = index;
				return false;
			}
			if (!ParsePattern(ParenEnd()))
			{
				CurrentPosition = index;
				return false;
			}
			return true;
		}

		return false;
	}

	[GeneratedRegex("""^(\w+)\(""")]
	private static partial Regex FunctionStart();

	[GeneratedRegex("""^(\,)""")]
	private static partial Regex Comma();

	[GeneratedRegex("""^(\))""")]
	private static partial Regex FunctionEnd();

	bool ParseFunction()
	{
		int index = CurrentPosition;
		if (!ParsePattern(FunctionStart()))
		{
			return false;
		}

		string FunctionName = MatchedString;

		if (!ParseExpr())
		{
			CurrentPosition = index;
			return false;
		}

		while (ParsePattern(Comma()))
		{
			if (!ParseExpr())
			{
				CurrentPosition = index;
				return false;
			}
			index = CurrentPosition;
		}

		if (!ParsePattern(FunctionEnd()))
		{
			CurrentPosition = index;
			return false;
		}

		RPM.Add(FunctionName);

		return true;
	}

	[GeneratedRegex("""^\s*""")]
	private static partial Regex Whitespace();

	public bool ParsePattern(Regex regex)
	{
		var match = Whitespace().Match(Expression.Substring(CurrentPosition));
		if (match.Success)
		{
			CurrentPosition += match.Length;
		}

		match = regex.Match(Expression.Substring(CurrentPosition));
		if (!match.Success)
		{
			return false;
		}
		CurrentPosition += match.Length;

		MatchedString = match.Groups[1].Value;

		match = Whitespace().Match(Expression.Substring(CurrentPosition));
		if (match.Success)
		{
			CurrentPosition += match.Length;
		}

		return true;
	}
}
