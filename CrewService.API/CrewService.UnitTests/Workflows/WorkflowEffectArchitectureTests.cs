using System.Reflection;
using System.Reflection.Emit;
using CrewService.Application.Workflows.Effects;
using CrewService.Domain.Interfaces;

namespace CrewService.UnitTests.Workflows;

public sealed class WorkflowEffectArchitectureTests
{
    [Fact]
    public void DatabaseWorkflowEffects_DoNotInjectOrchestrationUnitOfWorkFactory()
    {
        var effectTypes = GetDatabaseEffectTypes();

        foreach (var effectType in effectTypes)
        {
            var hasFactoryCtorParam = effectType
                .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .SelectMany(c => c.GetParameters())
                .Any(p => p.ParameterType == typeof(IOrchestrationUnitOfWorkFactory));

            Assert.False(hasFactoryCtorParam,
                $"Workflow DB effect '{effectType.FullName}' must not inject IOrchestrationUnitOfWorkFactory. Use the active IOrchestrationUnitOfWork from execution context.");
        }
    }

    [Fact]
    public void DatabaseWorkflowEffects_DoNotCallOrchestrationUnitOfWorkFactoryCreateAsync()
    {
        var effectTypes = GetDatabaseEffectTypes();

        foreach (var effectType in effectTypes)
        {
            var methods = effectType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => !m.IsAbstract)
                .ToList();

            foreach (var method in methods)
            {
                foreach (var called in GetCalledMethods(method))
                {
                    if (called.DeclaringType == typeof(IOrchestrationUnitOfWorkFactory)
                        && string.Equals(called.Name, nameof(IOrchestrationUnitOfWorkFactory.CreateAsync), StringComparison.Ordinal))
                    {
                        Assert.Fail(
                            $"Workflow DB effect '{effectType.FullName}' calls IOrchestrationUnitOfWorkFactory.CreateAsync in method '{method.Name}'. Effects must use the active orchestration UoW.");
                    }
                }
            }
        }
    }

    private static List<Type> GetDatabaseEffectTypes()
    {
        return typeof(IDatabaseWorkflowEffect).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IDatabaseWorkflowEffect).IsAssignableFrom(t))
            .ToList();
    }

    private static IEnumerable<MethodBase> GetCalledMethods(MethodInfo method)
    {
        var body = method.GetMethodBody();
        if (body is null)
            yield break;

        var il = body.GetILAsByteArray();
        if (il is null || il.Length == 0)
            yield break;

        var module = method.Module;
        var position = 0;
        while (position < il.Length)
        {
            var opCode = ReadOpCode(il, ref position);

            if (opCode == OpCodes.Call || opCode == OpCodes.Callvirt)
            {
                var token = ReadInt32(il, ref position);
                MethodBase? called;
                try
                {
                    called = module.ResolveMethod(token);
                }
                catch
                {
                    called = null;
                }

                if (called is not null)
                    yield return called;

                continue;
            }

            SkipOperand(opCode, il, ref position);
        }
    }

    private static OpCode ReadOpCode(byte[] il, ref int position)
    {
        var value = il[position++];
        if (value != 0xFE)
            return SingleByteOpCodes[value];

        var second = il[position++];
        return MultiByteOpCodes[second];
    }

    private static int ReadInt32(byte[] il, ref int position)
    {
        var value = BitConverter.ToInt32(il, position);
        position += 4;
        return value;
    }

    private static void SkipOperand(OpCode opCode, byte[] il, ref int position)
    {
        switch (opCode.OperandType)
        {
            case OperandType.InlineNone:
                return;
            case OperandType.ShortInlineBrTarget:
            case OperandType.ShortInlineI:
            case OperandType.ShortInlineVar:
                position += 1;
                return;
            case OperandType.InlineVar:
                position += 2;
                return;
            case OperandType.InlineI:
            case OperandType.InlineBrTarget:
            case OperandType.InlineField:
            case OperandType.InlineMethod:
            case OperandType.InlineSig:
            case OperandType.InlineString:
            case OperandType.InlineTok:
            case OperandType.InlineType:
            case OperandType.ShortInlineR:
                position += 4;
                return;
            case OperandType.InlineI8:
            case OperandType.InlineR:
                position += 8;
                return;
            case OperandType.InlineSwitch:
            {
                var count = BitConverter.ToInt32(il, position);
                position += 4 + (count * 4);
                return;
            }
            default:
                throw new NotSupportedException($"Unsupported IL operand type '{opCode.OperandType}'.");
        }
    }

    private static readonly OpCode[] SingleByteOpCodes = BuildSingleByteOpcodeTable();
    private static readonly OpCode[] MultiByteOpCodes = BuildMultiByteOpcodeTable();

    private static OpCode[] BuildSingleByteOpcodeTable()
    {
        var table = new OpCode[256];
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is not OpCode opCode)
                continue;

            var value = (ushort)opCode.Value;
            if (value <= byte.MaxValue)
                table[value] = opCode;
        }

        return table;
    }

    private static OpCode[] BuildMultiByteOpcodeTable()
    {
        var table = new OpCode[256];
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is not OpCode opCode)
                continue;

            var value = (ushort)opCode.Value;
            if ((value & 0xFF00) == 0xFE00)
                table[value & 0xFF] = opCode;
        }

        return table;
    }
}
