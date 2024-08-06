using System.Globalization;
using System.Text.RegularExpressions;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Converters;

sealed partial class MathExpression
{
	const NumberStyles numberStyle = NumberStyles.Float | NumberStyles.AllowThousands;

	static readonly IFormatProvider formatProvider = new CultureInfo("en-US");

	readonly IReadOnlyList<MathOperator> operators;
	readonly IReadOnlyList<object> arguments;

	internal MathExpression(string expression, IEnumerable<object>? arguments = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(expression, "Expression can't be null or empty.");

		var argumentList = arguments?.ToList() ?? [];

		Expression = expression.ToLower();

		var operators = new List<MathOperator>
		{
			new ("?", 3, MathOperatorPrecedence.Low, x => Convert.ToBoolean(x[0]) ? x[1] : x[2]),
			new ("==", 2, MathOperatorPrecedence.Low, x => object.Equals(x[0], x[1])),
			new ("!=", 2, MathOperatorPrecedence.Low, x => !object.Equals(x[0], x[1])),
			new ("&&", 2, MathOperatorPrecedence.Low, x => Convert.ToBoolean(x[0]) && Convert.ToBoolean(x[1])),
			new ("||", 2, MathOperatorPrecedence.Low, x => Convert.ToBoolean(x[0]) || Convert.ToBoolean(x[1])),
			new (">", 2, MathOperatorPrecedence.Low, x => Convert.ToDouble(x[0]) > Convert.ToDouble(x[1])),
			new (">=", 2, MathOperatorPrecedence.Low, x => Convert.ToDouble(x[0]) >= Convert.ToDouble(x[1])),
			new ("<", 2, MathOperatorPrecedence.Low, x => Convert.ToDouble(x[0]) < Convert.ToDouble(x[1])),
			new ("<=", 2, MathOperatorPrecedence.Low, x => Convert.ToDouble(x[0]) <= Convert.ToDouble(x[1])),
			new ("+", 2, MathOperatorPrecedence.Low, x => Convert.ToDouble(x[0]) + Convert.ToDouble(x[1])),
			new ("-", 2, MathOperatorPrecedence.Low, x => Convert.ToDouble(x[0]) - Convert.ToDouble(x[1])),
			new ("*", 2, MathOperatorPrecedence.Medium, x => Convert.ToDouble(x[0]) * Convert.ToDouble(x[1])),
			new ("/", 2, MathOperatorPrecedence.Medium, x => Convert.ToDouble(x[0]) / Convert.ToDouble(x[1])),
			new ("%", 2, MathOperatorPrecedence.Medium, x => Convert.ToDouble(x[0]) % Convert.ToDouble(x[1])),
			new ("abs", 1, MathOperatorPrecedence.Medium, x => Math.Abs(Convert.ToDouble(x[0]))),
			new ("acos", 1, MathOperatorPrecedence.Medium, x => Math.Acos(Convert.ToDouble(x[0]))),
			new ("asin", 1, MathOperatorPrecedence.Medium, x => Math.Asin(Convert.ToDouble(x[0]))),
			new ("atan", 1, MathOperatorPrecedence.Medium, x => Math.Atan(Convert.ToDouble(x[0]))),
			new ("atan2", 2, MathOperatorPrecedence.Medium, x => Math.Atan2(Convert.ToDouble(x[0]), Convert.ToDouble(x[1]))),
			new ("ceiling", 1, MathOperatorPrecedence.Medium, x => Math.Ceiling(Convert.ToDouble(x[0]))),
			new ("cos", 1, MathOperatorPrecedence.Medium, x => Math.Cos(Convert.ToDouble(x[0]))),
			new ("cosh", 1, MathOperatorPrecedence.Medium, x => Math.Cosh(Convert.ToDouble(x[0]))),
			new ("exp", 1, MathOperatorPrecedence.Medium, x => Math.Exp(Convert.ToDouble(x[0]))),
			new ("floor", 1, MathOperatorPrecedence.Medium, x => Math.Floor(Convert.ToDouble(x[0]))),
			new ("ieeeremainder", 2, MathOperatorPrecedence.Medium, x => Math.IEEERemainder(Convert.ToDouble(x[0]), Convert.ToDouble(x[1]))),
			new ("log", 2, MathOperatorPrecedence.Medium, x => Math.Log(Convert.ToDouble(x[0]), Convert.ToDouble(x[1]))),
			new ("log10", 1, MathOperatorPrecedence.Medium, x => Math.Log10(Convert.ToDouble(x[0]))),
			new ("max", 2, MathOperatorPrecedence.Medium, x => Math.Max(Convert.ToDouble(x[0]), Convert.ToDouble(x[1]))),
			new ("min", 2, MathOperatorPrecedence.Medium, x => Math.Min(Convert.ToDouble(x[0]), Convert.ToDouble(x[1]))),
			new ("pow", 2, MathOperatorPrecedence.Medium, x => Math.Pow(Convert.ToDouble(x[0]), Convert.ToDouble(x[1]))),
			new ("round", 2, MathOperatorPrecedence.Medium, x => Math.Round(Convert.ToDouble(x[0]), Convert.ToInt32(x[1]))),
			new ("sign", 1, MathOperatorPrecedence.Medium, x => Math.Sign(Convert.ToDouble(x[0]))),
			new ("sin", 1, MathOperatorPrecedence.Medium, x => Math.Sin(Convert.ToDouble(x[0]))),
			new ("sinh", 1, MathOperatorPrecedence.Medium, x => Math.Sinh(Convert.ToDouble(x[0]))),
			new ("sqrt", 1, MathOperatorPrecedence.Medium, x => Math.Sqrt(Convert.ToDouble(x[0]))),
			new ("tan", 1, MathOperatorPrecedence.Medium, x => Math.Tan(Convert.ToDouble(x[0]))),
			new ("tanh", 1, MathOperatorPrecedence.Medium, x => Math.Tanh(Convert.ToDouble(x[0]))),
			new ("truncate", 1, MathOperatorPrecedence.Medium, x => Math.Truncate(Convert.ToDouble(x[0]))),
			new ("^", 2, MathOperatorPrecedence.High, x => Math.Pow(Convert.ToDouble(x[0]), Convert.ToDouble(x[1]))),
			new ("pi", 0, MathOperatorPrecedence.Constant, _ => Math.PI),
			new ("e", 0, MathOperatorPrecedence.Constant, _ => Math.E),
			new ("true", 0, MathOperatorPrecedence.Constant, _ => true),
			new ("false", 0, MathOperatorPrecedence.Constant, _ => false),
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

	public object Calculate()
	{
		if (!ParseExpression())
		{
			throw new ArgumentException("Invalid math expression.");
		}

		var stack = new Stack<object>();

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

			var args = new List<object>();
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

	bool ParseEquality() => ParseBinaryOperators(EqualityOperators(), ParseLogical);

	[GeneratedRegex("""^(\&\&|\|\|)""")]
	private static partial Regex LogicalOperators();

	bool ParseLogical() => ParseBinaryOperators(LogicalOperators(), ParseCompare);

	[GeneratedRegex("""^(\>\=?|\<\=?)""")]
	private static partial Regex CompareOperators();

	bool ParseCompare() => ParseBinaryOperators(CompareOperators(), ParseSum);

	[GeneratedRegex("""^(\+|\-)""")]
	private static partial Regex SumOperators();

	bool ParseSum() => ParseBinaryOperators(SumOperators(), ParseProduct);

	[GeneratedRegex("""^(\*|\/|\%)""")]
	private static partial Regex ProductOperators();

	bool ParseProduct() => ParseBinaryOperators(ProductOperators(), ParsePower);

	[GeneratedRegex("""^(\^)""")]
	private static partial Regex PowerOperator();

	bool ParsePower() => ParseBinaryOperators(PowerOperator(), ParsePrimary);

	bool ParseBinaryOperators(Regex BinaryOperators, Func<bool> ParseNext)
	{
		if (!ParseNext())
		{
			return false;
		}
		int index = CurrentPosition;
		while (ParsePattern(BinaryOperators))
		{
			string Operator = new string(MatchedString);
			if (!ParseNext())
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
	private static partial Regex Constants();

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

		if (ParsePattern(Constants()))
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
