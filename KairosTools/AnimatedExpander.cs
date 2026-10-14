using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace KairosTools
{
    public static class AnimatedExpander
    {
        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.RegisterAttached(
                "IsExpanded", typeof(bool), typeof(AnimatedExpander),
                new PropertyMetadata(false, OnIsExpandedChanged));

        private static readonly DependencyProperty AnimationIdProperty =
            DependencyProperty.RegisterAttached(
                "AnimationId", typeof(int), typeof(AnimatedExpander),
                new PropertyMetadata(0));

        public static bool GetIsExpanded(DependencyObject obj) => (bool)obj.GetValue(IsExpandedProperty);
        public static void SetIsExpanded(DependencyObject obj, bool value) => obj.SetValue(IsExpandedProperty, value);

        private static int _globalId = 0;

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is Border border)) return;
            bool expand = (bool)e.NewValue;
            int myId = ++_globalId;
            border.SetValue(AnimationIdProperty, myId);

            if (AppSettings.Instance.IsWeakPcMode)
            {
                border.BeginAnimation(FrameworkElement.HeightProperty, null);
                border.BeginAnimation(UIElement.OpacityProperty, null);
                if (expand)
                {
                    border.Visibility = Visibility.Visible;
                    border.Height = double.NaN;
                    border.Opacity = 1;
                }
                else
                {
                    border.Height = 0;
                    border.Opacity = 0;
                    border.Visibility = Visibility.Collapsed;
                }
                return;
            }

            double currentHeight = border.Height;
            if (double.IsNaN(currentHeight) || currentHeight < 0)
                currentHeight = border.ActualHeight;
            if (currentHeight < 0) currentHeight = 0;
            double currentOpacity = border.Opacity;

            border.BeginAnimation(FrameworkElement.HeightProperty, null);
            border.BeginAnimation(UIElement.OpacityProperty, null);
            border.Height = currentHeight;
            border.Opacity = currentOpacity;

            if (expand)
            {
                border.Visibility = Visibility.Visible;
                double targetHeight = MeasureHeight(border, currentHeight);
                if (currentHeight >= targetHeight) currentHeight = targetHeight;

                var hAnim = new DoubleAnimation(currentHeight, targetHeight, TimeSpan.FromMilliseconds(320))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.HoldEnd };
                hAnim.Completed += (s, ev) =>
                {
                    if ((int)border.GetValue(AnimationIdProperty) != myId) return;
                    border.BeginAnimation(FrameworkElement.HeightProperty, null);
                    border.Height = double.NaN;
                };
                border.BeginAnimation(FrameworkElement.HeightProperty, hAnim);

                var oAnim = new DoubleAnimation(currentOpacity, 1, TimeSpan.FromMilliseconds(240))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.HoldEnd };
                border.BeginAnimation(UIElement.OpacityProperty, oAnim);
            }
            else
            {
                var hAnim = new DoubleAnimation(currentHeight, 0, TimeSpan.FromMilliseconds(280))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.HoldEnd };
                hAnim.Completed += (s, ev) =>
                {
                    if ((int)border.GetValue(AnimationIdProperty) != myId) return;
                    border.BeginAnimation(FrameworkElement.HeightProperty, null);
                    border.Height = 0;
                    border.Visibility = Visibility.Collapsed;
                };
                border.BeginAnimation(FrameworkElement.HeightProperty, hAnim);

                var oAnim = new DoubleAnimation(currentOpacity, 0, TimeSpan.FromMilliseconds(220))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.HoldEnd };
                border.BeginAnimation(UIElement.OpacityProperty, oAnim);
            }
        }

        private static double MeasureHeight(Border border, double currentHeight)
        {
            border.Height = double.NaN;
            double width = border.ActualWidth;
            if (width <= 0)
            {
                var parent = border.Parent as FrameworkElement;
                if (parent != null && parent.ActualWidth > 0) width = parent.ActualWidth;
                else width = 560;
            }
            border.Measure(new Size(width, double.PositiveInfinity));
            double h = border.DesiredSize.Height;
            if (h <= 0) h = 100;
            border.Height = currentHeight;
            return h;
        }
    }
}