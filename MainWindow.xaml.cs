using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Path = System.IO.Path;

namespace BakCleaner
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        Dictionary<string, List<MyDataItem>> MainDta = new Dictionary<string, List<MyDataItem>>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Init()
        {
            ProjectsListBox.Items.Clear();
            FilesDataGrid.Items.Clear();
            MainDta = new Dictionary<string, List<MyDataItem>>();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            Init();
            var folder = MainPathText.Text.Trim();
            var pattern = SuffixText.Text.Trim();
            if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(pattern))
            {
                return;
            }

            Thread thread = new Thread(new ThreadStart(delegate { 
                GetData(folder, pattern);
                Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate () { ShowData(); });
                
            }));
            thread.Start();

            

        }

        private void ShowData()
        {
            if (MainDta.Count == 0) 
            {
                MessageBox.Show("文件数据为空！");
                return;
            }
            int maxNum;
            bool isMaxNum = int.TryParse(MaxNumText.Text.Trim(), out maxNum);
            if (!isMaxNum)
            {
                MessageBox.Show("MaxNum设置错误！");
                return;
            }

            // 列表
            foreach (var item in MainDta)
            {
                
                if (item.Value.Count > maxNum)
                {
                    ProjectsListBox.Items.Add(item.Key);
                }
            }
            ProjectsListBox.SelectedIndex = 0;
            var firstName = ProjectsListBox.Items.GetItemAt(0).ToString();
            
            // 表格
            foreach (var item in MainDta[firstName])
            {
                FilesDataGrid.Items.Add(item);
            }
        }

        private void GetData(string folder, string pattern)
        {
            var files = Directory.GetFiles(folder, pattern, SearchOption.AllDirectories);
            foreach (var item in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(item);
                if (!Regex.IsMatch(fileName, @"\-\d{4}\-\d{2}\-\d{2}_\d{4}$"))
                {
                    continue; // 忽略不带时间戳的文件
                }
                var projectName = Regex.Replace(fileName, @"\-\d{4}\-\d{2}\-\d{2}_\d{4}$", "");
                var mTime = File.GetLastWriteTime(item).ToString("d");
                //projectList.Add(projectName);

                // 项目列表
                if (!MainDta.ContainsKey(projectName))
                {
                    MainDta.Add(projectName, new List<MyDataItem>());
                }

                // 详细表格
                var dataItem = new MyDataItem() { FileName = fileName, MTime = mTime, FilePath = item };
                MainDta[projectName].Add(dataItem);
            }
        }

        struct MyDataItem
        {
            public string FileName { get; set; }
            public string MTime { get; set; }
            public string FilePath { get; set; }
        }

        private void ProjectsListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var currentItem = ItemsControl.ContainerFromElement(ProjectsListBox, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (currentItem == null)
            {
                return;
            }

            var currentName = currentItem.Content.ToString();
            FilesDataGrid.Items.Clear();
            sorted = false;

            foreach (var item in MainDta[currentName])
            {
                FilesDataGrid.Items.Add(item);
            }
        }

        private void PathButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.Cancel)
            {
                MainPathText.Text = dialog.SelectedPath.Trim();
            }
        }

        private void DelButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取列表选中项
            var currentName = ProjectsListBox.SelectedItem.ToString();

            // 获取表格选中行
            var items = FilesDataGrid.SelectedItems;
            if (items.Count == 0)
            {
                return;
            }
            var result = MessageBox.Show($"已选择 {items.Count} 个文件，确实要删除吗？", "msg", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No)
            {
                return;
            }

            // 删除文件，和字典的数据
            foreach (var item in items)
            {
                var myItem = (MyDataItem)item;
                var filePath = myItem.FilePath;

                MainDta[currentName].Remove(myItem);

                File.Delete(filePath);
            }

            // 刷新表格
            FilesDataGrid.Items.Clear();
            foreach (var item in MainDta[currentName])
            {
                FilesDataGrid.Items.Add(item);
            }
        }

        private bool sorted = false;

        private void FilesDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            sorted = false;
        }

        private void FilesDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (sorted) return;

            int maxNum;
            bool isMaxNum = int.TryParse(MaxNumText.Text.Trim(), out maxNum);
            if (!isMaxNum)
            {
                MessageBox.Show("MaxNum设置错误！");
                return;
            }

            var count = FilesDataGrid.Items.Count;
            if (count <= maxNum)
            {
                return;
            }
            FilesDataGrid.SelectedItems.Clear();
            for (int i = maxNum; i < count; i++)
            {
                FilesDataGrid.SelectedItems.Add(FilesDataGrid.Items[i]);
            }

            sorted = true;
        }

        private void MainPathText_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (Directory.Exists(files[0]))
            {
                MainPathText.Text = files[0];
            }
        }

        private void MainPathText_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void Rectangle_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void Rectangle_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (Directory.Exists(files[0]))
            {
                MainPathText.Text = files[0];
            }
        }
    }
}
