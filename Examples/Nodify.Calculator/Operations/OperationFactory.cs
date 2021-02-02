using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nodify.Calculator
{
    public static class OperationFactory
    {
        public static List<OperationInfoViewModel> GetOperationsInfo(Type container)  
        {
            List<OperationInfoViewModel> result = new List<OperationInfoViewModel>();

            foreach (var method in container.GetMethods())                                                      // return MethodInfo[]
            {
                if (method.IsStatic)                                                                            // method 가 static 인지
                {
                    OperationInfoViewModel op = new OperationInfoViewModel
                    {
                        Title = method.Name
                    };

                    var attr = method.GetCustomAttribute<OperationAttribute>();                                 // OperationAttribute 타입의 attribute 를 가지고 옴.
                    var para = method.GetParameters();                                                          // 파라미터를 가지고 옴.

                    bool generateInputNames = true;

                    op.Type = OperationType.Normal;                                                             // 파라미터가 2개인 일반적인 op 를 의미

                    if (para.Length == 2)                                                                       // 파라미터가 2개 일때
                    {
                        var delType = typeof(Func<decimal, decimal, decimal>);
                        var del = (Func<decimal, decimal, decimal>)Delegate.CreateDelegate(delType, method);    // static 함수 만들어주고!..

                        op.Operation = new BinaryOperation(del);                                                // 파라미터가 2개인 익명함수 invoke 할 수 있는 형태로 만듬.??...
                    }
                    else if (para.Length == 1)
                    {
                        if (para[0].ParameterType.IsArray)                                                      // 파라미터가 한개라면 이것이 배열인지 확인한다.
                        {
                            op.Type = OperationType.Expando;                

                            var delType = typeof(Func<decimal[], decimal>);                                     // 첫번째 파라미터를 배열로 받는다.
                            var del = (Func<decimal[], decimal>)Delegate.CreateDelegate(delType, method);

                            op.Operation = new ParamsOperation(del);                                            // 파라미터가 1개인 익명함수 invoke 할 수 있도록
                            op.MaxInput = int.MaxValue;                                                         // 밑에서 세팅이 다되는데 여기서 해줄 필요가 있는가??
                        }
                        else
                        {
                            var delType = typeof(Func<decimal, decimal>);                                       // 배열이 아닌경우이므로 그냥 첫번째 파라미터인 타입의 익명함수 타입을 만든다.
                            var del = (Func<decimal, decimal>)Delegate.CreateDelegate(delType, method);

                            op.Operation = new UnaryOperation(del);
                        }
                    }
                    else if (para.Length == 0)                                                                  // 파라미터가 없는 경우
                    {
                        var delType = typeof(Func<decimal>);
                        var del = (Func<decimal>)Delegate.CreateDelegate(delType, method);

                        op.Operation = new ValueOperation(del);
                    }

                    if (attr != null)
                    {
                        op.MinInput = attr.MinInput;
                        op.MaxInput = attr.MaxInput;
                        generateInputNames = attr.GenerateInputNames;
                    }
                    else
                    {
                        op.MinInput = (uint)para.Length;
                        op.MaxInput = (uint)para.Length;
                    }

                    foreach (var param in para)
                    {
                        op.Input.Add(generateInputNames ? param.Name : null);                               // generateInputNames 가 ture 이면 파라미터의 이름이 저장된다.
                    }

                    for (int i = op.Input.Count; i < op.MinInput; i++)                                      // 실제 함수의 파라미터가 2개인데 attribute 에서 최소값이 3 이면 나머지 하나는 null 처리하는 구문
                    {
                        op.Input.Add(null);
                    }

                    result.Add(op);
                }
            }

            return result;
        }

        public static OperationViewModel GetOperation(OperationInfoViewModel info)
        {
            // ConnectorViewModel 의 정체를 확인하자
            // ConnectorViewModel 로 변환하는 구문 OperationInfoViewModel info -> ConnectorViewModel input
            var input = info.Input.Select(i => new ConnectorViewModel
            {
                Title = i
            });

            if (info.Type == OperationType.Expression)                                                  // method 타입이 custom 이면 Expression
            {
                return new ExpressionOperationViewModel
                {
                    Title = info.Title,
                    Output = new ConnectorViewModel(),
                    Operation = info.Operation,
                    Expression = "1 + sin {a} + cos {b}"
                };
            }
            else if (info.Type == OperationType.Expando)
            {
                var o = new ExpandoOperationViewModel
                {
                    MaxInput = info.MaxInput,
                    MinInput = info.MinInput,
                    Title = info.Title,
                    Output = new ConnectorViewModel(),
                    Operation = info.Operation
                };

                o.Input.AddRange(input);
                return o;
            }

            var op = new OperationViewModel
            {
                Title = info.Title,
                Output = new ConnectorViewModel(),
                Operation = info.Operation
            };

            op.Input.AddRange(input);
            return op;
        }
    }
}
