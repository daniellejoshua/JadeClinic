Imports Microsoft.Data.Sqlite
Imports System.Data.Common
Imports System.Data
Imports System.IO
Imports System.Threading.Tasks

Public Class Inventory
    Private allProducts As New List(Of Dictionary(Of String, Object))
    Private filteredProducts As New List(Of Dictionary(Of String, Object))
    Private visibleStartIndex As Integer = 0
    Private visibleItemCount As Integer = 15 ' Number of visible items
    Private itemHeight As Integer = 80 ' Height of each product panel
    Private loadingOverlay As Panel
    Private _cachedLogo As Image = Nothing

    ' Navigation flag to prevent exit confirmation on programmatic close
    Private isNavigating As Boolean = False

    ' Profile managed by ProfileManager

    ' Custom tooltip implementation for better DataGridView support
    Private customTooltip As ToolTip
    Private tooltipTimer As Timer
    Private currentTooltipCell As DataGridViewCell = Nothing
    Private lastMousePosition As Point = Point.Empty
    ' Add this field near the other private fields at the top of the class
    Private statusFilter As Nullable(Of Boolean) = Nothing ' Nothing = All, True = Active, False = Inactive

    ' Pagination state
    Private _pagination As PaginationControl
    Private _currentPage As Integer = 1
    Private _pageSize As Integer = 10

    Private Async Sub Inventory_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.BackColor = Color.FromArgb(248, 248, 247)
        btnActive.BorderRadius = 10
        btnAll.BorderRadius = 10
        btnInactive.BorderRadius = 10

        ' Reset filter button: hover -> gold background with white text
        btnResetFilter.HoverState.FillColor = Color.FromArgb(253, 198, 44)
        btnResetFilter.HoverState.ForeColor = Color.White

        ' Export button: same hover as reset filter + clickable cursor
        Exportbtn.HoverState.FillColor = Color.FromArgb(253, 198, 44)
        Exportbtn.HoverState.ForeColor = Color.White
        Exportbtn.Cursor = Cursors.Hand

        ' Add Product button: clickable cursor
        Guna2Button1.Cursor = Cursors.Hand

        ' Enable double buffering for smooth scrollinga
        SetDoubleBuffered(Guna2DataGridView1)
        ' ... inside Inventory_Load, after CreateNavigationMenu() and InitializeProfileSection()
        ' Apply the new visual palette (non-destructive � overrides colors at runtime)
        ' Initialize custom tooltip system
        InitializeCustomTooltip()
        ' Stop idle timeout monitoring
        IdleTimeoutManager.Instance.StartMonitoring(Me)
        ' Make form full-screen and non-resizable like Dashboard/Sales; cover entire screen including taskbar
        Me.FormBorderStyle = FormBorderStyle.None
        Me.TopMost = True
        Me.WindowState = FormWindowState.Normal
        Me.Bounds = Screen.PrimaryScreen.Bounds
        Me.WindowState = FormWindowState.Maximized

        ' Validate user session
        If Not ValidateUserSession() Then
            Return
        End If

        ' Create navigation menu using shared NavigationBuilder
        NavigationBuilder.Build(DashboardPanel, Me, "Inventory")

        ' Initialize profile section
        InitializeProfileSection()

        ' Setup filter events
        SetupFilterEvents()

        ' Load categories for filter
        LoadCategoriesForFilter()


        ' Update form title to show logged-in user
        Me.Text = $"Inventory - {frmLoginvb.LoggedInUsername}"

        ' Start idle timeout monitoring
        IdleTimeoutManager.Instance.StartMonitoring(Me)

        ' Load products asynchronously (ShowLoadingOverlay is now called inside LoadProductsAsync)
        Await LoadProductsAsync()
    End Sub
    ' Helper: find a control by name anywhere in the form (recursive)
    Private Function FindControlRecursive(parent As Control, name As String) As Control
        If parent Is Nothing Then Return Nothing
        If String.Equals(parent.Name, name, StringComparison.OrdinalIgnoreCase) Then
            Return parent
        End If

        For Each c As Control In parent.Controls
            Dim found = FindControlRecursive(c, name)
            If found IsNot Nothing Then Return found
        Next

        Return Nothing
    End Function
    Private Sub InitializeCustomTooltip()
        Try
            ' Dispose any existing tooltip and timer first
            If customTooltip IsNot Nothing Then
                customTooltip.Dispose()
                customTooltip = Nothing
            End If

            If tooltipTimer IsNot Nothing Then
                tooltipTimer.Stop()
                tooltipTimer.Dispose()
                tooltipTimer = Nothing
            End If

            ' Create main tooltip
            customTooltip = New ToolTip()
            customTooltip.AutoPopDelay = 8000  ' Show for 8 seconds
            customTooltip.InitialDelay = 300   ' Show after 300ms
            customTooltip.ReshowDelay = 100    ' Quick reshow
            customTooltip.ShowAlways = True
            customTooltip.UseAnimation = True
            customTooltip.UseFading = True
            customTooltip.IsBalloon = False

            ' Create timer for delayed tooltip display
            tooltipTimer = New Timer()
            tooltipTimer.Interval = 500  ' 500ms delay
            AddHandler tooltipTimer.Tick, AddressOf TooltipTimer_Tick

        Catch ex As Exception
            ' Fallback if tooltip initialization fails
            Try
                customTooltip = New ToolTip()
            Catch
                ' If even basic tooltip fails, leave as Nothing and handle in other methods
                customTooltip = Nothing
            End Try
        End Try
    End Sub

    Private Sub TooltipTimer_Tick(sender As Object, e As EventArgs)
        Try
            ' Stop the timer
            tooltipTimer.Stop()

            ' Show tooltip if we still have a valid cell and mouse hasn't moved significantly
            If currentTooltipCell IsNot Nothing AndAlso currentTooltipCell.DataGridView IsNot Nothing Then
                Dim currentMousePos As Point = Guna2DataGridView1.PointToClient(Cursor.Position)

                ' Check if mouse is still in a reasonable area
                If Math.Abs(currentMousePos.X - lastMousePosition.X) < 10 AndAlso
                   Math.Abs(currentMousePos.Y - lastMousePosition.Y) < 10 Then
                    ShowTooltipForCell(currentTooltipCell)
                End If
            End If

        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    Private Sub ShowTooltipForCell(cell As DataGridViewCell)
        Try
            If cell IsNot Nothing AndAlso cell.RowIndex >= 0 AndAlso
               Guna2DataGridView1.Columns(cell.ColumnIndex).Name = "ProductName" Then

                Dim productData As Dictionary(Of String, Object) = CType(Guna2DataGridView1.Rows(cell.RowIndex).Tag, Dictionary(Of String, Object))

                If productData IsNot Nothing Then
                    Dim fullProductName As String = productData("ProductName").ToString()
                    Dim productCode As String = productData("ProductCode").ToString()
                    Dim category As String = productData("Category").ToString()

                    ' Create tooltip text without barcode (since ProductCode serves as barcode now)
                    Dim tooltipText As String = $"Product: {fullProductName}" & Environment.NewLine &
                                               $"Code: {productCode}" & Environment.NewLine &
                                               $"Category: {category}"

                    ' Calculate tooltip position
                    Dim cellRect As Rectangle = Guna2DataGridView1.GetCellDisplayRectangle(cell.ColumnIndex, cell.RowIndex, False)
                    Dim tooltipPoint As Point = Guna2DataGridView1.PointToScreen(New Point(cellRect.X + 10, cellRect.Y + cellRect.Height + 5))

                    ' Hide any existing tooltip first
                    customTooltip.Hide(Guna2DataGridView1)

                    ' Show new tooltip
                    customTooltip.Show(tooltipText, Guna2DataGridView1, Guna2DataGridView1.PointToClient(tooltipPoint))
                End If
            End If

        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    Private Sub SetDoubleBuffered(ctrl As Control)
        Try
            Dim prop = ctrl.GetType().GetProperty("DoubleBuffered", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
            If prop IsNot Nothing Then prop.SetValue(ctrl, True, Nothing)
        Catch ex As Exception
            ' Silent fail for double buffering
        End Try
    End Sub

    Private Sub ShowLoadingOverlay()
        If DashboardPanel IsNot Nothing Then DashboardPanel.ShadowDecoration.Enabled = False
        If Guna2Panel1 IsNot Nothing Then Guna2Panel1.ShadowDecoration.Enabled = False

        ' Create loading overlay with DarkGray background
        loadingOverlay = New Panel()
        loadingOverlay.BackColor = Color.Transparent ' Changed to DarkGray
        loadingOverlay.Dock = DockStyle.Fill
        loadingOverlay.Location = New Point(0, 0)
        loadingOverlay.Size = Me.ClientSize

        ' Add overlay to form
        Me.Controls.Add(loadingOverlay)
        loadingOverlay.BringToFront()

        ' Create loading label
        Dim loadingLabel As New Label With {
            .Text = "Loading Inventory...",
            .ForeColor = Color.FromArgb(42, 42, 42),
            .Font = New Font("Poppins", 16, FontStyle.Regular),
            .AutoSize = True,
            .BackColor = Color.Transparent
        }

        ' Add label to overlay
        loadingOverlay.Controls.Add(loadingLabel)

        ' Center the label after it's added to the overlay
        CenterLoadingLabel(loadingLabel)

        ' Add resize handler to keep label centered
        AddHandler loadingOverlay.SizeChanged, Sub()
                                                   CenterLoadingLabel(loadingLabel)
                                               End Sub
    End Sub

    Private Sub CenterLoadingLabel(loadingLabel As Label)
        Try
            If loadingLabel IsNot Nothing AndAlso loadingOverlay IsNot Nothing Then
                ' Force the label to measure its size
                loadingLabel.AutoSize = True
                Application.DoEvents() ' Let the system calculate the size

                ' Center the label
                loadingLabel.Location = New Point(
                    (loadingOverlay.Width - loadingLabel.Width) \ 2,
                    (loadingOverlay.Height - loadingLabel.Height) \ 2
                )
            End If
        Catch ex As Exception
            ' Silent fail for centering issues
        End Try
    End Sub

    Private Sub HideLoadingOverlay()
        If loadingOverlay IsNot Nothing Then
            Me.Controls.Remove(loadingOverlay)
            loadingOverlay.Dispose()
            loadingOverlay = Nothing
        End If
        If DashboardPanel IsNot Nothing Then DashboardPanel.ShadowDecoration.Enabled = True
        If Guna2Panel1 IsNot Nothing Then Guna2Panel1.ShadowDecoration.Enabled = True
    End Sub

    Private Async Function LoadProductsAsync() As Task
        Try
            ' Show loading panel first with minimum display time
            ShowLoadingOverlay()
            Await Task.Delay(200) ' Let UI render the overlay

            ' Start timing to ensure minimum loading display time
            Dim startTime As DateTime = DateTime.Now

            ' Run the loading operation on a background thread
            Await Task.Run(Sub()
                               LoadProductsFromDatabase()
                           End Sub)

            ' Ensure minimum loading display time of 1 second
            Dim elapsedMs As Integer = CInt((DateTime.Now - startTime).TotalMilliseconds)
            If elapsedMs < 1000 Then
                Await Task.Delay(1000 - elapsedMs)
            End If

            ' Update UI on the main thread
            ' Replace occurrences that previously wrote item counts into lblUsername.
            ' Inside LoadProductsAsync, update the UI section:
            If Me.IsHandleCreated Then
                Me.Invoke(Sub()
                              ' Initially show all products
                              filteredProducts = New List(Of Dictionary(Of String, Object))(allProducts)

                              _currentPage = 1

                              ' Set up virtual scrolling and render
                              RefreshProductDisplay()

                              ' Hide loading overlay
                              HideLoadingOverlay()
                          End Sub)
            End If
        Catch ex As Exception
            ' Handle errors on main thread
            If Me.IsHandleCreated Then
                Me.Invoke(Sub()
                              HideLoadingOverlay()
                              MessageBox.Show("Error loading products: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                          End Sub)
            End If
        End Try
    End Function
    Private Sub LoadProductsFromDatabase()
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqliteConnection(connStr)
                conn.Open()

                Dim query As String = "SELECT p.ProductID, p.ProductCode, p.ProductName, p.Category, " &
                      "p.Unit, p.CurrentStock, p.ReorderLevel, p.CostPrice, p.SellingPrice, p.IsActive, " &
                      "pi.FilePath AS ProductImage " &
                      "FROM Products p " &
                      "LEFT JOIN ProductImageMapping pim ON p.ProductID = pim.ProductID " &
                      "LEFT JOIN ProductImages pi ON pim.ImageID = pi.ImageID " &
                      "ORDER BY p.ProductName"

                Using cmd As New SqliteCommand(query, conn)
                    Using reader As DbDataReader = cmd.ExecuteReader()
                        allProducts.Clear()

                        While reader.Read()
                            Dim currentStockVal As Integer = If(IsDBNull(reader("CurrentStock")), 0, Convert.ToInt32(reader("CurrentStock")))
                            Dim reorderLevelVal As Integer = If(IsDBNull(reader("ReorderLevel")), 0, Convert.ToInt32(reader("ReorderLevel")))
                            Dim sellingPriceVal As Decimal = If(IsDBNull(reader("SellingPrice")), 0D, Convert.ToDecimal(reader("SellingPrice")))
                            Dim costPriceVal As Decimal = If(IsDBNull(reader("CostPrice")), 0D, Convert.ToDecimal(reader("CostPrice")))
                            Dim unitVal As String = If(IsDBNull(reader("Unit")), String.Empty, reader("Unit").ToString())
                            Dim productCodeVal As String = If(IsDBNull(reader("ProductCode")), String.Empty, reader("ProductCode").ToString())
                            Dim productNameVal As String = If(IsDBNull(reader("ProductName")), String.Empty, reader("ProductName").ToString())
                            Dim categoryVal As String = If(IsDBNull(reader("Category")), String.Empty, reader("Category").ToString())
                            Dim productImageObj As Object = If(IsDBNull(reader("ProductImage")), Nothing, reader("ProductImage"))
                            Dim isActiveVal As Boolean = If(IsDBNull(reader("IsActive")), True, Convert.ToBoolean(reader("IsActive")))

                            ' Set status: zero stock = Out of Stock, if stock > 0 AND reorder > 0 AND stock <= reorder -> Below Reorder Level (B.R.L).
                            ' Otherwise treat as Above Reorder Level.
                            Dim statusStr As String
                            If currentStockVal = 0 Then
                                statusStr = "Out of Stock"
                            ElseIf reorderLevelVal > 0 AndAlso currentStockVal > 0 AndAlso currentStockVal <= reorderLevelVal Then
                                statusStr = "Below Reorder Level"
                            Else
                                statusStr = "Above Reorder Level"
                            End If

                            Dim productData As New Dictionary(Of String, Object) From {
                        {"ProductID", reader("ProductID")},
                        {"ProductCode", productCodeVal},
                        {"ProductName", productNameVal},
                        {"Category", categoryVal},
                        {"Unit", unitVal},
                        {"CurrentStock", currentStockVal},
                        {"ReorderLevel", reorderLevelVal},
                        {"CostPrice", costPriceVal},
                        {"SellingPrice", sellingPriceVal},
                        {"ProductImage", productImageObj},
                        {"Sizing", unitVal},
                        {"Description", String.Empty},
                        {"Price", sellingPriceVal},
                        {"StockQty", currentStockVal},
                        {"Status", statusStr},      ' stock-based status kept for other logic
                        {"IsActive", isActiveVal}, ' new flag
                        {"Color", String.Empty}
                    }

                            allProducts.Add(productData)
                        End While
                    End Using
                End Using
            End Using

        Catch ex As Exception
            Console.WriteLine($"LoadProductsFromDatabase error: {ex.Message}")
        End Try
    End Sub
    ' Replace SetupFilterEvents body with this version that finds and wires the status buttons reliably.
    Private Sub SetupFilterEvents()
        ' Setup filter events
        AddHandler txtSearch.TextChanged, AddressOf ApplyFilters
        AddHandler Guna2ComboBox1.SelectedIndexChanged, AddressOf ApplyFilters
        AddHandler StockCmbBox.SelectedIndexChanged, AddressOf ApplyFilters
        AddHandler txtFilterQuantity.TextChanged, AddressOf ApplyFilters
        AddHandler txtMinPrice.TextChanged, AddressOf ApplyFilters
        AddHandler txtMaxPrice.TextChanged, AddressOf ApplyFilters
        AddHandler btnResetFilter.Click, AddressOf ResetFilters_Click

        ' Wire status buttons (search recursively)
        Try
            Dim btnAll = TryCast(FindControlRecursive(Me, "btnAll"), Guna.UI2.WinForms.Guna2Button)
            Dim btnActive = TryCast(FindControlRecursive(Me, "btnActive"), Guna.UI2.WinForms.Guna2Button)
            Dim btnInactive = TryCast(FindControlRecursive(Me, "btnInactive"), Guna.UI2.WinForms.Guna2Button)

            If btnAll IsNot Nothing Then
                RemoveHandler btnAll.Click, AddressOf BtnAll_Click
                AddHandler btnAll.Click, AddressOf BtnAll_Click
            End If

            If btnActive IsNot Nothing Then
                RemoveHandler btnActive.Click, AddressOf BtnActive_Click
                AddHandler btnActive.Click, AddressOf BtnActive_Click
            End If

            If btnInactive IsNot Nothing Then
                RemoveHandler btnInactive.Click, AddressOf BtnInactive_Click
                AddHandler btnInactive.Click, AddressOf BtnInactive_Click
            End If
        Catch
            ' ignore wiring issues
        End Try

        ' Populate stock filter options (use "Above Reorder Level" instead of "Active")
        If StockCmbBox.Items.Count = 0 Then
            StockCmbBox.Items.Clear()
            StockCmbBox.Items.Add("All")
            StockCmbBox.Items.Add("Below Reorder Level")
            StockCmbBox.Items.Add("Out of Stock")
            StockCmbBox.Items.Add("Above Reorder Level")
            StockCmbBox.Items.Add("Inactive")
        End If
        StockCmbBox.SelectedIndex = 0 ' Select "All"

        ' Initialize visuals for status buttons
        SetStatusButtonsVisualState()
    End Sub    ' Replace existing BtnAll/BtnActive/BtnInactive handlers with these (they already set the filter).
    ' Replace the three Btn handlers with these (they already set the filter).
    Private Sub BtnAll_Click(sender As Object, e As EventArgs)
        statusFilter = Nothing
        SetStatusButtonsVisualState()
        ApplyFilters(Nothing, Nothing)
    End Sub

    Private Sub BtnActive_Click(sender As Object, e As EventArgs)
        statusFilter = True
        SetStatusButtonsVisualState()
        ApplyFilters(Nothing, Nothing)
    End Sub

    Private Sub BtnInactive_Click(sender As Object, e As EventArgs)
        statusFilter = False
        SetStatusButtonsVisualState()
        ApplyFilters(Nothing, Nothing)
    End Sub
    Private Sub LoadCategoriesForFilter()
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqliteConnection(connStr)
                conn.Open()

                ' Get distinct categories from existing products
                Dim query As String = "SELECT DISTINCT Category FROM Products WHERE Category IS NOT NULL AND IsActive = 1 ORDER BY Category"
                Using cmd As New SqliteCommand(query, conn)
                    Using reader As DbDataReader = cmd.ExecuteReader()
                        Guna2ComboBox1.Items.Clear()
                        Guna2ComboBox1.Items.Add("All Categories") ' Add default option

                        While reader.Read()
                            If Not IsDBNull(reader("Category")) Then
                                Guna2ComboBox1.Items.Add(reader("Category").ToString())
                            End If
                        End While

                        ' Select "All Categories" by default
                        Guna2ComboBox1.SelectedIndex = 0
                    End Using
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show("Error loading categories for filter: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub RefreshProductDisplay()
        ' Set up DataGridView instead of virtual scrolling panels
        SetupProductDataGrid()

        ' Configure pagination for the filtered set (bar is created by SetupProductDataGrid)
        If _pagination IsNot Nothing Then
            Dim total As Integer = If(filteredProducts Is Nothing, 0, filteredProducts.Count)
            Dim maxPage As Integer = If(total = 0, 1, CInt(Math.Ceiling(CDbl(total) / _pageSize)))
            If _currentPage > maxPage Then _currentPage = maxPage
            If _currentPage < 1 Then _currentPage = 1
            _pagination.Configure(total, _pageSize, _currentPage)
            _currentPage = _pagination.CurrentPage
        End If

        LoadProductsIntoDataGrid()
    End Sub

    Private Sub SetupProductDataGrid()
        Try
            ' Use the existing Guna2DataGridView1 control
            Dim productDataGrid As Guna.UI2.WinForms.Guna2DataGridView = Guna2DataGridView1

            ' Set DataGridView properties
            productDataGrid.AutoGenerateColumns = False
            productDataGrid.AllowUserToAddRows = False
            productDataGrid.AllowUserToDeleteRows = False
            productDataGrid.AllowUserToResizeColumns = False
            productDataGrid.AllowUserToResizeRows = False
            productDataGrid.ReadOnly = True
            productDataGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            productDataGrid.MultiSelect = False
            productDataGrid.ScrollBars = ScrollBars.Vertical
            productDataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            productDataGrid.RowHeadersVisible = False
            productDataGrid.EnableHeadersVisualStyles = False

            ' Disable DataGridView's built-in tooltips to prevent conflicts
            productDataGrid.ShowCellToolTips = False

            ' --- JADE CLINIC BRAND PALETTE ---
            productDataGrid.BackgroundColor = System.Drawing.Color.FromArgb(250, 249, 246)
            productDataGrid.GridColor = System.Drawing.Color.FromArgb(230, 230, 230)          ' BorderGray
            productDataGrid.BorderStyle = BorderStyle.None
            productDataGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            productDataGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None

            productDataGrid.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(255, 255, 255)  ' PureWhite
            productDataGrid.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(250, 249, 246)
            productDataGrid.DefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51)     ' DarkText
            productDataGrid.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(235, 228, 200) ' Olive-beige
            productDataGrid.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.FromArgb(51, 51, 51) ' DarkText
            productDataGrid.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
            productDataGrid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            productDataGrid.DefaultCellStyle.Padding = New Padding(10, 6, 10, 6)

            ' Header styling
            productDataGrid.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(250, 249, 246)
            productDataGrid.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51)  ' DarkText
            productDataGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(250, 249, 246)
            productDataGrid.ColumnHeadersDefaultCellStyle.Font = New Font("Poppins SemiBold", 9.0F, FontStyle.Bold)
            productDataGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            productDataGrid.ColumnHeadersHeight = 40
            productDataGrid.RowTemplate.Height = 75
            productDataGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            productDataGrid.ThemeStyle.RowsStyle.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
            productDataGrid.ThemeStyle.HeaderStyle.Font = New Font("Poppins", 9.0F, FontStyle.Bold)

            ' Pagination bar (same control used by Sales, which renders the count correctly)
            ' docked to the bottom of the panel; the grid shrinks to sit above it
            If _pagination Is Nothing Then
                _pagination = New PaginationControl()
                AddHandler _pagination.PageChanged, AddressOf OnPaginationPageChanged
                Guna2Panel1.Controls.Add(_pagination)
                _pagination.BringToFront()
            End If
            _pagination.Width = Guna2Panel1.Width
            _pagination.Location = New Point(0, Guna2Panel1.Height - _pagination.Height)
            productDataGrid.Location = New Point(3, 3)
            productDataGrid.Width = Guna2Panel1.Width - 8
            productDataGrid.Height = _pagination.Location.Y - 9

            ' Clear existing columns
            productDataGrid.Columns.Clear()

            ' Jade Clinic brand accents
            Dim GoldenYellow As Color = System.Drawing.Color.FromArgb(254, 191, 16)
            Dim JadeOlive As Color = System.Drawing.Color.FromArgb(190, 154, 48)
            Dim DarkText As Color = System.Drawing.Color.FromArgb(51, 51, 51)
            Dim MediumText As Color = System.Drawing.Color.FromArgb(102, 102, 102)

            ' Product Information column (slightly shorter to make room for Status)
            ' In SetupProductDataGrid(), replace ProductName column config with this:

            Dim colProductName As New DataGridViewTextBoxColumn()
            colProductName.Name = "ProductName"
            colProductName.HeaderText = "Product Information"
            colProductName.FillWeight = 35
            colProductName.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
            colProductName.DefaultCellStyle.ForeColor = DarkText
            colProductName.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
            colProductName.DefaultCellStyle.SelectionBackColor = Color.FromArgb(235, 228, 200)
            colProductName.DefaultCellStyle.SelectionForeColor = DarkText
            productDataGrid.Columns.Add(colProductName)
            ' Category column (slightly narrowed)
            Dim colCategory As New DataGridViewTextBoxColumn()
            colCategory.Name = "Category"
            colCategory.HeaderText = "Category"
            colCategory.FillWeight = 16
            colCategory.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            colCategory.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
            colCategory.DefaultCellStyle.ForeColor = MediumText
            colCategory.DefaultCellStyle.SelectionBackColor = Color.FromArgb(235, 228, 200)
            colCategory.DefaultCellStyle.SelectionForeColor = DarkText
            productDataGrid.Columns.Add(colCategory)

            ' Unit column (slightly narrowed)
            Dim colUnit As New DataGridViewTextBoxColumn()
            colUnit.Name = "Unit"
            colUnit.HeaderText = "Unit"
            colUnit.FillWeight = 8
            colUnit.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            colUnit.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
            colUnit.DefaultCellStyle.ForeColor = MediumText
            colUnit.DefaultCellStyle.SelectionBackColor = Color.FromArgb(235, 228, 200)
            colUnit.DefaultCellStyle.SelectionForeColor = DarkText
            productDataGrid.Columns.Add(colUnit)

            ' Stock column (slightly narrowed)
            Dim colStock As New DataGridViewTextBoxColumn()
            colStock.Name = "CurrentStock"
            colStock.HeaderText = "Stock"
            colStock.FillWeight = 9
            colStock.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            colStock.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
            colStock.DefaultCellStyle.ForeColor = MediumText
            colStock.DefaultCellStyle.SelectionBackColor = Color.FromArgb(235, 228, 200)
            colStock.DefaultCellStyle.SelectionForeColor = DarkText
            productDataGrid.Columns.Add(colStock)

            ' Cost Price column
            Dim colCostPrice As New DataGridViewTextBoxColumn()
            colCostPrice.Name = "CostPrice"
            colCostPrice.HeaderText = "Cost"
            colCostPrice.FillWeight = 12
            colCostPrice.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            colCostPrice.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
            colCostPrice.DefaultCellStyle.ForeColor = MediumText
            colCostPrice.DefaultCellStyle.SelectionBackColor = Color.FromArgb(235, 228, 200)
            colCostPrice.DefaultCellStyle.SelectionForeColor = DarkText
            productDataGrid.Columns.Add(colCostPrice)

            ' Selling Price column
            Dim colSellingPrice As New DataGridViewTextBoxColumn()
            colSellingPrice.Name = "SellingPrice"
            colSellingPrice.HeaderText = "Price"
            colSellingPrice.FillWeight = 14
            colSellingPrice.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            colSellingPrice.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
            colSellingPrice.DefaultCellStyle.ForeColor = DarkText
            colSellingPrice.DefaultCellStyle.SelectionBackColor = Color.FromArgb(235, 228, 200)
            colSellingPrice.DefaultCellStyle.SelectionForeColor = DarkText
            productDataGrid.Columns.Add(colSellingPrice)

            ' Status column (Active / Inactive) - fixed width for consistent layout
            Dim colStatus As New DataGridViewTextBoxColumn()
            colStatus.Name = "Status"
            colStatus.HeaderText = "Status"

            ' Make the column fixed-width (not participating in Fill autosizing)
            colStatus.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            colStatus.Width = 130
            colStatus.MinimumWidth = 70
            colStatus.MaxInputLength = 50
            colStatus.ReadOnly = True

            colStatus.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            colStatus.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
            colStatus.DefaultCellStyle.ForeColor = MediumText
            colStatus.DefaultCellStyle.SelectionBackColor = Color.FromArgb(235, 228, 200)
            colStatus.DefaultCellStyle.SelectionForeColor = DarkText

            productDataGrid.Columns.Add(colStatus)
            ' Actions column — fixed width, non-resizable, shows edit icon/text
            Dim colActions As New DataGridViewTextBoxColumn()
            colActions.Name = "Actions"
            colActions.HeaderText = ""
            colActions.ReadOnly = True

            ' Make the column fixed-width so layout is stable
            colActions.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            colActions.Width = 20
            colActions.MinimumWidth = 60
            colActions.Resizable = DataGridViewTriState.False
            colActions.SortMode = DataGridViewColumnSortMode.NotSortable
            colActions.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter

            ' Visuals
            colActions.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(255, 255, 255)
            colActions.DefaultCellStyle.ForeColor = JadeOlive
            colActions.DefaultCellStyle.SelectionBackColor = Color.FromArgb(235, 228, 200)
            colActions.DefaultCellStyle.SelectionForeColor = DarkText
            colActions.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Regular)

            ' Show a default edit marker when cell value is null/empty
            colActions.DefaultCellStyle.NullValue = ChrW(&H270F)
            productDataGrid.Columns.Add(colActions)

            ' Wire up events and painting (preserve existing handlers)
            RemoveHandler productDataGrid.CellContentClick, AddressOf ProductDataGrid_CellContentClick
            AddHandler productDataGrid.CellContentClick, AddressOf ProductDataGrid_CellContentClick

            RemoveHandler productDataGrid.CellPainting, AddressOf ProductDataGrid_CellPainting
            AddHandler productDataGrid.CellPainting, AddressOf ProductDataGrid_CellPainting

            RemoveHandler productDataGrid.CellMouseEnter, AddressOf ProductDataGrid_CellMouseEnter
            AddHandler productDataGrid.CellMouseEnter, AddressOf ProductDataGrid_CellMouseEnter

            RemoveHandler productDataGrid.CellMouseLeave, AddressOf ProductDataGrid_CellMouseLeave
            AddHandler productDataGrid.CellMouseLeave, AddressOf ProductDataGrid_CellMouseLeave

            RemoveHandler productDataGrid.MouseMove, AddressOf ProductDataGrid_MouseMove
            AddHandler productDataGrid.MouseMove, AddressOf ProductDataGrid_MouseMove

            RemoveHandler productDataGrid.Enter, AddressOf ProductDataGrid_Enter
            AddHandler productDataGrid.Enter, AddressOf ProductDataGrid_Enter

            RemoveHandler productDataGrid.Leave, AddressOf ProductDataGrid_Leave
            AddHandler productDataGrid.Leave, AddressOf ProductDataGrid_Leave

        Catch ex As Exception
            MessageBox.Show($"Error setting up DataGrid: {ex.Message}", "DataGrid Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub LoadProductsIntoDataGrid()
        Try
            ' Use the existing Guna2DataGridView1 control
            Dim productDataGrid As Guna.UI2.WinForms.Guna2DataGridView = Guna2DataGridView1

            ' Clear existing rows
            productDataGrid.Rows.Clear()

            ' Hide any existing "No records" message
            DataGridViewHelper.HideNoRecordsMessage()

            ' Handle no products found
            Dim total As Integer = If(filteredProducts Is Nothing, 0, filteredProducts.Count)
            If total = 0 Then
                DataGridViewHelper.ShowNoRecordsMessage(productDataGrid, "No Products Found")
                Return
            End If

            ' Render only the current page
            Dim pageItems As IEnumerable(Of Dictionary(Of String, Object)) =
                filteredProducts.Skip((_currentPage - 1) * _pageSize).Take(_pageSize)

            ' Define lighter palette colors
            Dim LightRed As Color = Color.FromArgb(220, 80, 70)        ' red for out of stock / inactive
            Dim LightOrange As Color = Color.FromArgb(230, 150, 40)    ' orange for below reorder (B.R.L)
            Dim LightGreen As Color = Color.FromArgb(80, 160, 80)      ' green for above reorder (A.R.L)
            Dim JadeOlive As Color = Color.FromArgb(190, 154, 48)

            ' Load filtered products into DataGridView
            For Each productData As Dictionary(Of String, Object) In pageItems
                Try
                    ' Create display text for product name ONLY (remove code duplication)
                    Dim productName As String = productData("ProductName").ToString()
                    Dim displayName As String = productName

                    ' Truncate long names with ellipsis for better layout
                    If displayName.Length > 40 Then ' Increased character limit since we have more space
                        displayName = displayName.Substring(0, 37) + "..."
                    End If

                    ' Get stock values for color coding
                    Dim currentStock As Integer = Convert.ToInt32(productData("CurrentStock"))
                    Dim reorderLevel As Integer = Convert.ToInt32(productData("ReorderLevel"))

                    ' Determine status text from IsActive flag for grid column
                    Dim isActiveFlag As Boolean = If(productData.ContainsKey("IsActive"), Convert.ToBoolean(productData("IsActive")), True)
                    Dim statusDisplay As String = If(isActiveFlag, "Active", "Inactive")

                    ' Add row to DataGridView - include Status before Actions
                    Dim rowIndex As Integer = productDataGrid.Rows.Add(
                displayName,  ' ProductName
                productData("Category").ToString(),     ' Category
                productData("Unit").ToString(),         ' Unit
                currentStock.ToString(),                ' CurrentStock
                "₱" & Convert.ToDecimal(productData("CostPrice")).ToString("N2"),    ' CostPrice
                "₱" & Convert.ToDecimal(productData("SellingPrice")).ToString("N2"), ' SellingPrice
                statusDisplay, ' Status
                ChrW(&H270F)  ' ✏ pencil icon for Actions
            )

                    ' Store product data in row tag for edit functionality
                    productDataGrid.Rows(rowIndex).Tag = productData

                    ' Apply stock level color coding with lighter colors
                    If currentStock = 0 Then
                        productDataGrid.Rows(rowIndex).Cells("CurrentStock").Style.ForeColor = LightRed
                        productDataGrid.Rows(rowIndex).Cells("CurrentStock").Style.Font = New Font("Poppins", 9.5F, FontStyle.Bold)
                        productDataGrid.Rows(rowIndex).Cells("CurrentStock").Tag = "OUT_OF_STOCK"
                    ElseIf reorderLevel > 0 AndAlso currentStock <= reorderLevel Then
                        ' B.R.L when stock is equal to or less than reorder (and > 0)
                        productDataGrid.Rows(rowIndex).Cells("CurrentStock").Style.ForeColor = LightOrange
                        productDataGrid.Rows(rowIndex).Cells("CurrentStock").Style.Font = New Font("Poppins", 9.5F, FontStyle.Bold)
                        productDataGrid.Rows(rowIndex).Cells("CurrentStock").Tag = "LOW_STOCK"
                    Else
                        productDataGrid.Rows(rowIndex).Cells("CurrentStock").Style.ForeColor = LightGreen
                        productDataGrid.Rows(rowIndex).Cells("CurrentStock").Style.Font = New Font("Poppins", 9.5F, FontStyle.Bold)
                        productDataGrid.Rows(rowIndex).Cells("CurrentStock").Tag = "IN_STOCK"
                    End If

                    ' Apply status color: Active -> light green, Inactive -> light red (user requested)
                    Dim statusCell = productDataGrid.Rows(rowIndex).Cells("Status")
                    If isActiveFlag Then
                        statusCell.Style.ForeColor = LightGreen
                        statusCell.Style.Font = New Font("Poppins", 9.0F, FontStyle.Bold)
                    Else
                        statusCell.Style.ForeColor = LightRed
                        statusCell.Style.Font = New Font("Poppins", 9.0F, FontStyle.Bold)
                    End If

                    ' Make row non-resizable
                    productDataGrid.Rows(rowIndex).Resizable = DataGridViewTriState.False

                Catch ex As Exception
                    ' Continue with next product if there's an error with this one
                    Continue For
                End Try
            Next

        Catch ex As Exception
            MessageBox.Show($"Error loading products into DataGrid: {ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub OnPaginationPageChanged(page As Integer)
        _currentPage = page
        LoadProductsIntoDataGrid()
    End Sub
    Private Sub ProductDataGrid_CellContentClick(sender As Object, e As DataGridViewCellEventArgs)
        Try
            If e.RowIndex >= 0 AndAlso e.ColumnIndex >= 0 Then
                Dim productDataGrid As Guna.UI2.WinForms.Guna2DataGridView = CType(sender, Guna.UI2.WinForms.Guna2DataGridView)

                ' Check if the Actions column was clicked
                If productDataGrid.Columns(e.ColumnIndex).Name = "Actions" Then
                    ' Get product data from row tag
                    Dim productData As Dictionary(Of String, Object) = CType(productDataGrid.Rows(e.RowIndex).Tag, Dictionary(Of String, Object))

                    If productData IsNot Nothing Then
                        Dim productId As Integer = Convert.ToInt32(productData("ProductID"))
                        EditProduct_Click_FromGrid(productId)
                    End If
                End If
            End If
        Catch ex As Exception
            MessageBox.Show($"Error handling edit action: {ex.Message}", "Edit Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' REPLACE THESE TWO METHODS with improved overlay handling:

    ' Helper method to create professional overlay panel (matching EscForm dimming)
    Private Function CreateOverlayPanel() As Panel
        Dim overlay As New Panel()
        ' Match EscForm's professional semi-transparent dimming
        ' Alpha value 100-120 creates perfect visual dimming without being too dark
        overlay.BackColor = Color.FromArgb(100, 0, 0, 0) ' 55% opacity semi-transparent black
        overlay.Dock = DockStyle.Fill
        overlay.Location = New Point(0, 0)
        overlay.Size = Me.ClientSize
        ' Ensure overlay stays on top
        overlay.BringToFront()
        Return overlay
    End Function
    Private Function CreateOverlayForm() As Form
        Dim overlay As New Form() With {
            .FormBorderStyle = FormBorderStyle.None,
            .ShowInTaskbar = False,
            .StartPosition = FormStartPosition.Manual,
            .BackColor = Color.Black,
            .Opacity = 0.55,  ' ? Semi-transparent - lets background show through!
            .TopMost = False
        }
        overlay.Bounds = Me.Bounds
        overlay.Owner = Me
        overlay.Show()
        Return overlay
    End Function


    Private Sub EditProduct_Click_FromGrid(productId As Integer)
        Dim overlayForm As Form = Nothing
        Try
            ' Create overlay form (semi-transparent, like EscForm)
            overlayForm = CreateOverlayForm()

            ' Create and show Edit Product form
            Dim editProductForm As New AddProduct()
            editProductForm.SetEditMode(productId)
            editProductForm.StartPosition = FormStartPosition.CenterScreen
            editProductForm.TopMost = True

            Utilities.EnableEscCloseModal(editProductForm)
            Dim result As DialogResult = editProductForm.ShowDialog(Me)

            ' Refresh the inventory list if product was updated
            If result = DialogResult.OK Then
                LoadProducts()
            End If

        Catch ex As Exception
            MessageBox.Show($"Error opening edit form: {ex.Message}", "Edit Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            ' Cleanup overlay
            If overlayForm IsNot Nothing Then
                overlayForm.Close()
                overlayForm.Dispose()
            End If
        End Try
    End Sub

    Private Function GetCachedLogo() As Image
        If _cachedLogo Is Nothing Then
            _cachedLogo = CompanySettingsManager.Instance.GetCompanyLogo()
        End If
        Return _cachedLogo
    End Function

    Private Sub ProductDataGrid_CellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
        Try
            ' Customize both ProductName and CurrentStock columns
            If e.ColumnIndex >= 0 AndAlso e.RowIndex >= 0 Then
                Dim productDataGrid As Guna.UI2.WinForms.Guna2DataGridView = CType(sender, Guna.UI2.WinForms.Guna2DataGridView)

                ' Handle ProductName column for image and enhanced layout
                If productDataGrid.Columns(e.ColumnIndex).Name = "ProductName" Then
                    ' Get the product data
                    Dim productData As Dictionary(Of String, Object) = CType(productDataGrid.Rows(e.RowIndex).Tag, Dictionary(Of String, Object))

                    If productData IsNot Nothing Then
                        e.Handled = True
                        e.PaintBackground(e.ClipBounds, True)

                        ' Define Jade Clinic brand colors
                        Dim GoldenYellow As Color = Color.FromArgb(254, 191, 16)
                        Dim JadeOlive As Color = Color.FromArgb(190, 154, 48)
                        Dim MediumGray As Color = Color.FromArgb(120, 120, 120)
                        Dim LightGray As Color = Color.FromArgb(200, 200, 200)
                        Dim DarkText As Color = Color.FromArgb(51, 51, 51)

                        ' Calculate layout dimensions with better spacing
                        Dim imageSize As Integer = 50
                        Dim padding As Integer = 10
                        Dim imageRect As New Rectangle(
                        e.CellBounds.Left + padding,
                        e.CellBounds.Top + ((e.CellBounds.Height - imageSize) \ 2),
                        imageSize, imageSize)

                        Try
                            Dim img As Image = Nothing
                            If productData("ProductImage") IsNot Nothing Then
                                Dim filePath As String = productData("ProductImage").ToString()
                                If Not String.IsNullOrEmpty(filePath) Then
                                    Dim fullPath As String = Path.Combine(Connection.GetImagesFolder("products"), filePath)
                                    If IO.File.Exists(fullPath) Then
                                        img = Image.FromFile(fullPath)
                                    End If
                                End If
                            End If

                            If img IsNot Nothing Then
                                e.Graphics.DrawImage(img, imageRect)
                                Using borderPen As New Pen(LightGray, 1)
                                    e.Graphics.DrawRectangle(borderPen, imageRect)
                                End Using
                            Else
                                Dim logo As Image = My.Resources.product_placeholder
                                If logo IsNot Nothing Then
                                    e.Graphics.DrawImage(logo, imageRect)
                                    Using borderPen As New Pen(LightGray, 1)
                                        e.Graphics.DrawRectangle(borderPen, imageRect)
                                    End Using
                                Else
                                    Using bgBrush As New SolidBrush(Color.FromArgb(245, 245, 245))
                                        e.Graphics.FillRectangle(bgBrush, imageRect)
                                    End Using
                                    Using borderPen As New Pen(LightGray, 1)
                                        e.Graphics.DrawRectangle(borderPen, imageRect)
                                    End Using
                                    Using placeholderFont As New Font("Segoe UI", 8, FontStyle.Regular)
                                        Using placeholderBrush As New SolidBrush(MediumGray)
                                            Dim placeholderFormat As New StringFormat() With {
                                            .Alignment = StringAlignment.Center,
                                            .LineAlignment = StringAlignment.Center
                                        }
                                            e.Graphics.DrawString(Char.ConvertFromUtf32(&H1F4F7), placeholderFont, placeholderBrush, imageRect, placeholderFormat)
                                        End Using
                                    End Using
                                End If
                            End If
                            If img IsNot Nothing Then img.Dispose()
                        Catch
                            Using bgBrush As New SolidBrush(Color.FromArgb(245, 245, 245))
                                e.Graphics.FillRectangle(bgBrush, imageRect)
                            End Using
                            Using borderPen As New Pen(JadeOlive, 1)
                                e.Graphics.DrawRectangle(borderPen, imageRect)
                            End Using
                            Using noImageFont As New Font("Poppins", 7, FontStyle.Regular)
                                Using noImageBrush As New SolidBrush(MediumGray)
                                    Dim noImageFormat As New StringFormat() With {
                                    .Alignment = StringAlignment.Center,
                                    .LineAlignment = StringAlignment.Center
                                }
                                    e.Graphics.DrawString("No Image", noImageFont, noImageBrush, imageRect, noImageFormat)
                                End Using
                            End Using
                        End Try

                        ' Calculate text area with proper spacing
                        Dim textStartX As Integer = imageRect.Right + (padding * 2)
                        Dim textWidth As Integer = e.CellBounds.Width - textStartX - padding
                        Dim textRect As New Rectangle(textStartX, e.CellBounds.Top + 8, textWidth, e.CellBounds.Height - 16)

                        ' Get clean product name
                        Dim productName As String = productData("ProductName").ToString()
                        Dim displayName As String = productName

                        ' Truncate if needed for layout
                        If displayName.Length > 40 Then
                            displayName = displayName.Substring(0, 37) + "..."
                        End If

                        ' Determine if row is selected
                        Dim isRowSelected As Boolean = productDataGrid.Rows(e.RowIndex).Selected

                        ' Choose main text color based on selection (dark text idle, dark text on selection)
                        Dim productNameColor As Color = If(isRowSelected, DarkText, DarkText)
                        Using productNameFont As New Font("Poppins SemiBold", 10.5F, FontStyle.Bold)
                            Using productNameBrush As New SolidBrush(productNameColor)
                                Dim nameFormat As New StringFormat() With {
                                .LineAlignment = StringAlignment.Near,
                                .Alignment = StringAlignment.Near,
                                .Trimming = StringTrimming.EllipsisCharacter,
                                .FormatFlags = StringFormatFlags.NoWrap
                            }

                                ' Calculate name area (upper portion)
                                Dim nameRect As New Rectangle(textRect.X, textRect.Y + 2, textRect.Width, (textRect.Height * 60) \ 100)
                                e.Graphics.DrawString(displayName, productNameFont, productNameBrush, nameRect, nameFormat)
                            End Using
                        End Using

                        ' Draw product code as subtitle with consistent spacing
                        Dim productCode As String = productData("ProductCode").ToString()

                        ' Subtitle color: dark on gold selection, medium gray otherwise
                        Dim codeTextColor As Color = If(isRowSelected, DarkText, MediumGray)

                        Using codeFont As New Font("Poppins", 8.5F, FontStyle.Regular)
                            Using codeBrush As New SolidBrush(codeTextColor)
                                Dim codeFormat As New StringFormat() With {
                                .LineAlignment = StringAlignment.Near,
                                .Alignment = StringAlignment.Near
                            }

                                ' Calculate code area (lower portion)
                                Dim codeYStart As Integer = textRect.Y + (textRect.Height * 60) \ 100
                                Dim codeRect As New Rectangle(textRect.X, codeYStart + 4, textWidth, (textRect.Height * 40) \ 100)
                                e.Graphics.DrawString($"Code: {productCode}", codeFont, codeBrush, codeRect, codeFormat)
                            End Using
                        End Using
                    End If

                    ' Handle CurrentStock column for maintaining color coding when selected
                ElseIf productDataGrid.Columns(e.ColumnIndex).Name = "CurrentStock" Then
                    ' Get stock status from cell tag
                    Dim stockStatus As String = If(productDataGrid.Rows(e.RowIndex).Cells("CurrentStock").Tag?.ToString(), "IN_STOCK")
                    Dim isRowSelected As Boolean = productDataGrid.Rows(e.RowIndex).Selected

                    ' If row is selected, use the same medium colors for readability on soft selection
                    If isRowSelected Then
                        e.Handled = True
                        e.PaintBackground(e.ClipBounds, True)

                        ' Use the same medium stock colors on the soft selection background
                        Dim textColor As Color
                        Select Case stockStatus
                            Case "OUT_OF_STOCK"
                                textColor = Color.FromArgb(220, 80, 70)
                            Case "LOW_STOCK"
                                textColor = Color.FromArgb(230, 150, 40)
                            Case Else ' "IN_STOCK"
                                textColor = Color.FromArgb(80, 160, 80)
                        End Select

                        ' Draw the stock number with appropriate color
                        Using stockFont As New Font("Poppins", 9.5F, FontStyle.Bold)
                            Using stockBrush As New SolidBrush(textColor)
                                Dim stockFormat As New StringFormat() With {
                                .Alignment = StringAlignment.Center,
                                .LineAlignment = StringAlignment.Center
                            }

                                Dim stockText As String = If(e.Value IsNot Nothing, e.Value.ToString(), "0")
                                e.Graphics.DrawString(stockText, stockFont, stockBrush, e.CellBounds, stockFormat)
                            End Using
                        End Using
                    Else
                        ' Not selected: let default rendering (cell ForeColor was set at load) handle it
                    End If
                End If
            End If
        Catch ex As Exception
            ' Silent fail for custom painting to prevent crashes
        End Try
    End Sub

    ' ============================================================================
    ' FUNCTION 2: ADD PRODUCT BUTTON CLICK - REFACTORED
    ' ============================================================================
    ' Copy this ENTIRE method and REPLACE your existing Guna2Button1_Click
    ' Location: Replace the current Guna2Button1_Click in Inventory.vb
    ' ============================================================================
    Private Sub Guna2Button1_Click(sender As Object, e As EventArgs) Handles Guna2Button1.Click
        Dim overlayForm As Form = Nothing
        Try
            ' Create overlay form (semi-transparent, like EscForm)
            overlayForm = CreateOverlayForm()

            ' Create and show AddProduct form
            Dim addProductForm As New AddProduct()
            addProductForm.StartPosition = FormStartPosition.CenterScreen
            addProductForm.TopMost = True

            Utilities.EnableEscCloseModal(addProductForm)
            Dim result As DialogResult = addProductForm.ShowDialog(Me)

            ' Refresh if successful
            If result = DialogResult.OK Then
                LoadProducts()
            End If

        Catch ex As Exception
            MessageBox.Show($"Error opening add product form: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            ' Cleanup overlay
            If overlayForm IsNot Nothing Then
                overlayForm.Close()
                overlayForm.Dispose()
            End If
        End Try
    End Sub

    Private Function MatchesFilter(product As Dictionary(Of String, Object)) As Boolean
        Try
            ' Search filter (product name / barcode only)
            Dim searchText As String = txtSearch.Text.Trim().ToLower()
            If Not String.IsNullOrWhiteSpace(searchText) Then
                Dim productName As String = product("ProductName").ToString().ToLower()
                Dim productCode As String = product("ProductCode").ToString().ToLower()

                If Not (productName.Contains(searchText) Or productCode.Contains(searchText)) Then
                    Return False
                End If
            End If

            ' Category filter
            If Guna2ComboBox1.SelectedItem IsNot Nothing AndAlso Guna2ComboBox1.SelectedItem.ToString() <> "All Categories" Then
                If product("Category").ToString() <> Guna2ComboBox1.SelectedItem.ToString() Then
                    Return False
                End If
            End If

            ' Status buttons filter (Active / Inactive / All)
            If statusFilter.HasValue Then
                Dim isActive As Boolean = If(product.ContainsKey("IsActive"), Convert.ToBoolean(product("IsActive")), True)
                If isActive <> statusFilter.Value Then Return False
            End If

            ' Stock status filter
            If StockCmbBox.SelectedItem IsNot Nothing Then
                Dim currentStock As Integer = Convert.ToInt32(product("CurrentStock"))
                Dim reorderLevel As Integer = Convert.ToInt32(product("ReorderLevel"))
                Dim stockFilter As String = StockCmbBox.SelectedItem.ToString()

                Select Case stockFilter
                    Case "Out of Stock"
                        If currentStock > 0 Then Return False

                    Case "Below Re-order Level"
                        ' Exclude zero-stock items; show items with stock > 0 and stock <= reorder level (equality counts as B.R.L)
                        If Not (reorderLevel > 0 AndAlso currentStock > 0 AndAlso currentStock <= reorderLevel) Then Return False

                    Case "Above Re-order Level"
                        ' Show products that have a positive reorder level and stock strictly above it
                        If Not (reorderLevel > 0 AndAlso currentStock > reorderLevel) Then Return False

                    Case "Inactive"
                        Dim isActiveInv As Boolean = If(product.ContainsKey("IsActive"), Convert.ToBoolean(product("IsActive")), True)
                        If isActiveInv Then Return False

                        ' "All" does nothing
                End Select
            End If

            ' Quantity filter
            If Not String.IsNullOrWhiteSpace(txtFilterQuantity.Text) Then
                Dim filterQty As Integer
                If Integer.TryParse(txtFilterQuantity.Text.Trim(), filterQty) Then
                    Dim currentStock As Integer = Convert.ToInt32(product("CurrentStock"))
                    If currentStock < filterQty Then Return False
                End If
            End If

            ' Price range filter (min/max)
            If Not String.IsNullOrWhiteSpace(txtMinPrice.Text) Then
                Dim minPrice As Decimal
                If Decimal.TryParse(txtMinPrice.Text.Trim(), minPrice) Then
                    Dim sellingPrice As Decimal = Convert.ToDecimal(product("SellingPrice"))
                    If sellingPrice < minPrice Then Return False
                End If
            End If
            If Not String.IsNullOrWhiteSpace(txtMaxPrice.Text) Then
                Dim maxPrice As Decimal
                If Decimal.TryParse(txtMaxPrice.Text.Trim(), maxPrice) Then
                    Dim sellingPrice As Decimal = Convert.ToDecimal(product("SellingPrice"))
                    If sellingPrice > maxPrice Then Return False
                End If
            End If

            Return True

        Catch ex As Exception
            Return True ' Include item if filter check fails
        End Try
    End Function
    Private Sub ResetFilters_Click(sender As Object, e As EventArgs)
        ' Clear all filter inputs
        txtSearch.Text = ""
        txtFilterQuantity.Text = ""
        txtMinPrice.Text = ""
        txtMaxPrice.Text = ""

        ' Reset dropdowns to default
        If Guna2ComboBox1.Items.Count > 0 Then
            Guna2ComboBox1.SelectedIndex = 0 ' "All Categories"
        End If

        If StockCmbBox.Items.Count > 0 Then
            StockCmbBox.SelectedIndex = 0 ' "All"
        End If

        ' Apply filters (which will show all products)
        ApplyFilters(Nothing, Nothing)
    End Sub

    Private Sub Guna2HtmlLabel13_Click(sender As Object, e As EventArgs)
        isNavigating = True
        InventoryLog.Show()
        Close()
    End Sub

    Private Sub LoadProducts()
        ' This method is now just for refreshing after edits
        ' The main loading is handled by LoadProductsAsync
        Try
            LoadProductsFromDatabase()
            LoadCategoriesForFilter()

            ' Initially show all products
            filteredProducts = New List(Of Dictionary(Of String, Object))(allProducts)

            ' Reset to the first page on refresh
            _currentPage = 1

            ' Set up virtual scrolling and render
            RefreshProductDisplay()

        Catch ex As Exception
            MessageBox.Show("Error loading products: " & ex.Message & vbCrLf & vbCrLf &
                          "Stack Trace: " & ex.StackTrace, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' FormClosing event handler with exit confirmation
    Private Sub Inventory_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        ' Stop idle timeout monitoring
        IdleTimeoutManager.Instance.StopMonitoring(Me)

        ' Hide loading overlay if it's still visible
        HideLoadingOverlay()

        ' Dispose of custom tooltip system with null checks
        If tooltipTimer IsNot Nothing Then
            tooltipTimer.Stop()
            tooltipTimer.Dispose()
            tooltipTimer = Nothing
        End If

        If customTooltip IsNot Nothing Then
            customTooltip.Dispose()
            customTooltip = Nothing
        End If

        If _cachedLogo IsNot Nothing Then
            _cachedLogo.Dispose()
            _cachedLogo = Nothing
        End If

        currentTooltipCell = Nothing

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

    Private Sub CreateNavigationMenu()
        NavigationBuilder.Build(DashboardPanel, Me, "Inventory")
    End Sub
    Private Sub NavSystemSettings_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Sys.Show()
        Me.Close()
    End Sub

    Private Function CreateLargeNavButton(text As String, yPosition As Integer, isActive As Boolean, buttonWidth As Integer, buttonHeight As Integer) As Guna.UI2.WinForms.Guna2Button
        Dim btn As New Guna.UI2.WinForms.Guna2Button()

        btn.Text = text
        btn.Size = New Size(buttonWidth, buttonHeight)
        btn.Location = New Point(20, yPosition)
        btn.BorderRadius = 12
        btn.Font = New Font("Poppins", 10, FontStyle.Regular)
        btn.TextAlign = HorizontalAlignment.Left

        ' Jade Clinic palette for nav buttons
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
    ' Navigation event handlers
    Private Sub NavDashboard_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Dashboard.Show()
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

    Private Sub NavSuppliers_Click(sender As Object, e As EventArgs)
        Try
            isNavigating = True
            Supplier.Show()
            Me.Close()
        Catch ex As Exception
            MessageBox.Show($"Unable to open Suppliers: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    Private Sub NavInventoryLog_Click(sender As Object, e As EventArgs)
        isNavigating = True
        InventoryLog.Show()
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

    Private Sub InitializeProfileSection()
        ProfileManager.InitializeProfile(Me, lblUsername, Guna2CirclePictureBox5, AddressOf NavigateToProfileSettings)
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

    Private Sub StockPanel_Scroll(sender As Object, e As ScrollEventArgs)
        ' This method is no longer used since we're using DataGridView
        ' Keeping for compatibility but can be removed in the future
    End Sub

    Private Sub RenderVisibleItems()
        ' This method is no longer used since DataGridView handles virtual scrolling
        ' Keeping for compatibility but can be removed in the future
    End Sub

    Private Sub CreateProductPanel(productData As Dictionary(Of String, Object), index As Integer)
        ' This method has been replaced by DataGridView implementation
        ' Keeping for compatibility but can be removed in the future
    End Sub

    Private Sub EditProduct_Click(sender As Object, e As EventArgs)
        ' This method has been replaced by EditProduct_Click_FromGrid
        ' Keeping for compatibility but can be removed in the future
    End Sub

    Private Sub ProductDataGrid_CellMouseEnter(sender As Object, e As DataGridViewCellEventArgs)
        Try
            If e.RowIndex >= 0 AndAlso e.ColumnIndex >= 0 Then
                Dim productDataGrid As Guna.UI2.WinForms.Guna2DataGridView = CType(sender, Guna.UI2.WinForms.Guna2DataGridView)

                ' Only handle ProductName column for tooltips
                If productDataGrid.Columns(e.ColumnIndex).Name = "ProductName" Then
                    ' Store the cell for tooltip display
                    currentTooltipCell = productDataGrid.Rows(e.RowIndex).Cells(e.ColumnIndex)
                    lastMousePosition = productDataGrid.PointToClient(Cursor.Position)

                    ' Start tooltip timer
                    tooltipTimer.Stop()
                    tooltipTimer.Start()
                End If

                ' Handle cursor for Actions column
                If productDataGrid.Columns(e.ColumnIndex).Name = "Actions" Then
                    productDataGrid.Cursor = Cursors.Hand
                End If
            End If
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    Private Sub ProductDataGrid_CellMouseLeave(sender As Object, e As DataGridViewCellEventArgs)
        Try
            ' ADDED NULL CHECK
            If tooltipTimer Is Nothing Then Return

            ' Stop tooltip timer and hide tooltip with null checks
            tooltipTimer.Stop()
            currentTooltipCell = Nothing

            ' Hide tooltip with null check
            If customTooltip IsNot Nothing Then
                customTooltip.Hide(Guna2DataGridView1)
            End If

            ' Reset cursor
            Guna2DataGridView1.Cursor = Cursors.Default

        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub
    Private Sub UpdateItemCountLabel(count As Integer)
        Try
            ' Prefer an explicitly named label if present (designer-created)
            Dim ctrl As Control = Me.Controls.Find("lblItemCount", True).FirstOrDefault()
            If ctrl IsNot Nothing Then
                ctrl.Text = $"{count} Items"
                Return
            End If

            ' Try alternate common names
            ctrl = Me.Controls.Find("lblItemsCount", True).FirstOrDefault()
            If ctrl IsNot Nothing Then
                ctrl.Text = $"{count} Items"
                Return
            End If

            ' Fallback: do NOT overwrite lblUsername (it must always show the logged-in user).
            ' Create a small runtime label named lblItemCount and anchor it top-right so it's visible.

        Catch
            ' Silent fail - do not interfere with username label
        End Try
    End Sub

    Private Sub ProductDataGrid_MouseMove(sender As Object, e As MouseEventArgs)
        Try
            ' Update last mouse position for tooltip accuracy
            lastMousePosition = e.Location

            ' Get cell at current mouse position
            Dim hit As DataGridView.HitTestInfo = Guna2DataGridView1.HitTest(e.X, e.Y)

            If hit.RowIndex >= 0 AndAlso hit.ColumnIndex >= 0 Then
                ' Update cursor based on column
                If Guna2DataGridView1.Columns(hit.ColumnIndex).Name = "Actions" Then
                    Guna2DataGridView1.Cursor = Cursors.Hand
                Else
                    Guna2DataGridView1.Cursor = Cursors.Default
                End If

                ' If mouse moved significantly, reset tooltip
                If currentTooltipCell IsNot Nothing AndAlso
                   (hit.RowIndex <> currentTooltipCell.RowIndex OrElse hit.ColumnIndex <> currentTooltipCell.ColumnIndex) Then
                    tooltipTimer.Stop()
                    customTooltip.Hide(Guna2DataGridView1)
                    currentTooltipCell = Nothing
                End If
            End If

        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    Private Sub ProductDataGrid_Enter(sender As Object, e As EventArgs)
        Try
            ' DataGridView gained focus - this helps with Alt+Tab scenarios
            ' Reset tooltip state to prevent lingering tooltips with null checks
            If tooltipTimer IsNot Nothing Then
                tooltipTimer.Stop()
            End If
            currentTooltipCell = Nothing
            If customTooltip IsNot Nothing Then
                customTooltip.Hide(Guna2DataGridView1)
            End If
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub
    Private Sub NavAuditLog_Click(sender As Object, e As EventArgs)
        isNavigating = True
        AuditLog.Show()
        Me.Close()
    End Sub
    Private Sub ProductDataGrid_Leave(sender As Object, e As EventArgs)
        Try
            ' DataGridView lost focus - hide tooltips with null checks
            If tooltipTimer IsNot Nothing Then
                tooltipTimer.Stop()
            End If
            currentTooltipCell = Nothing
            If customTooltip IsNot Nothing Then
                customTooltip.Hide(Guna2DataGridView1)
            End If
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    Private Sub NavigateToProfileSettings()
        ' Navigate to ProfileSettings form (preserve audit and dropdown state).
        Try
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Navigation", "Navigated from Inventory to ProfileSettings")
            End If

            ' Prevent the form-closing confirmation and hide the dropdown first
            isNavigating = True
            ProfileManager.HideProfileDropdown(Me)

            ' Open ProfileSettings and close Inventory
            Dim profileForm As New ProfileSettings()
            profileForm.StartPosition = FormStartPosition.CenterScreen
            profileForm.Show()

            Me.Close()
        Catch ex As Exception
            ' Restore navigating flag on failure and show error
            isNavigating = False
            MessageBox.Show($"Unable to open Profile Settings: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    ' Add this field near the other private fields at the top of the class
    Private currentFilterDescription As String = "All Products"

    ' Replace the existing ApplyFilters method with this version (updates currentFilterDescription)
    ' Helper: update visual state of the three status buttons
    ' Replace existing SetStatusButtonsVisualState with this resilient implementation.
    Private Sub SetStatusButtonsVisualState()
        Try
            Dim btnAllCtrl = TryCast(FindControlRecursive(Me, "btnAll"), Guna.UI2.WinForms.Guna2Button)
            Dim btnActiveCtrl = TryCast(FindControlRecursive(Me, "btnActive"), Guna.UI2.WinForms.Guna2Button)
            Dim btnInactiveCtrl = TryCast(FindControlRecursive(Me, "btnInactive"), Guna.UI2.WinForms.Guna2Button)

            ' default visuals (opaque white so the 10px border radius renders in every state;
            ' transparent fill exposes the square BackColor at the corners)
            Dim defaultFill As Color = Color.White
            Dim defaultFore As Color = Color.FromArgb(51, 51, 51)

            ' Clear all to default first
            If btnAllCtrl IsNot Nothing Then
                btnAllCtrl.FillColor = defaultFill
                btnAllCtrl.ForeColor = defaultFore
            End If
            If btnActiveCtrl IsNot Nothing Then
                btnActiveCtrl.FillColor = defaultFill
                btnActiveCtrl.ForeColor = defaultFore
            End If
            If btnInactiveCtrl IsNot Nothing Then
                btnInactiveCtrl.FillColor = defaultFill
                btnInactiveCtrl.ForeColor = defaultFore
            End If

            ' Apply active visuals
            If btnAllCtrl IsNot Nothing AndAlso Not statusFilter.HasValue Then
                btnAllCtrl.FillColor = Color.FromArgb(254, 191, 16)
                btnAllCtrl.ForeColor = Color.FromArgb(51, 51, 51)
            End If

            If btnActiveCtrl IsNot Nothing AndAlso statusFilter.HasValue AndAlso statusFilter.Value Then
                btnActiveCtrl.FillColor = Color.FromArgb(20, 140, 50)
                btnActiveCtrl.ForeColor = Color.White
            End If

            If btnInactiveCtrl IsNot Nothing AndAlso statusFilter.HasValue AndAlso statusFilter.Value = False Then
                btnInactiveCtrl.FillColor = Color.FromArgb(200, 40, 50)
                btnInactiveCtrl.ForeColor = Color.White
            End If

            ' Subtle border for selected state
            If btnAllCtrl IsNot Nothing Then btnAllCtrl.BorderColor = If(Not statusFilter.HasValue, Color.FromArgb(230, 230, 230), Color.FromArgb(200, 200, 200))
            If btnActiveCtrl IsNot Nothing Then btnActiveCtrl.BorderColor = If(statusFilter.HasValue AndAlso statusFilter.Value, Color.FromArgb(200, 200, 200), Color.FromArgb(230, 230, 230))
            If btnInactiveCtrl IsNot Nothing Then btnInactiveCtrl.BorderColor = If(statusFilter.HasValue AndAlso statusFilter.Value = False, Color.FromArgb(200, 200, 200), Color.FromArgb(230, 230, 230))

        Catch ex As Exception
            ' Non-fatal: ignore visual update errors
        End Try
    End Sub
    Private Sub ApplyFilters(sender As Object, e As EventArgs)
        Try
            filteredProducts.Clear()

            For Each product In allProducts
                If MatchesFilter(product) Then
                    filteredProducts.Add(product)
                End If
            Next

            ' Build a human-readable filter description to pass to the exporter
            Dim parts As New List(Of String)()

            Dim searchText As String = txtSearch.Text.Trim()
            If Not String.IsNullOrWhiteSpace(searchText) Then
                parts.Add($"Search: '{searchText}'")
            End If

            If Guna2ComboBox1.SelectedItem IsNot Nothing AndAlso Guna2ComboBox1.SelectedItem.ToString() <> "All Categories" Then
                parts.Add($"Category: {Guna2ComboBox1.SelectedItem}")
            End If

            If StockCmbBox.SelectedItem IsNot Nothing AndAlso StockCmbBox.SelectedItem.ToString() <> "All" Then
                parts.Add($"Stock: {StockCmbBox.SelectedItem}")
            End If

            If statusFilter.HasValue Then
                parts.Add($"Status: {(If(statusFilter.Value, "Active", "Inactive"))}")
            End If

            If Not String.IsNullOrWhiteSpace(txtFilterQuantity.Text) Then
                parts.Add($"Min Qty: {txtFilterQuantity.Text.Trim()}")
            End If

            Dim minPriceText As String = If(String.IsNullOrWhiteSpace(txtMinPrice.Text), "", txtMinPrice.Text.Trim())
            Dim maxPriceText As String = If(String.IsNullOrWhiteSpace(txtMaxPrice.Text), "", txtMaxPrice.Text.Trim())
            If minPriceText <> "" AndAlso maxPriceText <> "" Then
                parts.Add($"Price: {minPriceText} - {maxPriceText}")
            ElseIf minPriceText <> "" Then
                parts.Add($"Min Price: {minPriceText}")
            ElseIf maxPriceText <> "" Then
                parts.Add($"Max Price: {maxPriceText}")
            End If

            If parts.Count = 0 Then
                currentFilterDescription = "All Products"
            Else
                currentFilterDescription = String.Join(" | ", parts)
            End If

            ' Refresh display (start from page 1 on any filter change)
            _currentPage = 1
            RefreshProductDisplay()

        Catch ex As Exception
            ' Silent fail for filter errors
        End Try
    End Sub
    ' Add helper to check role permissions (used by export)
    Private Function IsStaffUser() As Boolean
        Try
            Dim role As String = If(frmLoginvb.LoggedInRole, "").ToString().ToUpperInvariant()
            Return role = "STAFF"
        Catch
            ' If role cannot be determined, deny access to be safe
            Return True
        End Try
    End Function

    ' Implement Export button to respect permissions and pass current filters
    Private Sub Exportbtn_Click(sender As Object, e As EventArgs) Handles Exportbtn.Click
        Try
            If IsStaffUser() Then
                MessageBox.Show("You do not have permission to use this function.", "Access Restricted", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            If filteredProducts IsNot Nothing AndAlso filteredProducts.Count > 0 Then
                InventoryExporter.ExportInventoryReport(filteredProducts, currentFilterDescription)
            Else
                MessageBox.Show("No products to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        Catch ex As Exception
            MessageBox.Show($"Export failed: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Escape Then
            If Me.OwnedForms.Cast(Of Form)().Any(Function(f) f.Visible) Then
                Return MyBase.ProcessCmdKey(msg, keyData)
            End If

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
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Application Exit", "User exited the application via Inventory.")
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

End Class
