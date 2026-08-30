Imports Guna.UI2.WinForms
Imports System.Data.Common
Imports System.IO
Imports LiveChartsCore
Imports LiveChartsCore.SkiaSharpView
Imports LiveChartsCore.SkiaSharpView.WinForms
Imports SkiaSharp
' Extension method for creating rounded rectangles
Public Module GraphicsExtensions
    <System.Runtime.CompilerServices.Extension()>
    Public Sub AddRoundedRectangle(path As Drawing2D.GraphicsPath, rect As Rectangle, radius As Integer)
        Dim diameter As Integer = radius * 2
        Dim size As New Size(diameter, diameter)
        Dim arc As New Rectangle(rect.Location, size)

        ' Top left arc
        path.AddArc(arc, 180, 90)

        ' Top right arc
        arc.X = rect.Right - diameter
        path.AddArc(arc, 270, 90)

        ' Bottom right arc
        arc.Y = rect.Bottom - diameter
        path.AddArc(arc, 0, 90)

        ' Bottom left arc
        arc.X = rect.Left
        path.AddArc(arc, 90, 90)

        path.CloseFigure()
    End Sub
End Module

Public Class Dashboard
    Private filterPanel As Panel
    Private titleLabel As Label
    Private legendPanel As Panel
    Private chartPanel As Panel
    Private lastProductCount As Integer = -1
    ' Navigation flag to prevent exit confirmation on programmatic close
    Private isNavigating As Boolean = False
    Private salesChart As CartesianChart
    Private currentChartMode As String = "Today"
    ' Loading panel fields
    Private loadingPanel As Panel
    Private loadingLabel As Label

    ' Navigation controls
    Private navInventoryBtn As Guna2Button
    Private navPOSBtn As Guna2Button
    Private navSalesRecordsBtn As Guna2Button
    Private navStaffBtn As Guna2Button
    Private navInventoryLogBtn As Guna2Button
    Private navAuditLogBtn As Guna2Button
    Private navLogoutBtn As Guna2Button
    Private navDashboardBtn As Guna2Button

    ' Runtime KPI card fields
    Private _lblTotalOrdersValue As Label
    Private _lblTotalOrdersSub As Label
    Private _lblStockValueValue As Label
    Private _lblStockValueSub As Label
    Private _lblRevenueValue As Label
    Private _lblRevenueSub As Label
    Private _lblGrossProfitValue As Label
    Private _lblGrossProfitSub As Label
    Private _kpiCards As New List(Of Guna2Panel)
    Private Const EmojiChart As String = "📊"
    Private Const EmojiMoney As String = "💰"
    Private Const EmojiTrend As String = "📈"
    Private Const EmojiProfit As String = "💵"
    Private CircleBg As Color = Color.FromArgb(255, 244, 217)

    ' Time-period filter
    Private _periodCombo As Guna2ComboBox
    Private _selectedPeriod As String = "Last 30 Days"

    ' Margin summary strip above the performance chart
    Private _marginStrip As Panel
    Private _headerPanel As Panel
    Private _lblSummaryRevenue As Label
    Private _lblSummaryCost As Label
    Private _lblSummaryProfit As Label
    Private _lblSummaryMargin As Label

    Public Sub New()
        Try
            Console.WriteLine("Dashboard constructor starting...")
            InitializeComponent()
            Console.WriteLine("InitializeComponent completed")

            Console.WriteLine("Dashboard constructor completed successfully")
        Catch ex As Exception
            Console.WriteLine($"Error in Dashboard constructor: {ex.Message}")
            Console.WriteLine($"Stack trace: {ex.StackTrace}")
            Throw
        End Try
    End Sub

    Private Sub ShowLoadingOverlay()
        If DashboardPanel IsNot Nothing Then DashboardPanel.ShadowDecoration.Enabled = False

        loadingPanel = New Panel()
        loadingPanel.BackColor = Color.Transparent
        loadingPanel.Dock = DockStyle.Fill
        loadingPanel.Location = New Point(0, 0)
        loadingPanel.Size = Me.ClientSize

        Me.Controls.Add(loadingPanel)
        loadingPanel.BringToFront()

        loadingLabel = New Label With {
            .Text = "Loading Dashboard...",
            .ForeColor = Color.FromArgb(51, 51, 51),
            .Font = New Font("Poppins", 16, FontStyle.Regular),
            .AutoSize = True,
            .BackColor = Color.Transparent
        }

        loadingPanel.Controls.Add(loadingLabel)
        CenterLoadingLabel()

        AddHandler loadingPanel.SizeChanged, Sub()
                                                 CenterLoadingLabel()
                                             End Sub
    End Sub

    Private Sub HideLoadingOverlay()
        If loadingPanel IsNot Nothing Then
            Me.Controls.Remove(loadingPanel)
            loadingPanel.Dispose()
            loadingPanel = Nothing
        End If
        loadingLabel = Nothing
        If DashboardPanel IsNot Nothing Then DashboardPanel.ShadowDecoration.Enabled = False
    End Sub

    Private Sub CenterLoadingLabel()
        Try
            If loadingLabel IsNot Nothing AndAlso loadingPanel IsNot Nothing Then
                loadingLabel.AutoSize = True
                Application.DoEvents()
                loadingLabel.Location = New Point(
                    (loadingPanel.Width - loadingLabel.Width) \ 2,
                    (loadingPanel.Height - loadingLabel.Height) \ 2
                )
            End If
        Catch
        End Try
    End Sub

    Private Async Sub Dashboard_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            Console.WriteLine("Dashboard_Load starting...")

            ' Initialize form with new color scheme
            Me.Text = $"JadeClinic Dashboard - Welcome {frmLoginvb.LoggedInUsername}"
            Me.BackColor = Color.FromArgb(248, 248, 247)
            Me.MaximizeBox = False
            Me.MinimizeBox = False

            ' Improved full-screen behavior: remove window chrome and cover the entire screen including taskbar
            Me.FormBorderStyle = FormBorderStyle.None
            Me.TopMost = True
            Me.WindowState = FormWindowState.Normal
            Me.Bounds = Screen.PrimaryScreen.Bounds
            Me.WindowState = FormWindowState.Maximized
            Console.WriteLine("Basic form properties set (full screen)")

            ' Build runtime KPI cards
            BuildKPICards()
            Console.WriteLine("KPI cards built")

            ' Apply new color scheme to existing panels
            ApplyNewColorScheme()
            Console.WriteLine("Color scheme applied")

            ' Show loading overlay
            ShowLoadingOverlay()
            Await Task.Delay(200)
            Console.WriteLine("Loading overlay shown")

            ' Create navigation menu (use shared builder)
            NavigationBuilder.Build(DashboardPanel, Me, "Dashboard")
            Console.WriteLine("Navigation menu created")

            ' Load all UI/data while loading panel is visible
            LoadChartInterface()
            Console.WriteLine("Chart interface loaded")

            ' Build the compact margin chips inside the chart panel (top-right)
            BuildMarginSummaryStrip()
            Console.WriteLine("Margin summary strip built")

            InitializeProductSearch()
            Console.WriteLine("Product search initialized")

            LoadDashboardData()
            Console.WriteLine("Dashboard data loaded")

            UpdateMonthlyStockTrend()
            Console.WriteLine("Monthly stock trend updated")

            LoadChartData("Today")
            Console.WriteLine("Chart data loaded")

            ' Make DataGridView columns non-sortable
            If Guna2DataGridView1 IsNot Nothing AndAlso Guna2DataGridView1.Columns IsNot Nothing Then
                For Each col As DataGridViewColumn In Guna2DataGridView1.Columns
                    col.SortMode = DataGridViewColumnSortMode.NotSortable
                Next
            End If
            Console.WriteLine("DataGridView configured")

            ' Initialize profile section
            InitializeProfileSection()
            Console.WriteLine("Profile section initialized")

            ' Hide loading overlay
            HideLoadingOverlay()
            Console.WriteLine("Loading overlay hidden")

            Console.WriteLine("Dashboard_Load completed successfully")

            ' Update form title to show logged-in user
            Me.Text = $"JadeClinic Dashboard - Welcome {frmLoginvb.LoggedInUsername}"

            ' Start idle timeout monitoring
            IdleTimeoutManager.Instance.StartMonitoring(Me)

            ' Start the background periodic cloud sync schedule
            SyncQueue.Instance.StartScheduledSync()

            SetupTabIndex()

        Catch ex As Exception
            Console.WriteLine($"Error in Dashboard_Load: {ex.Message}")
            Console.WriteLine($"Stack trace: {ex.StackTrace}")

            HideLoadingOverlay()

            ' Show error to user
            MessageBox.Show($"Error loading dashboard: {ex.Message}{vbCrLf}{vbCrLf}Some features may not work correctly.",
                          "Dashboard Load Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Sub SetupTabIndex()
        txtProductSearch.TabIndex = 0
        Utilities.ApplyInputFocusEffects(Me)
    End Sub

    Private Sub UpdateMonthlyStockTrend()
        ' Placeholder for monthly trend updates
        ' This can be expanded later when LiveCharts is properly configured
    End Sub
    Private Sub ApplyRoundedCorners(target As Control, radius As Integer)
        Try
            If target Is Nothing Then Return

            Dim updateRegion As Action = Sub()
                                             If target.Width <= 1 OrElse target.Height <= 1 Then Return

                                             Using path As New Drawing2D.GraphicsPath()
                                                 path.AddRoundedRectangle(New Rectangle(0, 0, target.Width - 1, target.Height - 1), radius)
                                                 Dim oldRegion = target.Region
                                                 target.Region = New Region(path)
                                                 If oldRegion IsNot Nothing Then oldRegion.Dispose()
                                             End Using
                                         End Sub

            updateRegion()
            AddHandler target.SizeChanged, Sub(sender, e)
                                               updateRegion()
                                           End Sub
        Catch ex As Exception
            Console.WriteLine($"Error applying rounded corners: {ex.Message}")
        End Try
    End Sub
    Private Sub ApplyNewColorScheme()
        Try
            Console.WriteLine("ApplyNewColorScheme starting...")
            Me.BackColor = Color.FromArgb(248, 248, 247)
            Console.WriteLine("ApplyNewColorScheme completed")
        Catch ex As Exception
            Console.WriteLine($"Error applying color scheme: {ex.Message}")
        End Try
    End Sub

    ' Helper method to validate user session
    Private Function ValidateUserSession() As Boolean
        If String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
            MessageBox.Show("User session expired. Please log in again.", "Session Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            frmLoginvb.Show()
            Me.Hide()
            Return False
        End If
        Return True
    End Function

    Private Sub InitializeProfileSection()
        Try
            ' Initialize profile section for logged-in user
        Catch ex As Exception
            ' Fallback if there's an error - silently fail since profile controls may not exist on all forms
        End Try
    End Sub



    Private Sub LoadDashboardData()
        LoadKPIData()
        LoadAllPopularProducts()
        LoadInventoryStatusChart()
    End Sub

    Private Sub OnPeriodChanged(sender As Object, e As EventArgs)
        If _periodCombo Is Nothing OrElse _periodCombo.SelectedIndex < 0 Then Return
        _selectedPeriod = _periodCombo.SelectedItem.ToString()

        ' Map period to chart mode
        Select Case _selectedPeriod
            Case "Today"
                currentChartMode = "Today"
            Case "Last 7 Days"
                currentChartMode = "Weekly"
            Case "Last 30 Days", "This Month", "Last Month"
                currentChartMode = "Monthly"
            Case "This Year", "All Time"
                currentChartMode = "Yearly"
            Case Else
                currentChartMode = "Monthly"
        End Select

        LoadKPIData()
        LoadChartData(currentChartMode)
        LoadAllPopularProducts()
    End Sub

    Private Function GetDateRange() As (StartSql As String, EndSql As String, Label As String)
        Dim today As Date = Date.Today
        Dim startDt As Date
        Dim endDt As Date = today.AddDays(1).AddTicks(-1)
        Dim label As String = ""

        Select Case _selectedPeriod
            Case "Today"
                startDt = today
                label = "Today, " & today.ToString("MMM d, yyyy")
            Case "Last 7 Days"
                startDt = today.AddDays(-6)
                label = startDt.ToString("MMM d") & " - " & today.ToString("d, yyyy")
            Case "Last 30 Days"
                startDt = today.AddDays(-29)
                If startDt.Month = today.Month Then
                    label = startDt.ToString("MMM d") & " - " & today.ToString("d, yyyy")
                Else
                    label = startDt.ToString("MMM d") & " - " & today.ToString("MMM d, yyyy")
                End If
            Case "This Month"
                startDt = New Date(today.Year, today.Month, 1)
                If startDt.Month = today.Month Then
                    label = startDt.ToString("MMM d") & " - " & today.ToString("d, yyyy")
                Else
                    label = startDt.ToString("MMM d") & " - " & today.ToString("MMM d, yyyy")
                End If
            Case "Last Month"
                Dim lastMonth As Date = today.AddMonths(-1)
                startDt = New Date(lastMonth.Year, lastMonth.Month, 1)
                endDt = startDt.AddMonths(1).AddTicks(-1)
                label = startDt.ToString("MMM d") & " - " & endDt.ToString("d, yyyy")
            Case "This Year"
                startDt = New Date(today.Year, 1, 1)
                label = "Jan 1 - " & today.ToString("MMM d, yyyy")
            Case "All Time"
                startDt = Date.MinValue
                label = "All time"
            Case Else
                startDt = Date.MinValue
                label = "All time"
        End Select

        Return (startDt.ToString("yyyy-MM-dd"), endDt.ToString("yyyy-MM-dd HH:mm:ss"), label)
    End Function

    Private Sub LoadKPIData()
        Try
            Dim dr = GetDateRange()
            Dim dateFilter As String = ""
            Dim orderFilter As String = ""

            If _selectedPeriod <> "All Time" Then
                dateFilter = $"AND s.SaleDate >= '{dr.StartSql}' AND s.SaleDate <= '{dr.EndSql}'"
                orderFilter = $"AND si.SaleID IN (SELECT SaleID FROM Sales WHERE SaleDate >= '{dr.StartSql}' AND SaleDate <= '{dr.EndSql}')"
            End If

            ' Orders + Revenue + COGS in one query
            Dim query As String = $"
                SELECT
                    IFNULL((SELECT COUNT(*) FROM Sales s WHERE 1=1 {dateFilter}), 0) AS TotalOrders,
                    IFNULL((SELECT SUM(TotalAmount) FROM Sales s WHERE 1=1 {dateFilter}), 0) AS TotalRevenue,
                    IFNULL((SELECT SUM(si.Quantity * p.CostPrice) FROM SaleItems si JOIN Products p ON si.ProductID = p.ProductID {orderFilter}), 0) AS TotalCOGS,
                    IFNULL((SELECT SUM(CostPrice * CurrentStock) FROM Products WHERE IsActive = 1), 0) AS ActiveStockValue,
                    IFNULL((SELECT COUNT(*) FROM Products WHERE IsActive = 1), 0) AS ActiveProductCount"

            Using reader As DbDataReader = Utilities.ExecuteReader(query, Nothing)
                If reader.Read() Then
                    Dim totalOrders As Integer = Convert.ToInt32(reader("TotalOrders"))
                    Dim totalRevenue As Decimal = Convert.ToDecimal(reader("TotalRevenue"))
                    Dim totalCOGS As Decimal = Convert.ToDecimal(reader("TotalCOGS"))
                    Dim activeStockValue As Decimal = Convert.ToDecimal(reader("ActiveStockValue"))
                    Dim activeProductCount As Integer = Convert.ToInt32(reader("ActiveProductCount"))
                    Dim grossProfit As Decimal = totalRevenue - totalCOGS
                    Dim pesoSign As String = ChrW(&H20B1)

                    ' Card 1: Total Orders
                    If _lblTotalOrdersValue IsNot Nothing Then _lblTotalOrdersValue.Text = totalOrders.ToString("N0")
                    If _lblTotalOrdersSub IsNot Nothing Then
                        Dim daysInPeriod As Integer = Math.Max(1, (Date.Parse(dr.EndSql) - Date.Parse(dr.StartSql)).Days + 1)
                        If _selectedPeriod = "All Time" Then
                            Dim allDays = Math.Max(1, (Date.Today - New Date(2020, 1, 1)).Days + 1)
                            _lblTotalOrdersSub.Text = $"Avg {totalOrders / allDays:F1} /day"
                        Else
                            _lblTotalOrdersSub.Text = $"Avg {totalOrders / daysInPeriod:F1} /day"
                        End If
                    End If

                    ' Card 2: Inventory Value
                    If _lblStockValueValue IsNot Nothing Then _lblStockValueValue.Text = String.Format(Globalization.CultureInfo.GetCultureInfo("en-PH"), "{0}{1:N0}", pesoSign, activeStockValue)
                    If _lblStockValueSub IsNot Nothing Then _lblStockValueSub.Text = $"{activeProductCount} products"

                    ' Card 3: Total Revenue
                    If _lblRevenueValue IsNot Nothing Then _lblRevenueValue.Text = String.Format(Globalization.CultureInfo.GetCultureInfo("en-PH"), "{0}{1:N0}", pesoSign, totalRevenue)
                    If _lblRevenueSub IsNot Nothing Then _lblRevenueSub.Text = dr.Label

                    ' Card 4: Gross Profit
                    If _lblGrossProfitValue IsNot Nothing Then _lblGrossProfitValue.Text = String.Format(Globalization.CultureInfo.GetCultureInfo("en-PH"), "{0}{1:N0}", pesoSign, grossProfit)
                    If _lblGrossProfitSub IsNot Nothing Then
                        If totalRevenue > 0 Then
                            Dim margin As Decimal = (grossProfit / totalRevenue) * 100
                            _lblGrossProfitSub.Text = $"{margin:F0}% margin"
                        Else
                            _lblGrossProfitSub.Text = "0% margin"
                        End If
                    End If

                    ' Update the margin summary strip above the performance chart
                    UpdateMarginSummary(totalRevenue, totalCOGS, grossProfit)

                    lastProductCount = totalOrders
                Else
                    If _lblTotalOrdersValue IsNot Nothing Then _lblTotalOrdersValue.Text = "0"
                    If _lblStockValueValue IsNot Nothing Then _lblStockValueValue.Text = ChrW(&H20B1) & "0"
                    If _lblRevenueValue IsNot Nothing Then _lblRevenueValue.Text = ChrW(&H20B1) & "0"
                    If _lblGrossProfitValue IsNot Nothing Then _lblGrossProfitValue.Text = ChrW(&H20B1) & "0"
                End If
            End Using
        Catch ex As Exception
            Console.WriteLine($"Error loading KPI data: {ex.Message}")
        End Try
    End Sub

    Private Sub BuildMarginSummaryStrip()
        Try
            If _marginStrip IsNot Nothing Then
                If _headerPanel IsNot Nothing Then _headerPanel.Controls.Remove(_marginStrip)
                _marginStrip.Dispose()
                _marginStrip = Nothing
            End If

            ' Compact margin chips rendered in the top-right of the AreaChart panel,
            ' visually belonging to the chart below them.

            Dim captions() As String = {"Revenue", "Cost of Goods", "Gross Profit", "Margin"}
            Dim colors() As Color = {
                Color.FromArgb(245, 158, 11),
                Color.FromArgb(148, 163, 184),
                Color.FromArgb(46, 125, 50),
                Color.FromArgb(34, 34, 34)
            }

            Dim chipH As Integer = 28
            Dim chipGap As Integer = 14
            Dim rightMargin As Integer = 6

            ' Pre-measure captions so caption and value never collide
            Dim captionFont As New Font("Poppins", 8.0F, FontStyle.Regular)
            Dim valueFont As New Font("Poppins", 8.5F, FontStyle.Bold)
            Dim capW(captions.Length - 1) As Integer
            For i As Integer = 0 To captions.Length - 1
                capW(i) = TextRenderer.MeasureText(captions(i) & " ", captionFont).Width
            Next

            ' Value display width: grows for large numbers but caps to avoid overflow
            Dim valueW As Integer = 108

            ' Compute chip widths and right-aligned positions
            Dim chipW(captions.Length - 1) As Integer
            Dim totalW As Integer = 0
            For i As Integer = 0 To captions.Length - 1
                chipW(i) = 10 + capW(i) + 6 + valueW
                totalW += chipW(i)
                If i < captions.Length - 1 Then totalW += chipGap
            Next

            Dim stripW As Integer = totalW + rightMargin
            Dim stripX As Integer = 0
            If _headerPanel IsNot Nothing Then stripX = _headerPanel.Width - stripW - 4
            _marginStrip = New Panel() With {
                .Location = New Point(stripX, 12),
                .Size = New Size(stripW, 34),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
                .BackColor = Color.Transparent
            }

            Dim rightEdge As Integer = rightMargin
            For i As Integer = captions.Length - 1 To 0 Step -1
                Dim x As Integer = _marginStrip.Width - rightEdge - chipW(i)
                Dim chip As New Guna.UI2.WinForms.Guna2Panel() With {
                    .Location = New Point(x, 3),
                    .Size = New Size(chipW(i), chipH),
                    .FillColor = Color.FromArgb(255, 253, 248),
                    .BorderColor = Color.FromArgb(232, 232, 232),
                    .BorderThickness = 1,
                    .BorderRadius = 8
                }
                chip.ShadowDecoration.Enabled = False

                Dim cap As New Label() With {
                    .Text = captions(i),
                    .Font = captionFont,
                    .ForeColor = Color.FromArgb(140, 140, 140),
                    .BackColor = Color.Transparent,
                    .Location = New Point(10, 4),
                    .AutoSize = True
                }

                Dim val As New Label() With {
                    .Text = "",
                    .Font = valueFont,
                    .ForeColor = colors(i),
                    .BackColor = Color.Transparent,
                    .AutoSize = False,
                    .AutoEllipsis = True,
                    .TextAlign = ContentAlignment.MiddleLeft,
                    .Location = New Point(14 + capW(i), 4),
                    .Size = New Size(valueW - 6, chipH - 8)
                }

                Select Case i
                    Case 0
                        _lblSummaryRevenue = val
                    Case 1
                        _lblSummaryCost = val
                    Case 2
                        _lblSummaryProfit = val
                    Case 3
                        _lblSummaryMargin = val
                End Select

                chip.Controls.Add(cap)
                chip.Controls.Add(val)
                _marginStrip.Controls.Add(chip)

                rightEdge += chipW(i) + chipGap
            Next

            If _headerPanel IsNot Nothing Then
                _headerPanel.Controls.Add(_marginStrip)
                _marginStrip.BringToFront()
            End If
            If _headerPanel IsNot Nothing AndAlso titleLabel IsNot Nothing Then
                titleLabel.SendToBack()
            End If
        Catch ex As Exception
            Console.WriteLine($"Error building margin summary strip: {ex.Message}")
        End Try
    End Sub

    Private Sub UpdateMarginSummary(totalRevenue As Decimal, totalCost As Decimal, grossProfit As Decimal)
        Try
            Dim pesoSign As String = ChrW(&H20B1)
            If _lblSummaryRevenue IsNot Nothing Then
                _lblSummaryRevenue.Text = String.Format(Globalization.CultureInfo.GetCultureInfo("en-PH"), "{0}{1:N0}", pesoSign, totalRevenue)
            End If
            If _lblSummaryCost IsNot Nothing Then
                _lblSummaryCost.Text = String.Format(Globalization.CultureInfo.GetCultureInfo("en-PH"), "{0}{1:N0}", pesoSign, totalCost)
            End If
            If _lblSummaryProfit IsNot Nothing Then
                _lblSummaryProfit.Text = String.Format(Globalization.CultureInfo.GetCultureInfo("en-PH"), "{0}{1:N0}", pesoSign, grossProfit)
            End If
            If _lblSummaryMargin IsNot Nothing Then
                If totalRevenue > 0 Then
                    Dim margin As Decimal = (grossProfit / totalRevenue) * 100
                    _lblSummaryMargin.Text = $"{margin:F0}%"
                Else
                    _lblSummaryMargin.Text = "0%"
                End If
            End If
        Catch ex As Exception
            Console.WriteLine($"Error updating margin summary: {ex.Message}")
        End Try
    End Sub

    Private Sub LoadAllPopularProducts()
        Try
            ' Clear any existing labels that might interfere
            For Each control As Control In PopularPanel.Controls.OfType(Of Label).ToArray()
                If control.Name <> "txtProductSearch" AndAlso control.Name <> "Guna2DataGridView1" Then
                    PopularPanel.Controls.Remove(control)
                    control.Dispose()
                End If
            Next

            PopularPanel.Padding = New Padding(15, 10, 15, 15)
            PopularPanel.FillColor = Color.White
            PopularPanel.BorderColor = Color.FromArgb(232, 232, 232)
            PopularPanel.BorderThickness = 2

            ' Keep the existing header panel (so txtProductSearch is not disposed)
            Dim headerPanel As Panel = PopularPanel.Controls.OfType(Of Panel)().
            FirstOrDefault(Function(p) p.Name = "popularHeaderPanel")

            If headerPanel Is Nothing Then
                headerPanel = New Panel() With {
                .Name = "popularHeaderPanel",
                .Dock = DockStyle.Top,
                .Height = 45,
                .BackColor = Color.Transparent,
                .Padding = New Padding(15, 8, 15, 8)
            }
                PopularPanel.Controls.Add(headerPanel)
            Else
                ' Clear only labels inside the header (keep txtProductSearch)
                For Each ctrl As Control In headerPanel.Controls.OfType(Of Label).ToArray()
                    headerPanel.Controls.Remove(ctrl)
                    ctrl.Dispose()
                Next
            End If

            Dim titleLabel As New Label()
            titleLabel.Text = "Popular Product"
            titleLabel.Font = New Font("Poppins Medium", 13.8F, FontStyle.Regular)
            titleLabel.ForeColor = Color.FromArgb(51, 51, 51)
            titleLabel.AutoSize = True
            titleLabel.BackColor = Color.Transparent
            titleLabel.Location = New Point(0, 6)
            headerPanel.Controls.Add(titleLabel)

            txtProductSearch.Size = New Size(220, 24)
            txtProductSearch.BorderRadius = 10
            txtProductSearch.BackColor = Color.Transparent
            txtProductSearch.BorderThickness = 1
            txtProductSearch.Anchor = AnchorStyles.Top Or AnchorStyles.Right

            Dim positionSearch As Action = Sub()
                                               txtProductSearch.Location = New Point(headerPanel.ClientSize.Width - txtProductSearch.Width, 6)
                                           End Sub
            positionSearch()
            AddHandler headerPanel.SizeChanged, Sub(sender, e)
                                                    positionSearch()
                                                End Sub

            If Not headerPanel.Controls.Contains(txtProductSearch) Then
                headerPanel.Controls.Add(txtProductSearch)
            End If

            If Not PopularPanel.Controls.Contains(Guna2DataGridView1) Then
                PopularPanel.Controls.Add(Guna2DataGridView1)
            End If

            Guna2DataGridView1.Dock = DockStyle.Fill
            headerPanel.BringToFront()

            RemoveHandler txtProductSearch.TextChanged, AddressOf TxtProductSearch_TextChanged
            AddHandler txtProductSearch.TextChanged, AddressOf TxtProductSearch_TextChanged

            Dim searchText As String = If(txtProductSearch.Text, "").Trim()
            Dim isSearching As Boolean = Not String.IsNullOrWhiteSpace(searchText)

            ' Configure the existing DataGridView with new color scheme
            Guna2DataGridView1.Columns.Clear()
            Guna2DataGridView1.Rows.Clear()

            ' Apply new dark theme styling
            Guna2DataGridView1.BackgroundColor = Color.White
            Guna2DataGridView1.BorderStyle = BorderStyle.None
            Guna2DataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            Guna2DataGridView1.RowHeadersVisible = False
            Guna2DataGridView1.AllowUserToAddRows = False
            Guna2DataGridView1.AllowUserToDeleteRows = False
            Guna2DataGridView1.AllowUserToResizeColumns = False
            Guna2DataGridView1.AllowUserToResizeRows = False
            Guna2DataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            Guna2DataGridView1.MultiSelect = False
            Guna2DataGridView1.ReadOnly = True
            Guna2DataGridView1.ScrollBars = ScrollBars.Vertical
            Guna2DataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill

            ' Cell styling
            Guna2DataGridView1.GridColor = Color.FromArgb(230, 230, 230)
            Guna2DataGridView1.DefaultCellStyle.BackColor = Color.White
            Guna2DataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 250, 249)
            Guna2DataGridView1.DefaultCellStyle.ForeColor = Color.FromArgb(51, 51, 51)
            Guna2DataGridView1.DefaultCellStyle.SelectionBackColor = Color.FromArgb(235, 228, 200)
            Guna2DataGridView1.DefaultCellStyle.SelectionForeColor = Color.FromArgb(51, 51, 51)
            Guna2DataGridView1.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
            Guna2DataGridView1.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            Guna2DataGridView1.DefaultCellStyle.Padding = New Padding(5, 4, 5, 4)

            ' Header styling
            Guna2DataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(250, 250, 249)
            Guna2DataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(51, 51, 51)
            Guna2DataGridView1.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(250, 250, 249)
            Guna2DataGridView1.ColumnHeadersDefaultCellStyle.Font = New Font("Poppins SemiBold", 10.0F, FontStyle.Regular)
            Guna2DataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            Guna2DataGridView1.ColumnHeadersHeight = 50
            Guna2DataGridView1.RowTemplate.Height = 60
            Guna2DataGridView1.EnableHeadersVisualStyles = False
            Guna2DataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing


            For Each col As DataGridViewColumn In Guna2DataGridView1.Columns
                col.Resizable = DataGridViewTriState.False
            Next

            ' Add columns
            Dim noColumn As New DataGridViewTextBoxColumn()
            noColumn.Name = "No"
            noColumn.HeaderText = "No"
            noColumn.FillWeight = 8
            noColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            noColumn.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Bold)
            Guna2DataGridView1.Columns.Add(noColumn)

            Dim codeColumn As New DataGridViewTextBoxColumn()
            codeColumn.Name = "ProductCode"
            codeColumn.HeaderText = "Code"
            codeColumn.FillWeight = 15
            codeColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            Guna2DataGridView1.Columns.Add(codeColumn)

            Dim nameColumn As New DataGridViewTextBoxColumn()
            nameColumn.Name = "ProductName"
            nameColumn.HeaderText = "Product Name"
            nameColumn.FillWeight = 35
            nameColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
            nameColumn.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
            Guna2DataGridView1.Columns.Add(nameColumn)

            Dim categoryColumn As New DataGridViewTextBoxColumn()
            categoryColumn.Name = "Category"
            categoryColumn.HeaderText = "Category"
            categoryColumn.FillWeight = 20
            categoryColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            Guna2DataGridView1.Columns.Add(categoryColumn)

            Dim soldColumn As New DataGridViewTextBoxColumn()
            soldColumn.Name = "TimesSold"
            soldColumn.HeaderText = "Sold"
            soldColumn.FillWeight = 12
            soldColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            soldColumn.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Bold)
            Guna2DataGridView1.Columns.Add(soldColumn)

            Dim priceColumn As New DataGridViewTextBoxColumn()
            priceColumn.Name = "SellingPrice"
            priceColumn.HeaderText = "Price"
            priceColumn.FillWeight = 15
            priceColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            priceColumn.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Bold)
            priceColumn.DefaultCellStyle.ForeColor = Color.FromArgb(51, 51, 51)
            Guna2DataGridView1.Columns.Add(priceColumn)


            ' Query to get products ranked by sold quantity
            Dim query As String = "
            SELECT
                p.ProductID,
                p.ProductCode,
                p.ProductName,
                p.Category,
                p.SellingPrice,
                IFNULL(SUM(si.Quantity), 0) AS TimesSold
            FROM Products p
            LEFT JOIN SaleItems si ON p.ProductID = si.ProductID
            WHERE p.IsActive = 1
              AND (
                    @Search = ''
                    OR p.ProductCode LIKE @SearchLike
                    OR p.ProductName LIKE @SearchLike
                    OR p.Category LIKE @SearchLike
                  )
            GROUP BY p.ProductID, p.ProductCode, p.ProductName, p.Category, p.SellingPrice
            ORDER BY IFNULL(SUM(si.Quantity), 0) DESC, p.ProductName
            LIMIT 20"

            Dim rowIndex As Integer = 1
            Dim parameters As SqlParameter() = {
            New SqlParameter("@Search", searchText),
            New SqlParameter("@SearchLike", "%" & searchText & "%")
        }

            Using reader As DbDataReader = Utilities.ExecuteReader(query, parameters)
                While reader.Read()
                    Dim row As DataGridViewRow = New DataGridViewRow()
                    row.CreateCells(Guna2DataGridView1)

                    row.Cells(0).Value = rowIndex.ToString()
                    row.Cells(1).Value = reader("ProductCode").ToString()
                    row.Cells(2).Value = reader("ProductName").ToString()
                    row.Cells(3).Value = reader("Category").ToString()
                    row.Cells(4).Value = Convert.ToInt32(reader("TimesSold")).ToString()
                    row.Cells(5).Value = ChrW(&H20B1) & Convert.ToDecimal(reader("SellingPrice")).ToString("F2")

                    Guna2DataGridView1.Rows.Add(row)
                    rowIndex += 1
                End While
            End Using

            If Guna2DataGridView1.Rows.Count = 0 AndAlso isSearching Then
                Dim emptyRowIndex As Integer = Guna2DataGridView1.Rows.Add()
                Dim emptyRow = Guna2DataGridView1.Rows(emptyRowIndex)

                For i As Integer = 0 To Guna2DataGridView1.Columns.Count - 1
                    emptyRow.Cells(i).Value = String.Empty
                Next

                emptyRow.Cells("ProductName").Value = "No product found"
                emptyRow.DefaultCellStyle.ForeColor = Color.FromArgb(102, 102, 102)
                emptyRow.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Italic)
                emptyRow.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            End If

            If Not isSearching Then
                If Guna2DataGridView1.Rows.Count > 0 Then
                    Guna2DataGridView1.Rows(0).DefaultCellStyle.BackColor = Color.FromArgb(235, 205, 80)
                    Guna2DataGridView1.Rows(0).DefaultCellStyle.ForeColor = Color.FromArgb(51, 51, 51)
                End If
                If Guna2DataGridView1.Rows.Count > 1 Then
                    Guna2DataGridView1.Rows(1).DefaultCellStyle.BackColor = Color.FromArgb(205, 205, 210)
                    Guna2DataGridView1.Rows(1).DefaultCellStyle.ForeColor = Color.FromArgb(51, 51, 51)
                End If
                If Guna2DataGridView1.Rows.Count > 2 Then
                    Guna2DataGridView1.Rows(2).DefaultCellStyle.BackColor = Color.FromArgb(180, 145, 80)
                    Guna2DataGridView1.Rows(2).DefaultCellStyle.ForeColor = Color.FromArgb(51, 51, 51)
                End If
            End If

            For Each row As DataGridViewRow In Guna2DataGridView1.Rows
                row.Resizable = DataGridViewTriState.False
            Next

            Guna2DataGridView1.ClearSelection()

            txtProductSearch.PlaceholderText = Char.ConvertFromUtf32(&H1F50D) & " Search products..."
            txtProductSearch.Font = New Font("Poppins", 10.0F)
            txtProductSearch.ForeColor = Color.FromArgb(51, 51, 51)
            txtProductSearch.BackColor = Color.FromArgb(245, 245, 245)
            txtProductSearch.BorderRadius = 10

        Catch ex As Exception
            Console.WriteLine($"Error loading products: {ex.Message}")
            MessageBox.Show("Unable to load product data. Please try refreshing the dashboard.", "Data Load Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub
    Private Sub TxtProductSearch_TextChanged(sender As Object, e As EventArgs)
        LoadAllPopularProducts()
    End Sub

    Private Sub LoadInventoryStatusChart()
        Try
            ' Clear existing controls in LowStockPanel
            LowStockPanel.Controls.Clear()

            ' Add title for the inventory status chart
            Dim titleLabel As New Label()
            titleLabel.Text = "Inventory Status Overview"
            titleLabel.Font = New Font("Poppins Medium", 13.8F, FontStyle.Regular)
            titleLabel.ForeColor = Color.FromArgb(51, 51, 51)
            titleLabel.BackColor = Color.Transparent
            titleLabel.Dock = DockStyle.Top
            titleLabel.Height = 50
            titleLabel.TextAlign = ContentAlignment.MiddleLeft
            titleLabel.Padding = New Padding(25, 0, 0, 0)
            LowStockPanel.Controls.Add(titleLabel)

            ' Query to get inventory status counts for requested categories:
            ' B.R.L, A.R.L, Inactive, No Stock
            Dim query As String = "
                SELECT 
                    SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) AS InactiveCount,
                    SUM(CASE WHEN IsActive = 1 AND CurrentStock = 0 THEN 1 ELSE 0 END) AS NoStockCount,
                    SUM(CASE WHEN IsActive = 1 AND CurrentStock > 0 AND ReorderLevel > 0 AND CurrentStock <= ReorderLevel THEN 1 ELSE 0 END) AS BRLCount,
                    SUM(CASE WHEN IsActive = 1 AND CurrentStock > 0 AND (ReorderLevel <= 0 OR CurrentStock > ReorderLevel) THEN 1 ELSE 0 END) AS ARLCount
                FROM Products"

            Dim inactiveCount As Integer = 0
            Dim noStockCount As Integer = 0
            Dim brlCount As Integer = 0
            Dim arlCount As Integer = 0

            Using reader As DbDataReader = Utilities.ExecuteReader(query, Nothing)
                If reader.Read() Then
                    inactiveCount = Convert.ToInt32(reader("InactiveCount"))
                    noStockCount = Convert.ToInt32(reader("NoStockCount"))
                    brlCount = Convert.ToInt32(reader("BRLCount"))
                    arlCount = Convert.ToInt32(reader("ARLCount"))
                End If
            End Using

            ' Create main container that fills remaining space
            Dim mainContainer As New Panel()
            mainContainer.Dock = DockStyle.Fill
            mainContainer.Padding = New Padding(16)
            mainContainer.BackColor = Color.White
            LowStockPanel.Controls.Add(mainContainer)
            ApplyRoundedCorners(mainContainer, 18)

            ' Create pie chart for status overview (responsive)
            Dim pieChart As New PieChart()
            pieChart.BackColor = Color.White
            pieChart.Margin = New Padding(2)
            ApplyRoundedCorners(pieChart, 14)

            Dim updateChartLayout As Action = Sub()
                                                  Dim availW = Math.Max(100, mainContainer.ClientSize.Width - 40)
                                                  Dim availH = Math.Max(100, mainContainer.ClientSize.Height - 80)
                                                  Dim chartSize = Math.Min(availW, availH)

                                                  pieChart.Size = New Size(chartSize, chartSize)
                                                  pieChart.Location = New Point((mainContainer.ClientSize.Width - pieChart.Width) \ 2,
                                                                               (mainContainer.ClientSize.Height - pieChart.Height) \ 2)
                                              End Sub

            updateChartLayout()
            AddHandler mainContainer.SizeChanged, Sub(sender, e)
                                                      updateChartLayout()
                                                  End Sub

            ' Create pie series data (B.R.L, A.R.L, Inactive, No Stock)
            Dim series As New List(Of ISeries)()

            If brlCount > 0 Then
                series.Add(New PieSeries(Of Integer) With {
                .Values = {brlCount},
                .Name = "B.R.L",
                .Fill = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#FFB547")),
                .Stroke = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#FFFFFF")) With {.StrokeThickness = 2}
            })
            End If

            If arlCount > 0 Then
                series.Add(New PieSeries(Of Integer) With {
                .Values = {arlCount},
                .Name = "A.R.L",
                .Fill = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#10D862")),
                .Stroke = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#FFFFFF")) With {.StrokeThickness = 2}
            })
            End If

            If inactiveCount > 0 Then
                series.Add(New PieSeries(Of Integer) With {
                .Values = {inactiveCount},
                .Name = "Inactive",
                .Fill = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#7F8C8D")),
                .Stroke = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#FFFFFF")) With {.StrokeThickness = 2}
            })
            End If

            If noStockCount > 0 Then
                series.Add(New PieSeries(Of Integer) With {
                .Values = {noStockCount},
                .Name = "No Stock",
                .Fill = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#FF4757")),
                .Stroke = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#FFFFFF")) With {.StrokeThickness = 2}
            })
            End If

            pieChart.Series = series

            ' Configure chart animation and interactivity
            pieChart.AnimationsSpeed = TimeSpan.FromMilliseconds(1200)
            pieChart.LegendPosition = LiveChartsCore.Measure.LegendPosition.Bottom
            pieChart.LegendTextPaint = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#333333"))
            pieChart.LegendBackgroundPaint = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#FFFFFF"))

            ' Enhanced tooltip with numbers
            pieChart.TooltipPosition = LiveChartsCore.Measure.TooltipPosition.Top
            pieChart.TooltipBackgroundPaint = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#FFFFFF"))
            pieChart.TooltipTextPaint = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#333333"))

            mainContainer.Controls.Add(pieChart)

        Catch ex As Exception
            Console.WriteLine($"Error loading inventory status chart: {ex.Message}")
        End Try
    End Sub
    Private Sub LoadChartInterface()
        Try
            AreaChart.Controls.Clear()

            ' Header band: title on the left, margin KPI cards on the right
            _headerPanel = New Panel()
            _headerPanel.Dock = DockStyle.Top
            _headerPanel.Height = 58
            _headerPanel.BackColor = Color.Transparent

            titleLabel = New Label()
            titleLabel.Text = "Sales Overview"
            titleLabel.Font = New Font("Poppins", 14, FontStyle.Bold)
            titleLabel.ForeColor = Color.FromArgb(51, 51, 51)
            titleLabel.Dock = DockStyle.Fill
            titleLabel.TextAlign = ContentAlignment.MiddleLeft
            titleLabel.Padding = New Padding(28, 0, 0, 0)
            titleLabel.BackColor = Color.Transparent

            ' Chart panel with margins; keep the plot area as tall as possible
            chartPanel = New Panel()
            chartPanel.Dock = DockStyle.Fill
            chartPanel.Padding = New Padding(16, 20, 16, 46)
            chartPanel.BackColor = Color.White
            ApplyRoundedCorners(chartPanel, 18)

            ' Try to create LiveCharts CartesianChart
            Try
                salesChart = New CartesianChart()
                salesChart.Dock = DockStyle.Fill
                salesChart.Margin = New Padding(2)
                salesChart.LegendPosition = LiveChartsCore.Measure.LegendPosition.Top
                salesChart.LegendTextPaint = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#666666"))
                salesChart.LegendBackgroundPaint = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#FFFFFF"))
                salesChart.BackColor = Color.White
                chartPanel.Controls.Add(salesChart)
                ApplyRoundedCorners(salesChart, 14)
                Console.WriteLine("LiveCharts CartesianChart created successfully")
            Catch chartEx As Exception
                Console.WriteLine($"Failed to create LiveCharts: {chartEx.Message}")
                salesChart = Nothing
            End Try

            ' Add panels in proper order
            AreaChart.Controls.Add(chartPanel)
            AreaChart.Controls.Add(_headerPanel)

            _headerPanel.Controls.Add(titleLabel)

            ' Bring to front in proper order
            _headerPanel.BringToFront()
            chartPanel.BringToFront()

            LoadChartData(currentChartMode)
        Catch ex As Exception
            Console.WriteLine($"Error loading chart interface: {ex.Message}")
        End Try
    End Sub

    Private Sub LoadChartData(mode As String)
        Try
            currentChartMode = mode

            ' Build the continuous time-bucketed series (revenue vs cost of goods).
            Dim revenueData As New List(Of Double)()
            Dim cogsData As New List(Of Double)()
            Dim labels As New List(Of String)()
            Dim tooltipDates As New List(Of String)()
            BuildRevenueCostSeries(revenueData, cogsData, labels, tooltipDates)

            Dim dateArray As String() = tooltipDates.ToArray()

            ' Update the description header
            If titleLabel IsNot Nothing Then
                titleLabel.Text = "Revenue vs Cost of Goods" & "  ·  " & _selectedPeriod
            End If

            ' Check if salesChart is properly initialized before using it
            If salesChart IsNot Nothing Then
                Try
                    ' Set the chart series with LiveCharts: Revenue + Cost of Goods as filled lines.
                    ' XToolTipLabelFormatter shows the bucket date as the tooltip header.
                    salesChart.Series = {
                    New LineSeries(Of Double) With {
                        .Values = revenueData.ToArray(),
                        .Name = "Revenue",
                        .GeometrySize = 8,
                        .GeometryStroke = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#B57408"), 2),
                        .Fill = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#F59E0B").WithAlpha(40)),
                        .Stroke = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#F59E0B"), 4),
                        .LineSmoothness = 0.8,
                        .AnimationsSpeed = TimeSpan.FromMilliseconds(1500),
                        .XToolTipLabelFormatter = Function(point) If(point.Index >= 0 AndAlso point.Index < dateArray.Length, dateArray(point.Index), "")
                    },
                    New LineSeries(Of Double) With {
                        .Values = cogsData.ToArray(),
                        .Name = "Cost of Goods",
                        .GeometrySize = 8,
                        .GeometryStroke = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#7084A8"), 2),
                        .Fill = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#94A3B8").WithAlpha(40)),
                        .Stroke = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#94A3B8"), 4),
                        .LineSmoothness = 0.8,
                        .AnimationsSpeed = TimeSpan.FromMilliseconds(1500),
                        .XToolTipLabelFormatter = Function(point) If(point.Index >= 0 AndAlso point.Index < dateArray.Length, dateArray(point.Index), "")
                    }
                }

                    Dim combined As New List(Of Double)()
                    combined.AddRange(revenueData)
                    combined.AddRange(cogsData)
                    Dim maxValue As Double = If(combined.Count > 0, combined.Max(), 0)
                    Dim isZeroData As Boolean = maxValue <= 0
                    ' small negative floor so the 0-baseline fill never sits flush with the
                    ' x-axis label row (which otherwise looks 'cut')
                    Dim yAxisMin As Double = If(isZeroData, -2, -(Math.Max(maxValue * 0.05, 500)))
                    Dim yAxisMax As Double = If(isZeroData, 5, Math.Ceiling(maxValue * 1.2))

                    ' Configure axes
                    salesChart.XAxes = {
                    New Axis With {
                        .Labels = labels.ToArray(),
                        .TextSize = 12,
                        .Padding = New LiveChartsCore.Drawing.Padding(0),
                        .LabelsPaint = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#666666")),
                        .SeparatorsPaint = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#F1F0EC").WithAlpha(50)) With {
                            .StrokeThickness = 1
                        },
                        .AnimationsSpeed = TimeSpan.FromMilliseconds(1000)
                    }
                }

                    salesChart.YAxes = {
                    New Axis With {
                        .TextSize = 12,
                        .LabelsPaint = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#666666")),
                        .Position = LiveChartsCore.Measure.AxisPosition.Start,
                        .MinLimit = yAxisMin,
                        .MaxLimit = yAxisMax,
                        .SeparatorsPaint = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#F1F0EC").WithAlpha(40)) With {
                            .StrokeThickness = 1
                        },
                        .AnimationsSpeed = TimeSpan.FromMilliseconds(800),
                        .Labeler = Function(value)
                                       If isZeroData Then
                                           If Math.Abs(value) < 0.001 Then Return ChrW(&H20B1) & "0"
                                           Return ""
                                       End If
                                       Return ChrW(&H20B1) & $"{value / 1000.0:0.#}k"
                                   End Function
                    }
                }
                Catch chartEx As Exception
                    Console.WriteLine($"Error setting up LiveCharts: {chartEx.Message}")
                    ' Create fallback display
                    CreateFallbackChartDisplay(mode, revenueData, labels)
                End Try
            Else
                Console.WriteLine("salesChart is null, creating fallback display")
                ' Create fallback display
                CreateFallbackChartDisplay(mode, revenueData, labels)
            End If

        Catch ex As Exception
            Console.WriteLine($"Error loading chart data: {ex.Message}")
        End Try
    End Sub

    Private Sub BuildRevenueCostSeries(revenueData As List(Of Double), cogsData As List(Of Double), labels As List(Of String), tooltipDates As List(Of String))
        Try
            Dim now As DateTime = DateTime.Now
            Dim startDt As DateTime
            Dim endDt As DateTime = now

            Select Case _selectedPeriod
                Case "Today"
                    startDt = now.Date
                Case "Last 7 Days"
                    startDt = now.Date.AddDays(-6)
                    endDt = now.Date.AddDays(1).AddSeconds(-1)
                Case "Last 30 Days"
                    startDt = now.Date.AddDays(-29)
                    endDt = now.Date.AddDays(1).AddSeconds(-1)
                Case "This Month"
                    startDt = New Date(now.Year, now.Month, 1)
                Case "Last Month"
                    Dim lm As Date = now.AddMonths(-1)
                    startDt = New Date(lm.Year, lm.Month, 1)
                    endDt = startDt.AddMonths(1).AddSeconds(-1)
                Case "This Year"
                    startDt = New Date(now.Year, 1, 1)
                Case Else ' All Time
                    startDt = GetEarliestSaleDate()
            End Select

            If startDt > endDt Then startDt = endDt

            ' Determine granularity based on the range length
            Dim dayCount As Integer = CInt(Math.Floor((endDt - startDt).TotalDays))
            Dim granularity As String
            If _selectedPeriod = "Today" OrElse dayCount = 0 Then
                granularity = "hourly"
            ElseIf dayCount <= 31 Then
                granularity = "daily"
            ElseIf dayCount <= 180 Then
                granularity = "weekly"
            Else
                granularity = "monthly"
            End If

            ' Bucket key expression in SQL depends on granularity
            Dim keyExpr As String
            Select Case granularity
                Case "hourly"
                    keyExpr = "strftime('%Y-%m-%d %H', SaleDate)"
                Case "daily"
                    keyExpr = "date(SaleDate)"
                Case "weekly"
                    keyExpr = "date(SaleDate)" ' aggregated to ISO week in VB
                Case Else
                    keyExpr = "strftime('%Y-%m', SaleDate)"
            End Select

            ' Build the continuous bucket list + labels
            Dim buckets As New List(Of DateTime)()
            Select Case granularity
                Case "hourly"
                    Dim h As DateTime = startDt.Date.AddHours(startDt.Hour)
                    While h <= endDt
                        buckets.Add(h)
                        h = h.AddHours(1)
                    End While
                Case "daily"
                    Dim d As DateTime = startDt.Date
                    While d <= endDt.Date
                        buckets.Add(d)
                        d = d.AddDays(1)
                    End While
                Case "weekly"
                    ' Start on the Monday of the week containing startDt
                    Dim firstMon As DateTime = startDt.Date.AddDays(-((CInt(startDt.DayOfWeek) + 6) Mod 7))
                    Dim wk As DateTime = firstMon
                    While wk <= endDt.Date
                        buckets.Add(wk)
                        wk = wk.AddDays(7)
                    End While
                Case Else ' monthly
                    Dim first As DateTime = New Date(startDt.Year, startDt.Month, 1)
                    Dim m As DateTime = first
                    While m <= endDt
                        buckets.Add(m)
                        m = m.AddMonths(1)
                    End While
            End Select

            Dim startSql As String = startDt.ToString("yyyy-MM-dd HH:mm:ss")
            Dim endSql As String = endDt.ToString("yyyy-MM-dd HH:mm:ss")
            Dim parameters As SqlParameter() = {
                New SqlParameter("@S", startSql),
                New SqlParameter("@E", endSql)
            }

            ' Revenue per bucket (exclude aborted)
            Dim revenueQuery As String = "SELECT " & keyExpr & " AS k, IFNULL(SUM(TotalAmount),0) AS Rev " &
                "FROM Sales WHERE Status <> 'Aborted' AND SaleDate >= @S AND SaleDate <= @E GROUP BY k"
            Dim revenueByKey As New Dictionary(Of String, Double)()
            Using reader As DbDataReader = Utilities.ExecuteReader(revenueQuery, parameters)
                While reader.Read()
                    Dim k As String = Convert.ToString(reader("k"))
                    revenueByKey(k) = Convert.ToDouble(reader("Rev"))
                End While
            End Using

            ' COGS per bucket — join SaleItems x Products via Sales for the date
            Dim dailyKeyExpr As String = If(granularity = "weekly", "date(s.SaleDate)", keyExpr)
            Dim cogsQuery As String = "SELECT " & dailyKeyExpr & " AS k, IFNULL(SUM(si.Quantity * p.CostPrice),0) AS C " &
                "FROM SaleItems si " &
                "JOIN Sales s ON si.SaleID = s.SaleID " &
                "JOIN Products p ON si.ProductID = p.ProductID " &
                "WHERE s.Status <> 'Aborted' AND s.SaleDate >= @S AND s.SaleDate <= @E GROUP BY k"
            Dim cogsByKey As New Dictionary(Of String, Double)()
            Using reader As DbDataReader = Utilities.ExecuteReader(cogsQuery, parameters)
                While reader.Read()
                    Dim k As String = Convert.ToString(reader("k"))
                    cogsByKey(k) = Convert.ToDouble(reader("C"))
                End While
            End Using

            Dim bucketIdx As Integer = 0
            For Each b As DateTime In buckets
                Dim key As String
                Select Case granularity
                    Case "hourly"
                        key = b.ToString("yyyy-MM-dd HH")
                    Case "daily"
                        key = b.ToString("yyyy-MM-dd")
                    Case "weekly"
                        key = b.ToString("yyyy-MM-dd")
                    Case Else
                        key = b.ToString("yyyy-MM")
                End Select

                Dim rev As Double = If(revenueByKey.ContainsKey(key), revenueByKey(key), 0)

                ' For weekly we aggregate COGS across the 7 days of the bucket's week
                Dim cogs As Double = 0
                If granularity = "weekly" Then
                    For i As Integer = 0 To 6
                        Dim dayKey As String = b.AddDays(i).ToString("yyyy-MM-dd")
                        If cogsByKey.ContainsKey(dayKey) Then cogs += cogsByKey(dayKey)
                    Next
                Else
                    cogs = If(cogsByKey.ContainsKey(key), cogsByKey(key), 0)
                End If

                ' Build the axis label; for long ranges show a summarized subset
                Dim lbl As String = ""
                Select Case granularity
                    Case "hourly"
                        lbl = b.ToString("h tt")
                        tooltipDates.Add(b.ToString("ddd, MMM d · h tt"))
                    Case "daily"
                        tooltipDates.Add(b.ToString("ddd, MMM d, yyyy"))
                        ' summarize: keep ~4-5 labels across the run, full date is in tooltip
                        If dayCount <= 31 Then
                            Dim labelStep As Integer = If(dayCount > 14, 5, 3)
                            lbl = If(b.Day = 1 OrElse b.Day Mod labelStep = 0 OrElse bucketIdx = buckets.Count - 1, b.ToString("MMM d"), "")
                        Else
                            ' long daily range: label first-of-month days only
                            lbl = If(b.Day = 1, b.ToString("MMM yy"), "")
                        End If
                    Case "weekly"
                        tooltipDates.Add("Week of " & b.ToString("MMM d, yyyy"))
                        ' label the first week of each month and the last bucket
                        If b.Day <= 7 OrElse bucketIdx = buckets.Count - 1 Then
                            lbl = b.ToString("d MMM")
                        Else
                            lbl = ""
                        End If
                    Case Else ' monthly
                        tooltipDates.Add(b.ToString("MMMM yyyy"))
                        ' label January of each year, then every 3rd month after
                        If b.Month = 1 OrElse (b.Month - 1) Mod 3 = 0 OrElse bucketIdx = buckets.Count - 1 Then
                            lbl = b.ToString("MMM yy")
                        Else
                            lbl = ""
                        End If
                End Select
                labels.Add(lbl)

                revenueData.Add(rev)
                cogsData.Add(cogs)
                bucketIdx += 1
            Next

            If buckets.Count = 0 Then
                labels.Add(_selectedPeriod)
                tooltipDates.Add(_selectedPeriod)
                revenueData.Add(0)
                cogsData.Add(0)
            End If
        Catch ex As Exception
            Console.WriteLine($"Error building revenue/cost series: {ex.Message}")
        End Try
    End Sub

    Private Function GetEarliestSaleDate() As DateTime
        Try
            Using reader As DbDataReader = Utilities.ExecuteReader("SELECT MIN(SaleDate) AS MinSale FROM Sales", Nothing)
                If reader.Read() Then
                    Dim raw As Object = reader("MinSale")
                    If raw IsNot DBNull.Value AndAlso Not String.IsNullOrEmpty(Convert.ToString(raw)) Then
                        Dim parsed As DateTime
                        If DateTime.TryParse(Convert.ToString(raw), parsed) Then
                            Return parsed.Date
                        End If
                    End If
                End If
            End Using
        Catch
        End Try
        Return DateTime.Now.Date
    End Function

    Private Sub CreateFallbackChartDisplay(mode As String, salesData As List(Of Double), labels As List(Of String))
        Try
            ' Clear the chart panel and add a simple text display
            If chartPanel IsNot Nothing Then
                chartPanel.Controls.Clear()

                Dim fallbackLabel As New Label() With {
                .Text = $"Chart Mode: {mode}" & vbCrLf &
                       $"Data Points: {salesData.Count}" & vbCrLf &
                       $"Total Sales: ?{If(salesData.Count > 0, salesData.Sum(), 0):N2}" & vbCrLf &
                       "LiveCharts not available - using fallback display",
                .Font = New Font("Poppins", 12, FontStyle.Regular),
                .ForeColor = Color.FromArgb(51, 51, 51),
                .BackColor = Color.Transparent,
                .AutoSize = False,
                .Size = New Size(400, 200),
                .Location = New Point(50, 50),
                .TextAlign = ContentAlignment.MiddleCenter
            }

                chartPanel.Controls.Add(fallbackLabel)
            End If
        Catch ex As Exception
            Console.WriteLine($"Error creating fallback display: {ex.Message}")
        End Try
    End Sub
    Private Function GetMonthAbbreviation(month As Integer) As String
        Dim monthNames As String() = {"JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"}
        Return monthNames(month - 1)
    End Function
    Private Sub LoadMonthlyData(salesData As List(Of Double), revenueData As List(Of Double), labels As List(Of String))
        Dim currentYear As Integer = DateTime.Now.Year

        ' Single query for all 12 months
        Dim query As String = "
            SELECT CAST(strftime('%m', SaleDate) AS INTEGER) AS MonthNum,
                   IFNULL(SUM(TotalAmount), 0) AS Revenue
            FROM Sales
            WHERE CAST(strftime('%Y', SaleDate) AS INTEGER) = @Year
            GROUP BY MonthNum
            ORDER BY MonthNum"

        Dim parameters As SqlParameter() = {
            New SqlParameter("@Year", currentYear)
        }

        Dim monthlyData As New Dictionary(Of Integer, Double)()
        For m As Integer = 1 To 12
            monthlyData(m) = 0
        Next

        Using reader As DbDataReader = Utilities.ExecuteReader(query, parameters)
            While reader.Read()
                Dim monthNum As Integer = Convert.ToInt32(reader("MonthNum"))
                monthlyData(monthNum) = Convert.ToDouble(reader("Revenue"))
            End While
        End Using

        For month As Integer = 1 To 12
            labels.Add(GetMonthAbbreviation(month))
            Dim rev As Double = monthlyData(month)
            salesData.Add(rev)
            revenueData.Add(rev)
        Next
    End Sub

    Private Sub LoadWeeklyData(salesData As List(Of Double), revenueData As List(Of Double), labels As List(Of String))
        Dim dayNames As String() = {"Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"}
        Dim startDate As DateTime = DateTime.Now.AddDays(-6).Date

        ' Single query for last 7 days
        Dim query As String = "
            SELECT DATE(SaleDate) AS SaleDay,
                   IFNULL(SUM(TotalAmount), 0) AS Revenue
            FROM Sales
            WHERE DATE(SaleDate) >= @StartDate
            GROUP BY SaleDay
            ORDER BY SaleDay"

        Dim parameters As SqlParameter() = {
            New SqlParameter("@StartDate", startDate)
        }

        Dim dailyData As New Dictionary(Of String, Double)()
        Using reader As DbDataReader = Utilities.ExecuteReader(query, parameters)
            While reader.Read()
                Dim day As String = Convert.ToDateTime(reader("SaleDay")).ToString("yyyy-MM-dd")
                dailyData(day) = Convert.ToDouble(reader("Revenue"))
            End While
        End Using

        For i As Integer = 6 To 0 Step -1
            Dim targetDate As DateTime = DateTime.Now.AddDays(-i).Date
            labels.Add(dayNames(CInt(targetDate.DayOfWeek)))
            Dim key As String = targetDate.ToString("yyyy-MM-dd")
            Dim rev As Double = If(dailyData.ContainsKey(key), dailyData(key), 0)
            salesData.Add(rev)
            revenueData.Add(rev)
        Next
    End Sub

    Private Sub LoadDailyData(salesData As List(Of Double), revenueData As List(Of Double), labels As List(Of String))
        Dim startDate = DateTime.Today.AddDays(-11)

        ' Single query for last 12 days
        Dim query As String = "
            SELECT DATE(SaleDate) AS SaleDay,
                   IFNULL(SUM(TotalAmount), 0) AS Revenue
            FROM Sales
            WHERE DATE(SaleDate) >= @StartDate
            GROUP BY SaleDay
            ORDER BY SaleDay"

        Dim parameters As SqlParameter() = {
            New SqlParameter("@StartDate", startDate)
        }

        Dim dailyData As New Dictionary(Of String, Double)()
        Using reader As DbDataReader = Utilities.ExecuteReader(query, parameters)
            While reader.Read()
                Dim day As String = Convert.ToDateTime(reader("SaleDay")).ToString("yyyy-MM-dd")
                dailyData(day) = Convert.ToDouble(reader("Revenue"))
            End While
        End Using

        For i As Integer = 0 To 11
            Dim targetDate As DateTime = startDate.AddDays(i)
            labels.Add(targetDate.ToString("dd MMM"))
            Dim key As String = targetDate.ToString("yyyy-MM-dd")
            Dim rev As Double = If(dailyData.ContainsKey(key), dailyData(key), 0)
            salesData.Add(rev)
            revenueData.Add(rev)
        Next
    End Sub

    Private Sub LoadYearlyData(salesData As List(Of Double), revenueData As List(Of Double), labels As List(Of String))
        Dim startYear As Integer = DateTime.Now.Year - 5

        ' Single query for 6 years
        Dim query As String = "
            SELECT CAST(strftime('%Y', SaleDate) AS INTEGER) AS SaleYear,
                   IFNULL(SUM(TotalAmount), 0) AS Revenue
            FROM Sales
            WHERE CAST(strftime('%Y', SaleDate) AS INTEGER) >= @StartYear
            GROUP BY SaleYear
            ORDER BY SaleYear"

        Dim parameters As SqlParameter() = {
            New SqlParameter("@StartYear", startYear)
        }

        Dim yearlyData As New Dictionary(Of Integer, Double)()
        Using reader As DbDataReader = Utilities.ExecuteReader(query, parameters)
            While reader.Read()
                Dim yr As Integer = Convert.ToInt32(reader("SaleYear"))
                yearlyData(yr) = Convert.ToDouble(reader("Revenue"))
            End While
        End Using

        For year As Integer = startYear To DateTime.Now.Year
            labels.Add(year.ToString())
            Dim rev As Double = If(yearlyData.ContainsKey(year), yearlyData(year), 0)
            salesData.Add(rev)
            revenueData.Add(rev)
        Next
    End Sub

    Private Sub LoadAllTimeData(salesData As List(Of Double), revenueData As List(Of Double), labels As List(Of String))
        Dim query As String = "
            SELECT CAST(strftime('%Y', SaleDate) AS INTEGER) AS SaleYear, IFNULL(SUM(TotalAmount), 0) AS Revenue
            FROM Sales
            GROUP BY SaleYear
            ORDER BY SaleYear"

        Using reader As DbDataReader = Utilities.ExecuteReader(query, Nothing)
            While reader.Read()
                labels.Add(reader("SaleYear").ToString())
                Dim revenue As Double = Convert.ToDouble(reader("Revenue"))
                salesData.Add(revenue)
                revenueData.Add(revenue)
            End While
        End Using

        If labels.Count = 0 Then
            labels.Add("All Time")
            salesData.Add(0)
            revenueData.Add(0)
        End If
    End Sub

    Private Function TryGetCompanyOperatingHours(ByRef opening As TimeSpan, ByRef closing As TimeSpan) As Boolean
        opening = TimeSpan.FromHours(9)
        closing = TimeSpan.FromHours(17)

        Try
            Dim rawHours As String = CompanySettingsManager.Instance.GetSettingString("CompanyHours", "")
            If String.IsNullOrWhiteSpace(rawHours) Then
                Return False
            End If

            Dim lines = rawHours.Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            Dim hasOpening As Boolean = False
            Dim hasClosing As Boolean = False

            For Each line In lines
                If line.StartsWith("Opening:", StringComparison.OrdinalIgnoreCase) Then
                    Dim timePart = line.Substring(8).Trim()
                    Dim parsed As DateTime
                    If DateTime.TryParse(timePart, parsed) Then
                        opening = parsed.TimeOfDay
                        hasOpening = True
                    End If
                ElseIf line.StartsWith("Closing:", StringComparison.OrdinalIgnoreCase) Then
                    Dim timePart = line.Substring(8).Trim()
                    Dim parsed As DateTime
                    If DateTime.TryParse(timePart, parsed) Then
                        closing = parsed.TimeOfDay
                        hasClosing = True
                    End If
                End If
            Next

            Return hasOpening AndAlso hasClosing
        Catch
            Return False
        End Try
    End Function

    Private Sub LoadTodayData(salesData As List(Of Double), revenueData As List(Of Double), labels As List(Of String))
        Dim opening As TimeSpan
        Dim closing As TimeSpan
        TryGetCompanyOperatingHours(opening, closing)

        Dim startDateTime As DateTime = Date.Today.Add(opening)
        Dim endDateTime As DateTime = Date.Today.Add(closing)

        If endDateTime <= startDateTime Then
            endDateTime = endDateTime.AddDays(1)
        End If

        ' Single query grouped by hour
        Dim query As String = "
            SELECT CAST(strftime('%H', SaleDate) AS INTEGER) AS HourNum,
                   IFNULL(SUM(TotalAmount), 0) AS Revenue
            FROM Sales
            WHERE SaleDate >= @StartDateTime AND SaleDate < @EndDateTime
            GROUP BY HourNum
            ORDER BY HourNum"

        Dim parameters As SqlParameter() = {
            New SqlParameter("@StartDateTime", startDateTime),
            New SqlParameter("@EndDateTime", endDateTime)
        }

        Dim hourlyData As New Dictionary(Of Integer, Double)()
        Using reader As DbDataReader = Utilities.ExecuteReader(query, parameters)
            While reader.Read()
                Dim hr As Integer = Convert.ToInt32(reader("HourNum"))
                hourlyData(hr) = Convert.ToDouble(reader("Revenue"))
            End While
        End Using

        Dim slotStart As DateTime = startDateTime
        While slotStart < endDateTime
            Dim slotEnd As DateTime = slotStart.AddHours(1)
            If slotEnd > endDateTime Then
                slotEnd = endDateTime
            End If

            labels.Add(slotStart.ToString("hh tt"))
            Dim hr As Integer = slotStart.Hour
            Dim rev As Double = If(hourlyData.ContainsKey(hr), hourlyData(hr), 0)
            salesData.Add(rev)
            revenueData.Add(rev)

            slotStart = slotEnd
        End While

        If labels.Count = 0 Then
            labels.Add("Today")
            salesData.Add(0)
            revenueData.Add(0)
        End If
    End Sub

    Private Sub CreateNavigationMenu()
        NavigationBuilder.Build(DashboardPanel, Me, "Dashboard")
    End Sub

    Private Sub CreateUserInfoSection()
        Try
            ' Remove existing tiny panel to avoid duplicates
            For Each c In DashboardPanel.Controls.OfType(Of Guna.UI2.WinForms.Guna2Panel)().ToArray()
                If c.Name = "tinyUserInfoPanel" Then
                    DashboardPanel.Controls.Remove(c)
                    c.Dispose()
                End If
            Next

            ' Determine placement under logout button (fallback to bottom-left)
            Dim panelWidth As Integer = 140
            Dim panelHeight As Integer = 55
            Dim panelX As Integer = 20
            Dim panelY As Integer = DashboardPanel.Height - panelHeight - 20

            If navLogoutBtn IsNot Nothing Then
                panelX = navLogoutBtn.Location.X + 10
                panelY = navLogoutBtn.Location.Y + navLogoutBtn.Height + 8
                If panelX + panelWidth > DashboardPanel.Width - 20 Then
                    panelX = Math.Max(20, DashboardPanel.Width - 20 - panelWidth)
                End If
            End If

            ' Tiny user info panel
            Dim tinyPanel As New Guna.UI2.WinForms.Guna2Panel() With {
            .Name = "tinyUserInfoPanel",
            .Size = New Size(panelWidth, panelHeight),
            .Location = New Point(panelX + 10, panelY),
            .FillColor = Color.FromArgb(250, 250, 249),
            .BorderRadius = 8,
            .BackColor = Color.FromArgb(250, 250, 249)
        }

            ' Small avatar picture box
            Dim avatarSize As Integer = 30
            Dim avatar As New PictureBox() With {
            .Size = New Size(avatarSize, avatarSize),
            .Location = New Point(6, (panelHeight - avatarSize) \ 2),
            .SizeMode = PictureBoxSizeMode.Zoom,
            .BackColor = Color.Transparent
        }

            ' Create default avatar from shared resource
            Dim username As String = If(String.IsNullOrEmpty(frmLoginvb.LoggedInUsername), "U", frmLoginvb.LoggedInUsername)
            Try
                avatar.Image = New Bitmap(My.Resources.avatar_default_svgrepo_com)
            Catch
                ' fallback: plain background
                avatar.BackColor = Color.FromArgb(200, 200, 200)
            End Try

            ' Username label (compact) - enable ellipsis when text is too long
            Dim userLabel As New Label() With {
            .AutoSize = False,
            .Size = New Size(panelWidth - avatar.Width - 14, 18),
            .Location = New Point(avatar.Right + 6, 6),
            .Text = username,
            .Font = New Font("Poppins", 9.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(51, 51, 51),
            .BackColor = Color.Transparent,
            .AutoEllipsis = True
        }

            ' Role / subtitle (very small)
            Dim roleLabel As New Label() With {
            .AutoSize = False,
            .Size = New Size(panelWidth - avatar.Width - 14, 20),
            .Location = New Point(avatar.Right + 6, userLabel.Bottom - 2),
            .Text = If(String.IsNullOrEmpty(frmLoginvb.LoggedInRole), "", frmLoginvb.LoggedInRole),
            .Font = New Font("Poppins", 8.0F, FontStyle.Regular),
            .ForeColor = Color.FromArgb(102, 102, 102),
            .BackColor = Color.Transparent
        }

            ' Add a tooltip when username is long (user examples: 10+ chars)
            If Not String.IsNullOrEmpty(username) AndAlso username.Length > 10 Then
                Dim tt As New ToolTip()
                tt.AutoPopDelay = 5000
                tt.InitialDelay = 300
                tt.ReshowDelay = 100
                tt.ShowAlways = True
                tt.SetToolTip(userLabel, username)
            End If

            tinyPanel.Controls.Add(avatar)
            tinyPanel.Controls.Add(userLabel)
            tinyPanel.Controls.Add(roleLabel)

            DashboardPanel.Controls.Add(tinyPanel)
            tinyPanel.BringToFront()

        Catch ex As Exception
            Console.WriteLine($"Error creating tiny user info: {ex.Message}")
        End Try
    End Sub

    Private Sub NavLogout_Click(sender As Object, e As EventArgs)
        ' Confirm logout
        Dim result As DialogResult = MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

        If result = DialogResult.Yes Then
            ' Clear user session
            frmLoginvb.LogoutUser()

            ' Navigate to login
            isNavigating = True
            Me.Close()
            Dim loginForm As New frmLoginvb()
            loginForm.Show()
        End If
    End Sub
    Private Function CreateLargeNavButton(text As String, yPosition As Integer, isActive As Boolean, buttonWidth As Integer, buttonHeight As Integer) As Guna.UI2.WinForms.Guna2Button
        Dim btn As New Guna.UI2.WinForms.Guna2Button()
        btn.Text = text
        btn.Size = New System.Drawing.Size(buttonWidth, buttonHeight)
        btn.Location = New Point(20, yPosition)
        btn.BorderRadius = 12
        btn.Font = New Font("Poppins", 10, FontStyle.Regular)
        btn.TextAlign = HorizontalAlignment.Left

        btn.FillColor = If(isActive, System.Drawing.Color.FromArgb(254, 191, 16), System.Drawing.Color.Transparent)
        btn.ForeColor = If(isActive, System.Drawing.Color.FromArgb(51, 51, 51), System.Drawing.Color.FromArgb(51, 51, 51))
        btn.BorderThickness = If(isActive, 0, 1)
        btn.BorderColor = If(isActive, System.Drawing.Color.Transparent, System.Drawing.Color.FromArgb(200, 200, 200))
        btn.BackColor = System.Drawing.Color.Transparent
        btn.Cursor = Cursors.Hand

        btn.ShadowDecoration.Enabled = True
        btn.ShadowDecoration.Color = System.Drawing.Color.FromArgb(200, 200, 200)
        btn.ShadowDecoration.Depth = 2

        AddHandler btn.MouseEnter, Sub()
                                       If Not isActive Then
                                           btn.FillColor = System.Drawing.Color.FromArgb(240, 240, 240)
                                           btn.BorderColor = System.Drawing.Color.FromArgb(254, 191, 16)
                                           btn.Font = New Font("Poppins", 9, FontStyle.Bold)
                                       End If
                                   End Sub
        AddHandler btn.MouseLeave, Sub()
                                       If Not isActive Then
                                           btn.FillColor = System.Drawing.Color.Transparent
                                           btn.BorderColor = System.Drawing.Color.FromArgb(200, 200, 200)
                                           btn.Font = New Font("Poppins", 10, FontStyle.Regular)
                                       End If
                                   End Sub

        DashboardPanel.Controls.Add(btn)
        Return btn
    End Function
    Private Sub NavDashboard_Click(sender As Object, e As EventArgs)
    End Sub

    Private Sub NavInventory_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Inventory.Show()
        Me.Close()
    End Sub

    Private Sub NavPOS_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Sales.Show()
        Me.Close()
    End Sub

    Private Sub NavSalesRecords_Click(sender As Object, e As EventArgs)
        isNavigating = True
        SalesRecord.Show()
        Me.Close()
    End Sub

    Private Sub NavStaff_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Staff.Show()
        Me.Close()
    End Sub

    Private Sub NavInventoryLog_Click(sender As Object, e As EventArgs)
        isNavigating = True
        InventoryLog.Show()
        Me.Close()
    End Sub

    Private Sub NavAuditLog_Click(sender As Object, e As EventArgs)
        isNavigating = True
        AuditLog.Show()
        Me.Close()
    End Sub

    Private Sub NavSuppliers_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Supplier.Show()
        Me.Close()
    End Sub

    Private Sub NavSystemSettings_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Sys.Show()
        Me.Close()
    End Sub

    Private Sub InitializeProductSearch()
        Try
            ' Set search textbox position and styling once
            txtProductSearch.Location = New Point(420, 10)
            txtProductSearch.Size = New Size(200, 20)
            txtProductSearch.BorderRadius = 10
            txtProductSearch.BackColor = Color.Transparent
            txtProductSearch.BorderThickness = 1
            txtProductSearch.PlaceholderText = Char.ConvertFromUtf32(&H1F50D) & " Search products..."
            txtProductSearch.Font = New Font("Poppins", 10.0F)
            txtProductSearch.ForeColor = Color.FromArgb(51, 51, 51)
            txtProductSearch.BackColor = Color.FromArgb(245, 245, 245)
            txtProductSearch.BorderRadius = 10

            ' Add event handler for search
            AddHandler txtProductSearch.TextChanged, AddressOf TxtProductSearch_TextChanged
        Catch ex As Exception
            Console.WriteLine($"Error initializing product search: {ex.Message}")
        End Try
    End Sub
    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Escape Then
            ' If a modal dialog owned by this form is visible, do not show EscForm
            If Me.OwnedForms.Cast(Of Form)().Any(Function(f) f.Visible) Then
                Return MyBase.ProcessCmdKey(msg, keyData)
            End If

            ' Only handle when this form contains focus
            If Not Me.ContainsFocus Then
                Return MyBase.ProcessCmdKey(msg, keyData)
            End If

            If isNavigating Then
                Return True
            End If

            Dim result As DialogResult = EscForm.ConfirmExit(Me)
            Me.Activate()
            If result = DialogResult.Yes Then
                If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Application Exit", "User exited the application via Dashboard.")
                End If

                For Each form As Form In Application.OpenForms.Cast(Of Form).ToArray()
                    If form IsNot Me Then
                        form.Close()
                    End If
                Next

                Application.Exit()
            End If

            Return True
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Private Sub BuildKPICards()
        ' === Header row with title + time-period filter ===
        Dim headerPanel As New Panel() With {
            .Location = New Point(236, 40), .Size = New Size(1636, 52),
            .BackColor = Color.Transparent
        }
        Dim headerTitle As New Label() With {
            .Text = "Dashboard Overview", .Font = New Font("Poppins", 15, FontStyle.Regular),
            .ForeColor = Color.FromArgb(34, 34, 34), .BackColor = Color.Transparent,
            .Location = New Point(0, 14), .AutoSize = True
        }
        headerPanel.Controls.Add(headerTitle)

        Dim filterLabel As New Label() With {
            .Text = "Period:", .Font = New Font("Poppins", 9.5F, FontStyle.Regular),
            .ForeColor = Color.FromArgb(100, 100, 100), .BackColor = Color.Transparent,
            .AutoSize = True, .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
            .Location = New Point(headerPanel.Width - 290, 14)
        }
        headerPanel.Controls.Add(filterLabel)

        _periodCombo = New Guna2ComboBox() With {
            .Size = New Size(210, 36),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
            .Location = New Point(headerPanel.Width - 210, 8),
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Font = New Font("Poppins", 10, FontStyle.Regular),
            .FillColor = Color.White,
            .BorderColor = Color.FromArgb(218, 218, 218),
            .BorderThickness = 1,
            .BorderRadius = 8,
            .Cursor = Cursors.Hand,
            .TextAlign = HorizontalAlignment.Left
        }
        _periodCombo.Items.AddRange({"Today", "Last 7 Days", "Last 30 Days", "This Month", "Last Month", "This Year", "All Time"})
        _periodCombo.SelectedIndex = 2 ' Last 30 Days default
        AddHandler _periodCombo.SelectedIndexChanged, AddressOf OnPeriodChanged
        headerPanel.Controls.Add(_periodCombo)

        Me.Controls.Add(headerPanel)
        headerPanel.BringToFront()

        ' === KPI Cards ===
        Dim cardY As Integer = 100
        Dim cardH As Integer = 150
        Dim startX As Integer = 236
        Dim totalWidth As Integer = 1636
        Dim cardW As Integer = 354
        Dim cardGap As Integer = (totalWidth - (cardW * 4)) \ 3

        Dim cardBg As Color = Color.FromArgb(255, 253, 248)
        Dim cardBorder As Color = Color.FromArgb(232, 232, 232)
        Dim hoverBg As Color = Color.FromArgb(255, 254, 249)
        Dim hoverBorder As Color = Color.FromArgb(196, 154, 44)

        ' Card 1: Total Orders
        Dim card1 As New Guna2Panel() With {
            .Location = New Point(startX, cardY), .Size = New Size(cardW, cardH),
            .FillColor = cardBg, .BorderRadius = 12, .BorderThickness = 2,
            .BorderColor = cardBorder, .Cursor = Cursors.Hand
        }
        card1.ShadowDecoration.Enabled = False
        AddHandler card1.Paint, Sub(s, ev)
                                    DrawCircleWithEmoji(ev.Graphics, 28, (cardH - 52) \ 2, 52, EmojiChart, Color.FromArgb(196, 154, 44))
                                End Sub
        card1.Controls.Add(New Label() With {.Text = "TOTAL ORDERS", .Font = New Font("Poppins", 8.0F, FontStyle.Regular), .ForeColor = Color.FromArgb(119, 119, 119), .BackColor = Color.Transparent, .Location = New Point(96, 20), .AutoSize = True})
        _lblTotalOrdersValue = New Label() With {.Text = "0", .Font = New Font("Poppins", 22.0F, FontStyle.Regular), .ForeColor = Color.FromArgb(34, 34, 34), .BackColor = Color.Transparent, .Location = New Point(94, 46), .AutoSize = True}
        card1.Controls.Add(_lblTotalOrdersValue)
        _lblTotalOrdersSub = New Label() With {.Text = "All recorded orders", .Font = New Font("Poppins", 8.0F, FontStyle.Regular), .ForeColor = Color.FromArgb(46, 125, 50), .BackColor = Color.Transparent, .Location = New Point(98, cardH - 28), .AutoSize = True}
        card1.Controls.Add(_lblTotalOrdersSub)

        ' Card 2: Stock Value
        Dim card2 As New Guna2Panel() With {
            .Location = New Point(startX + cardW + cardGap, cardY), .Size = New Size(cardW, cardH),
            .FillColor = cardBg, .BorderRadius = 12, .BorderThickness = 2,
            .BorderColor = cardBorder, .Cursor = Cursors.Hand
        }
        card2.ShadowDecoration.Enabled = False
        AddHandler card2.Paint, Sub(s, ev)
                                    DrawCircleWithEmoji(ev.Graphics, 28, (cardH - 52) \ 2, 52, EmojiMoney, Color.FromArgb(255, 152, 0))
                                End Sub
        card2.Controls.Add(New Label() With {.Text = "STOCK VALUE", .Font = New Font("Poppins", 8.0F, FontStyle.Regular), .ForeColor = Color.FromArgb(119, 119, 119), .BackColor = Color.Transparent, .Location = New Point(96, 20), .AutoSize = True})
        _lblStockValueValue = New Label() With {.Text = ChrW(&H20B1) & "0", .Font = New Font("Poppins", 22.0F, FontStyle.Regular), .ForeColor = Color.FromArgb(34, 34, 34), .BackColor = Color.Transparent, .Location = New Point(94, 46), .AutoSize = True}
        card2.Controls.Add(_lblStockValueValue)
        _lblStockValueSub = New Label() With {.Text = "Active products", .Font = New Font("Poppins", 8.0F, FontStyle.Regular), .ForeColor = Color.FromArgb(46, 125, 50), .BackColor = Color.Transparent, .Location = New Point(98, cardH - 28), .AutoSize = True}
        card2.Controls.Add(_lblStockValueSub)

        ' Card 3: Total Revenue
        Dim card3 As New Guna2Panel() With {
            .Location = New Point(startX + (cardW + cardGap) * 2, cardY), .Size = New Size(cardW, cardH),
            .FillColor = cardBg, .BorderRadius = 12, .BorderThickness = 2,
            .BorderColor = cardBorder, .Cursor = Cursors.Hand
        }
        card3.ShadowDecoration.Enabled = False
        AddHandler card3.Paint, Sub(s, ev)
                                    DrawCircleWithEmoji(ev.Graphics, 28, (cardH - 52) \ 2, 52, EmojiTrend, Color.FromArgb(46, 125, 50))
                                End Sub
        card3.Controls.Add(New Label() With {.Text = "TOTAL REVENUE", .Font = New Font("Poppins", 8.0F, FontStyle.Regular), .ForeColor = Color.FromArgb(119, 119, 119), .BackColor = Color.Transparent, .Location = New Point(96, 20), .AutoSize = True})
        _lblRevenueValue = New Label() With {.Text = ChrW(&H20B1) & "0", .Font = New Font("Poppins", 22.0F, FontStyle.Regular), .ForeColor = Color.FromArgb(34, 34, 34), .BackColor = Color.Transparent, .Location = New Point(94, 46), .AutoSize = True}
        card3.Controls.Add(_lblRevenueValue)
        _lblRevenueSub = New Label() With {.Text = "All recorded sales", .Font = New Font("Poppins", 8.0F, FontStyle.Regular), .ForeColor = Color.FromArgb(119, 119, 119), .BackColor = Color.Transparent, .Location = New Point(98, cardH - 28), .AutoSize = True}
        card3.Controls.Add(_lblRevenueSub)

        ' Card 4: Gross Profit
        Dim card4 As New Guna2Panel() With {
            .Location = New Point(startX + (cardW + cardGap) * 3, cardY), .Size = New Size(cardW, cardH),
            .FillColor = cardBg, .BorderRadius = 12, .BorderThickness = 2,
            .BorderColor = cardBorder, .Cursor = Cursors.Hand
        }
        card4.ShadowDecoration.Enabled = False
        AddHandler card4.Paint, Sub(s, ev)
                                    DrawCircleWithEmoji(ev.Graphics, 28, (cardH - 52) \ 2, 52, EmojiProfit, Color.FromArgb(232, 176, 76))
                                End Sub
        card4.Controls.Add(New Label() With {.Text = "GROSS PROFIT", .Font = New Font("Poppins", 8.0F, FontStyle.Regular), .ForeColor = Color.FromArgb(119, 119, 119), .BackColor = Color.Transparent, .Location = New Point(96, 20), .AutoSize = True})
        _lblGrossProfitValue = New Label() With {.Text = ChrW(&H20B1) & "0", .Font = New Font("Poppins", 22.0F, FontStyle.Regular), .ForeColor = Color.FromArgb(34, 34, 34), .BackColor = Color.Transparent, .Location = New Point(94, 46), .AutoSize = True}
        card4.Controls.Add(_lblGrossProfitValue)
        _lblGrossProfitSub = New Label() With {.Text = "Revenue minus COGS", .Font = New Font("Poppins", 8.0F, FontStyle.Regular), .ForeColor = Color.FromArgb(119, 119, 119), .BackColor = Color.Transparent, .Location = New Point(98, cardH - 28), .AutoSize = True}
        card4.Controls.Add(_lblGrossProfitSub)

        ' Add hover handlers
        For Each card As Guna2Panel In {card1, card2, card3, card4}
            Dim c As Color = cardBg
            Dim hb As Color = hoverBg
            Dim hc As Color = hoverBorder
            AddHandler card.MouseEnter, Sub(s, e)
                                            card.FillColor = hb
                                            card.BorderColor = hc
                                        End Sub
            AddHandler card.MouseLeave, Sub(s, e)
                                            card.FillColor = c
                                            card.BorderColor = cardBorder
                                        End Sub
        Next

        Me.Controls.Add(card4)
        Me.Controls.Add(card3)
        Me.Controls.Add(card2)
        Me.Controls.Add(card1)
        card1.BringToFront()
        card2.BringToFront()
        card3.BringToFront()
        card4.BringToFront()

        _kpiCards.AddRange({card1, card2, card3, card4})
    End Sub

    Private Sub DrawCircleWithEmoji(g As Graphics, x As Integer, y As Integer, diameter As Integer, emoji As String, emojiColor As Color)
        Try
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit

            Using brush As New SolidBrush(CircleBg)
                g.FillEllipse(brush, x, y, diameter, diameter)
            End Using

            Dim emojiFontName As String = ResolveEmojiFontFamily()
            Using f As New Font(emojiFontName, 16.0F, FontStyle.Regular)
                Dim sz As Size = TextRenderer.MeasureText(emoji, f)
                Dim ex As Integer = x + (diameter - sz.Width) \ 2 + 4
                Dim ey As Integer = y + (diameter - sz.Height) \ 2
                TextRenderer.DrawText(g, emoji, f, New Point(ex, ey), emojiColor)
            End Using
        Catch
        End Try
    End Sub

    Private Function ResolveEmojiFontFamily() As String
        Try
            Dim installed As New HashSet(Of String)()
            For Each family As FontFamily In System.Drawing.FontFamily.Families
                installed.Add(family.Name)
            Next
            For Each name As String In {"Segoe UI Emoji", "Segoe UI Symbol", "Noto Color Emoji", "Arial"}
                If installed.Contains(name) Then Return name
            Next
        Catch
        End Try
        Return "Segoe UI"
    End Function
End Class