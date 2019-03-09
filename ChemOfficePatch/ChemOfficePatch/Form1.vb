Public Class Form1
    Private Declare Function ExtractIcon Lib "shell32" Alias "ExtractIconA" (ByVal str1 As String, ByVal str2 As String, ByVal int As Integer) As IntPtr
    Private Declare Function SendMessage Lib "user32" Alias "SendMessageA" (ByVal hWnd As IntPtr, ByVal Msg As Int32, ByVal wParam As Int32, ByVal lParam As Int32) As Int32
    Private Const BCM_SETSHIELD = &H160C
    Private regname As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey("dllfile\DefaultIcon")
    Private path(6) As String, pos(6) As Integer, invokeAdmin As Boolean, tFont As Font, tIcon As Bitmap

    Private Sub UpdateList(Optional ByVal OsBit As Integer = 96, Optional ByVal preRun As Boolean = False)
        If OsBit <> 64 Then TextBox1.Text = TextBox1.Text.Replace("/", "\").TrimEnd("\")
        If OsBit <> 32 Then TextBox2.Text = TextBox2.Text.Replace("/", "\").TrimEnd("\")
        For i = If(OsBit <> 64, 0, 5) To If(OsBit <> 32, 6, 4)
            path(i) = IO.Path.Combine(If(i < 5, TextBox1.Text, TextBox2.Text), ListView1.Items(i).SubItems(1).Text)
            Dim preCheck As Boolean = ListView1.Items(i).Checked
            ListView1.Items(i).Checked = False
            ListView1.Items(i).ForeColor = SystemColors.GrayText
            ListView1.Items(i).BackColor = SystemColors.Window
            ListView1.Items(i).SubItems(3).Text = Hex(pos(i)).PadLeft(6, "0")
            If IO.File.Exists(path(i)) Then
                ListView1.Items(i).SubItems(2).Text = "未知类型"
                If i <> 4 AndAlso i <> 6 Then
                    Dim hIcon = ExtractIcon("", If((i = 1 Or i = 5) And ComboBox1.SelectedIndex, path(i).Substring(0, path(i).Length - 8) & ".exe", path(i)), 0)
                    If hIcon = 0 Then
                        ListView1.Items(i).Checked = False
                        hIcon = ExtractIcon("", "shell32.dll", 0)
                        If hIcon = 0 Then hIcon = Me.Icon.Handle
                    Else
                        CheckPatch(i, 0, preCheck Or (Not preRun))
                    End If
                    ImageList1.Images.Add(Icon.FromHandle(hIcon))
                    ListView1.Items(i).ImageIndex = ImageList1.Images.Count - 1
                Else
                    ListView1.Items(i).ImageIndex = 1
                    CheckPatch(i, 0, preCheck Or (Not preRun))
                End If
            Else
                ListView1.Items(i).ImageIndex = 0
                ListView1.Items(i).SubItems(2).Text = "不存在"
            End If
        Next
        Try
            If IO.Directory.Exists(TextBox1.Text) AndAlso IO.Directory.Exists(TextBox2.Text) Then
                Dim f1 As New IO.FileStream(IO.Path.Combine(TextBox1.Text, "TestAdmin"), IO.FileMode.Create, IO.FileAccess.Write) : f1.Close()
                Dim f2 As New IO.FileStream(IO.Path.Combine(TextBox2.Text, "TestAdmin"), IO.FileMode.Create, IO.FileAccess.Write) : f2.Close()
                IO.File.Delete(IO.Path.Combine(TextBox1.Text, "TestAdmin"))
                IO.File.Delete(IO.Path.Combine(TextBox2.Text, "TestAdmin"))
            Else
                MsgBox("警告：指定目录不存在。", MsgBoxStyle.Exclamation)
            End If
            SendMessage(Button3.Handle, BCM_SETSHIELD, 0, 0)
            invokeAdmin = False
        Catch ex As UnauthorizedAccessException
            SendMessage(Button3.Handle, BCM_SETSHIELD, 0, -1)
            invokeAdmin = True
        Catch ex2 As Exception
            MsgBox("错误：无法读写指定文件。" & vbCrLf & "请关闭所有ChemOffice相关程序后再运行破解/还原。", MsgBoxStyle.Critical)
        End Try
    End Sub

    Private Sub CheckPatch(ByVal i As Integer, ByVal type As Integer, Optional ByVal setChecked As Boolean = True)
        Dim iostrm = New IO.FileStream(path(i), IO.FileMode.Open, IO.FileAccess.Read)
        iostrm.Position = pos(i)
        Select Case iostrm.ReadByte
            Case &H19
                If ComboBox1.SelectedIndex Then GoTo origin
            Case &H16
                If ComboBox1.SelectedIndex Then GoTo cracked
            Case &H6
origin:
                ListView1.Items(i).ForeColor = SystemColors.WindowText
                ListView1.Items(i).BackColor = Color.LavenderBlush
                ListView1.Items(i).SubItems(2).Text = "原始文件"
                ListView1.Items(i).Checked = setChecked
            Case &H17
cracked:
                ListView1.Items(i).ForeColor = SystemColors.WindowText
                ListView1.Items(i).BackColor = Color.MintCream
                ListView1.Items(i).SubItems(2).Text = "破解文件"
                ListView1.Items(i).Checked = setChecked
        End Select
        iostrm.Close()
    End Sub

    Private Sub Patch(Optional ByVal restore As Boolean = False)
        Dim args = My.Application.CommandLineArgs
        Dim patchArr As New ArrayList, path1, path2 As String, newVer As Boolean
        Try
            path1 = args(1).Trim("""")
            path2 = args(2).Trim("""")
            newVer = CInt(args(3).Substring(0, 1))

            pos = If(newVer, {&H4C5BD1, &H9C804D, &HDCEA5, &H989A9, &HD96FF, &HBB5A10, &HFEC91}, {&H56705B, &H6F1003, &HB5837, &H1A2A37, &HC1AE7, 0, 0})
            ListView1.Items(1).SubItems(1).Text = If(newVer, "ChemDraw\ChemDrawBase.dll", "ChemDraw\ChemDraw.exe")

            For i = 1 To 7
                If args(3).Substring(i, 1) = "1" Then patchArr.Add(i - 1)
            Next
        Catch ex As Exception
            MsgBox("错误: 传入参数错误。", MsgBoxStyle.Critical) : End
        End Try
        For Each i In patchArr
            path(i) = IO.Path.Combine(If(i < 5, path1, path2), ListView1.Items(i).SubItems(1).Text)
            Try
                Dim iostrm = New IO.FileStream(path(i), IO.FileMode.Open, IO.FileAccess.ReadWrite)
                iostrm.Position = pos(i)
                If (i = 4 OrElse i = 6) AndAlso newVer Then
                    iostrm.WriteByte(If(restore, &H19, &H16))
                    iostrm.Seek(&H44, IO.SeekOrigin.Current)
                    iostrm.WriteByte(If(restore, &H19, &H16))
                    iostrm.Seek(&H7B, IO.SeekOrigin.Current)
                    iostrm.WriteByte(If(restore, &H19, &H16))
                Else
                    iostrm.WriteByte(If(restore, &H6, &H17))
                End If
                iostrm.Close()
            Catch ex As UnauthorizedAccessException
                MsgBox("错误：无法读写指定文件：" & path(i) & vbCrLf & "可能当前用户不具备管理员权限。", MsgBoxStyle.Critical)
            Catch ex2 As Exception
                MsgBox("错误：无法读写指定文件：" & path(i) & vbCrLf & "请关闭所有ChemOffice相关程序后再运行破解/还原。", MsgBoxStyle.Critical)
            End Try
        Next

        End
    End Sub

    Private Sub Form1_HelpButtonClicked(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles Me.HelpButtonClicked
        AboutBox1.ShowDialog()
        e.Cancel = True
    End Sub


    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            Select Case My.Application.CommandLineArgs(0)
                Case "patch" : patch()
                Case "restore" : patch(True)
                Case Else : MsgBox("错误: 未知传入参数。", MsgBoxStyle.Critical) : End
            End Select
        Catch ex As Exception
            Dim hIcon As Integer
            Try
                Dim r As String() = regname.GetValue("", "Null").ToString.Split(",")
                hIcon = ExtractIcon("", r(0), CInt(r(1)))
            Catch ex2 As Exception
                hIcon = 0
            End Try
            If hIcon = 0 Then hIcon = ExtractIcon("", "shell32.dll", 0)
            If hIcon = 0 Then hIcon = Me.Icon.Handle
            ImageList1.Images.Add(Icon.FromHandle(hIcon))

            ComboBox1.SelectedIndex = If(IO.Directory.Exists("C:\Program Files\PerkinElmerInformatics\ChemOffice2017"), 0, 1)
        End Try

        tFont = New Font("微软雅黑", 10.5!, FontStyle.Bold)
        tIcon = New Bitmap(Me.Icon.ToBitmap, New Size(16, 16))
        ToolStripMenuItem1.Image = tIcon
    End Sub

    Private Sub TextBox_TextChanged(sender As Object, e As EventArgs) Handles TextBox1.TextChanged, TextBox2.TextChanged
        If sender Is TextBox1 Then FolderBrowserDialog1.SelectedPath = TextBox1.Text
        If sender Is TextBox2 Then FolderBrowserDialog2.SelectedPath = TextBox2.Text
    End Sub

    Private Sub Button_Click(sender As Object, e As EventArgs) Handles Button1.Click, Button2.Click
        If sender Is Button1 Then
            If FolderBrowserDialog1.ShowDialog() = Windows.Forms.DialogResult.OK Then TextBox1.Text = FolderBrowserDialog1.SelectedPath : TextBox1.SelectAll() : TextBox1.Focus() : UpdateList(32)
        ElseIf sender Is Button2 Then
            If FolderBrowserDialog2.ShowDialog() = Windows.Forms.DialogResult.OK Then TextBox2.Text = FolderBrowserDialog2.SelectedPath : TextBox2.SelectAll() : TextBox2.Focus() : UpdateList(64)
        End If
    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox1.SelectedIndexChanged
        If ComboBox1.SelectedIndex = -1 Then Exit Sub
        Dim sel As Boolean = (TextBox1.Text <> "")
        TextBox1.Text = "C:\Program Files (x86)\PerkinElmerInformatics\ChemOffice201" & If(ComboBox1.SelectedIndex, "8", "7")
        FolderBrowserDialog1.SelectedPath = TextBox1.Text
        If sel Then TextBox1.SelectAll() : TextBox1.Focus()
        TextBox2.Text = "C:\Program Files\PerkinElmerInformatics\ChemOffice201" & If(ComboBox1.SelectedIndex, "8", "7")
        FolderBrowserDialog2.SelectedPath = TextBox2.Text

        pos = If(ComboBox1.SelectedIndex, {&H4C5BD1, &H9C804D, &HDCEA5, &H989A9, &HD96FF, &HBB5A10, &HFEC91}, {&H56705B, &H6F1003, &HB5837, &H1A2A37, &HC1AE7, 0, 0})
        ListView1.Items(1).SubItems(1).Text = If(ComboBox1.SelectedIndex, "ChemDraw\ChemDrawBase.dll", "ChemDraw\ChemDraw.exe")
        ListView1.Items(5).SubItems(1).Text = If(ComboBox1.SelectedIndex, "ChemDraw\ChemDrawBase.dll", "N/A")
        ListView1.Items(6).SubItems(1).Text = If(ComboBox1.SelectedIndex, "ChemDraw for Excel\CambridgeSoft.ChemOffice.ChemDrawExcel.LibChemDrawWrapper.dll", "N/A")

        UpdateList()
    End Sub

    Private Sub CrackRestore_Click(sender As Object, e As EventArgs) Handles Button3.Click, Button4.Click
        UpdateList(, True)
        Dim patchArr As String = ComboBox1.SelectedIndex
        For Each i In ListView1.Items
            patchArr += If(i.Checked, "1", "0")
        Next
        If patchArr.Substring(1) = "0000000" Then MsgBox("警告：请至少选取一个需要破解/还原的文件。", MsgBoxStyle.Exclamation) : Exit Sub
        If MsgBox("提示：是否开始运行破解/还原？" & vbCrLf & "请检查安装路径及程序列表。", MsgBoxStyle.Question Or MsgBoxStyle.YesNo) = MsgBoxResult.No Then Exit Sub
        Me.UseWaitCursor = True
        ProgressBar1.Visible = True
        ListView1.Enabled = False
        Application.DoEvents()
        Dim strPatch As String = If(sender Is Button4, "restore", "patch")
        Dim patchProc = New ProcessStartInfo With {.UseShellExecute = True, .Verb = If(invokeAdmin, "runas", "open"), .WindowStyle = ProcessWindowStyle.Minimized, _
                                                   .FileName = System.Windows.Forms.Application.ExecutablePath, .Arguments = Join({strPatch, _
                                                   """" & TextBox1.Text & """", """" & TextBox2.Text & """", patchArr}, " ")}
        Try
            Process.Start(patchProc)
        Catch ex As Exception
            MsgBox("警告：未运行破解/还原程序。", MsgBoxStyle.Exclamation)
            GoTo Final
        End Try
        MsgBox("提示：运行破解/还原完成。", MsgBoxStyle.Information)
        UpdateList(, True)
Final:
        Me.UseWaitCursor = False
        ProgressBar1.Visible = False
        ListView1.Enabled = True
    End Sub

    Private Sub ListView1_ColumnClick(sender As Object, e As ColumnClickEventArgs) Handles ListView1.ColumnClick
        If e.Column = 0 Then
            Dim checkedNum As Integer
            For Each i In ListView1.Items
                If i.Checked Then checkedNum += 1
            Next
            For Each i In ListView1.Items
                i.Checked = (checkedNum = 0)
            Next
        Else
            UpdateList(, True)
        End If
    End Sub

    Private Sub ListView1_ItemChecked(sender As Object, e As ItemCheckedEventArgs) Handles ListView1.ItemChecked
        If e.Item.SubItems(2).Text = "不存在" OrElse e.Item.SubItems(2).Text = "未知类型" Then e.Item.Checked = False
    End Sub

    Private Sub ToolTip1_Draw(ByVal sender As Object, ByVal e As System.Windows.Forms.DrawToolTipEventArgs) Handles ToolTip1.Draw
        Dim Lable As String = e.ToolTipText
        Dim rc = e.Bounds.Location
        rc.X += 10 : rc.Y += 10

        e.DrawBackground()
        e.DrawBorder()
        e.Graphics.DrawImage(tIcon, rc.X + 2, rc.Y + 2)
        e.Graphics.DrawString(ToolTip1.ToolTipTitle, tFont, Brushes.Black, rc.X + 24, rc.Y)
        e.Graphics.DrawString(Lable, Me.Font, Brushes.Black, rc.X, rc.Y + 24)
    End Sub

    Private Sub ToolTip1_Popup(ByVal sender As Object, ByVal e As System.Windows.Forms.PopupEventArgs) Handles ToolTip1.Popup
        Dim rc = TextRenderer.MeasureText(ToolTip1.GetToolTip(e.AssociatedControl), Me.Font)
        rc.Width += 20 : rc.Height += 44
        e.ToolTipSize = rc
    End Sub

    Private Sub ToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem1.Click
        UpdateList(If(ContextMenuStrip1.SourceControl Is TextBox1, 32, 64))
    End Sub

    Private Sub ToolStripMenuItem2_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem2.Click
        If ContextMenuStrip1.SourceControl Is TextBox1 Then Button1.PerformClick() Else Button2.PerformClick()
    End Sub
End Class
