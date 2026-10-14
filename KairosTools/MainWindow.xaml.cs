using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace KairosTools
{
    public partial class MainWindow : Window, System.ComponentModel.INotifyPropertyChanged
    {
        public ObservableCollection<BasicItem> BasicItems { get; set; }
        public ObservableCollection<BasicItem> HibernateItems { get; set; }
        public ObservableCollection<PowerPlanItem> PowerPlans { get; set; }
        public AppSettings Settings => AppSettings.Instance;

        private bool _isTipExpanded;
        public bool IsTipExpanded { get => _isTipExpanded; set { _isTipExpanded = value; OnPropertyChanged(nameof(IsTipExpanded)); } }
        private bool _isPresetExpanded;
        public bool IsPresetExpanded { get => _isPresetExpanded; set { _isPresetExpanded = value; OnPropertyChanged(nameof(IsPresetExpanded)); } }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

        public static readonly DependencyProperty IsWeakPcModeProperty =
            DependencyProperty.Register(nameof(IsWeakPcMode), typeof(bool), typeof(MainWindow),
                new PropertyMetadata(false, OnIsWeakPcModeChanged));

        public bool IsWeakPcMode
        {
            get => (bool)GetValue(IsWeakPcModeProperty);
            set => SetValue(IsWeakPcModeProperty, value);
        }

        private static void OnIsWeakPcModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is MainWindow mw)) return;
            bool newValue = (bool)e.NewValue;
            AppSettings.Instance.IsWeakPcMode = newValue;
            AppSettings.Instance.Save();

            if (mw.BasicItems != null) foreach (var item in mw.BasicItems) item.IsWeakPcMode = newValue;
            if (mw.HibernateItems != null) foreach (var item in mw.HibernateItems) item.IsWeakPcMode = newValue;

            if (newValue)
                mw.Dispatcher.BeginInvoke(new Action(() => mw.ClearAllButtonEffects()), DispatcherPriority.Background);
        }

        public MainWindow()
        {
            AppSettings.Instance.Load();
            InitializeComponent();
            IsWeakPcMode = AppSettings.Instance.IsWeakPcMode;

            StateChanged += (s, e) =>
            {
                if (MaxBtn != null) MaxBtn.Content = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
                if (RootGrid != null) RootGrid.Margin = WindowState == WindowState.Maximized ? new Thickness(7) : new Thickness(0);
            };

            BasicItems = new ObservableCollection<BasicItem>
            {
                new BasicItem { Name = "鼠标加速", Description = "移除鼠标指针的加速，保持严格的线性1:1鼠标移动。\n\n“增强指针精度”增加了指针路径与传感器移动速度的依赖性：鼠标移动得越快，指针移动得越远。这破坏了肌肉记忆，使瞄准变得不可预测，因此所有职业选手都保持加速关闭。调整将MouseSpeed=0和两个阈值=0硬性写入HKCU\\Control Panel\\Mouse。在Windows中也可以通过”设置一蓝牙和设备一鼠标”来实现。",
                    RecommendText = "个人建议在任何竞技游戏中禁用；如果主要使用触控板，则保持默认。",
                    Recommended = false, NeedRestart = false,
                    IsOn = MouseHelper.IsMouseAccelerationEnabled(),
                    IsWeakPcMode = AppSettings.Instance.IsWeakPcMode },
                new BasicItem { Name = "SysMain", Description = "禁用 Prefetch、Superfetch。\n\nSysMain 分析启动的应用程序并提前将其数据预热到缓存中。在HDD上这加快了启动速度，但在带 SSD的游戏系统上，服务有时会消耗5-10%的CPU或导致短暂的负载峰值。将其禁用可消除这些峰值。微软建议在实时设备和高延迟场景中禁用 SysMain。",
                    RecommendText = "对于SSD，禁用；在HDD上，在做决定前建议测试游戏加载时间影响。禁用减少写入操作次数，延长固态硬盘寿命并降低CPU使用率。",
                    Recommended = false, NeedRestart = false,
                    IsOn = SystemHelper.IsSysMainEnabled(),
                    IsWeakPcMode = AppSettings.Instance.IsWeakPcMode },
                new BasicItem { Name = "内存压缩", Description = "禁用以减少CPU占用，优化带宽的占用与访问延迟。\n\n内存压缩是用CPU开销和访问延迟换内存容量，代价是 CPU 占用升高、访问变慢、尾延迟和系统抖动变差；冷页且压缩比高时影响较小，热数据多或压缩比低时可能明显拖慢系统。使用Disable-MMAgent -mc可解决此问题。",
                    RecommendText = "在24GB及以上的设备中，建议禁用；如果内存紧张，则保持默认。",
                    Recommended = false, NeedRestart = true,
                    IsOn = SystemHelper.IsMemoryCompressionEnabled(),
                    IsWeakPcMode = AppSettings.Instance.IsWeakPcMode },
                new BasicItem { Name = "HAGS", Description = "启用硬件加速GPU调度(HAGS)。\n\nHAGS将命令队列管理从系统调度程序转移到GPU 的一个模块，从而减轻CPU上的高优先级线程负担，并可能降低渲染延迟。在实际测试中，帧数差异在0到+3之间波动，但该技术对于DLSS-FG和AMD FSR3帧生成是必需的。调整将HwSchMode设置为2(启用)。",
                    RecommendText = "NVIDIA 2000系和AMD 7000系以上启用；在2018年之前的系统上保持默认。\n如果需要开启DLSS帧生成和FSR3帧生成，那么此项目必须启用。",
                    Recommended = true, NeedRestart = true,
                    IsOn = SystemHelper.IsHagsEnabled(),
                    IsWeakPcMode = AppSettings.Instance.IsWeakPcMode },
                new BasicItem { Name = "交付优化", Description = "禁用P2P更新分发给其他PC。\n\nDelivery Optimization 可以通过本地网络和互联网共享下载的补丁，创建额外的出站流。设置 DODownloadMode=0 禁用此共享，仅保留从微软服务器的直接下载。设置不影响更新过程本身，并排除了强制绕过时出现的 0x80d03002 错误。在早期的 Windows 10 中，启用此功能会导致严重的性能问题。",
                    RecommendText = "建议在家中只有1台PC时始终禁用。在使用早期 Windows 10 的PC上始终禁用。",
                    Recommended = false, NeedRestart = false,
                    IsOn = SystemHelper.IsDeliveryOptimizationEnabled(),
                    IsWeakPcMode = AppSettings.Instance.IsWeakPcMode },
            };
            foreach (var item in BasicItems) AttachBasicItemHandler(item);

            HibernateItems = new ObservableCollection<BasicItem>
            {
                new BasicItem { Name = "休眠", Description = "去除休眠模式，从而删除文件 hiberfil.sys。\n\n休眠将RAM内容写入 hiberfil.sys（最多40% RAM），并在下次启动时恢复会话。\n在SSD/NVMe上，收益稍小：开关的加载时间差异为1-2秒，该文件空间可能达到3-32GB。此外，休眠会启用”快速启动”，并在更新后有时会导致驱动程序错误。在电池电量为0%的情况下，笔记本电脑依赖于此功能，因此对于电池较弱的移动设备来说，该功能仍然有用。该解决方案仅影响一个系统标志，随时可以在不重启 BIOS的情况下恢复。",
                    RecommendText = "对于台式机和SSD笔记本电脑，电池充足时明智地放弃；对于远离插座工作的人，保持现有模式。\n释放数十GB空间并去除每次关机时的多余写入操作。在HDD上，系统启动可能会变慢。",
                    Recommended = false, NeedRestart = false,
                    IsOn = PowerPlanHelper.IsHibernateEnabled(),
                    IsWeakPcMode = AppSettings.Instance.IsWeakPcMode }
            };
            foreach (var item in HibernateItems) AttachBasicItemHandler(item);

            PowerPlans = new ObservableCollection<PowerPlanItem>();
            LoadPowerPlans();

            DataContext = this;

            Loaded += (s, e) =>
            {
                NavHighlight.SetActive(NavHome, true);
                RegisterAllLinkButtons();
            };
        }

        private void AttachBasicItemHandler(BasicItem item)
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
                            if (isOn) MouseHelper.EnableMouseAcceleration(); else MouseHelper.DisableMouseAcceleration();
                            break;
                        case "SysMain":
                            if (isOn) SystemHelper.EnableSysMain(); else SystemHelper.DisableSysMain();
                            break;
                        case "内存压缩":
                            if (isOn) SystemHelper.EnableMemoryCompression(); else SystemHelper.DisableMemoryCompression();
                            break;
                        case "HAGS":
                            if (isOn) SystemHelper.EnableHags(); else SystemHelper.DisableHags();
                            break;
                        case "交付优化":
                            if (isOn) SystemHelper.EnableDeliveryOptimization(); else SystemHelper.DisableDeliveryOptimization();
                            break;
                        case "休眠":
                            if (isOn) PowerPlanHelper.EnableHibernate(); else PowerPlanHelper.DisableHibernate();
                            break;
                    }
                });
                string suffix = item.NeedRestart ? "，重启后生效" : "";
                ShowToast($"{item.Name}（{(isOn ? "启用" : "禁用")}）设置完毕{suffix}");
            };
        }

        private void LoadPowerPlans()
        {
            PowerPlans.Clear();
            foreach (var p in PowerPlanHelper.GetPowerPlans())
                PowerPlans.Add(new PowerPlanItem { Guid = p.Guid, Name = p.Name, IsActive = p.IsActive });
        }

        private void ActivatePlan_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn)) return;
            if (!(btn.Tag is string guid)) return;
            Task.Run(() => PowerPlanHelper.ActivatePlan(guid));
            LoadPowerPlans();
            ShowToast("电源计划已切换");
        }

        private void DeletePlan_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn)) return;
            if (!(btn.Tag is string guid)) return;

            var result = MessageBox.Show("确定要删除此电源计划吗？", "确认删除",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            Task.Run(() =>
            {
                bool ok = PowerPlanHelper.DeletePlan(guid);
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    LoadPowerPlans();
                    ShowToast(ok ? "电源计划已删除" : "删除失败（当前活动计划无法删除）");
                }));
            });
        }

        private void ImportKhorvie_Click(object sender, RoutedEventArgs e)
        {
            string powPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Khorvie.pow");
            if (!File.Exists(powPath)) { ShowToast("找不到 Khorvie.pow"); return; }

            Task.Run(() =>
            {
                string newGuid = PowerPlanHelper.ImportPlan(powPath);
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (newGuid != null)
                    {
                        PowerPlanHelper.ActivatePlan(newGuid);
                        LoadPowerPlans();
                        ShowToast("Khorvie 电源计划已导入并启用");
                    }
                    else ShowToast("导入失败");
                }));
            });
        }

        private void RegisterAllLinkButtons()
        {
            foreach (var btn in FindVisualChildren<Button>(this))
            {
                if (btn.Style == null) continue;
                if (!ReferenceEquals(btn.Style, FindResource("LinkButtonStyle"))) continue;
                btn.MouseEnter -= LinkBtn_MouseEnter;
                btn.MouseLeave -= LinkBtn_MouseLeave;
                btn.MouseEnter += LinkBtn_MouseEnter;
                btn.MouseLeave += LinkBtn_MouseLeave;
            }
        }

        private void LinkBtn_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!(sender is Button btn)) return;
            var bd = btn.Template.FindName("bd", btn) as Border;
            if (bd == null) return;
            var st = GetOrCreateTransform(bd);

            if (IsWeakPcMode) { st.ScaleX = 1.05; st.ScaleY = 1.05; return; }

            var duration = TimeSpan.FromMilliseconds(220);
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

            var shadow = new DropShadowEffect { Color = Colors.Black, ShadowDepth = 6, BlurRadius = 0, Opacity = 0, Direction = 270 };
            bd.Effect = shadow;
            shadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, new DoubleAnimation(0, 28, duration) { EasingFunction = ease });
            shadow.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(0, 0.22, duration) { EasingFunction = ease });

            double cur = st.ScaleX;
            st.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(cur, 1.05, duration) { EasingFunction = ease });
            st.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(cur, 1.05, duration) { EasingFunction = ease });
        }

        private void LinkBtn_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!(sender is Button btn)) return;
            var bd = btn.Template.FindName("bd", btn) as Border;
            if (bd == null) return;
            var st = GetOrCreateTransform(bd);

            if (IsWeakPcMode) { bd.Effect = null; st.ScaleX = 1; st.ScaleY = 1; return; }

            var duration = TimeSpan.FromMilliseconds(220);
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

            if (bd.Effect is DropShadowEffect shadow)
            {
                var blurAnim = new DoubleAnimation(shadow.BlurRadius, 0, duration) { EasingFunction = ease };
                var opAnim = new DoubleAnimation(shadow.Opacity, 0, duration) { EasingFunction = ease };
                opAnim.Completed += (s, ev) => { bd.Effect = null; };
                shadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blurAnim);
                shadow.BeginAnimation(DropShadowEffect.OpacityProperty, opAnim);
            }
            double cur = st.ScaleX;
            st.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(cur, 1, duration) { EasingFunction = ease });
            st.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(cur, 1, duration) { EasingFunction = ease });
        }

        private ScaleTransform GetOrCreateTransform(Border bd)
        {
            if (bd.RenderTransform is ScaleTransform st && !st.IsFrozen) return st;
            st = new ScaleTransform(1, 1);
            bd.RenderTransformOrigin = new Point(0.5, 0.5);
            bd.RenderTransform = st;
            return st;
        }

        private void ClearAllButtonEffects()
        {
            foreach (var btn in FindVisualChildren<Button>(this))
            {
                if (btn.Template == null) continue;
                var bd = btn.Template.FindName("bd", btn) as Border;
                if (bd == null) continue;
                bd.Effect = null;
                if (bd.RenderTransform is ScaleTransform st && !st.IsFrozen)
                {
                    st.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    st.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                    st.ScaleX = 1; st.ScaleY = 1;
                }
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) yield return t;
                foreach (var sub in FindVisualChildren<T>(child)) yield return sub;
            }
        }

        private void MinBtn_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void MaxBtn_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

        private async void ShowToast(string message)
        {
            ToastText.Text = message;
            Toast.Visibility = Visibility.Visible;

            if (IsWeakPcMode)
            {
                Toast.Opacity = 1;
                await Task.Delay(2000);
                Toast.Visibility = Visibility.Collapsed;
                Toast.Opacity = 0;
                return;
            }

            // 淡入 250ms
            Toast.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });

            // 显示 2000ms
            await Task.Delay(2000);

            // 淡出 350ms
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(350))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            fadeOut.Completed += (s, e) => Toast.Visibility = Visibility.Collapsed;
            Toast.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private void PlaySlideInAnimation()
        {
            if (BasicItemsControl == null) return;
            if (IsWeakPcMode)
            {
                BasicHeader.Opacity = 1;
                for (int i = 0; i < BasicItemsControl.Items.Count; i++)
                {
                    var c = BasicItemsControl.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                    if (c == null) continue;
                    var root = VisualTreeHelper.GetChild(c, 0) as FrameworkElement;
                    if (root != null) root.Opacity = 1;
                }
                return;
            }
            AnimateSlideIn(BasicHeader, 0);
            for (int i = 0; i < BasicItemsControl.Items.Count; i++)
            {
                var container = BasicItemsControl.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                if (container == null) continue;
                var root = VisualTreeHelper.GetChild(container, 0) as FrameworkElement;
                if (root == null) continue;
                AnimateSlideIn(root, (i + 1) * 70);
            }
        }

        private void PlayPowerSlideInAnimation()
        {
            if (PowerHeader == null || PowerContentPanel == null) return;

            if (IsWeakPcMode)
            {
                PowerHeader.Opacity = 1;
                foreach (var el in FindVisualChildren<FrameworkElement>(PowerContentPanel))
                    el.Opacity = 1;
                return;
            }

            ResetPowerElementsOpacity();

            Dispatcher.BeginInvoke(new Action(() =>
            {
                int delay = 0;
                AnimateSlideIn(PowerHeader, delay); delay += 80;
                AnimateSlideIn(TipTitleBorder, delay); delay += 60;

                if (IsTipExpanded && TipContentSection != null && TipContentSection.Visibility == Visibility.Visible)
                {
                    AnimateSlideIn(TipContentSection, delay);
                    delay += 60;
                }

                if (PowerPlansControl != null)
                {
                    for (int i = 0; i < PowerPlansControl.Items.Count; i++)
                    {
                        var container = PowerPlansControl.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                        if (container == null) continue;
                        var root = VisualTreeHelper.GetChild(container, 0) as FrameworkElement;
                        if (root == null) continue;
                        AnimateSlideIn(root, delay);
                        delay += 50;
                    }
                }

                AnimateSlideIn(SettingsTitleSection, delay); delay += 60;

                // ★ 休眠：先把内部 item 的 Opacity 设为 1（模板里是 0），再把整个 ItemsControl 当整体滑入
                if (HibernateItemsControl != null)
                {
                    for (int i = 0; i < HibernateItemsControl.Items.Count; i++)
                    {
                        var container = HibernateItemsControl.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                        if (container == null) continue;
                        var root = VisualTreeHelper.GetChild(container, 0) as FrameworkElement;
                        if (root != null) root.Opacity = 1;
                    }

                    AnimateSlideIn(HibernateItemsControl, delay);
                    delay += 60;
                }

                AnimateSlideIn(PresetTitleSection, delay); delay += 60;
                AnimateSlideIn(KhorvieRow, delay);
            }), DispatcherPriority.Render);
        }

        private void ResetPowerElementsOpacity()
        {
            if (PowerHeader != null) PowerHeader.Opacity = 0;
            if (TipTitleBorder != null) TipTitleBorder.Opacity = 0;
            if (TipContentSection != null) TipContentSection.Opacity = 0;
            if (SettingsTitleSection != null) SettingsTitleSection.Opacity = 0;
            if (PresetTitleSection != null) PresetTitleSection.Opacity = 0;
            if (KhorvieRow != null) KhorvieRow.Opacity = 0;
            if (HibernateItemsControl != null) HibernateItemsControl.Opacity = 0;

            if (PowerPlansControl != null)
            {
                for (int i = 0; i < PowerPlansControl.Items.Count; i++)
                {
                    var container = PowerPlansControl.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                    if (container == null) continue;
                    var root = VisualTreeHelper.GetChild(container, 0) as FrameworkElement;
                    if (root != null) root.Opacity = 0;
                }
            }
        }

        /// <summary>把电源页所有参与动画的元素 Opacity 重置为 0</summary>

        private void AnimateSlideIn(FrameworkElement el, int delayMs)
        {
            if (el == null) return;
            el.BeginAnimation(UIElement.OpacityProperty, null);
            el.Opacity = 0;
            var tt = new TranslateTransform(-70, 0);
            el.RenderTransform = tt;
            var opAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280))
            { BeginTime = TimeSpan.FromMilliseconds(delayMs), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            el.BeginAnimation(UIElement.OpacityProperty, opAnim);
            var xAnim = new DoubleAnimation(-70, 0, TimeSpan.FromMilliseconds(340))
            { BeginTime = TimeSpan.FromMilliseconds(delayMs), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            tt.BeginAnimation(TranslateTransform.XProperty, xAnim);
        }

        private void PlayFadeIn(FrameworkElement el)
        {
            if (el == null) return;
            if (IsWeakPcMode) { el.Opacity = 1; return; }
            el.BeginAnimation(UIElement.OpacityProperty, null);
            el.Opacity = 0;
            var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(320))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            el.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        private void NavOptimize_Checked(object sender, RoutedEventArgs e)
        { PlaySubMenuAnimation(true); AnimateArrow(OptimizeArrow, 90); }
        private void NavOptimize_Unchecked(object sender, RoutedEventArgs e)
        { PlaySubMenuAnimation(false); AnimateArrow(OptimizeArrow, 0); }

        private void AnimateArrow(TextBlock arrow, double toAngle)
        {
            if (arrow == null) return;
            if (!(arrow.RenderTransform is RotateTransform rot))
            { rot = new RotateTransform(0); arrow.RenderTransform = rot; }
            if (IsWeakPcMode) { rot.BeginAnimation(RotateTransform.AngleProperty, null); rot.Angle = toAngle; return; }
            double currentAngle = rot.Angle;
            rot.BeginAnimation(RotateTransform.AngleProperty, null);
            rot.Angle = currentAngle;
            var anim = new DoubleAnimation(currentAngle, toAngle, TimeSpan.FromMilliseconds(250))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.HoldEnd };
            rot.BeginAnimation(RotateTransform.AngleProperty, anim);
        }

        private void PlaySubMenuAnimation(bool expand)
        {
            if (OptimizeSubMenuBorder == null) return;
            if (IsWeakPcMode)
            {
                if (expand) { OptimizeSubMenuBorder.Visibility = Visibility.Visible; OptimizeSubMenuBorder.Height = double.NaN; }
                else { OptimizeSubMenuBorder.Height = 0; OptimizeSubMenuBorder.Visibility = Visibility.Collapsed; }
                return;
            }
            double currentHeight = OptimizeSubMenuBorder.Height;
            if (double.IsNaN(currentHeight) || currentHeight < 0) currentHeight = OptimizeSubMenuBorder.ActualHeight;
            if (currentHeight < 0) currentHeight = 0;
            OptimizeSubMenuBorder.BeginAnimation(FrameworkElement.HeightProperty, null);
            OptimizeSubMenuBorder.Height = currentHeight;
            if (expand)
            {
                OptimizeSubMenuBorder.Visibility = Visibility.Visible;
                double targetHeight = MeasureSubMenuHeight(currentHeight);
                if (currentHeight >= targetHeight) currentHeight = targetHeight;
                var anim = new DoubleAnimation(currentHeight, targetHeight, TimeSpan.FromMilliseconds(300))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.HoldEnd };
                OptimizeSubMenuBorder.BeginAnimation(FrameworkElement.HeightProperty, anim);
                PlaySubItemsStaggered();
            }
            else
            {
                var anim = new DoubleAnimation(currentHeight, 0, TimeSpan.FromMilliseconds(260))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.HoldEnd };
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
            var subItems = new FrameworkElement[] { SubOptimizePlan, SubOptimizeBasic, SubOptimizeSecurity, SubOptimizePower, SubOptimizePrivacy, SubOptimizeTask };
            for (int i = 0; i < subItems.Length; i++)
            {
                var el = subItems[i];
                if (el == null) continue;
                if (IsWeakPcMode) { el.Opacity = 1; el.RenderTransform = new TranslateTransform(0, 0); continue; }
                el.BeginAnimation(UIElement.OpacityProperty, null);
                el.Opacity = 0;
                if (!(el.RenderTransform is TranslateTransform)) el.RenderTransform = new TranslateTransform(0, -8);
                var tt = el.RenderTransform as TranslateTransform;
                tt.X = 0; tt.Y = -8;
                int delay = i * 35;
                var opAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220))
                { BeginTime = TimeSpan.FromMilliseconds(delay), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.HoldEnd };
                el.BeginAnimation(UIElement.OpacityProperty, opAnim);
                var yAnim = new DoubleAnimation(-8, 0, TimeSpan.FromMilliseconds(260))
                { BeginTime = TimeSpan.FromMilliseconds(delay), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.HoldEnd };
                tt.BeginAnimation(TranslateTransform.YProperty, yAnim);
            }
        }

        private void OpenBilibili(object sender, RoutedEventArgs e)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://space.bilibili.com/1223116001", UseShellExecute = true }); }
            catch (Exception ex) { ShowToast("打开链接失败：" + ex.Message); }
        }
        private void OpenGithub(object sender, RoutedEventArgs e)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://github.com/Sh1r0ko0721", UseShellExecute = true }); }
            catch (Exception ex) { ShowToast("打开链接失败：" + ex.Message); }
        }

        private void HideAllPages()
        {
            // ★ 先清理所有"参与动画"元素的 Opacity 动画覆盖
            // 让它们的 Opacity 回到 XAML 里设定的基础值（0），下次显示时就是透明的
            ResetOpacityAnimations(PageHome);
            ResetOpacityAnimations(PageOptimizePlan);
            ResetOpacityAnimations(PageOptimizeBasic);
            ResetOpacityAnimations(PageOptimizeSecurity);
            ResetOpacityAnimations(PageOptimizePower);
            ResetOpacityAnimations(PageOptimizePrivacy);
            ResetOpacityAnimations(PageOptimizeTask);
            ResetOpacityAnimations(PageSettings);
            ResetOpacityAnimations(PageAbout);

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

        /// <summary>递归清掉指定元素及其所有子元素的 Opacity 动画覆盖，让它们回到基础值</summary>
        private void ResetOpacityAnimations(DependencyObject parent)
        {
            if (parent == null) return;

            if (parent is UIElement ui)
            {
                ui.BeginAnimation(UIElement.OpacityProperty, null);
            }

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                ResetOpacityAnimations(child);
            }
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
            ClearSubMenuSelection();
            NavHighlight.SetActive(NavOptimize, false);
            NavHighlight.SetActive(NavHome, false);
            NavHighlight.SetActive(NavSettings, false);
            NavHighlight.SetActive(NavAbout, false);
            NavHighlight.SetActive(radio, true);
            HideAllPages();
            switch (radio.Name)
            {
                case "NavHome": PageHome.Visibility = Visibility.Visible; PlayFadeIn(PageHome); break;
                case "NavSettings": PageSettings.Visibility = Visibility.Visible; PlayFadeIn(PageSettings); break;
                case "NavAbout": PageAbout.Visibility = Visibility.Visible; PlayFadeIn(PageAbout); RegisterAllLinkButtons(); break;
            }
        }

        private void SubNav_Checked(object sender, RoutedEventArgs e)
        {
            if (PageHome == null) return;
            if (NavHome != null) NavHome.IsChecked = false;
            if (NavSettings != null) NavSettings.IsChecked = false;
            if (NavAbout != null) NavAbout.IsChecked = false;
            NavHighlight.SetActive(NavHome, false);
            NavHighlight.SetActive(NavSettings, false);
            NavHighlight.SetActive(NavAbout, false);
            NavHighlight.SetActive(NavOptimize, true);
            HideAllPages();
            ShowSubPage();
        }

        private void ShowSubPage()
        {
            if (SubOptimizePlan.IsChecked == true) { PageOptimizePlan.Visibility = Visibility.Visible; PlayFadeIn(PageOptimizePlan); }
            else if (SubOptimizeBasic.IsChecked == true)
            { PageOptimizeBasic.Visibility = Visibility.Visible; Dispatcher.BeginInvoke(new Action(PlaySlideInAnimation), DispatcherPriority.Loaded); }
            else if (SubOptimizeSecurity.IsChecked == true) { PageOptimizeSecurity.Visibility = Visibility.Visible; PlayFadeIn(PageOptimizeSecurity); }
            else if (SubOptimizePower.IsChecked == true)
            {
                LoadPowerPlans();
                PageOptimizePower.Visibility = Visibility.Visible;
                // ★ 先让布局完成（生成 ContentPresenter），再播放动画
                Dispatcher.BeginInvoke(new Action(PlayPowerSlideInAnimation), DispatcherPriority.Loaded);
            }
            else if (SubOptimizePrivacy.IsChecked == true) { PageOptimizePrivacy.Visibility = Visibility.Visible; PlayFadeIn(PageOptimizePrivacy); }
            else if (SubOptimizeTask.IsChecked == true) { PageOptimizeTask.Visibility = Visibility.Visible; PlayFadeIn(PageOptimizeTask); }
        }
    }
}