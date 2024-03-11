; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 2.0.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
LRN001 | Correctness | Warning | ThereIsOnlyOneRestrictedNamespaceAnalyzer
LRT001 | Correctness | Warning | AllTypesInNationalInstrumentsNamespaceAnalyzer
NI0017 | Correctness | Warning | DatabaseColumnsShouldBeNullableAnalyzer
NI1004 | Correctness | Warning | StringShouldBeInResourcesAnalyzer
NI1005 | Correctness | Warning | ReceiveWeakEventMustReturnTrueAnalyzer
NI1006 | Correctness | Warning | DoNotUseBannedMethodsAnalyzer
NI1007 | Correctness | Warning | TestClassesMustInheritFromAutoTestAnalyzer
NI1009 | Correctness | Disabled | ReferencedInternalMustHaveVisibleInternalAttributeAnalyzer
NI1015 | Correctness | Warning | AwaitInReadLockOrTransactionAnalyzer
NI1016 | Correctness | Warning | DoNotLockDirectlyOnPrivateMemberLockAnalyzer
NI1019 | Correctness | Warning | RecordWithEnumerablesShouldOverrideDefaultEqualityAnalyzer
NI1800 | Correctness | Warning | ApprovedNamespaceAnalyzer
NI1001 | Style | Warning | FieldsCamelCasedWithUnderscoreAnalyzer
NI1017 | Style | Warning | ChainOfMethodsWithLambdasAnalyzer
NI1704 | Style | Warning | SpellingAnalyzer
NI1728 | Style | Disabled | SpellingAnalyzer
