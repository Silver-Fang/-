Public Enum 文件类型 As Byte
	PNG = 0
	JPG = 1
	SWF = 2
	所有 = 3
End Enum
Public Class 从文件获取图像
	Const PNG标头长度 As Byte = 16, PNG末尾长度 As Byte = 8, JPG标头长度 As Byte = 8, JPG末尾长度 As Byte = 2, SWF标头长度 As Byte = 3
	Private Shared Function 字节流转字符串(字节流 As Byte()) As String
		Static ChrW委托 As Func(Of Byte, Char) = AddressOf ChrW
		Dim a As IEnumerable(Of Char) = 字节流.Select(ChrW委托), b As Char(), c As New Text.StringBuilder
		Const 枚举最大长度 As UInteger = 536870912
		Do
			b = a.Take(枚举最大长度).ToArray
			a = a.Skip(枚举最大长度)
			c.Append(b)
		Loop While b.Length = 枚举最大长度
		Return c.ToString
	End Function

	Event 找到文件(文件 As IEnumerable(Of Byte), 类型 As 文件类型, 起始字节 As UInteger, 长度 As UInteger)
	Event 进度报告(已检查字节数 As UInteger, 总字节数 As UInteger, 类型 As 文件类型)
	Private Sub 获取PNG(源 As ArraySegment(Of Byte), 字符串 As String, 当前已检查的大小 As Integer)
		Const PNG至少长度 As Byte = 24
		Static PNG标头 As String = 字节流转字符串({&H89, &H50, &H4E, &H47, &HD, &HA, &H1A, &HA, &H0, &H0, &H0, &HD, &H49, &H48, &H44, &H52}), PNG末尾 As String = 字节流转字符串({&H49, &H45, &H4E, &H44, &HAE, &H42, &H60, &H82})
		Dim b As UInteger = 字符串.Length, PNG最后位置 As UInteger = b - PNG至少长度, 开始 As Integer, 结束 As Integer, 文件长度 As UInteger
		While 当前已检查的大小 < PNG最后位置
			RaiseEvent 进度报告(当前已检查的大小, b, 文件类型.PNG)
			开始 = 字符串.IndexOf(PNG标头, 当前已检查的大小, StringComparison.Ordinal)
			If 开始 = -1 Then Exit While
			结束 = 字符串.IndexOf(PNG末尾, 开始 + PNG标头长度, StringComparison.Ordinal)
			If 结束 = -1 Then Exit While
			当前已检查的大小 = 结束 + PNG末尾长度
			文件长度 = 当前已检查的大小 - 开始
			RaiseEvent 找到文件(源.Skip(开始).Take(文件长度), 文件类型.PNG, 开始, 文件长度)
		End While
		RaiseEvent 进度报告(b, b, 文件类型.PNG)
	End Sub
	Private Sub 获取JPG(源 As ArraySegment(Of Byte), 字符串 As String, 当前已检查的大小 As Integer)
		Const JPG至少长度 As Byte = 24
		Static JPG标头 As String = 字节流转字符串({&HFF, &HD8, &HFF, &HE0, &H0, &H10, &H4A, &H46}), JPG末尾 As String = 字节流转字符串({&HFF, &HD9})
		Dim b As UInteger = 字符串.Length, JPG最后位置 As UInteger = b - JPG至少长度, 开始 As Integer, 结束 As Integer, 文件长度 As UInteger
		While 当前已检查的大小 < JPG最后位置
			RaiseEvent 进度报告(当前已检查的大小, b, 文件类型.JPG)
			开始 = 字符串.IndexOf(JPG标头, 当前已检查的大小, StringComparison.Ordinal)
			If 开始 = -1 Then Exit While
			结束 = 字符串.IndexOf(JPG末尾, 开始 + JPG标头长度, StringComparison.Ordinal)
			If 结束 = -1 Then Exit While
			当前已检查的大小 = 结束 + JPG末尾长度
			文件长度 = 当前已检查的大小 - 开始
			RaiseEvent 找到文件(源.Skip(开始).Take(文件长度), 文件类型.JPG, 开始, 文件长度)
		End While
		RaiseEvent 进度报告(b, b, 文件类型.JPG)
	End Sub
	Private Sub 获取SWF(源 As Byte(), 字符串 As String, 当前已检查的大小 As Integer)
		Const SWF至少长度 As Byte = 8
		Dim b As UInteger = 字符串.Length, SWF最后位置 As UInteger = b - SWF至少长度, 开始 As Integer, c As UInteger, 文件长度 As UInteger
		Static SWF标头 As String = 字节流转字符串({&H46, &H57, &H53})
		While 当前已检查的大小 < SWF最后位置
			RaiseEvent 进度报告(当前已检查的大小, b, 文件类型.SWF)
			开始 = 字符串.IndexOf(SWF标头, 当前已检查的大小, StringComparison.Ordinal)
			If 开始 = -1 Then Exit While
			c = 开始 + SWF标头长度
			If 源(c) < 20 Then
				文件长度 = New IO.BinaryReader(New IO.MemoryStream(源.Skip(c + 1).Take(4).ToArray)).ReadUInt32
				If 开始 + 文件长度 > b Then
					当前已检查的大小 = 开始 + 1
				Else
					RaiseEvent 找到文件(源.Skip(开始).Take(文件长度), 文件类型.SWF, 开始, 文件长度)
					当前已检查的大小 = 开始 + 文件长度
				End If
			Else
				当前已检查的大小 = 开始 + 1
			End If
		End While
		RaiseEvent 进度报告(b, b, 文件类型.SWF)
	End Sub
	Function 开始获取_速度优化(源 As Byte(), 提取PNG As Boolean, 提取JPG As Boolean, 提取SWF As Boolean, Optional 起始字节 As UInteger = 0) As Task
		Return Task.Run(Async Function()
							Dim a As String = 字节流转字符串(源), 任务列表 As New List(Of Task)
							If 提取PNG Then
								任务列表.Add(Task.Run(Sub()
													  获取PNG(源, a, 起始字节)
												  End Sub))
							End If
							If 提取JPG Then
								任务列表.Add(Task.Run(Sub()
													  获取JPG(源, a, 起始字节)
												  End Sub))
							End If
							If 提取SWF Then
								任务列表.Add(Task.Run(Sub()
													  获取SWF(源, a, 起始字节)
												  End Sub))
							End If
							For Each b As Task In 任务列表
								Await b
							Next
						End Function)
	End Function
	Function 开始获取_内存优化(源 As List(Of Byte), 提取PNG As Boolean, 提取JPG As Boolean, 提取SWF As Boolean, Optional 起始字节 As UInteger = 0) As Task
		Return Task.Run(Sub() 开始获取_内存优化_内部(源, 提取PNG, 提取JPG, 提取SWF, 起始字节))
	End Function
	Private Sub 开始获取_内存优化_内部(源 As List(Of Byte), 提取PNG As Boolean, 提取JPG As Boolean, 提取SWF As Boolean, 起始字节 As UInteger)
		Dim 启发标头 As New List(Of Byte)
		If 提取PNG Then 启发标头.Add(&H89)
		If 提取JPG Then 启发标头.Add(&HFF)
		If 提取SWF Then 启发标头.Add(&H46)
		Dim 是启发标头 As Predicate(Of Byte) = AddressOf 启发标头.Contains
		Static PNG标头 As Byte() = {&H89, &H50, &H4E, &H47, &HD, &HA, &H1A, &HA, &H0, &H0, &H0, &HD, &H49, &H48, &H44, &H52}, PNG末尾 As Byte() = {&H49, &H45, &H4E, &H44, &HAE, &H42, &H60, &H82}, JPG标头 As Byte() = {&HFF, &HD8, &HFF, &HE0, &H0, &H10, &H4A, &H46}, JPG末尾 As Byte() = {&HFF, &HD9}, SWF标头 As Byte() = {&H46, &H57, &H53}, 是PNG末尾启发 As Predicate(Of Byte) = AddressOf &H49.Equals, 是JPG末尾启发 As Predicate(Of Byte) = AddressOf &HFF.Equals, 标头类型 As New Dictionary(Of Byte, Byte) From {{&H89, 文件类型.PNG}, {&HFF, 文件类型.JPG}, {&H46, 文件类型.SWF}}
		Dim 开始 As UInteger, a As UInteger, b As UInteger = 源.Count, c As UInteger, d As UInteger = b - 7, e As UInteger
		While 起始字节 < d
			RaiseEvent 进度报告(起始字节, b, 文件类型.所有)
			开始 = 源.FindIndex(起始字节, 是启发标头)
			If 开始 = -1 Then Exit While
			Select Case 标头类型(源(开始))
				Case 文件类型.PNG
					If 源.Skip(开始).Take(PNG标头长度).SequenceEqual(PNG标头) Then
						a = 开始 + PNG标头长度
						While a < b
							c = 源.FindIndex(a, 是PNG末尾启发)
							If c = -1 Then Exit While
							If 源.Skip(c).Take(PNG末尾长度).SequenceEqual(PNG末尾) Then
								起始字节 = c + PNG末尾长度
								e = 起始字节 - 开始
								RaiseEvent 找到文件(源.Skip(开始).Take(e), 文件类型.PNG, 开始, e)
								Exit Select
							Else
								a = c + 1
							End If
						End While
					End If
					起始字节 = 开始 + 1
				Case 文件类型.JPG
					If 源.Skip(开始).Take(JPG标头长度).SequenceEqual(JPG标头) Then
						a = 开始 + JPG标头长度
						While a < b
							c = 源.FindIndex(a, 是JPG末尾启发)
							If c = -1 Then Exit While
							If 源.Skip(c).Take(JPG末尾长度).SequenceEqual(JPG末尾) Then
								起始字节 = c + JPG末尾长度
								e = 起始字节 - 开始
								RaiseEvent 找到文件(源.Skip(开始).Take(e), 文件类型.JPG, 开始, e)
								Exit Select
							Else
								a = c + 1
							End If
						End While
					End If
					起始字节 = 开始 + 1
				Case 文件类型.SWF
					If 源.Skip(开始).Take(SWF标头长度).SequenceEqual(SWF标头) Then
						a = 开始 + SWF标头长度
						If 源(a) < 20 Then
							e = New IO.BinaryReader(New IO.MemoryStream(源.Skip(a + 1).Take(4).ToArray)).ReadUInt32
							起始字节 = 开始 + e
							If 起始字节 > b Then
								起始字节 = 开始 + 1
							Else
								RaiseEvent 找到文件(源.Skip(开始).Take(e), 文件类型.SWF, 开始, e)
							End If
						Else
							起始字节 = 开始 + 1
						End If
					Else
						起始字节 = 开始 + 1
					End If
			End Select
		End While
	End Sub
End Class
