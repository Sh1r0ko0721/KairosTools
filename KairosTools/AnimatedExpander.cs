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

            if (expand)
            {
                border.Visibility = Visibility.Visible;

                // ★ 精确测量：临时让高度自适应 + Opacity=0，同步布局，读出真实高度
                double tempOpacity = currentOpacity;
                border.Height = double.NaN;
                border.Opacity = 0;
                border.UpdateLayout();
                double targetHeight = border.ActualHeight;
                if (targetHeight <= 0) targetHeight = 100;

                // 恢复起始状态，准备动画
                border.Height = currentHeight;
                border.Opacity = tempOpacity;
                if (currentHeight >= targetHeight) currentHeight = targetHeight;

                var hAnim = new DoubleAnimation(currentHeight, targetHeight, TimeSpan.FromMilliseconds(260))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.HoldEnd };
                hAnim.Completed += (s, ev) =>
                {
                    if ((int)border.GetValue(AnimationIdProperty) != myId) return;
                    border.BeginAnimation(FrameworkElement.HeightProperty, null);
                    border.Height = double.NaN;
                };
                border.BeginAnimation(FrameworkElement.HeightProperty, hAnim);

                var oAnim = new DoubleAnimation(tempOpacity, 1, TimeSpan.FromMilliseconds(200))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.HoldEnd };
                border.BeginAnimation(UIElement.OpacityProperty, oAnim);
            }
            else
            {
                border.Height = currentHeight;
                border.Opacity = currentOpacity;

                var hAnim = new DoubleAnimation(currentHeight, 0, TimeSpan.FromMilliseconds(240))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.HoldEnd };
                hAnim.Completed += (s, ev) =>
                {
                    if ((int)border.GetValue(AnimationIdProperty) != myId) return;
                    border.BeginAnimation(FrameworkElement.HeightProperty, null);
                    border.Height = 0;
                    border.Visibility = Visibility.Collapsed;
                };
                border.BeginAnimation(FrameworkElement.HeightProperty, hAnim);

                var oAnim = new DoubleAnimation(currentOpacity, 0, TimeSpan.FromMilliseconds(180))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.HoldEnd };
                border.BeginAnimation(UIElement.OpacityProperty, oAnim);
            }
        }
    }
}