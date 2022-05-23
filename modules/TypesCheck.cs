namespace Firesharp;

static partial class Firesharp
{
    static Stack<DataType> dataStack = new Stack<DataType>();

    static void TypeCheck(List<Op> program)
    {
        foreach (Op op in program)
        {
            TypeCheckOp(op)();
        }
    }

    static Action TypeCheckOp(Op op)  => op.Type switch
    {
        OpType.push_int  => () => dataStack.Push(DataType._int),
        OpType.push_bool => () => dataStack.Push(DataType._bool),
        OpType.push_ptr  => () => dataStack.Push(DataType._ptr),
        OpType.push_str  => () => dataStack.Push(DataType._str),
        OpType.push_cstr => () => dataStack.Push(DataType._cstr),
        OpType.swap => () => 
        {
            dataStack.ExpectArity(2, ArityType.any);
            var A = dataStack.Pop();
            var B = dataStack.Pop();
            dataStack.Push(A);
            dataStack.Push(B);
        },
        OpType.drop => () =>
        {
            dataStack.ExpectArity(1, ArityType.any);
            dataStack.Pop();
        },
        OpType.dup => () =>
        {
            dataStack.ExpectArity(1, ArityType.any);
            dataStack.Push(dataStack.Peek());
        },
        OpType.rot => () =>
        {
            dataStack.ExpectArity(3, ArityType.any);
            var A = dataStack.Pop();
            var B = dataStack.Pop();
            var C = dataStack.Pop();
            dataStack.Push(B);
            dataStack.Push(A);
            dataStack.Push(C);
        },
        OpType.over => () =>
        {
            dataStack.ExpectArity(2, ArityType.any);
            var A = dataStack.Pop();
            var B = dataStack.Pop();
            dataStack.Push(B);
            dataStack.Push(A);
            dataStack.Push(B);
        },
        OpType.intrinsic => () => ((IntrinsicType)op.Operand switch
        {
            IntrinsicType.plus => () =>
            {
                dataStack.ExpectArity(2, ArityType.same);
                dataStack.Pop();
            },
            IntrinsicType.minus => () =>
            {
                dataStack.ExpectArity(2, ArityType.same);
                dataStack.Pop();
            },
            IntrinsicType.equal => () =>
            {
                dataStack.ExpectArity(2, ArityType.same);
                dataStack.Pop();
                dataStack.Pop();
                dataStack.Push(DataType._bool);
            },
            _ => (Action) (() => Error($"intrinsic value `{(IntrinsicType)op.Operand}`is not valid or is not implemented"))
        })(),
        OpType.call => () =>
        {
            Assert(procList.Count > op.Operand, "Proclist does not contain the needed proc id");
            Proc proc = procList.ElementAt(op.Operand);
            dataStack.ExpectArity(proc.contract);
            TypeCheck(proc.procOps);
            Error("OpType.call outputs are not typechecked yet");
        },
        _ => () => Error($"Op type not implemented in typechecking: {op.Type.ToString()}")
    };

    static void ExpectArity(this Stack<DataType> stack, Contract contract)
    {
        int count = contract.inTypes.Count() - 1;
        Assert(stack.Count > count, "Stack has less elements than expected");

        for (int i = 0; i <= count; i++)
        {
            if (!stack.ElementAt(i).Equals(contract.inTypes[count - i]))
            {
                Error("Arity check failled");
                return;
            }
        }
    }

    static void ExpectArity(this Stack<DataType> stack, int arityN, ArityType arityT)
    {
        Assert(stack.Count >= arityN, "Stack has less elements than expected");
        Assert(arityT switch
        {
            ArityType.any  => true,
            ArityType.same => ExpectSame(stack, arityN),
            _ => false
        }, "Arity check failled");
    }

    static bool ExpectSame(Stack<DataType> stack, int arityN)
    {
        DataType type = stack.ElementAt(0);
        for (int i = 0; i < arityN - 1; ++i)
        {
            if (!type.Equals(stack.ElementAt(i)))
            {
                return false;
            }
        }
        return true;
    }
}