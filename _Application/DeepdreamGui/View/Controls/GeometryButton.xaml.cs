using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DeepdreamGui.View.Controls
{
    /// <summary>
    /// Interaktionslogik für GeometryButton.xaml
    /// </summary>
    public partial class GeometryButton : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// Gemoetry Button Default Constructor
        /// </summary>
        public GeometryButton()
        {
            InitializeComponent();
            this.InnerButton.DataContext = this;
        }

        /// <summary>
        /// Geometry Path Dependency Property
        /// </summary>
        public static readonly DependencyProperty GeometryPathProperty = DependencyProperty.Register("GeometryPath",typeof(string),typeof(GeometryButton),new FrameworkPropertyMetadata());

        /// <summary>
        /// Vector Geometry Path for Icon
        /// </summary>
        public string GeometryPath
        {
            get { return (string) GetValue(GeometryPathProperty); }
            set { SetValue(GeometryPathProperty, value);}
        }

        /// <summary>
        /// Button Click Command Dependency Property
        /// </summary>
        public static DependencyProperty ButtonClickCommand = DependencyProperty.Register("ButtonClick", typeof(ICommand), typeof(GeometryButton), new FrameworkPropertyMetadata());

        /// <summary>
        /// Button Click Command. Gets Executed when Button is clicked.
        /// </summary>
        public ICommand ButtonClick
        {
            get { return (ICommand)GetValue(ButtonClickCommand); }
            set
            {
                SetValue(ButtonClickCommand, value); OnPropertyChanged();
            }
        }

        /// <summary>
        /// Button Click Command Parameter Dependency Property
        /// </summary>
        public static DependencyProperty ButtonClickCommandParameter = DependencyProperty.Register("ButtonClickParameter",typeof(Object),typeof(GeometryButton),new FrameworkPropertyMetadata());

        /// <summary>
        /// Button Click Parameter. Can be Attached to Button Click Command.
        /// </summary>
        public Object ButtonClickParameter
        {
            get { return (Object) GetValue(ButtonClickCommandParameter); }
            set
            {
                SetValue(ButtonClickCommandParameter, value);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Property Changed Event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Implementation of INotifyPropertyChanged
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            ButtonClick?.Execute(null);
        }
    }
}
