Class MainWindow


    Private _TestRecipient As TestRecipientViewModel = New TestRecipientViewModel()

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        Me.DataContext = New MainWindowViewModel()

        _TestRecipient.IsActive = True

    End Sub


End Class
