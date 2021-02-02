using System;

namespace Nodify_Tester
{
    public class UnaryOperation : IOperation
    {
        private readonly Func<decimal, decimal> _func;

        public UnaryOperation(Func<decimal, decimal> func) => _func = func;

        public decimal Execute(params decimal[] operands)
            => _func.Invoke(operands[0]);
    }
}
