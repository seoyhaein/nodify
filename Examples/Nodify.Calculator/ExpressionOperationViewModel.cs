using StringMath;
using System.Linq;

namespace Nodify.Calculator
{
    public class ExpressionOperationViewModel : OperationViewModel
    {
        private string? _expression;
        public string? Expression
        {
            get => _expression;
            set => SetProperty(ref _expression, value)                      // 성공하면 true 실패하면 false
                .Then(GenerateInput);                                       // 확장메소드를 사용했음. 위의 구문이 성공하면 GenerateInput 을 수행하고 true, 실패하면 false 리턴. false 리턴하면 문제가 없나???
        }                                                                   // set 의 경우는 리턴이 없으므로 가능한거 같은데...

        private void GenerateInput()
        {
            try
            {
                var operation = SMath.CreateOperation(Expression);                                  // StringMath https://github.com/miroiu/string-math
                var toRemove = Input.Where(i => !operation.Replacements.Contains(i.Title)).ToArray();
                toRemove.ForEach(i => Input.Remove(i));
                var left = Input.Select(s => s.Title).ToHashSet();

                foreach (var variable in operation.Replacements.Where(s => !left.Contains(s)))
                {
                    Input.Add(new ConnectorViewModel
                    {
                        Title = variable
                    });
                }

                OnInputValueChanged();
            }
            catch
            {

            }
        }

        protected override void OnInputValueChanged()
        {
            if (Output != null)
            {
                try
                {
                    var repl = new Replacements();
                    Input.ForEach(i => repl[i.Title] = i.Value);

                    Output.Value = SMath.Evaluate(Expression, repl);
                }
                catch
                {

                }
            }
        }
    }
}
