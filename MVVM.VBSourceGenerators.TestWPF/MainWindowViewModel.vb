Imports System.ComponentModel
Imports System.Threading

Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input


Partial Public Class MainWindowViewModel : Inherits ObservableRecipient


    <ObservableProperty>
    <NotifyPropertyChangedRecipients>
    Private _RandomObject As RandomClass = New RandomClass()


    <ObservableProperty>
    <NotifyPropertyChangedFor(NameOf(FullName))>
    Private _firstName As String = "John"

    <ObservableProperty>
    <NotifyPropertyChangedFor(NameOf(FullName))>
    Private _lastName As String = "Cena"

    Public ReadOnly Property FullName
        Get
            Return $"{FirstName} {LastName}"
        End Get
    End Property


    <ObservableProperty>
    Private _NewedUp As New List(Of String)
    Private Sub OnLastNameChanged(value As String)
        Debug.WriteLine($"OnLastNameChanged called with: {value}")
        RandomObject = New RandomClass()
    End Sub


    Private Sub OnFirstNameChanged(value As String)
        Debug.WriteLine($"OnFirstNameChanged called with: {value}")
    End Sub


    Public Sub New()
    End Sub

    <RelayCommand>
    Public Sub SetSimple(name As String)
        Debug.WriteLine($"SetSimple called")

    End Sub

    Private Function CanSetSimple(name As String) As Boolean
        Task.Delay(1000).Wait() ' Simulate some delay for the example)
        Debug.WriteLine($"CanSetSimple called with: {name}")

        If name = "John" Then
            Return True
        End If
        Return False
    End Function


    <RelayCommand>
    Public Sub SetSimpleWithParameter(value As String)
        Debug.WriteLine($"SetSimpleWithParameter called with: {value}")
    End Sub

    <RelayCommand>
    Public Function SetSimpleFunction() As String
        Debug.WriteLine($"SetSimpleWithParameter called with: x")
        Return "Hello"
    End Function


    <RelayCommand>
    Public Async Sub SetAsyncSub()
        Await Task.Delay(1000)
        Debug.WriteLine($"SetAsyncSub called")
    End Sub


    'Public ReadOnly Property SetAsyncFunctionCommand As CommunityToolkit.Mvvm.Input.IAsyncRelayCommand = New CommunityToolkit.Mvvm.Input.AsyncRelayCommand(Of Global.System.Threading.CancellationToken)(AddressOf SetAsyncFunction, options:=CommunityToolkit.Mvvm.Input.AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler Or CommunityToolkit.Mvvm.Input.AsyncRelayCommandOptions.AllowConcurrentExecutions)
    'Public ReadOnly Property SetAsyncFunctionCancelCommand As System.Windows.Input.ICommand = CommunityToolkit.Mvvm.Input.IAsyncRelayCommandExtensions.CreateCancelCommand(SetAsyncFunctionCommand)

    <RelayCommand>
    Public Async Function SetAsyncFunction(ctoken As CancellationToken) As Task(Of String)

        Await Task.Delay(1000, ctoken)
        Debug.WriteLine($"SetAsyncFunction called")
        Return "Hello from async function"
    End Function

    <RelayCommand>
    Public Async Function SetAsyncFunction2() As Task(Of String)

        Await Task.Delay(1000)
        Debug.WriteLine($"SetAsyncFunction called")
        Return "Hello from async function"
    End Function

    <RelayCommand>
    Public Async Function SetAsyncFunction3(str As String, cToken As System.Threading.CancellationToken) As Task(Of String)

        Await Task.Delay(1000, cToken)
        Debug.WriteLine($"SetAsyncFunction called")
        Return "Hello from async function"
    End Function

    <RelayCommand>
    Public Async Function SetAsyncFunction4(cToken As CancellationToken, str As String) As Task(Of String)

        Await Task.Delay(1000, cToken)
        Debug.WriteLine($"SetAsyncFunction called")
        Return "Hello from async function"
    End Function


End Class



Public Class RandomClass

End Class