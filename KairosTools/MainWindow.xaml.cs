using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace KairosTools
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<BasicItem> BasicItems { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            bool mouseAccelOn = MouseHelper.IsMouseAccelerationEnabled();
            bool sysMainOn = SystemHelper.IsSysMainEnabled();
            bool memCompOn = SystemHelper.IsMemoryCompressionEnabled();
            bool hagsOn = SystemHelper.IsHagsEnabled();

            BasicItems = new ObservableCollection<BasicItem>
            {
                new BasicItem
                {
                    Name = "鼠标加速",
                    Description =
                        "移除鼠标指针的加速，保持严格的线性1:1鼠标移动。\n\n" +
                        "“增强指针精度”增加了指针路径与传感器移动速度的依赖性：鼠标移动得越快，指针移动得越远。" +
                        "这破坏了肌肉记忆，使瞄准变得不可预测，因此所有职业选手都保持加速关闭。" +
                        "调整将MouseSpeed=0和两个阈值=0硬性写入HKCU\\Control Panel\\Mouse。" +
                        "在Windows中也可以通过”设置一蓝牙和设备一鼠标”来实现。",
                    RecommendText = "个人建议在任何竞技游戏中禁用；如果主要使用触控板，则保持默认。",
                    Recommended = false,
                    NeedRestart = false,
                    IsOn = mouseAccelOn
                },
                new BasicItem
                {
                    Name = "SysMain",
                    Description =
                        "禁用 Prefetch、Superfetch。\n\n" +
                        "SysMain 分析启动的应用程序并提前将其数据预热到缓存中。在HDD上这加快了启动速度，" +
                        "但在带 SSD的游戏系统上，服务有时会消耗5-10%的CPU或导致短暂的负载峰值。将其禁用可消除这些峰值。" +
                        "微软建议在实时设备和高延迟场景中禁用 SysMain。",
                    RecommendText =
                        "对于SSD，禁用；在HDD上，在做决定前建议测试游戏加载时间影响。" +
                        "禁用减少写入操作次数，延长固态硬盘寿命并降低CPU使用率。",
                    Recommended = false,
                    NeedRestart = false,
                    IsOn = sysMainOn
                },
                new BasicItem
                {
                    Name = "内存压缩",
                    Description =
                        "禁用以减少CPU占用，优化带宽的占用与访问延迟。\n\n" +
                        "内存压缩是用CPU开销和访问延迟换内存容量，代价是 CPU 占用升高、访问变慢、尾延迟和系统抖动变差；" +
                        "冷页且压缩比高时影响较小，热数据多或压缩比低时可能明显拖慢系统。" +
                        "使用Disable-MMAgent -mc可解决此问题。",
                    RecommendText = "在24GB及以上的设备中，建议禁用；如果内存紧张，则保持默认。",
                    Recommended = false,
                    NeedRestart = true,
                    IsOn = memCompOn
                },
                new BasicItem
                {
                    Name = "HAGS",
                    Description =
                        "启用硬件加速GPU调度(HAGS)。\n\n" +
                        "HAGS将命令队列管理从系统调度程序转移到GPU 的一个模块，从而减轻CPU上的高优先级线程负担，" +
                        "并可能降低渲染延迟。在实际测试中，帧数差异在0到+3之间波动，" +
                        "但该技术对于DLSS-FG和AMD FSR3帧生成是必需的。" +
                        "调整将HwSchMode设置为2(启用)。",
                    RecommendText =
                        "NVIDIA 2000系和AMD 7000系以上启用；在2018年之前的系统上保持默认。\n" +
                        "如果需要开启DLSS帧生成和FSR3帧生成，那么此项目必须启用。",
                    Recommended = true,
                    NeedRestart = true,
                    IsOn = hagsOn
                },
            };

            foreach (var item in BasicItems)
            {
                item.PropertyChanged += async (s, e) =>
                {
                    if (e.PropertyName != nameof(BasicItem.IsOn)) return;

                    bool isOn = item.IsOn;

                    await Task.Run(() =>
                    {
                        switch (item.Name)
                        {
                            case "鼠标加速":
                                if (isOn) MouseHelper.EnableMouseAcceleration();
                                else MouseHelper.DisableMouseAcceleration();
                                break;
                            case "SysMain":
                                if (isOn) SystemHelper.EnableSysMain();
                                else SystemHelper.DisableSysMain();
                                break;
                            case "内存压缩":
                                if (isOn) SystemHelper.EnableMemoryCompression();
                                else SystemHelper.DisableMemoryCompression();
                                break;
                            case "HAGS":
                                if (isOn) SystemHelper.EnableHags();
                                else SystemHelper.DisableHags();
                                break;
                        }
                    });

                    string suffix = item.NeedRestart ? "，重启后生效" : "";
                    ShowToast($"{item.Name}（{(isOn ? "启用" : "禁用")}）设置完毕{suffix}");
                };
            }

            DataContext = this;

            Loaded += (s, e) =>
            {
                NavHighlight.SetActive(NavHome, true);
            };
        }

        private async void ShowToast(string message)
        {
            ToastText.Text = message;
            Toast.Visibility = Visibility.Visible;

            Toast.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)));

            await Task.Delay(2400);

            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
            fadeOut.Completed += (s, e) => Toast.Visibility = Visibility.Collapsed;
            Toast.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        // ==================== 基础页动画 ====================

        private void PlaySlideInAnimation()
        {
            if (BasicItemsControl == null) return;

            AnimateSlideIn(BasicHeader, 0);

            for (int i = 0; i < BasicItemsControl.Items.Count; i++)
            {
                var container = BasicItemsControl.ItemContainerGenerator
                    .ContainerFromIndex(i) as ContentPresenter;
                if (container == null) continue;

                var root = VisualTreeHelper.GetChild(container, 0) as FrameworkElement;
                if (root == null) continue;

                AnimateSlideIn(root, (i + 1) * 60);
            }
        }

        private void AnimateSlideIn(FrameworkElement el, int delayMs)
        {
            if (el == null) return;

            el.BeginAnimation(UIElement.OpacityProperty, null);
            el.Opacity = 0;

            var tt = new TranslateTransform(-70, 0);
            el.RenderTransform = tt;

            var opAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280))
            {
                BeginTime = TimeSpan.FromMilliseconds(delayMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            el.BeginAnimation(UIElement.OpacityProperty, opAnim);

            var xAnim = new DoubleAnimation(-70, 0, TimeSpan.FromMilliseconds(340))
            {
                BeginTime = TimeSpan.FromMilliseconds(delayMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            tt.BeginAnimation(TranslateTransform.XProperty, xAnim);
        }

        private void PlayFadeIn(FrameworkElement el)
        {
            if (el == null) return;

            el.BeginAnimation(UIElement.OpacityProperty, null);
            el.Opacity = 0;

            var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(320))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            el.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        // ==================== 侧栏"优化"子菜单 ====================

        private void NavOptimize_Checked(object sender, RoutedEventArgs e)
        {
            PlaySubMenuAnimation(true);
            AnimateArrow(OptimizeArrow, 90);
        }

        private void NavOptimize_Unchecked(object sender, RoutedEventArgs e)
        {
            PlaySubMenuAnimation(false);
            AnimateArrow(OptimizeArrow, 0);
        }

        private void AnimateArrow(TextBlock arrow, double toAngle)
        {
            if (arrow == null) return;
            if (!(arrow.RenderTransform is RotateTransform rot))
            {
                rot = new RotateTransform(0);
                arrow.RenderTransform = rot;
            }
            var anim = new DoubleAnimation(rot.Angle, toAngle, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.HoldEnd
            };
            rot.BeginAnimation(RotateTransform.AngleProperty, anim);
        }

        private void PlaySubMenuAnimation(bool expand)
        {
            if (OptimizeSubMenuBorder == null) return;

            double currentHeight = OptimizeSubMenuBorder.Height;
            if (double.IsNaN(currentHeight) || currentHeight < 0)
                currentHeight = OptimizeSubMenuBorder.ActualHeight;
            if (currentHeight < 0) currentHeight = 0;

            OptimizeSubMenuBorder.BeginAnimation(FrameworkElement.HeightProperty, null);
            OptimizeSubMenuBorder.Height = currentHeight;

            if (expand)
            {
                OptimizeSubMenuBorder.Visibility = Visibility.Visible;
                double targetHeight = MeasureSubMenuHeight(currentHeight);
                if (currentHeight >= targetHeight) currentHeight = targetHeight;

                var anim = new DoubleAnimation(currentHeight, targetHeight,
                    TimeSpan.FromMilliseconds(300))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                    FillBehavior = FillBehavior.HoldEnd
                };
                OptimizeSubMenuBorder.BeginAnimation(FrameworkElement.HeightProperty, anim);

                PlaySubItemsStaggered();
            }
            else
            {
                var anim = new DoubleAnimation(currentHeight, 0,
                    TimeSpan.FromMilliseconds(260))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                    FillBehavior = FillBehavior.HoldEnd
                };
                OptimizeSubMenuBorder.BeginAnimation(FrameworkElement.HeightProperty, anim);
            }
        }

        private double MeasureSubMenuHeight(double currentHeight)
        {
            OptimizeSubMenuBorder.Height = double.NaN;
            OptimizeSubMenuBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double h = OptimizeSubMenuBorder.DesiredSize.Height;
            if (h <= 0) h = 6 * 36;
            OptimizeSubMenuBorder.Height = currentHeight;
            return h;
        }

        private void PlaySubItemsStaggered()
        {
            var subItems = new FrameworkElement[]
            {
                SubOptimizePlan, SubOptimizeBasic, SubOptimizeSecurity,
                SubOptimizePower, SubOptimizePrivacy, SubOptimizeTask
            };

            for (int i = 0; i < subItems.Length; i++)
            {
                var el = subItems[i];
                if (el == null) continue;

                el.BeginAnimation(UIElement.OpacityProperty, null);
                el.Opacity = 0;

                if (!(el.RenderTransform is TranslateTransform))
                {
                    el.RenderTransform = new TranslateTransform(0, -8);
                }
                var tt = el.RenderTransform as TranslateTransform;
                tt.X = 0;
                tt.Y = -8;

                int delay = i * 35;

                var opAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220))
                {
                    BeginTime = TimeSpan.FromMilliseconds(delay),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                    FillBehavior = FillBehavior.HoldEnd
                };
                el.BeginAnimation(UIElement.OpacityProperty, opAnim);

                var yAnim = new DoubleAnimation(-8, 0, TimeSpan.FromMilliseconds(260))
                {
                    BeginTime = TimeSpan.FromMilliseconds(delay),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                    FillBehavior = FillBehavior.HoldEnd
                };
                tt.BeginAnimation(TranslateTransform.YProperty, yAnim);
            }
        }

        // ==================== 链接 ====================

        private void OpenBilibili(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://space.bilibili.com/1223116001",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ShowToast("打开链接失败：" + ex.Message);
            }
        }

        private void OpenGithub(object sender, RoutedEventArgs e)
        {
            ShowToast("GitHub 主页暂未开放");
        }

        // ==================== 页面切换 ====================

        private void HideAllPages()
        {
            PageHome.Visibility = Visibility.Collapsed;
            PageOptimizePlan.Visibility = Visibility.Collapsed;
            PageOptimizeBasic.Visibility = Visibility.Collapsed;
            PageOptimizeSecurity.Visibility = Visibility.Collapsed;
            PageOptimizePower.Visibility = Visibility.Collapsed;
            PageOptimizePrivacy.Visibility = Visibility.Collapsed;
            PageOptimizeTask.Visibility = Visibility.Collapsed;
            PageSettings.Visibility = Visibility.Collapsed;
            PageAbout.Visibility = Visibility.Collapsed;
        }

        private void ClearSubMenuSelection()
        {
            if (SubOptimizePlan != null) SubOptimizePlan.IsChecked = false;
            if (SubOptimizeBasic != null) SubOptimizeBasic.IsChecked = false;
            if (SubOptimizeSecurity != null) SubOptimizeSecurity.IsChecked = false;
            if (SubOptimizePower != null) SubOptimizePower.IsChecked = false;
            if (SubOptimizePrivacy != null) SubOptimizePrivacy.IsChecked = false;
            if (SubOptimizeTask != null) SubOptimizeTask.IsChecked = false;
        }

        private void Nav_Checked(object sender, RoutedEventArgs e)
        {
            if (PageHome == null) return;
            var radio = (RadioButton)sender;

            // 清除子菜单选中 + 优化高亮
            ClearSubMenuSelection();
            NavHighlight.SetActive(NavOptimize, false);

            // 主菜单高亮只保留当前
            NavHighlight.SetActive(NavHome, false);
            NavHighlight.SetActive(NavSettings, false);
            NavHighlight.SetActive(NavAbout, false);
            NavHighlight.SetActive(radio, true);

            HideAllPages();

            switch (radio.Name)
            {
                case "NavHome":
                    PageHome.Visibility = Visibility.Visible;
                    PlayFadeIn(PageHome);
                    break;
                case "NavSettings":
                    PageSettings.Visibility = Visibility.Visible;
                    PlayFadeIn(PageSettings);
                    break;
                case "NavAbout":
                    PageAbout.Visibility = Visibility.Visible;
                    PlayFadeIn(PageAbout);
                    break;
            }
        }

        private void SubNav_Checked(object sender, RoutedEventArgs e)
        {
            if (PageHome == null) return;

            // ★ 关键：清除所有主菜单的 IsChecked，确保下次点它们能触发 Checked 事件
            if (NavHome != null) NavHome.IsChecked = false;
            if (NavSettings != null) NavSettings.IsChecked = false;
            if (NavAbout != null) NavAbout.IsChecked = false;

            // 高亮切换到"优化"
            NavHighlight.SetActive(NavHome, false);
            NavHighlight.SetActive(NavSettings, false);
            NavHighlight.SetActive(NavAbout, false);
            NavHighlight.SetActive(NavOptimize, true);

            HideAllPages();
            ShowSubPage();
        }

        private void ShowSubPage()
        {
            if (SubOptimizePlan.IsChecked == true)
            {
                PageOptimizePlan.Visibility = Visibility.Visible;
                PlayFadeIn(PageOptimizePlan);
            }
            else if (SubOptimizeBasic.IsChecked == true)
            {
                PageOptimizeBasic.Visibility = Visibility.Visible;
                Dispatcher.BeginInvoke(new Action(PlaySlideInAnimation),
                    DispatcherPriority.Loaded);
            }
            else if (SubOptimizeSecurity.IsChecked == true)
            {
                PageOptimizeSecurity.Visibility = Visibility.Visible;
                PlayFadeIn(PageOptimizeSecurity);
            }
            else if (SubOptimizePower.IsChecked == true)
            {
                PageOptimizePower.Visibility = Visibility.Visible;
                PlayFadeIn(PageOptimizePower);
            }
            else if (SubOptimizePrivacy.IsChecked == true)
            {
                PageOptimizePrivacy.Visibility = Visibility.Visible;
                PlayFadeIn(PageOptimizePrivacy);
            }
            else if (SubOptimizeTask.IsChecked == true)
            {
                PageOptimizeTask.Visibility = Visibility.Visible;
                PlayFadeIn(PageOptimizeTask);
            }
        }
    }
}