using System;
using System.Windows.Input;

namespace Nodify
{
    public interface INodifyCommand : ICommand
    {
        void RaiseCanExecuteChanged();
    }

    public class DelegateCommand : INodifyCommand
    {
        private readonly Action _action;
        
        private readonly Func<bool>? _condition;                                                    // 타입? 의 경우 해당 타입이 원래는 null 을 가질 수 없는 타입인데, null 을 가질 수 있는 타입으로 만들어 준다.
                                                                                                    // 리턴값이 bool 타입인 Func 델리게이트

       
        public event EventHandler? CanExecuteChanged;                                               // EventHandler -> 이벤트 데이터가 없는 이벤트를 처리할 메서드를 나타냅니다.
                                                                                                    // https://docs.microsoft.com/ko-kr/dotnet/api/system.eventhandler?view=net-5.0
                                                                                                    // public delegate void EventHandler(object? sender, EventArgs e);     

        public DelegateCommand(Action action, Func<bool>? executeCondition = default)
        {
            // ?? 연산자 왼쪽의 객체가 null 이 아니면 왼쪽을 반환하고 null 이면 오른쪽 을 처리한다.
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _condition = executeCondition;
        }
      
        public bool CanExecute(object parameter)                                                    // parmameter 는 왜 넣어주었지??
            => _condition?.Invoke() ?? true;                                                        // ?? 연산자 는  _condition?.Invoke() 가 null 이 아니면 _condition?.Invoke() 를 계산한 값을 리턴하고 null 이면 true 를 리턴한다. 여기서는 false 를 리턴해야할 거 같은데...
                                                                                                    // Invoke 함수는 threadSafe 하게 해준다.
                                                                                                    // https://blog.naver.com/ljh0707/10036599823

        public void Execute(object parameter)
            => _action();
                                                                                                     // https://docs.microsoft.com/ko-kr/dotnet/standard/events/     
        public void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, new EventArgs());                                     // 여기서 이벤트를 발생시킨다.
    }

    public class DelegateCommand<T> : INodifyCommand
    {
        private readonly Action<T> _action;
        private readonly Func<T, bool>? _condition;

        public event EventHandler? CanExecuteChanged;

        public DelegateCommand(Action<T> action, Func<T, bool>? executeCondition = default)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _condition = executeCondition;
        }
        // 여기서 parameter 를 바로 넘기지 않고 value 를 넘기지???
        // parameter 로 넘기면 에러가 발생한다. 왜???
        public bool CanExecute(object parameter)
        {
            if (parameter is T value)
            {
                return _condition?.Invoke(value) ?? true;
            }

            return _condition?.Invoke(default!) ?? true;
        }
        // command 가 호출될때 실행됨.
        public void Execute(object parameter)
        {
            if (parameter is T value)
            {
                _action(value);
            }
            else
            {
                _action(default!);
            }
        }

        public void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, new EventArgs());
    }
}
