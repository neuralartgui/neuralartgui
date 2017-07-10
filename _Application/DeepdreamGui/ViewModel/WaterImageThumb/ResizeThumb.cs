using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace DeepdreamGui.ViewModel.WaterImageThumb
{
    /// <summary>
    /// Class for the resize behavior of the Helpimage.
    /// Based on https://www.codeproject.com/articles/22952/wpf-diagram-designer-part-1
    /// </summary>
    public class ResizeThumb:Thumb
    {
        /// <summary>
        /// The angle of the rotated transformation of the item in radians
        /// </summary>
        private double _angle;
        /// <summary>
        /// Original transformation of the item
        /// </summary>
        private Point _transformOrigin;
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
        /// register on the ResizeThumbDragDelta for the value change in x and y during the drag
        /// </summary>
        public ResizeThumb()
        {
            DragStarted += ResizeThumb_DragStarted;
            DragDelta += ResizeThumbDragDelta;
        }
        /// <summary>
        /// Event Handler to get the states from the beginning of the drag from the control elements.
        /// </summary>
        /// <param name="sender">The Control which will be dragged</param>
        /// <param name="e">Eventargs, will not be used</param>
        private void ResizeThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            //Save the Elements, so we don't have to do the casting every time again during the drag
            _item = DataContext as Control;
            if (_item == null) return;
            _source = VisualTreeHelper.GetParent(_item) as Canvas;
            if (_source == null) return;
            //Get the rotation transformation
            _transformOrigin = _item.RenderTransformOrigin;
            RotateTransform rotateTransform = _item.RenderTransform as RotateTransform;
            //If there is an rotation transformation, get the angle in radians of this rotation for the resizing 
            _angle = rotateTransform?.Angle*Math.PI/180.0 ?? 0;

        }
        /// <summary>
        /// Event Handler to get the change of the control elements
        /// </summary>
        /// <param name="sender">The control which will be dragged(resized).</param>
        /// <param name="args">Eventargs, containing the horizontal and vertical change, so the change in x and y</param>
        private void ResizeThumbDragDelta(object sender, DragDeltaEventArgs args)
        {
            //Check if we have the item and source for dragging, if not so, there is an error and we can leave.
            if (_item == null || _source == null) return;
            double deltaVertical, deltaHorizontal;
            //Check if the thumb is Aligned to the Bottom or the Top of the canvas
            //And change the height of the item
            switch (VerticalAlignment)
            {
                case System.Windows.VerticalAlignment.Bottom:
                    //Check if the item is still inside the canvas with the resizing
                    deltaVertical = Math.Min(-args.VerticalChange, _item.ActualHeight - _item.MinHeight);
                    if (_item.Height - deltaVertical < 0 || Canvas.GetTop(_item) + _item.Height - deltaVertical >= _source.ActualHeight)
                    {
                        deltaVertical = 0;
                    }
                    //Move the control from the transform origin including the angle of an rotation and the delta it should be resized
                    Canvas.SetTop(_item, Canvas.GetTop(_item) + (_transformOrigin.Y*deltaVertical*(1 - Math.Cos(-_angle))));
                    Canvas.SetLeft(_item, Canvas.GetLeft(_item) - deltaVertical*_transformOrigin.Y*Math.Sin(-_angle));
                    //resize the item
                    _item.Height -= deltaVertical;
                    break;
                case System.Windows.VerticalAlignment.Top:
                    //Check if the item is still inside the canvas with the resizing
                    deltaVertical = Math.Min(args.VerticalChange, _item.ActualHeight - _item.MinHeight);
                    if (_item.Height + deltaVertical < 0 || Canvas.GetTop(_item)+ deltaVertical < 0)// || Canvas.GetTop(_item) + _item.Height - deltaVertical > _source.ActualHeight
                    {
                        deltaVertical = 0;
                    }
                    //Move the control from the transform origin including the angle of an rotation and the delta it should be resized
                    Canvas.SetTop(_item,
                        Canvas.GetTop(_item) + deltaVertical*Math.Cos(-_angle) +
                        (_transformOrigin.Y*deltaVertical*(1 - Math.Cos(-_angle))));
                    Canvas.SetLeft(_item,
                        Canvas.GetLeft(_item) + deltaVertical*Math.Sin(-_angle) -
                        (_transformOrigin.Y*deltaVertical*Math.Sin(-_angle)));
                    //resize the item
                    _item.Height -= deltaVertical;
                    break;

            }

            //Check if the thumb is Aligned to the Left or Right of the canvas
            //And change the width of the item
            switch (HorizontalAlignment)
            {
                case System.Windows.HorizontalAlignment.Left:
                    //Check if the item is still inside the canvas with the resizing
                    deltaHorizontal = Math.Min(args.HorizontalChange, _item.ActualWidth - _item.MinWidth);
                    if (_item.Width - deltaHorizontal < 0  || Canvas.GetLeft(_item)+ deltaHorizontal<0)//|| Canvas.GetLeft(_item) + _item.Width + deltaHorizontal > _source.ActualWidth
                    {
                        deltaHorizontal = 0;
                    }
                    //Move the control from the transform origin including the angle of an rotation and the delta it should be resized
                    Canvas.SetTop(_item,
                        Canvas.GetTop(_item) + deltaHorizontal*Math.Sin(_angle) -
                        _transformOrigin.X*deltaHorizontal*Math.Sin(_angle));
                    Canvas.SetLeft(_item,
                        Canvas.GetLeft(_item) + deltaHorizontal*Math.Cos(_angle) +
                        (_transformOrigin.X*deltaHorizontal*(1 - Math.Cos(_angle))));
                    //resize the item
                    _item.Width -= deltaHorizontal;
                    break;
                case System.Windows.HorizontalAlignment.Right:
                    //Check if the item is still inside the canvas with the resizing
                    deltaHorizontal = Math.Min(-args.HorizontalChange, _item.ActualWidth - _item.MinWidth);
                    if (_item.Width - deltaHorizontal < 0 ||
                        Canvas.GetLeft(_item) + _item.Width - deltaHorizontal > _source.ActualWidth)
                    {
                        deltaHorizontal = 0;
                    }
                    //Move the control from the transform origin including the angle of an rotation and the delta it should be resized
                    Canvas.SetTop(_item, Canvas.GetTop(_item) - _transformOrigin.X*deltaHorizontal*Math.Sin(_angle));
                    Canvas.SetLeft(_item,
                        Canvas.GetLeft(_item) + (deltaHorizontal*_transformOrigin.X*(1 - Math.Cos(_angle))));
                    //resize the item
                    _item.Width -= deltaHorizontal;
                    break;
            }
            args.Handled = true;
        }
    }
}
