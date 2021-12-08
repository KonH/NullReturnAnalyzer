using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using AnalyzerVerifier = Microsoft.CodeAnalysis.CSharp.Testing.NUnit.AnalyzerVerifier<NullReturnAnalyzer.Analyzer.NullReturnAnalyzer>;
using CodeFixVerifier = Microsoft.CodeAnalysis.CSharp.Testing.NUnit.CodeFixVerifier<NullReturnAnalyzer.Analyzer.NullReturnAnalyzer, NullReturnAnalyzer.Analyzer.NullReturnCodeFixProvider>;

namespace NullReturnAnalyzer.Tests {
	public sealed class Tests {
		string ExpectedValueId => Analyzer.NullReturnAnalyzer.MissingNullabilityAttributeByValueUsageDiagnostic.Id;

		string ExpectedValueMessage => Analyzer.NullReturnAnalyzer.MissingNullabilityAttributeByValueUsageDiagnostic.MessageFormat.ToString();

		string ExpectedAttributeId => Analyzer.NullReturnAnalyzer.MissingNullabilityAttributeByAttributeUsageDiagnostic.Id;

		string ExpectedAttributeMessage => Analyzer.NullReturnAnalyzer.MissingNullabilityAttributeByAttributeUsageDiagnostic.MessageFormat.ToString();

		[Test]
		public async Task IsWarningFoundOnAlwaysNullReturn() {
			var code = @"
public sealed class Samples {
	// Warning required
	object GetAlwaysNullObject() {
		return null;
	}
}";
			var expected = new[] {
				DiagnosticResult.CompilerWarning(ExpectedValueId).WithLocation(4, 9).WithMessage(ExpectedValueMessage),
			};
			await AnalyzerVerifier.VerifyAnalyzerAsync(code, expected);
		}

		[Test]
		public async Task IsWarningFoundOnAlwaysNullExpressionReturn() {
			var code = @"
public sealed class Samples {
	// Warning required
	object GetAlwaysNullObject() => null;
}";
			var expected = new[] {
				DiagnosticResult.CompilerWarning(ExpectedValueId).WithLocation(4, 9).WithMessage(ExpectedValueMessage),
			};
			await AnalyzerVerifier.VerifyAnalyzerAsync(code, expected);
		}

		[Test]
		public async Task IsWarningFoundOnSometimesNullReturnFirstReturn() {
			var code = @"
public sealed class Samples {
	// Warning required
	object GetSometimesNullObject1(int x) {
		if ( x % 10 == 0 ) {
			return null;
		}
		return new object();
	}
}";
			var expected = new[] {
				DiagnosticResult.CompilerWarning(ExpectedValueId).WithLocation(4, 9).WithMessage(ExpectedValueMessage),
			};
			await AnalyzerVerifier.VerifyAnalyzerAsync(code, expected);
		}

		[Test]
		public async Task IsWarningFoundOnSometimesNullReturnNextReturn() {
			var code = @"
public sealed class Samples {
	// Warning required
	object GetSometimesNullObject1(int x) {
		if ( x % 10 == 0 ) {
			return new object();
		}
		return null;
	}
}";
			var expected = new[] {
				DiagnosticResult.CompilerWarning(ExpectedValueId).WithLocation(4, 9).WithMessage(ExpectedValueMessage),
			};
			await AnalyzerVerifier.VerifyAnalyzerAsync(code, expected);
		}

		[Test]
		public async Task IsWarningFoundOnSometimesNullReturnWithOperator() {
			var code = @"
public sealed class Samples {
	// Warning required
	object GetSometimesNullObject(int x) => x == 1 ? new object() : null;
}";
			var expected = new[] {
				DiagnosticResult.CompilerWarning(ExpectedValueId).WithLocation(4, 9).WithMessage(ExpectedValueMessage),
			};
			await AnalyzerVerifier.VerifyAnalyzerAsync(code, expected);
		}

		[Test]
		public async Task IsWarningNotFoundOnNotNullReturn() {
			var code = @"
public sealed class Samples {
	// No warnings
	object GetNotNullObject() => new object();
}";
			await AnalyzerVerifier.VerifyAnalyzerAsync(code, Array.Empty<DiagnosticResult>());
		}

		[Test]
		public async Task IsWarningNotFoundOnNotNullReturnNullArgumentCornerCase() {
			var code = @"
public sealed class Samples {
	object GetNotNullObject(object obj) => new object();

	// No warnings
	object GetNotNullObject() => GetNotNullObject(null);
}";
			await AnalyzerVerifier.VerifyAnalyzerAsync(code, Array.Empty<DiagnosticResult>());
		}

		[Test]
		public async Task IsWarningNotFoundOnNotNullReturnNullComparisonCornerCase() {
			var code = @"
public sealed class Samples {
	// No warnings
	object GetNotNullObject(object arg) => (arg != null) ? new object() : new object();
}";
			await AnalyzerVerifier.VerifyAnalyzerAsync(code, Array.Empty<DiagnosticResult>());
		}

		[Test]
		public async Task IsWarningNotFoundOnNullReturnInheritanceCornerCase() {
			var code = @"
using System;

public sealed class CanBeNullAttribute : Attribute {}

public interface IInterface {
	[CanBeNull]
	object GetNullObject();
}

public sealed class Impl : IInterface {
	// No warnings
	public object GetNullObject() => null;
}

public class BaseClass {
	[CanBeNull]
	public virtual object GetNullObject() => null;
}

public sealed class ChildClass : BaseClass {
	// No warnings
	public override object GetNullObject() => null;
}";
			await AnalyzerVerifier.VerifyAnalyzerAsync(code, Array.Empty<DiagnosticResult>());
		}

		[Test]
		public async Task IsWarningNotFoundOnNullReturnWithAttribute() {
			var code = @"
using System;

public sealed class Samples {
	public sealed class CanBeNullAttribute : Attribute {}

	// No warnings
	[CanBeNull]
	object GetNullObject() => null;
}";
			await AnalyzerVerifier.VerifyAnalyzerAsync(code, Array.Empty<DiagnosticResult>());
		}

		[Test]
		public async Task IsWarningNotFoundOnReturnWithInstanceCall() {
			var code = @"
using System;

public sealed class Samples {
	public sealed class CanBeNullAttribute : Attribute {}

	[CanBeNull]
	object GetNullObject() => null;

	// No warnings (NRE check should handled by IDE)
	string GetNullObjectToString() => GetNullObject().ToString();
}";
			await AnalyzerVerifier.VerifyAnalyzerAsync(code, Array.Empty<DiagnosticResult>());
		}

		[Test]
		public async Task IsWarningNotFoundOnReturnWithInstanceProperty() {
			var code = @"
using System;

public sealed class Samples {
	public sealed class TestObject {
		public int X { get; set; }
	}

	public sealed class CanBeNullAttribute : Attribute {}

	[CanBeNull]
	TestObject GetNullObject() => null;

	// No warnings (NRE check should handled by IDE)
	int GetNullObjectToString() => GetNullObject().X;
}";
			await AnalyzerVerifier.VerifyAnalyzerAsync(code, Array.Empty<DiagnosticResult>());
		}

		[Test]
		public async Task IsWarningNotFoundOnReturnWithInstanceField() {
			var code = @"
using System;

public sealed class Samples {
	public sealed class TestObject {
		public int X;
	}

	public sealed class CanBeNullAttribute : Attribute {}

	[CanBeNull]
	TestObject GetNullObject() => null;

	// No warnings (NRE check should handled by IDE)
	int GetNullObjectToString() => GetNullObject().X;
}";
			await AnalyzerVerifier.VerifyAnalyzerAsync(code, Array.Empty<DiagnosticResult>());
		}

		[Test]
		public async Task IsWarningNotFoundOnReturnWithInstancePropertyNullSafe() {
			var code = @"
using System;

public sealed class Samples {
	public sealed class TestObject {
		public int X { get; set; }
	}

	public sealed class CanBeNullAttribute : Attribute {}

	[CanBeNull]
	TestObject GetNullObject() => null;

	// No warnings
	int GetNullObjectToString() => GetNullObject()?.X ?? 0;
}";
			await AnalyzerVerifier.VerifyAnalyzerAsync(code, Array.Empty<DiagnosticResult>());
		}

		[Test]
		public async Task IsWarningNotFoundOnReturnWithInstanceFieldNullSafe() {
			var code = @"
using System;

public sealed class Samples {
	public sealed class TestObject {
		public int X;
	}

	public sealed class CanBeNullAttribute : Attribute {}

	[CanBeNull]
	TestObject GetNullObject() => null;

	// No warnings
	int GetNullObjectToString() => GetNullObject()?.X ?? 0;
}";
			await AnalyzerVerifier.VerifyAnalyzerAsync(code, Array.Empty<DiagnosticResult>());
		}

		[Test]
		public async Task IsWarningNotFoundOnNullCheckReturn() {
			var code = @"
using System;

public sealed class Samples {
	public sealed class CanBeNullAttribute : Attribute {}

	[CanBeNull]
	object GetNullObject() => null;

	// No warnings
	bool IsObjectNull() => GetNullObject() == null;
}";
			await AnalyzerVerifier.VerifyAnalyzerAsync(code, Array.Empty<DiagnosticResult>());
		}

		[Test]
		public async Task IsWarningFoundOnNullChainReturn() {
			var code = @"
using System;

public sealed class Samples {
	public sealed class CanBeNullAttribute : Attribute {}

	[CanBeNull]
	object GetAlwaysNullObjectWithAnnotation() => null;

	// Warning required
	object NullObjectChain() => GetAlwaysNullObjectWithAnnotation();
}";
			var expected = new[] {
				DiagnosticResult.CompilerWarning(ExpectedAttributeId).WithLocation(11, 9).WithMessage(ExpectedAttributeMessage),
			};
			await AnalyzerVerifier.VerifyAnalyzerAsync(code, expected);
		}

		[Test]
		public async Task CodeFixForNullValueUsage() {
			var source = @"
using System;

public sealed class Samples {
	public sealed class CanBeNullAttribute : Attribute {}
	
	object GetAlwaysNullObject() {
		return null;
	}
}";
			var fixedSource = @"
using System;

public sealed class Samples {
	public sealed class CanBeNullAttribute : Attribute {}
	
	[CanBeNull]
	object GetAlwaysNullObject() {
		return null;
	}
}";
			var expected = new[] {
				DiagnosticResult.CompilerWarning(ExpectedValueId).WithLocation(7, 9).WithMessage(ExpectedValueMessage),
			};
			await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
		}

		[Test]
		public async Task CodeFixForNullAttributeUsage() {
			var source = @"
using System;

public sealed class Samples {
	public sealed class CanBeNullAttribute : Attribute {}

	[CanBeNull]
	object GetAlwaysNullObjectWithAnnotation() => null;

	object NullObjectChain() => GetAlwaysNullObjectWithAnnotation();
}";
			var fixedSource = @"
using System;

public sealed class Samples {
	public sealed class CanBeNullAttribute : Attribute {}

	[CanBeNull]
	object GetAlwaysNullObjectWithAnnotation() => null;

	[CanBeNull]
	object NullObjectChain() => GetAlwaysNullObjectWithAnnotation();
}";
			var expected = new[] {
				DiagnosticResult.CompilerWarning(ExpectedAttributeId).WithLocation(10, 9).WithMessage(ExpectedAttributeMessage),
			};
			await CodeFixVerifier.VerifyCodeFixAsync(source, expected, fixedSource);
		}
	}
}