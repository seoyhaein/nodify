using System.Linq;
using System.Windows;

namespace Nodify.Calculator
{
    public class CalculatorViewModel : ObservableObject
    {
        public CalculatorViewModel()
        {
            // CreateConnectionCommand 의 경우 바인딩이 되는 경우 두개의 함수를 delegate 하는 방식인가?
            CreateConnectionCommand = new DelegateCommand<(object Source, object Target)>(target => CreateConnection((ConnectorViewModel)target.Source, (ConnectorViewModel)target.Target), target => CanCreateConnection((ConnectorViewModel)target.Source, target.Target as ConnectorViewModel));
            CreateOperationCommand = new DelegateCommand<CreateOperationInfoViewModel>(CreateOperation); // 입력 파라미터가 CreateOperationInfoViewModel 인 대리함수 생성
            DisconnectConnectorCommand = new DelegateCommand<ConnectorViewModel>(DisconnectConnector);
            DeleteSelectionCommand = new DelegateCommand(DeleteSelection);

            Connections.WhenAdded(c =>
            {
                c.Input.IsConnected = true;
                c.Output.IsConnected = true;

                c.Input.Value = c.Output.Value;

                c.Output.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(ConnectorViewModel.Value))
                    {
                        c.Input.Value = c.Output.Value;
                    }
                };
            })
            .WhenRemoved(c =>
            {
                var ic = Connections.Count(con => con.Input == c.Input || con.Output == c.Input);
                var oc = Connections.Count(con => con.Input == c.Output || con.Output == c.Output);

                if (ic == 0)
                {
                    c.Input.IsConnected = false;
                }

                if (oc == 0)
                {
                    c.Output.IsConnected = false;
                }
            });

            Operations.WhenAdded(x =>
            {
                x.Input.WhenRemoved(i =>
                {
                    var c = Connections.Where(con => con.Input == i || con.Output == i).ToArray();
                    c.ForEach(con => Connections.Remove(con));
                });
            })
            .WhenRemoved(x =>
            {
                foreach (var input in x.Input)
                {
                    DisconnectConnector(input);
                }

                if (x.Output != null)
                {
                    DisconnectConnector(x.Output);
                }
            });

            UpdateAvailableOperations();
        }

        private NodifyObservableCollection<OperationViewModel> _operations = new NodifyObservableCollection<OperationViewModel>();
        public NodifyObservableCollection<OperationViewModel> Operations  // ItemsSource 에 바인딩 됨.
        {
            get => _operations;
            set => SetProperty(ref _operations, value);
        }
        // NodifyEditor.Connections
        public NodifyObservableCollection<ConnectionViewModel> Connections { get; } = new NodifyObservableCollection<ConnectionViewModel>();

        public NodifyObservableCollection<OperationInfoViewModel> AvailableOperations { get; } = new NodifyObservableCollection<OperationInfoViewModel>();

        public INodifyCommand CreateConnectionCommand { get; }
        public INodifyCommand CreateOperationCommand { get; }
        public INodifyCommand DisconnectConnectorCommand { get; }
        public INodifyCommand DeleteSelectionCommand { get; }

        private void DisconnectConnector(ConnectorViewModel connector)
        {
            var connections = Connections.Where(c => c.Input == connector || c.Output == connector).ToList();
            connections.ForEach(c => Connections.Remove(c));
        }
        // Func<T, bool>? executeCondition = default)
        private bool CanCreateConnection(ConnectorViewModel source, ConnectorViewModel? target)
            => target != null && source != target && source.Operation != target.Operation && source.IsInput != target.IsInput;  // 해당 조건이 모두 만족하면 true, 아니면 false
        
        // 여기서 실제로 Node가 어떻게 생기는지를 파악해야한다.
        private void CreateConnection(ConnectorViewModel source, ConnectorViewModel target)
        {
            var input = source.IsInput ? source : target;
            var output = target.IsInput ? source : target;

            DisconnectConnector(input);

            Connections.Add(new ConnectionViewModel
            {
                Input = input,
                Output = output
            });
        }
        /*
         CreateOperation 의 경우에는 바인딩될때 요소에서 입력 파라미터도 넘어오는 형태인데, CreateConnection 같은 경우는 입력 파라미터가 어디서 오는가?
         */
        private void CreateOperation(CreateOperationInfoViewModel arg)
        {
            var op = OperationFactory.GetOperation(arg.Info);  // 함수를 가지고 오는 거 같음.?? OperationViewModel 리턴함.
            op.Location = arg.Location;

            Operations.Add(op); // ItemsSource 에 매핑이 되는데 결국 노드가 생성됨.
                                // 이부분은 간단한 테스트가 필요해 보임.
        }          
        // 바인딩시 contstructor 에서 만들어줌.
        // Context 메뉴에 바인딩 되는 AvailableOperations 를 넣어줌.
        private void UpdateAvailableOperations()
        {
            AvailableOperations.Add(new OperationInfoViewModel
            {
                Type = OperationType.Expression,
                Title = "Custom", 
            });
             var operations = OperationFactory.GetOperationsInfo(typeof(OperationsContainer));  // 최초로 AvailableOperations 를 채워줌.
            AvailableOperations.AddRange(operations);
        }

        private void DeleteSelection()
        {
            var selected = Operations.Where(o => o.IsSelected).ToList();
            selected.ForEach(o => Operations.Remove(o));
        }
    }
}
