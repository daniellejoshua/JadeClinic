Imports Microsoft.Data.SqlClient
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

    ' Navigation flag to prevent exit confirmation on programmatic close
    Private isNavigating As Boolean = False

    ' Profile dropdown panel
    Private profileDropdownPanel As Panel = Nothing
    Private isProfileDropdownVisible As Boolean = False

    ' Custom tooltip implementation for better DataGridView support
    Private customTooltip As ToolTip
    Private tooltipTimer As Timer
    Private currentTooltipCell As DataGridViewCell = Nothing
    Private lastMousePosition As Point = Point.Empty
    ' Add this field near the other private fields at the top of the class
    Private statusFilter As Nullable(Of Boolean) = Nothing ' Nothing = All, True = Active, False = Inactive

    Private Async Sub Inventory_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Enable double buffering for smooth scrolling
        SetDoubleBuffered(Guna2DataGridView1)
        ' ... inside Inventory_Load, after CreateNavigationMenu() and InitializeProfileSection()
        ' Apply the new visual palette (non-destructive — overrides colors at runtime)
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
            .ForeColor = Color.White,
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
            Me.Invoke(Sub()
                          ' Initially show all products
                          filteredProducts = New List(Of Dictionary(Of String, Object))(allProducts)

                          ' Update item count (do not overwrite lblUsername)
                          UpdateItemCountLabel(filteredProducts.Count)

                          ' Set up virtual scrolling and render
                          RefreshProductDisplay()

                          ' Hide loading overlay
                          HideLoadingOverlay()
                      End Sub)
        Catch ex As Exception
            ' Handle errors on main thread
            Me.Invoke(Sub()
                          HideLoadingOverlay()
                          MessageBox.Show("Error loading products: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                      End Sub)
        End Try
    End Function
    Private Sub LoadProductsFromDatabase()
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqlConnection(connStr)
                conn.Open()

                Dim query As String = "SELECT p.ProductID, p.ProductCode, p.ProductName, p.Category, " &
                      "p.Unit, p.CurrentStock, p.ReorderLevel, p.CostPrice, p.SellingPrice, p.IsActive, " &
                      "pi.ImageData AS ProductImage " &
                      "FROM Products p " &
                      "LEFT JOIN ProductImageMapping pim ON p.ProductID = pim.ProductID " &
                      "LEFT JOIN ProductImages pi ON pim.ImageID = pi.ImageID " &
                      "ORDER BY p.ProductName"

                Using cmd As New SqlCommand(query, conn)
                    Using reader As SqlDataReader = cmd.ExecuteReader()
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
        AddHandler txtFilterPrice.TextChanged, AddressOf ApplyFilters
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

        ' Set placeholders
        txtSearch.PlaceholderText = "Search by name, code, or category..."
        txtFilterQuantity.PlaceholderText = "Minimum quantity (e.g., 10)"
        txtFilterPrice.PlaceholderText = "Minimum price (e.g., 100.00)"

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
            Using conn As New SqlConnection(connStr)
                conn.Open()

                ' Get distinct categories from existing products
                Dim query As String = "SELECT DISTINCT Category FROM Products WHERE Category IS NOT NULL AND IsActive = 1 ORDER BY Category"
                Using cmd As New SqlCommand(query, conn)
                    Using reader As SqlDataReader = cmd.ExecuteReader()
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

            ' --- DARK / BLACK STYLE PALETTE ---
            productDataGrid.BackgroundColor = System.Drawing.Color.FromArgb(41, 44, 45)
            productDataGrid.GridColor = System.Drawing.Color.White
            productDataGrid.BorderStyle = BorderStyle.None
            productDataGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal

            productDataGrid.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
            productDataGrid.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
            productDataGrid.DefaultCellStyle.ForeColor = System.Drawing.Color.LightGray
            productDataGrid.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(255, 204, 77)
            productDataGrid.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black
            productDataGrid.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
            productDataGrid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            productDataGrid.DefaultCellStyle.Padding = New Padding(10, 6, 10, 6)

            ' Header styling
            productDataGrid.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(30, 30, 30)
            productDataGrid.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.LightGray
            productDataGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(30, 30, 30)
            productDataGrid.ColumnHeadersDefaultCellStyle.Font = New Font("Poppins SemiBold", 10.5F, FontStyle.Bold)
            productDataGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            productDataGrid.ColumnHeadersHeight = 55
            productDataGrid.RowTemplate.Height = 75
            productDataGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing

            ' Clear existing columns
            productDataGrid.Columns.Clear()

            ' Colour accents
            Dim AccentGold As Color = System.Drawing.Color.FromArgb(255, 204, 77)
            Dim AccentGoldDark As Color = System.Drawing.Color.FromArgb(200, 140, 0)
            Dim PriceGold As Color = System.Drawing.Color.FromArgb(200, 140, 0)

            ' Product Information column (slightly shorter to make room for Status)
            ' In SetupProductDataGrid(), replace ProductName column config with this:

            Dim colProductName As New DataGridViewTextBoxColumn()
            colProductName.Name = "ProductName"
            colProductName.HeaderText = "Product Information"

            ' FIXED WIDTH
            colProductName.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            colProductName.Width = 280
            colProductName.MinimumWidth = 280

            colProductName.DefaultCellStyle.Font = New Font("Poppins SemiBold", 9.5F, FontStyle.Regular)
            colProductName.DefaultCellStyle.ForeColor = AccentGoldDark
            colProductName.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
            colProductName.DefaultCellStyle.Padding = New Padding(5, 5, 5, 5)
            colProductName.DefaultCellStyle.SelectionBackColor = AccentGold
            colProductName.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black
            productDataGrid.Columns.Add(colProductName)
            ' Category column (slightly narrowed)
            Dim colCategory As New DataGridViewTextBoxColumn()
            colCategory.Name = "Category"
            colCategory.HeaderText = "Category"
            colCategory.FillWeight = 16 ' reduced from 18
            colCategory.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            colCategory.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
            colCategory.DefaultCellStyle.ForeColor = System.Drawing.Color.LightGray
            colCategory.DefaultCellStyle.SelectionBackColor = AccentGold
            colCategory.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black
            productDataGrid.Columns.Add(colCategory)

            ' Unit column (slightly narrowed)
            Dim colUnit As New DataGridViewTextBoxColumn()
            colUnit.Name = "Unit"
            colUnit.HeaderText = "Unit"
            colUnit.FillWeight = 8 ' reduced from 10
            colUnit.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            colUnit.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
            colUnit.DefaultCellStyle.ForeColor = System.Drawing.Color.LightGray
            colUnit.DefaultCellStyle.SelectionBackColor = AccentGold
            colUnit.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black
            productDataGrid.Columns.Add(colUnit)

            ' Stock column (slightly narrowed)
            Dim colStock As New DataGridViewTextBoxColumn()
            colStock.Name = "CurrentStock"
            colStock.HeaderText = "Stock"
            colStock.FillWeight = 9 ' reduced from 10
            colStock.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            colStock.DefaultCellStyle.Font = New Font("PoppinsSemiBold", 9.5F, FontStyle.Bold)
            colStock.DefaultCellStyle.ForeColor = System.Drawing.Color.LightGray
            colStock.DefaultCellStyle.SelectionBackColor = AccentGold
            colStock.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black
            productDataGrid.Columns.Add(colStock)

            ' Cost Price column
            Dim colCostPrice As New DataGridViewTextBoxColumn()
            colCostPrice.Name = "CostPrice"
            colCostPrice.HeaderText = "Cost"
            colCostPrice.FillWeight = 12
            colCostPrice.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            colCostPrice.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
            colCostPrice.DefaultCellStyle.ForeColor = System.Drawing.Color.LightGray
            colCostPrice.DefaultCellStyle.SelectionBackColor = AccentGold
            colCostPrice.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black
            productDataGrid.Columns.Add(colCostPrice)

            ' Selling Price column
            Dim colSellingPrice As New DataGridViewTextBoxColumn()
            colSellingPrice.Name = "SellingPrice"
            colSellingPrice.HeaderText = "Price"
            colSellingPrice.FillWeight = 14 ' slightly reduced to balance layout
            colSellingPrice.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            colSellingPrice.DefaultCellStyle.Font = New Font("PoppinsSemiBold", 9.5F, FontStyle.Bold)
            colSellingPrice.DefaultCellStyle.ForeColor = PriceGold
            colSellingPrice.DefaultCellStyle.SelectionBackColor = AccentGold
            colSellingPrice.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black
            productDataGrid.Columns.Add(colSellingPrice)

            ' Status column (Active / Inactive) - fixed width for consistent layout
            Dim colStatus As New DataGridViewTextBoxColumn()
            colStatus.Name = "Status"
            colStatus.HeaderText = "Status"

            ' Make the column fixed-width (not participating in Fill autosizing)
            colStatus.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            colStatus.Width = 100              ' adjust to taste
            colStatus.MinimumWidth = 70
            colStatus.MaxInputLength = 50
            colStatus.ReadOnly = True

            colStatus.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            colStatus.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
            colStatus.DefaultCellStyle.ForeColor = System.Drawing.Color.LightGray
            colStatus.DefaultCellStyle.SelectionBackColor = AccentGold
            colStatus.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black

            productDataGrid.Columns.Add(colStatus)
            ' Actions column
            ' Actions column — fixed width, non-resizable, shows edit icon/text and not participating in Fill autosizing
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
            colActions.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
            colActions.DefaultCellStyle.ForeColor = AccentGoldDark
            colActions.DefaultCellStyle.SelectionBackColor = AccentGold
            colActions.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black
            colActions.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Regular)

            ' Show a default edit marker when cell value is null/empty
            colActions.DefaultCellStyle.NullValue = "✏️"
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
            If filteredProducts Is Nothing OrElse filteredProducts.Count = 0 Then
                DataGridViewHelper.ShowNoRecordsMessage(productDataGrid, "No Products Found")
                Return
            End If

            ' Define lighter palette colors
            Dim LightRed As Color = Color.FromArgb(255, 153, 153)       ' light red for out of stock / inactive
            Dim LightOrange As Color = Color.FromArgb(255, 179, 102)    ' light orange for below reorder (B.R.L)
            Dim LightGreen As Color = Color.FromArgb(144, 238, 144)     ' light green for above reorder (A.R.L)
            Dim AccentGoldDark As Color = Color.FromArgb(200, 140, 0)

            ' Load filtered products into DataGridView
            For Each productData As Dictionary(Of String, Object) In filteredProducts
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
                "✏️"  ' Actions - simple text
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
            .Opacity = 0.55,  ' ✅ Semi-transparent - lets background show through!
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

                        ' Define brand colors with golden accent
                        Dim AccentGold As Color = Color.FromArgb(255, 204, 77)
                        Dim AccentGoldDark As Color = Color.FromArgb(200, 140, 0)
                        Dim MediumGray As Color = Color.FromArgb(120, 120, 120)
                        Dim LightGray As Color = Color.FromArgb(200, 200, 200)
                        Dim HeaderTextSelected As Color = Color.FromArgb(26, 29, 31) ' darker text on gold selection for better contrast

                        ' Calculate layout dimensions with better spacing
                        Dim imageSize As Integer = 50
                        Dim padding As Integer = 10
                        Dim imageRect As New Rectangle(
                        e.CellBounds.Left + padding,
                        e.CellBounds.Top + ((e.CellBounds.Height - imageSize) \ 2),
                        imageSize, imageSize)

                        ' Draw product image with improved placeholder styling
                        Try
                            If productData("ProductImage") IsNot Nothing Then
                                Dim imgBytes As Byte() = CType(productData("ProductImage"), Byte())
                                Using ms As New MemoryStream(imgBytes)
                                    Using img As Image = Image.FromStream(ms)
                                        ' Draw image with simple rectangle
                                        e.Graphics.DrawImage(img, imageRect)

                                        ' Draw subtle border around image
                                        Using borderPen As New Pen(LightGray, 1)
                                            e.Graphics.DrawRectangle(borderPen, imageRect)
                                        End Using
                                    End Using
                                End Using
                            Else
                                ' Draw professional placeholder with gray theme
                                Using bgBrush As New SolidBrush(Color.FromArgb(245, 245, 245))
                                    e.Graphics.FillRectangle(bgBrush, imageRect)
                                End Using

                                Using borderPen As New Pen(LightGray, 1)
                                    e.Graphics.DrawRectangle(borderPen, imageRect)
                                End Using

                                ' Draw camera icon placeholder
                                Using placeholderFont As New Font("Segoe UI", 8, FontStyle.Regular)
                                    Using placeholderBrush As New SolidBrush(MediumGray)
                                        Dim placeholderFormat As New StringFormat() With {
                                        .Alignment = StringAlignment.Center,
                                        .LineAlignment = StringAlignment.Center
                                    }
                                        e.Graphics.DrawString("📷", placeholderFont, placeholderBrush, imageRect, placeholderFormat)
                                    End Using
                                End Using
                            End If
                        Catch
                            ' Fallback placeholder on image error with gray theme
                            Using bgBrush As New SolidBrush(Color.FromArgb(245, 245, 245))
                                e.Graphics.FillRectangle(bgBrush, imageRect)
                            End Using
                            Using borderPen As New Pen(AccentGoldDark, 1)
                                e.Graphics.DrawRectangle(borderPen, imageRect)
                            End Using

                            ' Add "No Image" text
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

                        ' Choose main text color based on selection (dark golden idle, dark text on selection)
                        Dim productNameColor As Color = If(isRowSelected, HeaderTextSelected, AccentGoldDark)
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
                        Dim codeTextColor As Color = If(isRowSelected, HeaderTextSelected, MediumGray)

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

                    ' If row is selected, custom paint darker colored text for contrast on gold selection background
                    If isRowSelected Then
                        e.Handled = True
                        e.PaintBackground(e.ClipBounds, True)

                        ' Determine darker text color based on stock status for readability on gold
                        Dim textColor As Color
                        Select Case stockStatus
                            Case "OUT_OF_STOCK"
                                textColor = Color.FromArgb(150, 30, 40) ' darker red
                            Case "LOW_STOCK"
                                textColor = Color.FromArgb(150, 100, 0) ' darker amber
                            Case Else ' "IN_STOCK"
                                textColor = Color.FromArgb(10, 90, 30) ' darker green
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
            ' Search filter
            Dim searchText As String = txtSearch.Text.Trim().ToLower()
            If Not String.IsNullOrWhiteSpace(searchText) Then
                Dim productName As String = product("ProductName").ToString().ToLower()
                Dim productCode As String = product("ProductCode").ToString().ToLower()
                Dim category As String = product("Category").ToString().ToLower()

                If Not (productName.Contains(searchText) Or productCode.Contains(searchText) Or
           category.Contains(searchText)) Then
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

            ' Price filter
            If Not String.IsNullOrWhiteSpace(txtFilterPrice.Text) Then
                Dim filterPrice As Decimal
                If Decimal.TryParse(txtFilterPrice.Text.Trim(), filterPrice) Then
                    Dim sellingPrice As Decimal = Convert.ToDecimal(product("SellingPrice"))
                    If sellingPrice < filterPrice Then Return False
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
        txtFilterPrice.Text = ""

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

            ' Initially show all products
            filteredProducts = New List(Of Dictionary(Of String, Object))(allProducts)

            ' And inside LoadProducts (refresh path) replace lblUsername update:
            ' Update item count (do not overwrite lblUsername)
            UpdateItemCountLabel(filteredProducts.Count)

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

        ' Consistent palette used across forms
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
        Try
            ' Set username without emoji
            lblUsername.Text = frmLoginvb.LoggedInUsername
            lblUsername.Font = New Font("Poppins", 10.0F, FontStyle.Regular)
            lblUsername.ForeColor = System.Drawing.Color.White

            ' Load user profile picture
            LoadUserProfilePicture()

            ' Add click event to profile picture and username
            AddHandler Guna2CirclePictureBox5.Click, AddressOf ProfilePicture_Click
            AddHandler lblUsername.Click, AddressOf ProfilePicture_Click

            ' Add hover effects
            AddHandler Guna2CirclePictureBox5.MouseEnter, Sub()
                                                              Guna2CirclePictureBox5.Cursor = Cursors.Hand
                                                          End Sub
            AddHandler lblUsername.MouseEnter, Sub()
                                                   lblUsername.Cursor = Cursors.Hand
                                               End Sub

        Catch ex As Exception
            ' Fallback if there's an error
            lblUsername.Text = frmLoginvb.LoggedInUsername
            Guna2CirclePictureBox5.Image = CreateDefaultProfileAvatar(frmLoginvb.LoggedInUsername)
        End Try
    End Sub

    Private Sub LoadUserProfilePicture()
        Try
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                ' Query to get the logged-in user's photo
                Dim query As String = "SELECT Photo FROM Users WHERE Username = @Username"
                Dim parameters As SqlParameter() = {
                    New SqlParameter("@Username", frmLoginvb.LoggedInUsername)
                }

                Using reader As SqlDataReader = Utilities.ExecuteReader(query, parameters)
                    If reader.Read() Then
                        ' Configure the PictureBox for circular profile picture
                        Guna2CirclePictureBox5.SizeMode = PictureBoxSizeMode.Zoom
                        Guna2CirclePictureBox5.BorderStyle = BorderStyle.None

                        If Not IsDBNull(reader("Photo")) Then
                            ' Load user's actual photo
                            Dim photoBytes As Byte() = CType(reader("Photo"), Byte())
                            Using ms As New IO.MemoryStream(photoBytes)
                                Dim loadedImage As Image = Image.FromStream(ms)
                                Guna2CirclePictureBox5.Image = New Bitmap(loadedImage)
                                loadedImage.Dispose()
                            End Using
                        Else
                            ' Create and display default avatar
                            Guna2CirclePictureBox5.Image = CreateDefaultProfileAvatar(frmLoginvb.LoggedInUsername)
                        End If
                    End If
                End Using
            End If
        Catch ex As Exception
            ' If there's an error, show default avatar
            Guna2CirclePictureBox5.Image = CreateDefaultProfileAvatar(If(frmLoginvb.LoggedInUsername, "User"))
        End Try
    End Sub

    ' Create default profile avatar method
    Private Function CreateDefaultProfileAvatar(username As String) As System.Drawing.Image
        Dim bitmap As New Bitmap(50, 50)
        Using g As Graphics = Graphics.FromImage(bitmap)
            ' Enable anti-aliasing for smooth circles
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

            ' Fill background with a color based on username
            Dim colors() As System.Drawing.Color = {
                System.Drawing.Color.FromArgb(255, 107, 107),
                System.Drawing.Color.FromArgb(78, 205, 196),
                System.Drawing.Color.FromArgb(85, 98, 112),
                System.Drawing.Color.FromArgb(129, 236, 236),
                System.Drawing.Color.FromArgb(116, 185, 255)
            }
            Dim colorIndex As Integer = Math.Abs(username.GetHashCode()) Mod colors.Length
            g.FillEllipse(New SolidBrush(colors(colorIndex)), 0, 0, 50, 50)

            ' Draw initials
            Dim initials As String = ""
            If username.Length > 0 Then
                initials = username.Substring(0, 1).ToUpper()
                If username.Length > 1 Then
                    For i As Integer = 1 To username.Length - 1
                        If Char.IsUpper(username(i)) OrElse username(i) = " "c Then
                            If username(i) <> " "c Then
                                initials += username(i).ToString().ToUpper()
                                Exit For
                            End If
                        End If
                    Next
                End If
            End If

            Using font As New System.Drawing.Font("Poppins", 14, System.Drawing.FontStyle.Bold)
                Dim textSize = g.MeasureString(initials, font)
                g.DrawString(initials, font, Brushes.White,
                    (50 - textSize.Width) / 2, (50 - textSize.Height) / 2)
            End Using
        End Using
        Return bitmap
    End Function

    Private Sub ProfilePicture_Click(sender As Object, e As EventArgs)
        ToggleProfileDropdown()
    End Sub

    Private Sub ToggleProfileDropdown()
        If isProfileDropdownVisible Then
            HideProfileDropdown()
        Else
            ShowProfileDropdown()
        End If
    End Sub

    Private Sub ShowProfileDropdown()
        If profileDropdownPanel IsNot Nothing Then
            HideProfileDropdown()
        End If

        ' Create dropdown panel
        profileDropdownPanel = New Panel()
        profileDropdownPanel.Size = New System.Drawing.Size(200, 100)
        profileDropdownPanel.BackColor = System.Drawing.Color.FromArgb(41, 44, 45)
        profileDropdownPanel.BorderStyle = BorderStyle.FixedSingle

        ' Position below the profile picture
        Dim profileLocation = Guna2CirclePictureBox5.Location
        profileDropdownPanel.Location = New Point(profileLocation.X - 90, profileLocation.Y + Guna2CirclePictureBox5.Height + 5)

        ' Create Profile Settings button
        Dim btnProfileSettings As New Label()
        btnProfileSettings.Text = "⚙️ Profile Settings"
        btnProfileSettings.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
        btnProfileSettings.ForeColor = System.Drawing.Color.White
        btnProfileSettings.BackColor = System.Drawing.Color.Transparent
        btnProfileSettings.Size = New System.Drawing.Size(190, 40)
        btnProfileSettings.Location = New Point(5, 5)
        btnProfileSettings.TextAlign = ContentAlignment.MiddleLeft
        btnProfileSettings.Cursor = Cursors.Hand

        ' Add hover effect to Profile Settings
        AddHandler btnProfileSettings.MouseEnter, Sub()
                                                      btnProfileSettings.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
                                                  End Sub
        AddHandler btnProfileSettings.MouseLeave, Sub()
                                                      btnProfileSettings.BackColor = System.Drawing.Color.Transparent
                                                  End Sub

        ' Add click event to Profile Settings
        AddHandler btnProfileSettings.Click, Sub()
                                                 HideProfileDropdown()
                                                 NavigateToProfileSettings()
                                             End Sub

        ' Create Log Out button
        Dim btnLogOut As New Label()
        btnLogOut.Text = "🚪 Log Out"
        btnLogOut.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
        btnLogOut.ForeColor = System.Drawing.Color.White
        btnLogOut.BackColor = System.Drawing.Color.Transparent
        btnLogOut.Size = New System.Drawing.Size(190, 40)
        btnLogOut.Location = New Point(5, 50)
        btnLogOut.TextAlign = ContentAlignment.MiddleLeft
        btnLogOut.Cursor = Cursors.Hand

        ' Add hover effect to Log Out
        AddHandler btnLogOut.MouseEnter, Sub()
                                             btnLogOut.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
                                         End Sub
        AddHandler btnLogOut.MouseLeave, Sub()
                                             btnLogOut.BackColor = System.Drawing.Color.Transparent
                                         End Sub

        ' Add click event to Log Out - JUST LOGOUT, DON'T EXIT APPLICATION
        AddHandler btnLogOut.Click, Sub()
                                        ' Confirm logout before proceeding
                                        Dim result As DialogResult = MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

                                        If result = DialogResult.Yes Then
                                            ' Log the logout action
                                            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                                                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Log Out", "User logged out of the application.")
                                            End If

                                            ' Clear user session and return to login (don't exit application)
                                            frmLoginvb.LogoutUser()

                                            ' Navigate to login form without closing the application
                                            isNavigating = True
                                            Me.Hide()
                                            Dim loginForm As New frmLoginvb()
                                            loginForm.Show()
                                        End If
                                    End Sub

        ' Add buttons to panel
        profileDropdownPanel.Controls.Add(btnProfileSettings)
        profileDropdownPanel.Controls.Add(btnLogOut)

        ' Add panel to form
        Me.Controls.Add(profileDropdownPanel)
        profileDropdownPanel.BringToFront()

        ' Add click event to form to hide dropdown when clicked elsewhere
        AddHandler Me.Click, AddressOf Form_Click

        isProfileDropdownVisible = True
    End Sub

    Private Sub HideProfileDropdown()
        If profileDropdownPanel IsNot Nothing Then
            Me.Controls.Remove(profileDropdownPanel)
            profileDropdownPanel.Dispose()
            profileDropdownPanel = Nothing
        End If
        isProfileDropdownVisible = False

        ' Remove form click event
        RemoveHandler Me.Click, AddressOf Form_Click
    End Sub

    Private Sub Form_Click(sender As Object, e As EventArgs)
        ' Hide dropdown when clicking elsewhere on the form
        HideProfileDropdown()
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
            HideProfileDropdown()

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

            ' default visuals
            Dim defaultFill As Color = Color.Transparent
            Dim defaultFore As Color = Color.White

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
                btnAllCtrl.FillColor = Color.White
                btnAllCtrl.ForeColor = Color.Black
            End If

            If btnActiveCtrl IsNot Nothing AndAlso statusFilter.HasValue AndAlso statusFilter.Value Then
                btnActiveCtrl.FillColor = Color.FromArgb(20, 140, 50)
                btnActiveCtrl.ForeColor = Color.White
            End If

            If btnInactiveCtrl IsNot Nothing AndAlso statusFilter.HasValue AndAlso statusFilter.Value = False Then
                btnInactiveCtrl.FillColor = Color.FromArgb(200, 40, 50)
                btnInactiveCtrl.ForeColor = Color.White
            End If

            ' Optional: give a subtle border when selected so contrast is clearer on dark background
            If btnAllCtrl IsNot Nothing Then btnAllCtrl.BorderColor = If(Not statusFilter.HasValue, Color.FromArgb(200, 200, 200), Color.FromArgb(80, 80, 80))
            If btnActiveCtrl IsNot Nothing Then btnActiveCtrl.BorderColor = If(statusFilter.HasValue AndAlso statusFilter.Value, Color.FromArgb(200, 200, 200), Color.FromArgb(80, 80, 80))
            If btnInactiveCtrl IsNot Nothing Then btnInactiveCtrl.BorderColor = If(statusFilter.HasValue AndAlso statusFilter.Value = False, Color.FromArgb(200, 200, 200), Color.FromArgb(80, 80, 80))

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

            If Not String.IsNullOrWhiteSpace(txtFilterPrice.Text) Then
                parts.Add($"Min Price: {txtFilterPrice.Text.Trim()}")
            End If

            If parts.Count = 0 Then
                currentFilterDescription = "All Products"
            Else
                currentFilterDescription = String.Join(" | ", parts)
            End If

            ' Refresh display
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

End Class