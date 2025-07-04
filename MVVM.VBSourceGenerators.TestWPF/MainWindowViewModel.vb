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

    <RelayCommand(IncludeCancelCommand:=False, AllowConcurrentExecutions:=True, FlowExceptionsToTaskScheduler:=True)>
    Public Async Function SetAsyncFunction() As Task(Of String)

        Await Task.Delay(1000)
        Debug.WriteLine($"SetAsyncFunction called")
        Return "Hello from async function"
    End Function


End Class
