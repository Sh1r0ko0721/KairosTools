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

        public static bool GetIsExpanded(DependencyObject obj) => (bool)obj.GetValue(IsExpandedProperty);
        public static void SetIsExpanded(DependencyObject obj, bool value) => obj.SetValue(IsExpandedProperty, value);

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is Border border)) return;
            bool expand = (bool)e.NewValue;

            // 弱PC模式：直接切换
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
                double targetHeight = MeasureTargetHeight(border, currentHeight);
                if (currentHeight >= targetHeight) currentHeight = targetHeight;

                var heightAnim = new DoubleAnimation(currentHeight, targetHeight,
                    TimeSpan.FromMilliseconds(320))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                    FillBehavior = FillBehavior.HoldEnd
                };

                // ★ 关键：动画完成后释放 Height，让布局自适应，避免改变窗口尺寸时出现空白
                heightAnim.Completed += (s, ev) =>
                {
                    if (border.Visibility == Visibility.Visible)
                    {
                        border.BeginAnimation(FrameworkElement.HeightProperty, null);
                        border.Height = double.NaN;
                    }
                };

                border.BeginAnimation(FrameworkElement.HeightProperty, heightAnim);

                var opAnim = new DoubleAnimation(currentOpacity, 1,
                    TimeSpan.FromMilliseconds(240))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                    FillBehavior = FillBehavior.HoldEnd
                };
                border.BeginAnimation(UIElement.OpacityProperty, opAnim);
            }
            else
            {
                var heightAnim = new DoubleAnimation(currentHeight, 0,
                    TimeSpan.FromMilliseconds(280))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                    FillBehavior = FillBehavior.HoldEnd
                };
                border.BeginAnimation(FrameworkElement.HeightProperty, heightAnim);

                var opAnim = new DoubleAnimation(currentOpacity, 0,
                    TimeSpan.FromMilliseconds(220))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                    FillBehavior = FillBehavior.HoldEnd
                };
                border.BeginAnimation(UIElement.OpacityProperty, opAnim);
            }
        }

        private static double MeasureTargetHeight(Border border, double currentHeight)
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