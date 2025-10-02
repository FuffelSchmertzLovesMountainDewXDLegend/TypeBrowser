using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TypeBrowserWP
{
    public partial class MainWindow : Window
    {
        private string searchEngine = "google.com";
        private Dictionary<TabItem, WebView2> tabWebViews = new Dictionary<TabItem, WebView2>();
        private bool isDarkTheme = true;

        public MainWindow()
        {
            InitializeComponent();

            
            Loaded += (s, e) => NewTabButton_Click(null, null);
        }

        
        private WebView2 GetActiveWebView()
        {
            var activeTab = MainTabControl.SelectedItem as TabItem;
            return activeTab != null && tabWebViews.ContainsKey(activeTab) ? tabWebViews[activeTab] : null;
        }

        
        private void UpdateUrlInSearchBox(string url)
        {
            Dispatcher.Invoke(() =>
            {
                TextSearch.Text = url;
            });
        }

        #region Управление окном
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeRestore();
            }
            else
            {
                DragMove();
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            MaximizeRestore();
        }

        private void MaximizeRestore()
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                MaximizeBtn.Content = "🗖";
            }
            else
            {
                WindowState = WindowState.Maximized;
                MaximizeBtn.Content = "🗗";
            }
        }

        private async void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var storyboard = (Storyboard)FindResource("FadeOut");
            storyboard.Completed += (s, args) => Close();
            storyboard.Begin(this);
        }
        #endregion

        #region Анимации кнопок
        private void ControlButton_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Button)sender).Foreground = Brushes.White;
            ((Button)sender).Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));
        }

        private void ControlButton_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Button)sender).Foreground = new SolidColorBrush(Color.FromArgb(255, 176, 176, 176));
            ((Button)sender).Background = Brushes.Transparent;
        }

        private void CloseButton_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Button)sender).Foreground = Brushes.White;
            ((Button)sender).Background = new SolidColorBrush(Color.FromArgb(255, 232, 17, 35));
        }

        private void CloseButton_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Button)sender).Foreground = new SolidColorBrush(Color.FromArgb(255, 176, 176, 176));
            ((Button)sender).Background = Brushes.Transparent;
        }

        private void NavButton_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Button)sender).Background = new SolidColorBrush(Color.FromArgb(255, 70, 70, 70));
        }

        private void NavButton_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Button)sender).Background = new SolidColorBrush(Color.FromArgb(255, 58, 58, 58));
        }
        #endregion

        #region Управление вкладками (ИСПРАВЛЕНО)
        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source == MainTabControl) 
            {
                var activeTab = MainTabControl.SelectedItem as TabItem;
                if (activeTab != null && tabWebViews.ContainsKey(activeTab))
                {
                    var activeWebView = tabWebViews[activeTab];
                    TabContentControl.Content = activeWebView;

                    if (activeWebView.CoreWebView2 != null && !string.IsNullOrEmpty(activeWebView.CoreWebView2.Source))
                    {
                        TextSearch.Text = activeWebView.CoreWebView2.Source;
                    }
                }
            }
        }

        private async void NewTabButton_Click(object sender, RoutedEventArgs e)
        {
            var newTab = new TabItem();
            newTab.Header = "Новая вкладка";

            var newWebView = new WebView2();
            

            

            MainTabControl.Items.Add(newTab);
            MainTabControl.SelectedItem = newTab;

            tabWebViews[newTab] = newWebView;
            TabContentControl.Content = newWebView; 

            try
            {
                await newWebView.EnsureCoreWebView2Async();
                newWebView.CoreWebView2.Navigate("https://www.google.com");

                
                newWebView.CoreWebView2.NavigationCompleted += (s, args) =>
                {
                    UpdateUrlInSearchBox(newWebView.CoreWebView2.Source);
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания новой вкладки: {ex.Message}");
            }
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainTabControl.Items.Count > 1)
            {
                var button = (Button)sender;
                var tabItem = FindParent<TabItem>(button);
                if (tabItem != null)
                {
                    
                    if (tabWebViews.ContainsKey(tabItem))
                    {
                        var webView = tabWebViews[tabItem];

                        
                        if (MainTabControl.SelectedItem == tabItem)
                        {
                            
                            int currentIndex = MainTabControl.Items.IndexOf(tabItem);
                            int newIndex = currentIndex > 0 ? currentIndex - 1 : 1;

                            if (newIndex < MainTabControl.Items.Count)
                            {
                                MainTabControl.SelectedItem = MainTabControl.Items[newIndex];
                            }
                        }

                        
                        webView.Dispose();
                        tabWebViews.Remove(tabItem);
                    }

                    MainTabControl.Items.Remove(tabItem);
                }
            }
        }
        #endregion

        #region Навигация
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private void TextSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var storyboard = (Storyboard)FindResource("ScaleSearch");
                storyboard.Begin(TextSearch);
                PerformSearch();
                e.Handled = true;
            }
        }

        private async void PerformSearch()
        {
            string input = TextSearch.Text.Trim();
            if (string.IsNullOrEmpty(input)) return;

            var activeWebView = GetActiveWebView();
            if (activeWebView?.CoreWebView2 == null)
            {
                MessageBox.Show("WebView2 не инициализирован для активной вкладки");
                return;
            }

            try
            {
                if (input.StartsWith("http://") || input.StartsWith("https://"))
                {
                    activeWebView.CoreWebView2.Navigate(input);
                }
                else if (input.Contains(".") && !input.Contains(" "))
                {
                    
                    activeWebView.CoreWebView2.Navigate("https://" + input);
                }
                else
                {
                    
                    string searchUrl = searchEngine == "duckduckgo.com"
                        ? $"https://duckduckgo.com/?q={Uri.EscapeDataString(input)}"
                        : $"https://www.google.com/search?q={Uri.EscapeDataString(input)}";

                    activeWebView.CoreWebView2.Navigate(searchUrl);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка навигации: {ex.Message}");
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var activeWebView = GetActiveWebView();
            if (activeWebView?.CoreWebView2?.CanGoBack == true)
            {
                try
                {
                    activeWebView.CoreWebView2.GoBack();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка назад: {ex.Message}");
                }
            }
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            var activeWebView = GetActiveWebView();
            if (activeWebView?.CoreWebView2?.CanGoForward == true)
            {
                try
                {
                    activeWebView.CoreWebView2.GoForward();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка вперёд: {ex.Message}");
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            var activeWebView = GetActiveWebView();
            try
            {
                activeWebView?.CoreWebView2?.Reload();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления: {ex.Message}");
            }
        }
        #endregion

        #region Настройки
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = SettingsPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void CloseSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = Visibility.Collapsed;
        }

        private void Google_Checked(object sender, RoutedEventArgs e)
        {
            searchEngine = "google.com";
        }

        private void Duck_Checked(object sender, RoutedEventArgs e)
        {
            searchEngine = "duckduckgo.com";
        }

        private void DarkTheme_Checked(object sender, RoutedEventArgs e)
        {
            if (!isDarkTheme)
            {
                ApplyDarkTheme();
                isDarkTheme = true;
            }
        }

        private void LightTheme_Checked(object sender, RoutedEventArgs e)
        {
            if (isDarkTheme)
            {
                ApplyLightTheme();
                isDarkTheme = false;
            }
        }

        private void ApplyDarkTheme()
        {
            this.Background = new SolidColorBrush(Color.FromArgb(255, 13, 13, 13));
            TitleBar.Background = new SolidColorBrush(Color.FromArgb(255, 26, 26, 26));
            NavBar.Background = new SolidColorBrush(Color.FromArgb(255, 37, 37, 37));
            SearchBox.Background = new SolidColorBrush(Color.FromArgb(255, 45, 45, 45));
            AppTitle.Foreground = new SolidColorBrush(Color.FromArgb(255, 224, 224, 224));
            TextSearch.Foreground = new SolidColorBrush(Color.FromArgb(255, 232, 232, 232));
        }

        private void ApplyLightTheme()
        {
            this.Background = new SolidColorBrush(Color.FromArgb(255, 245, 245, 245));
            TitleBar.Background = new SolidColorBrush(Color.FromArgb(255, 250, 250, 250));
            NavBar.Background = new SolidColorBrush(Color.FromArgb(255, 240, 240, 240));
            SearchBox.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            AppTitle.Foreground = new SolidColorBrush(Color.FromArgb(255, 51, 51, 51));
            TextSearch.Foreground = new SolidColorBrush(Color.FromArgb(255, 51, 51, 51));
        }
        #endregion

        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null && !(child is T))
                child = VisualTreeHelper.GetParent(child);
            return child as T;
        }
    }
}