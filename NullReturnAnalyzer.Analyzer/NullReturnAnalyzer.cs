using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NullReturnAnalyzer.Analyzer {
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class NullReturnAnalyzer : DiagnosticAnalyzer {
		public static readonly DiagnosticDescriptor MissingNullabilityAttributeByValueUsageDiagnostic =
			new DiagnosticDescriptor(
				"NR1001",
				"Missing nullability attribute (value usage)",
				"Method returns null value, but it is not marked with [CanBeNull]",
				"Usage",
				DiagnosticSeverity.Warning,
				true);

		public static readonly DiagnosticDescriptor MissingNullabilityAttributeByAttributeUsageDiagnostic =
			new DiagnosticDescriptor(
				"NR1002",
				"Missing nullability attribute (method usage)",
				"Method returns value from method with [CanBeNull] attribute, but it is not marked with [CanBeNull]",
				"Usage",
				DiagnosticSeverity.Warning,
				true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(new [] {
			MissingNullabilityAttributeByValueUsageDiagnostic,
			MissingNullabilityAttributeByAttributeUsageDiagnostic,
		});

		public override void Initialize(AnalysisContext context) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterOperationBlockStartAction(ctx => {
				var analyzer = new NullReturnBlockAnalyzer();
				ctx.RegisterOperationAction(analyzer.ValidateReturnOperation, OperationKind.Return);
			});
		}
	}
}