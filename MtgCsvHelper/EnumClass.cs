using System.Reflection;

namespace MtgCsvHelper;

public abstract class EnumClass : IComparable
{
	public string Name { get; private set; }

	public int Id { get; private set; }

	protected EnumClass(int id, string name) => (Id, Name) = (id, name);

	public override string ToString() => Name;

	public static IEnumerable<T> GetAll<T>() where T : EnumClass =>
		typeof(T).GetFields(BindingFlags.Public |
							BindingFlags.Static |
							BindingFlags.DeclaredOnly)
					.Select(f => f.GetValue(null))
					.Cast<T>();

	public override bool Equals(object? obj)
	{
		if (obj is not EnumClass otherValue)
		{
			return false;
		}

		var typeMatches = GetType().Equals(obj.GetType());
		var valueMatches = Id.Equals(otherValue.Id);

		return typeMatches && valueMatches;
	}

	public override int GetHashCode() => Id.GetHashCode();

	public static int AbsoluteDifference(EnumClass firstValue, EnumClass secondValue)
	{
		var absoluteDifference = Math.Abs(firstValue.Id - secondValue.Id);
		return absoluteDifference;
	}

	public static T FromValue<T>(int value) where T : EnumClass
	{
		var matchingItem = Parse<T, int>(value, "value", item => item.Id == value);
		return matchingItem;
	}

	public static T FromDisplayName<T>(string displayName) where T : EnumClass
	{
		var matchingItem = Parse<T, string>(displayName, "display name", item => item.Name == displayName);
		return matchingItem;
	}

	public static T Parse<T, K>(K value, string description, Func<T, bool> predicate) where T : EnumClass
	{
		var matchingItem = GetAll<T>().FirstOrDefault(predicate);
		return matchingItem ?? throw new InvalidOperationException($"'{value}' is not a valid {description} in {typeof(T)}");
	}

	public int CompareTo(object? obj) => Id.CompareTo((obj as EnumClass)?.Id);
}
