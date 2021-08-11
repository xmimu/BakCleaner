using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BakCleaner
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<string, List<MyDataItem>> MainDta = new Dictionary<string, List<MyDataItem>>();

        private bool sorted = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Init()
        {
            sorted = false;
            DelButton.IsEnabled = false;
            ProjectsListBox.Items.Clear();
            FilesDataGrid.Items.Clear();
            MainDta = new Dictionary<string, List<MyDataItem>>();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var folder = MainPathText.Text.Trim();
            var pattern = SuffixText.Text.Trim();
            if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(pattern))
            {
                MessageBox.Show("请输入正确的路径");
                return;
            }
            SearchButton.IsEnabled = false;
            Init();
            await Task.Run(() => GetData(folder, pattern));
            ShowData();
            SearchButton.IsEnabled = true;
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
            if (ProjectsListBox.Items.Count == 0)
            {
                MessageBox.Show("没有需要处理的文件！");
                return;
            }
            ProjectsListBox.SelectedIndex = 0;
            var firstName = ProjectsListBox.Items.GetItemAt(0).ToString();

            // 表格
            foreach (var item in MainDta[firstName])
            {
                FilesDataGrid.Items.Add(item);
            }
            DelButton.IsEnabled = true;
        }

        private void GetData(string folder, string pattern)
        {
            string[] files;
            try
            {
                files = Directory.GetFiles(folder, pattern, SearchOption.AllDirectories);
            }
            catch (System.Exception)
            {
                MessageBox.Show("文件夹打开错误，也可能没有访问权限！");
                return;
            }

            foreach (var item in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(item);
                if (!Regex.IsMatch(fileName, @"\-\d{4}\-\d{2}\-\d{2}_\d{4}$"))
                {
                    continue; // 忽略不带时间戳的文件
                }
                var projectName = Regex.Replace(fileName, @"\-\d{4}\-\d{2}\-\d{2}_\d{4}$", "");
                var mTime = File.GetLastWriteTime(item).ToString("d");

                // 项目列表
                if (!MainDta.ContainsKey(projectName))
                {
                    MainDta.Add(projectName, new List<MyDataItem>());
                }

                // 详细表格
                var dataItem = new MyDataItem() { FileName = fileName, MTime = mTime, FilePath = item };
                MainDta[projectName].Add(dataItem);
            }

            // 排序
            Dictionary<string, List<MyDataItem>> copyData = new Dictionary<string, List<MyDataItem>>();
            foreach (var item in MainDta)
            {
                var newValue = item.Value.OrderByDescending(t => t.FileName);
                copyData[item.Key] = newValue.ToList();
            }
            MainDta = copyData;
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
            if (MainDta.Count == 0)
            {
                return;
            }
            // 获取列表选中项
            var currentName = ProjectsListBox.SelectedItem.ToString();

            // 获取表格选中行
            var items = FilesDataGrid.SelectedItems;
            if (items.Count == 0)
            {
                return;
            }
            DelButton.IsEnabled = false;
            var result = MessageBox.Show($"已选择 {items.Count} 个文件，确实要删除吗？", "msg", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No)
            {
                DelButton.IsEnabled = true;
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
            DelButton.IsEnabled = true;
        }

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
