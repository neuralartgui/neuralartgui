using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DeepdreamGui.ViewModel;
using MahApps.Metro.Controls.Dialogs;

namespace DeepdreamGui.View
{
    /// <summary>
    /// Actions to let the the ViewModel control parts of the View. Should be injected.
    /// </summary>
    public interface IMainWindowActions
    {
        /// <summary>
        /// Show Metro Style Dialog
        /// </summary>
        /// <param name="title">Dialog Title</param>
        /// <param name="message">Dialog Message</param>
        /// <param name="style">Dialog Style</param>
        /// <returns></returns>
        Task<MessageDialogResult> ShowDialog(string title, string message, MessageDialogStyle style);

        /// <summary>
        /// Show Metro-Style progress Dialog
        /// </summary>
        /// <param name="title">Dialog Title</param>
        /// <param name="message">Dialog Message</param>
        /// <param name="isCancelable">Is Cancelable</param>
        /// <param name="style">Progress Dialog Style</param>
        /// <returns></returns>
        Task<ProgressDialogController> ShowProgressDialog(string title, string message, bool isCancelable, MetroDialogSettings style);

        /// <summary>
        /// Show MetroStyle Authorname and Reportname input Dialog
        /// </summary>
        /// <returns>Task that waits for the Dialog to close</returns>
        Task ShowNameInputDialog();

        /// <summary>
        /// Close Image Details Window
        /// </summary>
        /// <param name="vm">View Model of Window to Close</param>
        void CloseDetailsWindow(ResultImageViewModel vm);

        /// <summary>
        /// Show Image Details Window
        /// </summary>
        /// <param name="vm">ViewModel of Window</param>
        void ShowDetailsWindow(ResultImageViewModel vm);

        /// <summary>
        /// Exit Application
        /// </summary>
        void Exit();
    }
}
