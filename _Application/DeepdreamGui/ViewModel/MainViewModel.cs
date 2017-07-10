using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DeepdreamGui.ExtensionMethods;
using DeepdreamGui.Model;
using DeepdreamGui.View;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Application = System.Windows.Application;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Timer = System.Timers.Timer;

namespace DeepdreamGui.ViewModel
{
    /// <summary>
    /// Description of the possible modes.
    /// </summary>
    public enum Modes
    {
        /// <summary>
        /// Dream Mode: Google DeepDream
        /// </summary>
        Dream,
        /// <summary>
        /// Style Mode: A Neural Algorithm of Artistic Style
        /// </summary>
        Style
    }
    /// <summary>
    /// Primary ViewModel-class. ViewModel for the MainWindow
    /// Derivative by ParameterViewModel
    /// </summary>
    public class MainViewModel : ParameterViewModel
    {
        private double overallProgress;
        private bool preparingProcess;
        private RelayCommand loadWorkImageCommand;
        private BitmapImage workImagesource;
        private bool workImageLoading;
        private bool running;
        private RelayCommand startProcessCommand;
        private RelayCommand cancelProcessCommand;
        private string processStatusText;

        //WaterImage
        private ICommand loadWorkWaterImageCommand;
        private BitmapImage workWaterImagesource;
        private bool loadWorkerWaterImageEnable;
        private ICommand waterImageSwitchCheckChangedCommand;
        private double workWaterImageOpacity = .5;

        private ResultImageViewModel currentResultVm;
        // Time Measurement
        private Stopwatch progresStopwatch;
        private System.Timers.Timer progressTimer;
        private TimeSpan progressRemainingTimeSpan;
        private ObservableCollection<ResultImageViewModel> resultImages;
        private bool firstIteration = true;

        private double thumbnailSize = 250;
        private int workImageIterations = 2;
        private double workImageIntensity = 5;
        private int workImageOctave = 4;
        private int workImageScale = 100;
        private double workImageRotate = 0;
        private IMainWindowActions mainWindowActions;
        private RelayCommand saveReportCommand;
        private BitmapImage styleImageSource;
        private RelayCommand loadStyleImageCommand;

        private bool displayedPngWarning = false;
        private bool displayedStartWarning = false;
        private bool displayedRotaScaleWarning = false;
        private int mode;
        private int workImageMaxSideLength = 300;
        private bool workImageShrinkEnabled;
        private RelayCommand saveSelectedImagesCommand;
        private RelayCommand deleteSelectedImagesCommand;
        private RelayCommand selectAllCommand;
        private RelayCommand selectNoneCommand;
        private double workImageMinimalScale = 100;

        /// <summary>
        /// Constructor for the MainViewModel.
        /// Test if Docker is there and everything is setup in the right way.
        /// Initializing the timer for time measuring and initialize the the collections.
        /// </summary>
        /// <param name="mainWindowActions">The actions of the main window.</param>
        public MainViewModel(IMainWindowActions mainWindowActions)
        {
            Trace.Listeners.Add(new LocalTraceListener());

            this.mainWindowActions = mainWindowActions;
            Task.Factory.StartNew(async () =>
            {
                TestDockerRunning();
                await TestDockerImagesAsync();
            });
            resultImages = new ObservableCollection<ResultImageViewModel>();
            progresStopwatch = new Stopwatch();

            Worker.Instance.ProcessEvent += ProcessEventHandlerAsync;

            progressTimer = new Timer(1000) { AutoReset = true };
            progressTimer.Elapsed += ProgressTimer_Elapsed;
            progressTimer.Enabled = true;
        }
        /// <summary>
        /// Event Handler for the timer. Triggered every time the interval elapsed. 
        /// </summary>
        /// <param name="sender">Timer</param>
        /// <param name="e">Eventargs, data for the elapsed time</param>
        private void ProgressTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //Check if a process is even running
            if (!Running || PreparingProcess) return;
            //Initialize the update progress
            if(firstIteration && OverallProgress > 0)
                UpdateProgressTime();

            //Update the time during the process
            if (OverallProgress > 0 && progressRemainingTimeSpan.TotalSeconds > 1)
            {
                progressRemainingTimeSpan = TimeSpan.FromSeconds(progressRemainingTimeSpan.TotalSeconds - 1);
                UpdateProgressText();
            }
        }

        private string timestring = "";
        private string progressstring = "";
        private double percentPerSecond = 1.0;
        /// <summary>
        /// Update the guessed remaining time on the mainview based on the percent change per second.
        /// </summary>
        private void UpdateProgressTime()
        {
            if (overallProgress == 0) return;
            percentPerSecond = progresStopwatch.Elapsed.TotalSeconds / overallProgress;
            progressRemainingTimeSpan = TimeSpan.FromSeconds((100 - overallProgress) * percentPerSecond);
        }
        /// <summary>
        /// Function to update the displayed timer text
        /// </summary>
        /// <param name="newProgressData">Displays if a new progress has started.</param>
        private void UpdateProgressText(bool newProgressData = false)
        {
            //check if a process is running
            if (!Running) return;
            //Initialize the text
            if (PreparingProcess)
            {
                ProcessStatusText = Properties.Resources.info_preparing;
                return;
            }
            //Add the percentage progress value
            progressstring = Math.Round(OverallProgress, 2).ToString() + "%";
            if (progresStopwatch.IsRunning && overallProgress > 0)
            {
                if (!newProgressData)
                {
                    //Update the remaining time
                    if (progressRemainingTimeSpan.TotalSeconds > 1)
                    {
                        timestring = Properties.Resources.info_time_remainnig;
                        if (progressRemainingTimeSpan.TotalHours >= 1)
                            timestring += progressRemainingTimeSpan.ToString(@"h\:mm") + "h";
                        else if (progressRemainingTimeSpan.TotalMinutes >= 1)
                            timestring += progressRemainingTimeSpan.ToString(@"m\:ss") + "min";
                        else
                            timestring += progressRemainingTimeSpan.ToString(@"ss") + "sec";
                    }
                    else
                    {
                        timestring = String.Empty;
                    }
                }
            }
            //Update the view
            ProcessStatusText = progressstring;
            ProcessStatusText += timestring;
        }
        /// <summary>
        /// Property displaying the current mode.
        /// </summary>
        public int Mode
        {
            get { return mode; }
            set
            {
                mode = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Property displaying the chosen number of iterations 
        /// </summary>
        public int WorkImageIterations
        {
            get { return workImageIterations; }
            set
            {
                workImageIterations = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Property displaying the chosen intensity for the iterations
        /// </summary>
        public double WorkImageIntensity
        {
            get { return workImageIntensity; }
            set
            {
                workImageIntensity = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Property displaying the chosen octave for the iterations
        /// </summary>
        public int WorkImageOctave
        {
            get { return workImageOctave; }
            set
            {
                workImageOctave = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Property displaying the chosen scaling for the iterations
        /// </summary>
        public int WorkImageScale
        {
            get { return workImageScale; }
            set
            {
                workImageScale = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Property displaying the minimal scale value depending on the current rotation
        /// </summary>
        public double WorkImageMinimalScale
        {
            get { return workImageMinimalScale; }
            set { workImageMinimalScale = value;  OnPropertyChanged();}
        }
        /// <summary>
        /// Property displaying the chosen rotation for the iterations
        /// </summary>
        public double WorkImageRotate
        {
            get { return workImageRotate; }
            set
            {
                value = Math.Round(value, 2);

                if(WorkImage == null)
                {
                    ShowDialogAsync(Properties.Resources.info_rotation,
                                Properties.Resources.info_rotation_detail,
                                MessageDialogStyle.Affirmative);
                    return;
                }
                    //Depending on the rotation, the minimal scale will be changed, so that the picture will fill after a rotation the whole frame
                    var minscale = ImageModel.ScaleToFill(WorkImage.PixelWidth, WorkImage.PixelHeight, value);
                    if (WorkImageScale < minscale)
                    {
                        if (!displayedRotaScaleWarning)
                        {
                            ShowDialogAsync(Properties.Resources.info_scaling,
                                Properties.Resources.info_scaling_detail,
                                MessageDialogStyle.Affirmative);
                            displayedRotaScaleWarning = true;
                        }

                        WorkImageScale = (int)Math.Ceiling(minscale);
                    }
                    WorkImageMinimalScale = minscale;

                workImageRotate = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Property displaying if the shrink of the picture should be done during the deep style process
        /// </summary>
        public bool WorkImageShrinkEnabled
        {
            get { return workImageShrinkEnabled; }
            set
            {
                workImageShrinkEnabled = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Property displaying if the "save" "select" and "delete" buttons should be displayed
        /// </summary>
        public bool ButtonGroupVisible
        {
            get { return resultImages.Count(ri => ri.Selected) > 0; }
        }
        /// <summary>
        /// Property displaying if there is a process running currently.
        /// </summary>
        public bool Running
        {
            get { return running; }
            set
            {
                running = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Property displaying the overall progress of a process
        /// </summary>
        public double OverallProgress
        {
            get { return overallProgress; }
            set
            {
                overallProgress = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Property displaying that the process is currently preparing all the parameter
        /// </summary>
        public bool PreparingProcess
        {
            get { return preparingProcess; }
            set
            {
                preparingProcess = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Property displaying the current status of the process
        /// </summary>
        public string ProcessStatusText
        {
            get { return processStatusText; }
            set
            {
                processStatusText = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Property of the current loaded working image
        /// </summary>
        public BitmapImage WorkImage
        {
            get { return workImagesource; }
            set
            {
                workImagesource = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WorkImageSource));
            }
        }
        /// <summary>
        /// Property of the current loaded style image
        /// </summary>
        public BitmapImage StyleImage
        {
            get { return styleImageSource; }
            set
            {
                styleImageSource = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StyleImageSource));
            }
        }
        /// <summary>
        /// Property of the current loaded help picture
        /// </summary>
        public BitmapImage WorkWaterImage
        {
            get { return workWaterImagesource; }
            set
            {
                workWaterImagesource = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WorkWaterImagesource));
            }
        }
        /// <summary>
        /// Property of the Image that will be processed. Can be ether the work image or the work image rendered with the help picture 
        /// </summary>
        public BitmapImage FinalWorkImage { get; set; }
        /// <summary>
        /// Property displaying the final work image as an ImageSource
        /// </summary>
        public ImageSource FinalWorkImageSourge => FinalWorkImage;
        /// <summary>
        /// Property displaying the help picture as an ImageSource
        /// </summary>
        public ImageSource WorkWaterImagesource => workWaterImagesource;
        /// <summary>
        /// Property displaying the work image as an ImageSource
        /// </summary>
        public ImageSource WorkImageSource => workImagesource;
        /// <summary>
        /// Property displaying the style image as an ImageSource
        /// </summary>
        public ImageSource StyleImageSource => styleImageSource;
        /// <summary>
        /// Property displaying if the work image is loaded
        /// </summary>
        public bool WorkImageLoading
        {
            get { return workImageLoading; }
            set
            {
                workImageLoading = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Property displaying the name of the creator of a report
        /// </summary>
        public string CreatorName { get; set; }
        /// <summary>
        /// Property displaying the name of an report
        /// </summary>
        public string ReportName { get; set; }
        /// <summary>
        /// Property displaying the current id for a report
        /// </summary>
        private string CurrentSeriesId { get; set; }
        /// <summary>
        /// Property displaying the collection of the result images
        /// </summary>
        public ObservableCollection<ResultImageViewModel> ResultImages
        {
            get { return resultImages; }
            set
            {
                resultImages = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Property displaying the current Thumbnail size
        /// </summary>
        public double ThumbnailSize
        {
            get { return thumbnailSize; }
            set
            {
                thumbnailSize = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Async function to display a dialog.
        /// </summary>
        /// <param name="title">The title displayed in the dialog</param>
        /// <param name="message">The message displayed in the dialog</param>
        /// <param name="style">The style chosen for the dialog</param>
        /// <returns>Returning the result of the dialog</returns>
        private async Task<MessageDialogResult> ShowDialogAsync(string title, string message, MessageDialogStyle style)
        {
            //Invoke the ShowDialog event of the main window
            MessageDialogResult res = MessageDialogResult.Affirmative;
            await
                Application.Current.Dispatcher.Invoke(
                    async () => { res = await mainWindowActions.ShowDialog(title, message, style); });
            return res;
        }
        /// <summary>
        /// Async function to display a progress dialog.
        /// </summary>
        /// <param name="title">The title displayed in the dialog</param>
        /// <param name="message">The message displayed in the dialog</param>
        /// <param name="isCanceable">Displaying if the progress dialog can be canceled</param>
        /// <param name="style">The style chosen for the dialog</param>
        /// <returns>Returning the result of the dialog</returns>
        private async Task<ProgressDialogController> ShowProgressDialogAsync(string title, string message, bool isCanceable,
            MetroDialogSettings style)
        {
            //Invoke the ShowProgressDialog event of the main window
            ProgressDialogController res = null;
            await
                Application.Current.Dispatcher.Invoke(
                    async () =>
                    {
                        res = await mainWindowActions.ShowProgressDialog(title, message, isCanceable, style);
                    });
            return res;
        }

        /// <summary>
        /// Async function to load a image. Opens a file dialog for the user, where he can chose a picture.
        /// </summary>
        /// <returns>The chosen loaded picture.</returns>
        private async Task<BitmapImage> LoadImageAsync()
        {
            //Open a file dialog, so that the user can chose a picture
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = $"{Properties.Resources.param_picture} (*.png;*.jpeg;*jpg;*.gif;*.tif)|*.png;*jpg;*.jpeg;*.gif;*.tif"
            };

            if (openFileDialog.ShowDialog() != true) return null;

            var filename = Path.GetFullPath(openFileDialog.FileName);
            if (!File.Exists(filename)) return null;
            //If it is a png, we have to fill transparent space to convert it to a BitMap image
            if (filename.ToLower().EndsWith("png") && !displayedPngWarning)
            {
                displayedPngWarning = true;
                var res =
                    await
                        ShowDialogAsync(Properties.Resources.info_fill_transparency,
                            Properties.Resources.info_fill_transparency_detail,
                            MessageDialogStyle.AffirmativeAndNegative);
                if (res != MessageDialogResult.Affirmative) return null;
            }
            //Load the image
            WorkImageLoading = true;
            var img = await ImageModel.LoadImageAsync(filename);
            WorkImageLoading = false;
            ForceCommandEvaluation();

            if (img == null)
            {
                await ShowDialogAsync(Properties.Resources.info_error, Properties.Resources.info_could_not_open_file, MessageDialogStyle.Affirmative);
            }
            return img;
        }
        /// <summary>
        /// Command Handler to load the working image
        /// </summary>
        /// <param name="o">empty</param>
        private async void LoadWorkImageAsync(Object o)
        {
            WorkImage = await LoadImageAsync() ?? WorkImage;
        }
        /// <summary>
        /// Command Handler to load the help picture
        /// </summary>
        /// <param name="o">empty</param>
        private async void LoadWorkWaterImageAsync(Object o)
        {
            WorkWaterImage = await LoadImageAsync() ?? WorkWaterImage;
        }
        /// <summary>
        /// Command Handler to load the style image
        /// </summary>
        /// <param name="o">empty</param>
        private async void LoadStyleImageAsync(Object o)
        {
            StyleImage = await LoadImageAsync();
        }

        /// <summary>
        /// Property displaying the load work image command
        /// Invoked by view
        /// </summary>
        public ICommand LoadWorkImageCommand
        {
            get
            {
                return loadWorkImageCommand ??
                       (loadWorkImageCommand =
                           new RelayCommand(LoadWorkImageAsync, param => !Running));
            }
        }
        /// <summary>
        /// Property displaying the load help image command
        /// Invoked by view
        /// </summary>
        public ICommand LoadWorkWaterImageCommand
        {
            get
            {
                return loadWorkWaterImageCommand ??
                       (loadWorkWaterImageCommand = new RelayCommand(LoadWorkWaterImageAsync, param => !Running));
            }
        }
        /// <summary>
        /// Property displaying the load style image command
        /// Invoked by view
        /// </summary>
        public ICommand LoadStyleImageCommand
        {
            get
            {
                return loadStyleImageCommand ??
                       (loadStyleImageCommand = new RelayCommand(LoadStyleImageAsync, param => !Running));
            }
        }

        /// <summary>
        /// Property displaying the ToogleButton if a help image should be enabled and visible
        /// </summary>
        public ICommand WaterImageSwitchCheckChangedCommand
        {
            get
            {
                return waterImageSwitchCheckChangedCommand ??
                       (waterImageSwitchCheckChangedCommand = new RelayCommand(OnSwitchCheckChanged, param => !Running));
            }
        }
        /// <summary>
        /// Property displaying the max side length for a result image if the scaling was set true in the stlye
        /// </summary>
        public int WorkImageMaxSideLength
        {
            get { return workImageMaxSideLength; }
            set
            {
                workImageMaxSideLength = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Property displaying if the help image should be visible
        /// </summary>
        public bool LoadWorkerWaterImageEnable
        {
            get { return loadWorkerWaterImageEnable; }
            set
            {
                loadWorkerWaterImageEnable = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Property displaying the opacity of the help image
        /// </summary>
        public double WorkWaterImageOpacity
        {
            get { return workWaterImageOpacity; }
            set
            {
                workWaterImageOpacity = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Command Handler for the help image tool switch checked changed command
        /// </summary>
        /// <param name="o">The toggle switch</param>
        private void OnSwitchCheckChanged(object o)
        {
            ToggleSwitch sender = o as ToggleSwitch;
            LoadWorkerWaterImageEnable = sender?.IsChecked == true;
        }
        /// <summary>
        /// Async function to start a iteration process
        /// </summary>
        /// <param name="o">The canvas where the image is loaded</param>
        private async void StartProcessAsync(Object o)
        {
            //dispaly a message for the user
            var res = MessageDialogResult.Affirmative;
            if (!displayedStartWarning)
            {
                res =
                    await
                        ShowDialogAsync(Properties.Resources.info_confirm_start,
                            Properties.Resources.info_confirm_start_details,
                            MessageDialogStyle.AffirmativeAndNegative);
                displayedStartWarning = true;
            }
            //If he says ok, then start the process
            if (res == MessageDialogResult.Affirmative)
            {
                Trace.WriteLine($"MainViewModel.StartProcessAsync; Start Process Requested");
                double normalizedScale = 1 - (100 / (double)WorkImageScale);
                FinalWorkImage = WorkImage;
                //switch depending on the current mdoe
                switch (Mode)
                {
                    case (int)Modes.Dream:
                        if (LoadWorkerWaterImageEnable)
                        {
                            //If the mode is dream and a help image is enabled, render the canvas where the images are loaded to get the finale work image
                            Canvas source = o as Canvas;
                            if (source != null)
                            {//And start the process
                                
                                FinalWorkImage = ImageModel.RenderCanvas(source);
                                if (FinalWorkImage == null)
                                    Trace.WriteLine("Workimage in StartProcess is null, with waterimage.");
                                Worker.Instance.DeepDream(FinalWorkImage, WorkImageIterations, normalizedScale,
                                    WorkImageIntensity, WorkImageOctave, SelectedModelParameter.Key, WorkImageRotate * -1);
                            }
                        }
                        else
                        {
                            if (FinalWorkImage == null)
                                Trace.WriteLine("Workimage in StartProcess is null, without waterimage.");
                            //Just start the deep dream process
                            Worker.Instance.DeepDream(FinalWorkImage, WorkImageIterations, normalizedScale,
                                WorkImageIntensity, WorkImageOctave, SelectedModelParameter.Key, WorkImageRotate * -1);
                        }

                        break;
                    case (int)Modes.Style:
                        //Start the deep style process
                        Worker.Instance.NeuralArt(FinalWorkImage, StyleImage, WorkImageIterations, WorkImageShrinkEnabled,
                            WorkImageMaxSideLength);
                        break;
                }
            }
        }
        /// <summary>
        /// Property displaying the start process command
        /// </summary>
        public ICommand StartProcessCommand
        {
            get
            {
                return startProcessCommand ??
                       (startProcessCommand =
                           new RelayCommand(StartProcessAsync,
                               param =>
                                   !Running && WorkImage != null &&
                                   (StyleImage != null || Mode == (int)Modes.Dream)));
            }
        }
        /// <summary>
        /// Async function to stop a running progress
        /// Command Handler
        /// </summary>
        /// <param name="o">empty</param>
        private async void StopProcessAsync(Object o)
        {
            var res =
                await
                    ShowDialogAsync(Properties.Resources.info_confirm_cancel,
                        Properties.Resources.info_confirm_cancel_details,
                        MessageDialogStyle.AffirmativeAndNegative);
            if (res != MessageDialogResult.Affirmative) return;
            Trace.WriteLine($"MainViewModel.StopProcessAsync; Stop Process Requested");
            
            //Kill the process
            

            await Task.Factory.StartNew(()=>
            {
                var controller = ShowProgressDialogAsync(Properties.Resources.info_wait,
                Properties.Resources.info_process_will_be_canceled, false,
                new MetroDialogSettings { AnimateShow = true });
                controller.Wait();
                controller.Result.SetIndeterminate();
                Worker.Instance.Kill().ContinueWith(t => controller.Result.CloseAsync());
            });

        }
        /// <summary>
        /// Property displaying the cancel process command
        /// </summary>
        public ICommand CancelProcessCommand
        {
            get
            {
                return cancelProcessCommand ??
                       (cancelProcessCommand =
                           new RelayCommand(StopProcessAsync, param => Running));
            }
        }
        /// <summary>
        /// Async function to save selected images
        /// Command Handler
        /// </summary>
        /// <param name="o">empty</param>
        private async void SaveSelectedImagesAsync(Object o)
        {
            //Chose a folder to save the images and select all selected images
            var selected = resultImages.Where(ri => ri.Selected);
            var dialog = new FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result != DialogResult.OK) return;
            //Save the images
            foreach (var resultImageViewModel in selected)
            {
                await resultImageViewModel.SaveImageAsync(null, dialog.SelectedPath);
            }
        }
        /// <summary>
        /// Property displaying the save selected image command
        /// </summary>
        public ICommand SaveSelectedImagesCommand
        {
            get
            {
                return saveSelectedImagesCommand ??
                       (saveSelectedImagesCommand =
                           new RelayCommand(SaveSelectedImagesAsync, param => true));
            }
        }

        /// <summary>
        /// Function to delete all selected images.
        /// Command Handler
        /// </summary>
        /// <param name="o">empty</param>
        public void DeleteSelectedImages(Object o)
        {
            //Find the selected images
            var arr = resultImages.Where(ri => ri.Selected).ToArray();
            //Delete them
            DeleteImageAsync(arr);
        }
        /// <summary>
        /// Property displaying the delete selected images command
        /// </summary>
        public ICommand DeleteSelectedImagesCommand
        {
            get
            {
                return deleteSelectedImagesCommand ??
                       (deleteSelectedImagesCommand =
                           new RelayCommand(DeleteSelectedImages, param => true));
            }
        }

        /// <summary>
        /// Function to select all result images
        /// Command Handler
        /// </summary>
        /// <param name="o">empty</param>
        public void SelectAll(Object o)
        {
            foreach (var resultImageViewModel in ResultImages)
            {
                //set the selected property for all result images to true
                if (resultImageViewModel.Done) resultImageViewModel.Selected = true;
            }
        }
        /// <summary>
        /// Property displaying the selcet all command
        /// </summary>
        public ICommand SelectAllCommand
        {
            get
            {
                return selectAllCommand ??
                       (selectAllCommand =
                           new RelayCommand(SelectAll, param => ResultImages.Count > 0 && ResultImages.Count(img => img.Selected == false && img.Done)  > 0));
            }
        }

        /// <summary>
        /// Function to deselect all result images
        /// Command Handler
        /// </summary>
        /// <param name="o">empty</param>
        public void SelectNone(Object o)
        {
            foreach (var resultImageViewModel in ResultImages)
            {
                //set the selected property for all result images to false
                if (resultImageViewModel.Done) resultImageViewModel.Selected = false;
            }
        }
        /// <summary>
        /// Property displaying the select none command
        /// </summary>
        public ICommand SelectNoneCommand
        {
            get
            {
                return selectNoneCommand ??
                       (selectNoneCommand =
                           new RelayCommand(SelectNone, param => ButtonGroupVisible));
            }
        }
        /// <summary>
        /// Async function to start writing a report
        /// </summary>
        private async void StartWriteReportAsync()
        {
            var selectedPath = "";
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                //Open a folder browser dialog so that the user can chose a target directory for saving the report
                var selected = resultImages.Where(ri => ri.Selected);
                var dialog = new FolderBrowserDialog();
                var result = dialog.ShowDialog();
                if (result != DialogResult.OK) return;
                selectedPath = dialog.SelectedPath;
            });

            if (String.IsNullOrEmpty(selectedPath)) return;
            //Setup and start the generation
            var rw = new ReportWriter(resultImages.Where(ri => ri.Selected).ToList(), selectedPath, CreatorName, ReportName);
            await rw.Generate();
        }
        /// <summary>
        /// Async function to save a report
        /// Command Handler
        /// </summary>
        /// <param name="o">empty</param>
        private async void SaveReportAsync(Object o)
        {
            await mainWindowActions.ShowNameInputDialog().ContinueWith(t => StartWriteReportAsync());
        }
        /// <summary>
        /// Property displaying the save report command
        /// </summary>
        public ICommand SaveReportCommand
        {
            get
            {
                return saveReportCommand ??
                       (saveReportCommand =
                           new RelayCommand(SaveReportAsync, param => true));
            }
        }

        /// <summary>
        /// Function to delete a result image 
        /// </summary>
        /// <param name="vm">The view model of the result image which should be removed</param>
        private void DeleteImage(ResultImageViewModel vm)
        {
            ResultImageViewModel[] arr = new ResultImageViewModel[1];
            arr[0] = vm;

            DeleteImageAsync(arr);
        }
        /// <summary>
        /// Async function to delete a array of result images
        /// </summary>
        /// <param name="vm">The array of view models from the result images who should be removed</param>
        private async void DeleteImageAsync(ResultImageViewModel[] vm)
        {
            //Ask the user if he wants to remove the pictures
            var res =
                await
                    ShowDialogAsync(Properties.Resources.info_confirm_delete,
                        Properties.Resources.info_confirm_delete_detail,
                        MessageDialogStyle.AffirmativeAndNegative);
            if (res != MessageDialogResult.Affirmative) return;
            if (Application.Current != null)
                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    foreach (var resultImageViewModel in vm)
                    {
                        //Remove all images from the collection and delete them this way
                        resultImageViewModel.Selected = false;
                        mainWindowActions.CloseDetailsWindow(resultImageViewModel);
                        this.ResultImages.Remove(resultImageViewModel);
                    }
                }));
        }
        /// <summary>
        /// Function to force a update of the enabled parameter and the other binded propertys
        /// </summary>
        private void ForceCommandEvaluation()
        {
            Application.Current.Dispatcher?.BeginInvoke(new Action(CommandManager.InvalidateRequerySuggested));
        }
        /// <summary>
        /// Async function to test if the needed docker images are loaded
        /// </summary>
        /// <returns>Task void</returns>
        private async Task TestDockerImagesAsync()
        {
            if (!await DockerModel.TestDockerImagesAsync())
            {
                await
                    ShowDialogAsync(Properties.Resources.info_error,Properties.Resources.info_error_docker_container,
                        MessageDialogStyle.Affirmative);
                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    mainWindowActions.Exit();
                }));
            }
        }
        /// <summary>
        /// Function to test if docker is running
        /// </summary>
        private void TestDockerRunning()
        {
            var controller = ShowProgressDialogAsync(Properties.Resources.info_wait,
                Properties.Resources.info_checking_dependencies, false,
                new MetroDialogSettings { AnimateShow = true });
            controller.Wait();
            controller.Result.SetIndeterminate();

            Trace.WriteLine("Check for Docker.");
            //Check if docker is installed
            var docker = DockerModel.DockerInstalled();
            //Check if docker is running
            var dockerRunning = DockerModel.TestDockerRunningAsync();
            dockerRunning.Wait();

            controller.Result.CloseAsync();


            if (dockerRunning.Result)
            {
                Trace.WriteLine("Docker is running.");
                return;
            }


            if (!docker)
            {//If docker is not installed => close the program
                Trace.WriteLine(Properties.Resources.info_docker_not_installed);
                ShowDialogAsync(Properties.Resources.info_error,
                    Properties.Resources.info_docker_not_installed,
                    MessageDialogStyle.Affirmative).Wait();
               Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    mainWindowActions.Exit();
                }));
            }
            //Try to start docker
            controller = ShowProgressDialogAsync(Properties.Resources.info_wait,
                Properties.Resources.info_docker_starting, false,
                new MetroDialogSettings {AnimateShow = true});
            controller.Wait();
            controller.Result.SetIndeterminate();

            Trace.WriteLine(Properties.Resources.info_docker_starting);

            var startVar = DockerModel.StartDockerAsync();
            startVar.Wait();
            controller.Result.CloseAsync();

            if (startVar.Result)
            {
                Trace.WriteLine("Start of Docker was successfull.");
                return;
            }
            //Something went wrong during start => exit program
            Trace.WriteLine("Start of Docker was not successfull.");
            //It didn't work
            ShowDialogAsync(Properties.Resources.info_error,
                Properties.Resources.info_error_docker_container,
                MessageDialogStyle.Affirmative).Wait();
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                mainWindowActions.Exit();
            }));
        }
        /// <summary>
        /// Async function which will be invoked when there is an event triggered inside the worker class.
        /// </summary>
        /// <param name="sender">Worker instance which triggered the event</param>
        /// <param name="deepDreamEventArgs">Arguments passed by the event</param>
        private async void ProcessEventHandlerAsync(object sender, DeepDreamEventArgs deepDreamEventArgs)
        {
            switch (deepDreamEventArgs.Event)
            {
                case DeepDreamEvents.TotalProgress:
                    //Update for the progress
                    double value;
                    if (Double.TryParse(deepDreamEventArgs.Status, NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture, out value))
                        this.OverallProgress = value;

                    if (PreparingProcess)
                        progresStopwatch.Restart();

                    PreparingProcess = false;
                    break;
                case DeepDreamEvents.ProcessStarted:
                    //Initializing a new process
                    CurrentSeriesId = System.Guid.NewGuid().ToString();
                    PreparingProcess = true;
                    Running = true;
                    progressRemainingTimeSpan = TimeSpan.Zero;
                    progressstring = String.Empty;
                    timestring = String.Empty;
                    firstIteration = true;
                    break;
                case DeepDreamEvents.Progress:
                    break;
                case DeepDreamEvents.StepStarted:
                    //prepare a result image view model for a result image
                    currentResultVm = new ResultImageViewModel(DeleteImage, mainWindowActions.ShowDetailsWindow, this.OnPropertyChanged)
                    {
                        SeriesId = CurrentSeriesId,
                        Intensity = WorkImageIntensity,
                        Octave = WorkImageOctave,
                        SelectedModelParameter = this.SelectedModelParameter,
                        OriginalImage = FinalWorkImage,
                        StyleImage = StyleImage,
                        Mode = (Modes)this.Mode,
                        Name = Properties.Resources.info_wait
                    };
                    ResultImages.AddOnUi(currentResultVm);     
                    break;
                case DeepDreamEvents.StepComplete:
                    //Load the reult image into the prepared view model
                    if (currentResultVm == null) return;
                    currentResultVm.Done = true;
                    currentResultVm.Name = $"{Properties.Resources.param_picture} {deepDreamEventArgs.Status}";
                    int iteration = 0;
                    if (int.TryParse(deepDreamEventArgs.Status, out iteration))
                        currentResultVm.Iteration = iteration;
                    currentResultVm = null;
                    firstIteration = false;
                    UpdateProgressTime();
                    break;
                case DeepDreamEvents.AllComplete:
                    break;
                case DeepDreamEvents.SavedImage:
                    break;
                case DeepDreamEvents.Error:
                    //Something went wrong
                    await ShowDialogAsync(Properties.Resources.info_error, deepDreamEventArgs.Status, MessageDialogStyle.Affirmative);
                    break;
                case DeepDreamEvents.ProcessDone:
                    //Everything is done
                    ProcessStatusText = Properties.Resources.action_process_finished;
                    OverallProgress = 0;
                    Running = false;
                    PreparingProcess = false;
                    ResultImages = resultImages.Where(ri => ri.Done).ToObservableCollection(); //Delete Undone Images
                    break;
                default:
                    break;
            }
            //Update the ui
            UpdateProgressText(true);
            currentResultVm?.ProcessEventHandlerAsync(deepDreamEventArgs);
            ForceCommandEvaluation();
        }
    }
}
