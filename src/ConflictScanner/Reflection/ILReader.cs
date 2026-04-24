using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ConflictScanner.Reflection
{
    /// <summary>
    /// Minimal IL reader to extract constant string arguments from method calls.
    /// Assumes simple patterns: ldstr followed by call/callvirt.
    /// </summary>
    public static class ILReader
    {
        public static IEnumerable<(MethodInfo Target, List<object> Args)> FindCalls(MethodInfo method)
        {
            var body = method.GetMethodBody();
            if (body == null)
                yield break;

            var il = body.GetILAsByteArray();
            var module = method.Module;

            int index = 0;
            string? lastString = null;

            while (index < il.Length)
            {
                OpCode op = ReadOpCode(il, ref index);

                if (op == OpCodes.Ldstr)
                {
                    int token = ReadInt32(il, ref index);
                    try
                    {
                        lastString = module.ResolveString(token);
                    }
                    catch
                    {
                        lastString = null;
                    }
                }
                else if (op == OpCodes.Call || op == OpCodes.Callvirt)
                {
                    int token = ReadInt32(il, ref index);
                    MethodInfo? target = null;

                    try
                    {
                        target = module.ResolveMethod(token) as MethodInfo;
                    }
                    catch
                    {
                        target = null;
                    }

                    if (target != null)
                    {
                        var args = new List<object>();
                        if (lastString != null)
                        {
                            args.Add(lastString);
                            lastString = null;
                        }

                        yield return (target, args);
                    }
                }
                else
                {
                    SkipOperand(op, il, ref index);
                }
            }
        }

        private static OpCode ReadOpCode(byte[] il, ref int index)
        {
            byte code = il[index++];

            if (code != 0xFE)
                return singleByteOpCodes[code];

            byte second = il[index++];
            return multiByteOpCodes[second];
        }

        private static int ReadInt32(byte[] il, ref int index)
        {
            int value = BitConverter.ToInt32(il, index);
            index += 4;
            return value;
        }

        private static void SkipOperand(OpCode op, byte[] il, ref int index)
        {
            switch (op.OperandType)
            {
                case OperandType.InlineNone:
                    return;

                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                case OperandType.ShortInlineR:
                    index += 1;
                    return;

                case OperandType.InlineVar:
                    index += 2;
                    return;

                case OperandType.InlineI:
                case OperandType.InlineBrTarget:
                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineSig:
                case OperandType.InlineString:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                case OperandType.InlineR:
                    index += 4;
                    return;

                case OperandType.InlineI8:
                    index += 8;
                    return;

                case OperandType.InlineSwitch:
                    int count = BitConverter.ToInt32(il, index);
                    index += 4 + (count * 4);
                    return;

                default:
                    return;
            }
        }

        private static readonly OpCode[] singleByteOpCodes = new OpCode[256];
        private static readonly OpCode[] multiByteOpCodes = new OpCode[256];

        static ILReader()
        {
            foreach (var fi in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (fi.GetValue(null) is OpCode op)
                {
                    ushort value = (ushort)op.Value;
                    if (value < 0x100)
                        singleByteOpCodes[value] = op;
                    else if ((value & 0xFF00) == 0xFE00)
                        multiByteOpCodes[value & 0xFF] = op;
                }
            }
        }
    }
}
