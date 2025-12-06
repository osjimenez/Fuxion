using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fuxion.Analyzers.CodeFixes;

/// <summary>
/// Provides code fixes for FX001: Member requiring non-null accessed on nullable type.
/// Offers automatic fixes to add null-conditional operator for safe member access.
/// </summary>
/// <remarks>
/// This code fix provider handles members marked with [RequiresNotNull] attribute.
/// When accessing such members on nullable types, it suggests using the null-conditional
/// operator (?) to safely handle potential null values.
/// 
/// The fix adds only ONE null-conditional operator at the point where the nullable value
/// is accessed, not before subsequent method calls, since [RequiresNotNull] members
/// never return null themselves (they throw exceptions instead).
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RequiresNotNullCodeFixProvider)), Shared]
public class RequiresNotNullCodeFixProvider : CodeFixProvider
{
	private const string Title = "Use null-conditional operator (?)";

	public sealed override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.RequiresNotNullViolation];

	public sealed override FixAllProvider GetFixAllProvider()
		=> WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		var diagnostic = context.Diagnostics.First();
		var diagnosticSpan = diagnostic.Location.SourceSpan;

		// Find the member access expression identified by the diagnostic
		var memberAccess = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
			.OfType<MemberAccessExpressionSyntax>().FirstOrDefault();

		if (memberAccess is null)
			return;

		// Register code fix
		context.RegisterCodeFix(
			CodeAction.Create(
				title: Title,
				createChangedDocument: c => AddNullConditionalAsync(context.Document, memberAccess, c),
				equivalenceKey: nameof(RequiresNotNullCodeFixProvider)),
			diagnostic);
	}

	private static async Task<Document> AddNullConditionalAsync(
		Document document,
		MemberAccessExpressionSyntax memberAccess,
		CancellationToken cancellationToken)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root is null)
			return document;

		// Get the expression chain: nullableValue.SomeProperty.RequiresNotNullMember
		// We need to find where the nullable value is accessed
		var expressionChain = GetExpressionChain(memberAccess);
		var fullChain = GetFullMemberAccessChain(memberAccess);

		// Build the new expression with null-conditional operator
		// Transform: nullableStr.Fx.Pod.BuildUriKeyPod(...)
		// Into: nullableStr?.Fx.Pod.BuildUriKeyPod(...)
		// 
		// Note: Only ONE null-conditional operator is needed at the nullable value.
		// Members with [RequiresNotNull] never return null (they throw exceptions),
		// so we don't need ?. before subsequent method calls.

		// Find the first member access in the chain (the one after the nullable value)
		var firstMemberAccess = expressionChain.FirstOrDefault();
		if (firstMemberAccess?.Expression is null)
			return document;

		// Create: nullableValue?.FirstMember
		var conditionalAccess = SyntaxFactory.ConditionalAccessExpression(
			firstMemberAccess.Expression,
			SyntaxFactory.MemberBindingExpression(firstMemberAccess.Name));

		// Rebuild the rest of the chain
		ExpressionSyntax newExpression = conditionalAccess;
		
		for (int i = 1; i < expressionChain.Count; i++)
		{
			newExpression = SyntaxFactory.MemberAccessExpression(
				SyntaxKind.SimpleMemberAccessExpression,
				newExpression,
				expressionChain[i].Name);
		}

		// Handle invocation if present
		var currentNode = memberAccess.Parent;
		while (currentNode is MemberAccessExpressionSyntax nextMemberAccess)
		{
			newExpression = SyntaxFactory.MemberAccessExpression(
				SyntaxKind.SimpleMemberAccessExpression,
				newExpression,
				nextMemberAccess.Name);
			
			currentNode = nextMemberAccess.Parent;
			
			if (currentNode is InvocationExpressionSyntax invocation)
			{
				newExpression = SyntaxFactory.InvocationExpression(
					newExpression,
					invocation.ArgumentList);
				break;
			}
		}

		if (currentNode is InvocationExpressionSyntax directInvocation && 
		    directInvocation.Expression == memberAccess)
		{
			newExpression = SyntaxFactory.InvocationExpression(
				newExpression,
				directInvocation.ArgumentList);
		}

		// Preserve trivia (whitespace, comments)
		newExpression = newExpression.WithTriviaFrom(fullChain);

		var newRoot = root.ReplaceNode(fullChain, newExpression);
		return document.WithSyntaxRoot(newRoot);
	}

	private static List<MemberAccessExpressionSyntax> GetExpressionChain(MemberAccessExpressionSyntax memberAccess)
	{
		var chain = new List<MemberAccessExpressionSyntax>();
		var current = memberAccess;

		while (current is not null)
		{
			chain.Insert(0, current);
			current = current.Expression as MemberAccessExpressionSyntax;
		}

		return chain;
	}

	private static SyntaxNode GetFullMemberAccessChain(MemberAccessExpressionSyntax start)
	{
		// Walk up to find the full expression chain
		var current = (SyntaxNode)start;
		
		while (current.Parent is MemberAccessExpressionSyntax or InvocationExpressionSyntax)
		{
			if (current.Parent is InvocationExpressionSyntax invocation && 
			    invocation.Expression == current)
			{
				return invocation;
			}
			current = current.Parent;
		}

		return current;
	}
}
