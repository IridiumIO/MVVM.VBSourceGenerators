Imports System.Threading

Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input


Partial Public Class MainWindowViewModel : Inherits ObservableObject


    <ObservableProperty>
    <NotifyPropertyChangedFor(NameOf(FullName))>
    <NotifyPropertyChangedRecipients>
    Private _firstName As String = "John"

    <ObservableProperty>
    <NotifyPropertyChangedFor(NameOf(FullName))>
    Private _lastName As String = "Cena"

    Public ReadOnly Property FullName
        Get
            Return $"{FirstName} {LastName}"
        End Get
    End Property

    Private Sub OnLastNameChanged(value As String)
        Debug.WriteLine($"OnLastNameChanged called with: {value}")
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

    <RelayCommand>
    Public Async Function SetAsyncFunction(str As String, cToken As CancellationToken) As Task(Of String)
        Await Task.Delay(1000)
        Debug.WriteLine($"SetAsyncFunction called")
        Return "Hello from async function"
    End Function

    Public ReadOnly Property SetAsyncFunctionCommand1 As CommunityToolkit.Mvvm.Input.IAsyncRelayCommand = New AsyncRelayCommand(Of String)(AddressOf SetAsyncFunction)
    Public ReadOnly Property SetAsyncFunctionCommand2 As CommunityToolkit.Mvvm.Input.IAsyncRelayCommand = New AsyncRelayCommand(Of String)(New Func(Of String, CancellationToken, Task)(AddressOf SetAsyncFunction))

    Public ReadOnly Property TestFullCommand As IRelayCommand = New AsyncRelayCommand(Of String)(AddressOf TestFull, AddressOf CanTestFull)

    Private Async Function TestFull(val As String, ctoken As CancellationToken) As Task
        Await Task.Delay(1000) ' Simulate some delay for the example
        Throw New Exception("FUck")
        Debug.WriteLine($"TestFull called with: {val}")
    End Function

    Private Function CanTestFull(val As String) As Boolean
        If val = "Test" Then
            Return True
        End If
        Return False
    End Function


End Class
