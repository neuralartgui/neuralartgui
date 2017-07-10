using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DeepdreamGui.View;
using DeepdreamGui.ViewModel;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Windows.Controls;
using System;
using System.Globalization;

namespace DeepdreamGui
{
    /// <summary>
    /// Interactionlogic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, IMainWindowActions
    {
        private readonly AutoResetEvent inputNameAutoResetEvent = new AutoResetEvent(false);

        Task<MessageDialogResult> IMainWindowActions.ShowDialog(string title, string message, MessageDialogStyle style)
        {
            return this.ShowMessageAsync(title, message, style);
        }

        
        Task<ProgressDialogController> IMainWindowActions.ShowProgressDialog(string title, string message, bool isCancelable, MetroDialogSettings style)
        {
            return this.ShowProgressAsync(title, message, isCancelable, style);
        }

        Task IMainWindowActions.ShowNameInputDialog()
        {
            var baseDiag = (CustomDialog)this.Resources["NameInputDialog"];
            var settings = new MetroDialogSettings()
            {
                AffirmativeButtonText = "OK",
                AnimateHide = true,
                AnimateShow = true
            };
            this.ShowMetroDialogAsync(baseDiag,settings);
            return Task.Factory.StartNew(() => inputNameAutoResetEvent.WaitOne());
        }

        void IMainWindowActions.CloseDetailsWindow(ResultImageViewModel vm)
        {
            foreach (Window ownedWindow in this.OwnedWindows)
            {
                if (ownedWindow.DataContext == vm)
                    ownedWindow.Close();
            }
        }

        void IMainWindowActions.ShowDetailsWindow(ResultImageViewModel vm)
        {
            foreach (Window ownedWindow in this.OwnedWindows)
            {
                if (ownedWindow.DataContext != vm) continue;
                ownedWindow.Activate();
                return;
            }

            var window = new ResultImageDetail { DataContext = vm, Owner = this};
            window.Show();
        }
        
        
        void IMainWindowActions.Exit()
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Constructor for MainWindow
        /// </summary>
        public MainWindow()
        {
            //Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US"); //Test Language
            InitializeComponent();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = new MainViewModel(this);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var baseDiag = (CustomDialog)this.Resources["NameInputDialog"];
            this.HideMetroDialogAsync(baseDiag, null);
            inputNameAutoResetEvent.Set();
        }

        private async void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var w = (Window)sender;
            var v = (MainViewModel)this.DataContext;

            if (v == null || v.ResultImages.Count == 0) return;

            e.Cancel = true;
            var result = this.ShowMessageAsync(Properties.Resources.info_confirm_close, Properties.Resources.info_confirm_close_detail, MessageDialogStyle.AffirmativeAndNegative);

            if (await result != MessageDialogResult.Affirmative) return;
            this.DataContext = null;
            w.Close();
        }

        private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var waterMarkP = WaterMarkContentControl.TranslatePoint(new Point(0, 0), ImageCanvas);
            var backgroundP = BackgroundImage.TranslatePoint(new Point(0, 0), ImageCanvas);

            var vector = waterMarkP - backgroundP;

            var oldFactX = vector.X / e.PreviousSize.Width;
            var oldFactY = vector.Y / e.PreviousSize.Height;

            var diffX = e.NewSize.Width - e.PreviousSize.Width;
            var diffY = e.NewSize.Height - e.PreviousSize.Height;

            var factX = e.NewSize.Width / e.PreviousSize.Width;
            var factY = e.NewSize.Height / e.PreviousSize.Height;

            if (double.IsInfinity(factX) || double.IsInfinity(factY)) return;

            WaterMarkContentControl.Width *= factX;
            WaterMarkContentControl.Height *= factY;

            var newX = backgroundP.X + e.NewSize.Width * oldFactX;
            var newY = backgroundP.Y + e.NewSize.Height * oldFactY;

            if (Math.Abs(backgroundP.Y) > 0.0001) newY -= diffY*1000 / 2000.0;
            if (Math.Abs(backgroundP.X) > 0.0001) newX -= diffX*1000 / 2000.0;

            if (newX <= ImageCanvas.ActualWidth && newX >= 0)
                Canvas.SetLeft(WaterMarkContentControl, newX);
            else
                Canvas.SetLeft(WaterMarkContentControl, 0);

            if (newY <= ImageCanvas.ActualHeight && newY >= 0)
                Canvas.SetTop(WaterMarkContentControl,newY);
            else
                Canvas.SetTop(WaterMarkContentControl, 0);
        }

        private void ImageCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var waterMarkP = WaterMarkContentControl.TranslatePoint(new Point(0, 0), ImageCanvas);
            var backgroundP = BackgroundImage.TranslatePoint(new Point(0, 0), ImageCanvas);

            var diffX = e.NewSize.Width - e.PreviousSize.Width;
            var diffY = e.NewSize.Height - e.PreviousSize.Height;

            var newPosX = waterMarkP.X + diffX / 2.0;
            var newPosY = waterMarkP.Y + diffY / 2.0;

            if (backgroundP.X > 0 && Math.Abs(diffX) > 0.0001 && newPosX <= ImageCanvas.ActualWidth && newPosX >= 0)
                Canvas.SetLeft(WaterMarkContentControl, newPosX);

            if (backgroundP.Y > 0 && Math.Abs(diffY) > 0.0001 && newPosY <= ImageCanvas.ActualHeight && newPosY >= 0)
                Canvas.SetTop(WaterMarkContentControl, newPosY);
        }
    }
}
