namespace MtgCsvHelper.Tests;

public class CardMapFactoryTests
{
	[Fact]
	public void ReadableFormats_ExcludesAllWriteOnlyFormats()
	{
		CardMapFactory.ReadableFormats.Should().NotIntersectWith(CardMapFactory.WriteOnlyFormats,
			because: "write-only formats throw on GenerateReadMap and must not be offered as inputs");
	}

	[Fact]
	public void WritableFormats_ExcludesAllReadOnlyFormats()
	{
		CardMapFactory.WritableFormats.Should().NotIntersectWith(CardMapFactory.ReadOnlyFormats,
			because: "read-only formats throw on GenerateWriteMap and must not be offered as outputs");
	}

	[Fact]
	public void ReadableFormats_IsSupportedMinusWriteOnly()
	{
		CardMapFactory.ReadableFormats.Should().Equal(CardMapFactory.Supported.Except(CardMapFactory.WriteOnlyFormats));
	}

	[Fact]
	public void WritableFormats_IsSupportedMinusReadOnly()
	{
		CardMapFactory.WritableFormats.Should().Equal(CardMapFactory.Supported.Except(CardMapFactory.ReadOnlyFormats));
	}
}
