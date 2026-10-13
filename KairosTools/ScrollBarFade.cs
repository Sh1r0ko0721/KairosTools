using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace KairosTools
{
    public static class ScrollBarFade
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled", typeof(bool), typeof(ScrollBarFade),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is ScrollViewer sv)) return;

            if ((bool)e.NewValue)
            {
                sv.Loaded += Sv_Loaded;
                sv.ScrollChanged += Sv_ScrollChanged;
                sv.SizeChanged += Sv_SizeChanged;
                sv.IsVisibleChanged += Sv_IsVisibleChanged;
                sv.Unloaded += Sv_Unloaded;
            }
            else
            {
                sv.Loaded -= Sv_Loaded;
                sv.ScrollChanged -= Sv_ScrollChanged;
                sv.SizeChanged -= Sv_SizeChanged;
                sv.IsVisibleChanged -= Sv_IsVisibleChanged;
                sv.Unloaded -= Sv_Unloaded;
            }
        }

        private static void Sv_Loaded(object sender, RoutedEventArgs e)
        {
            var sv = (ScrollViewer)sender;
            sv.Dispatcher.BeginInvoke(new Action(() =>
            {
                sv.UpdateLayout();
                bool need = sv.ScrollableHeight > 0.5 || sv.ScrollableWidth > 0.5;
                foreach (var bar in FindScrollBars(sv))
                {
                    bar.BeginAnimation(UIElement.OpacityProperty, null);
                    bar.Opacity = need ? 1 : 0;
                }
            }), DispatcherPriority.Loaded);
        }

        private static void Sv_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (Math.Abs(e.ExtentHeightChange) < 0.1 &&
                Math.Abs(e.ViewportHeightChange) < 0.1)
                return;
            QueueUpdate((ScrollViewer)sender);
        }

        private static void Sv_SizeChanged(object sender, SizeChangedEventArgs e)
            => QueueUpdate((ScrollViewer)sender);

        private static void Sv_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var sv = (ScrollViewer)sender;
            if ((bool)e.NewValue)
            {
                sv.Dispatcher.BeginInvoke(new Action(() =>
                {
                    foreach (var bar in FindScrollBars(sv))
                    {
                        bar.BeginAnimation(UIElement.OpacityProperty, null);
                        bar.Opacity = 0;
                    }
                    sv.UpdateLayout();
                    Update(sv);
                }), DispatcherPriority.Loaded);
            }
            else
            {
                foreach (var bar in FindScrollBars(sv))
                {
                    bar.BeginAnimation(UIElement.OpacityProperty, null);
                    bar.Opacity = 0;
                }
            }
        }

        private static void Sv_Unloaded(object sender, RoutedEventArgs e)
        {
            var sv = (ScrollViewer)sender;
            sv.Loaded -= Sv_Loaded;
            sv.ScrollChanged -= Sv_ScrollChanged;
            sv.SizeChanged -= Sv_SizeChanged;
            sv.IsVisibleChanged -= Sv_IsVisibleChanged;
            sv.Unloaded -= Sv_Unloaded;
        }

        private static void QueueUpdate(ScrollViewer sv)
        {
            sv.Dispatcher.BeginInvoke(new Action(() => Update(sv)),
                DispatcherPriority.Background);
        }

        private static void Update(ScrollViewer sv)
        {
            sv.UpdateLayout();
            bool shouldShow = sv.ScrollableHeight > 0.5 || sv.ScrollableWidth > 0.5;
            double target = shouldShow ? 1 : 0;

            foreach (var bar in FindScrollBars(sv))
            {
                double current = bar.Opacity;
                if (Math.Abs(current - target) < 0.01) continue;

                var anim = new DoubleAnimation(current, target,
                    TimeSpan.FromMilliseconds(shouldShow ? 240 : 400))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                    FillBehavior = FillBehavior.HoldEnd
                };
                bar.BeginAnimation(UIElement.OpacityProperty, anim);
            }
        }

        private static IEnumerable<ScrollBar> FindScrollBars(DependencyObject parent)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is ScrollBar sb) yield return sb;
                foreach (var sub in FindScrollBars(child)) yield return sub;
            }
        }
    }
}