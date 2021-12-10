using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace NullReturnAnalyzer.Analyzer {
	sealed class NullReturnBlockAnalyzer {
		bool _isAlreadyReported;

		public void ValidateReturnOperation(OperationAnalysisContext context) {
			var operation     = context.Operation;
			var bodyOperation = LookupParentOperationBody(operation);
			if ( (bodyOperation == null) || (bodyOperation.Kind != OperationKind.MethodBodyOperation) ) {
				return;
			}
			var bodySyntax = bodyOperation.Syntax;
			if ( !bodySyntax.IsKind(SyntaxKind.MethodDeclaration) ) {
				return;
			}
			var symbol = context.ContainingSymbol;
			if ( symbol.Kind != SymbolKind.Method ) {
				return;
			}
			var methodSymbol = (IMethodSymbol)symbol;
			if ( !IsNullableMethodReturn(methodSymbol) || HasCanBeNullAttribute(methodSymbol) || HasCanBeNullAttributeAtParent(methodSymbol) ) {
				return;
			}
			var methodDeclaration = (MethodDeclarationSyntax)bodyOperation.Syntax;
			var literals = operation
				.Descendants()
				.OfType<ILiteralOperation>();
			foreach ( var literal in literals ) {
				var isLiteralValid = ValidateLiteral(literal);
				if ( !isLiteralValid ) {
					TryReportDiagnostic(
						context, bodyOperation, methodDeclaration,
						NullReturnAnalyzer.MissingNullabilityAttributeByValueUsageDiagnostic);
				}
			}
			var firstInvocation = operation
				.Descendants()
				.OfType<IInvocationOperation>()
				.FirstOrDefault();
			if ( firstInvocation == null ) {
				return;
			}
			var isInvocationValid = ValidateInvocation(firstInvocation);
			if ( !isInvocationValid ) {
				TryReportDiagnostic(
					context, bodyOperation, methodDeclaration,
					NullReturnAnalyzer.MissingNullabilityAttributeByAttributeUsageDiagnostic);
			}
		}

		bool IsNullableMethodReturn(IMethodSymbol methodSymbol) {
			if ( methodSymbol.ReturnsVoid ) {
				return false;
			}
			var returnType = methodSymbol.ReturnType;
			return !returnType.IsValueType;
		}

		IOperation LookupParentOperationBody(IOperation operation) {
			while ( (operation != null) && (operation.Kind != OperationKind.MethodBodyOperation) ) {
				operation = operation.Parent;
			}
			return operation;
		}

		bool IsArgumentLiteral(IOperation operation) {
			while ( operation != null ) {
				if ( operation.Parent == null ) {
					return false;
				}
				operation = operation.Parent;
				if ( (operation.Kind == OperationKind.Argument) || (operation.Kind == OperationKind.BinaryOperator) ) {
					return true;
				}
			}
			return false;
		}

		bool HasCanBeNullAttribute(ISymbol methodSymbol) =>
			IsContainsCanBeNullAttributeData(methodSymbol.GetAttributes());

		bool HasCanBeNullAttributeAtParent(IMethodSymbol methodSymbol) {
			var isOverride = methodSymbol.IsOverride;
			if ( isOverride ) {
				var overridenMethod = methodSymbol.OverriddenMethod;
				if ( HasCanBeNullAttribute(overridenMethod) ) {
					return true;
				}
			}
			foreach ( var typeInterface in methodSymbol.ContainingType.AllInterfaces ) {
				foreach ( var interfaceMethod in typeInterface.GetMembers(methodSymbol.Name) ) {
					if ( !Equals(methodSymbol, methodSymbol.ContainingType.FindImplementationForInterfaceMember(interfaceMethod)) ) {
						continue;
					}
					if ( HasCanBeNullAttribute(interfaceMethod) ) {
						return true;
					}
				}
			}
			return false;
		}

		bool ValidateLiteral(IOperation descendantOperation) {
			if ( IsArgumentLiteral(descendantOperation) ) {
				return true;
			}
			var constantValue = descendantOperation.ConstantValue;
			if ( !constantValue.HasValue || (constantValue.Value != null) ) {
				return true;
			}
			return false;
		}

		bool ValidateInvocation(IOperation descendantOperation) {
			var parent = descendantOperation.Parent;
			if ( parent != null ) {
				var kind = parent.Kind;
				if (
					(kind == OperationKind.FieldReference) ||
					(kind == OperationKind.PropertyReference) ||
					(kind == OperationKind.ConditionalAccess) ||
					(kind == OperationKind.SimpleAssignment) ||
					(kind == OperationKind.ArrayInitializer)
				) {
					return true;
				}
			}
			var call         = (IInvocationOperation)descendantOperation;
			var targetMethod = call.TargetMethod;
			if ( !IsContainsCanBeNullAttributeData(targetMethod.GetAttributes()) ) {
				return true;
			}
			return false;
		}

		bool IsContainsCanBeNullAttributeData(ImmutableArray<AttributeData> attributes) {
			foreach ( var attribute in attributes ) {
				if ( attribute.AttributeClass.Name == "CanBeNullAttribute" ) {
					return true;
				}
			}
			return false;
		}

		void TryReportDiagnostic(
			OperationAnalysisContext context, IOperation bodyOperation, MethodDeclarationSyntax methodDeclaration, DiagnosticDescriptor descriptor) {
			if ( _isAlreadyReported ) {
				return;
			}
			_isAlreadyReported = true;
			var syntaxTree = bodyOperation.Syntax.SyntaxTree;
			var span       = methodDeclaration.Identifier.Span;
			var location   = Location.Create(syntaxTree, span);
			var diagnostic = Diagnostic.Create(descriptor, location);
			context.ReportDiagnostic(diagnostic);
		}
	}
}