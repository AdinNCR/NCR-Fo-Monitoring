Imports System.Net.NetworkInformation
Imports System.Drawing
Imports System.Threading.Tasks
Imports System.IO

Public Class Form1
    Private WithEvents Timer1 As New Timer()
    Private ipAddresses As New Dictionary(Of String, String)()
    Private tableLayoutPanel As New TableLayoutPanel()
    Private statusStrip As New StatusStrip()
    Private toolStripStatusLabel As New ToolStripStatusLabel()

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Initialize TableLayoutPanel
        tableLayoutPanel.Dock = DockStyle.Fill
        tableLayoutPanel.AutoScroll = True
        tableLayoutPanel.ColumnCount = 15 ' Adjust the number of columns as needed
        tableLayoutPanel.RowCount = 0
        Me.Controls.Add(tableLayoutPanel)

        ' Initialize StatusStrip
        statusStrip.Items.Add(toolStripStatusLabel)
        statusStrip.Dock = DockStyle.Bottom
        Me.Controls.Add(statusStrip)

        Dim iniFilePath As String = Path.Combine(Application.StartupPath, "nodes.ini")

        ' Read the .ini file
        Using reader As New StreamReader(iniFilePath)
            Dim line As String
            While (InlineAssignHelper(line, reader.ReadLine())) IsNot Nothing
                If line.Contains("=") Then
                    Dim parts() As String = line.Split("="c)
                    If parts.Length = 2 Then
                        Dim store As String = parts(0).Trim()
                        Dim ipAddress As String = parts(1).Trim()
                        ipAddresses(store) = ipAddress

                        ' Add a new label for each store
                        Dim label As New Label()
                        label.Text = store
                        label.Size = New Size(30, 20)
                        label.Margin = New Padding(5)
                        label.ForeColor = Color.White
                        tableLayoutPanel.Controls.Add(label)
                    End If
                End If
            End While
        End Using

        ' Initialize and start the Timer
        Timer1.Interval = 60000 ' 60 seconds
        Timer1.Start()

        ' Initial status check
        UpdateStatusAsync()
    End Sub

    Private Async Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        ' Update the status of each IP address every minute
        Await UpdateStatusAsync()
    End Sub

    Private Async Function UpdateStatusAsync() As Task
        ' Check the status of each IP address asynchronously
        Dim tasks As New List(Of Task)()

        For Each store As String In ipAddresses.Keys
            Dim ipAddress As String = ipAddresses(store)
            tasks.Add(Task.Run(Async Function()
                                   Dim status As String = Await GetPingStatusAsync(ipAddress)
                                   UpdateLabelColor(store, status)
                               End Function))
        Next

        Await Task.WhenAll(tasks)

        ' Update the StatusStrip with the last update time
        toolStripStatusLabel.Text = $"Last Updated: {DateTime.Now}"

        ' Refresh the TableLayoutPanel to ensure it is fully updated
        tableLayoutPanel.Refresh()
    End Function

    Private Sub UpdateLabelColor(store As String, status As String)
        For Each control As Control In tableLayoutPanel.Controls
            If TypeOf control Is Label AndAlso control.Text = store Then
                If status = "Online" Then
                    control.BackColor = Color.Green
                ElseIf status = "Offline" Then
                    control.BackColor = Color.Red
                End If
            End If
        Next
    End Sub

    Private Async Function GetPingStatusAsync(ipAddress As String) As Task(Of String)
        Try
            Dim ping As New Ping()
            Dim reply As PingReply = Await ping.SendPingAsync(ipAddress, 1000)
            If reply.Status = IPStatus.Success Then
                Return "Online"
            Else
                Return "Offline"
            End If
        Catch ex As Exception
            Return "Offline"
        End Try
    End Function

    Private Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
        target = value
        Return value
    End Function
End Class
