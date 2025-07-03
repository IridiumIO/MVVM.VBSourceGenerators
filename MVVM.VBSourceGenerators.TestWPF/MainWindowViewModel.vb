Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input


Partial Public Class MainWindowViewModel : Inherits ObservableObject


    <ObservableProperty>
    <NotifyPropertyChangedFor(NameOf(FullName))>
    Private _firstName As String = "John"

    <ObservableProperty>
    <NotifyPropertyChangedFor(NameOf(FullName))>
    Private _lastName As String = "Cena"

    Public ReadOnly Property FullName As String
        Get
            Return $"{FirstName} {LastName}"
        End Get
    End Property



    Public Sub New()
    End Sub

    <RelayCommand>
    Public Sub SetSimple()
        Debug.WriteLine($"SetSimple called")

    End Sub

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
    Public Async Function SetAsyncFunction(x As MainWindowViewModel) As Task
        If x Is Me Then
            Debug.WriteLine($"SetAsyncFunction called with self instance")
        End If
        Await Task.Delay(1000)
        Debug.WriteLine($"SetAsyncFunction called")
    End Function

    Public Function CanSetAsyncFunction() As Boolean
        Return Not String.IsNullOrEmpty(FirstName)
    End Function

End Class
