Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Messaging
Imports CommunityToolkit.Mvvm.Messaging.Messages

Public Class TestRecipientViewModel : Inherits ObservableRecipient : Implements IRecipient(Of PropertyChangedMessage(Of RandomClass))




    Public Sub Receive(message As PropertyChangedMessage(Of RandomClass)) Implements IRecipient(Of PropertyChangedMessage(Of RandomClass)).Receive

        Dim sender = TryCast(message.Sender, MainWindowViewModel)

        If sender Is Nothing Then
            Debug.WriteLine("Received message from an unknown sender.")
            Return
        End If

        If message.PropertyName = NameOf(sender.RandomObject) Then
            Debug.WriteLine($"Received message for property '{message.PropertyName}' with value: {message.NewValue}")
        Else
            Debug.WriteLine($"Received message for an unrelated property: {message.PropertyName}")
        End If

    End Sub
End Class
