using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Usage", "CA2214:Do not call overridable methods in constructors",
	Scope = "type", Target = "~T:MtgCsvHelper.Maps.PhysicalCardMap",
	Justification = "ConfigureSetCode/ConfigureSetName are configuration extension points invoked during ClassMap setup, which by design happens in the constructor. The overrides use only static lookup tables and the MemberMap passed to them — never derived instance state — so the partially-constructed-object hazard CA2214 guards against does not apply.")]
