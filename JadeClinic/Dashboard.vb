Imports Guna.UI2.WinForms
Imports Microsoft.Data.SqlClient
Imports System.IO

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

            ' Initialize loadingPanel with null checks
            loadingPanel = New Panel With {
                .Dock = DockStyle.Fill,
                .BackColor = System.Drawing.Color.FromArgb(128, 0, 0, 0),
                .Visible = False
            }
            Console.WriteLine("LoadingPanel created")

            loadingLabel = New Label With {
                .Text = "Loading Dashboard...",
                .ForeColor = System.Drawing.Color.White,
                .Font = New Font("Poppins", 16),
                .AutoSize = True,
                .BackColor = System.Drawing.Color.Transparent
            }
            Console.WriteLine("LoadingLabel created")

            loadingPanel.Controls.Add(loadingLabel)
            Me.Controls.Add(loadingPanel)
            Console.WriteLine("Loading controls added")

            AddHandler loadingPanel.SizeChanged, Sub()
                                                     Try
                                                         If loadingLabel IsNot Nothing AndAlso loadingPanel IsNot Nothing Then
                                                             loadingLabel.Location = New Point((loadingPanel.Width - loadingLabel.Width) \ 2, (loadingPanel.Height - loadingLabel.Height) \ 2)
                                                         End If
                                                     Catch ex As Exception
                                                         Console.WriteLine($"Error in loadingPanel.SizeChanged: {ex.Message}")
                                                     End Try
                                                 End Sub
            loadingPanel.BringToFront()
            Console.WriteLine("Dashboard constructor completed successfully")

        Catch ex As Exception
            Console.WriteLine($"Error in Dashboard constructor: {ex.Message}")
            Console.WriteLine($"Stack trace: {ex.StackTrace}")
            Throw ' Re-throw the exception so the calling code knows there was an error
        End Try
    End Sub

    Private Async Sub Dashboard_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            Console.WriteLine("Dashboard_Load starting...")

            ' Initialize form with new color scheme
            Me.Text = $"JadeClinic Dashboard - Welcome {frmLoginvb.LoggedInUsername}"
            Me.BackColor = Color.FromArgb(26, 29, 31) ' Deep Charcoal background #1A1D1F
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.MinimumSize = Me.Size
            Me.MaximumSize = Me.Size
            Console.WriteLine("Basic form properties set")

            ' Apply new color scheme to existing panels
            ApplyNewColorScheme()
            Console.WriteLine("Color scheme applied")

            ' Show loading panel first
            If loadingPanel IsNot Nothing Then
                loadingPanel.Visible = True
                loadingPanel.BringToFront()
                Await Task.Delay(200) ' Let UI render the overlay
            End If
            Console.WriteLine("Loading panel shown")

            ' Create navigation menu
            CreateNavigationMenu()
            Console.WriteLine("Navigation menu created")

            ' Load all UI/data while loading panel is visible
            LoadChartInterface()
            Console.WriteLine("Chart interface loaded")

            LoadDashboardData()
            Console.WriteLine("Dashboard data loaded")

            UpdateMonthlyStockTrend()
            Console.WriteLine("Monthly stock trend updated")

            LoadChartData("Weekly")
            Console.WriteLine("Chart data loaded")

            ' Initialize chart filter buttons if they exist
            If btnAll IsNot Nothing Then
                btnAll.FillColor = Color.FromArgb(61, 65, 69) ' Graphite #3D4145
                btnAll.ForeColor = Color.FromArgb(255, 255, 255) ' Pure White
            End If
            If btnMonthly IsNot Nothing Then
                btnMonthly.FillColor = Color.FromArgb(61, 65, 69) ' Graphite #3D4145
                btnMonthly.ForeColor = Color.FromArgb(255, 255, 255) ' Pure White
            End If
            If btnWeekly IsNot Nothing Then
                btnWeekly.FillColor = Color.FromArgb(254, 191, 16) ' Golden Yellow #FECF10
                btnWeekly.ForeColor = Color.FromArgb(26, 29, 31) ' Deep Charcoal text
            End If
            If btnDaily IsNot Nothing Then
                btnDaily.FillColor = Color.FromArgb(61, 65, 69) ' Graphite #3D4145
                btnDaily.ForeColor = Color.FromArgb(255, 255, 255) ' Pure White
            End If
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

            ' Hide loading panel last
            If loadingPanel IsNot Nothing Then
                loadingPanel.Visible = False
            End If
            Console.WriteLine("Loading panel hidden")

            ' Show welcome message with new styling
            MessageBox.Show($"Welcome to JadeClinic Dashboard, {frmLoginvb.LoggedInUsername}!", "Welcome", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Console.WriteLine("Dashboard_Load completed successfully")

            ' Update form title to show logged-in user
            Me.Text = $"JadeClinic Dashboard - Welcome {frmLoginvb.LoggedInUsername}"

            ' Start idle timeout monitoring
            IdleTimeoutManager.Instance.StartMonitoring(Me)

        Catch ex As Exception
            Console.WriteLine($"Error in Dashboard_Load: {ex.Message}")
            Console.WriteLine($"Stack trace: {ex.StackTrace}")

            ' Try to hide loading panel even if there's an error
            Try
                If loadingPanel IsNot Nothing Then
                    loadingPanel.Visible = False
                End If
            Catch
                ' Ignore errors hiding loading panel
            End Try

            ' Show error to user
            MessageBox.Show($"Error loading dashboard: {ex.Message}{vbCrLf}{vbCrLf}Some features may not work correctly.",
                          "Dashboard Load Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Sub ApplyNewColorScheme()
        Try
            Console.WriteLine("ApplyNewColorScheme starting...")

            ' Update main form background
            Me.BackColor = Color.FromArgb(26, 29, 31) ' Deep Charcoal background #1A1D1F
            Console.WriteLine("Form background updated")

            ' Update main statistic panels with new color scheme - add null checks

            ' DailySalesPanel - First statistics card
            If DailySalesPanel IsNot Nothing Then
                DailySalesPanel.FillColor = Color.FromArgb(61, 65, 69) ' Graphite #3D4145
                Console.WriteLine("DailySalesPanel updated")

                ' Update labels in DailySalesPanel
                UpdatePanelLabels(DailySalesPanel)
            Else
                Console.WriteLine("DailySalesPanel is null")
            End If

            ' Guna2Panel6 - Second statistics card  
            If Guna2Panel6 IsNot Nothing Then
                Guna2Panel6.FillColor = Color.FromArgb(61, 65, 69) ' Graphite #3D4145
                UpdatePanelLabels(Guna2Panel6)
                Console.WriteLine("Guna2Panel6 updated")
            Else
                Console.WriteLine("Guna2Panel6 is null")
            End If

            ' Guna2Panel7 - Third statistics card
            If Guna2Panel7 IsNot Nothing Then
                Guna2Panel7.FillColor = Color.FromArgb(61, 65, 69) ' Graphite #3D4145
                UpdatePanelLabels(Guna2Panel7)
                Console.WriteLine("Guna2Panel7 updated")
            Else
                Console.WriteLine("Guna2Panel7 is null")
            End If

            ' LowStockPanel - Inventory status panel
            If LowStockPanel IsNot Nothing Then
                LowStockPanel.FillColor = Color.FromArgb(61, 65, 69) ' Graphite #3D4145
                Console.WriteLine("LowStockPanel updated")
            Else
                Console.WriteLine("LowStockPanel is null")
            End If

            ' AreaChart - Chart panel
            If AreaChart IsNot Nothing Then
                AreaChart.FillColor = Color.FromArgb(61, 65, 69) ' Graphite #3D4145
                Console.WriteLine("AreaChart updated")
            Else
                Console.WriteLine("AreaChart is null")
            End If

            ' Update navigation colors
            If DashboardPanel IsNot Nothing Then
                DashboardPanel.FillColor = Color.White
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

                    ' Check if it's a main heading/value label
                    If lbl.Font IsNot Nothing AndAlso (lbl.Font.Size > 12 OrElse lbl.Font.Bold) Then
                        lbl.ForeColor = Color.FromArgb(255, 255, 255) ' Pure White for main text
                    Else
                        lbl.ForeColor = Color.FromArgb(225, 229, 233) ' Light Silver for secondary text
                    End If

                ElseIf TypeOf control Is Guna2HtmlLabel Then
                    Dim htmlLbl As Guna2HtmlLabel = CType(control, Guna2HtmlLabel)

                    ' Check if it's a main value label by font size
                    If htmlLbl.Font IsNot Nothing AndAlso (htmlLbl.Font.Size > 12 OrElse htmlLbl.Font.Bold) Then
                        htmlLbl.ForeColor = Color.FromArgb(255, 255, 255) ' Pure White for main values
                    Else
                        ' Keep specific colors for growth indicators
                        If htmlLbl.Text IsNot Nothing Then
                            If htmlLbl.Text.Contains("↗") Then
                                htmlLbl.ForeColor = Color.FromArgb(16, 216, 98) ' Success Green
                            ElseIf htmlLbl.Text.Contains("↘") Then
                                htmlLbl.ForeColor = Color.FromArgb(255, 71, 87) ' Alert Red
                            Else
                                htmlLbl.ForeColor = Color.FromArgb(225, 229, 233) ' Light Silver
                            End If
                        Else
                            htmlLbl.ForeColor = Color.FromArgb(225, 229, 233) ' Light Silver
                        End If
                    End If

                ElseIf TypeOf control Is Guna2CircleButton Then
                    Dim circleBtn As Guna2CircleButton = CType(control, Guna2CircleButton)

                    ' Update circle buttons with golden yellow accent
                    circleBtn.FillColor = Color.FromArgb(254, 191, 16) ' Golden Yellow #FECF10
                    circleBtn.ForeColor = Color.FromArgb(26, 29, 31) ' Deep Charcoal text
                    circleBtn.BorderColor = Color.FromArgb(190, 154, 48) ' Rich Olive border

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

    ' Profile dropdown panel
    Private profileDropdownPanel As Panel = Nothing
    Private isProfileDropdownVisible As Boolean = False

    Private Sub InitializeProfileSection()
        Try
            ' Initialize profile section for logged-in user
            ' You can add profile picture loading and dropdown functionality here if needed
        Catch ex As Exception
            ' Fallback if there's an error - silently fail since profile controls may not exist on all forms
        End Try
    End Sub

    ' FormClosing event handler with exit confirmation
    Private Sub Dashboard_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        ' Stop idle timeout monitoring
        IdleTimeoutManager.Instance.StopMonitoring(Me)

        ' If this is programmatic navigation, don't show confirmation
        If isNavigating Then
            Return
        End If

        ' Prevent multiple confirmations by checking the close reason
        If e.CloseReason = CloseReason.ApplicationExitCall Then
            ' If Application.Exit() was already called, don't show confirmation again
            Return
        End If

        ' Show confirmation only for user-initiated close (X button)
        If e.CloseReason = CloseReason.UserClosing Then
            Dim result As DialogResult = MessageBox.Show("Are you sure you want to exit the application?", "Exit Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

            If result = DialogResult.Yes Then
                ' Close all forms properly
                For Each form As Form In Application.OpenForms.Cast(Of Form).ToArray()
                    If form IsNot Me Then
                        form.Close()
                    End If
                Next

                ' Now exit the application
                Application.Exit()
            Else
                ' Cancel the form closing
                e.Cancel = True
            End If
        End If
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
            ' For dental supply system, we'll show product-related metrics instead of orders
            Dim query As String = "
            SELECT 
                COUNT(ProductID) as ProductCount,
                ISNULL(SUM(CostPrice * CurrentStock), 0) as TotalInventoryValue
            FROM Products 
            WHERE IsActive = 1"

            Using reader As SqlDataReader = Utilities.ExecuteReader(query, Nothing)
                If reader.Read() Then
                    Dim productCount As Integer = Convert.ToInt32(reader("ProductCount"))
                    Dim inventoryValue As Decimal = Convert.ToDecimal(reader("TotalInventoryValue"))

                    ' Update Daily Sales Panel - show total active products
                    Guna2HtmlLabel3.Text = productCount.ToString()
                    lblDateDailySales.Text = DateTime.Now.ToString("MMM d, yyyy")
                    Guna2HtmlLabel1.Text = "Active Products"

                    ' Update third panel (Guna2Panel7) - show total inventory value
                    Guna2HtmlLabel16.Text = inventoryValue.ToString("N0")
                    Guna2HtmlLabel15.Text = "Inventory Value"
                    Guna2HtmlLabel15.ForeColor = Color.LightGray
                    lastProductCount = productCount

                    Guna2HtmlLabel18.Text = "Total Value"
                Else
                    Guna2HtmlLabel3.Text = "0"
                    Guna2HtmlLabel16.Text = "0"
                End If
            End Using
        Catch ex As Exception
            Guna2HtmlLabel3.Text = "0"
            Guna2HtmlLabel16.Text = "0"
            Console.WriteLine($"Error loading dashboard data: {ex.Message}")
        End Try
    End Sub

    Private Sub LoadMonthlySalesData()
        Try
            ' Get current vs last month's new products added
            Dim thisMonthProducts As Integer = 0
            Dim lastMonthProducts As Integer = 0

            ' Query for this month's new products
            Dim queryThisMonth As String = "
            SELECT COUNT(*) as ProductCount
            FROM Products
            WHERE YEAR(Created) = YEAR(GETDATE()) AND MONTH(Created) = MONTH(GETDATE())"
            Using reader As SqlDataReader = Utilities.ExecuteReader(queryThisMonth, Nothing)
                If reader.Read() Then
                    thisMonthProducts = Convert.ToInt32(reader("ProductCount"))
                End If
            End Using

            ' Query for last month's products
            Dim queryLastMonth As String = "
            SELECT COUNT(*) as ProductCount
            FROM Products
            WHERE 
                (YEAR(Created) = YEAR(DATEADD(month, -1, GETDATE())) AND 
                 MONTH(Created) = MONTH(DATEADD(month, -1, GETDATE())))"
            Using reader As SqlDataReader = Utilities.ExecuteReader(queryLastMonth, Nothing)
                If reader.Read() Then
                    lastMonthProducts = Convert.ToInt32(reader("ProductCount"))
                End If
            End Using

            ' Calculate percent change
            Dim percentChange As Decimal = 0
            If lastMonthProducts > 0 Then
                percentChange = ((thisMonthProducts - lastMonthProducts) / lastMonthProducts) * 100D
            ElseIf thisMonthProducts > 0 Then
                percentChange = 100
            Else
                percentChange = 0
            End If

            ' Set label text and color
            If percentChange >= 0 Then
                Guna2HtmlLabel11.Text = $"↗ {percentChange:N1}% from last month"
                Guna2HtmlLabel11.ForeColor = Color.LightGreen
            Else
                Guna2HtmlLabel11.Text = $"↘ {Math.Abs(percentChange):N1}% from last month"
                Guna2HtmlLabel11.ForeColor = Color.FromArgb(255, 128, 128) ' Red
            End If

            ' Set monthly new products count
            Guna2HtmlLabel12.Text = thisMonthProducts.ToString()
            Guna2HtmlLabel14.Text = "New Products This Month"
        Catch ex As Exception
            Guna2HtmlLabel12.Text = "0"
            Guna2HtmlLabel11.Text = "0% from last month"
            Guna2HtmlLabel11.ForeColor = Color.Gray
            Console.WriteLine($"Error loading monthly data: {ex.Message}")
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

            ' Set PopularPanel background to Graphite
            PopularPanel.FillColor = Color.FromArgb(61, 65, 69) ' Graphite #3D4145

            ' Add title label for Popular Products
            Dim titleLabel As New Label()
            titleLabel.Text = "Popular Products"
            titleLabel.Font = New Font("Poppins Medium", 13.8F, FontStyle.Regular)
            titleLabel.ForeColor = Color.FromArgb(255, 255, 255) ' Pure White
            titleLabel.Location = New Point(30, 15)
            titleLabel.AutoSize = True
            titleLabel.BackColor = Color.Transparent
            PopularPanel.Controls.Add(titleLabel)

            ' Move search textbox to specified position with new styling
            txtProductSearch.Location = New Point(550, 15)
            txtProductSearch.Size = New Size(200, 20)
            txtProductSearch.BorderRadius = 10
            txtProductSearch.BackColor = Color.Transparent

            ' Configure the existing DataGridView with new color scheme
            Guna2DataGridView1.Columns.Clear()
            Guna2DataGridView1.Rows.Clear()

            ' Apply new dark theme styling
            Guna2DataGridView1.BackgroundColor = Color.FromArgb(61, 65, 69) ' Graphite #3D4145
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

            ' Cell styling with new color scheme
            Guna2DataGridView1.GridColor = Color.FromArgb(74, 79, 84) ' Steel Gray separators #4A4F54
            Guna2DataGridView1.DefaultCellStyle.BackColor = Color.FromArgb(43, 47, 50) ' Dark Slate #2B2F32
            Guna2DataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(43, 47, 50) ' Dark Slate #2B2F32
            Guna2DataGridView1.DefaultCellStyle.ForeColor = Color.FromArgb(225, 229, 233) ' Light Silver #E1E5E9
            Guna2DataGridView1.DefaultCellStyle.SelectionBackColor = Color.FromArgb(254, 191, 16) ' Golden Yellow #FECF10
            Guna2DataGridView1.DefaultCellStyle.SelectionForeColor = Color.FromArgb(26, 29, 31) ' Deep Charcoal
            Guna2DataGridView1.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
            Guna2DataGridView1.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            Guna2DataGridView1.DefaultCellStyle.Padding = New Padding(5, 4, 5, 4)

            ' Header styling with new colors
            Guna2DataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(26, 29, 31) ' Deep Charcoal #1A1D1F
            Guna2DataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(225, 229, 233) ' Light Silver #E1E5E9
            Guna2DataGridView1.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(26, 29, 31) ' Deep Charcoal
            Guna2DataGridView1.ColumnHeadersDefaultCellStyle.Font = New Font("Poppins SemiBold", 10.0F, FontStyle.Regular)
            Guna2DataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            Guna2DataGridView1.ColumnHeadersHeight = 50
            Guna2DataGridView1.RowTemplate.Height = 60
            Guna2DataGridView1.EnableHeadersVisualStyles = False

            ' Add columns
            ' No column
            Dim noColumn As New DataGridViewTextBoxColumn()
            noColumn.Name = "No"
            noColumn.HeaderText = "No"
            noColumn.FillWeight = 8
            noColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            noColumn.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Bold)
            Guna2DataGridView1.Columns.Add(noColumn)

            ' Product Code column
            Dim codeColumn As New DataGridViewTextBoxColumn()
            codeColumn.Name = "ProductCode"
            codeColumn.HeaderText = "Code"
            codeColumn.FillWeight = 15
            codeColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            Guna2DataGridView1.Columns.Add(codeColumn)

            ' Product Name column
            Dim nameColumn As New DataGridViewTextBoxColumn()
            nameColumn.Name = "ProductName"
            nameColumn.HeaderText = "Product Name"
            nameColumn.FillWeight = 35
            nameColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
            nameColumn.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
            Guna2DataGridView1.Columns.Add(nameColumn)

            ' Category column
            Dim categoryColumn As New DataGridViewTextBoxColumn()
            categoryColumn.Name = "Category"
            categoryColumn.HeaderText = "Category"
            categoryColumn.FillWeight = 20
            categoryColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            Guna2DataGridView1.Columns.Add(categoryColumn)

            ' Current Stock column
            Dim stockColumn As New DataGridViewTextBoxColumn()
            stockColumn.Name = "CurrentStock"
            stockColumn.HeaderText = "Stock"
            stockColumn.FillWeight = 12
            stockColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            stockColumn.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Bold)
            Guna2DataGridView1.Columns.Add(stockColumn)

            ' Price column with Success Green color
            Dim priceColumn As New DataGridViewTextBoxColumn()
            priceColumn.Name = "SellingPrice"
            priceColumn.HeaderText = "Price"
            priceColumn.FillWeight = 15
            priceColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            priceColumn.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Bold)
            priceColumn.DefaultCellStyle.ForeColor = Color.FromArgb(16, 216, 98) ' Success Green #10D862
            Guna2DataGridView1.Columns.Add(priceColumn)

            ' Query to get ALL products
            Dim query As String = "
            SELECT TOP 20
                p.ProductID,
                p.ProductCode,
                p.ProductName,
                p.Category,
                p.CurrentStock,
                p.SellingPrice,
                p.IsActive
            FROM Products p
            WHERE p.IsActive = 1
            ORDER BY p.CurrentStock DESC, p.ProductName"

            Dim rowIndex As Integer = 1

            Using reader As SqlDataReader = Utilities.ExecuteReader(query, Nothing)
                While reader.Read()
                    Dim row As DataGridViewRow = New DataGridViewRow()
                    row.CreateCells(Guna2DataGridView1)

                    ' Set cell values
                    row.Cells(0).Value = rowIndex.ToString() ' No
                    row.Cells(1).Value = reader("ProductCode").ToString() ' Code
                    row.Cells(2).Value = reader("ProductName").ToString() ' Name
                    row.Cells(3).Value = reader("Category").ToString() ' Category
                    row.Cells(4).Value = reader("CurrentStock").ToString() ' Stock
                    row.Cells(5).Value = "₱" & Convert.ToDecimal(reader("SellingPrice")).ToString("F2") ' Price

                    ' Color coding for stock levels with new color scheme
                    Dim currentStock As Integer = Convert.ToInt32(reader("CurrentStock"))
                    If currentStock <= 10 Then
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 71, 87) ' Alert Red #FF4757
                        row.Cells(4).Style.ForeColor = Color.FromArgb(255, 255, 255) ' White text
                    ElseIf currentStock <= 50 Then
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 159, 67) ' Warning Orange #FF9F43
                        row.Cells(4).Style.ForeColor = Color.FromArgb(26, 29, 31) ' Deep Charcoal text
                    End If

                    Guna2DataGridView1.Rows.Add(row)
                    rowIndex += 1
                End While
            End Using

            ' Prevent row resizing for all rows
            For Each row As DataGridViewRow In Guna2DataGridView1.Rows
                row.Resizable = DataGridViewTriState.False
            Next

            ' Configure the search textbox styling with new colors
            txtProductSearch.PlaceholderText = "🔍 Search products..."
            txtProductSearch.Font = New Font("Poppins", 10.0F)
            txtProductSearch.ForeColor = Color.FromArgb(225, 229, 233) ' Light Silver #E1E5E9
            txtProductSearch.BackColor = Color.FromArgb(43, 47, 50) ' Dark Slate #2B2F32
            txtProductSearch.BorderRadius = 10

        Catch ex As Exception
            ' Handle error and show user-friendly message
            Console.WriteLine($"Error loading products: {ex.Message}")
            MessageBox.Show("Unable to load product data. Please try refreshing the dashboard.", "Data Load Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Sub UpdateMonthlyStockTrend()
        ' Placeholder for monthly trend updates
        ' This can be expanded later when LiveCharts is properly configured
    End Sub

    Private Sub LoadInventoryStatusChart()
        Try
            ' Clear existing controls in LowStockPanel
            LowStockPanel.Controls.Clear()

            ' Add title for the inventory status chart
            Dim titleLabel As New Label()
            titleLabel.Text = "Inventory Status Overview"
            titleLabel.Font = New Font("Poppins Medium", 13.8F, FontStyle.Regular)
            titleLabel.ForeColor = Color.White
            titleLabel.Location = New Point(25, 20)
            titleLabel.AutoSize = True
            LowStockPanel.Controls.Add(titleLabel)

            ' Query to get inventory status counts
            Dim query As String = "
        SELECT 
            SUM(CASE WHEN CurrentStock = 0 THEN 1 ELSE 0 END) as OutOfStock,
            SUM(CASE WHEN CurrentStock > 0 AND CurrentStock <= 10 THEN 1 ELSE 0 END) as LowStock,
            SUM(CASE WHEN CurrentStock > 10 THEN 1 ELSE 0 END) as Active
        FROM Products 
        WHERE IsActive = 1"

            Dim outOfStockCount As Integer = 0
            Dim lowStockCount As Integer = 0
            Dim activeCount As Integer = 0

            Using reader As SqlDataReader = Utilities.ExecuteReader(query, Nothing)
                If reader.Read() Then
                    outOfStockCount = Convert.ToInt32(reader("OutOfStock"))
                    lowStockCount = Convert.ToInt32(reader("LowStock"))
                    activeCount = Convert.ToInt32(reader("Active"))
                End If
            End Using

            ' Create simple status display since LiveCharts might not be available
            Dim yPosition As Integer = 80

            ' Active products
            Dim activeLabel As New Label()
            activeLabel.Text = $"✅ Active Stock: {activeCount} products"
            activeLabel.Font = New Font("Poppins", 11, FontStyle.Regular)
            activeLabel.ForeColor = Color.FromArgb(76, 175, 80) ' Green
            activeLabel.Location = New Point(25, yPosition)
            activeLabel.AutoSize = True
            LowStockPanel.Controls.Add(activeLabel)
            yPosition += 40

            ' Low stock
            Dim lowStockLabel As New Label()
            lowStockLabel.Text = $"⚠ Low Stock: {lowStockCount} products"
            lowStockLabel.Font = New Font("Poppins", 11, FontStyle.Regular)
            lowStockLabel.ForeColor = Color.FromArgb(255, 152, 0) ' Orange
            lowStockLabel.Location = New Point(25, yPosition)
            lowStockLabel.AutoSize = True
            LowStockPanel.Controls.Add(lowStockLabel)
            yPosition += 40

            ' Out of stock
            Dim outOfStockLabel As New Label()
            outOfStockLabel.Text = $"❌ Out of Stock: {outOfStockCount} products"
            outOfStockLabel.Font = New Font("Poppins", 11, FontStyle.Regular)
            outOfStockLabel.ForeColor = Color.FromArgb(244, 67, 54) ' Red
            outOfStockLabel.Location = New Point(25, yPosition)
            outOfStockLabel.AutoSize = True
            LowStockPanel.Controls.Add(outOfStockLabel)

        Catch ex As Exception
            ' Handle error silently
            Console.WriteLine($"Error loading inventory status: {ex.Message}")
        End Try
    End Sub

    Private Sub LoadChartInterface()
        Try
            ' Basic chart interface setup for the AreaChart panel
            ' Clear any existing controls
            AreaChart.Controls.Clear()

            ' Title label
            titleLabel = New Label()
            titleLabel.Text = "Inventory Overview"
            titleLabel.Font = New Font("Poppins Medium", 16, FontStyle.Bold)
            titleLabel.ForeColor = Color.White
            titleLabel.Dock = DockStyle.Top
            titleLabel.Height = 40
            titleLabel.TextAlign = ContentAlignment.MiddleLeft
            titleLabel.Padding = New Padding(15, 0, 0, 0)

            ' Simple chart placeholder
            Dim chartPlaceholder As New Label()
            chartPlaceholder.Text = "📊 Product inventory trends will be displayed here"
            chartPlaceholder.Font = New Font("Poppins", 12, FontStyle.Regular)
            chartPlaceholder.ForeColor = Color.LightGray
            chartPlaceholder.Dock = DockStyle.Fill
            chartPlaceholder.TextAlign = ContentAlignment.MiddleCenter

            AreaChart.Controls.Add(chartPlaceholder)
            AreaChart.Controls.Add(titleLabel)
            titleLabel.BringToFront()

        Catch ex As Exception
            Console.WriteLine($"Error loading chart interface: {ex.Message}")
        End Try
    End Sub

    Private Sub LoadChartData(mode As String)
        ' Placeholder for chart data loading
        ' This can be expanded later when LiveCharts is properly configured
    End Sub

    Private Sub txtProductSearch_TextChanged(sender As Object, e As EventArgs) Handles txtProductSearch.TextChanged
        Try
            ' Get the search text
            Dim searchText As String = txtProductSearch.Text.Trim().ToLower()

            ' If search is empty, show all rows
            If String.IsNullOrEmpty(searchText) Then
                For Each row As DataGridViewRow In Guna2DataGridView1.Rows
                    row.Visible = True
                Next
                Return
            End If

            ' Filter rows based on search text
            For Each row As DataGridViewRow In Guna2DataGridView1.Rows
                If row.Cells("ProductName").Value IsNot Nothing Then
                    Dim productName As String = row.Cells("ProductName").Value.ToString().ToLower()
                    Dim productCode As String = ""
                    If row.Cells("ProductCode").Value IsNot Nothing Then
                        productCode = row.Cells("ProductCode").Value.ToString().ToLower()
                    End If

                    ' Show row if product name or code contains search text
                    row.Visible = productName.Contains(searchText) OrElse productCode.Contains(searchText)
                Else
                    ' Hide rows with no product name
                    row.Visible = False
                End If
            Next

        Catch ex As Exception
            ' Handle any errors silently to prevent crashes during typing
            Console.WriteLine($"Search error: {ex.Message}")
        End Try
    End Sub

    ' Navigation event handlers
    Private Sub NavDashboard_Click(sender As Object, e As EventArgs)
        ' Already on dashboard - do nothing
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
        Sales.Show()
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

    ' Helper method to update navigation button states
    Private Sub UpdateNavButtonStates(activeButton As String)
        ' Reset all buttons to inactive state
        Dim navButtons As New Dictionary(Of String, Guna2Button) From {
        {"Dashboard", navDashboardBtn},
        {"Inventory", navInventoryBtn},
        {"POS", navPOSBtn},
        {"SalesRecords", navSalesRecordsBtn},
        {"Staff", navStaffBtn},
        {"InventoryLog", navInventoryLogBtn},
        {"AuditLog", navAuditLogBtn}
    }

        For Each kvp In navButtons
            If kvp.Value IsNot Nothing Then
                If kvp.Key = activeButton Then
                    ' Active button
                    kvp.Value.FillColor = Color.FromArgb(255, 204, 77) ' Orange
                    kvp.Value.ForeColor = Color.Black
                Else
                    ' Inactive button
                    kvp.Value.FillColor = Color.Transparent
                    kvp.Value.ForeColor = Color.White
                End If
            End If
        Next
    End Sub

    ' Navigation event handlers for Dashboard form (using original controls)
    Private Sub btnToOrderForm_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Sales.Show()
        Close()
    End Sub

    Private Sub toOrderFormIcon_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Sales.Show()
        Close()
    End Sub

    Private Sub toOrderFormLbl_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Sales.Show()
        Close()
    End Sub

    Private Sub Guna2CircleButton3_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Inventory.Show()
        Close()
    End Sub

    Private Sub Guna2CirclePictureBox3_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Inventory.Show()
        Close()
    End Sub

    Private Sub Guna2HtmlLabel24_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Inventory.Show()
        Close()
    End Sub

    ' Staff Management Navigation
    Private Sub Guna2CircleButton4_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Staff.Show()
        Close()
    End Sub

    Private Sub Guna2CirclePictureBox4_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Staff.Show()
        Close()
    End Sub

    Private Sub Guna2HtmlLabel25_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Staff.Show()
        Close()
    End Sub

    ' InventoryLog Navigation
    Private Sub Guna2CircleButton2_Click(sender As Object, e As EventArgs)
        isNavigating = True
        InventoryLog.Show()
        Close()
    End Sub

    Private Sub Guna2CirclePictureBox2_Click(sender As Object, e As EventArgs)
        isNavigating = True
        InventoryLog.Show()
        Close()
    End Sub

    Private Sub Guna2HtmlLabel23_Click(sender As Object, e As EventArgs)
        isNavigating = True
        InventoryLog.Show()
        Close()
    End Sub

    ' Audit Logs Navigation (keeping the existing handlers)
    Private Sub Guna2CircleButton1_Click(sender As Object, e As EventArgs)
        MessageBox.Show("Audit logs feature will be available soon!", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub Guna2CirclePictureBox6_Click(sender As Object, e As EventArgs)
        MessageBox.Show("Audit logs feature will be available soon!", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub Guna2HtmlLabel19_Click(sender As Object, e As EventArgs)
        MessageBox.Show("Audit logs feature will be available soon!", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    ' Logout functionality
    Private Sub Guna2CirclePictureBox21_Click(sender As Object, e As EventArgs)
        ' Close this form and show login
        Close()
        frmLoginvb.Show()
    End Sub

    Private Sub Guna2HtmlLabel6_Click(sender As Object, e As EventArgs)
        ' Close this form and show login
        Close()
        frmLoginvb.Show()
    End Sub

    Private Sub Guna2CircleButton9_Click(sender As Object, e As EventArgs)
        ' Close this form and show login
        Close()
        frmLoginvb.Show()
    End Sub

    Private Sub PopularPanel_Paint(sender As Object, e As PaintEventArgs) Handles PopularPanel.Paint
        ' Handle panel paint events if needed
    End Sub

    Private Sub Guna2HtmlLabel12_Click(sender As Object, e As EventArgs) Handles Guna2HtmlLabel12.Click
        ' Handle label click if needed
    End Sub

    Private Sub Guna2HtmlLabel11_Click(sender As Object, e As EventArgs) Handles Guna2HtmlLabel11.Click
        ' Handle label click if needed
    End Sub

    Private Sub DashboardPanel_Paint(sender As Object, e As PaintEventArgs) Handles DashboardPanel.Paint

    End Sub

    Private Sub Guna2CirclePictureBox6_Click_1(sender As Object, e As EventArgs)

    End Sub

    Private Sub Guna2HtmlLabel22_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub Guna2CirclePictureBox1_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub Label3_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub Guna2CircleButton5_Click(sender As Object, e As EventArgs) Handles Guna2CircleButton5.Click

    End Sub

    Private Sub Guna2DataGridView1_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles Guna2DataGridView1.CellContentClick

    End Sub

    Private Sub Label4_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub CreateNavigationMenu()
        Try
            ' Clear existing controls except PictureBox9 (logo)
            For i = DashboardPanel.Controls.Count - 1 To 0 Step -1
                Dim control As Control = DashboardPanel.Controls(i)
                If TypeOf control IsNot PictureBox Then
                    DashboardPanel.Controls.Remove(control)
                    control.Dispose()
                End If
            Next

            ' Use dark navigation background (match SalesRecord style but keep Dashboard color choices)
            DashboardPanel.FillColor = System.Drawing.Color.FromArgb(61, 65, 66)

            ' Logo area: render company logo into existing PictureBox9 without resizing or adding handlers
            If PictureBox9 IsNot Nothing Then
                Try
                    Dim logoImg As System.Drawing.Image = CompanySettingsManager.Instance.GetCompanyLogo()
                    If logoImg IsNot Nothing Then
                        PictureBox9.Image = logoImg
                        PictureBox9.Location = New Point(81, 15)
                        ' Do NOT change PictureBox9.Size or Location or add any event handlers
                    End If
                Catch ex As Exception
                    Console.WriteLine($"Unable to set dashboard logo: {ex.Message}")
                End Try
                PictureBox9.BringToFront()
            End If

            Dim availableWidth As Integer = DashboardPanel.Width - 40
            Dim startY As Integer = 250
            Dim buttonHeight As Integer = 50
            Dim buttonSpacing As Integer = 15
            Dim buttonWidth As Integer = availableWidth - 5
            Dim buttonIndex As Integer = 0

            ' Company name from settings (uses CompanySettingsManager)
            Dim companyName As String = CompanySettingsManager.Instance.GetSettingString("CompanyName", "JADE CLINIC")

            ' Title and subtitle (dark nav - use light text)
            Dim titleLabel As New Label()
            titleLabel.Text = companyName
            titleLabel.Font = New Font("Poppins", 14, FontStyle.Bold)
            titleLabel.ForeColor = Color.FromArgb(254, 191, 16) ' Golden Yellow
            titleLabel.BackColor = Color.Transparent
            titleLabel.AutoSize = False
            titleLabel.Size = New Size(availableWidth, 30)
            titleLabel.Location = New Point(20, 110)
            titleLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(titleLabel)

            Dim subtitleLabel As New Label()
            subtitleLabel.Text = "Dental Supply Management"
            subtitleLabel.Font = New Font("Poppins", 10, FontStyle.Regular)
            subtitleLabel.ForeColor = Color.FromArgb(225, 229, 233) ' LightSilver for dark bg
            subtitleLabel.BackColor = Color.Transparent
            subtitleLabel.AutoSize = False
            subtitleLabel.Size = New Size(availableWidth, 25)
            subtitleLabel.Location = New Point(20, 145)
            subtitleLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(subtitleLabel)

            Dim separator1 As New Panel()
            separator1.BackColor = System.Drawing.Color.FromArgb(50, 50, 50)
            separator1.Size = New System.Drawing.Size(availableWidth - 20, 2)
            separator1.Location = New System.Drawing.Point(30, 190)
            DashboardPanel.Controls.Add(separator1)

            Dim navLabel As New Label()
            navLabel.Text = "NAVIGATION"
            navLabel.Font = New Font("Poppins", 10, FontStyle.Bold)
            navLabel.ForeColor = Color.FromArgb(225, 229, 233)
            navLabel.BackColor = Color.Transparent
            navLabel.AutoSize = False
            navLabel.Size = New System.Drawing.Size(availableWidth, 25)
            navLabel.Location = New System.Drawing.Point(20, 205)
            navLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(navLabel)

            ' Role logic
            Dim currentRole As String = If(frmLoginvb.LoggedInRole, "Staff").ToUpper()

            ' Create navigation buttons directly with the isActive flag (styleForDarkNav removed)
            navDashboardBtn = CreateLargeNavButton("🏠 Dashboard", startY + buttonIndex * (buttonHeight + buttonSpacing), True, buttonWidth, buttonHeight)
            AddHandler navDashboardBtn.Click, AddressOf NavDashboard_Click
            buttonIndex += 1

            navPOSBtn = CreateLargeNavButton("🛒 POS / Sales", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navPOSBtn.Click, AddressOf NavPOS_Click
            buttonIndex += 1

            If currentRole = "MANAGER" Or currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                navInventoryBtn = CreateLargeNavButton("📦 Inventory", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navInventoryBtn.Click, AddressOf NavInventory_Click
                buttonIndex += 1

                navSalesRecordsBtn = CreateLargeNavButton("📊 Sales Records", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navSalesRecordsBtn.Click, AddressOf NavSalesRecords_Click
                buttonIndex += 1

                navStaffBtn = CreateLargeNavButton("👥 Staff", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navStaffBtn.Click, AddressOf NavStaff_Click
                buttonIndex += 1

                navInventoryLogBtn = CreateLargeNavButton("📋 Inventory Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navInventoryLogBtn.Click, AddressOf NavInventoryLog_Click
                buttonIndex += 1
            End If
            If currentRole = "MANAGER" Or currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                ' Suppliers (place above Audit Logs)
                Dim navSuppliersBtn = CreateLargeNavButton("🏷️ Suppliers", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navSuppliersBtn.Click, AddressOf NavSuppliers_Click
                buttonIndex += 1
            End If
            If currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                navAuditLogBtn = CreateLargeNavButton("🔍 Audit Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navAuditLogBtn.Click, AddressOf NavAuditLog_Click
                buttonIndex += 1

                Dim systemSettingsBtn = CreateLargeNavButton("⚙️ System", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler systemSettingsBtn.Click, AddressOf NavSystemSettings_Click
                buttonIndex += 1
            End If

            ' Add separator before logout (visual)
            Dim separator2 As New Panel()
            separator2.BackColor = System.Drawing.Color.FromArgb(50, 50, 50)
            separator2.Size = New Size(availableWidth - 40, 2)
            separator2.Location = New Point(40, startY + buttonIndex * (buttonHeight + buttonSpacing) + 10)
            DashboardPanel.Controls.Add(separator2)

            ' Logout button: keep existing user info & logout behavior untouched
            navLogoutBtn = CreateLargeNavButton("🚪 Logout", startY + buttonIndex * (buttonHeight + buttonSpacing) + 30, False, buttonWidth, buttonHeight)

            ' Style logout to stand out (red) and ensure hover does not change color
            navLogoutBtn.FillColor = Color.FromArgb(255, 71, 87) ' Alert Red
            navLogoutBtn.ForeColor = Color.White

            ' Override hover handlers so the button remains red on hover (no color changes)
            AddHandler navLogoutBtn.MouseEnter, Sub()
                                                    navLogoutBtn.FillColor = Color.FromArgb(255, 71, 87)
                                                    navLogoutBtn.ForeColor = Color.White
                                                    navLogoutBtn.Font = New Font("Poppins", 10, FontStyle.Bold)
                                                End Sub
            AddHandler navLogoutBtn.MouseLeave, Sub()
                                                    navLogoutBtn.FillColor = Color.FromArgb(255, 71, 87)
                                                    navLogoutBtn.ForeColor = Color.White
                                                    navLogoutBtn.Font = New Font("Poppins", 10, FontStyle.Regular)
                                                End Sub

            AddHandler navLogoutBtn.Click, AddressOf NavLogout_Click

            ' Add user info section (do not modify behavior; but update styling inside separate method)
            CreateUserInfoSection()

        Catch ex As Exception
            Console.WriteLine($"Error creating navigation menu: {ex.Message}")
        End Try
    End Sub
    Private Sub NavSuppliers_Click(sender As Object, e As EventArgs)
        Try
            isNavigating = True
            Supplier.Show()
            Me.Close()
        Catch ex As Exception
            MessageBox.Show($"Unable to open Suppliers: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
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
            .FillColor = Color.FromArgb(61, 65, 69),
            .BorderRadius = 8,
            .BackColor = Color.FromArgb(61, 65, 69)
        }

            ' Small avatar picture box
            Dim avatarSize As Integer = 30
            Dim avatar As New PictureBox() With {
            .Size = New Size(avatarSize, avatarSize),
            .Location = New Point(6, (panelHeight - avatarSize) \ 2),
            .SizeMode = PictureBoxSizeMode.Zoom,
            .BackColor = Color.Transparent
        }

            ' Create default initials avatar (inline, avoids external helper)
            Dim username As String = If(String.IsNullOrEmpty(frmLoginvb.LoggedInUsername), "U", frmLoginvb.LoggedInUsername)
            Try
                Dim bmp As New Bitmap(avatarSize, avatarSize)
                Using g As Graphics = Graphics.FromImage(bmp)
                    g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

                    ' color palette (kept small)
                    Dim colors() As Color = {
                    Color.FromArgb(255, 107, 107),
                    Color.FromArgb(78, 205, 196),
                    Color.FromArgb(85, 98, 112),
                    Color.FromArgb(129, 236, 236),
                    Color.FromArgb(116, 185, 255)
                }
                    Dim idx As Integer = Math.Abs(username.GetHashCode()) Mod colors.Length
                    Using br As New SolidBrush(colors(idx))
                        g.FillEllipse(br, 0, 0, avatarSize - 1, avatarSize - 1)
                    End Using

                    ' build initials (1 or 2 letters)
                    Dim initials As String = username.Substring(0, 1).ToUpper()
                    For i As Integer = 1 To username.Length - 1
                        If Char.IsUpper(username(i)) OrElse username(i) = " "c Then
                            If username(i) <> " "c Then
                                initials &= username(i).ToString().ToUpper()
                                Exit For
                            End If
                        End If
                    Next

                    Using font As New Font("Poppins", 10, FontStyle.Bold, GraphicsUnit.Point)
                        Dim sf As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
                        Using brushWhite As New SolidBrush(Color.White)
                            g.DrawString(initials, font, brushWhite, New RectangleF(0, 0, avatarSize, avatarSize), sf)
                        End Using
                    End Using
                End Using
                avatar.Image = bmp
            Catch
                ' fallback: plain background
                avatar.BackColor = Color.FromArgb(80, 80, 80)
            End Try

            ' Username label (compact) - enable ellipsis when text is too long
            Dim userLabel As New Label() With {
            .AutoSize = False,
            .Size = New Size(panelWidth - avatar.Width - 14, 18),
            .Location = New Point(avatar.Right + 6, 6),
            .Text = username,
            .Font = New Font("Poppins", 9.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(254, 191, 16),
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
            .ForeColor = Color.FromArgb(225, 229, 233),
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
    Private Sub NavSystemSettings_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Sys.Show()
        Me.Close()
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
        btn.ForeColor = If(isActive, System.Drawing.Color.FromArgb(26, 29, 31), System.Drawing.Color.White)
        btn.BorderThickness = If(isActive, 0, 1)
        btn.BorderColor = If(isActive, System.Drawing.Color.Transparent, System.Drawing.Color.FromArgb(80, 80, 80))
        btn.BackColor = System.Drawing.Color.Transparent
        btn.Cursor = Cursors.Hand

        btn.ShadowDecoration.Enabled = True
        btn.ShadowDecoration.Color = System.Drawing.Color.FromArgb(30, 30, 30)
        btn.ShadowDecoration.Depth = 4

        AddHandler btn.MouseEnter, Sub()
                                       If Not isActive Then
                                           btn.FillColor = System.Drawing.Color.FromArgb(48, 52, 54)
                                           btn.BorderColor = System.Drawing.Color.FromArgb(254, 191, 16)
                                           btn.Font = New Font("Poppins", 9, FontStyle.Bold)
                                       End If
                                   End Sub
        AddHandler btn.MouseLeave, Sub()
                                       If Not isActive Then
                                           btn.FillColor = System.Drawing.Color.Transparent
                                           btn.BorderColor = System.Drawing.Color.FromArgb(80, 80, 80)
                                           btn.Font = New Font("Poppins", 10, FontStyle.Regular)
                                       End If
                                   End Sub

        DashboardPanel.Controls.Add(btn)
        Return btn
    End Function
End Class