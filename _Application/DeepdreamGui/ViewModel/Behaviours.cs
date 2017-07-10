using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DeepdreamGui.ViewModel
{
    //https://gayotfow.wordpress.com/2014/04/27/to-implement-drag-and-drop-in-mvvm-without-much-experience-in-wpf-you-can-refer-to-5-steps-i-will-outline-these/
    public static class Behaviours
    {
        #region DandBehaviour
        public static readonly DependencyProperty DanDdBehaviourProperty =
            DependencyProperty.RegisterAttached("DandDBehaviour", typeof(ICommand), typeof(Behaviours),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.None,
                    OnDandDBehaviourChanged));
        public static ICommand GetDandDBehaviour(DependencyObject dObject)
        {
            return (ICommand)dObject.GetValue(DanDdBehaviourProperty);
        }
        public static void SetDandDBehaviour(DependencyObject dObject, ICommand value)
        {
            dObject.SetValue(DanDdBehaviourProperty, value);
        }
        private static void OnDandDBehaviourChanged(DependencyObject dObject, DependencyPropertyChangedEventArgs e)
        {
            Grid g = dObject as Grid;
            if (g != null)
            {
                g.Drop += (s, a) =>
                {
                    ICommand iCommand = GetDandDBehaviour(dObject);
                    if (iCommand != null)
                    {
                        if (iCommand.CanExecute(a.Data))
                        {
                            iCommand.Execute(a.Data);
                        }
                    }
                };
            }
            else
            {
                throw new ApplicationException("Non grid");
            }
        }
        #endregion
    }
}
