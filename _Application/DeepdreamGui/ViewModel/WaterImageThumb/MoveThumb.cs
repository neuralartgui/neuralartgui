using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using MahApps.Metro.Controls;

namespace DeepdreamGui.ViewModel.WaterImageThumb
{
    /// <summary>
    /// Class for the moving behavior of the Helpimage.
    /// Based on https://www.codeproject.com/articles/22952/wpf-diagram-designer-part-1
    /// </summary>
    public class MoveThumb:Thumb
    {
        /// <summary>
        /// Control that contains the image.
        /// </summary>
        private Control _item;
        /// <summary>
        /// Canvas that contains the control with the image.
        /// </summary>
        private Canvas _source;
        /// <summary>
        /// Constructor 
        /// register on DragStarted event for the beginning state of the drag  
        /// register on the MoveThumbDragDelta for the value change in x and y during the drag
        /// </summary>
        public MoveThumb()
        {
            DragDelta += MoveThumbDragDelta; 
            DragStarted += OnDragStarted;
        }
        /// <summary>
        /// Event Handler to get the states at the beginning of the control elements.
        /// </summary>
        /// <param name="sender">The Control which will be dragged</param>
        /// <param name="dragStartedEventArgs">Eventargs, will not be used</param>
        private void OnDragStarted(object sender, DragStartedEventArgs dragStartedEventArgs)
        {
            //Save the Elements, so we don't have to do the casting every time again during the drag
            _item = this.DataContext as Control;
            if (_item == null) return;
            _source = VisualTreeHelper.GetParent(_item) as Canvas;
            if (_source == null) return;
        }
        /// <summary>
        /// Event Handler to get the change of the control elements
        /// </summary>
        /// <param name="sender">The control which will be dragged.</param>
        /// <param name="args">Eventargs, containing the horizontal and vertical change, so the change in x and y</param>
        private void MoveThumbDragDelta(object sender, DragDeltaEventArgs args)
        {
            //Check if we have the item and source for dragging, if not so, there is an error and we can leave.
            if(_item == null || _source == null)return;
            //Get the current rotation of the item
            RotateTransform rotateTransform = _item.RenderTransform as RotateTransform;
            //Transform the change in x and y with the transformation of th item, to get the drag delta in context of an already existing change.
            //if there is no transformation yet, just get the change as a point
            Point delta = rotateTransform?.Transform(new Point(args.HorizontalChange, args.VerticalChange)) ?? new Point(args.HorizontalChange, args.VerticalChange);
            //Add the drag delta to the current position
            double X = Canvas.GetLeft(_item) + delta.X;
            double Y = Canvas.GetTop(_item) + delta.Y;
            //check if the new position is still inside the canvas
            //If not, set it to the edge
            if (X > _source.ActualWidth - _item.ActualWidth)
            {
                X = _source.ActualWidth - _item.ActualWidth;
            }else if (X < 0.0)
            {
                X = 0.0;
            }
            if (Y > _source.ActualHeight - _item.ActualHeight)
            {
                Y = _source.ActualHeight - _item.ActualHeight;
            }else if (Y < 0.0)
            {
                Y = 0.0;
            }
            //Move the item
            Canvas.SetLeft(_item, X);
            Canvas.SetTop(_item, Y);
        }
    }
}
