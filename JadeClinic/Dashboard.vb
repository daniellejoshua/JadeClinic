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
    Private btnMonthly As Guna2Button
    Private btnDaily As Guna2Button
    Private btnWeekly As Guna2Button
    Private btnExport As Guna2Button
    Private titleLabel As Label
    Private legendPanel As Panel
    Private chartPanel As Panel
    Private lastProductCount As Integer = -1
    Private btnAll As Guna2Button
    ' Navigation flag to prevent exit confirmation on programmatic close
    Private isNavigating As Boolean = False
    Private salesChart As CartesianChart
    Private btnYearly As Guna2Button
    Private btnToday As Guna2Button
    Private btnAllTime As Guna2Button
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
        If DashboardPanel IsNot Nothing Then DashboardPanel.ShadowDecoration.Enabled = True
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
            Me.BackColor = Color.White
            Me.MaximizeBox = False
            Me.MinimizeBox = False

            ' Improved full-screen behavior: remove window chrome and cover the entire screen including taskbar
            Me.FormBorderStyle = FormBorderStyle.None
            Me.TopMost = True
            Me.WindowState = FormWindowState.Normal
            Me.Bounds = Screen.PrimaryScreen.Bounds
            Me.WindowState = FormWindowState.Maximized
            Console.WriteLine("Basic form properties set (full screen)")

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

            InitializeProductSearch()
            Console.WriteLine("Product search initialized")

            LoadDashboardData()
            Console.WriteLine("Dashboard data loaded")

            UpdateMonthlyStockTrend()
            Console.WriteLine("Monthly stock trend updated")

            LoadChartData("Today")
            Console.WriteLine("Chart data loaded")

            ' Initialize chart filter buttons if they exist
            If btnAll IsNot Nothing Then
                btnAll.FillColor = Color.FromArgb(240, 240, 240)
                btnAll.ForeColor = Color.FromArgb(51, 51, 51)
            End If
            If btnMonthly IsNot Nothing Then
                btnMonthly.FillColor = Color.FromArgb(240, 240, 240)
                btnMonthly.ForeColor = Color.FromArgb(51, 51, 51)
            End If
            If btnWeekly IsNot Nothing Then
                btnWeekly.FillColor = Color.FromArgb(254, 191, 16)
                btnWeekly.ForeColor = Color.FromArgb(51, 51, 51)
            End If
            If btnDaily IsNot Nothing Then
                btnDaily.FillColor = Color.FromArgb(240, 240, 240)
                btnDaily.ForeColor = Color.FromArgb(51, 51, 51)
            End If
            SetActiveChartButton(currentChartMode)
            Console.WriteLine("Filter buttons initialized")

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
        Guna2CircleButton5.TabIndex = 1
        Guna2CircleButton6.TabIndex = 2
        Guna2CircleButton7.TabIndex = 3
        Utilities.ApplyInputFocusEffects(Me)
    End Sub

    Private Sub UpdateMonthlyStockTrend()
        ' Placeholder for monthly trend updates
        ' This can be expanded later when LiveCharts is properly configured
    End Sub
    Private Sub SetActiveChartButton(mode As String)
        Dim buttons = New Dictionary(Of String, Guna2Button) From {
        {"Yearly", btnYearly},
        {"Monthly", btnMonthly},
        {"Weekly", btnWeekly},
        {"Today", btnToday}
    }

        For Each kv In buttons
            If kv.Value Is Nothing Then Continue For

            Dim active = kv.Key.Equals(mode, StringComparison.OrdinalIgnoreCase)
            kv.Value.FillColor = If(active, Color.FromArgb(254, 191, 16), Color.FromArgb(240, 240, 240))
            kv.Value.ForeColor = If(active, Color.FromArgb(51, 51, 51), Color.FromArgb(51, 51, 51))
        Next
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

            ' Update main form background
            Me.BackColor = Color.White
            Console.WriteLine("Form background updated")

            ' Update main statistic panels with new color scheme - add null checks

            ' DailySalesPanel - First statistics card
            If DailySalesPanel IsNot Nothing Then
                DailySalesPanel.FillColor = Color.FromArgb(250, 250, 249)
                DailySalesPanel.BorderColor = Color.FromArgb(246, 245, 242)
                DailySalesPanel.BorderThickness = 2
                Console.WriteLine("DailySalesPanel updated")

                ' Update labels in DailySalesPanel
                UpdatePanelLabels(DailySalesPanel)
            Else
                Console.WriteLine("DailySalesPanel is null")
            End If

            ' Guna2Panel6 - Second statistics card  
            If Guna2Panel6 IsNot Nothing Then
                Guna2Panel6.FillColor = Color.FromArgb(250, 250, 249)
                Guna2Panel6.BorderColor = Color.FromArgb(246, 245, 242)
                Guna2Panel6.BorderThickness = 2
                UpdatePanelLabels(Guna2Panel6)
                Console.WriteLine("Guna2Panel6 updated")
            Else
                Console.WriteLine("Guna2Panel6 is null")
            End If

            ' Guna2Panel7 - Third statistics card
            If Guna2Panel7 IsNot Nothing Then
                Guna2Panel7.FillColor = Color.FromArgb(250, 250, 249)
                Guna2Panel7.BorderColor = Color.FromArgb(246, 245, 242)
                Guna2Panel7.BorderThickness = 2
                UpdatePanelLabels(Guna2Panel7)
                Console.WriteLine("Guna2Panel7 updated")
            Else
                Console.WriteLine("Guna2Panel7 is null")
            End If

            ' LowStockPanel - Inventory status panel
            If LowStockPanel IsNot Nothing Then
                LowStockPanel.FillColor = Color.FromArgb(250, 250, 249)
                LowStockPanel.BorderColor = Color.FromArgb(246, 245, 242)
                LowStockPanel.BorderThickness = 2
                Console.WriteLine("LowStockPanel updated")
            Else
                Console.WriteLine("LowStockPanel is null")
            End If

            ' AreaChart - Chart panel
            If AreaChart IsNot Nothing Then
                AreaChart.FillColor = Color.FromArgb(250, 250, 249)
                AreaChart.BorderColor = Color.FromArgb(246, 245, 242)
                AreaChart.BorderThickness = 2
                Console.WriteLine("AreaChart updated")
            Else
                Console.WriteLine("AreaChart is null")
            End If

            ' Update navigation colors
            If DashboardPanel IsNot Nothing Then
                DashboardPanel.FillColor = Color.FromArgb(250, 250, 249)
                DashboardPanel.BorderColor = Color.FromArgb(246, 245, 242)
                DashboardPanel.BorderThickness = 2
                Console.WriteLine("DashboardPanel updated")
            Else
                Console.WriteLine("DashboardPanel is null")
            End If

            ' PopularPanel is already updated in LoadAllPopularProducts()
            Console.WriteLine("ApplyNewColorScheme completed")

        Catch ex As Exception
            Console.WriteLine($"Error applying color scheme: {ex.Message}")
            Console.WriteLine($"Stack trace: {ex.StackTrace}")
        End Try
    End Sub

    Private Sub UpdatePanelLabels(panel As Panel)
        Try
            If panel Is Nothing Then
                Console.WriteLine("UpdatePanelLabels called with null panel")
                Return
            End If

            If panel.Controls Is Nothing Then
                Console.WriteLine("Panel.Controls is null")
                Return
            End If

            Console.WriteLine($"UpdatePanelLabels processing panel with {panel.Controls.Count} controls")

            ' Update all labels in the panel with appropriate colors
            For Each control As Control In panel.Controls
                If control Is Nothing Then Continue For

                If TypeOf control Is Label Then
                    Dim lbl As Label = CType(control, Label)

                    ' Skip icon labels on circle buttons (olive BackColor) � keep designer ForeColor
                    If lbl.BackColor = Color.FromArgb(191, 155, 48) Then
                        Continue For
                    End If

                    ' Check if it's a main heading/value label
                    If lbl.Font IsNot Nothing AndAlso (lbl.Font.Size > 12 OrElse lbl.Font.Bold) Then
                        lbl.ForeColor = Color.FromArgb(51, 51, 51) ' DarkText for main text
                    Else
                        lbl.ForeColor = Color.FromArgb(102, 102, 102) ' MediumText for secondary text
                    End If

                ElseIf TypeOf control Is Guna2HtmlLabel Then
                    Dim htmlLbl As Guna2HtmlLabel = CType(control, Guna2HtmlLabel)

                    ' Check if it's a main value label by font size
                    If htmlLbl.Font IsNot Nothing AndAlso (htmlLbl.Font.Size > 12 OrElse htmlLbl.Font.Bold) Then
                        htmlLbl.ForeColor = Color.FromArgb(51, 51, 51) ' DarkText for main values
                    Else
                        ' Keep specific colors for growth indicators
                        If htmlLbl.Text IsNot Nothing Then
                            If htmlLbl.Text.Contains("?") Then
                                htmlLbl.ForeColor = Color.FromArgb(16, 216, 98) ' Success Green
                            ElseIf htmlLbl.Text.Contains("?") Then
                                htmlLbl.ForeColor = Color.FromArgb(255, 71, 87) ' Alert Red
                            Else
                                htmlLbl.ForeColor = Color.FromArgb(102, 102, 102) ' MediumText
                            End If
                        Else
                            htmlLbl.ForeColor = Color.FromArgb(102, 102, 102) ' MediumText
                        End If
                    End If

                ElseIf TypeOf control Is Guna2CircleButton Then
                    Dim circleBtn As Guna2CircleButton = CType(control, Guna2CircleButton)

                    ' Update circle buttons with JadeOlive accent � no hover color change
                    circleBtn.FillColor = Color.FromArgb(191, 155, 48) ' JadeOlive
                    circleBtn.ForeColor = Color.White
                    circleBtn.BorderColor = Color.FromArgb(191, 155, 48) ' JadeOlive border
                    circleBtn.HoverState.FillColor = Color.FromArgb(191, 155, 48)
                    circleBtn.HoverState.ForeColor = Color.White

                End If
            Next

        Catch ex As Exception
            Console.WriteLine($"Error updating panel labels: {ex.Message}")
            Console.WriteLine($"Stack trace: {ex.StackTrace}")
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
        ' Load real sales/inventory data for all three cards
        LoadDailySalesData()
        LoadMonthlySalesData()

        ' Load popular products with existing DataGridView
        LoadAllPopularProducts()

        ' Load inventory status chart
        LoadInventoryStatusChart()
    End Sub

    Private Sub LoadDailySalesData()
        Try
            Dim query As String = "
            SELECT
                IFNULL((SELECT COUNT(*) FROM Sales), 0) AS TotalOrders,
                IFNULL((SELECT SUM(CostPrice * CurrentStock) FROM Products WHERE IsActive = 1), 0) AS ActiveStockValue"

            Using reader As DbDataReader = Utilities.ExecuteReader(query, Nothing)
                If reader.Read() Then
                    Dim totalOrders As Integer = Convert.ToInt32(reader("TotalOrders"))
                    Dim activeStockValue As Decimal = Convert.ToDecimal(reader("ActiveStockValue"))

                    ' Card 1: Total Orders
                    Guna2HtmlLabel3.Text = totalOrders.ToString("N0")
                    Guna2HtmlLabel1.Text = "Total Orders"
                    lblDateDailySales.Text = "All Time"

                    ' Card 2: Stock Value (active products only)
                    Guna2HtmlLabel12.Text = ChrW(&H20B1) & activeStockValue.ToString("N0")
                    Guna2HtmlLabel14.Text = "Stock Value"
                    Guna2HtmlLabel11.Text = "Active products only"
                    Guna2HtmlLabel11.ForeColor = Color.FromArgb(102, 102, 102)

                    lastProductCount = totalOrders
                Else
                    Guna2HtmlLabel3.Text = "0"
                    Guna2HtmlLabel12.Text = ChrW(&H20B1) & "0"
                End If
            End Using
        Catch ex As Exception
            Guna2HtmlLabel3.Text = "0"
            Guna2HtmlLabel12.Text = ChrW(&H20B1) & "0"
            Guna2HtmlLabel11.Text = "Active products only"
            Guna2HtmlLabel11.ForeColor = Color.FromArgb(102, 102, 102)
            Console.WriteLine($"Error loading dashboard card #1/#2 data: {ex.Message}")
        End Try
    End Sub

    Private Sub LoadMonthlySalesData()
        Try
            Dim query As String = "
        SELECT IFNULL(SUM(TotalAmount), 0) AS TotalRevenue
        FROM Sales"

            Using reader As DbDataReader = Utilities.ExecuteReader(query, Nothing)
                If reader.Read() Then
                    Dim totalRevenue As Decimal = Convert.ToDecimal(reader("TotalRevenue"))
                    Dim pesoSign As String = ChrW(&H20B1)

                    ' Ensure font supports peso symbol
                    Guna2HtmlLabel16.Font = New Font("Segoe UI", Guna2HtmlLabel16.Font.Size, Guna2HtmlLabel16.Font.Style)

                    ' Card 3: Total Revenue
                    Guna2HtmlLabel16.Text = String.Format(Globalization.CultureInfo.GetCultureInfo("en-PH"), "{0}{1:N0}", pesoSign, totalRevenue)
                    Guna2HtmlLabel15.Text = "Total Revenue"
                    Guna2HtmlLabel15.ForeColor = Color.FromArgb(102, 102, 102)
                    Guna2HtmlLabel18.Text = "All recorded sales"
                Else
                    Guna2HtmlLabel16.Font = New Font("Segoe UI", Guna2HtmlLabel16.Font.Size, Guna2HtmlLabel16.Font.Style)
                    Guna2HtmlLabel16.Text = ChrW(&H20B1) & "0"
                End If
            End Using
        Catch ex As Exception
            Guna2HtmlLabel16.Font = New Font("Segoe UI", Guna2HtmlLabel16.Font.Size, Guna2HtmlLabel16.Font.Style)
            Guna2HtmlLabel16.Text = ChrW(&H20B1) & "0"
            Console.WriteLine($"Error loading dashboard card #3 data: {ex.Message}")
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
            PopularPanel.FillColor = Color.FromArgb(250, 250, 249)
            PopularPanel.BorderColor = Color.FromArgb(246, 245, 242)
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
            Guna2DataGridView1.BackgroundColor = Color.FromArgb(250, 250, 249)
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
            mainContainer.Padding = New Padding(12)
            mainContainer.BackColor = Color.FromArgb(250, 250, 249)
            LowStockPanel.Controls.Add(mainContainer)
            ApplyRoundedCorners(mainContainer, 18)

            ' Create pie chart for status overview (responsive)
            Dim pieChart As New PieChart()
            pieChart.BackColor = Color.FromArgb(250, 250, 249)
            pieChart.Margin = New Padding(6)
            ApplyRoundedCorners(pieChart, 14)

            Dim updateChartLayout As Action = Sub()
                                                  Dim maxWidth = Math.Max(420, mainContainer.ClientSize.Width - 30)
                                                  Dim maxHeight = Math.Max(420, mainContainer.ClientSize.Height - 30)
                                                  Dim chartSize = Math.Min(maxWidth, maxHeight)

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
            pieChart.LegendPosition = LiveChartsCore.Measure.LegendPosition.Right
            pieChart.LegendTextPaint = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#333333"))
            pieChart.LegendBackgroundPaint = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#F5F5F5"))

            ' Enhanced tooltip with numbers
            pieChart.TooltipPosition = LiveChartsCore.Measure.TooltipPosition.Top
            pieChart.TooltipBackgroundPaint = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#F5F5F5"))
            pieChart.TooltipTextPaint = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#333333"))

            mainContainer.Controls.Add(pieChart)

        Catch ex As Exception
            Console.WriteLine($"Error loading inventory status chart: {ex.Message}")
        End Try
    End Sub
    Private Sub LoadChartInterface()
        Try
            AreaChart.Controls.Clear()

            ' Title label
            titleLabel = New Label()
            titleLabel.Text = "Sales Overview"
            titleLabel.Font = New Font("Poppins Medium", 16, FontStyle.Bold)
            titleLabel.ForeColor = Color.FromArgb(51, 51, 51)
            titleLabel.Dock = DockStyle.Top
            titleLabel.Height = 40
            titleLabel.TextAlign = ContentAlignment.MiddleLeft
            titleLabel.Padding = New Padding(15, 0, 0, 0)

            ' Legend and Filter panel combined for better layout
            Dim topPanel As New Panel()
            topPanel.Height = 50
            topPanel.Dock = DockStyle.Top
            topPanel.BackColor = Color.Transparent

            ' Sales legend on the left
            Dim salesLegend As New Label()
            salesLegend.Text = Char.ConvertFromUtf32(&H1F4CA) & " Sales Tracking"
            salesLegend.Font = New Font("Poppins", 11, FontStyle.Bold)
            salesLegend.ForeColor = Color.FromArgb(254, 191, 16)
            salesLegend.AutoSize = True
            salesLegend.Location = New Point(15, 15)
            topPanel.Controls.Add(salesLegend)

            ' FlowLayoutPanel for filter buttons positioned cleanly on the right
            Dim buttonFlow As New FlowLayoutPanel()
            buttonFlow.AutoSize = True
            buttonFlow.AutoSizeMode = AutoSizeMode.GrowAndShrink
            buttonFlow.FlowDirection = FlowDirection.LeftToRight
            buttonFlow.WrapContents = False
            buttonFlow.BackColor = Color.Transparent
            buttonFlow.Anchor = AnchorStyles.Top Or AnchorStyles.Right
            topPanel.Controls.Add(buttonFlow)

            ' Create filter buttons
            btnYearly = CreateChartFilterButton("Yearly")
            AddHandler btnYearly.Click, Sub() LoadChartData("Yearly")

            btnMonthly = CreateChartFilterButton("Monthly")
            AddHandler btnMonthly.Click, Sub() LoadChartData("Monthly")

            btnWeekly = CreateChartFilterButton("Weekly")
            AddHandler btnWeekly.Click, Sub() LoadChartData("Weekly")

            btnToday = CreateChartFilterButton("Today")
            AddHandler btnToday.Click, Sub() LoadChartData("Today")

            buttonFlow.Controls.Add(btnYearly)
            buttonFlow.Controls.Add(btnMonthly)
            buttonFlow.Controls.Add(btnWeekly)
            buttonFlow.Controls.Add(btnToday)

            ' Position filter buttons professionally on the right side
            Dim positionFilterButtons As Action = Sub()
                                                      buttonFlow.Location = New Point(topPanel.ClientSize.Width - buttonFlow.Width - 15, 9)
                                                  End Sub
            positionFilterButtons()
            AddHandler topPanel.SizeChanged, Sub(sender, e)
                                                 positionFilterButtons()
                                             End Sub

            ' Chart panel with proper margins
            chartPanel = New Panel()
            chartPanel.Dock = DockStyle.Fill
            chartPanel.Padding = New Padding(20)
            chartPanel.BackColor = Color.Transparent
            ApplyRoundedCorners(chartPanel, 18)

            ' Try to create LiveCharts CartesianChart
            Try
                salesChart = New CartesianChart()
                salesChart.Dock = DockStyle.Fill
                salesChart.Margin = New Padding(5)
                salesChart.LegendPosition = LiveChartsCore.Measure.LegendPosition.Hidden
                salesChart.BackColor = Color.Transparent
                chartPanel.Controls.Add(salesChart)
                ApplyRoundedCorners(salesChart, 14)
                Console.WriteLine("LiveCharts CartesianChart created successfully")
            Catch chartEx As Exception
                Console.WriteLine($"Failed to create LiveCharts: {chartEx.Message}")
                salesChart = Nothing
            End Try

            ' Add panels in proper order
            AreaChart.Controls.Add(chartPanel)
            AreaChart.Controls.Add(topPanel)
            AreaChart.Controls.Add(titleLabel)

            ' Bring to front in proper order
            titleLabel.BringToFront()
            topPanel.BringToFront()
            chartPanel.BringToFront()

            LoadChartData(currentChartMode)
        Catch ex As Exception
            Console.WriteLine($"Error loading chart interface: {ex.Message}")
        End Try
    End Sub
    Private Function CreateChartFilterButton(text As String) As Guna2Button
        Dim btn As New Guna2Button()
        btn.Text = text
        btn.Font = New Font("Poppins", 10, FontStyle.Regular)
        btn.FillColor = Color.FromArgb(240, 240, 240)
        btn.ForeColor = Color.FromArgb(51, 51, 51)
        btn.BorderRadius = 10
        btn.Size = New Size(120, 32)
        btn.Margin = New Padding(0, 0, 8, 0)
        btn.TabStop = False

        ' Remove press flash: keep pressed/hover close to base fill
        btn.PressedColor = btn.FillColor
        btn.HoverState.FillColor = btn.FillColor
        btn.HoverState.ForeColor = btn.ForeColor

        Return btn
    End Function

    Private Sub LoadChartData(mode As String)
        Try
            currentChartMode = mode

            Dim salesData As New List(Of Double)()
            Dim revenueData As New List(Of Double)()
            Dim labels As New List(Of String)()

            Select Case mode
                Case "Today"
                    LoadTodayData(salesData, revenueData, labels)
                Case "Monthly"
                    LoadMonthlyData(salesData, revenueData, labels)
                Case "Weekly"
                    LoadWeeklyData(salesData, revenueData, labels)
                Case "Daily"
                    LoadDailyData(salesData, revenueData, labels)
                Case "Yearly"
                    LoadYearlyData(salesData, revenueData, labels)
            End Select

            ' Check if salesChart is properly initialized before using it
            If salesChart IsNot Nothing Then
                Try
                    ' Set the chart series with LiveCharts
                    salesChart.Series = {
                    New LineSeries(Of Double) With {
                        .Values = salesData.ToArray(),
                        .Name = "Sales",
                        .GeometrySize = 8,
                        .GeometryStroke = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#333333"), 2),
                        .Fill = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#FECF10").WithAlpha(30)),
                        .Stroke = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#FECF10"), 4),
                        .LineSmoothness = 0.8,
                        .AnimationsSpeed = TimeSpan.FromMilliseconds(1500)
                    }
                }

                    Dim maxValue As Double = If(salesData.Count > 0, salesData.Max(), 0)
                    Dim isZeroData As Boolean = maxValue <= 0
                    Dim yAxisMin As Double = If(isZeroData, -2, 0)
                    Dim yAxisMax As Double = If(isZeroData, 5, Math.Ceiling(maxValue * 1.2))

                    ' Configure axes
                    salesChart.XAxes = {
                    New Axis With {
                        .Labels = labels.ToArray(),
                        .TextSize = 12,
                        .LabelsPaint = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#666666")),
                        .SeparatorsPaint = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#CCCCCC").WithAlpha(50)) With {
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
                        .SeparatorsPaint = New LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColor.Parse("#CCCCCC").WithAlpha(40)) With {
                            .StrokeThickness = 1
                        },
                        .AnimationsSpeed = TimeSpan.FromMilliseconds(800),
                        .Labeler = Function(value)
                                       If isZeroData Then
                                           If Math.Abs(value) < 0.001 Then Return ChrW(&H20B1) & "0"
                                           Return ""
                                       End If
                                       Return ChrW(&H20B1) & $"{value:N0}"
                                   End Function
                    }
                }
                Catch chartEx As Exception
                    Console.WriteLine($"Error setting up LiveCharts: {chartEx.Message}")
                    ' Create fallback display
                    CreateFallbackChartDisplay(mode, salesData, labels)
                End Try
            Else
                Console.WriteLine("salesChart is null, creating fallback display")
                ' Create fallback display
                CreateFallbackChartDisplay(mode, salesData, labels)
            End If

            SetActiveChartButton(mode)
        Catch ex As Exception
            Console.WriteLine($"Error loading chart data: {ex.Message}")
        End Try
    End Sub

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

    Private Sub Guna2CircleButton6_Click(sender As Object, e As EventArgs) Handles Guna2CircleButton6.Click

    End Sub

    Private Sub Guna2CircleButton5_Click(sender As Object, e As EventArgs) Handles Guna2CircleButton5.Click

    End Sub
End Class