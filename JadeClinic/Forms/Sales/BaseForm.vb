Imports System.Drawing
Imports System.Windows.Forms
Imports Guna.UI2.WinForms

''' <summary>
''' Base form class that provides centralized color management
''' All forms should inherit from this class to ensure consistent styling
''' </summary>
Public Class BaseForm
    Inherits Form

    ' Dynamic color properties that connect to CompanySettingsManager
    Protected ReadOnly Property PrimaryColor As Color
        Get
            Return CompanySettingsManager.Instance.GetColor("PrimaryColor")
        End Get
    End Property

    Protected ReadOnly Property SecondaryColor As Color
        Get
            Return CompanySettingsManager.Instance.GetColor("SecondaryColor")
        End Get
    End Property

    Protected ReadOnly Property BackgroundDark As Color
        Get
            Return CompanySettingsManager.Instance.GetColor("BackgroundDark")
        End Get
    End Property

    Protected ReadOnly Property BackgroundMid As Color
        Get
            Return CompanySettingsManager.Instance.GetColor("BackgroundMid")
        End Get
    End Property

    Protected ReadOnly Property BackgroundLight As Color
        Get
            Return CompanySettingsManager.Instance.GetColor("BackgroundLight")
        End Get
    End Property

    Protected ReadOnly Property InteractiveColor As Color
        Get
            Return CompanySettingsManager.Instance.GetColor("InteractiveColor")
        End Get
    End Property

    Protected ReadOnly Property TextPrimary As Color
        Get
            Return CompanySettingsManager.Instance.GetColor("TextPrimary")
        End Get
    End Property

    Protected ReadOnly Property TextSecondary As Color
        Get
            Return CompanySettingsManager.Instance.GetColor("TextSecondary")
        End Get
    End Property

    Protected ReadOnly Property SuccessColor As Color
        Get
            Return CompanySettingsManager.Instance.GetColor("SuccessColor")
        End Get
    End Property

    Protected ReadOnly Property ErrorColor As Color
        Get
            Return CompanySettingsManager.Instance.GetColor("ErrorColor")
        End Get
    End Property

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        ApplyBasicTheme()
    End Sub

    ''' <summary>
    ''' Applies basic theme colors to the form
    ''' </summary>
    Protected Overridable Sub ApplyBasicTheme()
        Me.BackColor = BackgroundDark
    End Sub

    ''' <summary>
    ''' Call this method to refresh colors after theme changes
    ''' </summary>
    Public Sub RefreshTheme()
        ApplyBasicTheme()
        Me.Refresh()
    End Sub
End Class