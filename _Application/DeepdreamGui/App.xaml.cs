using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DeepdreamGui.Model;

namespace DeepdreamGui
{
    /// <summary>
    /// Interactionlogic for "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        private async void Application_Exit(object sender, ExitEventArgs e)
        {
            await Worker.Instance.Kill().ContinueWith(task => Worker.DeleteDirectory(Worker.DataPath));
        }
    }


}
