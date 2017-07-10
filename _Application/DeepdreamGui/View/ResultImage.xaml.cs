using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace DeepdreamGui.View
{
    /// <summary>
    /// Interaktionslogik für ResultImage.xaml
    /// </summary>
    public partial class ResultImage : INotifyPropertyChanged
    {
        /// <summary>
        /// Constructor for Result Image
        /// </summary>
        public ResultImage()
        {
            InitializeComponent();
            this.DataContextChanged += (sender, args) =>
            {
                var selectedBinding = new Binding
                {
                    Source = this.DataContext,
                    Path = new PropertyPath("Selected"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                BindingOperations.SetBinding(this, SelectedProperty, selectedBinding);
            };
        }

        /// <summary>
        /// Dependency Property for Selected Property
        /// </summary>
        public static DependencyProperty SelectedProperty = DependencyProperty.Register("Selected", typeof(Boolean), typeof(ResultImage), new FrameworkPropertyMetadata());

        /// <summary>
        /// Selected Flag for Result Image
        /// </summary>
        public bool Selected
        {
            get { return (bool)GetValue(SelectedProperty); }
            set
            {
                SetValue(SelectedProperty, value);  OnPropertyChanged();
            }
        }


        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Selected = !Selected;
        }

        /// <summary>
        /// Property Changed Event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// INotifyPropertyChanged implementation
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
