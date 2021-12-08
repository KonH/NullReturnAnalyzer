using JetBrains.Annotations;

namespace NullReturnAnalyzer.Samples {
	public sealed class Samples {
		// Warning required
		object GetAlwaysNullObject1() {
			return null;
		}

		// Warning required
		object GetAlwaysNullObject2() => null;

		// Warning required
		object GetSometimesNullObject1(int x) {
			if ( x % 10 == 0 ) {
				return null;
			}
			return new();
		}

		// Warning required
		object GetSometimesNullObject2(int x) {
			if ( x % 10 == 0 ) {
				return null;
			}
			return null;
		}

		// Warning required
		object GetSometimesNullObject3(int x) => x == 1 ? new() : null;

		// No warnings
		object GetNotNullObject() => new();

		[CanBeNull]
		object GetAlwaysNullObjectWithAnnotation() => null;

		// Warning required
		object NullObjectChain() => GetAlwaysNullObjectWithAnnotation();
	}
}