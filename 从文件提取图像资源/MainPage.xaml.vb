Imports Windows.Storage
''' <summary>
''' 可用于自身或导航至 Frame 内部的空白页。
''' </summary>
Public NotInheritable Class MainPage
    Inherits Page

	ReadOnly 漫游设置 As IPropertySet = ApplicationData.Current.RoamingSettings.Values, 最近使用的列表 As AccessCache.StorageItemMostRecentlyUsedList = AccessCache.StorageApplicationPermissions.MostRecentlyUsedList, 文件选取器 As New Pickers.FileOpenPicker, 目录选取器 As New Pickers.FolderPicker
	WithEvents 获取器 As New 从文件获取图像
	Private 输入文件 As StorageFile, 输出目录 As StorageFolder

	Private Sub JPG_Checked(sender As Object, e As RoutedEventArgs) Handles JPG.Checked, JPG.Unchecked
		漫游设置("JPG") = JPG.IsChecked
	End Sub
	Private Sub PNG_Checked(sender As Object, e As RoutedEventArgs) Handles PNG.Checked, PNG.Unchecked
		漫游设置("PNG") = PNG.IsChecked
	End Sub
	Private Sub SWF_Checked(sender As Object, e As RoutedEventArgs) Handles SWF.Checked, SWF.Unchecked
		漫游设置("SWF") = SWF.IsChecked
	End Sub
	Private Async Sub 异步初始化()
		If 最近使用的列表.ContainsItem("输入文件") Then
			输入文件 = Await 最近使用的列表.GetFileAsync("输入文件")
			If 输入文件 IsNot Nothing Then 输入路径.Text = 输入文件.Path
		End If
		If 最近使用的列表.ContainsItem("输出目录") Then
			输出目录 = Await 最近使用的列表.GetFolderAsync("输出目录")
			If 输出目录 IsNot Nothing Then 输出路径.Text = 输出目录.Path
		End If
	End Sub
	Sub New()

		' 此调用是设计器所必需的。
		InitializeComponent()

		' 在 InitializeComponent() 调用之后添加任何初始化。

		If 漫游设置.ContainsKey("JPG") Then JPG.IsChecked = 漫游设置("JPG")
		If 漫游设置.ContainsKey("PNG") Then PNG.IsChecked = 漫游设置("PNG")
		If 漫游设置.ContainsKey("SWF") Then SWF.IsChecked = 漫游设置("SWF")
		If 漫游设置.ContainsKey("优化") Then
			Select Case 漫游设置("优化")
				Case 优化.内存
					内存优化.IsChecked = True
				Case 优化.速度
					速度优化.IsChecked = True
			End Select
		End If
		文件选取器.FileTypeFilter.Add("*")
		目录选取器.FileTypeFilter.Add("*")
		If 漫游设置.ContainsKey("起始字节") Then 起始字节.Text = 漫游设置("起始字节")
		异步初始化()
	End Sub
	Enum 优化 As Byte
		速度
		内存
	End Enum
	Private Sub 内存优化_Checked(sender As Object, e As RoutedEventArgs) Handles 内存优化.Checked
		漫游设置("优化") = CByte(优化.内存)
	End Sub

	Private Sub 速度优化_Checked(sender As Object, e As RoutedEventArgs) Handles 速度优化.Checked
		漫游设置("优化") = CByte(优化.速度)
	End Sub

	Private Async Sub 输入浏览_Click(sender As Object, e As RoutedEventArgs) Handles 输入浏览.Click
		输入文件 = Await 文件选取器.PickSingleFileAsync
		If 输入文件 IsNot Nothing Then
			最近使用的列表.AddOrReplace("输入文件", 输入文件)
			输入路径.Text = 输入文件.Path
		End If
	End Sub

	Private Async Sub 输出浏览_Click(sender As Object, e As RoutedEventArgs) Handles 输出浏览.Click
		输出目录 = Await 目录选取器.PickSingleFolderAsync
		If 输出目录 IsNot Nothing Then
			最近使用的列表.AddOrReplace("输出目录", 输出目录)
			输出路径.Text = 输出目录.Path
		End If
	End Sub

	ReadOnly 进度(3) As Integer
	Private Async Sub 开始搜索_Click(sender As Object, e As RoutedEventArgs) Handles 开始搜索.Click
		If 输入文件 Is Nothing Then
			提示信息.Text = "未指定输入文件"
			浮出窗口.ShowAt(sender)
			Exit Sub
		End If
		If 输出目录 Is Nothing Then
			提示信息.Text = "未指定输出目录"
			浮出窗口.ShowAt(sender)
			Exit Sub
		End If
		If 内存优化.IsChecked = False AndAlso 速度优化.IsChecked = False Then
			提示信息.Text = "未指定优化策略"
			浮出窗口.ShowAt(sender)
			Exit Sub
		End If
		If PNG.IsChecked = False AndAlso JPG.IsChecked = False AndAlso SWF.IsChecked = False Then
			提示信息.Text = "未指定待查类型"
			浮出窗口.ShowAt(sender)
			Exit Sub
		End If
		Dim b As UInteger
		Try
			b = 起始字节.Text
		Catch ex As Exception
			提示信息.Text = "无效的起始字节"
			浮出窗口.ShowAt(sender)
			Exit Sub
		End Try
		Dim a As Stream = Await 输入文件.OpenStreamForReadAsync, f As UInteger = a.Length - b, c(f - 1) As Byte
		a.Position = b
		Dim d As Task = a.ReadAsync(c, 0, f)
		漫游设置("起始字节") = b
		开始搜索.IsEnabled = False
		内存优化.IsEnabled = False
		速度优化.IsEnabled = False
		输入浏览.IsEnabled = False
		输出浏览.IsEnabled = False
		PNG.IsEnabled = False
		JPG.IsEnabled = False
		SWF.IsEnabled = False
		进度.Initialize()
		If 内存优化.IsChecked Then
			进度条.Maximum = f
			进度条.Value = 0
			Await d
			Await 获取器.开始获取_内存优化(c.ToList, PNG.IsChecked, JPG.IsChecked, SWF.IsChecked, b)
		End If
		If 速度优化.IsChecked Then
			进度条.Maximum = f * (If(PNG.IsChecked, 1, 0) + If(JPG.IsChecked, 1, 0) + If(SWF.IsChecked, 1, 0))
			进度条.Value = 0
			Await d
			Await 获取器.开始获取_速度优化(c, PNG.IsChecked, JPG.IsChecked, SWF.IsChecked, b)
		End If
		开始搜索.IsEnabled = True
		内存优化.IsEnabled = True
		速度优化.IsEnabled = True
		输入浏览.IsEnabled = True
		输出浏览.IsEnabled = True
		PNG.IsEnabled = True
		JPG.IsEnabled = True
		SWF.IsEnabled = True
	End Sub

	Private Sub 获取器_进度报告(已检查字节数 As UInteger, 总字节数 As UInteger, 类型 As 文件类型) Handles 获取器.进度报告
		进度(类型) = 已检查字节数
		Call Dispatcher.TryRunIdleAsync(Sub() 进度条.Value = 进度.Sum())
	End Sub

	Private Async Sub 获取器_找到文件(文件 As IEnumerable(Of Byte), 类型 As 文件类型, 起始字节 As UInteger, 长度 As UInteger) Handles 获取器.找到文件
		Dim a As String = ""
		Select Case 类型
			Case 文件类型.PNG
				a = ".png"
			Case 文件类型.JPG
				a = ".jpg"
			Case 文件类型.SWF
				a = ".swf"
		End Select
		Dim b As Stream = Await (Await 输出目录.CreateFileAsync(起始字节 & a, CreationCollisionOption.ReplaceExisting)).OpenStreamForWriteAsync
		b.Write(文件.ToArray, 0, 长度)
		b.Close()
	End Sub
End Class
