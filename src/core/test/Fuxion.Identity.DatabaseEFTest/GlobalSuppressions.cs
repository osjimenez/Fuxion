﻿
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Usage",
	"xUnit1026:Theory methods should use all of their parameters",
	Justification = "The parameter _ in theories is for display purpose only")]

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>", Scope = "member", Target = "~M:Fuxion.Identity.DatabaseEFTest.IdentityDatabaseEFTestRepository.RemoveAsync(System.String)~System.Threading.Tasks.Task")]

