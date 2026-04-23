using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ConflictScanner.Reflection
{
    /// <summary>
    /// Minimal IL reader to extract constant string arguments from method calls.
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

            int i = 0;
            while (i < il.Length)
            {
                OpCode op = ReadOpCode(il, ref i);

                if (op == OpCodes.Call || op == OpCodes.Callvirt)
                {
                    int token = ReadInt32(il, ref i);
                    var target = module.ResolveMethod(token) as MethodInfo;

                    if (target != null)
                    {
                        var args = ExtractArguments(il, ref i);
                        yield return (target, args);
                    }
                }
                else
                {
                    SkipOperand(op, il, ref i);
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
                    index += 1;
                    return;
                case OperandType.InlineVar:
                    index += 2;
                    return;
                case OperandType.InlineI:
                case OperandType.InlineBrTarget:
                case OperandType.InlineR:
                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineSig:
                case OperandType.InlineString:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                    index += 4;
                    return;
                case OperandType.InlineI8:
                case OperandType.InlineR8:
                    index += 8;
                    return;
                case OperandType.InlineSwitch:
                    int count = BitConverter.ToInt32(il, index);
                    index += 4 + (count * 4);
                    return;
            }
        }

        private static List<object> ExtractArguments(byte[] il, ref int index)
        {
            // This is intentionally simple: we only extract constant strings.
            var args = new List<object>();

            // Look backwards for ldstr
            int back = index - 6;
            if (back >= 0 && il[back] == OpCodes.Ldstr.Value)
            {
                int token = BitConverter.ToInt32(il, back + 1);
                string str = ilModule.ResolveString(token);
                args.Add(str);
            }

            return args;
        }

        private static readonly OpCode[] singleByteOpCodes = new OpCode[256];
        private static readonly OpCode[] multiByteOpCodes = new OpCode[256];

        static ILReader()
        {
            foreach (var fi in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (fi.GetValue(null) is OpCode op)
                {
                    if (op.Size == 1)
                        singleByteOpCodes[op.Value] = op;
                    else
                        multiByteOpCodes[op.Value & 0xFF] = op;
                }
            }
        }
    }
}
