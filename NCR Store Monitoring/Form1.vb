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

    Private Async Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Initialize TableLayoutPanel
        tableLayoutPanel.Dock = DockStyle.Fill
        tableLayoutPanel.AutoScroll = True
        tableLayoutPanel.ColumnCount = 20 ' Adjust the number of columns as needed
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

        ' Read the .ini file from GitHub
        Dim iniFileContent As String = Await ReadIniFileFromUrlAsync("https://raw.githubusercontent.com/AdinNCR/NCR-Fo-Monitoring/refs/heads/master/NCR%20Store%20Monitoring/nodes.ini")

        ' Debug: Print the content of the ini file
        Console.WriteLine("INI File Content:")
        Console.WriteLine(iniFileContent)

        ' Process the .ini file content
        Using reader As New StringReader(iniFileContent)
            Dim line As String
            While (InlineAssignHelper(line, reader.ReadLine())) IsNot Nothing
                ' Debug: Print each line read from the ini file
                Console.WriteLine("Read Line: " & line)

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

                            ' Debug: Print the added label information
                            Console.WriteLine($"Added Label: {store}, IP: {ipAddress}, Location: {location}")
                        End If
                    End If
                End If
            End While
        End Using

        ' Initialize and start the Timer
        Timer1.Interval = 180000 ' 60 seconds
        Timer1.Start()

        ' Initial status check
        Await UpdateStatusAsync()
    End Sub

    Private Async Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        ' Update the status of each IP address every minute
        Await UpdateStatusAsync()
    End Sub

    'Private Async Function UpdateStatusAsync() As Task
    '    ' Check the status of each IP address asynchronously
    '    Dim tasks As New List(Of Task)()

    '    For Each store As String In ipAddresses.Keys
    '        Dim ipAddress As String = ipAddresses(store).Item1
    '        tasks.Add(Task.Run(Async Function()
    '                               Dim status As String = Await GetPingStatusAsync(ipAddress)
    '                               UpdateLabelColor(store, status)
    '                               Console.WriteLine($"Ping: {store}, IP: {ipAddress}, Status: {status}")
    '                           End Function))

    '    Next

    '    Await Task.WhenAll(tasks)

    '    ' Update the StatusStrip with the last update time
    '    toolStripStatusLabel.Text = $"Last Updated: {DateTime.Now}"

    '    ' Refresh the TableLayoutPanel to ensure it is fully updated
    '    tableLayoutPanel.Refresh()
    'End Function

    Private Async Function UpdateStatusAsync() As Task
        ' Create a queue for the IP addresses
        Dim ipQueue As New Queue(Of String)(ipAddresses.Values.Select(Function(x) x.Item1))

        ' Check the status of each IP address asynchronously
        While ipQueue.Count > 0
            Dim ipAddress As String = ipQueue.Dequeue()
            Dim store As String = ipAddresses.FirstOrDefault(Function(x) x.Value.Item1 = ipAddress).Key

            Dim status As String = Await GetPingStatusAsync(ipAddress)
            UpdateLabelColor(store, status)
            Console.WriteLine($"Ping: {store}, IP: {ipAddress}, Status: {status}")

            ' Delay between pings
            Await Task.Delay(100) ' Adjust the delay as needed
        End While

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
            Dim reply As PingReply = Await ping.SendPingAsync(ipAddress, 5000)
            If reply.Status = IPStatus.Success Then
                Return "Online"
            Else
                Return "Offline"
            End If
        Catch ex As Exception
            Console.WriteLine($"Error pinging {ipAddress}: {ex.Message}")
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
