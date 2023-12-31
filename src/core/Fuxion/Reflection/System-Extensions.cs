﻿using Fuxion.Reflection;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace System.Reflection
{
	public static class MemberInfoExtensions
	{
		//public static TAttribute? GetCustomAttribute<TAttribute>(this MemberInfo member, bool inherit = true, bool exceptionIfNotFound = true, bool exceptionIfMoreThanOne = true) where TAttribute : Attribute
		//{
		//	object[] objAtts = member.GetCustomAttributes(typeof(TAttribute), inherit);
		//	IEnumerable<TAttribute> atts = objAtts?.Cast<TAttribute>();
		//	if (exceptionIfMoreThanOne && atts != null && atts.Count() > 1)
		//		throw new AttributeMoreThanOneException(member, typeof(TAttribute));
		//	var att = atts?.FirstOrDefault();
		//	if (exceptionIfNotFound && att == null)
		//		throw new AttributeNotFoundException(member, typeof(TAttribute));
		//	return att;
		//}
		/// <summary>
		/// Retrieves a custom attribute of a specified type that is applied to a specified
		/// member, and optionally inspects the ancestors of that member.
		/// </summary>
		/// <typeparam name="TAttribute">The type of attribute to search for.</typeparam>
		/// <param name="me">The member to inspect.</param>
		/// <param name="inherit">true to inspect the ancestors of element; otherwise, false.</param>
		/// <param name="exceptionIfNotFound">true to throw <see cref="AttributeNotFoundException"/> if custom attribute not found.</param>
		/// <returns></returns>
		public static TAttribute? GetCustomAttribute<TAttribute>(this MemberInfo me, bool inherit, [DoesNotReturnIf(true)] bool exceptionIfNotFound) where TAttribute : Attribute
			=> GetCustomAttribute<TAttribute>(me, inherit, exceptionIfNotFound, true);
		/// <summary>
		/// Retrieves a custom attribute of a specified type that is applied to a specified
		/// member, and optionally inspects the ancestors of that member.
		/// </summary>
		/// <typeparam name="TAttribute">The type of attribute to search for.</typeparam>
		/// <param name="me">The member to inspect.</param>
		/// <param name="inherit">true to inspect the ancestors of element; otherwise, false.</param>
		/// <param name="exceptionIfNotFound">true to throw <see cref="AttributeNotFoundException"/> if custom attribute not found.</param>
		/// <param name="exceptionIfMoreThanOne">true to throw <see cref="AttributeMoreThanOneException"/> if custom attribute found more than once.</param>
		/// <returns></returns>
		public static TAttribute? GetCustomAttribute<TAttribute>(this MemberInfo me, bool inherit, [DoesNotReturnIf(true)] bool exceptionIfNotFound, [DoesNotReturnIf(true)] bool exceptionIfMoreThanOne) where TAttribute : Attribute
		{
			var objAtts = me.GetCustomAttributes(typeof(TAttribute), inherit);
			var atts = objAtts?.Cast<TAttribute>();
			if (exceptionIfMoreThanOne && atts != null && atts.Count() > 1)
				throw new AttributeMoreThanOneException(me, typeof(TAttribute));
			var att = atts?.FirstOrDefault();
			if (exceptionIfNotFound && att == null)
				throw new AttributeNotFoundException(me, typeof(TAttribute));
			return att;
		}


		public static bool HasCustomAttribute<TAttribute>(this MemberInfo member, bool inherit = true, [DoesNotReturnIf(true)] bool exceptionIfMoreThanOne = true) where TAttribute : Attribute
		{
			var att = member.GetCustomAttribute<TAttribute>(inherit, false, exceptionIfMoreThanOne);
			return att != null;
		}
		public static string GetSignature(this MethodBase method,
			bool includeAccessModifiers = false,
			bool includeReturn = false,
			bool includeDeclaringType = true,
			bool useFullNames = false,
			bool includeParameters = true,
			bool includeParametersNames = false,
			Func<bool, bool, MethodBase, object?, string>? parametersFunction = null,
			object? parametersFunctionArguments = null
			)
		{
			StringBuilder res = new StringBuilder();
			// Access modifiers
			if (includeAccessModifiers)
			{
				if (method.IsPublic)
					res.Append("public ");
				else if (method.IsPrivate)
					res.Append("private ");
				else if (method.IsAssembly)
					res.Append("internal ");
				if (method.IsFamily)
					res.Append("protected ");
				if (method.IsStatic)
					res.Append("static ");
				if (method.IsVirtual)
					res.Append("virtual ");
				if (method.IsAbstract)
					res.Append("abstract ");
			}
			// Return type
			if (includeReturn)
			{
				if (method is MethodInfo mi)
					res.Append(mi.ReturnType.GetSignature(useFullNames) + " ");
				else
					res.Append("<ctor> ");
			}
			// Method name
			if (includeDeclaringType)
				res.Append(method.DeclaringType?.GetSignature(useFullNames) + ".");
			res.Append(method.Name);
			// Generics arguments
			if (method.IsGenericMethod)
			{
				res.Append("<");
				Type[] genericArgs = method.GetGenericArguments();
				for (int i = 0; i < genericArgs.Length; i++)
					res.Append((i > 0 ? ", " : "") + genericArgs[i].GetSignature(useFullNames));
				res.Append(">");
			}
			// Parameters
			if (includeParameters)
			{
				res.Append("(");
				if (parametersFunction != null)
				{
					res.Append(parametersFunction(useFullNames, includeParametersNames, method, parametersFunctionArguments));
				}
				else
				{
					ParameterInfo[] pars = method.GetParameters();
					for (int i = 0; i < pars.Length; i++)
					{
						ParameterInfo par = pars[i];
						if (i == 0 && method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
							res.Append("this ");
						if (par.ParameterType.IsByRef)
							res.Append("ref ");
						else if (par.IsOut)
							res.Append("out ");
						res.Append(par.ParameterType.GetSignature(useFullNames));
						if (includeParametersNames)
							res.Append(" " + par.Name);
						if (i < pars.Length - 1)
							res.Append(", ");
					}
				}
				res.Append(")");
			}
			return res.ToString();
		}
	}
}
