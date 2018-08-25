using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace UDProtean
{
    internal static class ReflectionEx
    {
		const BindingFlags BINDING_FLAGS = BindingFlags.CreateInstance | BindingFlags.Default | BindingFlags.Instance
										| BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

		public static IEnumerable<MemberInfo> GetMembers<Attr>(this Type type) where Attr : Attribute
		{
			foreach (MemberInfo member in type.GetTypeInfo().GetProperties(BINDING_FLAGS))
			{
				if (member.GetCustomAttribute<Attr>() != null)
					yield return member;
			}
			foreach (MemberInfo member in type.GetTypeInfo().GetFields(BINDING_FLAGS))
			{
				if (member.GetCustomAttribute<Attr>() != null)
					yield return member;
			}
		}

		public static IEnumerable<MethodInfo> GetMethods<Attr>(this Type type) where Attr : Attribute
		{
			foreach (MethodInfo method in type.GetTypeInfo().GetMethods(BINDING_FLAGS))
			{
				if (method.GetCustomAttribute<Attr>() != null)
					yield return method;
			}
		}

		public static MethodInfo GetGenericMethod(this Type type, string name, Type arg)
		{
			MethodInfo method = type.GetTypeInfo().GetMethod(name, BINDING_FLAGS);
			return method.MakeGenericMethod(arg);
		}

		public static Type UnderlyingType(this MemberInfo member)
		{
			if (member is PropertyInfo)
			{
				return (member as PropertyInfo).PropertyType;
			}
			else if (member is FieldInfo)
			{
				return (member as FieldInfo).FieldType;
			}
			return null;
		}

		public static object GetValue(this MemberInfo member)
		{
			if (member is PropertyInfo)
			{
				return (member as PropertyInfo).GetValue();
			}
			else if (member is FieldInfo)
			{
				return (member as FieldInfo).GetValue();
			}
			return null;
		}
    }
}
