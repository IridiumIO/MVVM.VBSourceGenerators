# MVVM.VBSourceGenerators

The [CommunityToolkit.MVVM](https://github.com/CommunityToolkit/dotnet) source generators only work in C#. This package augments the toolkit to allow some of the generators to work for VB.NET.



## Working

- [`<ObservableProperty>`](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty) 
    - Generates a public property with OnPropertyChanged notification as well as most helper methods defined as per the CommunityToolkit.
- [`<NotifyPropertyChangedFor(NameOf(T))>`](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty#notifying-dependent-properties) 
    - Generates a property with change notification for existing properties.
- [`<RelayCommand>`](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/relaycommand)
   - Can decorate `Sub`, `Async Sub`, or `Async Function`
   - Supports `CanExecute` callback functionality
   - Supports parameter passthrough
   - Supports `CancellationToken` for async commands
   - Supports all `RelayCommand` [attribute properties](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/relaycommand) from the CommunityToolkit.MVVM library, *except* for the CanExecute property
     - CanExecute is defined by simply naming a Sub `Can[MethodName]`.
- [`NotifyCanExecuteChangedFor(NameOf(T))`](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty#notifying-dependent-commands)
   - Use on an `ObservableProperty` marked field to automatically notify the `CanExecute` state of a command when the property changes.
- [`NotifyPropertyChangedRecipients`](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty#sending-notification-messages)
   - Use on an `ObservableProperty` marked field to send a message when the property changes
   - Does not support use on existing properties, only on generated properties.

## Planned
- [Property Validation Attributes](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty#requesting-property-validation)
- [INotifyPropertyChanged Attribute](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/inotifypropertychanged)
- [RelayCommand Custom Attributes](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/relaycommand#adding-custom-attributes)

## Other TODO
- [ ] Add On`PropertyName`Changed parameterless overload (currently only works if a parameter is passed)

## Installation

1. Add the NuGet package for MVVM.VBSourceGenerators to your VB.NET project:
    ```shell
    dotnet add package IridiumIO.MVVM.VBSourceGenerators
    ```
2. Ensure that you also reference `CommunityToolkit.MVVM` in your project.

## ObservableProperty Usage

1. In your VB.NET ViewModel, use the `[ObservableProperty]` attribute as you would in C#
2. Make sure your fields follow the naming convention of starting with an underscore (e.g., `_name`).
3. You can then use the generated Uppercase property `Name` in your code.

Example 1:

```vbnet
Imports CommunityToolkit.Mvvm.ComponentModel

Partial Public Class MyViewModel
    Inherits ObservableObject

    <ObservableProperty>
    Private _name As String
End Class
```

&nbsp;

Example 2: Including `NotifyPropertyChangedFor`

```vbnet
Imports CommunityToolkit.Mvvm.ComponentModel

Partial Public Class PersonViewModel
    Inherits ObservableObject

    <ObservableProperty>
    <NotifyPropertyChangedFor(NameOf(FullName))>
    Private _firstName As String

    <ObservableProperty>
    <NotifyPropertyChangedFor(NameOf(FullName))>
    Private _lastName As String

    Public ReadOnly Property FullName As String
        Get
            Return $"{FirstName} {LastName}"
        End Get
    End Property

End Class
```

#### Additional Features:
- As defined [here](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty), additional methods for On[Name]Changed and On[Name]Changing are also generated as partial methods and can be included. 

## RelayCommand Usage

1. In your VB.NET ViewModel, use the `[RelayCommand]` attribute as you would in C# on a method.


#### Examples:

```vbnet
Imports CommunityToolkit.Mvvm.ComponentModel

Partial Public Class MyViewModel
    Inherits ObservableObject

    <RelayCommand>
    Private Sub Save()
        ' Save logic here
    End Sub

    <RelayCommand>
    Private Sub SelectItem(item As String)
        ' Logic to select an item
    End Sub

    'Will generate two commands:
    'LoadDataAsyncCommand and CancelLoadDataAsyncCommand
    <RelayCommand(IncludeCancelCommand:=True)>
    Private Async Function LoadDataAsync(ctx As CancellationToken) As Task
        ' Load data logic here
        Await Task.Delay(1000) ' Simulating async work
    End Function

End Class
```

&nbsp;

#### Example 2: Including CanExecute callback

By creating a function that returns a `Boolean` and prefixing it with `Can`, you can control whether the command can execute.

```vbnet
Imports CommunityToolkit.Mvvm.ComponentModel

Partial Public Class PersonViewModel
    Inherits ObservableObject

    <RelayCommand>
    Private Sub SaveData()
        ' Logic to save data
    End Sub

    Private Function CanSaveData() As Boolean
        ' Logic to determine if saving is allowed
         Return MyData.Count > 0
    End Function

End Class
```
