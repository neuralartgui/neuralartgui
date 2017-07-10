using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace DeepdreamGui.ViewModel.WaterImageThumb
{
    /// <summary>
    /// Class for the rotate behavior of the Helpimage.
    /// Based on https://www.codeproject.com/articles/22952/wpf-diagram-designer-part-1
    /// </summary>
    public class RotateThumb:Thumb
    {
        /// <summary>
        /// Center point of the control
        /// </summary>
        private Point _centerPoint;
        /// <summary>
        /// The vector from the center point to the mouse
        /// </summary>
        private Vector _startVector;
        /// <summary>
        /// The initial angle of an maybe already existing rotation
        /// </summary>
        private double _initialAngle;
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
        /// register on the DragDelta for the value change in x and y during the drag
        /// </summary>
        public RotateThumb()
        {
            DragDelta += OnDragDelta;
            DragStarted += OnDragStarted;
        }

        /// <summary>
        /// Event Handler to get the states from the beginning of the drag from the control elements.
        /// </summary>
        /// <param name="sender">The Control which will be dragged</param>
        /// <param name="dragStartedEventArgs">Eventargs, will not be used</param>
        private void OnDragStarted(object sender, DragStartedEventArgs dragStartedEventArgs)
        {
            //Save the Elements, so we don't have to do the casting every time again during the drag
            _item = DataContext as Control;
            if (_item == null) return;
            _source = VisualTreeHelper.GetParent(_item) as Canvas;
            if(_source == null) return;

            //calculate the center point of the control inside the canvas
            _centerPoint = _item.TranslatePoint(new Point(_item.ActualWidth * _item.RenderTransformOrigin.X, _item.ActualHeight * _item.RenderTransformOrigin.Y), _source);
            //calculate the startvector in the canvas of the mouse position to the center point
            _startVector = Point.Subtract(Mouse.GetPosition(_source), _centerPoint);
            //Get the rotation angle if there was already one. If not, create an rotation transformation
            RotateTransform rotateTransform = _item.RenderTransform as RotateTransform;
            if (rotateTransform == null)
            {
                _item.RenderTransform = new RotateTransform();
                _initialAngle = 0.0;
            }
            else
            {
                _initialAngle = rotateTransform.Angle;
            }
        }

        /// <summary>
        /// Event Handler to get the change of the control elements
        /// </summary>
        /// <param name="sender">The control which will be dragged(rotated).</param>
        /// <param name="dragDeltaEventArgs">Eventargs, containing the horizontal and vertical change, so the change in x and y</param>
        private void OnDragDelta(object sender, DragDeltaEventArgs dragDeltaEventArgs)
        {
            //Check if we have the item and source for dragging, if not so, there is an error and we can leave.
            if (_item == null|| _source == null) return;
            //Calculate the angle of which the mouse has moved from the beginning
            double angle = Vector.AngleBetween(_startVector, Point.Subtract(Mouse.GetPosition(_source), _centerPoint));
            //Get the currently existing rotation transformation of the item
            RotateTransform rotateTransform = _item.RenderTransform as RotateTransform;
            if (rotateTransform == null) return;
            //Add the new rotation angle to the initial angle and calculate it depending on the transformation
            rotateTransform.Angle = _initialAngle + Math.Round(angle, 0);
            _item.InvalidateMeasure();
        }
    }
}
