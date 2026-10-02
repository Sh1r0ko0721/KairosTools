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
                    NeedRestart = true,   // ★ 需要重启
                    IsOn = memCompOn
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
                        }
                    });

                    string suffix = item.NeedRestart ? "，重启后生效" : "";
                    ShowToast($"{item.Name}（{(isOn ? "启用" : "禁用")}）设置完毕{suffix}");
                };
            }

            DataContext = this;
        }

        /// <summary>底部弹出 Toast 提示</summary>
        private async void ShowToast(string message)
        {
            ToastText.Text = message;
            Toast.Visibility = Visibility.Visible;

            Toast.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)));

            await Task.Delay(2200);

            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
            fadeOut.Completed += (s, e) => Toast.Visibility = Visibility.Collapsed;
            Toast.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        // ==================== 动画 ====================

        /// <summary>基础优化页：标题 + 每项依次滑入</summary>
        private void PlaySlideInAnimation()
        {
            if (BasicItemsControl == null) return;

            // 标题先滑入
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

        /// <summary>单个元素：从左侧滑入 + 淡入</summary>
        private void AnimateSlideIn(FrameworkElement el, int delayMs)
        {
            if (el == null) return;

            // 清掉旧动画，重置到起始状态（不会闪，因为 XAML 里 Opacity=0）
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

        /// <summary>通用淡入（首页、关于、设置等非列表页面）</summary>
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

        private void Nav_Checked(object sender, RoutedEventArgs e)
        {
            if (PageHome == null) return;
            HideAllPages();
            var radio = (RadioButton)sender;

            switch (radio.Name)
            {
                case "NavHome":
                    OptimizeSubMenu.Visibility = Visibility.Collapsed;
                    PageHome.Visibility = Visibility.Visible;
                    PlayFadeIn(PageHome);
                    break;

                case "NavOptimize":
                    OptimizeSubMenu.Visibility = Visibility.Visible;
                    if (SubOptimizePlan.IsChecked != true && SubOptimizeBasic.IsChecked != true &&
                        SubOptimizeSecurity.IsChecked != true && SubOptimizePower.IsChecked != true &&
                        SubOptimizePrivacy.IsChecked != true && SubOptimizeTask.IsChecked != true)
                    {
                        SubOptimizePlan.IsChecked = true;
                    }
                    else ShowSubPage();
                    break;

                case "NavSettings":
                    OptimizeSubMenu.Visibility = Visibility.Collapsed;
                    PageSettings.Visibility = Visibility.Visible;
                    PlayFadeIn(PageSettings);
                    break;

                case "NavAbout":
                    OptimizeSubMenu.Visibility = Visibility.Collapsed;
                    PageAbout.Visibility = Visibility.Visible;
                    PlayFadeIn(PageAbout);
                    break;
            }
        }

        private void SubNav_Checked(object sender, RoutedEventArgs e)
        {
            if (PageHome == null) return;
            ShowSubPage();
        }

        private void ShowSubPage()
        {
            PageOptimizePlan.Visibility = Visibility.Collapsed;
            PageOptimizeBasic.Visibility = Visibility.Collapsed;
            PageOptimizeSecurity.Visibility = Visibility.Collapsed;
            PageOptimizePower.Visibility = Visibility.Collapsed;
            PageOptimizePrivacy.Visibility = Visibility.Collapsed;
            PageOptimizeTask.Visibility = Visibility.Collapsed;

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