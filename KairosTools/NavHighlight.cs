using System.Windows;

namespace KairosTools
{
    /// <summary>
    /// 附加属性：控制侧栏按钮的高亮状态。
    /// 用于把"展开/折叠"和"高亮"两个状态分开控制。
    /// </summary>
    public static class NavHighlight
    {
        public static readonly DependencyProperty ActiveProperty =
            DependencyProperty.RegisterAttached(
                "Active",
                typeof(bool),
                typeof(NavHighlight),
                new PropertyMetadata(false));

        public static bool GetActive(DependencyObject obj)
            => (bool)obj.GetValue(ActiveProperty);

        public static void SetActive(DependencyObject obj, bool value)
            => obj.SetValue(ActiveProperty, value);
    }
}