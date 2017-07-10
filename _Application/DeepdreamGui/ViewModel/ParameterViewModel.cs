using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeepdreamGui.Model;

namespace DeepdreamGui.ViewModel
{
    /// <summary>
    /// Base class for the view models
    /// Load Parameters for the view models
    /// Derivative by ViewModelBase
    /// </summary>
    public class ParameterViewModel  : ViewModelBase
    {
        private ParameterCollection modelParameter;
        private ParameterCollection inputParameter;
        private DeepDreamParameter selectedModelParameter;
        /// <summary>
        /// Constructor for the parameter view model. Load the parameter
        /// </summary>
        public ParameterViewModel()
        {
            LoadParameter();
        }
        /// <summary>
        /// Async function to load the parameter model
        /// </summary>
        private async void LoadParameter()
        {
            var param = await ParameterModel.LoadParameter();
            InputParameter = param.InputParameter;
            ModelParameter = param.ModelParameter;
        }
        /// <summary>
        /// Property displaying a collection of input parameters
        /// </summary>
        public ParameterCollection InputParameter
        {
            get { return inputParameter; }
            set { inputParameter = value; OnPropertyChanged(); }
        }
        /// <summary>
        /// Property displaying a collection of model propertys
        /// </summary>
        public ParameterCollection ModelParameter
        {
            get { return modelParameter; }
            set
            {
                modelParameter = value; OnPropertyChanged();
                if (modelParameter.Collection.Count > 0 && SelectedModelParameter == null) SelectedModelParameter = modelParameter.Collection[0];
            }
        }
        /// <summary>
        /// Property displaying the selected model parameter
        /// </summary>
        public DeepDreamParameter SelectedModelParameter
        {
            get { return selectedModelParameter; }
            set { selectedModelParameter = value; OnPropertyChanged(); }
        }
    }
}
