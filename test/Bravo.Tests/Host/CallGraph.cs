using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Bravo.Tests.Host;

/// <summary>
/// Reads the IL of methods to find the types whose members they reference.
/// </summary>
/// <remarks>
/// Calls are followed only to methods of the same assembly, as declared: an override or an interface implementation
/// reached through virtual dispatch is not followed. Constants do not appear, because the compiler copies their value.
/// </remarks>
internal static class CallGraph
{
    private static readonly Dictionary<ushort, OpCode> s_opCodes = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select((field) => (OpCode)field.GetValue(null)!)
        .ToDictionary((opCode) => (ushort)opCode.Value);

    /// <summary>
    /// Returns the types whose fields or methods are referenced by <paramref name="root"/> or by any method of its
    /// assembly that it reaches.
    /// </summary>
    public static IReadOnlySet<Type> GetReferencedTypes(MethodBase root)
    {
        var assembly = root.Module.Assembly;
        var visited = new HashSet<MethodBase>();
        var types = new HashSet<Type>();
        var pending = new Stack<MethodBase>();
        pending.Push(root);

        while (pending.TryPop(out var method))
        {
            if (!visited.Add(method))
                continue;

            foreach (var member in GetReferencedMembers(method))
            {
                if (member.DeclaringType is not null)
                    types.Add(member.DeclaringType);

                if (member is MethodBase callee && callee.Module.Assembly == assembly)
                    pending.Push(callee);
            }
        }

        return types;
    }

    private static IEnumerable<MemberInfo> GetReferencedMembers(MethodBase method)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray();
        if (il is null)
            yield break;

        var typeArguments = method.DeclaringType?.IsGenericType == true ? method.DeclaringType.GetGenericArguments() : null;
        var methodArguments = method.IsGenericMethod ? method.GetGenericArguments() : null;

        var position = 0;
        while (position < il.Length)
        {
            var opCode = ReadOpCode(il, ref position);

            if (opCode.OperandType is OperandType.InlineMethod or OperandType.InlineField)
            {
                var token = BitConverter.ToInt32(il, position);
                var member = method.Module.ResolveMember(token, typeArguments, methodArguments);
                if (member is not null)
                    yield return member;
            }

            position += GetOperandSize(opCode, il, position);
        }
    }

    private static OpCode ReadOpCode(byte[] il, ref int position)
    {
        var value = (ushort)il[position++];
        if (value == 0xFE)
            value = (ushort)(0xFE00 | il[position++]);

        return s_opCodes[value];
    }

    private static int GetOperandSize(OpCode opCode, byte[] il, int position)
    {
        return opCode.OperandType switch
        {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
            OperandType.InlineVar => 2,
            OperandType.InlineI8 or OperandType.InlineR => 8,
            OperandType.InlineSwitch => 4 + (4 * BitConverter.ToInt32(il, position)),
            _ => 4,
        };
    }
}
