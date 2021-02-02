using Nodify;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Nodify_Tester
{
    public class ItemSourceTester : ObservableObject
    {
        //Operations
        public ItemSourceTester() {
            //CreateOperationCommand = new DelegateCommand<CreateOperationInfoViewModel>(CreateOperation);
            CreateOperationCommand = new DelegateCommand<CreateOperationInfoViewModel>(Tester);
        }

        private NodifyObservableCollection<OperationViewModel> _operations = new NodifyObservableCollection<OperationViewModel>();
        public NodifyObservableCollection<OperationViewModel> Operations
        {
            get => _operations;
            set => SetProperty(ref _operations, value);
        }

        private void CreateOperation(CreateOperationInfoViewModel arg)
        {

            MessageBox.Show("Add");
            var op = OperationFactory.GetOperation(arg.Info);
            op.Location = arg.Location;

            Operations.Add(op);
        }

        private void Tester(CreateOperationInfoViewModel arg) {
            MessageBox.Show("Add");
        }

        public INodifyCommand CreateOperationCommand { get; }
    }
}
