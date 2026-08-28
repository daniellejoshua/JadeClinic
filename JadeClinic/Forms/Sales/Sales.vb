Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Printing
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Windows.Forms
Imports Guna.UI2.WinForms
Imports Microsoft.Data.Sqlite
Imports System.Data.Common
Imports Newtonsoft.Json


Public Class Sales
    Implements IDraftPersistable

    Private Class CartStateSnapshot
        Public Property CurrentOrderList As List(Of Dictionary(Of String, Object))
        Public Property DiscountType As String
        Public Property DiscountValue As Decimal
        Public Property DiscountAmount As Decimal
        Public Property DiscountedItemProductId As Integer?
        Public Property DiscountedItemName As String
        Public Property SelectedCustomerId As Integer?
        Public Property SelectedCustomerName As String
        Public Property SelectedCustomerPhone As String
        Public Property SelectedCustomerEmail As String
        Public Property SelectedCustomerTIN As String
        Public Property SelectedCustomerType As String
        Public Property SelectedPaymentMethod As String
        Public Property PaymentReference As String
    End Class
    Private originalOrderPanelControls As List(Of Control)
    Private originalTotalPanelControls As List(Of Control)
    ' Add near the top of the Sales class with other private fields
    Private discountedItemProductId As Integer? = Nothing
    Private discountedItemName As String = ""
    ' Customer and payment flow variables
    Private isDiscountDialogOpen As Boolean = False
    Private pinPanelButtons As List(Of Guna.UI2.WinForms.Guna2Button) ' Repurposed for customer panel buttons
    Private totalPanelButtons As List(Of Guna.UI2.WinForms.Guna2Button)
    Private pinPanelActive As Boolean = False ' Repurposed for customer selection
    Private totalPanelActive As Boolean = False

    Private enteredAmount As String = ""
    Private lblAmountDisplay As Label

    Private productCardControls As New List(Of Control)()
    Private productDbStock As New Dictionary(Of String, Integer)()

    ' Pagination for product / category listings (reuses one control across views)
    Private Const ProductPageSize As Integer = 8
    Private _pagination As PaginationControl
    Private Enum PaginationContext
        None
        Category
        Search
    End Enum
    Private _paginationContext As PaginationContext = PaginationContext.None
    Private _paginationCategory As String = ""
    Private _paginationSearchTerm As String = ""

    ' Unit filter for product listings
    Private _selectedUnitFilter As String = ""
    Private _cmbUnitFilter As Guna2ComboBox
    Private _lblUnitFilter As Label

    ' Receipt printing variables
    Private printDocument As PrintDocument
    Private receiptOrderId As String
    Private receiptCustomerName As String = "Walk-in Customer"
    Private receiptTotalAmount As Decimal
    Private receiptAmountReceived As Decimal
    Private receiptChange As Decimal
    Private receiptItems As List(Of Dictionary(Of String, Object))
    Private receiptSubtotal As Decimal
    Private receiptTax As Decimal

    ' Discount variables
    Private discountType As String = "None" ' "Percentage", "Fixed", "None"
    Private discountValue As Decimal = 0
    Private discountAmount As Decimal = 0 ' Fixed variable name (was discountAmouAnt)

    ' Navigation flag to prevent exit confirmation on programmatic close
    Private isNavigating As Boolean = False

    ' Update: allow customer name to be nullable (no default "Walk-in Customer")
    Private selectedCustomerId As Integer? = Nothing
    Private selectedCustomerName As String = Nothing
    Private selectedCustomerPhone As String = ""
    Private selectedCustomerEmail As String = ""
    Private customerSelectionPanel As Guna.UI2.WinForms.Guna2Panel = Nothing
    Private selectedCustomerType As String = "Walk-in" ' kept for compatibility

    ' Add these variables at the top of the Sales class (around line 30):
    Private selectedPaymentMethod As String = "Cash" ' Cash, GCash, Card
    Private paymentReference As String = ""
    Private subtotalVatInclusive As Decimal = 0 ' Tracks VAT-inclusive subtotal for discount calculations

    ' Helper function to normalize category names
    Private Function NormalizeCategory(name As String) As String
        Return name.Replace("-", "").Replace(" ", "").ToLower()
    End Function


    Private isVoidDialogOpen As Boolean = False


    ' Map overlay label -> the category button it visually sits on
    Private overlayToButton As New Dictionary(Of Control, Guna.UI2.WinForms.Guna2Button)()
    ' Code-generated category tiles (replaces the designer tile controls)
    Private _categoryTileButtons As New List(Of Guna.UI2.WinForms.Guna2Button)()
    ' Shadow hosts that wrap each category tile (same soft shadow as product cards)
    Private _categoryTileHosts As New List(Of Panel)()
    Private _categoryCountLabels As New Dictionary(Of String, Guna.UI2.WinForms.Guna2HtmlLabel)()
    Private _searchTimer As Timer
    ' Add this new field near the other receipt fields (top of class)
    Private receiptVatableBeforeDiscount As Decimal = 0D
    Private WithEvents txtBarcodeInput As New TextBox With {.Visible = True, .TabIndex = 0}
    ' Fixed non-resizable DataGridView that renders the order summary line items
    Private _orderSummaryGrid As DataGridView
    ' Empty-cart state shown when the order has no items
    Private _emptyCartPanel As Guna.UI2.WinForms.Guna2Panel

    ' Jade Clinic Color Palette Constants (from brand guide)
    Private ReadOnly GoldenYellow As Color = Color.FromArgb(254, 191, 16)      ' #FECF10 - Primary brand color
    Private ReadOnly JadeOlive As Color = Color.FromArgb(190, 154, 48)         ' #BE9A30 - Secondary accent
    Private ReadOnly OffWhite As Color = Color.FromArgb(249, 249, 249)         ' #F9F9F9 - Main background
    Private ReadOnly PureWhite As Color = Color.FromArgb(255, 255, 255)        ' #FFFFFF - Card backgrounds
    Private ReadOnly LightGray As Color = Color.FromArgb(240, 240, 240)        ' #F0F0F0 - Subtle surfaces
    Private ReadOnly BorderGray As Color = Color.FromArgb(230, 230, 230)       ' #E6E6E6 - Borders
    Private ReadOnly DarkText As Color = Color.FromArgb(51, 51, 51)            ' #333333 - Primary text
    Private ReadOnly MediumText As Color = Color.FromArgb(102, 102, 102)       ' #666666 - Secondary text
    Private ReadOnly LightText As Color = Color.FromArgb(153, 153, 153)        ' #999999 - Tertiary text
    Private ReadOnly SuccessGreen As Color = Color.FromArgb(16, 216, 98)       ' #10D862 - Success states
    Private ReadOnly AlertRed As Color = Color.FromArgb(255, 71, 87)           ' #FF4757 - Error/Alert states
    ' Category tile design: base accent #BE9A30, hover bg #FBF7EC, hover border #EEBC1B, text #222222, subtext #666666
    Private ReadOnly CategoryHoverBg As Color = Color.FromArgb(251, 247, 236)  ' #FBF7EC - Category tile hover background
    Private ReadOnly CategoryText As Color = Color.FromArgb(34, 34, 34)        ' #222222 - Category tile text

    ' === Code-generated category tiles (no designer controls) ===
    Private ReadOnly _categoryTileSize As New Size(215, 285)                    ' Wide Ortho-style tall tile
    Private ReadOnly _categoryGridStart As New Point(41, 87)
    Private Const _categoryGridCols As Integer = 4
    Private Const _categoryGridGapX As Integer = 38
    Private Const _categoryGridGapY As Integer = 25
    Private ReadOnly _preferredCategoryOrder As New List(Of String) From {
        "ORTHO", "ENDO", "CONSUMABLES", "SURGERY", "RESTO", "COSMETIC"
    }
    Private ReadOnly _categoryIconGlyphs As New Dictionary(Of String, String) From {
        {"ORTHO", "🦷"},
        {"ENDO", "💉"},
        {"CONSUMABLES", "🧻"},
        {"SURGERY", "🩺"},
        {"RESTO", "🪥"},
        {"COSMETIC", "💄"}
    }
    Private selectedCustomerTIN As String = ""

    Private pinPanel As Guna.UI2.WinForms.Guna2Panel = Nothing ' Repurposed for customer selection
    Private totalReceivedPanel As Guna.UI2.WinForms.Guna2Panel = Nothing
    ' Add these variables at the top of the Sales class
    Private barcodeBuffer As String = ""
    Private lastKeyTime As DateTime = DateTime.Now
    Private Const BARCODE_TIMEOUT As Integer = 100 ' milliseconds between barcode characters


    ' POS lock color control caches
    Private originalCategoryButtonFillColors As New Dictionary(Of Guna.UI2.WinForms.Guna2Button, Color)()
    Private originalCategoryOverlayColors As New Dictionary(Of Control, Color)()
    Private originalCategoryOverlayParents As New Dictionary(Of Control, Control)()
    Private originalCategoryOverlayLocations As New Dictionary(Of Control, Point)()
    ' Original DisabledState colors so the disabled rendering matches the locked fill
    Private originalCategoryDisabledFillColors As New Dictionary(Of Guna.UI2.WinForms.Guna2Button, Color)()
    Private originalCategoryDisabledBorderColors As New Dictionary(Of Guna.UI2.WinForms.Guna2Button, Color)()
    Private posLockCategoryFillColor As Color = LightGray
    Private posLockLabelBackColor As Color = Color.Empty
    Private _lockedReplacementLabels As New Dictionary(Of Guna.UI2.WinForms.Guna2HtmlLabel, Label)()
    Private _originalLabelAutoSize As New Dictionary(Of Control, Boolean)()






    ' === Daily Opening Capital (POS lock) ===
    Private posLockedForCapital As Boolean = False
    Private currentOpeningCapital As Decimal = 0D
    Private lblCapitalInfo As Label
    Private btnEditCapital As Guna2Button

    ' FIXED: Enhanced barcode scanning with proper key handling

    Private Sub InitializeCategoryLockCaches()
        Try
            originalCategoryButtonFillColors.Clear()
            originalCategoryOverlayColors.Clear()
            overlayToButton.Clear()
            originalCategoryDisabledFillColors.Clear()
            originalCategoryDisabledBorderColors.Clear()

            ' Cache all category buttons currently in the panel
            For Each btn In _categoryTileButtons
                If btn Is Nothing OrElse btn.IsDisposed Then Continue For
                If Not originalCategoryButtonFillColors.ContainsKey(btn) Then
                    originalCategoryButtonFillColors(btn) = btn.FillColor
                End If
                If Not originalCategoryDisabledFillColors.ContainsKey(btn) Then
                    originalCategoryDisabledFillColors(btn) = btn.DisabledState.FillColor
                End If
                If Not originalCategoryDisabledBorderColors.ContainsKey(btn) Then
                    originalCategoryDisabledBorderColors(btn) = btn.DisabledState.BorderColor
                End If
            Next

            ' Cache all overlay labels (designer + runtime tiles) via a full tree walk of the panel
            Dim stack As New Stack(Of Control)()
            stack.Push(CategoryPanel)
            While stack.Count > 0
                Dim current = stack.Pop()
                For Each child As Control In current.Controls
                    If child Is Nothing OrElse child.IsDisposed Then Continue For
                    stack.Push(child)
                    If TypeOf child Is Label OrElse TypeOf child Is Guna.UI2.WinForms.Guna2HtmlLabel Then
                        If Not originalCategoryOverlayColors.ContainsKey(child) Then
                            originalCategoryOverlayColors(child) = child.BackColor
                            originalCategoryOverlayParents(child) = child.Parent
                            originalCategoryOverlayLocations(child) = child.Location
                        End If
                    End If
                Next
            End While

            ' Build visual mapping from overlay controls -> the button beneath them (by screen coordinates)
            For Each kvp In originalCategoryOverlayColors.ToList()
                Dim overlayCtrl As Control = kvp.Key
                Try
                    Dim screenRect As Rectangle = overlayCtrl.RectangleToScreen(overlayCtrl.ClientRectangle)
                    Dim centerPoint As Point = New Point(screenRect.Left + screenRect.Width \ 2, screenRect.Top + screenRect.Height \ 2)

                    For Each ctrl As Control In CategoryPanel.Controls
                        If TypeOf ctrl Is Guna.UI2.WinForms.Guna2Button Then
                            Dim btn = CType(ctrl, Guna.UI2.WinForms.Guna2Button)
                            Dim btnRect As Rectangle = btn.RectangleToScreen(btn.ClientRectangle)
                            If btnRect.Contains(centerPoint) Then
                                overlayToButton(overlayCtrl) = btn
                                Exit For
                            End If
                        End If
                    Next
                Catch
                    ' ignore mapping errors for any specific control
                End Try
            Next

        Catch ex As Exception
            Console.WriteLine($"InitializeCategoryLockCaches error: {ex.Message}")
        End Try
    End Sub
    ' Call this to set the colors you want applied when the POS is locked.
    Public Sub SetPosLockColors(categoryFill As Color, Optional labelBack As Color = Nothing)
        posLockCategoryFillColor = categoryFill
        If labelBack <> Nothing Then posLockLabelBackColor = labelBack
    End Sub

    Private Sub ApplyPosLockColors(locked As Boolean)
        Try
            If originalCategoryButtonFillColors.Count = 0 OrElse originalCategoryOverlayColors.Count = 0 Then
                InitializeCategoryLockCaches()
            End If

            ' Update button fill colors
            For Each kvp In originalCategoryButtonFillColors.ToList()
                Dim btn = kvp.Key
                Dim originalFill = kvp.Value
                If btn Is Nothing OrElse btn.IsDisposed Then Continue For
                btn.FillColor = If(locked, posLockCategoryFillColor, originalFill)
                ' CategoryPanel is disabled while locked, so Guna2 renders DisabledState instead of
                ' FillColor. Match the disabled rendering to the locked fill so the tile and its
                ' labels are the exact same gray.
                If originalCategoryDisabledFillColors.ContainsKey(btn) Then
                    btn.DisabledState.FillColor = If(locked, posLockCategoryFillColor, originalCategoryDisabledFillColors(btn))
                End If
                If originalCategoryDisabledBorderColors.ContainsKey(btn) Then
                    btn.DisabledState.BorderColor = If(locked, posLockCategoryFillColor, originalCategoryDisabledBorderColors(btn))
                End If
            Next

            ' Simple behavior: when locked set all Label and Guna2HtmlLabel BackColor to the same color
            ' as the button fill; when unlocking restore original BackColor
            If locked Then
                Dim targetColor As Color = posLockCategoryFillColor
                ' Walk all controls under CategoryPanel (including nested) and set labels' BackColor
                Dim stack As New Stack(Of Control)()
                stack.Push(CategoryPanel)
                While stack.Count > 0
                    Dim c = stack.Pop()
                    For Each child As Control In c.Controls
                        If child Is Nothing OrElse child.IsDisposed Then Continue For
                        stack.Push(child)
                        If TypeOf child Is Label OrElse TypeOf child Is Guna.UI2.WinForms.Guna2HtmlLabel Then
                            Try
                                child.BackColor = targetColor
                            Catch
                                ' ignore
                            End Try
                        End If
                    Next
                End While
            Else
                ' Restore original overlay BackColor values we captured earlier
                For Each kvp In originalCategoryOverlayColors.ToList()
                    Dim ctrl = kvp.Key
                    Dim originalBack = kvp.Value
                    If ctrl Is Nothing OrElse ctrl.IsDisposed Then Continue For
                    Try
                        ctrl.BackColor = originalBack
                    Catch
                        ' ignore
                    End Try
                    ' Restore original parent and location if this control was reparented while locked
                    If originalCategoryOverlayParents.ContainsKey(ctrl) Then
                        Try
                            Dim originalParent = originalCategoryOverlayParents(ctrl)
                            If originalParent IsNot Nothing AndAlso Not originalParent.IsDisposed AndAlso ctrl.Parent IsNot originalParent Then
                                ctrl.Parent = originalParent
                            End If
                        Catch
                            ' ignore
                        End Try
                    End If
                    If originalCategoryOverlayLocations.ContainsKey(ctrl) Then
                        Try
                            ctrl.Location = originalCategoryOverlayLocations(ctrl)
                        Catch
                            ' ignore
                        End Try
                    End If
                Next
            End If

            CategoryPanel.Invalidate()
        Catch ex As Exception
            Console.WriteLine($"ApplyPosLockColors error: {ex.Message}")
        End Try
    End Sub

    Private _categoryTileLabelMap As New Dictionary(Of Guna.UI2.WinForms.Guna2Button, List(Of Control))

    ' Bind a category tile's child labels so they share the button's hover background (#FBF7EC)
    Private Sub AttachCategoryTileHover(btn As Guna.UI2.WinForms.Guna2Button)
        If btn Is Nothing OrElse btn.IsDisposed OrElse _categoryTileLabelMap.ContainsKey(btn) Then Return

        Dim labels As New List(Of Control)
        For Each child As Control In btn.Controls
            labels.Add(child)
        Next
        _categoryTileLabelMap(btn) = labels

        AddHandler btn.MouseEnter, Sub()
                                       ApplyCategoryTileHover(btn, True)
                                   End Sub
        AddHandler btn.MouseLeave, Sub()
                                       If Not CursorWithinButton(btn) Then
                                           ApplyCategoryTileHover(btn, False)
                                       End If
                                   End Sub

        ' Hovering over a child label must light up the whole tile too
        For Each child As Control In labels
            AddHandler child.MouseEnter, Sub()
                                             ApplyCategoryTileHover(btn, True)
                                         End Sub
            AddHandler child.MouseLeave, Sub()
                                             If Not CursorWithinButton(btn) Then
                                                 ApplyCategoryTileHover(btn, False)
                                             End If
                                         End Sub
            child.Cursor = Cursors.Hand
        Next
    End Sub

    Private Function CursorWithinButton(btn As Guna.UI2.WinForms.Guna2Button) As Boolean
        Try
            Dim pt As Point = btn.PointToClient(Control.MousePosition)
            Return btn.ClientRectangle.Contains(pt)
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Sub ApplyCategoryTileHover(btn As Guna.UI2.WinForms.Guna2Button, hovering As Boolean)
        If posLockedForCapital Then Return
        Dim labels As List(Of Control) = Nothing
        If Not _categoryTileLabelMap.TryGetValue(btn, labels) Then Return
        Dim targetColor As Color = If(hovering, CategoryHoverBg, Color.White)
        btn.FillColor = targetColor
        btn.BorderColor = If(hovering, Color.FromArgb(190, 190, 190), BorderGray)
        For Each ctrl As Control In labels
            If ctrl IsNot Nothing AndAlso Not ctrl.IsDisposed Then
                ctrl.BackColor = targetColor
            End If
        Next
        ' Strengthen the soft shadow slightly on hover (same behavior as product cards)
        ProductCardBuilder.SetSoftShadowHover(btn.Parent, hovering)
    End Sub
    ' FIXED: Proper barcode key input handling
    ' FIXED: Remove Ctrl key, only use Shift for quantity selection
    Private Sub HandleBarcodeKeyInput(e As KeyEventArgs)
        Dim currentTime As DateTime = DateTime.Now

        ' Check if this is part of a barcode scan (fast input) or manual typing (slow input)
        If (currentTime - lastKeyTime).TotalMilliseconds > BARCODE_TIMEOUT Then
            ' Reset buffer if too much time has passed (not a continuous barcode scan)
            barcodeBuffer = ""
        End If

        lastKeyTime = currentTime

        ' Handle different key types
        Select Case e.KeyCode
            Case Keys.Enter
                ' Process the complete barcode
                If Not String.IsNullOrEmpty(barcodeBuffer) Then
                    Console.WriteLine($"Processing barcode from buffer: '{barcodeBuffer}'")

                    ' FIXED: Only check for Shift key (removed Ctrl)
                    Dim shouldShowQuantitySelector As Boolean = e.Shift

                    ProcessBarcodeWithModifiers(barcodeBuffer, shouldShowQuantitySelector)
                    barcodeBuffer = ""
                End If
                e.Handled = True

            Case Keys.D0 To Keys.D9, Keys.NumPad0 To Keys.NumPad9
                ' Add digits to barcode buffer
                Dim digit As String = ""
                If e.KeyCode >= Keys.D0 AndAlso e.KeyCode <= Keys.D9 Then
                    digit = (e.KeyCode - Keys.D0).ToString()
                Else
                    digit = (e.KeyCode - Keys.NumPad0).ToString()
                End If
                barcodeBuffer += digit
                e.Handled = True

            Case Keys.A To Keys.Z
                ' Add letters to barcode buffer (some barcodes contain letters)
                If Not e.Control AndAlso Not e.Alt Then ' Allow Ctrl+A, Alt+Tab, etc.
                    barcodeBuffer += e.KeyCode.ToString()
                    e.Handled = True
                End If

            Case Keys.OemMinus, Keys.Subtract
                ' Add dash/hyphen for barcode formats like PRD-001234
                barcodeBuffer += "-"
                e.Handled = True

            Case Keys.Back, Keys.Delete
                ' Allow backspace/delete to clear buffer
                If barcodeBuffer.Length > 0 Then
                    barcodeBuffer = barcodeBuffer.Substring(0, barcodeBuffer.Length - 1)
                End If
                e.Handled = True

            Case Keys.Escape
                ' Clear barcode buffer on Escape
                barcodeBuffer = ""
                e.Handled = True

            Case Else
                ' Block all other keys during normal operation to prevent interference
                If Not e.Control AndAlso Not e.Alt Then ' Allow system shortcuts
                    e.Handled = True
                End If
        End Select

        ' Show current buffer for debugging
        If barcodeBuffer.Length > 0 Then
            Console.WriteLine($"Barcode buffer: '{barcodeBuffer}'")
        End If
    End Sub
    ' FIXED: Process barcode with proper modifier key detection
    Private Sub ProcessBarcodeWithModifiers(barcode As String, shouldShowQuantitySelector As Boolean)
        Try
            Console.WriteLine($"ProcessBarcodeWithModifiers called with: '{barcode}', ShowQuantity: {shouldShowQuantitySelector}")

            ' Look for product by ProductCode
            Dim query As String = "SELECT ProductID, ProductName, SellingPrice, ProductCode, CurrentStock, Category FROM Products WHERE ProductCode = @ProductCode AND IsActive = 1"
            Dim param As New SqlParameter("@ProductCode", barcode)

            Using reader As DbDataReader = Utilities.ExecuteReader(query, {param})
                If reader.Read() Then
                    Console.WriteLine($"Product found: {reader("ProductName")}")

                    Dim productData As New Dictionary(Of String, Object) From {
                    {"ProductID", reader("ProductID")},
                    {"ProductName", reader("ProductName")},
                    {"Price", reader("SellingPrice")},
                    {"ProductCode", reader("ProductCode")},
                    {"Category", reader("Category")},
                    {"CurrentStock", reader("CurrentStock")}
                }

                    ' FIXED: Use the quantity selector flag directly
                    If shouldShowQuantitySelector Then
                        Console.WriteLine("Showing quantity selector for barcode scan")
                        ShowQuantitySelector(productData)
                    Else
                        Console.WriteLine("Adding single item from barcode scan")
                        ShowProductDetailsPanel(productData)

                        ' Show notification for successful barcode scan
                        ShowBarcodeAddedNotification(productData("ProductName").ToString())
                    End If

                Else
                    Console.WriteLine($"No product found for barcode: '{barcode}'")
                    ShowBarcodeNotFoundNotification(barcode)
                End If
            End Using

        Catch ex As Exception
            Console.WriteLine($"Error in ProcessBarcodeWithModifiers: {ex.Message}")
            ShowBarcodeErrorNotification(ex.Message)
        End Try
    End Sub
    Private Sub LogDiagnostic(message As String)
        Try
            Dim logPath As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JadeClinic", "diag.log")
            System.IO.File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}")
        Catch
        End Try
    End Sub

    Private Sub Sales_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.BackColor = Color.FromArgb(248, 248, 247)
        LogDiagnostic($"DIAG02 LOAD Sales form version={Me.GetType().Assembly.GetName().Version}")
        ' Stop idle timeout monitoring
        ' Start idle timeout monitoring
        IdleTimeoutManager.Instance.StartMonitoring(Me)
        IdleTimeoutManager.Instance.OnBeforeLogout = AddressOf PersistCartState
        Me.KeyPreview = True

        ' Make form full-screen and non-resizable; cover entire screen including taskbar
        Me.FormBorderStyle = FormBorderStyle.None
        Me.TopMost = True
        Me.WindowState = FormWindowState.Normal
        Me.Bounds = Screen.PrimaryScreen.Bounds
        Me.WindowState = FormWindowState.Maximized

        CategoryPanel.BorderRadius = 12 ' Rounded corners

        ' Create navigation menu using shared NavigationBuilder
        NavigationBuilder.Build(DashboardPanel, Me, "Sales")

        ' Validate user session
        If Not ValidateUserSession() Then
            Return
        End If

        ' Initialize profile section
        InitializeProfileSection()

        ' Update form title to show logged-in user
        Me.Text = $"Sales - {frmLoginvb.LoggedInUsername}"

        ' Start idle timeout monitoring
        IdleTimeoutManager.Instance.StartMonitoring(Me)

        AttachClickHandlersToAllControls(Me)

        ' Initialize print document
        printDocument = New PrintDocument()
        AddHandler printDocument.PrintPage, AddressOf OnPrintPage

        ' Build category tiles from a single code template (main categories first, then any distinct DB categories)
        BuildCategoryTiles()
        ArrangeCategoryButtonsFlexWrap()

        ' Cache lock colors AFTER tiles are built so the generated tiles and their child labels are captured
        InitializeCategoryLockCaches()
        ApplyPosLockColors(posLockedForCapital)

        backCategory.Visible = False

        ' Show empty cart state on initial load
        EnsureOrderSummaryGrid()

        ' Enforce daily opening capital when the form is shown to avoid blocking initial render
        AddHandler Me.Shown, Sub(shSender, shArgs) Me.BeginInvoke(Sub() EnsureCapitalBeforeUsingPOS())
        ' Show next possible Sale ID in lblOrderId
        Dim nextSaleId As Integer = 1
        Try
            Dim query As String = "SELECT IFNULL(MAX(SaleID), 0) + 1 AS NextSaleID FROM Sales"
            Using reader As DbDataReader = Utilities.ExecuteReader(query, New SqlParameter() {})
                If reader.Read() Then
                    nextSaleId = Convert.ToInt32(reader("NextSaleID"))
                End If
            End Using
        Catch ex As Exception
            nextSaleId = 1
        End Try

        If lblOrderId IsNot Nothing Then
            lblOrderId.Text = "Sale ID: " & nextSaleId.ToString()
        End If

        If lblSubTotal IsNot Nothing Then lblSubTotal.Text = "0.00"
        If taxLbl IsNot Nothing Then taxLbl.Text = "0.00"
        If totalLbl IsNot Nothing Then totalLbl.Text = "0.00"
        If totalRLbl IsNot Nothing Then totalRLbl.Text = "0.00"

        ' Restore persisted cart state after labels are initialized/reset
        RestoreCartState()

        UpdateCategoryItemCounts()

        ' REMOVED: All txtBarcodeInput setup since we handle barcode input directly through form KeyDown
        ' No longer need the txtBarcodeInput control
        ' ... existing code in Sales_Load ...



        SetupTabIndex()

        ' ... rest of the method ...
        ' Add keyboard instructions for users
    End Sub

    Private Sub SetupTabIndex()
        Dim tabIndex As Integer = 0
        For Each btn In _categoryTileButtons
            btn.TabIndex = tabIndex
            tabIndex += 1
        Next
        backCategory.TabIndex = tabIndex
        tabIndex += 1
        btnDiscount.TabIndex = tabIndex
        tabIndex += 1
        btnPayment.TabIndex = tabIndex
        tabIndex += 1
        confirmBtn.TabIndex = tabIndex
        Utilities.ApplyInputFocusEffects(Me)
    End Sub

    ' Returns true if there are any sales records for today.
    ' Returns true if there are any sales records for today.
    ' Returns true if there are any sales records for today.
    Private Function HasSalesForToday() As Boolean
        Try
            Dim sql As String = "SELECT COUNT(1) FROM Sales WHERE CAST(SaleDate AS date) = @Today"
            Dim result = Utilities.ExecuteScalar(sql, New SqlParameter() {
                New SqlParameter("@Today", Date.Today)
            })
            If result Is Nothing OrElse result Is DBNull.Value Then
                Return False
            End If
            Dim count As Integer = Convert.ToInt32(result)
            Return count > 0
        Catch ex As Exception
            ' On error assume safe default (do not allow edit)
            Console.WriteLine($"HasSalesForToday error: {ex.Message}")
            Return True
        End Try
    End Function









    Public Sub ShowCategoryProducts(categoryName As String)
        ' Clear the CategoryPanel
        CategoryPanel.Controls.Clear()
        productCardControls.Clear()
        productDbStock.Clear()
        backCategory.Visible = True
        LabelTitle.Text = categoryName

        ' Padding creates a gap for the GDI+ border drawn in CategoryPanel_Paint
        CategoryPanel.Padding = New Padding(2)

        ' Keep the search box visible so users can refine within the category
        If Not CategoryPanel.Controls.Contains(TxtSearch) Then
            CategoryPanel.Controls.Add(TxtSearch)
        End If
        TxtSearch.BringToFront()

        ' Unit filter combo on the right side of the panel
        Dim unitCombo = CreateAndPopulateUnitFilter()
        If Not CategoryPanel.Controls.Contains(_lblUnitFilter) Then
            CategoryPanel.Controls.Add(_lblUnitFilter)
        End If
        _lblUnitFilter.BringToFront()
        If Not CategoryPanel.Controls.Contains(unitCombo) Then
            CategoryPanel.Controls.Add(unitCombo)
        End If
        unitCombo.BringToFront()

        ' Use FlowLayoutPanel for responsive card layout (Dock Fill so it stays
        ' inside the rounded border; top padding reserves space for the search box)
        Dim flowPanel As New FlowLayoutPanel()
        flowPanel.Dock = DockStyle.Fill
        flowPanel.AutoScroll = True
        flowPanel.BackColor = Color.White
        flowPanel.Padding = New Padding(14, 74, 14, 14)
        CategoryPanel.Controls.Add(flowPanel)

        ' Count matching rows so the footer can show total pages
        Dim totalItems As Integer = 0
        Try
            Dim countQuery As String = "SELECT COUNT(*) FROM Products WHERE Category = @Category AND IsActive = 1"
            Dim countResult As Object = Utilities.ExecuteScalar(countQuery, {New SqlParameter("@Category", categoryName)})
            LogDiagnostic($"CAT category='{categoryName}' countResult={If(countResult Is Nothing, "Nothing", countResult.ToString())} type={If(countResult Is Nothing, "-", countResult.GetType().Name)}")
            If countResult IsNot Nothing Then
                totalItems = Convert.ToInt32(countResult)
            End If
            LogDiagnostic($"CAT category='{categoryName}' totalItems={totalItems}")
        Catch ex As Exception
            LogDiagnostic($"CAT category='{categoryName}' EXCEPTION: {ex.ToString()}")
            Console.WriteLine($"Category count error: {ex.Message}")
            MessageBox.Show($"Category count failed: {ex.Message}")
        End Try

        ' Footer pagination bar (Dock Bottom)
        Dim pagination As PaginationControl = GetPagination()
        pagination.Dock = DockStyle.Bottom
        pagination.Height = 62
        CategoryPanel.Controls.Add(pagination)
        pagination.Configure(totalItems, ProductPageSize, 1)
        LogDiagnostic($"CAT category='{categoryName}' footer totalItems={totalItems} totalPages={pagination.TotalPages} instance={pagination.GetHashCode()}")

        _paginationCategory = categoryName
        _paginationContext = PaginationContext.Category
        LoadCategoryProductsPage(categoryName, 1)

        CategoryPanel.Invalidate()
    End Sub

    ' Loads a single page of product cards for a category (raises no events)
    Private Sub LoadCategoryProductsPage(categoryName As String, page As Integer)
        Dim flowPanel As FlowLayoutPanel = CategoryPanel.Controls.OfType(Of FlowLayoutPanel)().FirstOrDefault()
        If flowPanel Is Nothing Then Return
        flowPanel.Controls.Clear()
        flowPanel.SuspendLayout()
        productCardControls.Clear()
        productDbStock.Clear()

        Dim offset As Integer = (page - 1) * ProductPageSize
        ' ORDER BY keeps the paging stable across queries
        Dim baseWhere As String = "WHERE Category = @Category AND IsActive = 1"
        If Not String.IsNullOrEmpty(_selectedUnitFilter) Then
            baseWhere &= " AND Unit = @Unit"
        End If
        Dim query As String = $"SELECT ProductID, ProductName, SellingPrice, ProductCode, ReorderLevel, CurrentStock, Category, Unit FROM Products {baseWhere} ORDER BY ProductName LIMIT @Limit OFFSET @Offset"
        Dim paramList As New List(Of SqlParameter) From {
            New SqlParameter("@Category", categoryName),
            New SqlParameter("@Limit", ProductPageSize),
            New SqlParameter("@Offset", offset)
        }
        If Not String.IsNullOrEmpty(_selectedUnitFilter) Then
            paramList.Add(New SqlParameter("@Unit", _selectedUnitFilter))
        End If
        Dim parameters As SqlParameter() = paramList.ToArray()
        Try
            Using reader As DbDataReader = Utilities.ExecuteReader(query, parameters)
                While reader.Read()
                    Dim stock As Integer = Convert.ToInt32(reader("CurrentStock"))

                    Dim productData As New Dictionary(Of String, Object) From {
                        {"ProductID", reader("ProductID")},
                        {"ProductName", reader("ProductName")},
                        {"Price", Convert.ToDecimal(reader("SellingPrice"))},
                        {"ProductCode", reader("ProductCode")},
                        {"Category", reader("Category")},
                        {"Unit", reader("Unit")},
                        {"CurrentStock", stock}
                    }
                    productDbStock(reader("ProductID").ToString()) = stock

                    ' Show available stock (raw stock minus what is already reserved in the current order)
                    Dim reservedQty As Integer = 0
                    For Each orderItem In currentOrderList
                        If orderItem("ProductID").ToString() = reader("ProductID").ToString() Then
                            reservedQty += CInt(orderItem("Quantity"))
                        End If
                    Next

                    Dim productCard = ProductCardBuilder.Create(productData, LoadProductImage(Convert.ToInt32(reader("ProductID")), 85, 78),
                                                                Sub() HandleProductInteraction(productData, False))
                    ProductCardBuilder.UpdateStock(productCard, Math.Max(0, stock - reservedQty))

                    productCardControls.Add(productCard)
                    flowPanel.Controls.Add(productCard)
                End While
            End Using
        Catch ex As Exception
            Console.WriteLine($"Category load error: {ex.Message}")
        End Try

        flowPanel.ResumeLayout(True)
        flowPanel.AutoScrollPosition = New Point(0, 0)
        LogDiagnostic($"CAT category='{categoryName}' page={page} cards={flowPanel.Controls.Count}")
    End Sub

    ' Lazily creates the shared pagination footer so it can be re-docked across views
    Private Function GetPagination() As PaginationControl
        If _pagination Is Nothing Then
            _pagination = New PaginationControl()
            AddHandler _pagination.PageChanged, AddressOf Pagination_PageChanged
        End If
        Return _pagination
    End Function

    ' Routes footer navigation back to the active listing (category or search)
    Private Sub Pagination_PageChanged(page As Integer)
        If _paginationContext = PaginationContext.Category AndAlso Not String.IsNullOrEmpty(_paginationCategory) Then
            LoadCategoryProductsPage(_paginationCategory, page)
        ElseIf _paginationContext = PaginationContext.Search AndAlso Not String.IsNullOrEmpty(_paginationSearchTerm) Then
            LoadSearchProductsPage(_paginationSearchTerm, page)
        End If
        FocusBarcodeInputIfAllowed()
    End Sub

    ' Builds and returns a unit filter label + ComboBox positioned right of the search box
    Private Function CreateAndPopulateUnitFilter() As Guna2ComboBox
        ' Label — vertically centered next to the ComboBox
        If _lblUnitFilter Is Nothing OrElse _lblUnitFilter.IsDisposed Then
            _lblUnitFilter = New Label()
            _lblUnitFilter.Text = "Unit:"
            _lblUnitFilter.Font = New Font("Poppins", 10.0F, FontStyle.Regular)
            _lblUnitFilter.ForeColor = Color.FromArgb(80, 80, 80)
            _lblUnitFilter.BackColor = Color.Transparent
            _lblUnitFilter.AutoSize = True
            _lblUnitFilter.Location = New Point(391, 30)
        End If

        ' ComboBox — same Y/height as TxtSearch for a cohesive filter bar
        If _cmbUnitFilter Is Nothing OrElse _cmbUnitFilter.IsDisposed Then
            _cmbUnitFilter = New Guna2ComboBox()
            _cmbUnitFilter.Font = New Font("Poppins", 10.0F, FontStyle.Regular)
            _cmbUnitFilter.Size = New Size(170, 47)
            _cmbUnitFilter.Location = New Point(448, 22)
            _cmbUnitFilter.BorderRadius = 6
            _cmbUnitFilter.BackColor = Color.White
            _cmbUnitFilter.ForeColor = Color.FromArgb(51, 51, 51)
            _cmbUnitFilter.BorderColor = Color.DarkGray
            _cmbUnitFilter.BorderThickness = 1
            AddHandler _cmbUnitFilter.SelectedIndexChanged, AddressOf UnitFilter_Changed
        End If

        _cmbUnitFilter.Items.Clear()
        _cmbUnitFilter.Items.Add("All Units")
        Try
            Using reader = Utilities.ExecuteReader("SELECT DISTINCT Unit FROM Products WHERE IsActive = 1 AND Unit IS NOT NULL AND Unit <> '' ORDER BY Unit", {})
                While reader.Read()
                    _cmbUnitFilter.Items.Add(reader("Unit").ToString())
                End While
            End Using
        Catch
        End Try

        ' Default selection
        If _cmbUnitFilter.Items.Count > 0 Then
            _cmbUnitFilter.SelectedIndex = 0
        End If
        _selectedUnitFilter = ""

        Return _cmbUnitFilter
    End Function

    Private Sub UnitFilter_Changed(sender As Object, e As EventArgs)
        If _cmbUnitFilter Is Nothing OrElse _cmbUnitFilter.IsDisposed Then Return
        Dim selected As String = If(_cmbUnitFilter.SelectedItem IsNot Nothing, _cmbUnitFilter.SelectedItem.ToString(), "All Units")
        If selected = "All Units" Then
            _selectedUnitFilter = ""
        Else
            _selectedUnitFilter = selected
        End If

        ' Reload current page 1 with new filter
        If _paginationContext = PaginationContext.Category AndAlso Not String.IsNullOrEmpty(_paginationCategory) Then
            ReloadCategoryWithFilter()
        ElseIf _paginationContext = PaginationContext.Search AndAlso Not String.IsNullOrEmpty(_paginationSearchTerm) Then
            ReloadSearchWithFilter()
        End If
    End Sub

    Private Sub ReloadCategoryWithFilter()
        ' Recount with unit filter and reload page 1
        Dim totalItems As Integer = 0
        Try
            Dim countQuery As String = "SELECT COUNT(*) FROM Products WHERE Category = @Category AND IsActive = 1"
            Dim countParams As New List(Of SqlParameter) From {New SqlParameter("@Category", _paginationCategory)}
            If Not String.IsNullOrEmpty(_selectedUnitFilter) Then
                countQuery &= " AND Unit = @Unit"
                countParams.Add(New SqlParameter("@Unit", _selectedUnitFilter))
            End If
            Dim countResult = Utilities.ExecuteScalar(countQuery, countParams.ToArray())
            If countResult IsNot Nothing Then totalItems = Convert.ToInt32(countResult)
        Catch
        End Try

        Dim pagination = GetPagination()
        pagination.Configure(totalItems, ProductPageSize, 1)
        LoadCategoryProductsPage(_paginationCategory, 1)
    End Sub

    Private Sub ReloadSearchWithFilter()
        Dim totalItems As Integer = 0
        Try
            Dim countQuery As String = "SELECT COUNT(*) FROM Products WHERE IsActive = 1 AND (ProductCode = @term OR ProductName LIKE @like)"
            Dim countParams As New List(Of SqlParameter) From {
                New SqlParameter("@term", _paginationSearchTerm),
                New SqlParameter("@like", "%" & _paginationSearchTerm & "%")
            }
            If Not String.IsNullOrEmpty(_selectedUnitFilter) Then
                countQuery &= " AND Unit = @Unit"
                countParams.Add(New SqlParameter("@Unit", _selectedUnitFilter))
            End If
            Dim countResult = Utilities.ExecuteScalar(countQuery, countParams.ToArray())
            If countResult IsNot Nothing Then totalItems = Convert.ToInt32(countResult)
        Catch
        End Try

        Dim pagination = GetPagination()
        pagination.Configure(totalItems, ProductPageSize, 1)
        LoadSearchProductsPage(_paginationSearchTerm, 1)
    End Sub

    ' UNIFIED: Handle both manual clicks and barcode scans
    ' FIXED: Handle both manual clicks and barcode scans with better modifier detection
    ' FIXED: Handle both manual clicks and barcode scans with only Shift key
    Private Sub HandleProductInteraction(productData As Dictionary(Of String, Object), isFromBarcode As Boolean)
        ' Prevent interactions when customer selection or payment panels are active

        If posLockedForCapital Then
            MessageBox.Show("POS is locked. Manager/Admin must set opening capital first.", "POS Locked", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If



        If pinPanelActive OrElse totalPanelActive Then
            Return
        End If

        ' Check stock availability (DB stock minus what's already reserved in cart)
        Dim prodIdForCheck As String = productData("ProductID").ToString()
        Dim rawDbStock As Integer = If(productDbStock.ContainsKey(prodIdForCheck), productDbStock(prodIdForCheck), If(productData.ContainsKey("CurrentStock"), CInt(productData("CurrentStock")), 0))
        Dim reservedInCart As Integer = 0
        For Each item In currentOrderList
            If item("ProductID").ToString() = prodIdForCheck Then
                reservedInCart += CInt(item("Quantity"))
            End If
        Next
        Dim effectiveStock As Integer = Math.Max(0, rawDbStock - reservedInCart)
        If effectiveStock = 0 Then
            If isFromBarcode Then
                ShowBarcodeNotFoundNotification("Out of Stock")
            Else
                MessageBox.Show("This product is out of stock and cannot be added to the order.", "Out of Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
            Return
        End If

        ' FIXED: For barcode scans, the quantity selector decision is made in ProcessBarcodeWithModifiers
        ' For manual clicks, check only Shift key (removed Ctrl)
        If Not isFromBarcode Then
            Dim shouldShowQuantitySelector As Boolean = (Control.ModifierKeys = Keys.Shift)

            If shouldShowQuantitySelector Then
                ShowQuantitySelector(productData)
            Else
                ShowProductDetailsPanel(productData)
            End If
        Else
            ' For barcode scans, just add the item (modifier check already done)
            ShowProductDetailsPanel(productData)
            ShowBarcodeAddedNotification(productData("ProductName").ToString())
        End If

        ' No need to focus barcode input anymore since we handle keys directly
    End Sub
    ' Brief toast notification for barcode scans
    ' FIXED: Brief toast notification for barcode scans - centered at top of screen
    Private Sub ShowBarcodeAddedNotification(productName As String)
        ' Create a temporary label for feedback
        Dim notificationLabel As New Label()
        notificationLabel.Text = $"{productName} added!"
        notificationLabel.Font = New Font("Poppins", 12, FontStyle.Bold)
        notificationLabel.ForeColor = PureWhite
        notificationLabel.BackColor = SuccessGreen
        notificationLabel.AutoSize = True
        notificationLabel.Padding = New Padding(15, 8, 15, 8)
        notificationLabel.TextAlign = ContentAlignment.MiddleCenter

        ' FIXED: Center at the top of the entire form (not just CategoryPanel)
        ' Wait for the label to size itself
        Me.Controls.Add(notificationLabel)
        notificationLabel.BringToFront()

        ' Now position it at the center top after it's been added
        Application.DoEvents() ' Ensure label is sized

        Dim centerX As Integer = (Me.ClientSize.Width - notificationLabel.Width) / 2
        notificationLabel.Location = New Point(centerX, 20) ' 20px from top of form


        ' Add rounded corners effect
        notificationLabel.BackColor = Color.FromArgb(220, SuccessGreen.R, SuccessGreen.G, SuccessGreen.B)

        ' Auto-remove after 2 seconds with fade out effect
        Dim removeTimer As New Timer() With {.Interval = 1800} ' Start fade earlier
        Dim fadeTimer As New Timer() With {.Interval = 50} ' Fade animation
        Dim fadeSteps As Integer = 10
        Dim currentStep As Integer = 0

        AddHandler removeTimer.Tick, Sub()
                                         removeTimer.Stop()
                                         ' Start fade out animation
                                         AddHandler fadeTimer.Tick, Sub()
                                                                        currentStep += 1
                                                                        Dim alpha As Integer = CInt(255 * (1 - (currentStep / fadeSteps)))
                                                                        If alpha <= 0 OrElse currentStep >= fadeSteps Then
                                                                            fadeTimer.Stop()
                                                                            If Me.Controls.Contains(notificationLabel) Then
                                                                                Me.Controls.Remove(notificationLabel)
                                                                                notificationLabel.Dispose()
                                                                            End If
                                                                            fadeTimer.Dispose()
                                                                        Else
                                                                            notificationLabel.BackColor = Color.FromArgb(alpha, SuccessGreen.R, SuccessGreen.G, SuccessGreen.B)
                                                                        End If
                                                                    End Sub
                                         fadeTimer.Start()
                                         removeTimer.Dispose()
                                     End Sub
        removeTimer.Start()
    End Sub
    ' Add instruction label for users
    Private Sub AddBarcodeInstructions()
        Dim instructionLabel As New Label()
        instructionLabel.Text = "Tip: Hold Shift/Ctrl while scanning or clicking for quantity selection"
        instructionLabel.Font = New Font("Poppins", 9, FontStyle.Italic)
        instructionLabel.ForeColor = MediumText
        instructionLabel.Location = New Point(20, Me.Height - 80)
        instructionLabel.AutoSize = True
        Me.Controls.Add(instructionLabel)
    End Sub
    Private Sub ShowProductDetailsPanel(productData As Dictionary(Of String, Object))


        If posLockedForCapital Then
            MessageBox.Show("POS is locked. Manager/Admin must set opening capital first.", "POS Locked", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        ' Prevent product clicks when customer selection or payment panels are active
        If pinPanelActive OrElse totalPanelActive Then
            Return ' Exit without adding to order
        End If

        ' Check if the product has stock available (DB stock minus reserved in cart)
        Dim spProdId As String = productData("ProductID").ToString()
        Dim spDbStock As Integer = If(productDbStock.ContainsKey(spProdId), productDbStock(spProdId), If(productData.ContainsKey("CurrentStock"), CInt(productData("CurrentStock")), 0))
        Dim spReserved As Integer = 0
        For Each item In currentOrderList
            If item("ProductID").ToString() = spProdId Then
                spReserved += CInt(item("Quantity"))
            End If
        Next
        If Math.Max(0, spDbStock - spReserved) = 0 Then
            MessageBox.Show("This product is out of stock and cannot be added to the order.", "Out of Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return ' Exit without adding to order
        End If

        ' Check if product already exists in the order list
        Dim foundIndex As Integer = -1
        For i = 0 To currentOrderList.Count - 1
            If currentOrderList(i)("ProductID").ToString() = productData("ProductID").ToString() Then
                foundIndex = i
                Exit For
            End If
        Next

        Dim priceToUse As Decimal = Convert.ToDecimal(productData("Price"))

        If foundIndex <> -1 Then
            ' Check if we have enough stock for the increase
            Dim currentQuantity As Integer = CInt(currentOrderList(foundIndex)("Quantity"))
            Dim prodId As String = productData("ProductID").ToString()
            Dim dbStock As Integer = If(productDbStock.ContainsKey(prodId), productDbStock(prodId), If(productData.ContainsKey("CurrentStock"), CInt(productData("CurrentStock")), 0))

            ' Get already reserved quantity in order
            Dim reservedQuantity As Integer = 0
            For Each item In currentOrderList
                If item("ProductID").ToString() = prodId Then
                    reservedQuantity = CInt(item("Quantity"))
                    Exit For
                End If
            Next

            If reservedQuantity >= dbStock Then
                MessageBox.Show("Cannot add more items. Not enough stock available.", "Insufficient Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' Increase quantity by 1 (fixed the issue where it was set to 2)
            currentOrderList(foundIndex)("Quantity") = currentQuantity + 1
        Else
            ' Add product with quantity 1
            productData("Quantity") = 1
            productData("Price") = priceToUse
            currentOrderList.Add(productData)
        End If

        ' Update stock label using DB stock minus reserved in cart
        UpdateStockLabelFromDbStock(productData("ProductID").ToString())

        ' Refresh the order display
        RefreshOrderDisplay()
    End Sub

    ' New method to reduce item quantity or remove item
    Private Sub ReduceItemQuantity(itemIndex As Integer)
        If itemIndex < 0 OrElse itemIndex >= currentOrderList.Count Then
            Return
        End If

        Dim currentQuantity As Integer = CInt(currentOrderList(itemIndex)("Quantity"))
        Dim productName As String = currentOrderList(itemIndex)("ProductName").ToString()
        Dim productId As String = currentOrderList(itemIndex)("ProductID").ToString()

        ' Detect Shift key to void the entire line
        Dim shiftPressed As Boolean = (Control.ModifierKeys And Keys.Shift) = Keys.Shift

        If shiftPressed Then
            ' Directly show authorization modal (no extra MessageBox confirmation)
            Try
                ' Give UI a tick to settle focus/fires (improves responsiveness on first click)
                Application.DoEvents()

                ' Provide visual feedback that action is in progress
                Me.Cursor = Cursors.WaitCursor
                Dim approver As String = ""
                Dim authorized As Boolean = ShowVoidAuthorizationModal(productName, currentQuantity, approver)
                Me.Cursor = Cursors.Default

                If Not authorized Then
                    ' Authorization cancelled/failed � nothing to do
                    Return
                End If

                ' Remove the item line
                Dim removedProductId As String = currentOrderList(itemIndex)("ProductID").ToString()
                currentOrderList.RemoveAt(itemIndex)

                ' Update UI and counts
                RefreshOrderDisplay()
                UpdateCategoryItemCounts()
                UpdateStockLabelFromDbStock(removedProductId)

                ' Keep the persisted draft cart in sync so a later exit never
                ' restores a stale (pre-void) snapshot.
                PersistCartState()

                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "POS Line Voided", $"Product: {productName}, Qty: {currentQuantity}, AuthorizedBy: {approver}")
                ShowVoidSuccessNotification(productName & $" (x{currentQuantity})", approver)
            Finally
                Me.Cursor = Cursors.Default
            End Try

            Return
        End If

        ' Normal (single-step) reduction behavior
        If currentQuantity > 1 Then
            currentOrderList(itemIndex)("Quantity") = currentQuantity - 1

            UpdateStockLabelFromDbStock(productId)

            RefreshOrderDisplay()
            UpdateCategoryItemCounts()
            PersistCartState()
            Return
        End If

        ' currentQuantity = 1 => removing the last unit -> require authorization
        Dim approverLocal As String = ""
        If Not ShowVoidAuthorizationModal(productName, 1, approverLocal) Then
            Return
        End If

        Dim removedId As String = currentOrderList(itemIndex)("ProductID").ToString()
        currentOrderList.RemoveAt(itemIndex)
        UpdateStockLabelFromDbStock(removedId)
        RefreshOrderDisplay()
        UpdateCategoryItemCounts()
        PersistCartState()

        Utilities.LogAudit(frmLoginvb.LoggedInUsername, "POS Item Voided", $"Product: {productName}, Qty: 1, AuthorizedBy: {approverLocal}")
        ShowVoidSuccessNotification(productName, approverLocal)
    End Sub
    ' Non-blocking toast notification for void success
    Private Sub ShowVoidSuccessNotification(productName As String, approver As String)
        Dim extra As String = ""
        If Not String.IsNullOrWhiteSpace(approver) Then
            extra = $"By: {approver}"
        End If
        ShowToastNotification($"Item Voided: {productName}", SuccessGreen, extra)
    End Sub

    ' Non-blocking toast notification shown after a sale is created
    Private Sub ShowSaleCreatedNotification(saleNumber As String, total As Decimal)
        ShowToastNotification("Sale Created", SuccessGreen, $"{saleNumber}  •  {ChrW(&H20B1)}{total:F2}")
    End Sub

    ' Shared auto-dismissing toast near the top of the form (fades out after ~1.8s)
    Private Sub ShowToastNotification(text As String, accent As Color, Optional extra As String = "")
        Dim notificationLabel As New Label()
        notificationLabel.Text = text
        If Not String.IsNullOrWhiteSpace(extra) Then
            notificationLabel.Text &= $"  •  {extra}"
        End If
        notificationLabel.Font = New Font("Poppins", 11, FontStyle.Bold)
        notificationLabel.ForeColor = PureWhite
        notificationLabel.BackColor = Color.FromArgb(220, accent.R, accent.G, accent.B)
        notificationLabel.AutoSize = False
        notificationLabel.Padding = New Padding(12, 8, 12, 8)
        notificationLabel.TextAlign = ContentAlignment.MiddleCenter

        ' Measure the text first and place the toast at its final centered
        ' position before adding it, so it never renders at the top-left and
        ' then jumps to the middle.
        Dim measured As Size = TextRenderer.MeasureText(notificationLabel.Text, notificationLabel.Font)
        notificationLabel.Size = New Size(measured.Width + notificationLabel.Padding.Horizontal + 4,
                                          measured.Height + notificationLabel.Padding.Vertical + 4)
        Dim centerX As Integer = (Me.ClientSize.Width - notificationLabel.Width) / 2
        notificationLabel.Location = New Point(Math.Max(centerX, 4), 20) ' centered at the top

        Me.Controls.Add(notificationLabel)
        notificationLabel.BringToFront()

        ' Auto-remove after ~1.8s with fade out
        Dim removeTimer As New Timer() With {.Interval = 1800}
        Dim fadeTimer As New Timer() With {.Interval = 50}
        Dim fadeSteps As Integer = 10
        Dim currentStep As Integer = 0

        AddHandler removeTimer.Tick, Sub()
                                         removeTimer.Stop()
                                         AddHandler fadeTimer.Tick, Sub()
                                                                        currentStep += 1
                                                                        Dim alpha As Integer = CInt(255 * (1 - (currentStep / fadeSteps)))
                                                                        If alpha <= 0 OrElse currentStep >= fadeSteps Then
                                                                            fadeTimer.Stop()
                                                                            If Me.Controls.Contains(notificationLabel) Then
                                                                                Me.Controls.Remove(notificationLabel)
                                                                                notificationLabel.Dispose()
                                                                            End If
                                                                            fadeTimer.Dispose()
                                                                        Else
                                                                            notificationLabel.BackColor = Color.FromArgb(alpha, accent.R, accent.G, accent.B)
                                                                        End If
                                                                    End Sub
                                         fadeTimer.Start()
                                         removeTimer.Dispose()
                                     End Sub

        removeTimer.Start()
    End Sub
    Private Sub UpdateStockLabel(productId As String, newStock As Integer)
        For Each productCard As Control In productCardControls
            If ProductCardBuilder.GetProductId(productCard) = productId Then
                ProductCardBuilder.UpdateStock(productCard, newStock)
                Exit For
            End If
        Next
    End Sub

    Private Sub UpdateStockLabelFromDbStock(productId As String)
        If Not productDbStock.ContainsKey(productId) Then
            Dim card = productCardControls.FirstOrDefault(Function(c) ProductCardBuilder.GetProductId(c) = productId)
            If card IsNot Nothing Then
                productDbStock(productId) = ProductCardBuilder.GetDisplayedStock(card)
            End If
        End If
        If Not productDbStock.ContainsKey(productId) Then Return
        Dim dbStock As Integer = productDbStock(productId)
        Dim reservedQty As Integer = 0
        For Each item In currentOrderList
            If item("ProductID").ToString() = productId Then
                reservedQty += CInt(item("Quantity"))
            End If
        Next
        UpdateStockLabel(productId, Math.Max(0, dbStock - reservedQty))
    End Sub

    Private Sub UpdateCategoryItemCounts()
        ' Query the database to get the count of distinct products for each category
        Try
            Dim query As String = "SELECT Category, COUNT(*) AS TotalProducts FROM Products WHERE IsActive = 1 GROUP BY Category"
            Using reader As DbDataReader = Utilities.ExecuteReader(query, New SqlParameter() {})
                While reader.Read()
                    Dim category As String = reader("Category").ToString().ToUpper()
                    Dim totalProducts As Integer = Convert.ToInt32(reader("TotalProducts"))

                    ' Update the corresponding label if a tile exists for this category
                    If _categoryCountLabels.ContainsKey(category) Then
                        _categoryCountLabels(category).Text = $"{totalProducts.ToString()} Items"
                    End If
                End While
            End Using
        Catch ex As Exception
            Console.WriteLine($"Error updating category counts: {ex.Message}")
        End Try

        ' Set labels for categories with no products to "0"
        For Each kvp In _categoryCountLabels
            If String.IsNullOrEmpty(kvp.Value.Text) OrElse kvp.Value.Text = "0 Items" Then
                kvp.Value.Text = "0 Items"
            End If
        Next
    End Sub

    ' Build all category tiles in code: main categories first, then any distinct DB categories.
    Private Sub BuildCategoryTiles()
        ' Remove previously generated tiles (keep the designer search box)
        For Each host In _categoryTileHosts
            If CategoryPanel.Controls.Contains(host) Then
                CategoryPanel.Controls.Remove(host)
            End If
            host.Dispose()
        Next
        _categoryTileHosts.Clear()
        For Each btn In _categoryTileButtons
            btn.Dispose()
        Next
        _categoryTileButtons.Clear()
        _categoryCountLabels.Clear()
        _categoryTileLabelMap.Clear()

        ' Build the category list: main categories first, then distinct DB categories
        Dim categories As New List(Of String)(_preferredCategoryOrder)
        Try
            Dim query As String = "SELECT DISTINCT Category FROM Products WHERE Category IS NOT NULL AND Category <> '' AND IsActive = 1"
            Using reader As DbDataReader = Utilities.ExecuteReader(query, New SqlParameter() {})
                While reader.Read()
                    Dim catName As String = reader("Category").ToString().ToUpper()
                    If Not categories.Any(Function(c) NormalizeCategory(c) = NormalizeCategory(catName)) Then
                        categories.Add(catName)
                    End If
                End While
            End Using
        Catch ex As Exception
            Console.WriteLine($"Error loading categories: {ex.Message}")
        End Try

        ' Ensure the search box is present (BuildCategoryTiles also runs after the panel is cleared)
        If Not CategoryPanel.Controls.Contains(TxtSearch) Then
            CategoryPanel.Controls.Add(TxtSearch)
        End If

        Dim template As Guna.UI2.WinForms.Guna2Button = CreateCategoryTileTemplate()
        For Each catName In categories
            CreateCategoryTile(catName, template)
        Next
        template.Dispose()

        ' Refresh lock-state caches/colors so freshly built tiles reflect the current POS lock
        InitializeCategoryLockCaches()
        ApplyPosLockColors(posLockedForCapital)
    End Sub

    ' Shared tile appearance for every category tile
    Private Function CreateCategoryTileTemplate() As Guna.UI2.WinForms.Guna2Button
        Dim tpl As New Guna.UI2.WinForms.Guna2Button()
        tpl.Size = _categoryTileSize
        tpl.BorderRadius = 20
        tpl.FillColor = Color.White
        tpl.ForeColor = CategoryText
        tpl.BackColor = Color.Transparent
        tpl.BorderColor = BorderGray
        tpl.BorderThickness = 2
        tpl.PressedColor = CategoryHoverBg
        tpl.HoverState.FillColor = CategoryHoverBg
        tpl.HoverState.BorderColor = Color.FromArgb(190, 190, 190)
        tpl.Text = ""
        Return tpl
    End Function

    ' Clone the template and attach the icon / name / count child labels plus wiring
    Private Sub CreateCategoryTile(catName As String, template As Guna.UI2.WinForms.Guna2Button)
        Dim btn As New Guna.UI2.WinForms.Guna2Button()
        ' The tile face is inset slightly so the host's soft shadow peeks out on the right/bottom
        btn.Size = New Size(template.Size.Width - 10, template.Size.Height - 12)
        btn.BorderRadius = template.BorderRadius
        btn.FillColor = template.FillColor
        btn.ForeColor = template.ForeColor
        btn.BackColor = template.BackColor
        btn.BorderColor = template.BorderColor
        btn.BorderThickness = template.BorderThickness
        btn.PressedColor = template.PressedColor
        btn.HoverState.FillColor = template.HoverState.FillColor
        btn.HoverState.BorderColor = template.HoverState.BorderColor
        btn.Text = ""

        ' Icon label (top area)
        Dim iconLbl As New Label()
        iconLbl.Text = CategoryIcon(catName)
        iconLbl.AutoSize = False
        iconLbl.Size = New Size(190, 84)
        iconLbl.Location = New Point(12, 30)
        iconLbl.TextAlign = ContentAlignment.MiddleCenter
        iconLbl.Font = New Font("Segoe UI", 36.0F)
        iconLbl.ForeColor = JadeOlive
        iconLbl.BackColor = Color.White
        iconLbl.Name = "IconLbl"

        ' Category name label
        Dim nameLbl As New Label()
        nameLbl.Text = catName
        nameLbl.AutoSize = False
        nameLbl.Size = New Size(191, 38)
        nameLbl.Location = New Point(12, 124)
        nameLbl.TextAlign = ContentAlignment.MiddleCenter
        nameLbl.Font = New Font("Poppins", 12.0F, FontStyle.Bold)
        nameLbl.ForeColor = CategoryText
        nameLbl.BackColor = Color.White
        nameLbl.Name = "NameLbl"

        ' Item count label (subtext)
        Dim countLbl As New Guna.UI2.WinForms.Guna2HtmlLabel()
        countLbl.Text = "0 Items"
        countLbl.AutoSize = False
        countLbl.Size = New Size(191, 30)
        countLbl.Location = New Point(12, 170)
        countLbl.TextAlignment = ContentAlignment.MiddleCenter
        countLbl.Font = New Font("Poppins", 9.0F)
        countLbl.ForeColor = MediumText
        countLbl.BackColor = Color.White
        countLbl.Name = "CountLbl"

        btn.Controls.Add(iconLbl)
        btn.Controls.Add(nameLbl)
        btn.Controls.Add(countLbl)

        Dim toolTip As New ToolTip()
        toolTip.SetToolTip(btn, $"Click to view {catName} products")

        ' Whole tile clickable: forward clicks from the child labels to the same handler
        AddHandler btn.Click, Sub(senderBtn, eBtn)
                                  TileClicked(catName)
                              End Sub
        For Each lbl As Control In {iconLbl, nameLbl, countLbl}
            AddHandler lbl.Click, Sub(senderLbl, eLbl)
                                      TileClicked(catName)
                                  End Sub
        Next
        AddHandler btn.MouseEnter, Sub(senderBtn, eBtn)
                                       Dim b = CType(senderBtn, Guna.UI2.WinForms.Guna2Button)
                                       b.Cursor = Cursors.Hand
                                   End Sub
        AddHandler btn.MouseLeave, Sub(senderBtn, eBtn)
                                       Dim b = CType(senderBtn, Guna.UI2.WinForms.Guna2Button)
                                       b.Cursor = Cursors.Default
                                   End Sub
        AttachCategoryTileHover(btn)

        ' Wrap the tile face in a host that paints the same soft shadow as the
        ' product cards (replaces the old Guna ShadowDecoration).
        Dim host = ProductCardBuilder.CreateSoftShadowHost(template.Size, template.BorderRadius)
        host.Controls.Add(btn)

        _categoryTileButtons.Add(btn)
        _categoryTileHosts.Add(host)
        _categoryCountLabels(catName) = countLbl

        CategoryPanel.Controls.Add(host)
    End Sub

    ' Shared category-tile click behavior (button body or child labels)
    Private Sub TileClicked(catName As String)
        ShowCategoryProducts(catName)
        If ProfileManager.IsProfileDropdownVisible(Me) Then
            ProfileManager.HideProfileDropdown(Me)
        End If
        FocusBarcodeInputIfAllowed()
    End Sub

    ' Emoji icon per category (falls back to a package box for unknown categories)
    Private Function CategoryIcon(catName As String) As String
        For Each kvp In _categoryIconGlyphs
            If NormalizeCategory(kvp.Key) = NormalizeCategory(catName) Then
                Return kvp.Value
            End If
        Next
        Return "📦"
    End Function

    ' Arrange the code-generated tiles in a fixed grid (4 columns, Ortho-style tall tiles)
    Private Sub ArrangeCategoryButtonsFlexWrap()
        For index As Integer = 0 To _categoryTileHosts.Count - 1
            Dim host = _categoryTileHosts(index)
            Dim col As Integer = index Mod _categoryGridCols
            Dim row As Integer = index \ _categoryGridCols
            host.Size = _categoryTileSize
            host.Location = New Point(_categoryGridStart.X + (col * (_categoryTileSize.Width + _categoryGridGapX)), _categoryGridStart.Y + (row * (_categoryTileSize.Height + _categoryGridGapY)))
        Next

        ' Ensure CategoryPanel can scroll if content exceeds visible area
        CategoryPanel.AutoScroll = True
    End Sub

    Private Sub backCategory_Click(sender As Object, e As EventArgs) Handles backCategory.Click
        ' Going back to the categories grid resets the search box
        If _searchTimer IsNot Nothing Then _searchTimer.Stop()
        If TxtSearch IsNot Nothing Then TxtSearch.Text = ""
        _selectedUnitFilter = ""

        ' Store the current state before clearing
        CategoryPanel.SuspendLayout()

        ' Clear the CategoryPanel
        CategoryPanel.Controls.Clear()

        ' Rebuild the code-generated category tiles
        BuildCategoryTiles()
        ArrangeCategoryButtonsFlexWrap()
        UpdateCategoryItemCounts()

        LabelTitle.Text = "Categories"
        backCategory.Visible = False
        _paginationContext = PaginationContext.None

        ' Reset scroll position to top
        CategoryPanel.AutoScrollPosition = New Point(0, 0)

        ' Resume layout
        CategoryPanel.ResumeLayout(True)
        CategoryPanel.Refresh()
    End Sub

    Private Sub AttachClickHandlersToAllControls(parentControl As Control)
        For Each ctrl As Control In parentControl.Controls
            ' Skip the profile controls to avoid immediate hide after toggle
            If ctrl Is Guna2CirclePictureBox5 OrElse ctrl Is lblUsername Then
                If ctrl.HasChildren Then
                    AttachClickHandlersToAllControls(ctrl)
                End If
                Continue For
            End If

            ' Add a click handler that will hide the dropdown if visible
            AddHandler ctrl.Click, Sub()
                                       If ProfileManager.IsProfileDropdownVisible(Me) Then
                                           ProfileManager.HideProfileDropdown(Me)
                                       End If

                                       ' Keep original focus behaviour for barcode input
                                       FocusBarcodeInputIfAllowed()
                                   End Sub

            If ctrl.HasChildren Then
                AttachClickHandlersToAllControls(ctrl)
            End If
        Next
    End Sub

    Private Sub Control_Click(sender As Object, e As EventArgs)
        ' Focus the barcode input when any control is clicked
        FocusBarcodeInputIfAllowed()
    End Sub

    Private Sub FocusBarcodeInputIfAllowed()
        ' Don't focus barcode input when customer selection or payment panels are active
        If Not pinPanelActive AndAlso Not totalPanelActive AndAlso Not ProfileManager.IsProfileDropdownVisible(Me) Then
            Try
                If txtBarcodeInput IsNot Nothing AndAlso Not txtBarcodeInput.IsDisposed Then
                    Console.WriteLine("Focusing barcode input")
                    txtBarcodeInput.Focus()
                    txtBarcodeInput.Select()
                End If
            Catch ex As Exception
                Console.WriteLine($"Error focusing barcode input: {ex.Message}")
            End Try
        Else
            Console.WriteLine("Barcode input focus blocked - panels active")
        End If
    End Sub

    ' Make currentOrderList accessible
    Public currentOrderList As New List(Of Dictionary(Of String, Object))

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
    Private Function IsManagerOrAdmin() As Boolean
        Dim role As String = If(frmLoginvb.LoggedInRole, "").ToUpperInvariant()
        Return role = "MANAGER" OrElse role = "ADMIN" OrElse role = "ADMINISTRATOR"
    End Function

    Private Sub EnsureDailyOpeningCapitalTable()
        Dim sql As String =
        "CREATE TABLE IF NOT EXISTS DailyOpeningCapital (" &
        "CashDate TEXT PRIMARY KEY, " &
        "OpeningAmount REAL NOT NULL, " &
        "SetByUserID INTEGER NULL, " &
        "SetAt TEXT NOT NULL DEFAULT (datetime('now')))"
        Utilities.ExecuteNonQuery(sql, New SqlParameter() {})
    End Sub

    Private Function TryGetTodayOpeningCapital(ByRef amount As Decimal) As Boolean
        Dim sql As String = "SELECT OpeningAmount FROM DailyOpeningCapital WHERE CashDate = @CashDate"
        Dim result = Utilities.ExecuteScalar(sql, New SqlParameter() {
        New SqlParameter("@CashDate", Date.Today)
    })

        If result Is Nothing OrElse result Is DBNull.Value Then
            amount = 0D
            Return False
        End If

        amount = Convert.ToDecimal(result)
        Return True
    End Function

    Private Sub UpsertTodayOpeningCapital(amount As Decimal)
        Dim sql As String =
        "INSERT INTO DailyOpeningCapital (CashDate, OpeningAmount, SetByUserID, SetAt) " &
        "VALUES (@CashDate, @OpeningAmount, @SetByUserID, datetime('now')) " &
        "ON CONFLICT(CashDate) DO UPDATE SET " &
        "OpeningAmount = @OpeningAmount, SetByUserID = @SetByUserID, SetAt = datetime('now')"

        Utilities.ExecuteNonQuery(sql, New SqlParameter() {
        New SqlParameter("@CashDate", Date.Today),
        New SqlParameter("@OpeningAmount", amount),
        New SqlParameter("@SetByUserID", If(frmLoginvb.LoggedInUserID > 0, CType(frmLoginvb.LoggedInUserID, Object), DBNull.Value))
    })
    End Sub

    Private Sub InitializeCapitalHeaderUI()
        If lblCapitalInfo Is Nothing Then
            lblCapitalInfo = New Label() With {
                .AutoSize = True,
                .Font = New Font("Poppins", 9, FontStyle.Bold),
                .ForeColor = GoldenYellow,
                .BackColor = Color.Transparent,
                .Location = New Point(Me.ClientSize.Width - 20, 12),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
                .Text = "Opening Capital: Not set"
            }
            Me.Controls.Add(lblCapitalInfo)
            lblCapitalInfo.BringToFront()
        End If

        If btnEditCapital Is Nothing Then
            btnEditCapital = New Guna2Button() With {
                .Text = "Set Capital",
                .Size = New Size(110, 30),
                .Location = New Point(Me.ClientSize.Width - 120, 8),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
                .BorderRadius = 8,
                .FillColor = JadeOlive,
                .ForeColor = PureWhite
            }
            AddHandler btnEditCapital.Click, AddressOf BtnEditCapital_Click
            Me.Controls.Add(btnEditCapital)
            btnEditCapital.BringToFront()
        End If

        ' Hide edit button if user is not manager/admin or if there are sales already today
        btnEditCapital.Visible = IsManagerOrAdmin() AndAlso Not HasSalesForToday()
    End Sub
    Private Sub PositionCapitalHeaderCenterTop()
        If lblCapitalInfo Is Nothing OrElse btnEditCapital Is Nothing Then
            Return
        End If

        Dim spacing As Integer = 10
        Dim totalWidth As Integer = lblCapitalInfo.Width + spacing + btnEditCapital.Width
        Dim startX As Integer = (Me.ClientSize.Width - totalWidth) \ 2

        lblCapitalInfo.Location = New Point(startX, 12)

        Dim buttonX As Integer = lblCapitalInfo.Right + spacing
        If posLockedForCapital Then
            buttonX += 40 ' push further right so it partially clips while locked
        End If

        btnEditCapital.Location = New Point(buttonX, 8)
    End Sub

    Private Sub ApplyCapitalLockState(locked As Boolean)
        posLockedForCapital = locked

        CategoryPanel.Enabled = Not locked
        btnPayment.Enabled = Not locked
        confirmBtn.Enabled = Not locked
        btnDiscount.Enabled = Not locked

        If locked Then
            lblCapitalInfo.Text = "Opening Capital: Not set (POS Locked)"
            lblCapitalInfo.ForeColor = AlertRed
        Else
            lblCapitalInfo.ForeColor = GoldenYellow
        End If

        ' Update category/button/overlay colors according to lock state
        ApplyPosLockColors(locked)

        PositionCapitalHeaderCenterTop()
    End Sub
    Private Sub UpdateCapitalHeaderUI()
        lblCapitalInfo.Text = $"Opening Capital ({Date.Today:MM/dd/yyyy}): ₱{currentOpeningCapital:N2}"
        btnEditCapital.Text = If(currentOpeningCapital > 0D, "Edit Capital", "Set Capital")

        ' Do not allow editing if there are sales records for today.
        btnEditCapital.Visible = IsManagerOrAdmin() AndAlso Not HasSalesForToday()
    End Sub


    Private Function ShowSetCapitalDialog(isEdit As Boolean) As Boolean
        If Not IsManagerOrAdmin() Then Return False

        Dim dlg As New Form With {
        .Text = If(isEdit, "Edit Opening Capital", "Set Opening Capital"),
        .Size = New Size(440, 260),
        .StartPosition = FormStartPosition.CenterParent,
        .FormBorderStyle = FormBorderStyle.FixedDialog,
        .MaximizeBox = False,
        .MinimizeBox = False,
        .KeyPreview = True,
        .ShowInTaskbar = False,
        .TopMost = True
    }

        Dim lbl As New Label With {
        .Text = $"Enter opening capital for {Date.Today:MM/dd/yyyy}:",
        .Location = New Point(20, 16),
        .AutoSize = True,
        .Font = New Font("Poppins", 10, FontStyle.Regular),
        .ForeColor = DarkText
    }

        Dim txtCapital As New Guna.UI2.WinForms.Guna2TextBox() With {
        .Location = New Point(20, 48),
        .Size = New Size(380, 40),
        .BorderRadius = 8,
        .FillColor = PureWhite,
        .ForeColor = DarkText,
        .Font = New Font("Poppins", 12, FontStyle.Bold),
        .TextAlign = HorizontalAlignment.Right
    }

        ' Initialize with formatted value (thousands separator, two decimals)
        txtCapital.Text = currentOpeningCapital.ToString("N2", Globalization.CultureInfo.CurrentCulture)

        Dim btnOk As New Guna.UI2.WinForms.Guna2Button() With {
        .Text = "Save",
        .Size = New Size(160, 44),
        .Location = New Point(220, 150),
        .Font = New Font("Poppins", 10, FontStyle.Bold),
        .FillColor = JadeOlive,
        .ForeColor = PureWhite,
        .BorderRadius = 10
    }

        Dim btnCancel As New Guna.UI2.WinForms.Guna2Button() With {
        .Text = "Cancel",
        .Size = New Size(140, 44),
        .Location = New Point(40, 150),
        .Font = New Font("Poppins", 10, FontStyle.Regular),
        .FillColor = AlertRed,
        .ForeColor = PureWhite,
        .BorderRadius = 10
    }

        Dim previewLbl As New Label With {
        .Text = $"Preview: {txtCapital.Text}",
        .Location = New Point(20, 100),
        .AutoSize = True,
        .Font = New Font("Poppins", 10, FontStyle.Regular),
        .ForeColor = DarkText
    }

        ' Helper to parse user input (accepts current-culture decimal separator)
        Dim TryParseInput = Function(input As String, ByRef value As Decimal) As Boolean
                                Dim cleaned As String = System.Text.RegularExpressions.Regex.Replace(input.Trim(), "[^0-9\.\-]", "")
                                Dim decimalSep As String = Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator
                                If decimalSep <> "."c Then cleaned = cleaned.Replace(".", decimalSep)
                                Return Decimal.TryParse(cleaned, Globalization.NumberStyles.AllowDecimalPoint Or Globalization.NumberStyles.AllowLeadingSign, Globalization.CultureInfo.CurrentCulture, value)
                            End Function

        ' Allow digits, control keys and exactly one decimal separator while typing.
        AddHandler txtCapital.KeyPress, Sub(s, e)
                                            If Char.IsControl(e.KeyChar) Then Return

                                            Dim decimalSep As Char = Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator.Chars(0)

                                            ' If user types decimal separator: allow only if not already present
                                            If e.KeyChar = decimalSep Then
                                                If txtCapital.Text.IndexOf(decimalSep) >= 0 Then
                                                    e.Handled = True
                                                    Return
                                                End If

                                                ' If empty, prefix 0 before decimal for clarity
                                                If txtCapital.Text.Length = 0 Then
                                                    txtCapital.Text = "0" & decimalSep
                                                    txtCapital.SelectionStart = txtCapital.Text.Length
                                                    e.Handled = True
                                                    previewLbl.Text = $"Preview: {txtCapital.Text}"
                                                    Return
                                                End If

                                                ' allow the decimal
                                                Return
                                            End If

                                            ' Allow digits only otherwise
                                            If Not Char.IsDigit(e.KeyChar) Then
                                                e.Handled = True
                                                Return
                                            End If

                                            ' All other digits allowed � don't aggressively limit integer length here
                                        End Sub

        ' Update preview with thousands separator and two decimals while user types (non-intrusive)
        Dim handling As Boolean = False
        AddHandler txtCapital.TextChanged, Sub()
                                               If handling Then Return
                                               handling = True
                                               Try
                                                   Dim parsed As Decimal = 0D
                                                   If TryParseInput(txtCapital.Text, parsed) Then
                                                       ' Show formatted preview with thousands separator and two decimals
                                                       previewLbl.Text = $"Preview: {parsed.ToString("N2", Globalization.CultureInfo.CurrentCulture)}"
                                                   Else
                                                       previewLbl.Text = $"Preview: {txtCapital.Text}"
                                                   End If
                                               Finally
                                                   handling = False
                                               End Try
                                           End Sub

        ' Enter = save, Escape = cancel (on textbox)
        AddHandler txtCapital.KeyDown, Sub(s, e)
                                           If e.KeyCode = Keys.Enter Then
                                               e.Handled = True
                                               e.SuppressKeyPress = True
                                               btnOk.PerformClick()
                                           ElseIf e.KeyCode = Keys.Escape Then
                                               e.Handled = True
                                               e.SuppressKeyPress = True
                                               btnCancel.PerformClick()
                                           End If
                                       End Sub

        ' Dialog-level Enter/Esc support
        dlg.KeyPreview = True
        AddHandler dlg.KeyDown, Sub(s, e)
                                    If e.KeyCode = Keys.Enter Then
                                        e.Handled = True
                                        e.SuppressKeyPress = True
                                        btnOk.PerformClick()
                                    ElseIf e.KeyCode = Keys.Escape Then
                                        e.Handled = True
                                        e.SuppressKeyPress = True
                                        btnCancel.PerformClick()
                                    End If
                                End Sub

        ' Save logic: parse input, format with thousands separators and two decimals, persist
        AddHandler btnOk.Click, Sub()
                                    Dim cleaned As String = System.Text.RegularExpressions.Regex.Replace(txtCapital.Text, "[^0-9\.\-]", "")
                                    Dim parsed As Decimal
                                    If Not TryParseInput(cleaned, parsed) Then
                                        MessageBox.Show("Invalid amount. Please enter a valid number.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                        Return
                                    End If

                                    ' Persist rounded to 2 decimals
                                    parsed = Math.Round(parsed, 2, MidpointRounding.AwayFromZero)

                                    UpsertTodayOpeningCapital(parsed)
                                    currentOpeningCapital = parsed
                                    UpdateCapitalHeaderUI()
                                    ApplyCapitalLockState(False)
                                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Opening Capital Set", $"Date={Date.Today:yyyy-MM-dd}, Amount=₱{currentOpeningCapital:N2}")
                                    dlg.DialogResult = DialogResult.OK
                                    dlg.Close()
                                End Sub

        AddHandler btnCancel.Click, Sub()
                                        dlg.DialogResult = DialogResult.Cancel
                                        dlg.Close()
                                    End Sub

        dlg.Controls.Add(lbl)
        dlg.Controls.Add(txtCapital)
        dlg.Controls.Add(previewLbl)
        dlg.Controls.Add(btnOk)
        dlg.Controls.Add(btnCancel)

        ' Format final display on Leave (thousands separators + two decimals)
        AddHandler txtCapital.Leave, Sub()
                                         Dim p As Decimal = 0D
                                         If TryParseInput(txtCapital.Text, p) Then
                                             txtCapital.Text = p.ToString("N2", Globalization.CultureInfo.CurrentCulture)
                                             previewLbl.Text = $"Preview: {txtCapital.Text}"
                                         End If
                                     End Sub

        ' Place caret before decimal when dialog shown
        AddHandler dlg.Shown, Sub()
                                  txtCapital.Focus()
                                  Try
                                      Dim sep = Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator
                                      Dim idx As Integer = txtCapital.Text.IndexOf(sep)
                                      If idx >= 0 Then
                                          txtCapital.SelectionStart = idx
                                      Else
                                          txtCapital.SelectionStart = txtCapital.Text.Length
                                      End If
                                  Catch
                                  End Try
                              End Sub

        Dim result As DialogResult = dlg.ShowDialog(Me)
        dlg.Dispose()
        Return result = DialogResult.OK
    End Function
    Private Sub EnsureCapitalBeforeUsingPOS()
        EnsureDailyOpeningCapitalTable()
        InitializeCapitalHeaderUI()

        Dim found As Boolean = TryGetTodayOpeningCapital(currentOpeningCapital)
        If found Then
            UpdateCapitalHeaderUI()
            ApplyCapitalLockState(False)
            Return
        End If

        If IsManagerOrAdmin() Then
            Dim setOk As Boolean = ShowSetCapitalDialog(False)
            If Not setOk Then
                ApplyCapitalLockState(True)
            End If
        Else
            ApplyCapitalLockState(True)
            MessageBox.Show("POS is locked. Manager/Admin must set opening capital for today.", "Opening Capital Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If
    End Sub

    Private Sub BtnEditCapital_Click(sender As Object, e As EventArgs)
        If Not IsManagerOrAdmin() Then
            MessageBox.Show("Only Manager/Admin can edit opening capital.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Prevent editing if sales for today exist � button should be hidden but double-check.
        If HasSalesForToday() Then
            MessageBox.Show("Opening capital cannot be edited after sales have been recorded for today.", "Action Not Allowed", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            btnEditCapital.Visible = False
            Return
        End If

        ShowSetCapitalDialog(currentOpeningCapital > 0D)
    End Sub

    ' Barcode scanning functionality

    ' Enhanced receipt printing
    ' Helper method for "not found" notifications
    Private Sub ShowBarcodeNotFoundNotification(barcode As String)
        Dim notificationLabel As New Label()
        notificationLabel.Text = $"Product '{barcode}' not found"
        notificationLabel.Font = New Font("Poppins", 12, FontStyle.Bold)
        notificationLabel.ForeColor = PureWhite
        notificationLabel.BackColor = AlertRed
        notificationLabel.AutoSize = True
        notificationLabel.Padding = New Padding(15, 8, 15, 8)
        notificationLabel.TextAlign = ContentAlignment.MiddleCenter

        ' Position at center top
        Me.Controls.Add(notificationLabel)
        notificationLabel.BringToFront()
        Application.DoEvents()

        Dim centerX As Integer = (Me.ClientSize.Width - notificationLabel.Width) / 2
        notificationLabel.Location = New Point(centerX, 20)

        ' Auto-remove after 3 seconds (longer for error messages)
        Dim removeTimer As New Timer() With {.Interval = 3000}
        AddHandler removeTimer.Tick, Sub()
                                         removeTimer.Stop()
                                         If Me.Controls.Contains(notificationLabel) Then
                                             Me.Controls.Remove(notificationLabel)
                                             notificationLabel.Dispose()
                                         End If
                                         removeTimer.Dispose()
                                     End Sub
        removeTimer.Start()
    End Sub

    ' Helper method for error notifications
    Private Sub ShowBarcodeErrorNotification(errorMessage As String)
        Dim notificationLabel As New Label()
        notificationLabel.Text = $"Barcode Error: {errorMessage}"
        notificationLabel.Font = New Font("Poppins", 11, FontStyle.Bold)
        notificationLabel.ForeColor = PureWhite
        notificationLabel.BackColor = AlertRed
        notificationLabel.AutoSize = True
        notificationLabel.Padding = New Padding(15, 8, 15, 8)
        notificationLabel.TextAlign = ContentAlignment.MiddleCenter

        ' Position at center top
        Me.Controls.Add(notificationLabel)
        notificationLabel.BringToFront()
        Application.DoEvents()

        Dim centerX As Integer = (Me.ClientSize.Width - notificationLabel.Width) / 2
        notificationLabel.Location = New Point(centerX, 20)

        ' Auto-remove after 4 seconds (longest for errors)
        Dim removeTimer As New Timer() With {.Interval = 4000}
        AddHandler removeTimer.Tick, Sub()
                                         removeTimer.Stop()
                                         If Me.Controls.Contains(notificationLabel) Then
                                             Me.Controls.Remove(notificationLabel)
                                             notificationLabel.Dispose()
                                         End If
                                         removeTimer.Dispose()
                                     End Sub
        removeTimer.Start()
    End Sub

    Private Sub CreateNavigationMenu()
        NavigationBuilder.Build(DashboardPanel, Me, "Sales")
        Return
        Try
            ' Clear existing controls except PictureBox9 (logo)
            For i = DashboardPanel.Controls.Count - 1 To 0 Step -1
                Dim control As Control = DashboardPanel.Controls(i)
                If TypeOf control IsNot PictureBox Then
                    DashboardPanel.Controls.Remove(control)
                    control.Dispose()
                End If
            Next

            ' Set Navigation Panel Background to the new dark navigation color (61,65,66)
            DashboardPanel.FillColor = System.Drawing.Color.FromArgb(61, 65, 66)

            ' Calculate available space (DashboardPanel is 236x885)
            Dim availableWidth As Integer = DashboardPanel.Width - 40 ' 20px margins on each side
            Dim availableHeight As Integer = DashboardPanel.Height - 160 ' Space for logo and title

            ' Logo area (keep existing PictureBox9)
            PictureBox9.BringToFront()

            ' UPDATED: Get company name from settings
            Dim companyName As String = CompanySettingsManager.Instance.GetSettingString("CompanyName", "JADE CLINIC")

            ' Add title label - positioned below logo with Golden Yellow
            Dim titleLabel As New Label()
            titleLabel.Text = companyName
            titleLabel.Font = New Font("Poppins", 14, FontStyle.Bold)
            titleLabel.ForeColor = GoldenYellow
            titleLabel.BackColor = Color.Transparent
            titleLabel.AutoSize = False
            titleLabel.Size = New Size(availableWidth, 30)
            titleLabel.Location = New Point(20, 110)
            titleLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(titleLabel)

            ' Subtitle with MediumText (visible on dark nav background)
            Dim subtitleLabel As New Label()
            subtitleLabel.Text = "Dental Supply Management"
            subtitleLabel.Font = New Font("Poppins", 10, FontStyle.Regular)
            subtitleLabel.ForeColor = MediumText
            subtitleLabel.BackColor = Color.Transparent
            subtitleLabel.AutoSize = False
            subtitleLabel.Size = New Size(availableWidth, 25)
            subtitleLabel.Location = New Point(20, 145)
            subtitleLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(subtitleLabel)

            ' Navigation section separator with a subtle darker line
            Dim separator1 As New Panel()
            separator1.BackColor = System.Drawing.Color.FromArgb(50, 50, 50)
            separator1.Size = New System.Drawing.Size(availableWidth - 20, 2)
            separator1.Location = New Point(30, 190)
            DashboardPanel.Controls.Add(separator1)

            ' Navigation section label with MediumText (visible on dark background)
            Dim navLabel As New Label()
            navLabel.Text = "NAVIGATION"
            navLabel.Font = New Font("Poppins", 10, FontStyle.Bold)
            navLabel.ForeColor = MediumText
            navLabel.BackColor = Color.Transparent
            navLabel.AutoSize = False
            navLabel.Size = New System.Drawing.Size(availableWidth, 25)
            navLabel.Location = New Point(20, 205)
            navLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(navLabel)

            ' Calculate button positioning for role-based navigation
            Dim startY As Integer = 250
            Dim buttonHeight As Integer = 50
            Dim buttonSpacing As Integer = 15
            Dim buttonWidth As Integer = availableWidth - 5
            Dim buttonIndex As Integer = 0
            ' Logo area (keep existing PictureBox9)
            If PictureBox9 IsNot Nothing Then
                Try
                    ' Render company logo from settings into the existing PictureBox.
                    ' Do NOT change PictureBox size or add click handlers.
                    Dim logoImg As Image = CompanySettingsManager.Instance.GetCompanyLogo()
                    If logoImg IsNot Nothing Then
                        PictureBox9.Image = logoImg
                        PictureBox9.SizeMode = PictureBoxSizeMode.StretchImage
                    End If
                Catch ex As Exception
                    Console.WriteLine($"Unable to set dashboard logo: {ex.Message}")
                End Try

                PictureBox9.BringToFront()
            End If
            ' Get current user role for navigation filtering
            Dim currentRole As String = If(frmLoginvb.LoggedInRole, "Staff").ToUpper()

            ' Create navigation buttons based on role
            ' Dashboard Button (not active)
            If currentRole = "MANAGER" Or currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                Dim navDashboardBtn = CreateLargeNavButton("Dashboard", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navDashboardBtn.Click, AddressOf NavDashboard_Click
                buttonIndex += 1
            End If
            ' POS/Sales Button (ACTIVE - we're on this page)
            Dim navPOSBtn = CreateLargeNavButton("POS / Sales", startY + buttonIndex * (buttonHeight + buttonSpacing), True, buttonWidth, buttonHeight)
            buttonIndex += 1

            ' Manager and Admin only buttons - Inventory moved here
            If currentRole = "MANAGER" Or currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                ' Inventory Button (only for Manager and Admin)
                Dim navInventoryBtn = CreateLargeNavButton("Inventory", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navInventoryBtn.Click, AddressOf NavInventory_Click
                buttonIndex += 1

                ' Sales Records Button
                Dim navSalesRecordsBtn = CreateLargeNavButton("Sales Records", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navSalesRecordsBtn.Click, AddressOf NavSalesRecords_Click
                buttonIndex += 1

                ' Staff Management Button
                Dim navStaffBtn = CreateLargeNavButton("Staff", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navStaffBtn.Click, AddressOf NavStaff_Click
                buttonIndex += 1

                ' Inventory Logs Button
                Dim navInventoryLogBtn = CreateLargeNavButton("Inventory Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navInventoryLogBtn.Click, AddressOf NavInventoryLog_Click
                buttonIndex += 1

                ' Suppliers (place above Audit Logs)
                Dim navSuppliersBtn = CreateLargeNavButton("Suppliers", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navSuppliersBtn.Click, AddressOf NavSuppliers_Click
                buttonIndex += 1
            End If

            ' Admin only buttons
            If currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                ' Audit Logs Button
                Dim navAuditLogBtn = CreateLargeNavButton("Audit Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navAuditLogBtn.Click, AddressOf NavAuditLog_Click
                buttonIndex += 1

                ' System Settings Button
                Dim systemSettingsBtn = CreateLargeNavButton("System", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler systemSettingsBtn.Click, AddressOf NavSystemSettings_Click
                buttonIndex += 1
            End If

        Catch ex As Exception
            Console.WriteLine($"Error creating navigation menu: {ex.Message}")
        End Try
    End Sub
    Private Sub NavSuppliers_Click(sender As Object, e As EventArgs)
        PersistCartState()

        Try
            isNavigating = True
            Supplier.Show()
            Me.Close()
        Catch ex As Exception
            MessageBox.Show($"Unable to open Suppliers: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    Private Function CreateLargeNavButton(text As String, yPosition As Integer, isActive As Boolean, buttonWidth As Integer, buttonHeight As Integer) As Guna.UI2.WinForms.Guna2Button
        Dim btn As New Guna.UI2.WinForms.Guna2Button()

        ' Button properties with improved sizing and new color scheme for dark navigation
        btn.Text = text
        btn.Size = New System.Drawing.Size(buttonWidth, buttonHeight)
        btn.Location = New Point(20, yPosition)
        btn.BorderRadius = 12
        btn.Font = New Font("Poppins", 10, FontStyle.Regular)
        btn.TextAlign = HorizontalAlignment.Left

        ' Apply color scheme for dark navigation panel (idle = transparent, text = white)
        btn.FillColor = If(isActive, GoldenYellow, System.Drawing.Color.Transparent) ' Golden for active
        btn.ForeColor = If(isActive, DarkText, PureWhite) ' Dark text on active gold, white on dark background when inactive
        btn.BorderThickness = If(isActive, 0, 1)
        btn.BorderColor = If(isActive, System.Drawing.Color.Transparent, System.Drawing.Color.FromArgb(80, 80, 80)) ' subtle border on dark bg
        btn.BackColor = System.Drawing.Color.Transparent
        btn.Cursor = Cursors.Hand

        ' Add subtle shadow for depth (tuned for dark nav)
        btn.ShadowDecoration.Enabled = True
        btn.ShadowDecoration.Color = System.Drawing.Color.FromArgb(30, 30, 30)
        btn.ShadowDecoration.Depth = 4
        btn.ShadowDecoration.Shadow = New Padding(0, 1, 4, 4)

        ' Improved hover effects for dark navigation
        AddHandler btn.MouseEnter, Sub()
                                       If Not isActive Then
                                           btn.FillColor = System.Drawing.Color.FromArgb(48, 52, 54) ' slightly lighter than nav bg
                                           btn.BorderColor = GoldenYellow
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

        ' Add to panel
        DashboardPanel.Controls.Add(btn)

        Return btn
    End Function

    ' Add the System Settings navigation handler
    Private Sub NavSystemSettings_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Sys.Show()
        Me.Close()
    End Sub

    ' UPDATED: Enhanced receipt printing using company settings
    ' UPDATED: Enhanced receipt printing using company settings
    ' UPDATED: Enhanced receipt printing with cleaner formatting (no pipe separators)
    ' Modified: OnPrintPage - remove duplicate VATABLE SALES and show which item received the discount
    ' Replace OnPrintPage with this updated version that displays subtotal, discount, VATable sales, VAT and Total consistently
    Private Sub OnPrintPage(sender As Object, e As PrintPageEventArgs)
        Try
            ' Auto-fit: render once to measure, then scale the whole receipt down
            ' so the footer is never cut off, even with many line items.
            Dim marginBounds As New Rectangle(10, 10, e.MarginBounds.Width - 20, e.MarginBounds.Height - 20)
            Dim fit As Single = 1.0F
            Using measureG = Graphics.FromImage(New Bitmap(1, 1))
                Dim usedH As Single = DrawSalesReceipt(measureG, marginBounds)
                If usedH > CSng(marginBounds.Height) Then
                    fit = CSng(marginBounds.Height) / usedH
                    If fit < 0.5F Then fit = 0.5F
                End If
            End Using
            If fit < 1.0F Then
                e.Graphics.ScaleTransform(fit, fit)
            End If
            DrawSalesReceipt(e.Graphics, marginBounds)
        Catch ex As Exception
            Console.WriteLine($"Print error: {ex.Message}")
        End Try
    End Sub

    Private Function DrawSalesReceipt(g As Graphics, marginBounds As Rectangle) As Single
        Try
            ' Scale fonts down for narrow thermal rolls (e.g. 58mm) so the
            ' receipt does not clip horizontally. No change on 80mm or wider.
            Dim scale As Single = CSng(marginBounds.Width / 280.0F)
            scale = Math.Min(1.0F, Math.Max(0.55F, scale))
            Dim regularFont As New Font("Arial", 8.0F * scale)
            Dim boldFont As New Font("Arial", 10.0F * scale, FontStyle.Bold)
            Dim headerFont As New Font("Arial", 12.0F * scale, FontStyle.Bold)
            Dim sectionHeaderFont As New Font("Arial", 9.0F * scale, FontStyle.Bold)
            Dim brush As New SolidBrush(Color.Black)
            Dim pen As New Pen(Color.Black, 1.0F)
            Dim yPosition As Integer = 10
            Dim marginLeft As Integer = 10
            Dim contentWidth As Integer = marginBounds.Width - (marginLeft * 2)
            Dim centerX As Integer = marginBounds.Width \ 2
            Dim colGap As Integer = 20
            Dim colWidth As Integer = (contentWidth - colGap) \ 2
            Dim leftColX As Integer = marginLeft
            Dim rightColX As Integer = marginLeft + colWidth + colGap

            ' Separator line: a long run of "=" that spans the content width.
            Dim separator As String = New String("="c, Math.Max(45, CInt(contentWidth / g.MeasureString("=", regularFont).Width)))

            ' Company header
            Dim companyName As String = CompanySettingsManager.Instance.GetSettingString("CompanyName", "JADE CLINIC")
            Dim companyPhone As String = CompanySettingsManager.Instance.GetSettingString("Phone", "(02) 8123-4567")
            Dim companyAddress As String = CompanySettingsManager.Instance.GetSettingString("Address", "")
            Dim companyWebsite As String = CompanySettingsManager.Instance.GetSettingString("Website", "")
            Dim companyTIN As String = CompanySettingsManager.Instance.GetSettingString("TIN", "123-456-789-000")
            Dim birAuthNumber As String = CompanySettingsManager.Instance.GetSettingString("BIRAuthNumber", "ATP-2024-000001")
            Dim ptuNumber As String = CompanySettingsManager.Instance.GetSettingString("PTUNumber", "PTU-2024-001")
            Dim footerMessage As String = CompanySettingsManager.Instance.GetSettingString("ReceiptFooter", "Thank you for your business!" & vbCrLf & "Have a great day!")

            g.DrawString(companyName, headerFont, brush, CSng(centerX - (g.MeasureString(companyName, headerFont).Width / 2)), CSng(yPosition))
            yPosition += 24
            g.DrawString("Dental Supply Management", regularFont, brush, CSng(centerX - (g.MeasureString("Dental Supply Management", regularFont).Width / 2)), CSng(yPosition))
            yPosition += 14

            If Not String.IsNullOrEmpty(companyTIN) Then
                g.DrawString($"TIN: {companyTIN} (VAT Registered)", regularFont, brush, CSng(centerX - (g.MeasureString($"TIN: {companyTIN} (VAT Registered)", regularFont).Width / 2)), CSng(yPosition))
                yPosition += 14
            End If

            If Not String.IsNullOrEmpty(companyPhone) Then
                g.DrawString($"Tel: {companyPhone}", regularFont, brush, CSng(centerX - (g.MeasureString($"Tel: {companyPhone}", regularFont).Width / 2)), CSng(yPosition))
                yPosition += 14
            End If

            If Not String.IsNullOrEmpty(companyAddress) Then
                g.DrawString(companyAddress, regularFont, brush, CSng(centerX - (g.MeasureString(companyAddress, regularFont).Width / 2)), CSng(yPosition))
                yPosition += 14
            End If

            If Not String.IsNullOrEmpty(companyWebsite) Then
                g.DrawString(companyWebsite, regularFont, brush, CSng(centerX - (g.MeasureString(companyWebsite, regularFont).Width / 2)), CSng(yPosition))
                yPosition += 14
            End If

            g.DrawString(separator, regularFont, brush, marginLeft, yPosition)
            yPosition += 16

            ' Document title and metadata
            g.DrawString("SALES INVOICE", boldFont, brush, CSng(centerX - (g.MeasureString("SALES INVOICE", boldFont).Width / 2)), CSng(yPosition))
            yPosition += 22
            g.DrawString($"Receipt #: {receiptOrderId}", regularFont, brush, marginLeft, yPosition)
            yPosition += 12
            g.DrawString($"Date: {DateTime.Now:MM/dd/yyyy HH:mm:ss}", regularFont, brush, marginLeft, yPosition)
            yPosition += 12
            g.DrawString($"Cashier: {frmLoginvb.LoggedInUsername}", regularFont, brush, marginLeft, yPosition)
            yPosition += 14

            ' --- CUSTOMER BLOCK (2x2 layout) ---
            g.DrawString("Customer Details:", regularFont, brush, marginLeft, yPosition)
            yPosition += 12

            Dim printedName As String = If(Not String.IsNullOrWhiteSpace(receiptCustomerName), receiptCustomerName, If(Not String.IsNullOrWhiteSpace(selectedCustomerName), selectedCustomerName, "________________"))
            Dim printedTIN As String = If(Not String.IsNullOrWhiteSpace(selectedCustomerTIN), selectedCustomerTIN, "________________")
            Dim printedPhone As String = If(Not String.IsNullOrWhiteSpace(selectedCustomerPhone), selectedCustomerPhone, "________________")
            Dim printedEmail As String = If(Not String.IsNullOrWhiteSpace(selectedCustomerEmail), selectedCustomerEmail, "________________")

            ' Row 1: Name (left) | TIN (right)
            g.DrawString($"Name: {printedName}", regularFont, brush, leftColX, yPosition)
            g.DrawString($"TIN: {printedTIN}", regularFont, brush, rightColX, yPosition)
            yPosition += 12

            ' Row 2: Phone (left) | Email (right)
            g.DrawString($"Phone: {printedPhone}", regularFont, brush, leftColX, yPosition)
            g.DrawString($"Email: {printedEmail}", regularFont, brush, rightColX, yPosition)
            yPosition += 14

            g.DrawString(separator, regularFont, brush, marginLeft, yPosition)
            yPosition += 14

            ' --- ITEMS (VAT-INCLUSIVE unit prices, discount applied per logic) ---
            If receiptItems IsNot Nothing Then
                For Each item In receiptItems
                    Dim itemName As String = item("ProductName").ToString()
                    Dim quantity As Integer = CInt(item("Quantity"))

                    ' Determine unit VAT-inclusive price
                    Dim unitVatInc As Decimal = Convert.ToDecimal(If(item.ContainsKey("OriginalUnitPrice"), item("OriginalUnitPrice"), item("Price")))

                    ' If this item was discounted, compute discounted unit price
                    If discountType <> "None" AndAlso discountedItemProductId IsNot Nothing AndAlso item.ContainsKey("ProductID") Then
                        Try
                            Dim itemPid As Integer = Convert.ToInt32(item("ProductID"))
                            If itemPid = discountedItemProductId Then
                                If discountType = "Percentage" Then
                                    Dim pct As Decimal = discountValue
                                    unitVatInc = Math.Round((unitVatInc / 1.12D) * (1 - (pct / 100D)) * 1.12D, 2)
                                ElseIf discountType = "Fixed" Then
                                    Dim perUnitDiscountVatInc As Decimal = 0D
                                    If quantity > 0 Then perUnitDiscountVatInc = discountAmount / quantity
                                    Dim perUnitDiscountNet As Decimal = perUnitDiscountVatInc / 1.12D
                                    unitVatInc = Math.Round(((unitVatInc / 1.12D) - perUnitDiscountNet) * 1.12D, 2)
                                    If unitVatInc < 0D Then unitVatInc = 0D
                                End If
                            End If
                        Catch
                            ' ignore conversion issues
                        End Try
                    End If

                    Dim lineTotal As Decimal = Math.Round(unitVatInc * quantity, 2)

                    g.DrawString($"{quantity}x {itemName}", regularFont, brush, marginLeft, yPosition)
                    yPosition += 12
                    g.DrawString($"@ ₱{unitVatInc:F2}", regularFont, brush, marginLeft + 8, yPosition)
                    g.DrawString($"₱{lineTotal:F2}", regularFont, brush, CSng(marginBounds.Right - g.MeasureString($"₱{lineTotal:F2}", regularFont).Width), CSng(yPosition))
                    ' Padding between line items so the list reads more cleanly.
                    yPosition += 15
                    yPosition += 4
                Next
            End If

            g.DrawString(separator, regularFont, brush, marginLeft, yPosition)
            yPosition += 14

            ' --- VAT / TOTAL CALCULATION (consistent with RefreshOrderDisplay) ---
            Dim preDiscountVatInclusive As Decimal = Me.subtotalVatInclusive
            If preDiscountVatInclusive = 0D Then
                preDiscountVatInclusive = 0D
                If receiptItems IsNot Nothing Then
                    For Each it In receiptItems
                        Dim unitVatInc As Decimal = Convert.ToDecimal(If(it.ContainsKey("OriginalUnitPrice"), it("OriginalUnitPrice"), it("Price")))
                        preDiscountVatInclusive += unitVatInc * CInt(it("Quantity"))
                    Next
                End If
                preDiscountVatInclusive = Math.Round(preDiscountVatInclusive, 2)
            End If

            Dim discountVatInclusive As Decimal = discountAmount
            Dim remainingVatInclusive As Decimal = Math.Max(0D, preDiscountVatInclusive - discountVatInclusive)
            Dim vatAmt As Decimal = Math.Round(remainingVatInclusive * (0.12D / 1.12D), 2) ' VAT portion extracted from VAT-inclusive remainder
            Dim vatableNet As Decimal = Math.Round(remainingVatInclusive - vatAmt, 2)
            Dim totalDue As Decimal = Math.Round(remainingVatInclusive, 2)

            ' Print breakdown using clear labels
            g.DrawString("SUBTOTAL (VAT-INC):", regularFont, brush, marginLeft, yPosition)
            g.DrawString($"₱{preDiscountVatInclusive:F2}", regularFont, brush, CSng(marginBounds.Right - g.MeasureString($"₱{preDiscountVatInclusive:F2}", regularFont).Width), CSng(yPosition))
            yPosition += 12

            If discountVatInclusive > 0D Then
                Dim discountLabel As String = $"Less: Discount ({discountType})"
                If Not String.IsNullOrEmpty(discountedItemName) Then discountLabel &= $" on {discountedItemName}"
                g.DrawString(discountLabel & ":", regularFont, brush, marginLeft, yPosition)
                g.DrawString($"-₱{discountVatInclusive:F2}", regularFont, brush, CSng(marginBounds.Right - g.MeasureString($"-₱{discountVatInclusive:F2}", regularFont).Width), CSng(yPosition))
                yPosition += 12
            End If

            g.DrawString("VATABLE SALES (NET):", regularFont, brush, marginLeft, yPosition)
            g.DrawString($"₱{vatableNet:F2}", regularFont, brush, CSng(marginBounds.Right - g.MeasureString($"₱{vatableNet:F2}", regularFont).Width), CSng(yPosition))
            yPosition += 12

            g.DrawString("VAT (12%):", regularFont, brush, marginLeft, yPosition)
            g.DrawString($"₱{vatAmt:F2}", regularFont, brush, CSng(marginBounds.Right - g.MeasureString($"₱{vatAmt:F2}", regularFont).Width), CSng(yPosition))
            yPosition += 12

            g.DrawString(separator, regularFont, brush, marginLeft, yPosition)
            yPosition += 12

            g.DrawString("TOTAL AMOUNT DUE:", boldFont, brush, marginLeft, yPosition)
            g.DrawString($"₱{totalDue:F2}", boldFont, brush, CSng(marginBounds.Right - g.MeasureString($"₱{totalDue:F2}", boldFont).Width), CSng(yPosition))
            yPosition += 18

            ' Payment info
            g.DrawString("PAYMENT INFORMATION", sectionHeaderFont, brush, marginLeft, yPosition)
            yPosition += 14
            g.DrawString($"Payment Method: {selectedPaymentMethod}", regularFont, brush, marginLeft, yPosition)
            yPosition += 12
            If Not String.IsNullOrEmpty(paymentReference) Then
                g.DrawString($"Reference: {paymentReference}", regularFont, brush, marginLeft, yPosition)
                yPosition += 12
            End If
            g.DrawString($"Amount Received: ₱{receiptAmountReceived:F2}", regularFont, brush, marginLeft, yPosition)
            yPosition += 12
            g.DrawString($"Change: ₱{receiptChange:F2}", regularFont, brush, marginLeft, yPosition)
            yPosition += 14

            g.DrawString(separator, regularFont, brush, marginLeft, yPosition)
            yPosition += 12

            ' BIR and footer
            g.DrawString($"BIR Authority to Print No.: {birAuthNumber}", regularFont, brush, marginLeft, yPosition)
            yPosition += 12
            g.DrawString($"PTU No.: {ptuNumber}", regularFont, brush, marginLeft, yPosition)
            yPosition += 12

            g.DrawString(separator, regularFont, brush, marginLeft, yPosition)
            yPosition += 12
            Dim footerLines() As String = footerMessage.Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            For Each line As String In footerLines
                g.DrawString(line, regularFont, brush, CSng(centerX - (g.MeasureString(line, regularFont).Width / 2)), CSng(yPosition))
                yPosition += 12
            Next

            pen.Dispose()
            Return yPosition
        Catch ex As Exception
            Console.WriteLine($"Print error: {ex.Message}")
            Return 0
        End Try
    End Function
    Private Sub PrintReceipt()
        Try
            ' ESC/POS path for thermal/receipt printers: the preview renders the
            ' exact thermal layout, and its Print button sends the raw stream.
            ' If anything fails here we fall through to the GDI preview.
            Dim thermalName As String = FindReceiptPrinterName()
            If Not String.IsNullOrEmpty(thermalName) Then
                Try
                    Dim escLines As List(Of EscPosPrinter.EscLine) = BuildReceiptLinesEscPos()
                    Dim previewData As ReceiptData = BuildReceiptData()
                    Using dlg As New EscPosPreviewForm(thermalName, escLines, previewData)
                        dlg.ShowDialog(Me)
                    End Using
                    Return
                Catch escEx As Exception
                    Console.WriteLine($"ESC/POS preview error: {escEx.Message}")
                End Try
            End If

            Dim companyName As String = CompanySettingsManager.Instance.GetSettingString("CompanyName", "JADE CLINIC")

            Dim printDoc As New PrintDocument()
            ' Send the receipt to a thermal/receipt printer if one is installed
            ' (otherwise it falls back to the Windows default printer).
            Try
                If Not String.IsNullOrEmpty(thermalName) Then
                    Dim sel As New PrinterSettings()
                    sel.PrinterName = thermalName
                    If sel.IsValid Then printDoc.PrinterSettings = sel
                End If
            Catch prEx As Exception
                Console.WriteLine($"Receipt printer selection: {prEx.Message}")
            End Try

            ' A hard-coded custom PaperSize (e.g. 300x700) is silently rejected
            ' by many printer drivers - the job is sent but nothing comes out.
            ' Use a paper size the selected printer actually supports instead:
            ' prefer a receipt/thermal roll, otherwise the printer's default.
            Try
                Dim chosen As PaperSize = FindReceiptPaperSize(printDoc.PrinterSettings)
                If chosen Is Nothing Then chosen = printDoc.PrinterSettings.DefaultPageSettings.PaperSize
                printDoc.DefaultPageSettings.PaperSize = chosen
            Catch paperEx As Exception
                Console.WriteLine($"Receipt paper size fallback: {paperEx.Message}")
            End Try
            printDoc.DefaultPageSettings.Margins = New Margins(10, 10, 10, 10)

            AddHandler printDoc.PrintPage, AddressOf OnPrintPage

            Dim printPreview As New PrintPreviewDialog()
            printPreview.Document = printDoc
            printPreview.Text = $"Receipt Preview - {companyName}"
            printPreview.ShowInTaskbar = False
            printPreview.StartPosition = FormStartPosition.CenterParent
            printPreview.ShowDialog(Me)
        Catch ex As Exception
            MessageBox.Show($"Error printing receipt: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' Build the ESC/POS receipt lines (58mm / 384-dot, 32 chars wide on Font A,
    ' 42 on Font B). Mirrors the GDI layout in OnPrintPage.
    Private Function BuildReceiptLinesEscPos() As List(Of EscPosPrinter.EscLine)
        Dim lines As New List(Of EscPosPrinter.EscLine)()
        Dim cm As CompanySettingsManager = CompanySettingsManager.Instance
        Dim sep32 As String = New String("="c, 32)

        ' Company header
        Dim companyName As String = cm.GetSettingString("CompanyName", "JADE CLINIC")
        Dim companyPhone As String = cm.GetSettingString("Phone", "(02) 8123-4567")
        Dim companyAddress As String = cm.GetSettingString("Address", "")
        Dim companyWebsite As String = cm.GetSettingString("Website", "")
        Dim companyTIN As String = cm.GetSettingString("TIN", "123-456-789-000")
        Dim birAuthNumber As String = cm.GetSettingString("BIRAuthNumber", "ATP-2024-000001")
        Dim ptuNumber As String = cm.GetSettingString("PTUNumber", "PTU-2024-001")
        Dim footerMessage As String = cm.GetSettingString("ReceiptFooter", "Thank you for your business!" & vbCrLf & "Have a great day!")

        lines.Add(New EscPosPrinter.EscLine(companyName, 1, True, False, True))
        lines.Add(New EscPosPrinter.EscLine("Dental Supply Management", 1))
        If Not String.IsNullOrEmpty(companyTIN) Then
            lines.Add(New EscPosPrinter.EscLine("TIN: " & companyTIN & " (VAT Reg)", 1))
        End If
        If Not String.IsNullOrEmpty(companyPhone) Then
            lines.Add(New EscPosPrinter.EscLine("Tel: " & companyPhone, 1))
        End If
        If Not String.IsNullOrEmpty(companyAddress) Then
            lines.Add(New EscPosPrinter.EscLine(companyAddress, 1))
        End If
        If Not String.IsNullOrEmpty(companyWebsite) Then
            lines.Add(New EscPosPrinter.EscLine(companyWebsite, 1))
        End If
        lines.Add(New EscPosPrinter.EscLine(sep32, 0))

        ' Document title and metadata
        lines.Add(New EscPosPrinter.EscLine("SALES INVOICE", 1, True))
        lines.Add(New EscPosPrinter.EscLine("Receipt #: " & receiptOrderId, 0))
        lines.Add(New EscPosPrinter.EscLine("Date: " & DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"), 0))
        lines.Add(New EscPosPrinter.EscLine("Cashier: " & frmLoginvb.LoggedInUsername, 0))
        lines.Add(New EscPosPrinter.EscLine("", 0))

        ' Customer block
        lines.Add(New EscPosPrinter.EscLine("Customer Details:", 0, True))
        Dim printedName As String = If(Not String.IsNullOrWhiteSpace(receiptCustomerName), receiptCustomerName, If(Not String.IsNullOrWhiteSpace(selectedCustomerName), selectedCustomerName, "________________"))
        Dim printedTIN As String = If(Not String.IsNullOrWhiteSpace(selectedCustomerTIN), selectedCustomerTIN, "________________")
        Dim printedPhone As String = If(Not String.IsNullOrWhiteSpace(selectedCustomerPhone), selectedCustomerPhone, "________________")
        Dim printedEmail As String = If(Not String.IsNullOrWhiteSpace(selectedCustomerEmail), selectedCustomerEmail, "________________")
        lines.Add(New EscPosPrinter.EscLine("Name: " & printedName, 0))
        lines.Add(New EscPosPrinter.EscLine("TIN: " & printedTIN, 0))
        lines.Add(New EscPosPrinter.EscLine("Phone: " & printedPhone, 0))
        lines.Add(New EscPosPrinter.EscLine("Email: " & printedEmail, 0))
        lines.Add(New EscPosPrinter.EscLine(sep32, 0))

        ' Items (small font B = 42 chars)
        If receiptItems IsNot Nothing Then
            For Each item In receiptItems
                Dim itemName As String = item("ProductName").ToString()
                Dim quantity As Integer = CInt(item("Quantity"))
                Dim unitVatInc As Decimal = Convert.ToDecimal(If(item.ContainsKey("OriginalUnitPrice"), item("OriginalUnitPrice"), item("Price")))

                If discountType <> "None" AndAlso discountedItemProductId IsNot Nothing AndAlso item.ContainsKey("ProductID") Then
                    Try
                        Dim itemPid As Integer = Convert.ToInt32(item("ProductID"))
                        If itemPid = discountedItemProductId Then
                            If discountType = "Percentage" Then
                                Dim pct As Decimal = discountValue
                                unitVatInc = Math.Round((unitVatInc / 1.12D) * (1 - (pct / 100D)) * 1.12D, 2)
                            ElseIf discountType = "Fixed" Then
                                Dim perUnitDiscountVatInc As Decimal = 0D
                                If quantity > 0 Then perUnitDiscountVatInc = discountAmount / quantity
                                Dim perUnitDiscountNet As Decimal = perUnitDiscountVatInc / 1.12D
                                unitVatInc = Math.Round(((unitVatInc / 1.12D) - perUnitDiscountNet) * 1.12D, 2)
                                If unitVatInc < 0D Then unitVatInc = 0D
                            End If
                        End If
                    Catch
                    End Try
                End If

                Dim lineTotal As Decimal = Math.Round(unitVatInc * quantity, 2)
                Dim qtyName As String = $"{quantity}x {itemName}"
                Dim nameW As Integer = 42 - 12
                If qtyName.Length > nameW Then qtyName = qtyName.Substring(0, nameW)
                lines.Add(New EscPosPrinter.EscLine(qtyName.PadRight(nameW) & FormatRight($"P{unitVatInc:F2} @", 12), 0, False, True))
                lines.Add(New EscPosPrinter.EscLine("".PadRight(42 - 12) & FormatRight($"P{lineTotal:F2}", 12), 0, False, True))
            Next
        End If
        lines.Add(New EscPosPrinter.EscLine(sep32, 0))

        ' VAT / totals (same math as OnPrintPage)
        Dim preDiscountVatInclusive As Decimal = Me.subtotalVatInclusive
        If preDiscountVatInclusive = 0D Then
            If receiptItems IsNot Nothing Then
                For Each it In receiptItems
                    Dim unitVatInc As Decimal = Convert.ToDecimal(If(it.ContainsKey("OriginalUnitPrice"), it("OriginalUnitPrice"), it("Price")))
                    preDiscountVatInclusive += unitVatInc * CInt(it("Quantity"))
                Next
            End If
            preDiscountVatInclusive = Math.Round(preDiscountVatInclusive, 2)
        End If

        Dim discountVatInclusive As Decimal = discountAmount
        Dim remainingVatInclusive As Decimal = Math.Max(0D, preDiscountVatInclusive - discountVatInclusive)
        Dim vatAmt As Decimal = Math.Round(remainingVatInclusive * (0.12D / 1.12D), 2)
        Dim vatableNet As Decimal = Math.Round(remainingVatInclusive - vatAmt, 2)
        Dim totalDue As Decimal = Math.Round(remainingVatInclusive, 2)

        lines.Add(New EscPosPrinter.EscLine(Row("SUBTOTAL (VAT-INC):", $"P{preDiscountVatInclusive:F2}"), 0))
        If discountVatInclusive > 0D Then
            Dim discountLabel As String = "Less: Discount (" & discountType & ")"
            If Not String.IsNullOrEmpty(discountedItemName) Then discountLabel &= " on " & discountedItemName
            lines.Add(New EscPosPrinter.EscLine(Row(discountLabel & ":", $"-P{discountVatInclusive:F2}"), 0))
        End If
        lines.Add(New EscPosPrinter.EscLine(Row("VATABLE SALES (NET):", $"P{vatableNet:F2}"), 0))
        lines.Add(New EscPosPrinter.EscLine(Row("VAT (12%):", $"P{vatAmt:F2}"), 0))
        lines.Add(New EscPosPrinter.EscLine(sep32, 0))
        lines.Add(New EscPosPrinter.EscLine(Row("TOTAL AMOUNT DUE:", $"P{totalDue:F2}"), 0, True, False, True))

        lines.Add(New EscPosPrinter.EscLine("", 0))
        lines.Add(New EscPosPrinter.EscLine("PAYMENT INFORMATION", 0, True))
        lines.Add(New EscPosPrinter.EscLine("Payment Method: " & selectedPaymentMethod, 0))
        If Not String.IsNullOrEmpty(paymentReference) Then
            lines.Add(New EscPosPrinter.EscLine("Reference: " & paymentReference, 0))
        End If
        lines.Add(New EscPosPrinter.EscLine(Row("Amount Received:", $"P{receiptAmountReceived:F2}"), 0))
        lines.Add(New EscPosPrinter.EscLine(Row("Change:", $"P{receiptChange:F2}"), 0))
        lines.Add(New EscPosPrinter.EscLine(sep32, 0))

        lines.Add(New EscPosPrinter.EscLine("BIR ATP No.: " & birAuthNumber, 0))
        lines.Add(New EscPosPrinter.EscLine("PTU No.: " & ptuNumber, 0))
        lines.Add(New EscPosPrinter.EscLine(sep32, 0))

        Dim footerLines() As String = footerMessage.Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        For Each line As String In footerLines
            lines.Add(New EscPosPrinter.EscLine(line, 1))
        Next

        Return lines
    End Function

    ' Build the GDI preview data for the Sales form, mirroring the ESC/POS
    ' math in BuildReceiptLinesEscPos so the preview matches the printed
    ' receipt (same discount/VAT/totals).
    Private Function BuildReceiptData() As ReceiptData
        Dim data As New ReceiptData()
        data.ReceiptNumber = receiptOrderId
        data.SaleDate = DateTime.Now
        data.Cashier = frmLoginvb.LoggedInUsername

        data.CustomerName = If(Not String.IsNullOrWhiteSpace(receiptCustomerName), receiptCustomerName, If(Not String.IsNullOrWhiteSpace(selectedCustomerName), selectedCustomerName, "________________"))
        data.CustomerTIN = If(Not String.IsNullOrWhiteSpace(selectedCustomerTIN), selectedCustomerTIN, "________________")
        data.CustomerPhone = If(Not String.IsNullOrWhiteSpace(selectedCustomerPhone), selectedCustomerPhone, "________________")
        data.CustomerEmail = If(Not String.IsNullOrWhiteSpace(selectedCustomerEmail), selectedCustomerEmail, "________________")

        Dim preDiscountVatInclusive As Decimal = Me.subtotalVatInclusive
        If preDiscountVatInclusive = 0D AndAlso receiptItems IsNot Nothing Then
            For Each it In receiptItems
                Dim unitVatInc As Decimal = Convert.ToDecimal(If(it.ContainsKey("OriginalUnitPrice"), it("OriginalUnitPrice"), it("Price")))
                preDiscountVatInclusive += unitVatInc * CInt(it("Quantity"))
            Next
            preDiscountVatInclusive = Math.Round(preDiscountVatInclusive, 2)
        End If
        data.SubtotalVatInclusive = Math.Round(preDiscountVatInclusive, 2)

        If receiptItems IsNot Nothing Then
            For Each item In receiptItems
                Dim itemName As String = item("ProductName").ToString()
                Dim quantity As Integer = CInt(item("Quantity"))
                Dim unitVatInc As Decimal = Convert.ToDecimal(If(item.ContainsKey("OriginalUnitPrice"), item("OriginalUnitPrice"), item("Price")))
                Dim lineTotal As Decimal = Math.Round(unitVatInc * quantity, 2)
                data.Items.Add(New ReceiptLineItem() With {
                    .ProductName = itemName,
                    .Quantity = quantity,
                    .UnitVatInc = unitVatInc,
                    .LineTotal = lineTotal
                })
            Next
        End If

        data.DiscountAmount = discountAmount
        data.DiscountType = discountType
        data.DiscountedItemName = discountedItemName

        Dim discountVatInclusive As Decimal = discountAmount
        Dim remainingVatInclusive As Decimal = Math.Max(0D, data.SubtotalVatInclusive - discountVatInclusive)
        data.VatAmount = Math.Round(remainingVatInclusive * (0.12D / 1.12D), 2)
        data.VatableNet = Math.Round(remainingVatInclusive - data.VatAmount, 2)
        data.TotalDue = Math.Round(remainingVatInclusive, 2)

        data.PaymentMethod = selectedPaymentMethod
        data.PaymentReference = paymentReference
        data.AmountReceived = receiptAmountReceived
        data.Change = receiptChange

        Return data
    End Function

    ' Label + amount on one line, amount right-aligned at column 32 (Font A).
    Private Function Row(label As String, amount As String) As String
        Dim w As Integer = 32
        If label.Length > w - amount.Length - 1 Then label = label.Substring(0, Math.Max(0, w - amount.Length - 1))
        Return label.PadRight(w - amount.Length) & amount
    End Function

    ' Right-align text within the given width (used with Font B item rows).
    Private Function FormatRight(text As String, width As Integer) As String
        If text.Length > width Then text = text.Substring(0, width)
        Return text.PadLeft(width)
    End Function

    ' Returns the name of an installed thermal/receipt printer (matched by
    ' driver/name keywords). Returns Nothing if none is clearly identifiable,
    ' letting the print go to the Windows default printer instead.
    Private Function FindReceiptPrinterName() As String
        Try
            For Each p As String In PrinterSettings.InstalledPrinters
                Dim n As String = p.ToLowerInvariant()
                If n.Contains("thermal") OrElse n.Contains("receipt") OrElse n.Contains("reciept") OrElse
                   n.Contains("escpos") OrElse n.Contains("esc/pos") OrElse n.Contains("esc pos") OrElse
                   n.Contains("star") OrElse n.Contains("tm-") OrElse n.Contains("xp-") OrElse
                   n.Contains("kp-") OrElse n.Contains("58mm") OrElse n.Contains("80mm") OrElse
                   n.Contains("58.") OrElse n.Contains("e-z") OrElse n.Contains("ez10") OrElse
                   n.Contains("z10") OrElse n.Contains("pos printer") OrElse n.Contains("pos-") OrElse
                   n.Contains("gprinter") OrElse n.Contains("huasheng") Then
                    Return p
                End If
            Next
        Catch ex As Exception
            Return Nothing
        End Try
        Return Nothing
    End Function

    ' Picks a printer-supported paper size that fits a thermal receipt roll
    ' (58mm or 80mm). Prefers papers whose name suggests a receipt/thermal roll;
    ' otherwise the supported paper whose width is closest to the receipt width.
    ' Returns Nothing so the caller can fall back to the printer's default size.
    Private Function FindReceiptPaperSize(settings As PrinterSettings) As PaperSize
        Try
            For Each ps As PaperSize In settings.PaperSizes
                Dim n As String = ps.PaperName.ToLowerInvariant()
                If n.Contains("receipt") OrElse n.Contains("thermal") OrElse
                   n.Contains("80mm") OrElse n.Contains("80 mm") OrElse n.Contains("80x80") OrElse
                   n.Contains("58mm") OrElse n.Contains("58 mm") OrElse n.Contains("57mm") OrElse
                   n.Contains("57 mm") OrElse n.Contains("continuous") OrElse
                   n.Contains("roll") OrElse n.Contains("kp") Then
                    Return ps
                End If
            Next

            ' Fall back by width: thermal receipt widths are ~58mm (228) or ~80mm
            ' (315) in hundredths of an inch. Pick the closest match in that range.
            Dim best As PaperSize = Nothing
            Dim bestScore As Integer = Integer.MaxValue
            For Each ps As PaperSize In settings.PaperSizes
                If ps.Width >= 220 AndAlso ps.Width <= 340 AndAlso ps.Height >= 120 Then
                    Dim score As Integer = Math.Abs(ps.Width - 228)
                    If score < bestScore Then
                        bestScore = score
                        best = ps
                    End If
                End If
            Next
            Return best
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Private Sub Sales_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        ' Save draft cart on any close/navigation/app exit
        PersistCartState()

        IdleTimeoutManager.Instance.OnBeforeLogout = Nothing
        IdleTimeoutManager.Instance.StopMonitoring(Me)

        If isNavigating Then
            Return
        End If

        If e.CloseReason = CloseReason.ApplicationExitCall Then
            Return
        End If

        If e.CloseReason = CloseReason.UserClosing Then
            Dim result As DialogResult = MessageBox.Show("Are you sure you want to exit the application?", "Exit Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result = DialogResult.Yes Then
                If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Application Exit", "User exited the application via Sales form")
                End If

                For Each form As Form In Application.OpenForms.Cast(Of Form).ToArray()
                    If form IsNot Me Then
                        form.Close()
                    End If
                Next

                Application.Exit()
            Else
                e.Cancel = True
            End If
        End If
    End Sub
    ' Event handlers for form events
    Private Sub CategoryPanel_Paint(sender As Object, e As PaintEventArgs) Handles CategoryPanel.Paint
        Dim radius As Integer = CategoryPanel.BorderRadius
        Dim borderPen As New Pen(Color.FromArgb(232, 232, 232), 2)
        borderPen.Alignment = PenAlignment.Inset
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias

        Dim rect As New Rectangle(1, 1, CategoryPanel.Width - 3, CategoryPanel.Height - 3)
        Dim d As Integer = radius * 2
        Dim path As New GraphicsPath()
        path.AddArc(rect.X, rect.Y, d, d, 180, 90)
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90)
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90)
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90)
        path.CloseFigure()
        e.Graphics.DrawPath(borderPen, path)
        path.Dispose()
        borderPen.Dispose()
    End Sub

    Private Sub Guna2HtmlLabel16_Click(sender As Object, e As EventArgs)
    End Sub

    Private Sub btnShorts_Click(sender As Object, e As EventArgs)
    End Sub

    Private Sub Guna2HtmlLabel39_Click(sender As Object, e As EventArgs)
    End Sub

    Private Sub PictureBox12_Click(sender As Object, e As EventArgs)
    End Sub

    Private Sub orderSummaryPanel_Paint(sender As Object, e As PaintEventArgs) Handles orderSummaryPanel.Paint
    End Sub

    Private Sub totalPanel_Paint(sender As Object, e As PaintEventArgs) Handles totalPanel.Paint
    End Sub

    ' Customer data variables (moved to top of class)
    ' Private selectedCustomerId As Integer? = Nothing - REMOVED DUPLICATE
    ' Private selectedCustomerName As String = "Walk-in Customer" - REMOVED DUPLICATE

    ' Payment and discount methods - Updated to show customer selection FIRST
    ' Replace the existing customer and payment methods with these new modal implementations

    ' Payment and discount methods - Updated to use modals
    Private Sub btnPayment_Click(sender As Object, e As EventArgs) Handles btnPayment.Click
        If posLockedForCapital Then
            MessageBox.Show("POS is locked. Manager/Admin must set opening capital first.", "POS Locked", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        ' Validate user session
        If Not ValidateUserSession() Then
            Return
        End If

        ' Validate that there are items in the order
        If currentOrderList.Count = 0 Then
            MessageBox.Show("Please add items to the order before proceeding to payment.", "No Items", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' STEP 1: Show customer information modal FIRST
        ShowCustomerInformationModal()
    End Sub

    ' NEW: Customer Information Modal
    ' NEW: Enhanced Customer Information Modal with improved spacing and design
    ' NEW: Enhanced Customer Information Modal with improved spacing and design
    ' Replace ShowCustomerInformationModal � preserve snapshot fields but allow nulls
    Private Sub ShowCustomerInformationModal()
        ' Create customer information modal form (simplified � no customer type buttons)
        Dim customerForm As New Form()
        customerForm.Text = "Customer Information"
        customerForm.Size = New Size(535, 480) ' resized after removing customer-type controls
        customerForm.StartPosition = FormStartPosition.CenterParent
        customerForm.FormBorderStyle = FormBorderStyle.FixedDialog
        customerForm.MaximizeBox = False
        customerForm.MinimizeBox = False
        customerForm.BackColor = OffWhite
        customerForm.ShowInTaskbar = False

        ' Get total amount for display
        Dim totalAmount As Decimal = 0D
        If totalLbl IsNot Nothing Then
            Decimal.TryParse(totalLbl.Text, totalAmount)
        End If

        ' Header section
        Dim headerPanel As New Panel() With {
        .Size = New Size(480, 70),
        .Location = New Point(20, 10),
        .BackColor = Color.Transparent
    }
        customerForm.Controls.Add(headerPanel)

        Dim lblTitle As New Label() With {
        .Text = "CUSTOMER INFORMATION",
        .Font = New Font("Poppins", 18, FontStyle.Bold),
        .ForeColor = Color.FromArgb(95, 95, 95),
        .Location = New Point(0, 0),
        .Size = New Size(480, 35),
        .TextAlign = ContentAlignment.MiddleCenter
    }
        headerPanel.Controls.Add(lblTitle)

        Dim lblOrderTotal As New Label() With {
        .Text = $"Order Total: ₱{totalAmount:F2}",
        .Font = New Font("Poppins", 14, FontStyle.Bold),
        .ForeColor = GoldenYellow,
        .Location = New Point(0, 35),
        .Size = New Size(480, 30),
        .TextAlign = ContentAlignment.MiddleCenter
    }
        headerPanel.Controls.Add(lblOrderTotal)

        ' Separator
        Dim separator As New Panel() With {
        .Size = New Size(440, 2),
        .Location = New Point(40, 90),
        .BackColor = JadeOlive
    }
        customerForm.Controls.Add(separator)

        ' Customer Details Section (moved up)
        Dim detailsSection As New Panel() With {
        .Size = New Size(480, 260),
        .Location = New Point(25, 110),
        .BackColor = Color.Transparent
    }
        customerForm.Controls.Add(detailsSection)

        ' Name
        Dim lblName As New Label() With {
        .Text = "Customer Name",
        .Font = New Font("Poppins", 11, FontStyle.Regular),
        .ForeColor = MediumText,
        .Location = New Point(0, 0),
        .Size = New Size(150, 25)
    }
        detailsSection.Controls.Add(lblName)

        Dim txtCustomerName As New Guna.UI2.WinForms.Guna2TextBox() With {
        .Size = New Size(460, 40),
        .Location = New Point(0, 25),
        .PlaceholderText = "Customer Name (optional)",
        .PlaceholderForeColor = BorderGray,
        .Font = New Font("Poppins", 11, FontStyle.Regular),
        .BorderRadius = 10,
        .FillColor = PureWhite,
        .ForeColor = DarkText,
        .BorderColor = BorderGray,
        .BorderThickness = 1,
        .Text = If(selectedCustomerName, "")
    }
        detailsSection.Controls.Add(txtCustomerName)

        ' TIN
        Dim lblTIN As New Label() With {
        .Text = "Customer TIN",
        .Font = New Font("Poppins", 11, FontStyle.Regular),
        .ForeColor = MediumText,
        .Location = New Point(0, 75),
        .Size = New Size(150, 25)
    }
        detailsSection.Controls.Add(lblTIN)

        Dim txtTIN As New Guna.UI2.WinForms.Guna2TextBox() With {
        .Size = New Size(460, 40),
        .Location = New Point(0, 100),
        .PlaceholderText = "TIN (optional)",
        .PlaceholderForeColor = BorderGray,
        .Font = New Font("Poppins", 11, FontStyle.Regular),
        .BorderRadius = 10,
        .FillColor = PureWhite,
        .ForeColor = DarkText,
        .BorderColor = BorderGray,
        .BorderThickness = 1,
        .Text = selectedCustomerTIN
    }
        detailsSection.Controls.Add(txtTIN)

        ' Phone and Email (side-by-side)
        Dim lblPhone As New Label() With {
        .Text = "Phone Number",
        .Font = New Font("Poppins", 11, FontStyle.Regular),
        .ForeColor = MediumText,
        .Location = New Point(0, 150),
        .Size = New Size(150, 25)
    }
        detailsSection.Controls.Add(lblPhone)

        Dim txtPhone As New Guna.UI2.WinForms.Guna2TextBox() With {
        .Size = New Size(220, 45),
        .Location = New Point(0, 175),
        .PlaceholderText = "Phone (optional)",
        .PlaceholderForeColor = BorderGray,
        .Font = New Font("Poppins", 11, FontStyle.Regular),
        .BorderRadius = 10,
        .FillColor = PureWhite,
        .ForeColor = DarkText,
        .BorderColor = BorderGray,
        .BorderThickness = 1,
        .Text = selectedCustomerPhone
    }
        detailsSection.Controls.Add(txtPhone)

        Dim lblEmail As New Label() With {
        .Text = "Email Address",
        .Font = New Font("Poppins", 11, FontStyle.Regular),
        .ForeColor = MediumText,
        .Location = New Point(240, 150),
        .Size = New Size(150, 25)
    }
        detailsSection.Controls.Add(lblEmail)

        Dim txtEmail As New Guna.UI2.WinForms.Guna2TextBox() With {
        .Size = New Size(220, 45),
        .Location = New Point(240, 175),
        .PlaceholderText = "Email (optional)",
        .PlaceholderForeColor = BorderGray,
        .Font = New Font("Poppins", 11, FontStyle.Regular),
        .BorderRadius = 10,
        .FillColor = PureWhite,
        .ForeColor = DarkText,
        .BorderColor = BorderGray,
        .BorderThickness = 1,
        .Text = selectedCustomerEmail
    }
        detailsSection.Controls.Add(txtEmail)

        ' Action buttons (moved up)
        Dim buttonSection As New Panel() With {
        .Size = New Size(500, 60),
        .Location = New Point(17, 380),
        .BackColor = Color.Transparent
    }
        customerForm.Controls.Add(buttonSection)

        Dim btnContinue As New Guna.UI2.WinForms.Guna2Button() With {
        .Text = "Continue",
        .Size = New Size(200, 50),
        .Location = New Point(260, 0),
        .Font = New Font("Poppins", 12, FontStyle.Bold),
        .ForeColor = DarkText,
        .FillColor = SuccessGreen,
        .BorderRadius = 12,
        .BorderThickness = 0
    }
        AddHandler btnContinue.Click, Sub()
                                          Dim nameVal As String = txtCustomerName.Text.Trim()
                                          Dim phoneVal As String = txtPhone.Text.Trim()
                                          Dim emailVal As String = txtEmail.Text.Trim()
                                          Dim tinVal As String = txtTIN.Text.Trim()

                                          ' Preserve selectedCustomerName/email/phone � allow null if empty
                                          selectedCustomerName = If(String.IsNullOrWhiteSpace(nameVal), Nothing, nameVal)
                                          selectedCustomerPhone = If(String.IsNullOrWhiteSpace(phoneVal), Nothing, phoneVal)
                                          selectedCustomerEmail = If(String.IsNullOrWhiteSpace(emailVal), Nothing, emailVal)
                                          selectedCustomerTIN = If(String.IsNullOrWhiteSpace(tinVal), Nothing, tinVal)

                                          ' Do NOT create or query Customers table � treat customers as ephemeral
                                          selectedCustomerId = Nothing

                                          customerForm.DialogResult = DialogResult.OK
                                          customerForm.Close()
                                      End Sub
        buttonSection.Controls.Add(btnContinue)

        Dim btnCancel As New Guna.UI2.WinForms.Guna2Button() With {
        .Text = "Cancel",
        .Size = New Size(140, 50),
        .Location = New Point(80, 0),
        .Font = New Font("Poppins", 12, FontStyle.Regular),
        .ForeColor = Color.FromArgb(95, 95, 95),
        .FillColor = Color.FromArgb(250, 249, 246),
        .BorderColor = Color.FromArgb(200, 198, 192),
        .BorderThickness = 1,
        .BorderRadius = 12
    }
        AddHandler btnCancel.Click, Sub()
                                        selectedCustomerName = If(String.IsNullOrWhiteSpace(txtCustomerName.Text.Trim()), Nothing, txtCustomerName.Text.Trim())
                                        selectedCustomerPhone = If(String.IsNullOrWhiteSpace(txtPhone.Text.Trim()), Nothing, txtPhone.Text.Trim())
                                        selectedCustomerEmail = If(String.IsNullOrWhiteSpace(txtEmail.Text.Trim()), Nothing, txtEmail.Text.Trim())
                                        selectedCustomerTIN = If(String.IsNullOrWhiteSpace(txtTIN.Text.Trim()), Nothing, txtTIN.Text.Trim())
                                        customerForm.DialogResult = DialogResult.Cancel
                                        customerForm.Close()
                                    End Sub
        AddHandler btnCancel.MouseEnter, Sub() btnCancel.FillColor = Color.FromArgb(240, 238, 232)
        AddHandler btnCancel.MouseLeave, Sub() btnCancel.FillColor = Color.FromArgb(250, 249, 246)
        buttonSection.Controls.Add(btnCancel)

        ' Keyboard handlers
        customerForm.KeyPreview = True
        AddHandler customerForm.KeyDown, Sub(sender As Object, e As KeyEventArgs)
                                             If e.KeyCode = Keys.Enter Then
                                                 btnContinue.PerformClick()
                                                 e.Handled = True
                                             ElseIf e.KeyCode = Keys.Escape Then
                                                 btnCancel.PerformClick()
                                                 e.Handled = True
                                             End If
                                         End Sub

        customerForm.ActiveControl = txtCustomerName

        ' Show dialog
        Dim result As DialogResult = customerForm.ShowDialog()

        If result = DialogResult.OK Then
            ShowPaymentMethodModal()
        End If

        customerForm.Dispose()
    End Sub
    ' NEW: Payment Method Modal
    ' NEW: Payment Method Modal
    ' NEW: Payment Method Modal
    Private Sub ShowPaymentMethodModal()
        ' Create payment method modal form
        Dim paymentForm As New Form()
        paymentForm.Text = "Select Payment Method"
        paymentForm.Size = New Size(600, 450)
        paymentForm.StartPosition = FormStartPosition.CenterParent
        paymentForm.FormBorderStyle = FormBorderStyle.FixedDialog
        paymentForm.MaximizeBox = False
        paymentForm.MinimizeBox = False
        paymentForm.BackColor = OffWhite
        paymentForm.ShowInTaskbar = False

        ' Get total amount
        Dim totalAmount As Decimal = 0D
        If totalLbl IsNot Nothing Then
            Decimal.TryParse(totalLbl.Text, totalAmount)
        End If

        ' Title
        Dim lblTitle As New Label()
        lblTitle.Text = "Select Payment Method"
        lblTitle.Font = New Font("Poppins", 14, FontStyle.Bold)
        lblTitle.ForeColor = Color.FromArgb(95, 95, 95)
        lblTitle.Location = New Point(20, 10)
        lblTitle.Size = New Size(560, 30)
        lblTitle.TextAlign = ContentAlignment.MiddleCenter
        paymentForm.Controls.Add(lblTitle)
        ' Show customer name only if provided (show "Customer:" when empty)
        Dim lblCustomerInfo As New Label()
        lblCustomerInfo.Text = If(String.IsNullOrWhiteSpace(selectedCustomerName), "Customer:", $"Customer: {selectedCustomerName} ({selectedCustomerType})")
        lblCustomerInfo.Font = New Font("Poppins", 10, FontStyle.Regular)
        lblCustomerInfo.ForeColor = Color.FromArgb(95, 95, 95)
        lblCustomerInfo.Location = New Point(20, 60)
        lblCustomerInfo.Size = New Size(560, 25)
        lblCustomerInfo.TextAlign = ContentAlignment.MiddleCenter
        paymentForm.Controls.Add(lblCustomerInfo)

        ' Total amount display
        Dim lblTotal As New Label()
        lblTotal.Text = $"Total Amount: ₱{totalAmount:F2}"
        lblTotal.Font = New Font("Poppins", 10, FontStyle.Bold)
        lblTotal.ForeColor = Color.FromArgb(95, 95, 95)
        lblTotal.Location = New Point(20, 100)
        lblTotal.Size = New Size(560, 30)
        lblTotal.TextAlign = ContentAlignment.MiddleCenter
        paymentForm.Controls.Add(lblTotal)

        Dim cashColor As Color = Color.FromArgb(76, 175, 80)
        Dim gcashColor As Color = Color.FromArgb(0, 120, 212)
        Dim cardColor As Color = Color.FromArgb(124, 58, 237)
        Dim actionBorder As Color = Color.FromArgb(200, 198, 192)
        Dim goldAccent As Color = Color.FromArgb(191, 155, 48)

        ' Payment method buttons (centered)
        Dim buttonStartX As Integer = (paymentForm.Width - (3 * 150 + 2 * 40)) / 2

        ' Cash button
        Dim btnCash As New Guna.UI2.WinForms.Guna2Button()
        btnCash.Text = "💵" & vbCrLf & "Cash"
        btnCash.Size = New Size(150, 100)
        btnCash.Location = New Point(buttonStartX, 160)
        btnCash.Font = New Font("Poppins", 14, FontStyle.Bold)
        btnCash.ForeColor = Color.White
        btnCash.FillColor = cashColor
        btnCash.BorderThickness = 0
        btnCash.BorderRadius = 15
        btnCash.HoverState.FillColor = Color.FromArgb(67, 160, 71)
        btnCash.PressedColor = Color.FromArgb(56, 142, 60)
        AddHandler btnCash.Click, Sub()
                                      selectedPaymentMethod = "Cash"
                                      paymentReference = ""
                                      paymentForm.DialogResult = DialogResult.OK
                                      paymentForm.Close()
                                  End Sub
        paymentForm.Controls.Add(btnCash)

        ' GCash button
        Dim btnGCash As New Guna.UI2.WinForms.Guna2Button()
        btnGCash.Text = "📱" & vbCrLf & "GCash"
        btnGCash.Size = New Size(150, 100)
        btnGCash.Location = New Point(buttonStartX + 190, 160)
        btnGCash.Font = New Font("Poppins", 14, FontStyle.Bold)
        btnGCash.ForeColor = Color.White
        btnGCash.FillColor = gcashColor
        btnGCash.BorderThickness = 0
        btnGCash.BorderRadius = 15
        btnGCash.HoverState.FillColor = Color.FromArgb(0, 102, 190)
        btnGCash.PressedColor = Color.FromArgb(0, 85, 160)
        AddHandler btnGCash.Click, Sub()
                                       selectedPaymentMethod = "GCash"
                                       paymentForm.DialogResult = DialogResult.Yes
                                       paymentForm.Close()
                                   End Sub
        paymentForm.Controls.Add(btnGCash)

        ' Card button
        Dim btnCard As New Guna.UI2.WinForms.Guna2Button()
        btnCard.Text = "💳" & vbCrLf & "Card"
        btnCard.Size = New Size(150, 100)
        btnCard.Location = New Point(buttonStartX + 380, 160)
        btnCard.Font = New Font("Poppins", 14, FontStyle.Bold)
        btnCard.ForeColor = Color.White
        btnCard.FillColor = cardColor
        btnCard.BorderThickness = 0
        btnCard.BorderRadius = 15
        btnCard.HoverState.FillColor = Color.FromArgb(109, 46, 209)
        btnCard.PressedColor = Color.FromArgb(91, 33, 182)
        AddHandler btnCard.Click, Sub()
                                      selectedPaymentMethod = "Card"
                                      paymentForm.DialogResult = DialogResult.Yes
                                      paymentForm.Close()
                                  End Sub
        paymentForm.Controls.Add(btnCard)

        ' Action buttons
        Dim btnBackToCustomer As New Guna.UI2.WinForms.Guna2Button()
        btnBackToCustomer.Text = "← Back to Customer"
        btnBackToCustomer.Size = New Size(180, 45)
        btnBackToCustomer.Location = New Point(120, 300)
        btnBackToCustomer.Font = New Font("Poppins", 11, FontStyle.Regular)
        btnBackToCustomer.ForeColor = Color.FromArgb(95, 95, 95)
        btnBackToCustomer.FillColor = Color.FromArgb(250, 249, 246)
        btnBackToCustomer.BorderColor = actionBorder
        btnBackToCustomer.BorderThickness = 1
        btnBackToCustomer.BorderRadius = 12
        btnBackToCustomer.HoverState.FillColor = Color.FromArgb(240, 238, 232)
        AddHandler btnBackToCustomer.Click, Sub()
                                                paymentForm.DialogResult = DialogResult.Retry
                                                paymentForm.Close()
                                            End Sub
        paymentForm.Controls.Add(btnBackToCustomer)

        Dim btnCancel As New Guna.UI2.WinForms.Guna2Button()
        btnCancel.Text = "Cancel"
        btnCancel.Size = New Size(120, 45)
        btnCancel.Location = New Point(320, 300)
        btnCancel.Font = New Font("Poppins", 11, FontStyle.Regular)
        btnCancel.ForeColor = Color.FromArgb(200, 70, 70)
        btnCancel.FillColor = Color.FromArgb(255, 245, 245)
        btnCancel.BorderColor = Color.FromArgb(220, 120, 120)
        btnCancel.BorderThickness = 1
        btnCancel.BorderRadius = 12
        btnCancel.HoverState.FillColor = Color.FromArgb(255, 235, 235)
        AddHandler btnCancel.Click, Sub()
                                        paymentForm.DialogResult = DialogResult.Cancel
                                        paymentForm.Close()
                                    End Sub
        paymentForm.Controls.Add(btnCancel)

        ' Keyboard navigation support
        Dim paymentButtons As New List(Of Guna.UI2.WinForms.Guna2Button) From {btnCash, btnGCash, btnCard, btnBackToCustomer, btnCancel}
        For Each btn In paymentButtons
            AddHandler btn.GotFocus, Sub()
                                         btn.BorderColor = Color.FromArgb(191, 155, 48)
                                         btn.BorderThickness = 2
                                     End Sub
            AddHandler btn.LostFocus, Sub()
                                          btn.BorderColor = Color.FromArgb(200, 198, 192)
                                          btn.BorderThickness = 1
                                      End Sub
        Next

        paymentForm.KeyPreview = True
        AddHandler paymentForm.KeyDown, Sub(sender As Object, e As KeyEventArgs)
                                            If e.KeyCode = Keys.Up Or e.KeyCode = Keys.Left Then
                                                Dim currentIndex As Integer = paymentButtons.IndexOf(TryCast(paymentForm.ActiveControl, Guna.UI2.WinForms.Guna2Button))
                                                If currentIndex >= 0 Then
                                                    Dim prevIndex As Integer = If(currentIndex = 0, paymentButtons.Count - 1, currentIndex - 1)
                                                    paymentButtons(prevIndex).Focus()
                                                Else
                                                    paymentButtons(0).Focus()
                                                End If
                                                e.Handled = True
                                            ElseIf e.KeyCode = Keys.Down Or e.KeyCode = Keys.Right Then
                                                Dim currentIndex As Integer = paymentButtons.IndexOf(TryCast(paymentForm.ActiveControl, Guna.UI2.WinForms.Guna2Button))
                                                If currentIndex >= 0 Then
                                                    Dim nextIndex As Integer = If(currentIndex = paymentButtons.Count - 1, 0, currentIndex + 1)
                                                    paymentButtons(nextIndex).Focus()
                                                Else
                                                    paymentButtons(0).Focus()
                                                End If
                                                e.Handled = True
                                            ElseIf e.KeyCode = Keys.Escape Then
                                                paymentForm.DialogResult = DialogResult.Cancel
                                                paymentForm.Close()
                                                e.Handled = True
                                            End If
                                        End Sub

        ' Show modal and handle result
        Dim result As DialogResult = paymentForm.ShowDialog()
        paymentForm.Dispose()

        Select Case result
            Case DialogResult.OK
                ShowCashAmountInputModal()
            Case DialogResult.Yes
                If ShowReferenceInputModal() Then
                    confirmBtn.PerformClick()
                End If
            Case DialogResult.Retry
                ShowCustomerInformationModal()
            Case DialogResult.Cancel
                Return
        End Select
    End Sub

    ' NEW: Reference Input Modal for GCash/Card payments
    ' NEW: Reference Input Modal for GCash/Card payments
    Private Function ShowReferenceInputModal() As Boolean
        ' Create reference input modal form
        Dim refForm As New Form()
        refForm.Text = $"{selectedPaymentMethod} Payment Reference"
        refForm.Size = New Size(480, 300)
        refForm.StartPosition = FormStartPosition.CenterParent
        refForm.FormBorderStyle = FormBorderStyle.FixedDialog
        refForm.MaximizeBox = False
        refForm.MinimizeBox = False
        refForm.BackColor = OffWhite
        refForm.ShowInTaskbar = False
        refForm.KeyPreview = True ' Enable keyboard input for the form

        ' Get total amount
        Dim totalAmount As Decimal = 0D
        If totalLbl IsNot Nothing Then
            Decimal.TryParse(totalLbl.Text, totalAmount)
        End If

        ' Title - CENTERED
        Dim lblTitle As New Label()
        lblTitle.Text = $"{selectedPaymentMethod} Payment"
        lblTitle.Font = New Font("Poppins", 14, FontStyle.Bold)
        lblTitle.ForeColor = Color.FromArgb(95, 95, 95)
        lblTitle.Size = New Size(410, 30)
        lblTitle.Location = New Point((refForm.Width - lblTitle.Width) \ 2, 20) ' CENTERED
        lblTitle.TextAlign = ContentAlignment.MiddleCenter
        refForm.Controls.Add(lblTitle)

        ' Total amount display - CENTERED
        Dim lblTotal As New Label()
        lblTotal.Text = $"Total: ₱{totalAmount:F2}"
        lblTotal.Font = New Font("Poppins", 12, FontStyle.Bold)
        lblTotal.ForeColor = Color.FromArgb(95, 95, 95)
        lblTotal.Size = New Size(410, 25)
        lblTotal.Location = New Point((refForm.Width - lblTotal.Width) \ 2, 60) ' CENTERED
        lblTotal.TextAlign = ContentAlignment.MiddleCenter
        refForm.Controls.Add(lblTotal)

        ' Reference input label - CENTERED
        Dim lblReference As New Label()
        lblReference.Text = "Enter Reference Number:"
        lblReference.Font = New Font("Poppins", 12, FontStyle.Regular)
        lblReference.ForeColor = Color.FromArgb(95, 95, 95)
        lblReference.Size = New Size(200, 25)
        lblReference.Location = New Point((refForm.Width - lblReference.Width) \ 2, 110) ' CENTERED
        lblReference.TextAlign = ContentAlignment.MiddleCenter
        refForm.Controls.Add(lblReference)

        ' Reference input - CENTERED
        Dim txtReference As New Guna.UI2.WinForms.Guna2TextBox()
        txtReference.Size = New Size(390, 40)
        txtReference.Location = New Point((refForm.Width - txtReference.Width) \ 2, 140) ' CENTERED
        txtReference.PlaceholderText = "Enter transaction reference number"
        txtReference.Font = New Font("Poppins", 12, FontStyle.Regular)
        txtReference.BorderRadius = 8
        txtReference.FillColor = Color.FromArgb(250, 249, 246)
        txtReference.ForeColor = Color.FromArgb(95, 95, 95)
        txtReference.BorderColor = Color.FromArgb(200, 198, 192)
        txtReference.BorderThickness = 1
        refForm.Controls.Add(txtReference)

        ' Action buttons - CENTERED GROUP
        ' Calculate center for the button group
        Dim buttonSpacing As Integer = 20
        Dim totalButtonWidth As Integer = 120 + 200 + buttonSpacing ' btnBack + btnComplete + spacing
        Dim buttonGroupStartX As Integer = (refForm.Width - totalButtonWidth) \ 2

        Dim cardFill As Color = Color.FromArgb(250, 249, 246)
        Dim cardFore As Color = Color.FromArgb(95, 95, 95)
        Dim cardBorder As Color = Color.FromArgb(200, 198, 192)
        Dim cardHover As Color = Color.FromArgb(240, 238, 232)
        Dim goldAccent As Color = Color.FromArgb(191, 155, 48)

        Dim btnComplete As New Guna.UI2.WinForms.Guna2Button()
        btnComplete.Text = "Confirm Payment"
        btnComplete.Size = New Size(200, 50)
        btnComplete.Location = New Point(buttonGroupStartX + 120 + buttonSpacing, 200) ' Position after btnBack
        btnComplete.Font = New Font("Poppins", 10, FontStyle.Bold)
        btnComplete.ForeColor = Color.White
        btnComplete.FillColor = Color.FromArgb(76, 175, 80)
        btnComplete.BorderThickness = 0
        btnComplete.BorderRadius = 12
        btnComplete.HoverState.FillColor = Color.FromArgb(67, 160, 71)
        AddHandler btnComplete.Click, Sub()
                                          If String.IsNullOrWhiteSpace(txtReference.Text) Then
                                              MessageBox.Show("Please enter a reference number.", "Missing Reference", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                              Return
                                          End If
                                          paymentReference = txtReference.Text.Trim()
                                          refForm.DialogResult = DialogResult.OK
                                          refForm.Close()
                                      End Sub
        refForm.Controls.Add(btnComplete)

        Dim btnBack As New Guna.UI2.WinForms.Guna2Button()
        btnBack.Text = "← Back"
        btnBack.Size = New Size(120, 50)
        btnBack.Location = New Point(buttonGroupStartX, 200) ' Start of group
        btnBack.Font = New Font("Poppins", 12, FontStyle.Regular)
        btnBack.ForeColor = Color.FromArgb(95, 95, 95)
        btnBack.FillColor = Color.FromArgb(250, 249, 246)
        btnBack.BorderColor = Color.FromArgb(200, 198, 192)
        btnBack.BorderThickness = 1
        btnBack.BorderRadius = 12
        btnBack.HoverState.FillColor = Color.FromArgb(240, 238, 232)
        AddHandler btnBack.Click, Sub()
                                      refForm.DialogResult = DialogResult.Cancel
                                      refForm.Close()
                                  End Sub
        refForm.Controls.Add(btnBack)

        ' Add KeyDown handler for keyboard support
        AddHandler refForm.KeyDown, Sub(sender As Object, e As KeyEventArgs)
                                        If e.KeyCode = Keys.Enter Then
                                            ' Confirm the reference on Enter
                                            btnComplete.PerformClick()
                                            e.Handled = True
                                        ElseIf e.KeyCode = Keys.Escape Then
                                            ' Go back on Escape
                                            btnBack.PerformClick()
                                            e.Handled = True
                                        End If
                                    End Sub

        ' Show modal and return result
        Dim result As Boolean = refForm.ShowDialog() = DialogResult.OK
        refForm.Dispose()

        If Not result Then
            ' User cancelled, go back to payment method selection
            ShowPaymentMethodModal()
        End If

        Return result
    End Function

    ' NEW: Cash Amount Input Modal
    ' ENHANCED: Cash Amount Input Modal with improved spacing and keyboard support
    ' ENHANCED: Cash Amount Input Modal with comprehensive improvements
    Private Sub ShowCashAmountInputModal()
        ' Create cash amount input modal form
        Dim cashForm As New Form()
        cashForm.Text = "Cash Payment"
        cashForm.Size = New Size(520, 800) ' INCREASED height from 750 to 800 for better spacing
        cashForm.StartPosition = FormStartPosition.CenterParent
        cashForm.AutoScaleMode = AutoScaleMode.Dpi
        cashForm.FormBorderStyle = FormBorderStyle.FixedDialog
        cashForm.MaximizeBox = False
        cashForm.MinimizeBox = False
        cashForm.BackColor = OffWhite
        cashForm.ShowInTaskbar = False
        cashForm.KeyPreview = True ' Enable keyboard input for the form

        ' Get total amount
        Dim totalAmount As Decimal = 0D
        If totalLbl IsNot Nothing Then
            Decimal.TryParse(totalLbl.Text, totalAmount)
        End If

        ' Header section with improved spacing
        Dim headerSection As New Panel()
        headerSection.Size = New Size(480, 120)
        headerSection.Location = New Point(20, 20) ' Same Y position
        headerSection.BackColor = Color.Transparent
        cashForm.Controls.Add(headerSection)

        ' Title with better typography
        Dim lblTitle As New Label()
        lblTitle.Text = "CASH PAYMENT"
        lblTitle.Font = New Font("Poppins", 18, FontStyle.Bold)
        lblTitle.ForeColor = Color.FromArgb(95, 95, 95)
        lblTitle.Location = New Point(0, 0)
        lblTitle.Size = New Size(480, 35)
        lblTitle.TextAlign = ContentAlignment.MiddleCenter
        headerSection.Controls.Add(lblTitle)

        'Replace customer text in cash modal header to show "Customer:" when no name provided
        Dim lblCustomer As New Label()
        lblCustomer.Text = If(String.IsNullOrWhiteSpace(selectedCustomerName), "Customer:", $"Customer: {selectedCustomerName}")
        lblCustomer.Font = New Font("Poppins", 12, FontStyle.Regular)
        lblCustomer.ForeColor = GoldenYellow
        lblCustomer.Location = New Point(0, 40)
        lblCustomer.Size = New Size(480, 25)
        lblCustomer.TextAlign = ContentAlignment.MiddleCenter
        headerSection.Controls.Add(lblCustomer)

        ' Order total display
        Dim lblOrderTotal As New Label()
        lblOrderTotal.Text = $"Total Due: ₱{totalAmount:F2}"
        lblOrderTotal.Font = New Font("Poppins", 14, FontStyle.Bold)
        lblOrderTotal.ForeColor = MediumText
        lblOrderTotal.Location = New Point(0, 70)
        lblOrderTotal.Size = New Size(480, 30)
        lblOrderTotal.TextAlign = ContentAlignment.MiddleCenter
        headerSection.Controls.Add(lblOrderTotal)

        ' Separator line - ADJUSTED Y position for more spacing
        Dim separator As New Panel()
        separator.Size = New Size(440, 2)
        separator.Location = New Point(40, 160) ' Moved down from 155
        separator.BackColor = JadeOlive
        cashForm.Controls.Add(separator)

        ' Amount display section - uses full form width for proper centering
        Dim amountSection As New Panel()
        amountSection.Size = New Size(480, 120)
        amountSection.Location = New Point(20, 180)
        amountSection.BackColor = Color.Transparent
        cashForm.Controls.Add(amountSection)

        ' Amount received label
        Dim lblAmountReceived As New Label()
        lblAmountReceived.Text = "Amount Received"
        lblAmountReceived.Font = New Font("Poppins", 12, FontStyle.Regular)
        lblAmountReceived.ForeColor = Color.FromArgb(95, 95, 95)
        lblAmountReceived.Dock = DockStyle.Top
        lblAmountReceived.Height = 25
        lblAmountReceived.TextAlign = ContentAlignment.MiddleCenter
        amountSection.Controls.Add(lblAmountReceived)

        ' Amount display - use regular Label with Dock/TextAlign for automatic centering
        enteredAmount = "" ' Reset amount
        lblAmountDisplay = New Label()
        lblAmountDisplay.Text = "₱0.00"
        lblAmountDisplay.Font = New Font("Segoe UI", 28, FontStyle.Bold)
        lblAmountDisplay.ForeColor = Color.FromArgb(95, 95, 95)
        lblAmountDisplay.AutoSize = False
        lblAmountDisplay.Dock = DockStyle.Fill
        lblAmountDisplay.TextAlign = ContentAlignment.MiddleCenter
        amountSection.Controls.Add(lblAmountDisplay)

        ' Input hint label
        Dim lblInputHint As New Label()
        lblInputHint.Text = "Type amount or use keypad below"
        lblInputHint.Font = New Font("Poppins", 9, FontStyle.Italic)
        lblInputHint.ForeColor = BorderGray
        lblInputHint.Dock = DockStyle.Bottom
        lblInputHint.Height = 20
        lblInputHint.TextAlign = ContentAlignment.MiddleCenter
        amountSection.Controls.Add(lblInputHint)

        ' Change display centered in form
        Dim changeSection As New Panel()
        changeSection.Size = New Size(480, 40)
        changeSection.Location = New Point(20, 320)
        changeSection.BackColor = Color.Transparent
        cashForm.Controls.Add(changeSection)

        Dim innerChangeTable As New TableLayoutPanel()
        innerChangeTable.ColumnCount = 2
        innerChangeTable.RowCount = 1
        innerChangeTable.Dock = DockStyle.Fill
        innerChangeTable.BackColor = Color.Transparent
        innerChangeTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        innerChangeTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        changeSection.Controls.Add(innerChangeTable)

        Dim lblChangeLabel As New Label()
        lblChangeLabel.Text = "Change:"
        lblChangeLabel.Font = New Font("Poppins", 12, FontStyle.Regular)
        lblChangeLabel.ForeColor = Color.FromArgb(95, 95, 95)
        lblChangeLabel.Dock = DockStyle.Fill
        lblChangeLabel.TextAlign = ContentAlignment.MiddleRight
        lblChangeLabel.Padding = New Padding(0, 0, 5, 0)
        innerChangeTable.Controls.Add(lblChangeLabel, 0, 0)

        Dim lblChangeAmount As New Label()
        lblChangeAmount.Text = "₱0.00"
        lblChangeAmount.Font = New Font("Poppins", 12, FontStyle.Bold)
        lblChangeAmount.ForeColor = SuccessGreen
        lblChangeAmount.Dock = DockStyle.Fill
        lblChangeAmount.TextAlign = ContentAlignment.MiddleLeft
        lblChangeAmount.Padding = New Padding(5, 0, 0, 0)
        innerChangeTable.Controls.Add(lblChangeAmount, 1, 0)

        ' Update amount display with automatic centering (Dock + TextAlign handles position)
        Dim UpdateCashAmountDisplay = Sub()
                                          Dim displayValue As Decimal = 0D
                                          Dim amountText As String = enteredAmount

                                          If String.IsNullOrEmpty(amountText) Then
                                              displayValue = 0D
                                              lblAmountDisplay.Text = "₱0.0"
                                          ElseIf amountText.Contains(".") Then
                                              If Decimal.TryParse(amountText, displayValue) Then
                                                  lblAmountDisplay.Text = $"₱{displayValue:F1}"
                                              Else
                                                  If amountText.EndsWith(".") AndAlso amountText.Length > 1 Then
                                                      Dim wholePart As String = amountText.Substring(0, amountText.Length - 1)
                                                      If Decimal.TryParse(wholePart, displayValue) Then
                                                          lblAmountDisplay.Text = $"₱{displayValue:F1}"
                                                      Else
                                                          lblAmountDisplay.Text = "₱0.0"
                                                      End If
                                                  Else
                                                      lblAmountDisplay.Text = "₱0.0"
                                                  End If
                                              End If
                                          Else
                                              If Decimal.TryParse(amountText, displayValue) Then
                                                  lblAmountDisplay.Text = $"₱{displayValue:F1}"
                                              Else
                                                  lblAmountDisplay.Text = "₱0.0"
                                              End If
                                          End If

                                          ' Update change calculation
                                          Dim changeVal As Decimal = 0D
                                          If Decimal.TryParse(lblAmountDisplay.Text.Replace("₱", ""), changeVal) Then
                                              changeVal = changeVal - totalAmount
                                              lblChangeAmount.Text = $"₱{changeVal:F1}"
                                              lblChangeAmount.ForeColor = If(changeVal >= 0, SuccessGreen, AlertRed)
                                          End If

                                          ' Update input hint based on state
                                          If String.IsNullOrEmpty(amountText) Then
                                              lblInputHint.Text = "Type amount or use keypad below"
                                              lblInputHint.ForeColor = BorderGray
                                          ElseIf Decimal.TryParse(lblAmountDisplay.Text.Replace("₱", ""), displayValue) AndAlso (displayValue - totalAmount) >= 0D Then
                                              lblInputHint.Text = "Sufficient amount entered"
                                              lblInputHint.ForeColor = SuccessGreen
                                          Else
                                              lblInputHint.Text = "Insufficient amount"
                                              lblInputHint.ForeColor = AlertRed
                                          End If
                                      End Sub


        ' Keypad section with improved spacing - ADJUSTED Y position
        Dim keypadSection As New Panel()
        keypadSection.Size = New Size(480, 240)
        keypadSection.Location = New Point(20, 370) ' Moved down from 350
        keypadSection.BackColor = Color.Transparent
        cashForm.Controls.Add(keypadSection)

        ' Keypad buttons with better spacing
        Dim buttonSize As Integer = 70
        Dim buttonSpacing As Integer = 15
        Dim buttonStartX As Integer = (480 - (buttonSize * 3 + buttonSpacing * 2)) / 2
        Dim buttonStartY As Integer = 0
        Dim buttonTexts As String() = {"1", "2", "3", "4", "5", "6", "7", "8", "9", ".", "0", "X"}

        Dim kpFill As Color = Color.FromArgb(250, 249, 246)
        Dim kpFore As Color = Color.FromArgb(95, 95, 95)
        Dim kpHover As Color = Color.FromArgb(240, 238, 232)

        For i = 0 To buttonTexts.Length - 1
            Dim button As New Guna.UI2.WinForms.Guna2Button()
            button.Size = New Size(buttonSize, buttonSize)
            button.BorderRadius = 10
            button.FillColor = kpFill
            button.BackColor = OffWhite
            button.ForeColor = kpFore
            button.BorderColor = Color.FromArgb(200, 198, 192)
            button.BorderThickness = 1
            button.Font = New Font("Poppins", 14, FontStyle.Bold)
            button.Text = buttonTexts(i)
            button.TabStop = False
            button.HoverState.FillColor = kpHover

            ' Special styling for different buttons
            If button.Text = "X" Then
                button.FillColor = Color.FromArgb(220, 60, 75)
                button.ForeColor = Color.FromArgb(250, 249, 246)
                button.HoverState.FillColor = Color.FromArgb(200, 50, 50)
            ElseIf button.Text = "." Then
                ' Visual hint that decimal requires digits first
                button.Font = New Font("Poppins", 18, FontStyle.Bold)
            End If

            Dim row = i \ 3
            Dim col = i Mod 3
            button.Location = New Point(buttonStartX + col * (buttonSize + buttonSpacing), buttonStartY + row * (buttonSize + buttonSpacing))

            AddHandler button.Click, Sub(sender As Object, e As EventArgs)
                                         Dim btn As Guna.UI2.WinForms.Guna2Button = CType(sender, Guna.UI2.WinForms.Guna2Button)
                                         If btn.Text = "X" Then
                                             If enteredAmount.Length > 0 Then
                                                 enteredAmount = enteredAmount.Substring(0, enteredAmount.Length - 1)
                                             End If
                                         ElseIf btn.Text = "." Then
                                             ' Allow only one decimal point
                                             If Not enteredAmount.Contains(".") AndAlso enteredAmount.Length > 0 Then
                                                 enteredAmount &= "."
                                             End If
                                         ElseIf btn.Text >= "0" And btn.Text <= "9" Then
                                             If enteredAmount.Length < 10 Then
                                                 enteredAmount &= btn.Text
                                             End If
                                         End If
                                         UpdateAmountDisplay()
                                     End Sub
            keypadSection.Controls.Add(button)
        Next

        ' Quick amount buttons section - ADJUSTED Y position
        Dim quickAmountSection As New Panel()
        quickAmountSection.Size = New Size(480, 50)
        quickAmountSection.Location = New Point(20, 630) ' Moved down from 600
        quickAmountSection.BackColor = Color.Transparent
        cashForm.Controls.Add(quickAmountSection)

        Dim cardFill As Color = Color.FromArgb(250, 249, 246)
        Dim cardFore As Color = Color.FromArgb(95, 95, 95)
        Dim cardBorder As Color = Color.FromArgb(200, 198, 192)
        Dim cardHover As Color = Color.FromArgb(240, 238, 232)
        Dim goldAccent As Color = Color.FromArgb(191, 155, 48)

        ' Shared actions used by both the buttons and the keyboard shortcuts.
        ' Calling the logic directly (instead of PerformClick) means Guna2Button
        ' never steals focus, so Enter always confirms even right after E/C.
        Dim applyExactAction As System.Action = Sub()
                                                    enteredAmount = totalAmount.ToString("F2")
                                                    UpdateCashAmountDisplay()
                                                End Sub
        Dim clearAmountAction As System.Action = Sub()
                                                     enteredAmount = ""
                                                     UpdateCashAmountDisplay()
                                                 End Sub
        Dim confirmPaymentAction As System.Action = Sub()
                                                        Dim receivedAmount As Decimal = 0D
                                                        Dim amountText As String = lblAmountDisplay.Text.Replace("₱", "")
                                                        If Not Decimal.TryParse(amountText, receivedAmount) OrElse receivedAmount < totalAmount Then
                                                            MessageBox.Show("Amount received must be greater than or equal to order total.", "Payment Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                                            Return
                                                        End If
                                                        cashForm.DialogResult = DialogResult.OK
                                                        cashForm.Close()
                                                    End Sub

        Dim btnExact As New Guna.UI2.WinForms.Guna2Button()
        btnExact.Text = $"Exact"
        btnExact.Size = New Size(140, 40)
        btnExact.Location = New Point(100, 5)
        btnExact.Font = New Font("Poppins", 10, FontStyle.Bold)
        btnExact.ForeColor = Color.White
        btnExact.FillColor = Color.FromArgb(76, 175, 80)
        btnExact.BorderThickness = 0
        btnExact.BorderRadius = 10
        btnExact.HoverState.FillColor = Color.FromArgb(67, 160, 71)
        btnExact.TabStop = False ' keep focus on the form so E/Enter shortcuts always work
        AddHandler btnExact.Click, Sub()
                                       applyExactAction()
                                   End Sub
        quickAmountSection.Controls.Add(btnExact)
        ' Clear button
        Dim btnClear As New Guna.UI2.WinForms.Guna2Button()
        btnClear.Text = "Clear"
        btnClear.Size = New Size(100, 40)
        btnClear.Location = New Point(260, 5)
        btnClear.Font = New Font("Poppins", 10, FontStyle.Bold)
        btnClear.ForeColor = Color.FromArgb(95, 95, 95)
        btnClear.FillColor = Color.FromArgb(250, 249, 246)
        btnClear.BorderColor = Color.FromArgb(200, 198, 192)
        btnClear.BorderThickness = 1
        btnClear.BorderRadius = 10
        btnClear.HoverState.FillColor = Color.FromArgb(240, 238, 232)
        btnClear.TabStop = False
        AddHandler btnClear.Click, Sub()
                                       clearAmountAction()
                                   End Sub
        quickAmountSection.Controls.Add(btnClear)

        ' Action buttons section - ADJUSTED Y position
        Dim actionSection As New Panel()
        actionSection.Size = New Size(480, 60)
        actionSection.Location = New Point(20, 690) ' Moved down from 660
        actionSection.BackColor = Color.Transparent
        cashForm.Controls.Add(actionSection)

        Dim btnComplete As New Guna.UI2.WinForms.Guna2Button()
        btnComplete.Text = "Confirm"
        btnComplete.Size = New Size(160, 50)
        btnComplete.Location = New Point(300, 5)
        btnComplete.Font = New Font("Poppins", 12, FontStyle.Bold)
        btnComplete.ForeColor = Color.White
        btnComplete.FillColor = Color.FromArgb(76, 175, 80)
        btnComplete.BorderThickness = 0
        btnComplete.BorderRadius = 12
        btnComplete.HoverState.FillColor = Color.FromArgb(67, 160, 71)
        btnComplete.TabStop = False ' keep focus on the form so keyboard shortcuts always work
        AddHandler btnComplete.Click, Sub()
                                          confirmPaymentAction()
                                      End Sub
        actionSection.Controls.Add(btnComplete)

        Dim btnBack As New Guna.UI2.WinForms.Guna2Button()
        btnBack.Text = "← Back"
        btnBack.Size = New Size(120, 50)
        btnBack.Location = New Point(40, 5)
        btnBack.Font = New Font("Poppins", 11, FontStyle.Regular)
        btnBack.ForeColor = Color.FromArgb(95, 95, 95)
        btnBack.FillColor = Color.FromArgb(250, 249, 246)
        btnBack.BorderColor = Color.FromArgb(200, 198, 192)
        btnBack.BorderThickness = 1
        btnBack.BorderRadius = 12
        btnBack.HoverState.FillColor = Color.FromArgb(240, 238, 232)
        btnBack.TabStop = False
        AddHandler btnBack.Click, Sub()
                                      cashForm.DialogResult = DialogResult.Cancel
                                      cashForm.Close()
                                  End Sub
        actionSection.Controls.Add(btnBack)

        Dim btnCancel As New Guna.UI2.WinForms.Guna2Button()
        btnCancel.Text = "Cancel"
        btnCancel.Size = New Size(120, 50)
        btnCancel.Location = New Point(170, 5)
        btnCancel.Font = New Font("Poppins", 11, FontStyle.Regular)
        btnCancel.ForeColor = Color.FromArgb(200, 70, 70)
        btnCancel.FillColor = Color.FromArgb(255, 245, 245)
        btnCancel.BorderColor = Color.FromArgb(220, 120, 120)
        btnCancel.BorderThickness = 1
        btnCancel.BorderRadius = 12
        btnCancel.HoverState.FillColor = Color.FromArgb(255, 235, 235)
        btnCancel.TabStop = False
        AddHandler btnCancel.Click, Sub()
                                        cashForm.DialogResult = DialogResult.Abort
                                        cashForm.Close()
                                    End Sub
        actionSection.Controls.Add(btnCancel)

        ' ENHANCED: Keyboard support with better decimal validation
        AddHandler cashForm.KeyDown, Sub(sender As Object, e As KeyEventArgs)
                                         ' Handle number keys (0-9)
                                         If (e.KeyCode >= Keys.D0 AndAlso e.KeyCode <= Keys.D9) OrElse
                                     (e.KeyCode >= Keys.NumPad0 AndAlso e.KeyCode <= Keys.NumPad9) Then
                                             Dim digit As String = ""
                                             If e.KeyCode >= Keys.D0 AndAlso e.KeyCode <= Keys.D9 Then
                                                 digit = (e.KeyCode - Keys.D0).ToString()
                                             Else
                                                 digit = (e.KeyCode - Keys.NumPad0).ToString()
                                             End If
                                             ProcessKeypadInputEnhanced(digit, UpdateCashAmountDisplay)
                                             e.Handled = True

                                             ' Handle decimal point
                                         ElseIf e.KeyCode = Keys.Decimal OrElse e.KeyCode = Keys.OemPeriod Then
                                             ProcessKeypadInputEnhanced(".", UpdateCashAmountDisplay)
                                             e.Handled = True

                                             ' Handle backspace/delete
                                         ElseIf e.KeyCode = Keys.Back OrElse e.KeyCode = Keys.Delete Then
                                             ProcessKeypadInputEnhanced("?", UpdateCashAmountDisplay)
                                             e.Handled = True

                                             ' Handle Enter key to complete payment
                                         ElseIf e.KeyCode = Keys.Enter Then
                                             confirmPaymentAction()
                                             e.Handled = True
                                             e.SuppressKeyPress = True

                                             ' Handle Escape to cancel
                                         ElseIf e.KeyCode = Keys.Escape Then
                                             btnBack.PerformClick()
                                             e.Handled = True
                                             e.SuppressKeyPress = True

                                             ' Handle C key for clear
                                         ElseIf e.KeyCode = Keys.C Then
                                             clearAmountAction()
                                             e.Handled = True
                                             e.SuppressKeyPress = True

                                             ' Handle E key for exact amount
                                         ElseIf e.KeyCode = Keys.E Then
                                             applyExactAction()
                                             e.Handled = True
                                             e.SuppressKeyPress = True

                                         End If
                                     End Sub

        ' Initial amount display update
        UpdateCashAmountDisplay()

        ' Show modal and handle result
        Dim result As DialogResult = cashForm.ShowDialog()
        cashForm.Dispose()

        Select Case result
            Case DialogResult.OK
                ' Complete the sale with cash payment
                confirmBtn.PerformClick()

            Case DialogResult.Cancel
                ' Back to payment method selection
                ShowPaymentMethodModal()

            Case DialogResult.Abort
                ' Cancel completely - return to normal state
                Return
        End Select
    End Sub

    ' ENHANCED: Process keypad input with comprehensive decimal validation
    ' Replace the existing ProcessKeypadInputEnhanced with this updated implementation
    Private Sub ProcessKeypadInputEnhanced(input As String, updateCallback As Action)
        ' We enforce exactly 1 decimal place and make '.' put the next digit into the decimal slot
        Select Case input
            Case "?" ' Backspace
                If enteredAmount.Length > 0 Then
                    enteredAmount = enteredAmount.Substring(0, enteredAmount.Length - 1)
                End If

            Case "." ' Decimal point -> ensure there is exactly one decimal separator and position next input after it
                If enteredAmount.Length = 0 Then
                    ' start with 0.
                    enteredAmount = "0."
                ElseIf Not enteredAmount.Contains(".") Then
                    enteredAmount &= "."
                Else
                    ' If decimal already exists, trim any digits after decimal so the next digit will replace them
                    Dim idx As Integer = enteredAmount.IndexOf(".")
                    If idx >= 0 Then
                        enteredAmount = enteredAmount.Substring(0, idx + 1) ' keep trailing dot
                    End If
                End If

            Case "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" ' Digits
                ' If decimal present, allow only 1 digit after it
                If enteredAmount.Contains(".") Then
                    Dim decimalIndex As Integer = enteredAmount.IndexOf(".")
                    Dim decimalPlaces As Integer = enteredAmount.Length - decimalIndex - 1
                    If decimalPlaces >= 1 Then
                        ' already have one decimal digit, ignore further input
                        Return
                    End If

                    ' Append digit after decimal (or if user previously trimmed decimals, this becomes the decimal digit)
                    enteredAmount &= input
                Else
                    ' No decimal yet � append digit to integer portion
                    ' Keep sensible overall length (allows reasonably large amounts)
                    If enteredAmount.Length < 12 Then
                        enteredAmount &= input
                    End If
                End If
        End Select

        updateCallback()
    End Sub
    ' HELPER: Process keypad input consistently
    Private Sub ProcessKeypadInput(input As String, updateCallback As Action)
        Select Case input
            Case "?" ' Backspace
                If enteredAmount.Length > 0 Then
                    enteredAmount = enteredAmount.Substring(0, enteredAmount.Length - 1)
                End If

            Case "." ' Decimal point
                ' Allow only one decimal point and only if there are digits
                If Not enteredAmount.Contains(".") AndAlso enteredAmount.Length > 0 Then
                    enteredAmount &= "."
                End If

            Case "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" ' Digits
                If enteredAmount.Length < 10 Then ' Prevent overflow
                    enteredAmount &= input
                End If
        End Select

        ' Call the update callback
        updateCallback()
    End Sub

    ' REMOVE/COMMENT OUT the old panel-based methods:
    ' - ShowCustomerDetailsPanel()
    ' - ShowPaymentMethodSelectionModal() (the old one)
    ' - ShowReferenceInputPanel()
    ' - ShowCustomerSelectionPanel()
    ' - UpdateCustomerTypeButtons()

    ' Keep the existing confirmBtn_Click, UpdateAmountDisplay, and other core functionality unchanged
    Private Sub UpdateCustomerTypeButtons()
        ' This will be handled by the lambda function in ShowCustomerDetailsPanel
        ' Just here for reference
    End Sub


    Private Sub UpdateAmountDisplay()
        ' Check if lblAmountDisplay exists before using it
        If lblAmountDisplay Is Nothing Then
            Return ' Exit if lblAmountDisplay hasn't been created yet
        End If

        ' Format as decimal with two places
        Dim displayValue As Decimal = 0D
        Dim amountText As String = enteredAmount

        ' Handle decimal formatting
        If amountText.Contains(".") Then
            ' User entered a decimal point
            If Decimal.TryParse(amountText, displayValue) Then
                lblAmountDisplay.Text = displayValue.ToString("F2")
            Else
                lblAmountDisplay.Text = "0.00"
            End If
        Else
            ' No decimal point, treat as cents
            If amountText.Length = 0 Then
                displayValue = 0
            ElseIf Decimal.TryParse(amountText, displayValue) Then
                displayValue = displayValue / 100
            End If
            lblAmountDisplay.Text = displayValue.ToString("F2")
        End If

        If lblAmountDisplay IsNot Nothing AndAlso totalReceivedPanel IsNot Nothing Then
            lblAmountDisplay.Location = New Point((totalReceivedPanel.Width - lblAmountDisplay.Width) / 2, lblAmountDisplay.Location.Y)
        End If

        ' Update totalRLbl in real-time
        If totalRLbl IsNot Nothing Then
            totalRLbl.Text = lblAmountDisplay.Text
        End If

        ' Compute change in real-time - only if an amount has been entered
        If enteredAmount.Length = 0 Then
            ' No amount entered yet, show 0.00 for change
            If lblChange IsNot Nothing Then
                lblChange.Text = "0.00"
                lblChange.ForeColor = SuccessGreen
                lblChange.Visible = True
            End If
        Else
            Dim orderTotal As Decimal = 0D
            If totalLbl IsNot Nothing AndAlso Decimal.TryParse(totalLbl.Text, orderTotal) Then
                Dim changeVal As Decimal = displayValue - orderTotal
                If lblChange IsNot Nothing Then
                    lblChange.Text = changeVal.ToString("F2")
                    lblChange.ForeColor = SuccessGreen
                    lblChange.Visible = True
                End If
            End If
        End If
    End Sub

    ' Create the order-summary grid once and wire its hover/selection/double-click
    ' behavior. Row index maps 1:1 to the currentOrderList index (see PopulateGrid).
    Private Sub EnsureOrderSummaryGrid()
        If _orderSummaryGrid IsNot Nothing Then Return
        _orderSummaryGrid = OrderSummaryGridBuilder.BuildGrid()
        ' Visible rounded gray frame around the table (the panel already has BorderRadius 10)
        orderSummaryPanel.BorderColor = Color.FromArgb(232, 232, 232)
        orderSummaryPanel.BorderThickness = 2
        orderSummaryPanel.Padding = New Padding(2)
        orderSummaryPanel.AutoScroll = False
        orderSummaryPanel.Controls.Add(_orderSummaryGrid)

        ' Empty-cart state panel (sits under the grid, toggled by cart contents)
        If _emptyCartPanel Is Nothing Then
            _emptyCartPanel = EmptyCartStateBuilder.BuildEmptyCartState()
            orderSummaryPanel.Controls.Add(_emptyCartPanel)
        End If

        UpdateOrderSummaryVisibility()

        ' Hover acts as selection: #FBF7EC row highlight follows the mouse
        AddHandler _orderSummaryGrid.CellMouseEnter, Sub(s, e)
                                                         If e.RowIndex < 0 OrElse e.RowIndex >= _orderSummaryGrid.Rows.Count Then Return
                                                         Dim current As DataGridViewCell = _orderSummaryGrid.CurrentCell
                                                         If current Is Nothing OrElse current.RowIndex <> e.RowIndex Then
                                                             _orderSummaryGrid.CurrentCell = _orderSummaryGrid.Rows(e.RowIndex).Cells(0)
                                                         End If
                                                     End Sub
        AddHandler _orderSummaryGrid.MouseLeave, Sub(s, e)
                                                     _orderSummaryGrid.ClearSelection()
                                                 End Sub

        ' Double-click = reduce qty by 1 (Shift+double-click = void line). The
        ' modifier handling lives inside ReduceItemQuantity.
        AddHandler _orderSummaryGrid.CellDoubleClick, Sub(s, e)
                                                          If e.RowIndex < 0 Then Return
                                                          ReduceItemQuantity(e.RowIndex)
                                                      End Sub
    End Sub

    ' Show the empty-cart state when the order has no items, otherwise the grid.
    Private Sub UpdateOrderSummaryVisibility()
        If _orderSummaryGrid Is Nothing OrElse _emptyCartPanel Is Nothing Then Return
        Dim hasItems As Boolean = currentOrderList.Count > 0
        _orderSummaryGrid.Visible = hasItems
        _emptyCartPanel.Visible = Not hasItems
    End Sub

    ' Refresh the order display in the order summary panel
    ' FIXED: Refresh the order display with correct VAT calculations
    ' Refresh the order display in the order summary panel
    ' FIXED: Refresh the order display with correct VAT calculations
    ' Replace the existing RefreshOrderDisplay method with this updated implementation
    Private Sub RefreshOrderDisplay()
        ' Reset change label and totalRLbl when in normal order summary mode
        If Not pinPanelActive AndAlso Not totalPanelActive Then
            If lblChange IsNot Nothing Then
                lblChange.Text = "0.00"
                totalRLbl.Text = "0.00"
            End If
        End If

        EnsureOrderSummaryGrid()
        Dim displayRows As New List(Of OrderSummaryGridBuilder.OrderSummaryRowInfo)()

        ' Ensure every product has an OriginalUnitPrice stored (store VAT-INCLUSIVE unit prices)
        For Each prod In currentOrderList
            If Not prod.ContainsKey("OriginalUnitPrice") Then
                prod("OriginalUnitPrice") = Convert.ToDecimal(prod("Price"))
            End If
        Next

        ' Build UI rows and compute subtotal (VAT-INCLUSIVE) before discount
        Dim subtotalVatInclusiveLocal As Decimal = 0D
        For i = 0 To currentOrderList.Count - 1
            Dim prod = currentOrderList(i)
            Dim unitPriceVatInclusive As Decimal = Convert.ToDecimal(prod("OriginalUnitPrice"))
            Dim qtyInt As Integer = CInt(prod("Quantity"))

            ' Accumulate subtotal (VAT-INCLUSIVE)
            subtotalVatInclusiveLocal += unitPriceVatInclusive * qtyInt

            ' Prepare display values
            Dim displayUnitVatInc As Decimal = unitPriceVatInclusive
            Dim lineTotalVatInclusive As Decimal = unitPriceVatInclusive * qtyInt

            ' If this product received the discount, compute discounted unit price using inclusive math
            If discountType <> "None" AndAlso discountedItemProductId IsNot Nothing Then
                Try
                    Dim prodIdInt As Integer = Convert.ToInt32(prod("ProductID"))
                    If prodIdInt = discountedItemProductId Then
                        If discountType = "Percentage" Then
                            Dim pct As Decimal = discountValue
                            displayUnitVatInc = Math.Round((unitPriceVatInclusive / 1.12D) * (1 - (pct / 100D)) * 1.12D, 2)
                        ElseIf discountType = "Fixed" Then
                            Dim perUnitDiscountVatInc As Decimal = 0D
                            If qtyInt > 0 Then
                                perUnitDiscountVatInc = discountAmount / qtyInt
                            End If
                            Dim perUnitDiscountNet As Decimal = perUnitDiscountVatInc / 1.12D
                            displayUnitVatInc = Math.Round(((unitPriceVatInclusive / 1.12D) - perUnitDiscountNet) * 1.12D, 2)
                            If displayUnitVatInc < 0D Then displayUnitVatInc = 0D
                        End If
                        lineTotalVatInclusive = displayUnitVatInc * qtyInt
                    End If
                Catch
                    ' ignore conversion issues
                End Try
            End If

            ' Build a display row for the grid (VAT-INCLUSIVE, possibly discounted)
            Dim fullProductName As String = prod("ProductName").ToString()
            Dim maxNameLength As Integer = 28
            Dim displayName As String = If(fullProductName.Length > maxNameLength, fullProductName.Substring(0, maxNameLength) & "...", fullProductName)

            ' Tooltip: show original unit price and if discounted, the discounted unit price
            Dim tooltipText As String = $"Unit price (VAT inc): ₱{unitPriceVatInclusive:F2}"
            If displayUnitVatInc <> unitPriceVatInclusive Then
                tooltipText &= $" ? Discounted: ₱{displayUnitVatInc:F2}"
            End If

            Dim rowInfo As New OrderSummaryGridBuilder.OrderSummaryRowInfo()
            rowInfo.Number = (i + 1).ToString("D2")
            rowInfo.DisplayName = displayName
            rowInfo.FullName = fullProductName
            rowInfo.Qty = prod("Quantity").ToString() & "x"
            rowInfo.LineTotal = lineTotalVatInclusive.ToString("N2")
            rowInfo.AmountTooltip = tooltipText
            displayRows.Add(rowInfo)
        Next

        OrderSummaryGridBuilder.PopulateGrid(_orderSummaryGrid, displayRows)

        ' --- DISCOUNT-FIRST CALCULATION (VAT-INCLUSIVE) ---
        Dim discountVatInclusive As Decimal = discountAmount
        Dim remainingVatInclusive As Decimal = Math.Max(0D, subtotalVatInclusiveLocal - discountVatInclusive)
        Dim vatAmount As Decimal = Math.Round(remainingVatInclusive * (0.12D / 1.12D), 2)
        Dim vatableNet As Decimal = Math.Round(remainingVatInclusive - vatAmount, 2)
        Dim totalAmountVatInc As Decimal = Math.Round(remainingVatInclusive, 2)

        ' Persist values for receipt printing and confirm flow:
        Me.receiptVatableBeforeDiscount = subtotalVatInclusiveLocal
        Me.receiptSubtotal = vatableNet
        Me.receiptTax = vatAmount
        Me.receiptTotalAmount = totalAmountVatInc
        Me.subtotalVatInclusive = subtotalVatInclusiveLocal

        ' Update UI labels
        If lblSubTotal IsNot Nothing Then lblSubTotal.Text = vatableNet.ToString("N2")
        If taxLbl IsNot Nothing Then taxLbl.Text = vatAmount.ToString("N2")
        If totalLbl IsNot Nothing Then totalLbl.Text = totalAmountVatInc.ToString("N2")

        ' Toggle empty-cart state vs grid based on whether there are items
        UpdateOrderSummaryVisibility()
    End Sub
    ' FIXED: Enhanced receipt printing with correct VAT breakdown
    ' FIXED: Update the confirmBtn_Click method to set correct receipt values
    Private Sub confirmBtn_Click(sender As Object, e As EventArgs) Handles confirmBtn.Click
        If posLockedForCapital Then
            MessageBox.Show("POS is locked. Manager/Admin must set opening capital first.", "POS Locked", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Try
            If Not ValidateUserSession() Then Return

            ' Use the totals already computed and stored by RefreshOrderDisplay
            Dim orderTotal As Decimal = Me.receiptTotalAmount
            Dim receivedAmount As Decimal = 0D
            If selectedPaymentMethod = "Cash" Then
                If lblAmountDisplay IsNot Nothing Then
                    Dim amountText As String = lblAmountDisplay.Text.Replace("₱", "").Trim()
                    If Not Decimal.TryParse(amountText, receivedAmount) Then
                        MessageBox.Show("Invalid amount entered.", "Payment Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Return
                    End If
                End If
                If receivedAmount < orderTotal Then
                    MessageBox.Show($"Amount received (₱{receivedAmount:F2}) must be greater than or equal to order total (₱{orderTotal:F2}).", "Payment Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If
            Else
                receivedAmount = orderTotal
            End If

            Dim changeAmount As Decimal = receivedAmount - orderTotal

            ' Build sales data snapshot (JSON) for Sales.SalesData column
            Dim salesDataJson As String = ""
            Try
                salesDataJson = Newtonsoft.Json.JsonConvert.SerializeObject(New With {
                .payment = New With {
                    .method = selectedPaymentMethod,
                    .reference = paymentReference,
                    .received = receivedAmount,
                    .change = changeAmount,
                    .discount = New With {.type = discountType, .amount = discountAmount}
                },
                .items = currentOrderList
            })
            Catch
                salesDataJson = "{}"
            End Try

            ' Use a DB transaction to ensure Sale + SaleItems + stock updates + InventoryLog entries are atomic
            Dim connStr As String = Connection.GetConnectionString()
            If String.IsNullOrEmpty(connStr) Then
                Throw New Exception("Database connection string is not configured.")
            End If

            Using conn As New SqliteConnection(connStr)
                conn.Open()
                Using tran = conn.BeginTransaction()
                    Try
                        ' Resolve UserID inside the transaction (ensure non-NULL value for InventoryLog.UserID)
                        Dim userIdToUse As Object = DBNull.Value
                        Using cmdUserCheck As New SqliteCommand("SELECT UserID FROM Users WHERE Username = @Username", conn, tran)
                            cmdUserCheck.Parameters.AddWithValue("@Username", frmLoginvb.LoggedInUsername)
                            Dim uidObj = cmdUserCheck.ExecuteScalar()
                            If uidObj IsNot Nothing AndAlso Not IsDBNull(uidObj) Then
                                userIdToUse = Convert.ToInt32(uidObj)
                            End If
                        End Using

                        ' If still not found, fallback to any existing user (first row)
                        If userIdToUse Is DBNull.Value Then
                            Using cmdFb As New SqliteCommand("SELECT * LIMIT 1 UserID FROM Users ORDER BY UserID", conn, tran)
                                Dim fb = cmdFb.ExecuteScalar()
                                If fb IsNot Nothing AndAlso Not IsDBNull(fb) Then
                                    userIdToUse = Convert.ToInt32(fb)
                                End If
                            End Using
                        End If

                        If userIdToUse Is DBNull.Value Then
                            Throw New Exception("Current user not found in Users table. InventoryLog requires a valid UserID. Please ensure the logged in user exists in Users.")
                        End If

                        ' Insert Sale and get SaleID
                        Dim insertSaleQuery As String =
                    "INSERT INTO Sales (SaleDate, CustomerName, CustomerTIN, TotalAmount, AmountPaid, PaymentMethod, Reference, DiscountAmount, DiscountType, SalesData, UserID) " &
                    "VALUES (@SaleDate, @CustomerName, @CustomerTIN, @TotalAmount, @AmountPaid, @PaymentMethod, @Reference, @DiscountAmount, @DiscountType, @SalesData, @UserID); SELECT last_insert_rowid();"

                        Using cmdSale As New SqliteCommand(insertSaleQuery, conn, tran)
                            cmdSale.Parameters.AddWithValue("@SaleDate", DateTime.Now)
                            cmdSale.Parameters.AddWithValue("@CustomerName", If(String.IsNullOrWhiteSpace(selectedCustomerName), CType(DBNull.Value, Object), CType(selectedCustomerName, Object)))
                            cmdSale.Parameters.AddWithValue("@CustomerTIN", If(String.IsNullOrWhiteSpace(selectedCustomerTIN), CType(DBNull.Value, Object), CType(selectedCustomerTIN, Object)))
                            cmdSale.Parameters.AddWithValue("@TotalAmount", orderTotal)
                            cmdSale.Parameters.AddWithValue("@AmountPaid", receivedAmount)
                            cmdSale.Parameters.AddWithValue("@PaymentMethod", selectedPaymentMethod)
                            cmdSale.Parameters.AddWithValue("@Reference", If(String.IsNullOrWhiteSpace(paymentReference), CType(DBNull.Value, Object), CType(paymentReference, Object)))
                            cmdSale.Parameters.AddWithValue("@DiscountAmount", discountAmount)
                            cmdSale.Parameters.AddWithValue("@DiscountType", discountType)
                            cmdSale.Parameters.AddWithValue("@SalesData", salesDataJson)
                            cmdSale.Parameters.AddWithValue("@UserID", userIdToUse)

                            Dim saleIdObj As Object = cmdSale.ExecuteScalar()
                            Dim saleId As Integer = 0
                            If saleIdObj IsNot Nothing AndAlso Not IsDBNull(saleIdObj) Then
                                saleId = Convert.ToInt32(saleIdObj)
                            Else
                                Throw New Exception("Failed to create sale record.")
                            End If

                            ' Generate a readable, numbers-only SaleNumber for this sale
                            Dim generatedSaleNumber As String = Utilities.FormatSaleNumber(saleId)
                            Using cmdNum As New SqliteCommand("UPDATE Sales SET SaleNumber = @SaleNumber WHERE SaleID = @SaleID", conn, tran)
                                cmdNum.Parameters.AddWithValue("@SaleNumber", generatedSaleNumber)
                                cmdNum.Parameters.AddWithValue("@SaleID", saleId)
                                cmdNum.ExecuteNonQuery()
                            End Using

                            ' For each ordered item: insert SaleItems, update product stock, insert InventoryLog (OUT)
                            For Each item In currentOrderList
                                Dim prodId As Integer = Convert.ToInt32(item("ProductID"))
                                Dim qty As Integer = CInt(item("Quantity"))
                                Dim unitPrice As Decimal = Convert.ToDecimal(item("Price"))

                                ' Insert SaleItem
                                Dim origPrice As Object = Nothing
                                If item.ContainsKey("OriginalUnitPrice") Then
                                    origPrice = Convert.ToDecimal(item("OriginalUnitPrice"))
                                Else
                                    origPrice = unitPrice
                                End If
                                Using cmdItem As New SqliteCommand("INSERT INTO SaleItems (SaleID, ProductID, Quantity, UnitPrice, OriginalUnitPrice) VALUES (@SaleID, @ProductID, @Quantity, @UnitPrice, @OriginalUnitPrice)", conn, tran)
                                    cmdItem.Parameters.AddWithValue("@SaleID", saleId)
                                    cmdItem.Parameters.AddWithValue("@ProductID", prodId)
                                    cmdItem.Parameters.AddWithValue("@Quantity", qty)
                                    cmdItem.Parameters.AddWithValue("@UnitPrice", unitPrice)
                                    cmdItem.Parameters.AddWithValue("@OriginalUnitPrice", origPrice)
                                    cmdItem.ExecuteNonQuery()
                                End Using

                                ' Read previous stock (source of truth)
                                Dim previousStock As Integer = 0
                                Using cmdPrev As New SqliteCommand("SELECT IFNULL(CurrentStock, 0) FROM Products WHERE ProductID = @ProductID", conn, tran)
                                    cmdPrev.Parameters.AddWithValue("@ProductID", prodId)
                                    Dim prevObj = cmdPrev.ExecuteScalar()
                                    If prevObj IsNot Nothing AndAlso Not IsDBNull(prevObj) Then
                                        previousStock = Convert.ToInt32(prevObj)
                                    End If
                                End Using

                                ' Decrease stock atomically; block the sale when there is not enough stock
                                ' so stock can never be driven negative by a stale cart or concurrent write.
                                Dim newStock As Integer = previousStock - qty
                                Using cmdUpdate As New SqliteCommand("UPDATE Products SET CurrentStock = CurrentStock - @Qty WHERE ProductID = @ProductID AND CurrentStock >= @Qty", conn, tran)
                                    cmdUpdate.Parameters.AddWithValue("@Qty", qty)
                                    cmdUpdate.Parameters.AddWithValue("@ProductID", prodId)
                                    If cmdUpdate.ExecuteNonQuery() = 0 Then
                                        Dim productName As String = If(item.ContainsKey("ProductName"), item("ProductName").ToString(), "Product #" & prodId)
                                        Throw New Exception($"Insufficient stock for ""{productName}"". Available: {previousStock}, requested: {qty}. The sale was not completed.")
                                    End If
                                End Using

                                ' Insert InventoryLog entry (OUT) � use 'OUT' to satisfy CHECK constraint
                                Dim insertLogQuery As String =
                            "INSERT INTO InventoryLog (ProductID, TransactionType, Quantity, PreviousStock, NewStock, SupplierID, Reference, Notes, UserID, CreatedAt) " &
                            "VALUES (@ProductID, @TransactionType, @Quantity, @PreviousStock, @NewStock, @SupplierID, @Reference, @Notes, @UserID, @CreatedAt)"
                                Using cmdLog As New SqliteCommand(insertLogQuery, conn, tran)
                                    cmdLog.Parameters.AddWithValue("@ProductID", prodId)
                                    ' IMPORTANT: use 'OUT' (not 'Stock Out') to conform to InventoryLog CHECK constraint
                                    cmdLog.Parameters.AddWithValue("@TransactionType", "OUT")
                                    cmdLog.Parameters.AddWithValue("@Quantity", qty)
                                    cmdLog.Parameters.AddWithValue("@PreviousStock", previousStock)
                                    cmdLog.Parameters.AddWithValue("@NewStock", newStock)
                                    cmdLog.Parameters.AddWithValue("@SupplierID", DBNull.Value)
                                    cmdLog.Parameters.AddWithValue("@Reference", $"Sale ID:{saleId}")
                                    cmdLog.Parameters.AddWithValue("@Notes", $"Sold via POS - SaleID {saleId}")
                                    cmdLog.Parameters.AddWithValue("@UserID", userIdToUse)
                                    cmdLog.Parameters.AddWithValue("@CreatedAt", DateTime.Now)
                                    cmdLog.ExecuteNonQuery()
                                End Using
                            Next

                            ' Commit transaction after all items processed
                            tran.Commit()

                            ' Notify the background sync queue that new data is available
                            Try
                                SyncQueue.Instance.MarkDataChanged()
                            Catch syncEx As Exception
                                Console.WriteLine($"Could not notify sync queue: {syncEx.Message}")
                            End Try

                            ' Log audit (outside transaction is fine)
                            Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Sale Created", $"SaleID {saleId} created. Total ₱{orderTotal:F2}")

                            ' Prepare receipt data (use values computed by RefreshOrderDisplay)
                            receiptOrderId = generatedSaleNumber
                            receiptCustomerName = If(String.IsNullOrWhiteSpace(selectedCustomerName), "", selectedCustomerName)
                            receiptTotalAmount = Me.receiptTotalAmount
                            receiptAmountReceived = receivedAmount
                            receiptChange = changeAmount
                            receiptItems = New List(Of Dictionary(Of String, Object))(currentOrderList)

                            ' Print and finalize
                            PrintReceipt()

                            ' Confirm the sale with an auto-dismissing toast
                            ShowSaleCreatedNotification(generatedSaleNumber, orderTotal)

                            ' Reset for next transaction
                            ResetSale()
                        End Using

                    Catch exTran As Exception
                        Try
                            tran.Rollback()
                        Catch
                            ' ignore rollback errors
                        End Try
                        Throw ' rethrow to outer catch
                    End Try
                End Using
            End Using

        Catch ex As Exception
            MessageBox.Show($"Error processing sale: {ex.Message}", "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Sale Failed", $"Error: {ex.Message}")
        End Try
    End Sub
    ' Reset sale data for next transaction
    ' ENHANCED: Reset sale data for next transactio    n with proper order panel refresh
    Private Sub ResetSale()
        ' Inside ResetSale(), add:
        ClearPersistedCartState()
        ' Clear order
        currentOrderList.Clear()

        ' Reset customer info
        selectedCustomerId = Nothing
        selectedCustomerName = ""
        selectedCustomerPhone = ""
        selectedCustomerEmail = ""
        selectedCustomerType = "Walk-in"

        ' Reset payment info
        selectedPaymentMethod = "Cash"
        paymentReference = ""

        ' Reset discount
        discountAmount = 0
        discountType = "None"
        discountValue = 0

        ' ENHANCED: Reset UI elements and refresh order display
        If lblSubTotal IsNot Nothing Then lblSubTotal.Text = "0.00"
        If taxLbl IsNot Nothing Then taxLbl.Text = "0.00"
        If totalLbl IsNot Nothing Then totalLbl.Text = "0.00"
        If totalRLbl IsNot Nothing Then totalRLbl.Text = "0.00"
        If lblChange IsNot Nothing Then lblChange.Text = "0.00"

        ' Clear product cards
        For Each card In productCardControls
            If card IsNot Nothing Then
                card.Dispose()
            End If
        Next
        productCardControls.Clear()

        ' Reset category panel to initial state
        CategoryPanel.Controls.Clear()
        _paginationContext = PaginationContext.None
        BuildCategoryTiles()
        ArrangeCategoryButtonsFlexWrap()
        UpdateCategoryItemCounts()

        ' CRITICAL: Clear the order summary panel and refresh display
        ' Clear the order summary grid (rows) before starting a new order
        If _orderSummaryGrid IsNot Nothing Then
            _orderSummaryGrid.Rows.Clear()
        End If

        ' ENHANCED: Initialize next order ID and refresh order display
        InitializeOrderId()
        RefreshOrderDisplay()

        ' Clear any open panels
        If pinPanel IsNot Nothing AndAlso Me.Controls.Contains(pinPanel) Then
            Me.Controls.Remove(pinPanel)
            pinPanel.Dispose()
            pinPanel = Nothing
        End If

        If totalReceivedPanel IsNot Nothing AndAlso Me.Controls.Contains(totalReceivedPanel) Then
            Me.Controls.Remove(totalReceivedPanel)
            totalReceivedPanel.Dispose()
            totalReceivedPanel = Nothing
        End If

        If customerSelectionPanel IsNot Nothing AndAlso Me.Controls.Contains(customerSelectionPanel) Then
            Me.Controls.Remove(customerSelectionPanel)
            customerSelectionPanel.Dispose()
            customerSelectionPanel = Nothing
        End If

        ' ENHANCED: Reset panel states and button visibility
        pinPanelActive = False
        totalPanelActive = False

        ' Ensure payment and confirm buttons are in correct state
        If btnPayment IsNot Nothing Then btnPayment.Visible = True
        If confirmBtn IsNot Nothing Then confirmBtn.Visible = False

        ' ENHANCED: Refresh UI and bring order panel to front
        If orderSummaryPanel IsNot Nothing Then
            orderSummaryPanel.Visible = True
            orderSummaryPanel.BringToFront()
            orderSummaryPanel.Refresh()
        End If

        ' Return to categories view
        If LabelTitle IsNot Nothing Then LabelTitle.Text = "Categories"
        If backCategory IsNot Nothing Then backCategory.Visible = False

        ' ENHANCED: Reset scroll positions
        If CategoryPanel IsNot Nothing Then
            CategoryPanel.AutoScrollPosition = New Point(0, 0)
            CategoryPanel.Refresh()
        End If

        ' Re-enable form controls and focus
        Me.Enabled = True
        FocusBarcodeInputIfAllowed()

        ' ENHANCED: Force a complete UI refresh
        Me.Refresh()
        Application.DoEvents()

        ' Log the reset action
        Console.WriteLine($"Sale reset completed. Next Order ID: {lblOrderId?.Text}")
    End Sub

    ' ENHANCED: Initialize order ID display with better error handling
    Private Sub InitializeOrderId()
        Try
            Dim nextOrderId As Integer = 1
            Dim query As String = "SELECT IFNULL(MAX(SaleID), 0) + 1 AS NextOrderID FROM Sales"
            Using reader As DbDataReader = Utilities.ExecuteReader(query, New SqlParameter() {})
                If reader.Read() Then
                    nextOrderId = Convert.ToInt32(reader("NextOrderID"))
                End If
            End Using

            ' ENHANCED: Update order ID display with proper formatting
            If lblOrderId IsNot Nothing Then
                lblOrderId.Text = $"Sale ID: {nextOrderId}"
                lblOrderId.Refresh()
            End If

            ' Log the new order ID for debugging
            Console.WriteLine($"Initialized next Order ID: {nextOrderId}")
        Catch ex As Exception
            ' Fallback to default if database error
            If lblOrderId IsNot Nothing Then
                lblOrderId.Text = "Sale ID: 1"
            End If
            Console.WriteLine($"Error initializing Order ID: {ex.Message}")
        End Try
    End Sub

    ' Discount button click handler
    Private Sub btnDiscount_Click(sender As Object, e As EventArgs) Handles btnDiscount.Click
        ' Only allow discount if there are items in the order
        If currentOrderList.Count = 0 Then
            MessageBox.Show("Please add items to the order before applying a discount.", "No Items", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Show simple discount dialog
        ShowSimpleDiscountDialog()
    End Sub

    ' Simple discount dialog - modern Guna2 style
    Private Sub ShowSimpleDiscountDialog()
        Dim discountForm As New Form()
        discountForm.Text = "Apply Discount"
        discountForm.Size = New Size(440, 300)
        discountForm.StartPosition = FormStartPosition.CenterParent
        discountForm.FormBorderStyle = FormBorderStyle.FixedDialog
        discountForm.MaximizeBox = False
        discountForm.MinimizeBox = False
        discountForm.BackColor = Color.FromArgb(248, 247, 242)

        Dim labelColor As Color = Color.FromArgb(95, 95, 95)

        ' --- Percentage discount row ---
        Dim lblPercentage As New Label()
        lblPercentage.Text = "Percentage Discount (%):"
        lblPercentage.Location = New Point(25, 25)
        lblPercentage.Size = New Size(170, 30)
        lblPercentage.Font = New Font("Poppins", 10, FontStyle.Regular)
        lblPercentage.ForeColor = labelColor
        lblPercentage.TextAlign = ContentAlignment.MiddleLeft
        discountForm.Controls.Add(lblPercentage)

        Dim txtPercentage As New Guna.UI2.WinForms.Guna2TextBox()
        txtPercentage.Text = "0"
        txtPercentage.Location = New Point(200, 25)
        txtPercentage.Size = New Size(80, 32)
        txtPercentage.Font = New Font("Poppins", 10, FontStyle.Bold)
        txtPercentage.ForeColor = labelColor
        txtPercentage.FillColor = Color.FromArgb(250, 249, 246)
        txtPercentage.BorderColor = Color.FromArgb(200, 198, 192)
        txtPercentage.BorderThickness = 1
        txtPercentage.BorderRadius = 8
        txtPercentage.TextAlign = HorizontalAlignment.Center
        discountForm.Controls.Add(txtPercentage)

        Dim btnApplyPct As New Guna.UI2.WinForms.Guna2Button()
        btnApplyPct.Text = "Apply %"
        btnApplyPct.Size = New Size(100, 34)
        btnApplyPct.Location = New Point(295, 24)
        btnApplyPct.Font = New Font("Poppins", 10, FontStyle.Bold)
        btnApplyPct.ForeColor = Color.White
        btnApplyPct.FillColor = Color.FromArgb(76, 175, 80)
        btnApplyPct.BorderThickness = 0
        btnApplyPct.BorderRadius = 8
        btnApplyPct.HoverState.FillColor = Color.FromArgb(67, 160, 71)
        btnApplyPct.PressedColor = Color.FromArgb(56, 142, 60)
        AddHandler btnApplyPct.Click, Sub()
                                          Dim percentage As Decimal
                                          If Decimal.TryParse(txtPercentage.Text, percentage) Then
                                              ApplyPercentageDiscount(percentage)
                                              discountForm.Close()
                                          Else
                                              MessageBox.Show("Please enter a valid percentage.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                          End If
                                      End Sub
        discountForm.Controls.Add(btnApplyPct)

        ' --- Fixed discount row ---
        Dim lblFixed As New Label()
        lblFixed.Text = "Fixed Discount (₱):"
        lblFixed.Location = New Point(25, 75)
        lblFixed.Size = New Size(170, 30)
        lblFixed.Font = New Font("Poppins", 10, FontStyle.Regular)
        lblFixed.ForeColor = labelColor
        lblFixed.TextAlign = ContentAlignment.MiddleLeft
        discountForm.Controls.Add(lblFixed)

        Dim txtFixed As New Guna.UI2.WinForms.Guna2TextBox()
        txtFixed.Text = "0.00"
        txtFixed.Location = New Point(200, 75)
        txtFixed.Size = New Size(80, 32)
        txtFixed.Font = New Font("Poppins", 10, FontStyle.Bold)
        txtFixed.ForeColor = labelColor
        txtFixed.FillColor = Color.FromArgb(250, 249, 246)
        txtFixed.BorderColor = Color.FromArgb(200, 198, 192)
        txtFixed.BorderThickness = 1
        txtFixed.BorderRadius = 8
        txtFixed.TextAlign = HorizontalAlignment.Center
        discountForm.Controls.Add(txtFixed)

        Dim btnApplyFixed As New Guna.UI2.WinForms.Guna2Button()
        btnApplyFixed.Text = "Apply ₱"
        btnApplyFixed.Size = New Size(100, 34)
        btnApplyFixed.Location = New Point(295, 74)
        btnApplyFixed.Font = New Font("Poppins", 10, FontStyle.Bold)
        btnApplyFixed.ForeColor = Color.White
        btnApplyFixed.FillColor = Color.FromArgb(0, 120, 212)
        btnApplyFixed.BorderThickness = 0
        btnApplyFixed.BorderRadius = 8
        btnApplyFixed.HoverState.FillColor = Color.FromArgb(0, 102, 190)
        btnApplyFixed.PressedColor = Color.FromArgb(0, 85, 160)
        AddHandler btnApplyFixed.Click, Sub()
                                            Dim amount As Decimal
                                            If Decimal.TryParse(txtFixed.Text, amount) Then
                                                ApplyFixedDiscount(amount)
                                                discountForm.Close()
                                            Else
                                                MessageBox.Show("Please enter a valid amount.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                            End If
                                        End Sub
        discountForm.Controls.Add(btnApplyFixed)

        ' --- Current discount info ---
        If discountAmount > 0 Then
            Dim lblCurrentDiscount As New Label()
            lblCurrentDiscount.Text = $"Current discount: {discountType} ₱{discountAmount:F2}"
            lblCurrentDiscount.Location = New Point(25, 125)
            lblCurrentDiscount.Size = New Size(350, 25)
            lblCurrentDiscount.Font = New Font("Poppins", 9, FontStyle.Italic)
            lblCurrentDiscount.ForeColor = labelColor
            discountForm.Controls.Add(lblCurrentDiscount)
        End If

        ' --- Bottom row: Remove Discount + Close side by side ---
        Dim btnRemoveDiscount As New Guna.UI2.WinForms.Guna2Button()
        btnRemoveDiscount.Text = "Remove Discount"
        btnRemoveDiscount.Size = New Size(170, 40)
        btnRemoveDiscount.Location = New Point(40, 170)
        btnRemoveDiscount.Font = New Font("Poppins", 10, FontStyle.Bold)
        btnRemoveDiscount.ForeColor = Color.FromArgb(200, 70, 70)
        btnRemoveDiscount.FillColor = Color.FromArgb(255, 245, 245)
        btnRemoveDiscount.BorderColor = Color.FromArgb(220, 120, 120)
        btnRemoveDiscount.BorderThickness = 1
        btnRemoveDiscount.BorderRadius = 10
        btnRemoveDiscount.HoverState.FillColor = Color.FromArgb(255, 235, 235)
        btnRemoveDiscount.PressedColor = Color.FromArgb(255, 220, 220)
        AddHandler btnRemoveDiscount.Click, Sub()
                                                RemoveDiscount()
                                                discountForm.Close()
                                            End Sub
        discountForm.Controls.Add(btnRemoveDiscount)

        Dim btnClose As New Guna.UI2.WinForms.Guna2Button()
        btnClose.Text = "Close"
        btnClose.Size = New Size(170, 40)
        btnClose.Location = New Point(230, 170)
        btnClose.Font = New Font("Poppins", 10, FontStyle.Regular)
        btnClose.ForeColor = labelColor
        btnClose.FillColor = Color.FromArgb(250, 249, 246)
        btnClose.BorderColor = Color.FromArgb(200, 198, 192)
        btnClose.BorderThickness = 1
        btnClose.BorderRadius = 10
        btnClose.HoverState.FillColor = Color.FromArgb(240, 238, 232)
        AddHandler btnClose.Click, Sub()
                                       discountForm.Close()
                                   End Sub
        discountForm.Controls.Add(btnClose)

        ' Keyboard support
        discountForm.KeyPreview = True
        AddHandler discountForm.KeyDown, Sub(sender As Object, e As KeyEventArgs)
                                             If e.KeyCode = Keys.Escape Then
                                                 discountForm.Close()
                                                 e.Handled = True
                                             End If
                                         End Sub

        discountForm.ShowDialog()
    End Sub

    ' Apply percentage discount
    ' Modified: ApplyPercentageDiscount - record which item was discounted
    Private Sub ApplyPercentageDiscount(percentage As Decimal)
        If currentOrderList.Count = 0 Then
            MessageBox.Show("No items in the order to apply a discount.", "No Items", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If percentage < 0 Or percentage > 100 Then
            MessageBox.Show("Discount percentage must be between 0 and 100.", "Invalid Discount", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Find highest line total (VAT-inclusive) and apply percentage only to that item
        Dim highestLineTotal As Decimal = 0D
        Dim highestItemName As String = ""
        Dim highestProductId As Integer = -1
        Dim highestQty As Integer = 1
        Dim highestUnitVatInc As Decimal = 0D

        For Each item In currentOrderList
            Dim price As Decimal = Convert.ToDecimal(item("Price"))
            Dim qty As Integer = CInt(item("Quantity"))
            Dim lineTotal As Decimal = price * qty
            If lineTotal > highestLineTotal Then
                highestLineTotal = lineTotal
                highestItemName = item("ProductName").ToString()
                highestQty = qty
                highestUnitVatInc = price
                If item.ContainsKey("ProductID") Then
                    Integer.TryParse(item("ProductID").ToString(), highestProductId)
                End If
            End If
        Next

        If highestLineTotal <= 0D Then
            MessageBox.Show("Highest item total is zero. Cannot apply discount.", "Invalid Discount", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        discountType = "Percentage"
        discountValue = percentage

        ' Compute discounted unit price using VAT-inclusive -> net -> discount -> VAT re-apply formula:
        ' discountedUnitVatInc = Round((unitVatInc / 1.12) * (1 - p) * 1.12, 2)
        Dim discountedUnitVatInc As Decimal = Math.Round((highestUnitVatInc / 1.12D) * (1 - (percentage / 100D)) * 1.12D, 2)

        ' discount amount (VAT-inclusive) applied to this line
        discountAmount = Math.Round((highestUnitVatInc - discountedUnitVatInc) * highestQty, 2)

        ' Record which item received the discount
        discountedItemProductId = If(highestProductId > 0, CType(highestProductId, Integer?), Nothing)
        discountedItemName = highestItemName

        RefreshOrderDisplay()

        MessageBox.Show($"Applied {percentage}% discount on highest item: '{highestItemName}' (₱{discountAmount:F2})", "Discount Applied", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub
    ' Modified: ApplyFixedDiscount - record which item was discounted
    Private Sub ApplyFixedDiscount(amount As Decimal)
        If currentOrderList.Count = 0 Then
            MessageBox.Show("No items in the order to apply a discount.", "No Items", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If amount < 0 Then
            MessageBox.Show("Discount amount must be a positive value.", "Invalid Discount", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Find highest line total (VAT-inclusive) and apply fixed discount only to that item
        Dim highestLineTotal As Decimal = 0D
        Dim highestItemName As String = ""
        Dim highestProductId As Integer = -1
        Dim highestQty As Integer = 1
        Dim highestUnitVatInc As Decimal = 0D

        For Each item In currentOrderList
            Dim price As Decimal = Convert.ToDecimal(item("Price"))
            Dim qty As Integer = CInt(item("Quantity"))
            Dim lineTotal As Decimal = price * qty
            If lineTotal > highestLineTotal Then
                highestLineTotal = lineTotal
                highestItemName = item("ProductName").ToString()
                highestQty = qty
                highestUnitVatInc = price
                If item.ContainsKey("ProductID") Then
                    Integer.TryParse(item("ProductID").ToString(), highestProductId)
                End If
            End If
        Next

        If highestLineTotal <= 0D Then
            MessageBox.Show("Highest item total is zero. Cannot apply discount.", "Invalid Discount", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Cap fixed discount to highest line total
        Dim appliedAmount As Decimal = Math.Min(amount, highestLineTotal)

        discountType = "Fixed"
        discountValue = appliedAmount
        discountAmount = Math.Round(appliedAmount, 2)

        ' Compute discounted unit VAT-inclusive price by converting discount to net then reapplying VAT per unit:
        ' perUnitDiscountNet = (discountAmount / 1.12) / qty
        ' discountedUnitVatInc = Round((unitVatInc / 1.12 - perUnitDiscountNet) * 1.12, 2)
        Dim perUnitDiscountNet As Decimal = (discountAmount / 1.12D) / Math.Max(1, highestQty)
        Dim discountedUnitVatIncCalc As Decimal = Math.Round(((highestUnitVatInc / 1.12D) - perUnitDiscountNet) * 1.12D, 2)
        If discountedUnitVatIncCalc < 0D Then
            discountedUnitVatIncCalc = 0D
        End If

        ' Record which item received the discount
        discountedItemProductId = If(highestProductId > 0, CType(highestProductId, Integer?), Nothing)
        discountedItemName = highestItemName

        RefreshOrderDisplay()

        If appliedAmount < amount Then
            MessageBox.Show($"Fixed discount exceeded highest item total and was capped to ₱{discountAmount:F2} on '{highestItemName}'.", "Discount Applied (Capped)", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Else
            MessageBox.Show($"Applied fixed discount of ₱{discountAmount:F2} on highest item: '{highestItemName}'", "Discount Applied", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub
    ' Remove discount
    ' Modified: RemoveDiscount - clear recorded discounted item
    Private Sub RemoveDiscount()
        discountType = "None"
        discountValue = 0
        discountAmount = 0
        discountedItemProductId = Nothing
        discountedItemName = ""
        RefreshOrderDisplay()

        MessageBox.Show("Discount removed", "Discount Removed", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub InitializeProfileSection()
        ProfileManager.InitializeProfile(Me, lblUsername, Guna2CirclePictureBox5, AddressOf NavigateToProfileSettings)
    End Sub
    ' Navigation event handlers
    Private Sub NavDashboard_Click(sender As Object, e As EventArgs)
        PersistCartState()

        isNavigating = True
        Dashboard.Show()
        Me.Close()
    End Sub

    Private Sub NavInventory_Click(sender As Object, e As EventArgs)
        PersistCartState()

        isNavigating = True
        Inventory.Show()
        Me.Close()
    End Sub

    Private Sub NavSalesRecords_Click(sender As Object, e As EventArgs)

        Try
            isNavigating = True


            PersistCartState()

            ' Open SalesRecord form
            Dim salesRecordForm As New SalesRecord()
            salesRecordForm.Show()

            ' Close current form
            Me.Close()
        Catch ex As Exception
            isNavigating = False
            MessageBox.Show($"Unable to open Sales Records: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub NavStaff_Click(sender As Object, e As EventArgs)
        PersistCartState()

        isNavigating = True
        Staff.Show()
        Me.Close()
    End Sub

    Private Sub NavInventoryLog_Click(sender As Object, e As EventArgs)
        PersistCartState()

        isNavigating = True
        InventoryLog.Show()
        Me.Close()
    End Sub



    Private Sub NavigateToProfileSettings()
        PersistCartState()

        Try
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Navigation", "Navigated from Sales to ProfileSettings")
            End If

            ' Navigate to ProfileSettings form
            isNavigating = True

            ' If the ProfileSettings form exists in the project, open it.
            ' Use Show() so the user can return; close Sales to mimic existing navigation behavior.
            Dim profileForm As New ProfileSettings()
            profileForm.Show()

            ' Close or hide the Sales form to match other navigation handlers
            Me.Close()
        Catch ex As Exception
            isNavigating = False
            MessageBox.Show($"Unable to open Profile Settings: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub HandleTotalAmountKeyboardInput(e As KeyEventArgs)
        ' Handle numeric keys (0-9)
        If (e.KeyCode >= Keys.D0 AndAlso e.KeyCode <= Keys.D9) OrElse
           (e.KeyCode >= Keys.NumPad0 AndAlso e.KeyCode <= Keys.NumPad9) Then

            Dim digit As String = ""
            If e.KeyCode >= Keys.D0 AndAlso e.KeyCode <= Keys.D9 Then
                digit = (e.KeyCode - Keys.D0).ToString()
            Else
                digit = (e.KeyCode - Keys.NumPad0).ToString()
            End If

            If enteredAmount.Length < 10 Then
                enteredAmount &= digit
                UpdateAmountDisplay()
            End If
            e.Handled = True

            ' Handle backspace
        ElseIf e.KeyCode = Keys.Back Then
            If enteredAmount.Length > 0 Then
                enteredAmount = enteredAmount.Substring(0, enteredAmount.Length - 1)
                UpdateAmountDisplay()
            End If
            e.Handled = True

            ' Handle Enter key to confirm payment
        ElseIf e.KeyCode = Keys.Enter Then
            confirmBtn.PerformClick()
            e.Handled = True

            ' Handle Escape to go back to PIN panel
        ElseIf e.KeyCode = Keys.Escape Then
            If totalReceivedPanel IsNot Nothing AndAlso Me.Controls.Contains(totalReceivedPanel) Then
                Me.Controls.Remove(totalReceivedPanel)
                totalPanelActive = False
            End If
            ' Reset change label and totalRLbl when going back to PIN panel
            If lblChange IsNot Nothing Then
                lblChange.Text = "0.00"
                totalRLbl.Text = "0.00"
            End If
            e.Handled = True
        End If
    End Sub
    ' Add this method to handle form-level key events
    ' FIXED: Enhanced barcode scanning with proper key handling and Enter key priority

    Private Sub Guna2HtmlLabel17_Click(sender As Object, e As EventArgs) Handles Guna2HtmlLabel17.Click

    End Sub


    ' ENHANCED: Wider quantity selector form for better usability
    ' ENHANCED: Wider quantity selector form for better usability
    Private Sub ShowQuantitySelector(productData As Dictionary(Of String, Object))
        ' Prevent product clicks when customer selection or payment panels are active
        If pinPanelActive OrElse totalPanelActive Then
            Return ' Exit without showing selector
        End If
        If posLockedForCapital Then
            MessageBox.Show("POS is locked. Manager/Admin must set opening capital first.", "POS Locked", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        ' Check if the product stock is 0 (DB stock minus reserved in cart)
        Dim qtyProdId As String = productData("ProductID").ToString()
        Dim qtyDbStock As Integer = If(productDbStock.ContainsKey(qtyProdId), productDbStock(qtyProdId), If(productData.ContainsKey("CurrentStock"), CInt(productData("CurrentStock")), 0))
        Dim qtyReserved As Integer = 0
        For Each item In currentOrderList
            If item("ProductID").ToString() = qtyProdId Then
                qtyReserved += CInt(item("Quantity"))
            End If
        Next
        If Math.Max(0, qtyDbStock - qtyReserved) = 0 Then
            MessageBox.Show("This product is out of stock and cannot be added to the order.", "Out of Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return ' Exit without showing selector
        End If

        ' Create quantity selector modal form - WIDENED
        Dim quantityForm As New Form()
        quantityForm.Text = "Select Quantity"
        quantityForm.Size = New Size(550, 450) ' INCREASED from 400x350 to 550x400
        quantityForm.StartPosition = FormStartPosition.CenterParent
        quantityForm.FormBorderStyle = FormBorderStyle.FixedDialog
        quantityForm.MaximizeBox = False
        quantityForm.MinimizeBox = False
        quantityForm.BackColor = OffWhite
        quantityForm.ShowInTaskbar = False
        quantityForm.KeyPreview = True ' Enable keyboard input for the form

        ' Product name - WIDER
        Dim lblProductName As New Label()
        lblProductName.Text = productData("ProductName").ToString()
        lblProductName.Font = New Font("Poppins", 16, FontStyle.Bold) ' Increased font size
        lblProductName.ForeColor = Color.FromArgb(95, 95, 95)
        lblProductName.Location = New Point(30, 25) ' Adjusted margins
        lblProductName.Size = New Size(490, 35) ' WIDER
        lblProductName.TextAlign = ContentAlignment.MiddleCenter
        quantityForm.Controls.Add(lblProductName)

        ' Product price - WIDER
        Dim lblPrice As New Label()
        lblPrice.Text = $"Price: ₱{Convert.ToDecimal(productData("Price")):N2}"
        lblPrice.Font = New Font("Poppins", 14, FontStyle.Regular) ' Increased font size
        lblPrice.ForeColor = Color.FromArgb(95, 95, 95)
        lblPrice.Location = New Point(30, 70)
        lblPrice.Size = New Size(490, 30) ' WIDER
        lblPrice.TextAlign = ContentAlignment.MiddleCenter
        quantityForm.Controls.Add(lblPrice)

        ' Stock information - WIDER (DB stock minus what's already in cart)
        Dim qtyId As String = productData("ProductID").ToString()
        Dim qtyRawStock As Integer = If(productDbStock.ContainsKey(qtyId), productDbStock(qtyId), If(productData.ContainsKey("CurrentStock"), CInt(productData("CurrentStock")), 0))
        Dim qtyAlreadyReserved As Integer = 0
        For Each item In currentOrderList
            If item("ProductID").ToString() = qtyId Then
                qtyAlreadyReserved += CInt(item("Quantity"))
            End If
        Next
        Dim availableStock As Integer = Math.Max(0, qtyRawStock - qtyAlreadyReserved)
        Dim lblStock As New Label()
        lblStock.Text = $"Available Stock: {availableStock}"
        lblStock.Font = New Font("Poppins", 12, FontStyle.Regular) ' Increased font size
        lblStock.ForeColor = If(availableStock > 0, SuccessGreen, AlertRed)
        lblStock.Location = New Point(30, 110)
        lblStock.Size = New Size(490, 25) ' WIDER
        lblStock.TextAlign = ContentAlignment.MiddleCenter
        quantityForm.Controls.Add(lblStock)

        ' Quantity section - CENTERED with more space
        Dim quantitySection As New Panel()
        quantitySection.Size = New Size(490, 80)
        quantitySection.Location = New Point(30, 150)
        quantitySection.BackColor = Color.Transparent
        quantityForm.Controls.Add(quantitySection)

        ' Quantity label - CENTERED
        Dim lblQuantity As New Label()
        lblQuantity.Text = "Quantity:"
        lblQuantity.Font = New Font("Poppins", 14, FontStyle.Bold) ' Increased font size
        lblQuantity.ForeColor = Color.FromArgb(95, 95, 95)
        lblQuantity.Location = New Point(180, 5) ' CENTERED
        lblQuantity.Size = New Size(130, 30)
        lblQuantity.TextAlign = ContentAlignment.MiddleCenter
        quantitySection.Controls.Add(lblQuantity)

        ' Quantity input - LARGER and CENTERED
        Dim txtQuantity As New Guna.UI2.WinForms.Guna2TextBox()
        txtQuantity.Text = "1"
        txtQuantity.Font = New Font("Poppins", 16, FontStyle.Bold) ' Increased font size
        txtQuantity.ForeColor = DarkText
        txtQuantity.FillColor = PureWhite
        txtQuantity.BorderColor = BorderGray
        txtQuantity.BorderRadius = 10
        txtQuantity.Size = New Size(100, 45) ' LARGER
        txtQuantity.Location = New Point(195, 35) ' CENTERED
        txtQuantity.TextAlign = HorizontalAlignment.Center
        txtQuantity.MaxLength = 3
        quantitySection.Controls.Add(txtQuantity)

        ' Plus button - LARGER and repositioned
        Dim btnPlus As New Guna.UI2.WinForms.Guna2Button()
        btnPlus.Text = "+"
        btnPlus.Size = New Size(60, 45) ' LARGER
        btnPlus.Location = New Point(310, 35) ' Adjusted position
        btnPlus.Font = New Font("Poppins", 18, FontStyle.Bold) ' Increased font size
        btnPlus.ForeColor = Color.FromArgb(95, 95, 95)
        btnPlus.FillColor = Color.FromArgb(250, 249, 246)
        btnPlus.BorderColor = Color.FromArgb(200, 198, 192)
        btnPlus.BorderThickness = 1
        btnPlus.BorderRadius = 10
        btnPlus.HoverState.FillColor = Color.FromArgb(240, 238, 232)
        AddHandler btnPlus.Click, Sub()
                                      Dim currentQty As Integer
                                      If Integer.TryParse(txtQuantity.Text, currentQty) Then
                                          If currentQty < availableStock Then
                                              txtQuantity.Text = (currentQty + 1).ToString()
                                          End If
                                      End If
                                  End Sub
        quantitySection.Controls.Add(btnPlus)

        ' Minus button - LARGER and repositioned
        Dim btnMinus As New Guna.UI2.WinForms.Guna2Button()
        btnMinus.Text = "-"
        btnMinus.Size = New Size(60, 45) ' LARGER
        btnMinus.Location = New Point(120, 35) ' Adjusted position
        btnMinus.Font = New Font("Poppins", 18, FontStyle.Bold) ' Increased font size
        btnMinus.ForeColor = Color.FromArgb(95, 95, 95)
        btnMinus.FillColor = Color.FromArgb(250, 249, 246)
        btnMinus.BorderColor = Color.FromArgb(200, 198, 192)
        btnMinus.BorderThickness = 1
        btnMinus.BorderRadius = 10
        btnMinus.HoverState.FillColor = Color.FromArgb(240, 238, 232)
        AddHandler btnMinus.Click, Sub()
                                       Dim currentQty As Integer
                                       If Integer.TryParse(txtQuantity.Text, currentQty) Then
                                           If currentQty > 1 Then
                                               txtQuantity.Text = (currentQty - 1).ToString()
                                           End If
                                       End If
                                   End Sub
        quantitySection.Controls.Add(btnMinus)

        ' Total price display - WIDER and LARGER
        Dim lblTotal As New Label()
        lblTotal.Text = $"Total: ₱{Convert.ToDecimal(productData("Price")):N2}"
        lblTotal.Font = New Font("Poppins", 16, FontStyle.Bold) ' Increased font size
        lblTotal.ForeColor = Color.FromArgb(95, 95, 95)
        lblTotal.Location = New Point(30, 250)
        lblTotal.Size = New Size(490, 35) ' WIDER and TALLER
        lblTotal.TextAlign = ContentAlignment.MiddleCenter
        quantityForm.Controls.Add(lblTotal)

        ' Update total when quantity changes
        AddHandler txtQuantity.TextChanged, Sub()
                                                Dim qty As Integer
                                                If Integer.TryParse(txtQuantity.Text, qty) AndAlso qty > 0 Then
                                                    Dim total As Decimal = Convert.ToDecimal(productData("Price")) * qty
                                                    lblTotal.Text = $"Total: ₱{total:N2}"
                                                Else
                                                    lblTotal.Text = "Total: ₱0.00"
                                                End If
                                            End Sub

        ' Action buttons section - WIDER spacing
        Dim buttonSection As New Panel()
        buttonSection.Size = New Size(490, 60)
        buttonSection.Location = New Point(30, 300)
        buttonSection.BackColor = Color.Transparent
        quantityForm.Controls.Add(buttonSection)

        ' Add to Cart button - LARGER
        Dim btnAddToCart As New Guna.UI2.WinForms.Guna2Button()
        btnAddToCart.Text = "Add to Cart"
        btnAddToCart.Size = New Size(180, 55) ' LARGER
        btnAddToCart.Location = New Point(250, 0) ' Adjusted position
        btnAddToCart.Font = New Font("Poppins", 10, FontStyle.Bold) ' Increased font size
        btnAddToCart.ForeColor = Color.White
        btnAddToCart.FillColor = Color.FromArgb(76, 175, 80)
        btnAddToCart.BorderThickness = 0
        btnAddToCart.BorderRadius = 12
        btnAddToCart.HoverState.FillColor = Color.FromArgb(67, 160, 71)
        AddHandler btnAddToCart.Click, Sub()
                                           Dim quantity As Integer
                                           If Integer.TryParse(txtQuantity.Text, quantity) AndAlso quantity > 0 Then
                                               If quantity > availableStock Then
                                                   MessageBox.Show($"Cannot add more items. Not enough stock available.", "Insufficient Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                                   Return
                                               End If
                                               ' Add the specified quantity to the order
                                               AddProductToOrder(productData, quantity)
                                               quantityForm.DialogResult = DialogResult.OK
                                               quantityForm.Close()
                                           Else
                                               MessageBox.Show("Please enter a valid quantity.", "Invalid Quantity", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                           End If
                                       End Sub
        buttonSection.Controls.Add(btnAddToCart)
        ' Inside ShowQuantitySelector, after creating txtQuantity
        ' Inside ShowQuantitySelector, after creating txtQuantity

        ' Inside ShowQuantitySelector, after creating txtQuantity

        ' Restrict input to only digits and prevent deletion
        AddHandler txtQuantity.KeyPress, Sub(sender As Object, e As KeyPressEventArgs)
                                             ' Allow only digits and control keys (but we'll block deletion separately)
                                             If Not Char.IsDigit(e.KeyChar) AndAlso Not Char.IsControl(e.KeyChar) Then
                                                 e.Handled = True
                                             End If
                                         End Sub

        AddHandler txtQuantity.KeyDown, Sub(sender As Object, e As KeyEventArgs)
                                            ' Prevent backspace and delete to stop number removal
                                            If e.KeyCode = Keys.Back Or e.KeyCode = Keys.Delete Then
                                                e.Handled = True
                                            End If
                                        End Sub

        ' Ensure quantity cannot be less than 1 or empty, and position cursor at the end
        AddHandler txtQuantity.TextChanged, Sub()
                                                Dim qty As Integer
                                                If String.IsNullOrWhiteSpace(txtQuantity.Text) OrElse Not Integer.TryParse(txtQuantity.Text, qty) OrElse qty < 1 Then
                                                    txtQuantity.Text = "1"
                                                    ' Move cursor to the end of the text
                                                    txtQuantity.SelectionStart = txtQuantity.Text.Length
                                                    txtQuantity.SelectionLength = 0
                                                Else
                                                    ' For valid input, ensure cursor is at the end for appending
                                                    txtQuantity.SelectionStart = txtQuantity.Text.Length
                                                    txtQuantity.SelectionLength = 0
                                                End If
                                            End Sub

        ' Then, continue with the rest of the method...
        ' Cancel button - LARGER
        Dim btnCancel As New Guna.UI2.WinForms.Guna2Button()
        btnCancel.Text = "Cancel"
        btnCancel.Size = New Size(150, 55) ' LARGER
        btnCancel.Location = New Point(80, 0) ' Adjusted position
        btnCancel.Font = New Font("Poppins", 14, FontStyle.Bold) ' Increased font size
        btnCancel.ForeColor = Color.FromArgb(200, 70, 70)
        btnCancel.FillColor = Color.FromArgb(255, 245, 245)
        btnCancel.BorderColor = Color.FromArgb(220, 120, 120)
        btnCancel.BorderThickness = 1
        btnCancel.BorderRadius = 12
        btnCancel.HoverState.FillColor = Color.FromArgb(255, 235, 235)
        AddHandler btnCancel.Click, Sub()
                                        quantityForm.DialogResult = DialogResult.Cancel
                                        quantityForm.Close()
                                    End Sub
        buttonSection.Controls.Add(btnCancel)

        ' Add KeyDown handler for keyboard support
        AddHandler quantityForm.KeyDown, Sub(sender As Object, e As KeyEventArgs)
                                             If e.KeyCode = Keys.Add Or e.KeyCode = Keys.Oemplus Or e.KeyCode = Keys.Up Then
                                                 ' Increase quantity
                                                 Dim currentQty As Integer
                                                 If Integer.TryParse(txtQuantity.Text, currentQty) Then
                                                     If currentQty < availableStock Then
                                                         txtQuantity.Text = (currentQty + 1).ToString()
                                                     End If
                                                 End If
                                                 e.Handled = True
                                             ElseIf e.KeyCode = Keys.Subtract Or e.KeyCode = Keys.OemMinus Or e.KeyCode = Keys.Down Then
                                                 ' Decrease quantity
                                                 Dim currentQty As Integer
                                                 If Integer.TryParse(txtQuantity.Text, currentQty) Then
                                                     If currentQty > 1 Then
                                                         txtQuantity.Text = (currentQty - 1).ToString()
                                                     End If
                                                 End If
                                                 e.Handled = True
                                             ElseIf e.KeyCode = Keys.Enter Then
                                                 ' Confirm add to cart
                                                 btnAddToCart.PerformClick()
                                                 e.Handled = True
                                             ElseIf e.KeyCode = Keys.Escape Then
                                                 ' Cancel
                                                 btnCancel.PerformClick()
                                                 e.Handled = True
                                             End If
                                         End Sub

        ' Show modal and handle result
        quantityForm.ShowDialog()
        quantityForm.Dispose()
    End Sub

    ' New method to add product with specified quantity
    Private Sub AddProductToOrder(productData As Dictionary(Of String, Object), quantity As Integer)

        If posLockedForCapital Then
            MessageBox.Show("POS is locked. Manager/Admin must set opening capital first.", "POS Locked", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        ' Check if product already exists in the order list
        Dim foundIndex As Integer = -1
        For i = 0 To currentOrderList.Count - 1
            If currentOrderList(i)("ProductID").ToString() = productData("ProductID").ToString() Then
                foundIndex = i
                Exit For
            End If
        Next

        Dim priceToUse As Decimal = Convert.ToDecimal(productData("Price"))

        If foundIndex <> -1 Then
            ' Check if we have enough stock for the increase
            Dim currentQuantity As Integer = CInt(currentOrderList(foundIndex)("Quantity"))
            Dim apProdId As String = productData("ProductID").ToString()
            Dim apDbStock As Integer = If(productDbStock.ContainsKey(apProdId), productDbStock(apProdId), If(productData.ContainsKey("CurrentStock"), CInt(productData("CurrentStock")), 0))

            ' Get already reserved quantity in order
            Dim reservedQuantity As Integer = 0
            For Each item In currentOrderList
                If item("ProductID").ToString() = apProdId Then
                    reservedQuantity = CInt(item("Quantity"))
                    Exit For
                End If
            Next

            If reservedQuantity + quantity > apDbStock Then
                MessageBox.Show("Cannot add more items. Not enough stock available.", "Insufficient Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' Increase quantity
            currentOrderList(foundIndex)("Quantity") = currentQuantity + quantity
        Else
            ' Add product with specified quantity
            productData("Quantity") = quantity
            productData("Price") = priceToUse
            currentOrderList.Add(productData)
        End If

        ' Update stock display using DB stock minus reserved in cart
        UpdateStockLabelFromDbStock(productData("ProductID").ToString())

        ' Refresh the order display
        RefreshOrderDisplay()

        ' Keep the persisted draft cart in sync with the live order list
        PersistCartState()
    End Sub
    ' Helper: fetch primary image bytes for a product
    Private Sub NavAuditLog_Click(sender As Object, e As EventArgs)
        isNavigating = True
        AuditLog.Show()
        Me.Close()
    End Sub
    Private Function GetPrimaryImagePath(productId As Integer) As String
        Try
            Dim query As String = "SELECT pi.FilePath FROM ProductImageMapping pim " &
                                  "JOIN ProductImages pi ON pim.ImageID = pi.ImageID " &
                                  "WHERE pim.ProductID = @ProductID AND pi.FilePath IS NOT NULL AND pi.FilePath != '' LIMIT 1"
            Dim param As New SqlParameter("@ProductID", productId)
            Using reader As DbDataReader = Utilities.ExecuteReader(query, {param})
                If reader.Read() Then
                    If Not IsDBNull(reader("FilePath")) Then
                        Dim filePath As String = reader("FilePath").ToString()
                        Dim fullPath As String = Path.Combine(Connection.GetImagesFolder("products"), filePath)
                        If IO.File.Exists(fullPath) Then
                            Return fullPath
                        End If
                    End If
                End If
            End Using
        Catch ex As Exception
            Console.WriteLine($"GetPrimaryImagePath error for ProductID {productId}: {ex.Message}")
        End Try
        Return Nothing
    End Function

    Private Function LoadProductImage(productId As Integer, desiredWidth As Integer, desiredHeight As Integer) As Image
        Try
            Dim filePath As String = GetPrimaryImagePath(productId)
            If Not String.IsNullOrEmpty(filePath) AndAlso IO.File.Exists(filePath) Then
                Using src As Image = Image.FromFile(filePath)
                    Dim bmp As New Bitmap(desiredWidth, desiredHeight)
                    Using g As Graphics = Graphics.FromImage(bmp)
                        g.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic
                        g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
                        g.Clear(Color.Transparent)
                        g.DrawImage(src, New Rectangle(0, 0, desiredWidth, desiredHeight))
                    End Using
                    Return bmp
                End Using
            End If
        Catch ex As Exception
            Console.WriteLine($"LoadProductImage error for ProductID {productId}: {ex.Message}")
        End Try

        Try
            Return My.Resources.product_placeholder
        Catch
            Dim placeholder As New Bitmap(desiredWidth, desiredHeight)
            Using g As Graphics = Graphics.FromImage(placeholder)
                g.Clear(LightGray)
                Using f As New Font("Segoe UI", 8)
                    TextRenderer.DrawText(g, "No Image", f, New Rectangle(0, 0, desiredWidth, desiredHeight), Color.White, TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
                End Using
            End Using
            Return placeholder
        End Try
    End Function
    Private Sub lblSearchProduct_Click(sender As Object, e As EventArgs)
        ShowProductSearchModal()
    End Sub

    ' Live search via the TxtSearch box on the categories screen
    Private Sub TxtSearch_TextChanged(sender As Object, e As EventArgs) Handles TxtSearch.TextChanged
        If _searchTimer Is Nothing Then
            _searchTimer = New Timer() With {.Interval = 350}
            AddHandler _searchTimer.Tick, Sub()
                                              _searchTimer.Stop()
                                              ExecuteCategorySearch()
                                          End Sub
        End If
        _searchTimer.Stop()
        _searchTimer.Start()
    End Sub

    Private Sub TxtSearch_KeyDown(sender As Object, e As KeyEventArgs) Handles TxtSearch.KeyDown
        If e.KeyCode = Keys.Enter Then
            e.Handled = True
            If _searchTimer IsNot Nothing Then _searchTimer.Stop()
            ExecuteCategorySearch()
        ElseIf e.KeyCode = Keys.Escape Then
            e.Handled = True
            TxtSearch.Text = ""
        End If
    End Sub

    ' Run the search: empty restores the category grid, otherwise show matching products
    Private Sub ExecuteCategorySearch()
        If TxtSearch Is Nothing OrElse TxtSearch.IsDisposed Then Return
        Dim term As String = TxtSearch.Text.Trim()
        If String.IsNullOrWhiteSpace(term) Then
            CategoryPanel.Controls.Clear()
            BuildCategoryTiles()
            ArrangeCategoryButtonsFlexWrap()
            UpdateCategoryItemCounts()
            LabelTitle.Text = "Categories"
            backCategory.Visible = False
            _paginationContext = PaginationContext.None
        Else
            ShowSearchResults(term)
        End If
    End Sub

    ' Show all products matching the search term (barcode exact or name partial)
    Private Sub ShowSearchResults(term As String)
        CategoryPanel.Controls.Clear()
        productCardControls.Clear()
        productDbStock.Clear()
        backCategory.Visible = True
        LabelTitle.Text = $"Search: {term}"

        ' Keep the search box on top so the query can be refined
        If Not CategoryPanel.Controls.Contains(TxtSearch) Then
            CategoryPanel.Controls.Add(TxtSearch)
        End If

        ' Unit filter combo on the right side of the panel
        Dim unitCombo = CreateAndPopulateUnitFilter()
        If Not CategoryPanel.Controls.Contains(_lblUnitFilter) Then
            CategoryPanel.Controls.Add(_lblUnitFilter)
        End If
        _lblUnitFilter.BringToFront()
        If Not CategoryPanel.Controls.Contains(unitCombo) Then
            CategoryPanel.Controls.Add(unitCombo)
        End If
        unitCombo.BringToFront()

        ' Padding creates a gap for the GDI+ border drawn in CategoryPanel_Paint
        CategoryPanel.Padding = New Padding(2)

        ' Flow panel fills the space above the footer; top padding reserves the
        ' search box area so cards never slide underneath it (Dock Fill stays
        ' inside the rounded border, no manual inset needed)
        Dim flowPanel As New FlowLayoutPanel()
        flowPanel.Dock = DockStyle.Fill
        flowPanel.AutoScroll = True
        flowPanel.BackColor = Color.White
        flowPanel.Padding = New Padding(14, 86, 14, 14)
        CategoryPanel.Controls.Add(flowPanel)
        TxtSearch.BringToFront()

        ' Count matching rows so the footer can show total pages
        Dim totalItems As Integer = 0
        Try
            Dim countQuery As String = "SELECT COUNT(*) FROM Products WHERE IsActive = 1 AND (ProductCode = @term OR ProductName LIKE @like)"
            Dim countParameters As SqlParameter() = {
                New SqlParameter("@term", term),
                New SqlParameter("@like", "%" & term & "%")
            }
            Dim countResult As Object = Utilities.ExecuteScalar(countQuery, countParameters)
            LogDiagnostic($"SRCH term='{term}' countResult={If(countResult Is Nothing, "Nothing", countResult.ToString())} type={If(countResult Is Nothing, "-", countResult.GetType().Name)}")
            If countResult IsNot Nothing Then
                totalItems = Convert.ToInt32(countResult)
            End If
            LogDiagnostic($"SRCH term='{term}' totalItems={totalItems}")
        Catch ex As Exception
            LogDiagnostic($"SRCH term='{term}' EXCEPTION: {ex.ToString()}")
            Console.WriteLine($"Search count error: {ex.Message}")
            MessageBox.Show($"Search count failed: {ex.Message}")
        End Try

        ' Footer pagination bar (Dock Bottom)
        Dim pagination As PaginationControl = GetPagination()
        pagination.Dock = DockStyle.Bottom
        pagination.Height = 62
        CategoryPanel.Controls.Add(pagination)
        pagination.Configure(totalItems, ProductPageSize, 1)
        LogDiagnostic($"SRCH term='{term}' footer totalItems={totalItems} totalPages={pagination.TotalPages} instance={pagination.GetHashCode()}")

        _paginationSearchTerm = term
        _paginationContext = PaginationContext.Search
        LoadSearchProductsPage(term, 1)
    End Sub

    ' Loads a single page of search results (raises no events)
    Private Sub LoadSearchProductsPage(term As String, page As Integer)
        Dim flowPanel As FlowLayoutPanel = CategoryPanel.Controls.OfType(Of FlowLayoutPanel)().FirstOrDefault()
        If flowPanel Is Nothing Then Return
        flowPanel.Controls.Clear()
        flowPanel.SuspendLayout()
        productCardControls.Clear()
        productDbStock.Clear()

        Dim offset As Integer = (page - 1) * ProductPageSize
        Dim baseWhere As String = "WHERE IsActive = 1 AND (ProductCode = @term OR ProductName LIKE @like)"
        If Not String.IsNullOrEmpty(_selectedUnitFilter) Then
            baseWhere &= " AND Unit = @Unit"
        End If
        Dim query As String = $"SELECT ProductID, ProductName, SellingPrice, ProductCode, ReorderLevel, CurrentStock, Category, Unit FROM Products {baseWhere} ORDER BY CASE WHEN ProductCode = @term THEN 0 ELSE 1 END, ProductName LIMIT @Limit OFFSET @Offset"
        Dim paramList As New List(Of SqlParameter) From {
            New SqlParameter("@term", term),
            New SqlParameter("@like", "%" & term & "%"),
            New SqlParameter("@Limit", ProductPageSize),
            New SqlParameter("@Offset", offset)
        }
        If Not String.IsNullOrEmpty(_selectedUnitFilter) Then
            paramList.Add(New SqlParameter("@Unit", _selectedUnitFilter))
        End If
        Dim parameters As SqlParameter() = paramList.ToArray()
        Try
            Using reader As DbDataReader = Utilities.ExecuteReader(query, parameters)
                While reader.Read()
                    Dim stock As Integer = Convert.ToInt32(reader("CurrentStock"))

                    Dim productData As New Dictionary(Of String, Object) From {
                        {"ProductID", reader("ProductID")},
                        {"ProductName", reader("ProductName")},
                        {"Price", Convert.ToDecimal(reader("SellingPrice"))},
                        {"ProductCode", reader("ProductCode")},
                        {"Category", reader("Category")},
                        {"Unit", reader("Unit")},
                        {"CurrentStock", stock}
                    }
                    productDbStock(reader("ProductID").ToString()) = stock

                    Dim reservedQty As Integer = 0
                    For Each orderItem In currentOrderList
                        If orderItem("ProductID").ToString() = reader("ProductID").ToString() Then
                            reservedQty += CInt(orderItem("Quantity"))
                        End If
                    Next

                    Dim productCard = ProductCardBuilder.Create(productData, LoadProductImage(Convert.ToInt32(reader("ProductID")), 85, 78),
                                                                Sub() HandleProductInteraction(productData, False))
                    ProductCardBuilder.UpdateStock(productCard, Math.Max(0, stock - reservedQty))

                    productCardControls.Add(productCard)
                    flowPanel.Controls.Add(productCard)
                End While
            End Using
        Catch ex As Exception
            Console.WriteLine($"Search error: {ex.Message}")
        End Try

        ' Informational empty state when nothing matches
        If flowPanel.Controls.Count = 0 Then
            Dim noResults As New Label() With {
                .Text = $"No products found for '{term}'.",
                .AutoSize = True,
                .Font = New Font("Poppins", 11.0F),
                .ForeColor = MediumText,
                .Location = New Point(30, 100)
            }
            flowPanel.Controls.Add(noResults)
        End If

        flowPanel.ResumeLayout(True)
        flowPanel.AutoScrollPosition = New Point(0, 0)
        LogDiagnostic($"SRCH term='{term}' page={page} cards={flowPanel.Controls.Count}")
    End Sub

    ' Show only the matching product card from search
    Private Sub ShowSingleProductCard(productId As Integer, productName As String, category As String)
        CategoryPanel.Controls.Clear()
        _paginationContext = PaginationContext.None
        productCardControls.Clear()
        productDbStock.Clear()
        backCategory.Visible = True
        LabelTitle.Text = $"Search: {productName}"

        Dim query As String = "SELECT ProductID, ProductName, SellingPrice, ProductCode, ReorderLevel, CurrentStock, Category, Unit FROM Products WHERE ProductID = @ProductID AND IsActive = 1"
        Dim param As New SqlParameter("@ProductID", productId)
        Using reader As DbDataReader = Utilities.ExecuteReader(query, {param})
            If reader.Read() Then
                Dim stock As Integer = Convert.ToInt32(reader("CurrentStock"))

                Dim productData As New Dictionary(Of String, Object) From {
                    {"ProductID", reader("ProductID")},
                    {"ProductName", reader("ProductName")},
                    {"Price", Convert.ToDecimal(reader("SellingPrice"))},
                    {"ProductCode", reader("ProductCode")},
                    {"Category", reader("Category")},
                    {"Unit", reader("Unit")},
                    {"CurrentStock", stock}
                }
                productDbStock(reader("ProductID").ToString()) = stock

                Dim reservedQty As Integer = 0
                For Each orderItem In currentOrderList
                    If orderItem("ProductID").ToString() = reader("ProductID").ToString() Then
                        reservedQty += CInt(orderItem("Quantity"))
                    End If
                Next

                Dim productCard = ProductCardBuilder.Create(productData, LoadProductImage(productId, 85, 78),
                                                            Sub() HandleProductInteraction(productData, False))
                productCard.Location = New Point(28, 18)
                ProductCardBuilder.UpdateStock(productCard, Math.Max(0, stock - reservedQty))

                productCardControls.Add(productCard)
                CategoryPanel.Controls.Add(productCard)
            End If
        End Using
    End Sub

    ' Mini modal to search products by barcode or name and add to order (uses existing AddProductToOrder / ShowQuantitySelector)
    Private Sub ShowProductSearchModal()
        If posLockedForCapital Then
            MessageBox.Show("POS is locked. Manager/Admin must set opening capital first.", "POS Locked", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        ' Simple input modal: search by barcode (exact) or product name (partial).
        Dim searchForm As New Form() With {
            .Text = "Search Product",
            .Size = New Size(420, 180),
            .StartPosition = FormStartPosition.CenterParent,
            .FormBorderStyle = FormBorderStyle.FixedDialog,
            .BackColor = OffWhite,
            .MaximizeBox = False,
            .MinimizeBox = False,
            .ShowInTaskbar = False
        }

        Dim lbl As New Label() With {
            .Text = "Enter barcode or product name:",
            .ForeColor = Color.FromArgb(95, 95, 95),
            .Font = New Font("Poppins", 10, FontStyle.Regular),
            .Location = New Point(12, 12),
            .AutoSize = True
        }
        searchForm.Controls.Add(lbl)

        Dim txtSearch As New Guna.UI2.WinForms.Guna2TextBox() With {
            .PlaceholderText = "e.g. PRD-001234 or partial product name...",
            .Size = New Size(360, 36),
            .Location = New Point(12, 50),
            .BorderRadius = 8,
            .FillColor = Color.FromArgb(250, 249, 246),
            .ForeColor = Color.FromArgb(95, 95, 95),
            .BorderColor = Color.FromArgb(200, 198, 192),
            .BorderThickness = 1
        }
        searchForm.Controls.Add(txtSearch)

        Dim btnClose As New Guna.UI2.WinForms.Guna2Button() With {
            .Text = "Close",
            .Size = New Size(88, 34),
            .Location = New Point(284, 96),
            .FillColor = Color.FromArgb(250, 249, 246),
            .ForeColor = Color.FromArgb(95, 95, 95),
            .BorderColor = Color.FromArgb(200, 198, 192),
            .BorderThickness = 1,
            .BorderRadius = 8
        }
        btnClose.HoverState.FillColor = Color.FromArgb(240, 238, 232)
        searchForm.Controls.Add(btnClose)

        Dim btnSearch As New Guna.UI2.WinForms.Guna2Button() With {
            .Text = "Search",
            .Size = New Size(88, 34),
            .Location = New Point(190, 96),
            .FillColor = Color.FromArgb(0, 120, 212),
            .ForeColor = Color.White,
            .BorderThickness = 0,
            .BorderRadius = 8
        }
        btnSearch.HoverState.FillColor = Color.FromArgb(0, 102, 190)
        searchForm.Controls.Add(btnSearch)

        ' Stores search result for processing after modal closes
        Dim searchResult As Tuple(Of Integer, String, String) = Nothing

        Dim DoLookup = Sub()
                           Dim term As String = txtSearch.Text.Trim()
                           If String.IsNullOrWhiteSpace(term) Then
                               MessageBox.Show("Please enter a barcode or product name to search.", "Search", MessageBoxButtons.OK, MessageBoxIcon.Information)
                               Return
                           End If

                           Try
                               Dim query As String = "SELECT ProductID, ProductName, Category FROM Products WHERE IsActive = 1 AND (ProductCode = @term OR ProductName LIKE @like) ORDER BY CASE WHEN ProductCode = @term THEN 0 ELSE 1 END, ProductName"
                               Dim parameters As SqlParameter() = {
                                   New SqlParameter("@term", term),
                                   New SqlParameter("@like", "%" & term & "%")
                               }

                               Using reader As DbDataReader = Utilities.ExecuteReader(query, parameters)
                                   If reader.Read() Then
                                       Dim productId As Integer = Convert.ToInt32(reader("ProductID"))
                                       Dim productName As String = reader("ProductName").ToString()
                                       Dim category As String = If(reader("Category") IsNot DBNull.Value, reader("Category").ToString(), String.Empty)

                                       searchResult = Tuple.Create(productId, productName, category)
                                       searchForm.DialogResult = DialogResult.OK
                                   Else
                                       MessageBox.Show($"No product match for '{term}'.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                   End If
                               End Using
                           Catch ex As Exception
                               MessageBox.Show($"Search failed: {ex.Message}", "Search Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                           End Try
                       End Sub

        ' Event handlers
        AddHandler btnSearch.Click, Sub() DoLookup()
        AddHandler btnClose.Click, Sub() searchForm.Close()
        AddHandler txtSearch.KeyDown, Sub(s, ke)
                                          If ke.KeyCode = Keys.Enter Then
                                              ke.Handled = True
                                              DoLookup()
                                          ElseIf ke.KeyCode = Keys.Escape Then
                                              searchForm.Close()
                                          End If
                                      End Sub

        ' Show modal
        If searchForm.ShowDialog() = DialogResult.OK AndAlso searchResult IsNot Nothing Then
            Dim productId As Integer = searchResult.Item1
            Dim productName As String = searchResult.Item2
            Dim category As String = searchResult.Item3

            If Not String.IsNullOrEmpty(category) Then
                ShowSingleProductCard(productId, productName, category)
            Else
                backCategory.Visible = False
                CategoryPanel.AutoScrollPosition = New Point(0, 0)
            End If
        End If
        searchForm.Dispose()
    End Sub
    Private Function FindOrCreateCustomer(name As String, phone As String, email As String, tin As String, customerType As String) As Integer?
        ' Customers table removed � do not insert or query Customers.
        ' Keep customer snapshot in Sales (CustomerName, CustomerTIN, SalesData).
        Try
            Console.WriteLine("Customer persistence disabled: not creating or querying Customers table.")
        Catch
            ' ignore logging errors
        End Try
        Return Nothing
    End Function

    Private Sub Sales_KeyDown(sender As Object, e As KeyEventArgs) Handles MyBase.KeyDown
        If posLockedForCapital Then
            e.Handled = True
            Return
        End If

        ' While typing in TxtSearch, suppress ALL shortcuts (including the global
        ' D / Ctrl+Enter ones) so keys reach the search box as plain text
        If Me.ActiveControl Is TxtSearch Then
            Return
        End If

        ' GLOBAL SHORTCUTS (only when no modal/customer/payment panels are active)
        If Not totalPanelActive AndAlso Not pinPanelActive AndAlso Not ProfileManager.IsProfileDropdownVisible(Me) Then
            ' Ctrl+Enter -> go to payment (explicit shortcut)
            If e.KeyCode = Keys.Enter AndAlso e.Control Then
                If currentOrderList.Count > 0 Then
                    btnPayment.PerformClick()
                End If
                e.Handled = True
                Return
            End If



            ' D -> open discount modal
            If e.KeyCode = Keys.D AndAlso Not e.Control AndAlso Not e.Alt Then
                ' Reuse existing btnDiscount click handler (which validates order presence)
                btnDiscount.PerformClick()
                e.Handled = True
                Return
            End If
        End If

        ' Existing barcode / payment logic
        If Not totalPanelActive AndAlso Not pinPanelActive AndAlso Not ProfileManager.IsProfileDropdownVisible(Me) Then
            ' Check if this might be barcode input (we have characters in buffer or it's a barcode-related key)
            Dim isBarcodeKey As Boolean = False

            Select Case e.KeyCode
                Case Keys.D0 To Keys.D9, Keys.NumPad0 To Keys.NumPad9,
                 Keys.A To Keys.Z, Keys.OemMinus, Keys.Subtract,
                 Keys.Back, Keys.Delete, Keys.Escape
                    isBarcodeKey = True
                Case Keys.Enter
                    If Not String.IsNullOrEmpty(barcodeBuffer) Then
                        isBarcodeKey = True
                    Else
                        ' Require a modifier (Ctrl or Shift) to trigger payment on Enter to avoid accidental submits
                        If (e.Control Or e.Shift) AndAlso currentOrderList.Count > 0 Then
                            btnPayment.PerformClick()
                            e.Handled = True
                            Return
                        End If
                    End If
            End Select

            ' If it's a barcode key, handle it as barcode input
            If isBarcodeKey Then
                HandleBarcodeKeyInput(e)
            End If
        Else
            ' Handle payment panel keyboard input
            If totalPanelActive Then
                HandleTotalAmountKeyboardInput(e)
            End If
        End If
    End Sub


    Private Function ShowVoidAuthorizationModal(productName As String, quantityToVoid As Integer, ByRef approverUsername As String) As Boolean
        If isVoidDialogOpen Then Return False
        isVoidDialogOpen = True

        Try
            Dim dlg As New Form With {
            .Text = "Void Authorization",
            .Size = New Size(560, 500),
            .StartPosition = FormStartPosition.CenterParent,
            .FormBorderStyle = FormBorderStyle.FixedDialog,
            .MaximizeBox = False,
            .MinimizeBox = False,
            .BackColor = OffWhite,
            .KeyPreview = True,
            .ShowInTaskbar = False
        }

            Dim lblTitle As New Label With {
            .Text = "VOID AUTHORIZATION",
            .Font = New Font("Poppins", 16, FontStyle.Bold),
            .ForeColor = Color.Black,
            .AutoSize = False,
            .Size = New Size(520, 34),
            .Location = New Point(20, 16),
            .TextAlign = ContentAlignment.MiddleCenter
        }
            dlg.Controls.Add(lblTitle)

            Dim lblProduct As New Label With {
            .Text = productName,
            .Font = New Font("Poppins", 11, FontStyle.Bold),
            .ForeColor = GoldenYellow,
            .AutoSize = False,
            .Size = New Size(520, 24),
            .Location = New Point(20, 56),
            .TextAlign = ContentAlignment.MiddleCenter
        }
            dlg.Controls.Add(lblProduct)

            Dim lblQty As New Label With {
            .Text = $"Quantity to remove: {quantityToVoid}",
            .Font = New Font("Poppins", 9, FontStyle.Regular),
            .ForeColor = MediumText,
            .AutoSize = False,
            .Size = New Size(520, 20),
            .Location = New Point(20, 82),
            .TextAlign = ContentAlignment.MiddleCenter
        }
            dlg.Controls.Add(lblQty)

            Dim btnQrMode As New Guna2Button With {
            .Text = "QR",
            .Size = New Size(240, 42),
            .Location = New Point(30, 126),
            .BorderRadius = 10,
            .FillColor = JadeOlive,
            .ForeColor = PureWhite,
            .Font = New Font("Poppins", 11, FontStyle.Bold)
        }

            Dim btnPassMode As New Guna2Button With {
            .Text = "User/Pass",
            .Size = New Size(240, 42),
            .Location = New Point(290, 126),
            .BorderRadius = 10,
            .FillColor = BorderGray,
            .ForeColor = PureWhite,
            .Font = New Font("Poppins", 11, FontStyle.Bold)
        }

            dlg.Controls.Add(btnQrMode)
            dlg.Controls.Add(btnPassMode)

            Dim lblScanInstruction As New Label With {
            .Text = "Scan manager/admin QR now",
            .Font = New Font("Poppins", 10, FontStyle.Italic),
            .ForeColor = MediumText,
            .AutoSize = False,
            .Size = New Size(500, 24),
            .Location = New Point(30, 212),
            .TextAlign = ContentAlignment.MiddleCenter,
            .Visible = True
        }
            dlg.Controls.Add(lblScanInstruction)

            Dim txtQr As New Guna2TextBox With {
            .Size = New Size(1, 1),
            .Location = New Point(dlg.ClientSize.Width - 6, dlg.ClientSize.Height - 6),
            .BorderThickness = 0,
            .FillColor = OffWhite,
            .ForeColor = OffWhite,
            .PlaceholderText = "",
            .Visible = True
        }
            dlg.Controls.Add(txtQr)

            Dim pnlPass As New Panel With {
            .Location = New Point(30, 178),
            .Size = New Size(500, 178),
            .BackColor = Color.Transparent,
            .Visible = False
        }

            Dim lblUser As New Label With {
            .Text = "Username",
            .ForeColor = MediumText,
            .Font = New Font("Poppins", 8),
            .AutoSize = True,
            .Location = New Point(0, 0)
        }

            Dim txtUser As New Guna2TextBox With {
            .Size = New Size(500, 40),
            .Location = New Point(0, 26),
            .BorderRadius = 8,
            .FillColor = PureWhite,
            .ForeColor = DarkText
        }

            Dim lblPass As New Label With {
            .Text = "Password",
            .ForeColor = MediumText,
            .Font = New Font("Poppins", 8),
            .AutoSize = True,
            .Location = New Point(0, 80)
        }

            Dim txtPass As New Guna2TextBox With {
            .Size = New Size(500, 40),
            .Location = New Point(0, 108),
            .BorderRadius = 8,
            .FillColor = PureWhite,
            .ForeColor = DarkText,
            .UseSystemPasswordChar = True
        }

            pnlPass.Controls.Add(lblUser)
            pnlPass.Controls.Add(txtUser)
            pnlPass.Controls.Add(lblPass)
            pnlPass.Controls.Add(txtPass)
            dlg.Controls.Add(pnlPass)

            Dim lblStatus As New Label With {
            .Text = "Waiting for manager/admin authorization...",
            .ForeColor = MediumText,
            .Font = New Font("Poppins", 9, FontStyle.Italic),
            .AutoSize = False,
            .Size = New Size(500, 20),
            .Location = New Point(30, 366),
            .TextAlign = ContentAlignment.MiddleCenter
        }
            dlg.Controls.Add(lblStatus)

            Dim btnAuthorize As New Guna2Button With {
            .Text = "Authorize",
            .Size = New Size(180, 44),
            .Location = New Point(290, 405),
            .BorderRadius = 10,
            .FillColor = SuccessGreen,
            .ForeColor = DarkText,
            .Font = New Font("Poppins", 10, FontStyle.Bold)
        }

            Dim btnCancel As New Guna2Button With {
            .Text = "Cancel",
            .Size = New Size(140, 44),
            .Location = New Point(110, 405),
            .BorderRadius = 10,
            .FillColor = AlertRed,
            .ForeColor = PureWhite,
            .Font = New Font("Poppins", 10, FontStyle.Regular)
        }

            dlg.Controls.Add(btnAuthorize)
            dlg.Controls.Add(btnCancel)

            Dim authorized As Boolean = False
            Dim approverLocal As String = ""
            Dim qrMode As Boolean = True
            Dim isAuthorizingQr As Boolean = False

            Dim qrAutoAuthorizeTimer As New Timer With {.Interval = 150}

            Dim TryAuthorizeQr As Action =
            Sub()
                If Not qrMode OrElse isAuthorizingQr OrElse authorized Then Return

                Dim qrRaw As String = txtQr.Text.Trim()
                If String.IsNullOrWhiteSpace(qrRaw) Then Return

                Console.WriteLine($"[VOID AUTH DEBUG] QR scanned raw value: '{qrRaw}'")

                Dim approvedBy As String = ""
                isAuthorizingQr = True
                Try
                    lblStatus.Text = "Processing QR authorization..."
                    lblStatus.ForeColor = MediumText

                    If TryAuthorizeVoidByQr(qrRaw, approvedBy) Then
                        Console.WriteLine($"[VOID AUTH DEBUG] QR authorization SUCCESS. ApprovedBy='{approvedBy}'")
                        approverLocal = approvedBy
                        authorized = True
                        dlg.DialogResult = DialogResult.OK
                        dlg.Close()
                    Else
                        Console.WriteLine("[VOID AUTH DEBUG] QR authorization FAILED.")
                        lblStatus.Text = "Authorization failed. Manager/Admin required."
                        lblStatus.ForeColor = AlertRed
                        MessageBox.Show("Invalid QR code or insufficient role. Manager/Admin authorization is required.", "Authorization Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        txtQr.Clear()
                        txtQr.Focus()
                    End If
                Finally
                    isAuthorizingQr = False
                End Try
            End Sub

            AddHandler btnQrMode.Click, Sub()
                                            qrMode = True
                                            btnQrMode.FillColor = JadeOlive
                                            btnPassMode.FillColor = BorderGray
                                            pnlPass.Visible = False
                                            lblScanInstruction.Visible = True

                                            ' Hide Authorize button in QR mode (auto authorize only)
                                            btnAuthorize.Visible = False

                                            txtQr.Text = ""
                                            lblStatus.Text = "Waiting for manager/admin authorization..."
                                            lblStatus.ForeColor = MediumText
                                            txtQr.Focus()
                                        End Sub

            AddHandler btnPassMode.Click, Sub()
                                              qrMode = False
                                              btnPassMode.FillColor = JadeOlive
                                              btnQrMode.FillColor = BorderGray
                                              pnlPass.Visible = True
                                              lblScanInstruction.Visible = False

                                              ' Show Authorize button in User/Pass mode
                                              btnAuthorize.Visible = True

                                              txtUser.Focus()
                                          End Sub

            AddHandler btnCancel.Click, Sub()
                                            dlg.DialogResult = DialogResult.Cancel
                                            dlg.Close()
                                        End Sub

            AddHandler qrAutoAuthorizeTimer.Tick, Sub()
                                                      qrAutoAuthorizeTimer.Stop()
                                                      TryAuthorizeQr()
                                                  End Sub

            AddHandler txtQr.TextChanged, Sub()
                                              If Not qrMode Then Return
                                              qrAutoAuthorizeTimer.Stop()
                                              If txtQr.TextLength = 0 Then Return
                                              qrAutoAuthorizeTimer.Start()
                                          End Sub

            AddHandler btnAuthorize.Click, Sub()
                                               Dim ok As Boolean = False
                                               Dim approvedBy As String = ""

                                               If qrMode Then
                                                   TryAuthorizeQr()
                                                   Return
                                               Else
                                                   Console.WriteLine($"[VOID AUTH DEBUG] Password mode authorization attempt for user '{txtUser.Text.Trim()}'")
                                                   ok = TryAuthorizeVoidByPassword(txtUser.Text.Trim(), txtPass.Text, approvedBy)
                                               End If

                                               If ok Then
                                                   Console.WriteLine($"[VOID AUTH DEBUG] Password authorization SUCCESS. ApprovedBy='{approvedBy}'")
                                                   approverLocal = approvedBy
                                                   authorized = True
                                                   dlg.DialogResult = DialogResult.OK
                                                   dlg.Close()
                                               Else
                                                   Console.WriteLine("[VOID AUTH DEBUG] Password authorization FAILED.")
                                                   lblStatus.Text = "Authorization failed. Manager/Admin required."
                                                   lblStatus.ForeColor = AlertRed
                                                   MessageBox.Show("Invalid credentials or insufficient role. Manager/Admin authorization is required.", "Authorization Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                               End If
                                           End Sub

            AddHandler txtQr.KeyDown, Sub(s, e)
                                          If e.KeyCode = Keys.Enter Then
                                              qrAutoAuthorizeTimer.Stop()
                                              TryAuthorizeQr()
                                              e.Handled = True
                                          End If
                                      End Sub

            AddHandler txtPass.KeyDown, Sub(s, e)
                                            If e.KeyCode = Keys.Enter Then
                                                btnAuthorize.PerformClick()
                                                e.Handled = True
                                            End If
                                        End Sub

            AddHandler dlg.KeyDown, Sub(s, e)
                                        If e.KeyCode = Keys.Escape Then
                                            btnCancel.PerformClick()
                                            e.Handled = True
                                        End If
                                    End Sub

            AddHandler dlg.FormClosed, Sub()
                                           Try
                                               qrAutoAuthorizeTimer.Stop()
                                               qrAutoAuthorizeTimer.Dispose()
                                           Catch
                                           End Try
                                       End Sub

            btnQrMode.PerformClick()
            dlg.ShowDialog(Me)

            If authorized Then
                approverUsername = approverLocal
                Return True
            End If

            Return False

        Finally
            isVoidDialogOpen = False
        End Try
    End Function
    Private Function TryAuthorizeVoidByQr(qrRaw As String, ByRef approverUsername As String) As Boolean
        approverUsername = ""
        If String.IsNullOrWhiteSpace(qrRaw) Then Return False

        Dim token As String = ExtractManagerQrToken(qrRaw)
        If String.IsNullOrWhiteSpace(token) Then Return False

        Dim sql As String = "SELECT Username, UserRole, IsActive FROM Users WHERE QRCode = @QRCode LIMIT 1"
        Using reader As DbDataReader = Utilities.ExecuteReader(sql, New SqlParameter("@QRCode", token))
            If reader.Read() Then
                Dim userName As String = If(IsDBNull(reader("Username")), "", reader("Username").ToString())
                Dim role As String = If(IsDBNull(reader("UserRole")), "", reader("UserRole").ToString())
                Dim active As Boolean = If(IsDBNull(reader("IsActive")), False, Convert.ToBoolean(reader("IsActive")))

                If active AndAlso IsElevatedRole(role) Then
                    approverUsername = userName
                    Return True
                End If
            End If
        End Using

        Return False
    End Function

    Private Function TryAuthorizeVoidByPassword(managerUsername As String, managerPassword As String, ByRef approverUsername As String) As Boolean
        approverUsername = ""
        If String.IsNullOrWhiteSpace(managerUsername) OrElse String.IsNullOrWhiteSpace(managerPassword) Then Return False

        Dim sql As String = "SELECT Username, UserRole, IsActive, PasswordHash FROM Users WHERE Username = @Username LIMIT 1"
        Using reader As DbDataReader = Utilities.ExecuteReader(sql, New SqlParameter("@Username", managerUsername))
            If reader.Read() Then
                Dim role As String = If(IsDBNull(reader("UserRole")), "", reader("UserRole").ToString())
                Dim active As Boolean = If(IsDBNull(reader("IsActive")), False, Convert.ToBoolean(reader("IsActive")))
                Dim hash As String = If(IsDBNull(reader("PasswordHash")), "", reader("PasswordHash").ToString())

                If Not active OrElse Not IsElevatedRole(role) Then Return False
                If Not VerifyStoredPassword(managerPassword, hash) Then Return False

                approverUsername = If(IsDBNull(reader("Username")), managerUsername, reader("Username").ToString())
                Return True
            End If
        End Using

        Return False
    End Function

    Private Function ExtractManagerQrToken(rawInput As String) As String
        If String.IsNullOrWhiteSpace(rawInput) Then Return ""

        Dim match = Regex.Match(rawInput, "User-\d{5}", RegexOptions.IgnoreCase)
        If match.Success Then
            Return match.Value
        End If

        If rawInput.StartsWith("User-", StringComparison.OrdinalIgnoreCase) Then
            Return rawInput.Trim()
        End If

        Return ""
    End Function

    Private Function IsElevatedRole(role As String) As Boolean
        Dim r As String = If(role, "").Trim().ToUpperInvariant()
        Return r = "MANAGER" OrElse r = "ADMIN" OrElse r = "ADMINISTRATOR"
    End Function

    Private Function VerifyStoredPassword(inputPassword As String, storedPasswordHash As String) As Boolean
        If String.IsNullOrWhiteSpace(storedPasswordHash) Then Return False

        If storedPasswordHash.StartsWith("$2a$") OrElse storedPasswordHash.StartsWith("$2b$") OrElse storedPasswordHash.StartsWith("$2y$") Then
            Return BCrypt.Net.BCrypt.Verify(inputPassword, storedPasswordHash)
        End If

        Dim legacy As String = ComputeSha256Base64(inputPassword)
        Return String.Equals(legacy, storedPasswordHash, StringComparison.Ordinal)
    End Function

    Private Function ComputeSha256Base64(value As String) As String
        Using sha As System.Security.Cryptography.SHA256 = System.Security.Cryptography.SHA256.Create()
            Dim bytes = System.Text.Encoding.UTF8.GetBytes(value)
            Dim hash = sha.ComputeHash(bytes)
            Return Convert.ToBase64String(hash)
        End Using
    End Function

    ' Add near other private fields in class Sales
    Private Shared persistedOrderList As New List(Of Dictionary(Of String, Object))()
    Private Shared persistedDiscountType As String = "None"
    Private Shared persistedDiscountValue As Decimal = 0D
    Private Shared persistedDiscountAmount As Decimal = 0D
    Private Shared persistedDiscountedItemProductId As Integer? = Nothing
    Private Shared persistedDiscountedItemName As String = ""
    Private Shared persistedSelectedCustomerId As Integer? = Nothing
    Private Shared persistedSelectedCustomerName As String = Nothing
    Private Shared persistedSelectedCustomerPhone As String = ""
    Private Shared persistedSelectedCustomerEmail As String = ""
    Private Shared persistedSelectedCustomerTIN As String = ""
    Private Shared persistedSelectedCustomerType As String = "Walk-in"
    Private Shared persistedSelectedPaymentMethod As String = "Cash"
    Private Shared persistedPaymentReference As String = ""
    ' Add near other class fields
    Private Shared ReadOnly CartStateFolder As String = Path.Combine(Application.StartupPath, "cartstate")

    Private Function CloneOrderList(source As List(Of Dictionary(Of String, Object))) As List(Of Dictionary(Of String, Object))
        Dim cloned As New List(Of Dictionary(Of String, Object))()
        If source Is Nothing Then Return cloned

        For Each item In source
            Dim copy As New Dictionary(Of String, Object)()
            For Each kv In item
                copy(kv.Key) = kv.Value
            Next
            cloned.Add(copy)
        Next
        Return cloned
    End Function

    Private Shared Function GetCartStateFilePath(username As String) As String
        If String.IsNullOrWhiteSpace(username) Then
            Return Nothing
        End If

        Dim safeUsername As String = Regex.Replace(username, "[^\w\-]", "_")
        If String.IsNullOrWhiteSpace(safeUsername) Then
            safeUsername = "unknown"
        End If

        If Not Directory.Exists(CartStateFolder) Then
            Directory.CreateDirectory(CartStateFolder)
        End If

        Return Path.Combine(CartStateFolder, $"cart_{safeUsername}.json")
    End Function

    Public Sub PersistCartState() Implements IDraftPersistable.PersistDraft
        Try
            Dim filePath As String = GetCartStateFilePath(frmLoginvb.LoggedInUsername)
            If String.IsNullOrWhiteSpace(filePath) Then
                Return
            End If

            Dim snapshot As New CartStateSnapshot With {
                .CurrentOrderList = CloneOrderList(currentOrderList),
                .DiscountType = discountType,
                .DiscountValue = discountValue,
                .DiscountAmount = discountAmount,
                .DiscountedItemProductId = discountedItemProductId,
                .DiscountedItemName = discountedItemName,
                .SelectedCustomerId = selectedCustomerId,
                .SelectedCustomerName = selectedCustomerName,
                .SelectedCustomerPhone = selectedCustomerPhone,
                .SelectedCustomerEmail = selectedCustomerEmail,
                .SelectedCustomerTIN = selectedCustomerTIN,
                .SelectedCustomerType = selectedCustomerType,
                .SelectedPaymentMethod = selectedPaymentMethod,
                .PaymentReference = paymentReference
            }

            Dim json As String = JsonConvert.SerializeObject(snapshot, Formatting.Indented)
            File.WriteAllText(filePath, json)
        Catch ex As Exception
            Console.WriteLine($"PersistCartState error: {ex.Message}")
        End Try
    End Sub

    Private Sub RestoreCartState()
        Try
            Dim filePath As String = GetCartStateFilePath(frmLoginvb.LoggedInUsername)
            If String.IsNullOrWhiteSpace(filePath) OrElse Not File.Exists(filePath) Then
                Return
            End If

            ' Cart persisted on a previous day is stale: clear it instead of restoring it
            Try
                If File.GetLastWriteTime(filePath).Date < Date.Today Then
                    File.Delete(filePath)
                    Return
                End If
            Catch ex As Exception
                ' fall through to normal restore if the timestamp can't be read
            End Try

            Dim json As String = File.ReadAllText(filePath)
            Dim snapshot As CartStateSnapshot = JsonConvert.DeserializeObject(Of CartStateSnapshot)(json)
            If snapshot Is Nothing Then
                Return
            End If

            currentOrderList = CloneOrderList(snapshot.CurrentOrderList)
            discountType = If(snapshot.DiscountType, "None")
            discountValue = snapshot.DiscountValue
            discountAmount = snapshot.DiscountAmount
            discountedItemProductId = snapshot.DiscountedItemProductId
            discountedItemName = If(snapshot.DiscountedItemName, "")

            selectedCustomerId = snapshot.SelectedCustomerId
            selectedCustomerName = snapshot.SelectedCustomerName
            selectedCustomerPhone = snapshot.SelectedCustomerPhone
            selectedCustomerEmail = snapshot.SelectedCustomerEmail
            selectedCustomerTIN = snapshot.SelectedCustomerTIN
            selectedCustomerType = If(snapshot.SelectedCustomerType, "Walk-in")
            selectedPaymentMethod = If(snapshot.SelectedPaymentMethod, "Cash")
            paymentReference = snapshot.PaymentReference

            RefreshOrderDisplay()
        Catch ex As Exception
            Console.WriteLine($"RestoreCartState error: {ex.Message}")
        End Try
    End Sub

    Public Shared Sub ClearPersistedCartState(Optional username As String = Nothing)
        Try
            Dim userToClear As String = If(String.IsNullOrWhiteSpace(username), frmLoginvb.LoggedInUsername, username)
            Dim filePath As String = GetCartStateFilePath(userToClear)
            If Not String.IsNullOrWhiteSpace(filePath) AndAlso File.Exists(filePath) Then
                File.Delete(filePath)
            End If
        Catch ex As Exception
            Console.WriteLine($"ClearPersistedCartState error: {ex.Message}")
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

            ' Check if we're in barcode mode
            If Not String.IsNullOrEmpty(barcodeBuffer) Then
                barcodeBuffer = ""
                Return True
            End If

            If isNavigating Then
                Return True
            End If

            Dim result As DialogResult

            ' If showing products inside a category, Escape goes back to categories
            If backCategory IsNot Nothing AndAlso backCategory.Visible Then
                backCategory.PerformClick()
                Return True
            End If

            result = EscForm.ConfirmExit(Me)
            Me.Activate()
            If result = DialogResult.Yes Then
                If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Application Exit", "User exited the application via POS/Sales.")
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

    Private Function CreateButtonIcon(iconText As String, backColor As Color, Optional size As Integer = 36) As Bitmap
        Dim bmp As New Bitmap(size, size)
        Using g As Graphics = Graphics.FromImage(bmp)
            g.SmoothingMode = SmoothingMode.AntiAlias
            Using brush As New SolidBrush(backColor)
                g.FillEllipse(brush, 2, 2, size - 4, size - 4)
            End Using
            Using f As New Font("Segoe UI", size \ 2 - 2, FontStyle.Bold)
                TextRenderer.DrawText(g, iconText, f, New Rectangle(0, 0, size, size), Color.White, Color.Transparent, TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
            End Using
        End Using
        Return bmp
    End Function

    Private Sub PictureBox9_Click(sender As Object, e As EventArgs) Handles PictureBox9.Click

    End Sub
End Class
