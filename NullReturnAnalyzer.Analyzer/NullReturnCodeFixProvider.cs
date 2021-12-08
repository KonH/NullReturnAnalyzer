using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace NullReturnAnalyzer.Analyzer {
	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public sealed class NullReturnCodeFixProvider : CodeFixProvider {
		const string Title = "Add [CanBeNull] attribute";

		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(new[] {
			NullReturnAnalyzer.MissingNullabilityAttributeByValueUsageDiagnostic.Id,
			NullReturnAnalyzer.MissingNullabilityAttributeByAttributeUsageDiagnostic.Id,
		});

		public override Task RegisterCodeFixesAsync(CodeFixContext context) {
			var diagnostic     = context.Diagnostics.First();
			var diagnosticSpan = diagnostic.Location.SourceSpan;
			var codeAction = CodeAction.Create(
				Title,
				c => ApplyCodeFix(context.Document, diagnosticSpan, c),
				Title
			);
			context.RegisterCodeFix(codeAction, diagnostic);
			return Task.CompletedTask;
		}

		public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		async Task<Document> ApplyCodeFix(Document document, TextSpan diagnosticSpan, CancellationToken ct) {
			var oldText        = await document.GetTextAsync(ct).ConfigureAwait(false);
			var prevLine       = oldText.Lines.Last(l => l.End < diagnosticSpan.Start);
			var targetLine     = oldText.Lines.First(l => l.End > diagnosticSpan.Start);
			var targetLineText = targetLine.ToString();
			var prefixLenght   = targetLineText.Length - targetLineText.TrimStart().Length;
			var prefixText     = targetLineText.Substring(0, prefixLenght);
			var insertSpan     = new TextSpan(prevLine.End, 0);
			var change         = new TextChange(insertSpan, $"\n{prefixText}[CanBeNull]");
			var newText        = oldText.WithChanges(change);
			return document.WithText(newText);
		}
	}
}