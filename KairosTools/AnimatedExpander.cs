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
                "IsExpanded",
                typeof(bool),
                typeof(AnimatedExpander),
                new PropertyMetadata(false, OnIsExpandedChanged));

        // 内部用：记录当前目标状态，防止被打断的动画执行清理
        private static readonly DependencyProperty TargetStateProperty =
            DependencyProperty.RegisterAttached(
                "TargetState",
                typeof(bool),
                typeof(AnimatedExpander),
                new PropertyMetadata(false));

        public static bool GetIsExpanded(DependencyObject obj)
            => (bool)obj.GetValue(IsExpandedProperty);

        public static void SetIsExpanded(DependencyObject obj, bool value)
            => obj.SetValue(IsExpandedProperty, value);

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is Border border)) return;
            bool expand = (bool)e.NewValue;

            // ★ 关键：在清除动画前读当前高度（Height 会返回动画插值）
            double currentHeight = GetCurrentHeight(border);
            double currentOpacity = border.Opacity;

            // 记录新的目标状态
            border.SetValue(TargetStateProperty, expand);

            // 打断旧动画
            border.BeginAnimation(FrameworkElement.HeightProperty, null);
            border.BeginAnimation(UIElement.OpacityProperty, null);

            // 恢复到当前状态，避免闪一下
            border.Height = currentHeight;
            border.Opacity = currentOpacity;

            if (expand)
            {
                border.Visibility = Visibility.Visible;

                double targetHeight = MeasureTargetHeight(border);
                if (targetHeight <= 0) targetHeight = 100;
                if (currentHeight >= targetHeight) currentHeight = targetHeight;

                // 高度动画
                var heightAnim = new DoubleAnimation(currentHeight, targetHeight,
                    TimeSpan.FromMilliseconds(320))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                heightAnim.Completed += (s, ev) =>
                {
                    // 只有当前目标状态还是"展开"时才清理
                    if ((bool)border.GetValue(TargetStateProperty) != true) return;
                    border.BeginAnimation(FrameworkElement.HeightProperty, null);
                    border.Height = double.NaN;
                };
                border.BeginAnimation(FrameworkElement.HeightProperty, heightAnim);

                // 透明度动画
                var opAnim = new DoubleAnimation(currentOpacity, 1,
                    TimeSpan.FromMilliseconds(240))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                border.BeginAnimation(UIElement.OpacityProperty, opAnim);
            }
            else
            {
                if (currentHeight <= 0) currentHeight = 0;

                // ★ 收回也用 EaseOut，先快后慢
                var heightAnim = new DoubleAnimation(currentHeight, 0,
                    TimeSpan.FromMilliseconds(280))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                heightAnim.Completed += (s, ev) =>
                {
                    // 只有当前目标状态还是"收起"时才清理
                    if ((bool)border.GetValue(TargetStateProperty) != false) return;
                    border.BeginAnimation(FrameworkElement.HeightProperty, null);
                    border.Height = 0;
                    border.Visibility = Visibility.Collapsed;
                };
                border.BeginAnimation(FrameworkElement.HeightProperty, heightAnim);

                var opAnim = new DoubleAnimation(currentOpacity, 0,
                    TimeSpan.FromMilliseconds(220))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                border.BeginAnimation(UIElement.OpacityProperty, opAnim);
            }
        }

        /// <summary>
        /// 读取当前高度。
        /// Height 被动画控制时返回动画插值；否则返回布局高度。
        /// </summary>
        private static double GetCurrentHeight(Border border)
        {
            double h = border.Height;
            if (!double.IsNaN(h) && h >= 0)
                return h;

            double actual = border.ActualHeight;
            if (actual > 0)
                return actual;

            return 0;
        }

        /// <summary>测量展开后的目标高度</summary>
        private static double MeasureTargetHeight(Border border)
        {
            if (border.Child is FrameworkElement child)
            {
                double measureWidth = border.ActualWidth;
                if (measureWidth <= 0) measureWidth = 560;
                child.Measure(new Size(measureWidth, double.PositiveInfinity));
                return child.DesiredSize.Height;
            }
            return 100;
        }
    }
}