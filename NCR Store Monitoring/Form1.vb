Imports System.Net.NetworkInformation
Imports System.Drawing
Imports System.Threading.Tasks
Imports System.IO
Imports System.Windows.Forms
Imports System.Net.Http

Public Class Form1
    Private WithEvents Timer1 As New Timer()
    Private ipAddresses As New Dictionary(Of String, (String, String))()
    Private tableLayoutPanel As New TableLayoutPanel()
    Private statusStrip As New StatusStrip()
    Private toolStripStatusLabel As New ToolStripStatusLabel()
    Private toolTip As New ToolTip()
    Private iniFileUrl As String = "https://raw.githubusercontent.com/AdinNCR/NCR-Fo-Monitoring/refs/heads/master/NCR%20Store%20Monitoring/nodes.ini"
    Private vpnServerIp As String = "10.10.0.147"
    Private countdownTime As Integer = 120
    Private vpnStatus As String = "Unknown"

    Private Async Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Initialize TableLayoutPanel
        tableLayoutPanel.Dock = DockStyle.Fill
        tableLayoutPanel.AutoScroll = True
        tableLayoutPanel.ColumnCount = 20
        tableLayoutPanel.RowCount = 0
        Me.Controls.Add(tableLayoutPanel)

        ' Initialize StatusStrip
        statusStrip.Items.Add(toolStripStatusLabel)
        statusStrip.Dock = DockStyle.Bottom
        Me.Controls.Add(statusStrip)

        ' Initialize ToolTip
        toolTip.AutoPopDelay = 5000
        toolTip.InitialDelay = 1000
        toolTip.ReshowDelay = 500
        toolTip.ShowAlways = True

        ' Read the .ini file from URL
        Await LoadIniFileAsync(iniFileUrl)

        ' Check VPN status on startup
        vpnStatus = Await GetPingStatusAsync(vpnServerIp)
        toolStripStatusLabel.Text = $"VPN Status: {vpnStatus}, Time until next update: {countdownTime} seconds"
        Timer1.Interval = 1000 ' 1 second
        Timer1.Start()
        Await UpdateStatusAsync()

    End Sub

    Private Async Function LoadIniFileAsync(url As String) As Task
        Try
            Dim iniFileContent As String = Await ReadIniFileFromUrlAsync(url)
            Using reader As New StringReader(iniFileContent)
                Dim line As String
                While (InlineAssignHelper(line, reader.ReadLine())) IsNot Nothing
                    If line.Contains("=") Then
                        Dim parts() As String = line.Split("="c)
                        If parts.Length = 2 Then
                            Dim store As String = parts(0).Trim()
                            Dim ipAndLocation() As String = parts(1).Split(","c)
                            If ipAndLocation.Length = 2 Then
                                Dim ipAddress As String = ipAndLocation(1).Trim()
                                Dim location As String = ipAndLocation(0).Trim()
                                ipAddresses(store) = (ipAddress, location)

                                ' Add a new label for each store
                                Dim label As New Label()
                                label.Text = store
                                label.Size = New Size(30, 20)
                                label.Margin = New Padding(5)
                                label.ForeColor = Color.White
                                tableLayoutPanel.Controls.Add(label)

                                ' Set the tooltip for the label
                                toolTip.SetToolTip(label, $"IP Address: {ipAddress}, Location: {location}")
                            End If
                        End If
                    End If
                End While
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error loading INI file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Function

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        If countdownTime > 0 Then
            countdownTime -= 1 ' Decrement by 1 second
            toolStripStatusLabel.Text = $"VPN Status: {vpnStatus}, Time until next update: {countdownTime} seconds"
        Else
            countdownTime = 120 ' Reset countdown
            UpdateVpnStatus()
            UpdateStatusAsync()
        End If
    End Sub




    Private Async Sub UpdateVpnStatus()
        vpnStatus = Await GetPingStatusAsync(vpnServerIp)
        toolStripStatusLabel.Text = $"VPN Status: {vpnStatus}, Time until next update: {countdownTime} seconds"

        If vpnStatus = "Offline" Then
            ' Change all labels to light gray if VPN is not connected
            For Each control As Control In tableLayoutPanel.Controls
                If TypeOf control Is Label Then
                    control.BackColor = Color.LightGray
                End If
            Next
        End If
    End Sub

    Private Async Function UpdateStatusAsync() As Task
        ' First, check the VPN server status
        Dim vpnStatus As String = Await GetPingStatusAsync(vpnServerIp)

        If vpnStatus = "Online" Then
            ' VPN is connected, proceed to check other nodes
            Dim ipQueue As New Queue(Of String)(ipAddresses.Values.Select(Function(x) x.Item1))

            While ipQueue.Count > 0
                Dim ipAddress As String = ipQueue.Dequeue()
                Dim store As String = ipAddresses.FirstOrDefault(Function(x) x.Value.Item1 = ipAddress).Key

                Dim status As String = Await GetPingStatusAsync(ipAddress)
                UpdateLabelColor(store, status)

                ' Delay between pings
                Await Task.Delay(100)
            End While
        Else
            tableLayoutPanel.Refresh()
        End If
    End Function


    Private Sub UpdateLabelColor(store As String, status As String)
        For Each control As Control In tableLayoutPanel.Controls
            If TypeOf control Is Label AndAlso control.Text = store Then
                control.BackColor = If(status = "Online", Color.Green, Color.Red)
            End If
        Next
    End Sub

    Private Async Function GetPingStatusAsync(ipAddress As String) As Task(Of String)
        Try
            Dim ping As New Ping()
            Dim reply As PingReply = Await ping.SendPingAsync(ipAddress, 1000)
            Return If(reply.Status = IPStatus.Success, "Online", "Offline")
        Catch ex As Exception
            Return "Error"
        End Try
    End Function

    Private Async Function ReadIniFileFromUrlAsync(url As String) As Task(Of String)
        Using client As New HttpClient()
            client.DefaultRequestHeaders.CacheControl = New Headers.CacheControlHeaderValue() With {
                .NoCache = True,
                .NoStore = True
            }
            Return Await client.GetStringAsync(url)
        End Using
    End Function

    Private Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
        target = value
        Return value
    End Function
End Class
